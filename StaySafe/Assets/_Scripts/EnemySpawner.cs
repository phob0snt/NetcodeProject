using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    [SerializeField] private float _spawnRate;
    [SerializeField] private Enemy _enemy;
    [SerializeField] private float _spawnDistance;

    public override void OnNetworkSpawn()
    {
        if (IsHost)
            StartCoroutine(SpawnEnemy());
    }

    private IEnumerator SpawnEnemy()
    {
        NetworkObject temp = NetworkManager.SpawnManager.InstantiateAndSpawn(_enemy.GetComponent<NetworkObject>());
        temp.gameObject.transform.position = CalculateEnemySpawnpoint();
        yield return new WaitForSeconds(_spawnRate);
        StartCoroutine(SpawnEnemy());
    }
    private Vector3 CalculateEnemySpawnpoint()
    {
        float x = 0;
        float z = 0;
        if (Random.Range(0, 2) == 1)
        {
            x = Random.Range(-_spawnDistance, _spawnDistance);
            if (Random.Range(0, 2) == 1)
                z = _spawnDistance;
            else
                z = -_spawnDistance;
        }
        else
        {
            z = Random.Range(-_spawnDistance, _spawnDistance);
            if (Random.Range(0, 2) == 1)
                x = _spawnDistance;
            else
                x = -_spawnDistance;
        }
        return new Vector3(x, 1, z);
    }
}
