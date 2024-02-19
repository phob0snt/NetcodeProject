using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Rendering;
using UnityEngine;
using Zenject;

public class Builder : NetworkBehaviour
{
    enum RotationDirection { Null, Forward, Left, Right, Backward }
    private RotationDirection _Rotation;
    private List<ProtectionCube> blocksRef;
    [SerializeField] private Dictionary<ProtectionCube, int> _Inventory = new(8);
    [SerializeField] private Transform buildingRay;
    private Transform displayedBlock;
    private Vector3 buildingVector = Vector3.zero;
    [Inject] private BuilderView builderView;
    [SerializeField] NetworkSpawnManager spawnManager;

    private NetworkVariable<BlockParams> displayedBlockParams = new NetworkVariable<BlockParams>(new BlockParams(new Color(1, 1, 1, 0.3f), false, false), NetworkVariableReadPermission.Everyone);

    struct BlockParams : INetworkSerializable
    {
        public BlockParams(Color color, bool isCollider, bool isMeshRenderer)
        {
            _color = color;
            _isCollider = isCollider;
            _isMeshRenderer = isMeshRenderer;
        }
        public Color _color;
        public bool _isCollider;
        public bool _isMeshRenderer;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _color);
            serializer.SerializeValue(ref _isCollider);
            serializer.SerializeValue(ref _isMeshRenderer);
        }
    }

    struct NetworkVector3
    {

    }

    private bool isBuilding;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        blocksRef ??= Resources.Load<BlocksSO>("BlocksStorage").cubes;
        _Inventory.Add(blocksRef[0], 1);
        Debug.Log(_Inventory.ElementAt(0).Key.transform);
        EnableBuilderMode();
        builderView.Connect(this);
    }

    private void GetNewDisplayedBlock()
    {
        if (!IsOwner) return;
        displayedBlock = Instantiate(_Inventory.ElementAt(0).Key.transform, new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), Quaternion.identity);
        displayedBlock.GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1, 0.3f);
        displayedBlock.GetComponent<Collider>().enabled = false;
        Debug.Log(displayedBlock.GetComponent<NetworkObject>().IsSpawned);
        
    }

    private void EnableBuilderMode()
    {
        if (!IsOwner) return;
        isBuilding = true;
    }

    void Update()
    {
        if (!IsOwner) return;
        if (transform.eulerAngles.y > 50 && transform.eulerAngles.y <= 130)
            _Rotation = RotationDirection.Right;
        else if (transform.eulerAngles.y > 130 && transform.eulerAngles.y <= 230)
            _Rotation = RotationDirection.Backward;
        else if (transform.eulerAngles.y > 230 && transform.eulerAngles.y <= 310)
            _Rotation = RotationDirection.Left;
        else
        {
            float rotationAngle = transform.eulerAngles.y < 180 ? transform.eulerAngles.y : -(180 - (transform.eulerAngles.y - 180));
            if (rotationAngle > -50 && rotationAngle <= 50)
                _Rotation = RotationDirection.Forward;
        }

        RaycastHit forwardHit;
        RaycastHit underHit;
        switch (_Rotation)
        {
            case RotationDirection.Forward: buildingVector = Vector3.forward; break;
            case RotationDirection.Right: buildingVector = Vector3.right; break;
            case RotationDirection.Backward: buildingVector = -Vector3.forward; break;
            case RotationDirection.Left: buildingVector = -Vector3.right; break;
        }
        Physics.Raycast(buildingRay.position, buildingVector, out forwardHit, 1f);
        Physics.Raycast(transform.position, -transform.up, out underHit, 0.5f);
        Debug.DrawRay(buildingRay.position, buildingVector * 1f);
        if (underHit.collider != null && underHit.collider.gameObject.GetComponent<ProtectionCube>() != null && isBuilding)
        {
            if (forwardHit.collider == null)
            {
                if (displayedBlock == null)
                    GetNewDisplayedBlock();
                displayedBlock.GetComponent<MeshRenderer>().enabled = true;
                displayedBlock.transform.position = underHit.collider.transform.position + buildingVector;
                //displayedBlock.transform.position = new Vector3(100, 0, 0);
            }
            else
            {
                if (displayedBlock != null)
                    displayedBlock.GetComponent<MeshRenderer>().enabled = false;   
            }
        }
    }

    public void Build()
    {
        Debug.Log("BUILD");
        if (!IsOwner) return;
        if (IsHost)
        {
            if (!displayedBlock.GetComponent<MeshRenderer>().enabled) return;
            //obj.Build();
            displayedBlock.GetComponent<Collider>().enabled = true;
            displayedBlock.GetComponent<MeshRenderer>().material.color = Color.white;
            Debug.Log("isspawned" + displayedBlock.GetComponent<NetworkObject>().IsSpawned);
            Debug.Log("isvisible" + displayedBlock.GetComponent<NetworkObject>().IsNetworkVisibleTo(1));
            if (displayedBlock.GetComponent<NetworkObject>().IsSpawned)
                displayedBlock.GetComponent<NetworkObject>().Despawn(false);
            if (!displayedBlock.GetComponent<NetworkObject>().IsSpawned)
                displayedBlock.GetComponent<NetworkObject>().Spawn();
            if (!displayedBlock.GetComponent<NetworkObject>().IsNetworkVisibleTo(1))
                displayedBlock.GetComponent<NetworkObject>().NetworkShow(1);
        }
        else
        {
            ObjectSpawnServerRpc(displayedBlock.position);
            Destroy(displayedBlock.gameObject);
        }
        GetNewDisplayedBlock();
    }

    //[ServerRpc]
    //private void SpawnObjServerRpc()
    //{
    //    Transform temp = Instantiate(_Inventory.ElementAt(0).Key.transform, new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), Quaternion.identity);
    //    temp.GetComponent<NetworkObject>().SpawnWithOwnership(1);
    //    GetObjIDClientRpc(new NetworkObjectReference(temp.GetComponent<NetworkObject>()));
    //    temp.GetComponent<NetworkObject>().NetworkHide(0);
    //}

    [ServerRpc]
    private void ObjectSpawnServerRpc(Vector3 position)
    {
        Transform temp = Instantiate(Resources.Load<BlocksSO>("BlocksStorage").cubes[0].transform, new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), Quaternion.identity);
        temp.GetComponent<NetworkObject>().Spawn();
        temp.position = position;
    }



    //[ClientRpc]
    //private void GetObjIDClientRpc(NetworkObjectReference reference)
    //{
    //    if (IsHost) return;
    //    if (reference.TryGet(out NetworkObject networkObj))
    //    {
    //        displayedBlock = networkObj.GetComponent<Transform>();
    //        displayedBlock.GetComponent<Collider>().enabled = displayedBlockParams.Value._isCollider;
    //        displayedBlock.GetComponent<MeshRenderer>().enabled = displayedBlockParams.Value._isMeshRenderer;
    //        displayedBlock.GetComponent<MeshRenderer>().material.color = displayedBlockParams.Value._color;
    //        Debug.Log(displayedBlock.GetComponent<MeshRenderer>());
    //    }
    //    Debug.Log(displayedBlock.GetComponent<NetworkObject>());
    //    if (!displayedBlock.GetComponent<MeshRenderer>().enabled) return;
    //    displayedBlock.GetComponent<Collider>().enabled = true;
    //    displayedBlock.GetComponent<MeshRenderer>().material.color = Color.white;
    //    displayedBlock.GetComponent<NetworkObject>().NetworkShow(1);
    //    GetNewDisplayedBlock();
    //}

}
