using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity.Netcode
{
    /// <summary>
    /// Class that handles object spawning
    /// </summary>
    public class NetworkSpawnManager
    {
        // Stores the objects that need to be shown at end-of-frame
        internal Dictionary<ulong, List<NetworkObject>> ObjectsToShowToClient = new Dictionary<ulong, List<NetworkObject>>();

        /// <summary>
        /// The currently spawned objects
        /// </summary>
        public readonly Dictionary<ulong, NetworkObject> SpawnedObjects = new Dictionary<ulong, NetworkObject>();

        /// <summary>
        /// A list of the spawned objects
        /// </summary>
        public readonly HashSet<NetworkObject> SpawnedObjectsList = new HashSet<NetworkObject>();

        /// <summary>
        /// Use to get all NetworkObjects owned by a client
        /// Ownership to Objects Table Format:
        /// [ClientId][NetworkObjectId][NetworkObject]
        /// Server: Keeps track of all clients' ownership
        /// Client: Keeps track of only its ownership
        /// </summary>
        public readonly Dictionary<ulong, Dictionary<ulong, NetworkObject>> OwnershipToObjectsTable = new Dictionary<ulong, Dictionary<ulong, NetworkObject>>();

        /// <summary>
        /// Object to Ownership Table:
        /// [NetworkObjectId][ClientId]
        /// Used internally to find the client Id that currently owns
        /// the NetworkObject
        /// </summary>
        private Dictionary<ulong, ulong> m_ObjectToOwnershipTable = new Dictionary<ulong, ulong>();

        internal void MarkObjectForShowingTo(NetworkObject networkObject, ulong clientId)
        {
            if (!ObjectsToShowToClient.ContainsKey(clientId))
            {
                ObjectsToShowToClient.Add(clientId, new List<NetworkObject>());
            }
            ObjectsToShowToClient[clientId].Add(networkObject);
        }

        // returns whether any matching objects would have become visible and were returned to hidden state
        internal bool RemoveObjectFromShowingTo(NetworkObject networkObject, ulong clientId)
        {
            var ret = false;
            if (!ObjectsToShowToClient.ContainsKey(clientId))
            {
                return false;
            }

            // probably overkill, but deals with multiple entries
            while (ObjectsToShowToClient[clientId].Contains(networkObject))
            {
                Debug.LogWarning(
                    "Object was shown and hidden from the same client in the same Network frame. As a result, the client will _not_ receive a NetworkSpawn");
                ObjectsToShowToClient[clientId].Remove(networkObject);
                ret = true;
            }

            if (ret)
            {
                networkObject.Observers.Remove(clientId);
            }

            return ret;
        }

        /// <summary>
        /// Used to update a NetworkObject's ownership
        /// </summary>
        internal void UpdateOwnershipTable(NetworkObject networkObject, ulong newOwner, bool isRemoving = false)
        {
            var previousOwner = newOwner;

            // Use internal lookup table to see if the NetworkObject has a previous owner
            if (m_ObjectToOwnershipTable.ContainsKey(networkObject.NetworkObjectId))
            {
                // Keep track of the previous owner's ClientId
                previousOwner = m_ObjectToOwnershipTable[networkObject.NetworkObjectId];

                // We are either despawning (remove) or changing ownership (assign)
                if (isRemoving)
                {
                    m_ObjectToOwnershipTable.Remove(networkObject.NetworkObjectId);
                }
                else
                {
                    m_ObjectToOwnershipTable[networkObject.NetworkObjectId] = newOwner;
                }
            }
            else
            {
                // Otherwise, just add a new lookup entry
                m_ObjectToOwnershipTable.Add(networkObject.NetworkObjectId, newOwner);
            }

            // Check to see if we had a previous owner
            if (previousOwner != newOwner && OwnershipToObjectsTable.ContainsKey(previousOwner))
            {
                // Before updating the previous owner, assure this entry exists
                if (OwnershipToObjectsTable[previousOwner].ContainsKey(networkObject.NetworkObjectId))
                {
                    // Remove the previous owner's entry
                    OwnershipToObjectsTable[previousOwner].Remove(networkObject.NetworkObjectId);

                    // If we are removing the entry (i.e. despawning or client lost ownership)
                    if (isRemoving)
                    {
                        return;
                    }
                }
                else
                {
                    // Really, as long as UpdateOwnershipTable is invoked when ownership is gained or lost this should never happen
                    throw new Exception($"Client-ID {previousOwner} had a partial {nameof(m_ObjectToOwnershipTable)} entry! Potentially corrupted {nameof(OwnershipToObjectsTable)}?");
                }
            }

            // If the owner doesn't have an entry then create one
            if (!OwnershipToObjectsTable.ContainsKey(newOwner))
            {
                OwnershipToObjectsTable.Add(newOwner, new Dictionary<ulong, NetworkObject>());
            }

            // Sanity check to make sure we don't already have this entry (we shouldn't)
            if (!OwnershipToObjectsTable[newOwner].ContainsKey(networkObject.NetworkObjectId))
            {
                // Add the new ownership entry
                OwnershipToObjectsTable[newOwner].Add(networkObject.NetworkObjectId, networkObject);
            }
            else if (isRemoving)
            {
                OwnershipToObjectsTable[previousOwner].Remove(networkObject.NetworkObjectId);
            }
            else if (NetworkManager.LogLevel == LogLevel.Developer)
            {
                NetworkLog.LogWarning($"Setting ownership twice? Client-ID {previousOwner} already owns NetworkObject ID {networkObject.NetworkObjectId}!");
            }
        }

        /// <summary>
        /// Returns a list of all NetworkObjects that belong to a client.
        /// </summary>
        /// <param name="clientId">the client's id  <see cref="NetworkManager.LocalClientId"/></param>
        /// <returns>returns the list of <see cref="NetworkObject"/>s owned by the client</returns>
        public List<NetworkObject> GetClientOwnedObjects(ulong clientId)
        {
            if (!OwnershipToObjectsTable.ContainsKey(clientId))
            {
                OwnershipToObjectsTable.Add(clientId, new Dictionary<ulong, NetworkObject>());
            }
            return OwnershipToObjectsTable[clientId].Values.ToList();
        }

        /// <summary>
        /// Gets the NetworkManager associated with this SpawnManager.
        /// </summary>
        public NetworkManager NetworkManager { get; }

        internal readonly Queue<ReleasedNetworkId> ReleasedNetworkObjectIds = new Queue<ReleasedNetworkId>();
        private ulong m_NetworkObjectIdCounter;

        // A list of target ClientId, use when sending despawn commands. Kept as a member to reduce memory allocations
        private List<ulong> m_TargetClientIds = new List<ulong>();

        internal ulong GetNetworkObjectId()
        {
            if (ReleasedNetworkObjectIds.Count > 0 && NetworkManager.NetworkConfig.RecycleNetworkIds && (NetworkManager.RealTimeProvider.UnscaledTime - ReleasedNetworkObjectIds.Peek().ReleaseTime) >= NetworkManager.NetworkConfig.NetworkIdRecycleDelay)
            {
                return ReleasedNetworkObjectIds.Dequeue().NetworkId;
            }

            m_NetworkObjectIdCounter++;

            return m_NetworkObjectIdCounter;
        }

        /// <summary>
        /// Returns the local player object or null if one does not exist
        /// </summary>
        /// <returns>The local player object or null if one does not exist</returns>
        public NetworkObject GetLocalPlayerObject()
        {
            return GetPlayerNetworkObject(NetworkManager.LocalClientId);
        }


        /// <summary>
        /// Returns the player object with a given clientId or null if one does not exist. This is only valid server side.
        /// </summary>
        /// <param name="clientId">the client identifier of the player</param>
        /// <returns>The player object with a given clientId or null if one does not exist</returns>
        public NetworkObject GetPlayerNetworkObject(ulong clientId)
        {
            if (!NetworkManager.IsServer && NetworkManager.LocalClientId != clientId)
            {
                throw new NotServerException("Only the server can find player objects from other clients.");
            }

            if (TryGetNetworkClient(clientId, out NetworkClient networkClient))
            {
                return networkClient.PlayerObject;
            }

            return null;
        }

        /// <summary>
        /// Helper function to get a network client for a clientId from the NetworkManager.
        /// On the server this will check the <see cref="NetworkManager.ConnectedClients"/> list.
        /// On a non-server this will check the <see cref="NetworkManager.LocalClient"/> only.
        /// </summary>
        /// <param name="clientId">The clientId for which to try getting the NetworkClient for.</param>
        /// <param name="networkClient">The found NetworkClient. Null if no client was found.</param>
        /// <returns>True if a NetworkClient with a matching id was found else false.</returns>
        private bool TryGetNetworkClient(ulong clientId, out NetworkClient networkClient)
        {
            if (NetworkManager.IsServer)
            {
                return NetworkManager.ConnectedClients.TryGetValue(clientId, out networkClient);
            }

            if (NetworkManager.LocalClient != null && clientId == NetworkManager.LocalClient.ClientId)
            {
                networkClient = NetworkManager.LocalClient;
                return true;
            }

            networkClient = null;
            return false;
        }

        internal void RemoveOwnership(NetworkObject networkObject)
        {
            ChangeOwnership(networkObject, NetworkManager.ServerClientId);
        }

        internal void ChangeOwnership(NetworkObject networkObject, ulong clientId)
        {
            if (!NetworkManager.IsServer)
            {
                throw new NotServerException("Only the server can change ownership");
            }

            if (!networkObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            var previous = networkObject.OwnerClientId;
            // Assign the new owner
            networkObject.OwnerClientId = clientId;

            // Always notify locally on the server when ownership is lost
            networkObject.InvokeBehaviourOnLostOwnership();

            networkObject.MarkVariablesDirty(true);
            NetworkManager.BehaviourUpdater.AddForUpdate(networkObject);

            // Server adds entries for all client ownership
            UpdateOwnershipTable(networkObject, networkObject.OwnerClientId);

            // Always notify locally on the server when a new owner is assigned
            networkObject.InvokeBehaviourOnGainedOwnership();

            var message = new ChangeOwnershipMessage
            {
                NetworkObjectId = networkObject.NetworkObjectId,
                OwnerClientId = networkObject.OwnerClientId
            };

            foreach (var client in NetworkManager.ConnectedClients)
            {
                if (networkObject.IsNetworkVisibleTo(client.Value.ClientId))
                {
                    var size = NetworkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.ReliableSequenced, client.Value.ClientId);
                    NetworkManager.NetworkMetrics.TrackOwnershipChangeSent(client.Key, networkObject, size);
                }
            }

            // After we have sent the change ownership message to all client observers, invoke the ownership changed notification.
            /// !!Important!!
            /// This gets called specifically *after* sending the ownership message so any additional messages that need to proceed an ownership
            /// change can be sent from NetworkBehaviours that override the <see cref="NetworkBehaviour.OnOwnershipChanged"></see>
            networkObject.InvokeOwnershipChanged(previous, clientId);
        }

        internal bool HasPrefab(NetworkObject.SceneObject sceneObject)
        {
            if (!NetworkManager.NetworkConfig.EnableSceneManagement || !sceneObject.IsSceneObject)
            {
                if (NetworkManager.PrefabHandler.ContainsHandler(sceneObject.Hash))
                {
                    return true;
                }
                if (NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.TryGetValue(sceneObject.Hash, out var networkPrefab))
                {
                    switch (networkPrefab.Override)
                    {
                        default:
                        case NetworkPrefabOverride.None:
                            return networkPrefab.Prefab != null;
                        case NetworkPrefabOverride.Hash:
                        case NetworkPrefabOverride.Prefab:
                            return networkPrefab.OverridingTargetPrefab != null;
                    }
                }

                return false;
            }
            var networkObject = NetworkManager.SceneManager.GetSceneRelativeInSceneNetworkObject(sceneObject.Hash, sceneObject.NetworkSceneHandle);
            return networkObject != null;
        }

        /// <summary>
        /// Creates a local NetowrkObject to be spawned.
        /// </summary>
        /// <remarks>
        /// For most cases this is client-side only, with the exception of when the server
        /// is spawning a player.
        /// </remarks>
        internal NetworkObject CreateLocalNetworkObject(NetworkObject.SceneObject sceneObject)
        {
            NetworkObject networkObject = null;
            var globalObjectIdHash = sceneObject.Hash;
            var position = sceneObject.HasTransform ? sceneObject.Transform.Position : default;
            var rotation = sceneObject.HasTransform ? sceneObject.Transform.Rotation : default;
            var scale = sceneObject.HasTransform ? sceneObject.Transform.Scale : default;
            var parentNetworkId = sceneObject.HasParent ? sceneObject.ParentObjectId : default;
            var worldPositionStays = (!sceneObject.HasParent) || sceneObject.WorldPositionStays;
            var isSpawnedByPrefabHandler = false;

            // If scene management is disabled or the NetworkObject was dynamically spawned
            if (!NetworkManager.NetworkConfig.EnableSceneManagement || !sceneObject.IsSceneObject)
            {
                // If the prefab hash has a registered INetworkPrefabInstanceHandler derived class
                if (NetworkManager.PrefabHandler.ContainsHandler(globalObjectIdHash))
                {
                    // Let the handler spawn the NetworkObject
                    networkObject = NetworkManager.PrefabHandler.HandleNetworkPrefabSpawn(globalObjectIdHash, sceneObject.OwnerClientId, position, rotation);
                    networkObject.NetworkManagerOwner = NetworkManager;
                    isSpawnedByPrefabHandler = true;
                }
                else
                {
                    // See if there is a valid registered NetworkPrefabOverrideLink associated with the provided prefabHash
                    GameObject networkPrefabReference = null;
                    if (NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks.ContainsKey(globalObjectIdHash))
                    {
                        switch (NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks[globalObjectIdHash].Override)
                        {
                            default:
                            case NetworkPrefabOverride.None:
                                networkPrefabReference = NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks[globalObjectIdHash].Prefab;
                                break;
                            case NetworkPrefabOverride.Hash:
                            case NetworkPrefabOverride.Prefab:
                                networkPrefabReference = NetworkManager.NetworkConfig.Prefabs.NetworkPrefabOverrideLinks[globalObjectIdHash].OverridingTargetPrefab;
                                break;
                        }
                    }

                    // If not, then there is an issue (user possibly didn't register the prefab properly?)
                    if (networkPrefabReference == null)
                    {
                        if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                        {
                            NetworkLog.LogError($"Failed to create object locally. [{nameof(globalObjectIdHash)}={globalObjectIdHash}]. {nameof(NetworkPrefab)} could not be found. Is the prefab registered with {nameof(NetworkManager)}?");
                        }
                    }
                    else
                    {
                        // Create prefab instance
                        networkObject = UnityEngine.Object.Instantiate(networkPrefabReference).GetComponent<NetworkObject>();
                        networkObject.NetworkManagerOwner = NetworkManager;
                    }
                }
            }
            else // Get the in-scene placed NetworkObject
            {
                networkObject = NetworkManager.SceneManager.GetSceneRelativeInSceneNetworkObject(globalObjectIdHash, sceneObject.NetworkSceneHandle);

                if (networkObject == null)
                {
                    if (NetworkLog.CurrentLogLevel <= LogLevel.Error)
                    {
                        NetworkLog.LogError($"{nameof(NetworkPrefab)} hash was not found! In-Scene placed {nameof(NetworkObject)} soft synchronization failure for Hash: {globalObjectIdHash}!");
                    }
                }

                // Since this NetworkObject is an in-scene placed NetworkObject, if it is disabled then enable it so
                // NetworkBehaviours will have their OnNetworkSpawn method invoked
                if (networkObject != null && !networkObject.gameObject.activeInHierarchy)
                {
                    networkObject.gameObject.SetActive(true);
                }
            }

            if (networkObject != null)
            {
                networkObject.DestroyWithScene = sceneObject.DestroyWithScene;
                networkObject.NetworkSceneHandle = sceneObject.NetworkSceneHandle;

                // SPECIAL CASE FOR IN-SCENE PLACED:  (only when the parent has a NetworkObject)
                // This is a special case scenario where a late joining client has joined and loaded one or
                // more scenes that contain nested in-scene placed NetworkObject children yet the server's
                // synchronization information does not indicate the NetworkObject in question has a parent.
                // Under this scenario, we want to remove the parent before spawning and setting the transform values.
                if (sceneObject.IsSceneObject && !sceneObject.HasParent && networkObject.transform.parent != null)
                {
                    // if the in-scene placed NetworkObject has a parent NetworkObject but the synchronization information does not
                    // include parenting, then we need to force the removal of that parent
                    if (networkObject.transform.parent.GetComponent<NetworkObject>() != null)
                    {
                        // remove the parent
                        networkObject.ApplyNetworkParenting(true, true);
                    }
                }

                // Set the transform unless we were spawned by a prefab handler
                // Note: prefab handlers are provided the position and rotation
                // but it is up to the user to set those values
                if (sceneObject.HasTransform && !isSpawnedByPrefabHandler)
                {
                    // If world position stays is true or we have auto object parent synchronization disabled
                    // then we want to apply the position and rotation values world space relative
                    if (worldPositionStays || !networkObject.AutoObjectParentSync)
                    {
                        networkObject.transform.position = position;
                        networkObject.transform.rotation = rotation;
                    }
                    else
                    {
                        networkObject.transform.localPosition = position;
                        networkObject.transform.localRotation = rotation;
                    }

                    // SPECIAL CASE:
                    // Since players are created uniquely we don't apply scale because
                    // the ConnectionApprovalResponse does not currently provide the
                    // ability to specify scale. So, we just use the default scale of
                    // the network prefab used to represent the player.
                    // Note: not doing this would set the player's scale to zero since
                    // that is the default value of Vector3.
                    if (!sceneObject.IsPlayerObject)
                    {
                        // Since scale is always applied to local space scale, we do the transform
                        // space logic during serialization such that it works out whether AutoObjectParentSync
                        // is enabled or not (see NetworkObject.SceneObject)
                        networkObject.transform.localScale = scale;
                    }
                }

                if (sceneObject.HasParent)
                {
                    // Go ahead and set network parenting properties, if the latest parent is not set then pass in null
                    // (we always want to set worldPositionStays)
                    ulong? parentId = null;
                    if (sceneObject.IsLatestParentSet)
                    {
                        parentId = parentNetworkId;
                    }
                    networkObject.SetNetworkParenting(parentId, worldPositionStays);
                }


                // Dynamically spawned NetworkObjects that occur during a LoadSceneMode.Single load scene event are migrated into the DDOL
                // until the scene is loaded. They are then migrated back into the newly loaded and currently active scene.
                if (!sceneObject.IsSceneObject && NetworkSceneManager.IsSpawnedObjectsPendingInDontDestroyOnLoad)
                {
                    UnityEngine.Object.DontDestroyOnLoad(networkObject.gameObject);
                }
            }
            return networkObject;
        }

        // Ran on both server and client
        internal void SpawnNetworkObjectLocally(NetworkObject networkObject, ulong networkId, bool sceneObject, bool playerObject, ulong ownerClientId, bool destroyWithScene)
        {
            if (networkObject == null)
            {
                throw new ArgumentNullException(nameof(networkObject), "Cannot spawn null object");
            }

            if (networkObject.IsSpawned)
            {
                throw new SpawnStateException("Object is already spawned");
            }

            if (!sceneObject)
            {
                var networkObjectChildren = networkObject.GetComponentsInChildren<NetworkObject>();
                if (networkObjectChildren.Length > 1)
                {
                    Debug.LogError("Spawning NetworkObjects with nested NetworkObjects is only supported for scene objects. Child NetworkObjects will not be spawned over the network!");
                }
            }

            SpawnNetworkObjectLocallyCommon(networkObject, networkId, sceneObject, playerObject, ownerClientId, destroyWithScene);
        }

        // Ran on both server and client
        internal void SpawnNetworkObjectLocally(NetworkObject networkObject, in NetworkObject.SceneObject sceneObject, bool destroyWithScene)
        {
            if (networkObject == null)
            {
                throw new ArgumentNullException(nameof(networkObject), "Cannot spawn null object");
            }

            if (networkObject.IsSpawned)
            {
                throw new SpawnStateException("Object is already spawned");
            }

            SpawnNetworkObjectLocallyCommon(networkObject, sceneObject.NetworkObjectId, sceneObject.IsSceneObject, sceneObject.IsPlayerObject, sceneObject.OwnerClientId, destroyWithScene);
        }

        private void SpawnNetworkObjectLocallyCommon(NetworkObject networkObject, ulong networkId, bool sceneObject, bool playerObject, ulong ownerClientId, bool destroyWithScene)
        {
            if (SpawnedObjects.ContainsKey(networkId))
            {
                Debug.LogWarning($"Trying to spawn {nameof(NetworkObject.NetworkObjectId)} {networkId} that already exists!");
                return;
            }

            networkObject.IsSpawned = true;
            networkObject.IsSceneObject = sceneObject;

            // Always check to make sure our scene of origin is properly set for in-scene placed NetworkObjects
            // Note: Always check SceneOriginHandle directly at this specific location.
            if (networkObject.IsSceneObject != false && networkObject.SceneOriginHandle == 0)
            {
                networkObject.SceneOrigin = networkObject.gameObject.scene;
            }

            // For integration testing, this makes sure that the appropriate NetworkManager is assigned to
            // the NetworkObject since it uses the NetworkManager.Singleton when not set
            if (networkObject.NetworkManagerOwner != NetworkManager)
            {
                networkObject.NetworkManagerOwner = NetworkManager;
            }

            networkObject.NetworkObjectId = networkId;

            networkObject.DestroyWithScene = sceneObject || destroyWithScene;

            networkObject.OwnerClientId = ownerClientId;

            networkObject.IsPlayerObject = playerObject;

            SpawnedObjects.Add(networkObject.NetworkObjectId, networkObject);
            SpawnedObjectsList.Add(networkObject);

            if (NetworkManager.IsServer)
            {
                if (playerObject)
                {
                    // If there was an already existing player object for this player, then mark it as no longer
                    // a player object.
                    if (NetworkManager.ConnectedClients[ownerClientId].PlayerObject != null)
                    {
                        NetworkManager.ConnectedClients[ownerClientId].PlayerObject.IsPlayerObject = false;
                    }
                    NetworkManager.ConnectedClients[ownerClientId].PlayerObject = networkObject;
                }
            }
            else if (ownerClientId == NetworkManager.LocalClientId)
            {
                if (playerObject)
                {
                    // If there was an already existing player object for this player, then mark it as no longer a player object.
                    if (NetworkManager.LocalClient.PlayerObject != null)
                    {
                        NetworkManager.LocalClient.PlayerObject.IsPlayerObject = false;
                    }
                    NetworkManager.LocalClient.PlayerObject = networkObject;
                }
            }

            // If we are the server and should spawn with observers
            if (NetworkManager.IsServer && networkObject.SpawnWithObservers)
            {
                // Add client observers
                for (int i = 0; i < NetworkManager.ConnectedClientsList.Count; i++)
                {
                    if (networkObject.CheckObjectVisibility == null || networkObject.CheckObjectVisibility(NetworkManager.ConnectedClientsList[i].ClientId))
                    {
                        networkObject.Observers.Add(NetworkManager.ConnectedClientsList[i].ClientId);
                    }
                }
            }

            networkObject.ApplyNetworkParenting();
            NetworkObject.CheckOrphanChildren();

            networkObject.InvokeBehaviourNetworkSpawn();

            NetworkManager.DeferredMessageManager.ProcessTriggers(IDeferredNetworkMessageManager.TriggerType.OnSpawn, networkId);

            // propagate the IsSceneObject setting to child NetworkObjects
            var children = networkObject.GetComponentsInChildren<NetworkObject>();
            foreach (var childObject in children)
            {
                // Do not propagate the in-scene object setting if a child was dynamically spawned.
                if (childObject.IsSceneObject.HasValue && !childObject.IsSceneObject.Value)
                {
                    continue;
                }
                childObject.IsSceneObject = sceneObject;
            }

            // Only dynamically spawned NetworkObjects are allowed
            if (!sceneObject)
            {
                networkObject.SubscribeToActiveSceneForSynch();
            }
        }

        internal void SendSpawnCallForObject(ulong clientId, NetworkObject networkObject)
        {
            // If we are a host and sending to the host's client id, then we can skip sending ourselves the spawn message.
            if (clientId == NetworkManager.ServerClientId)
            {
                return;
            }

            var message = new CreateObjectMessage
            {
                ObjectInfo = networkObject.GetMessageSceneObject(clientId)
            };
            var size = NetworkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.ReliableFragmentedSequenced, clientId);
            NetworkManager.NetworkMetrics.TrackObjectSpawnSent(clientId, networkObject, size);
        }

        internal ulong? GetSpawnParentId(NetworkObject networkObject)
        {
            NetworkObject parentNetworkObject = null;

            if (!networkObject.AlwaysReplicateAsRoot && networkObject.transform.parent != null)
            {
                parentNetworkObject = networkObject.transform.parent.GetComponent<NetworkObject>();
            }

            if (parentNetworkObject == null)
            {
                return null;
            }

            return parentNetworkObject.NetworkObjectId;
        }

        internal void DespawnObject(NetworkObject networkObject, bool destroyObject = false)
        {
            if (!networkObject.IsSpawned)
            {
                throw new SpawnStateException("Object is not spawned");
            }

            if (!NetworkManager.IsServer)
            {
                throw new NotServerException("Only server can despawn objects");
            }

            OnDespawnObject(networkObject, destroyObject);
        }

        // Makes scene objects ready to be reused
        internal void ServerResetShudownStateForSceneObjects()
        {
#if UNITY_2023_1_OR_NEWER
            var networkObjects = UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID).Where((c) => c.IsSceneObject != null && c.IsSceneObject == true);
#else
            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>().Where((c) => c.IsSceneObject != null && c.IsSceneObject == true);
#endif
            foreach (var sobj in networkObjects)
            {
                sobj.IsSpawned = false;
                sobj.DestroyWithScene = false;
                sobj.IsSceneObject = null;
            }
        }

        /// <summary>
        /// Gets called only by NetworkSceneManager.SwitchScene
        /// </summary>
        internal void ServerDestroySpawnedSceneObjects()
        {
            // This Allocation is "OK" for now because this code only executes when a new scene is switched to
            // We need to create a new copy the HashSet of NetworkObjects (SpawnedObjectsList) so we can remove
            // objects from the HashSet (SpawnedObjectsList) without causing a list has been modified exception to occur.
            var spawnedObjects = SpawnedObjectsList.ToList();

            foreach (var sobj in spawnedObjects)
            {
                if (sobj.IsSceneObject != null && sobj.IsSceneObject.Value && sobj.DestroyWithScene && sobj.gameObject.scene != NetworkManager.SceneManager.DontDestroyOnLoadScene)
                {
                    SpawnedObjectsList.Remove(sobj);
                    UnityEngine.Object.Destroy(sobj.gameObject);
                }
            }
        }

        internal void DespawnAndDestroyNetworkObjects()
        {
#if UNITY_2023_1_OR_NEWER
            var networkObjects = UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID);
#else
            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
#endif

            for (int i = 0; i < networkObjects.Length; i++)
            {
                if (networkObjects[i].NetworkManager == NetworkManager)
                {
                    if (NetworkManager.PrefabHandler.ContainsHandler(networkObjects[i]))
                    {
                        OnDespawnObject(networkObjects[i], false);
                        // Leave destruction up to the handler
                        NetworkManager.PrefabHandler.HandleNetworkPrefabDestroy(networkObjects[i]);
                    }
                    else if (networkObjects[i].IsSpawned)
                    {
                        // If it is an in-scene placed NetworkObject then just despawn
                        // and let it be destroyed when the scene is unloaded. Otherwise, despawn and destroy it.
                        var shouldDestroy = !(networkObjects[i].IsSceneObject != null && networkObjects[i].IsSceneObject.Value);

                        OnDespawnObject(networkObjects[i], shouldDestroy);
                    }
                    else if (networkObjects[i].IsSceneObject != null && !networkObjects[i].IsSceneObject.Value)
                    {
                        UnityEngine.Object.Destroy(networkObjects[i].gameObject);
                    }
                }
            }
        }

        internal void DestroySceneObjects()
        {
#if UNITY_2023_1_OR_NEWER
            var networkObjects = UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID);
#else
            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
#endif

            for (int i = 0; i < networkObjects.Length; i++)
            {
                if (networkObjects[i].NetworkManager == NetworkManager)
                {
                    if (networkObjects[i].IsSceneObject == null || networkObjects[i].IsSceneObject.Value == true)
                    {
                        if (NetworkManager.PrefabHandler.ContainsHandler(networkObjects[i]))
                        {
                            NetworkManager.PrefabHandler.HandleNetworkPrefabDestroy(networkObjects[i]);
                            if (SpawnedObjects.ContainsKey(networkObjects[i].NetworkObjectId))
                            {
                                OnDespawnObject(networkObjects[i], false);
                            }
                        }
                        else
                        {
                            UnityEngine.Object.Destroy(networkObjects[i].gameObject);
                        }
                    }
                }
            }
        }

        internal void ServerSpawnSceneObjectsOnStartSweep()
        {
#if UNITY_2023_1_OR_NEWER
            var networkObjects = UnityEngine.Object.FindObjectsByType<NetworkObject>(FindObjectsSortMode.InstanceID);
#else
            var networkObjects = UnityEngine.Object.FindObjectsOfType<NetworkObject>();
#endif
            var networkObjectsToSpawn = new List<NetworkObject>();

            for (int i = 0; i < networkObjects.Length; i++)
            {
                if (networkObjects[i].NetworkManager == NetworkManager)
                {
                    if (networkObjects[i].IsSceneObject == null)
                    {
                        networkObjectsToSpawn.Add(networkObjects[i]);
                    }
                }
            }


            foreach (var networkObject in networkObjectsToSpawn)
            {
                SpawnNetworkObjectLocally(networkObject, GetNetworkObjectId(), true, false, networkObject.OwnerClientId, true);
            }
        }

        internal void OnDespawnObject(NetworkObject networkObject, bool destroyGameObject)
        {
            if (NetworkManager == null)
            {
                return;
            }

            // We have to do this check first as subsequent checks assume we can access NetworkObjectId.
            if (networkObject == null)
            {
                Debug.LogWarning($"Trying to destroy network object but it is null");
                return;
            }

            // Removal of spawned object
            if (!SpawnedObjects.ContainsKey(networkObject.NetworkObjectId))
            {
                Debug.LogWarning($"Trying to destroy object {networkObject.NetworkObjectId} but it doesn't seem to exist anymore!");
                return;
            }

            // If we are shutting down the NetworkManager, then ignore resetting the parent
            // and only attempt to remove the child's parent on the server-side
            if (!NetworkManager.ShutdownInProgress && NetworkManager.IsServer)
            {
                // Move child NetworkObjects to the root when parent NetworkObject is destroyed
                foreach (var spawnedNetObj in SpawnedObjectsList)
                {
                    var latestParent = spawnedNetObj.GetNetworkParenting();
                    if (latestParent.HasValue && latestParent.Value == networkObject.NetworkObjectId)
                    {
                        // Try to remove the parent using the cached WorldPositioNStays value
                        // Note: WorldPositionStays will still default to true if this was an
                        // in-scene placed NetworkObject and parenting was predefined in the
                        // scene via the editor.
                        if (!spawnedNetObj.TryRemoveParentCachedWorldPositionStays())
                        {
                            if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                            {
                                NetworkLog.LogError($"{nameof(NetworkObject)} #{spawnedNetObj.NetworkObjectId} could not be moved to the root when its parent {nameof(NetworkObject)} #{networkObject.NetworkObjectId} was being destroyed");
                            }
                        }

                        if (NetworkLog.CurrentLogLevel <= LogLevel.Normal)
                        {
                            NetworkLog.LogWarning($"{nameof(NetworkObject)} #{spawnedNetObj.NetworkObjectId} moved to the root because its parent {nameof(NetworkObject)} #{networkObject.NetworkObjectId} is destroyed");
                        }
                    }
                }
            }

            networkObject.InvokeBehaviourNetworkDespawn();

            if (NetworkManager != null && NetworkManager.IsServer)
            {
                if (NetworkManager.NetworkConfig.RecycleNetworkIds)
                {
                    ReleasedNetworkObjectIds.Enqueue(new ReleasedNetworkId()
                    {
                        NetworkId = networkObject.NetworkObjectId,
                        ReleaseTime = NetworkManager.RealTimeProvider.UnscaledTime
                    });
                }

                if (networkObject != null)
                {
                    // As long as we have any remaining clients, then notify of the object being destroy.
                    if (NetworkManager.ConnectedClientsList.Count > 0)
                    {
                        m_TargetClientIds.Clear();

                        // We keep only the client for which the object is visible
                        // as the other clients have them already despawned
                        foreach (var clientId in NetworkManager.ConnectedClientsIds)
                        {
                            if (networkObject.IsNetworkVisibleTo(clientId))
                            {
                                m_TargetClientIds.Add(clientId);
                            }
                        }

                        var message = new DestroyObjectMessage
                        {
                            NetworkObjectId = networkObject.NetworkObjectId,
                            DestroyGameObject = networkObject.IsSceneObject != false ? destroyGameObject : true
                        };
                        var size = NetworkManager.ConnectionManager.SendMessage(ref message, NetworkDelivery.ReliableSequenced, m_TargetClientIds);
                        foreach (var targetClientId in m_TargetClientIds)
                        {
                            NetworkManager.NetworkMetrics.TrackObjectDestroySent(targetClientId, networkObject, size);
                        }
                    }
                }
            }

            networkObject.IsSpawned = false;

            if (SpawnedObjects.Remove(networkObject.NetworkObjectId))
            {
                SpawnedObjectsList.Remove(networkObject);
            }

            // Always clear out the observers list when despawned
            networkObject.Observers.Clear();

            var gobj = networkObject.gameObject;
            if (destroyGameObject && gobj != null)
            {
                if (NetworkManager.PrefabHandler.ContainsHandler(networkObject))
                {
                    NetworkManager.PrefabHandler.HandleNetworkPrefabDestroy(networkObject);
                }
                else
                {
                    UnityEngine.Object.Destroy(gobj);
                }
            }
        }

        /// <summary>
        /// Updates all spawned <see cref="NetworkObject.Observers"/> for the specified newly connected client 
        /// Note: if the clientId is the server then it is observable to all spawned <see cref="NetworkObject"/>'s
        /// </summary>
        /// <remarks>
        /// This method is to only to be used for newly connected clients in order to update the observers list for
        /// each NetworkObject instance.
        /// </remarks>
        internal void UpdateObservedNetworkObjects(ulong clientId)
        {
            foreach (var sobj in SpawnedObjectsList)
            {
                // If the NetworkObject has no visibility check then prepare to add this client as an observer
                if (sobj.CheckObjectVisibility == null)
                {
                    // If the client is not part of the observers and spawn with observers is enabled on this instance or the clientId is the server
                    if (!sobj.Observers.Contains(clientId) && (sobj.SpawnWithObservers || clientId == NetworkManager.ServerClientId))
                    {
                        sobj.Observers.Add(clientId);
                    }
                }
                else
                {
                    // CheckObject visibility overrides SpawnWithObservers under this condition
                    if (sobj.CheckObjectVisibility(clientId))
                    {
                        sobj.Observers.Add(clientId);
                    }
                    else // Otherwise, if the observers contains the clientId (shouldn't happen) then remove it since CheckObjectVisibility returned false
                    if (sobj.Observers.Contains(clientId))
                    {
                        sobj.Observers.Remove(clientId);
                    }
                }
            }
        }

        /// <summary>
        /// See <see cref="NetworkBehaviourUpdater.NetworkBehaviourUpdater_Tick"/>
        /// </summary>
        internal void HandleNetworkObjectShow()
        {
            // Handle NetworkObjects to show
            foreach (var client in ObjectsToShowToClient)
            {
                ulong clientId = client.Key;
                foreach (var networkObject in client.Value)
                {
                    SendSpawnCallForObject(clientId, networkObject);
                }
            }
            ObjectsToShowToClient.Clear();
        }

        internal NetworkSpawnManager(NetworkManager networkManager)
        {
            NetworkManager = networkManager;
        }
    }
}
