using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(MeshRenderer), typeof(Collider), typeof(NavMeshObstacle))]
public abstract class ProtectionCube : NetworkBehaviour, IBuildable, IAgrable
{
    protected ObjectType _objectType = ObjectType.Block;
    public abstract int Durability { get; protected set; }
    public abstract int AgrePriority { get; }

    private MeshRenderer _meshRenderer;
    private Collider _collider;
    private NavMeshObstacle _navObstacle;

    private Color _transparentColor = new(1, 1, 1, 0.3f);

    protected virtual void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();
        _navObstacle = GetComponent<NavMeshObstacle>();
    }

    public virtual void ApplyDamage(int dmg)
    {
        if (dmg < Durability)
            Durability -= dmg;
        else
        {
            Durability = 0;
            Break();
        }
    }
    private void Break()
    {
        Destroy(gameObject);
    }

    public void ShowTransparent()
    {
        _meshRenderer.material.color = _transparentColor;
        _collider.enabled = false;
        _navObstacle.enabled = false;
    }

    public void Build()
    {
        _meshRenderer.material.color = Color.white;
        _collider.enabled = true;
        _navObstacle.enabled = true;
    }
}
