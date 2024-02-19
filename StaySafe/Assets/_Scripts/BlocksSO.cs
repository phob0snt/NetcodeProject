using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[CreateAssetMenu(menuName="BlocksStorage")]
public class BlocksSO : ScriptableObject
{
    public List<ProtectionCube> cubes;
}
