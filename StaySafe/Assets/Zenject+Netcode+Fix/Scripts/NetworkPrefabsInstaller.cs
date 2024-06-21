using Unity.Netcode;
using UnityEngine;
using Zenject;

public class NetworkPrefabsInstaller : MonoInstaller
{
    [SerializeField] private NetworkObject[] prefabs;
    [SerializeField] private NetworkManager networkManager;
    public override void InstallBindings()
    {
        foreach (var prefab in prefabs)
        {
            var networkObject = prefab.GetComponent<NetworkObject>();
            networkManager.AddNetworkPrefab(prefab.gameObject);
            networkManager.PrefabHandler.AddHandler(prefab, new ZenjectNetCode(prefab.gameObject, Container));
        }
    }
}
