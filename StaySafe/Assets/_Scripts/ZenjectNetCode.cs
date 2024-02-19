using Unity.Netcode;
using UnityEngine;
using Zenject;

public class ZenjectNetCode : INetworkPrefabInstanceHandler
{
    private GameObject _prefab;
    private DiContainer _container;
    public ZenjectNetCode(GameObject prefab, DiContainer container)
    {
        _prefab = prefab;
        _container = container;
    }
    public NetworkObject Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        return _container.InstantiateNetworkPrefab(_prefab);
    }

    public void Destroy(NetworkObject networkObject)
    {
        Object.Destroy(networkObject.gameObject);
    }
}

public static class ContainerExtension
{
    public static NetworkObject InstantiateNetworkPrefab(this DiContainer container, GameObject prefab)
    {
        bool state = prefab.activeSelf;
        prefab.SetActive(false);
        var gameObject = container.InstantiatePrefab(prefab);
        prefab.SetActive(state);
        gameObject.SetActive(true);
        NetworkObject netObj = gameObject.GetComponent<NetworkObject>();
        return netObj;
    }
}
