using UnityEngine;
using UnityEngine.AI;

public class Core : MonoBehaviour
{
    public Vector3 CalculateNearestEnemyPoint(Vector3 enemyPos)
    {
        Debug.Log("Calc");
        NavMesh.Raycast(enemyPos, transform.position, out NavMeshHit hit, 1);
        Debug.Log(hit.position);
        return hit.position;
    }
}
