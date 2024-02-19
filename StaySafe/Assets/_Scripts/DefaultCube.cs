using UnityEngine;
using UnityEngine.EventSystems;

public class DefaultCube : ProtectionCube
{
    public override GameObject TransparentObj { get { return _transparentObj; } }
    [SerializeField] private GameObject _transparentObj;

    protected override int _Durability { get; set; } = 100;
}
