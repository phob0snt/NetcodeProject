using DG.Tweening;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

[RequireComponent(typeof(NavMeshAgent))]
public class Enemy : NetworkBehaviour
{
    private NavMeshAgent _navAgent;
    [Inject] private Core _core;
    [SerializeField] private float _targetDetectRadius;
    private bool _isStopped = true;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
    }
    private void Start()
    {
        Vector3 spawnDisplacement = new (transform.position.x > 0 ? 0.1f : -0.1f, 0, transform.position.z > 0 ? 0.1f : -0.1f);
        _navAgent.SetDestination(_core.transform.position + spawnDisplacement);
        Debug.Log(_core.transform.position + spawnDisplacement);
    }

    private void FixedUpdate()
    {
        if (_isStopped && _navAgent.velocity.magnitude > 0.1f)
        {
            _isStopped = false;
        }
        if (_navAgent.velocity.magnitude < 0.1f && !_isStopped)
        {
            Debug.Log(_navAgent.velocity.magnitude);
            Debug.Log("SDASDASDA");
            _isStopped = true;
            LookAtTarget(_core.transform.position);
            TryToBreakObstacle();
        }
    }

    private void LookAtTarget(Vector3 target)
    {
        Debug.Log("PENMIS");
        transform.DORotateQuaternion(Quaternion.LookRotation(target - transform.position), 0.8f);
    }

    private void TryToBreakObstacle()
    {
        Collider[] obstacles = Physics.OverlapSphere(transform.position, 0.5f, LayerMask.GetMask("Breakable"));
        Collider nearestObst;
        float nearestObstDist = float.MaxValue;
        foreach (Collider obst in obstacles)
        {
            float distance = Vector3.Distance(transform.position, obst.transform.position);
            if (distance < nearestObstDist)
            {
                nearestObst = obst;
                nearestObstDist = distance;
            }
        }
        // ломание препятствия =)
    }
}
