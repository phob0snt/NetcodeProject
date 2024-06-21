using Unity.Netcode;
using UnityEngine;
using Zenject;

public class TestPlayer : NetworkBehaviour
{
    [Inject] private TestGameObject _testGO;

    // check if test gameobject injected
    public override void OnNetworkSpawn()
    {
        _testGO.ConfirmConnection();
        transform.position = Vector3.up;
    }
}
