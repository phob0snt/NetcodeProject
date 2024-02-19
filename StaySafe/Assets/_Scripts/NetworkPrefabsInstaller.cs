using Unity.Netcode;
using UnityEngine;
using Zenject;

public class NetworkPrefabsInstaller : MonoInstaller
{
    [SerializeField] private NetworkObject[] prefabs;
    [SerializeField] private NetworkManager _NetworkManager;
    public override void InstallBindings()
    {
        foreach (var prefab in prefabs)
        {
            var networkObject = prefab.GetComponent<NetworkObject>();
            _NetworkManager.AddNetworkPrefab(prefab.gameObject);
            _NetworkManager.PrefabHandler.AddHandler(prefab, new ZenjectNetCode(prefab.gameObject, Container));
        }
    }
}
