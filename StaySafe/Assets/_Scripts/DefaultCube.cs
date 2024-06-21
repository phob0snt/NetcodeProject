using UnityEngine;

public class DefaultCube : ProtectionCube
{
    public override int Durability { get; protected set; } = 100;

    public override int AgrePriority => 1;
}
