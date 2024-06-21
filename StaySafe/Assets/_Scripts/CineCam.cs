using Cinemachine;
using UnityEngine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CineCam : MonoBehaviour
{
    private void OnEnable()
    {
        Player.OnPlayerSpawn.AddListener(ConnectPlayer);
    }

    private void ConnectPlayer(Player player)
    {
        Debug.Log(player.CamFollow);
        GetComponent<CinemachineVirtualCamera>().m_LookAt = player.CamFollow;
        GetComponent<CinemachineVirtualCamera>().m_Follow = player.CamFollow;
        Debug.Log(GetComponent<CinemachineVirtualCamera>().m_LookAt);
    }
}
