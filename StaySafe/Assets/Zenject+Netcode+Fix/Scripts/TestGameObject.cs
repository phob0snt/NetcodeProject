using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TestGameObject : MonoBehaviour
{
    private MeshRenderer _mesh;

    private void Awake()
    {
        _mesh = GetComponent<MeshRenderer>();
    }

    public void ConfirmConnection()
    {
        _mesh.material.color = Color.green;
        Debug.Log("Player has reference of test gameobject!");
    }
}
