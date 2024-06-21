using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Player : NetworkBehaviour, IAgrable
{
    public int AgrePriority => 2;

    public static UnityEvent<Player> OnPlayerSpawn = new();
    [SerializeField] private Transform _camFollow;
    public Transform CamFollow { get => _camFollow; } 

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            Debug.Log(OwnerClientId);
            return;
        }
        OnPlayerSpawn.Invoke(this);
    }
}
