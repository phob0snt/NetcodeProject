using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public abstract class ProtectionCube : NetworkBehaviour, IBuildable
{
    protected ObjectType _ObjectType = ObjectType.Block;
    protected abstract int _Durability { get; set; }

    public abstract GameObject TransparentObj { get; }

    public virtual void ApplyDamage(int dmg)
    {
        if (dmg < _Durability)
            _Durability -= dmg;
        else
        {
            _Durability = 0;
            Break();
        }
    }
    private void Break()
    {
        Destroy(gameObject);
    }

    protected virtual void DisplayPlace()
    {
        throw new System.NotImplementedException();
    }

    public void Build()
    {
        throw new System.NotImplementedException();
    }
}
