using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(Player))]
public class Builder : NetworkBehaviour
{
    enum RotationDirection { Null, Forward, Left, Right, Backward }
    private RotationDirection _rotation;
    private List<ProtectionCube> _blocksRef;
    [SerializeField] private Dictionary<ProtectionCube, int> _inventory = new(8);
    [SerializeField] private Transform _buildingRay;
    private Transform _displayedBlock;
    private Vector3 _buildingVector = Vector3.zero;
    [Inject] private BuilderView _builderView;
    [SerializeField] private NetworkSpawnManager _spawnManager;

    private NetworkVariable<BlockParams> _displayedBlockParams = new NetworkVariable<BlockParams>(new BlockParams(new Color(1, 1, 1, 0.3f), false, false), NetworkVariableReadPermission.Everyone);

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

    private bool isBuilding;
    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        _blocksRef ??= Resources.Load<BlocksSO>("BlocksStorage").cubes;
        _inventory.Add(_blocksRef[0], 1);
        Debug.Log(_inventory.ElementAt(0).Key.transform);
        EnableBuilderMode();
        _builderView.Connect(this);
    }

    private void GetNewDisplayedBlock()
    {
        if (!IsOwner) return;
        _displayedBlock = Instantiate(_inventory.ElementAt(0).Key.transform, new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), Quaternion.identity);
        Debug.Log(_displayedBlock);
        _displayedBlock.GetComponent<ProtectionCube>().ShowTransparent();
        Debug.Log(_displayedBlock.GetComponent<NetworkObject>().IsSpawned);
        
    }

    private void EnableBuilderMode()
    {
        if (!IsOwner) return;
        isBuilding = true;
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (transform.eulerAngles.y > 50 && transform.eulerAngles.y <= 130)
            _rotation = RotationDirection.Right;
        else if (transform.eulerAngles.y > 130 && transform.eulerAngles.y <= 230)
            _rotation = RotationDirection.Backward;
        else if (transform.eulerAngles.y > 230 && transform.eulerAngles.y <= 310)
            _rotation = RotationDirection.Left;
        else
        {
            float rotationAngle = transform.eulerAngles.y < 180 ? transform.eulerAngles.y : -(180 - (transform.eulerAngles.y - 180));
            if (rotationAngle > -50 && rotationAngle <= 50)
                _rotation = RotationDirection.Forward;
        }

        switch (_rotation)
        {
            case RotationDirection.Forward: _buildingVector = Vector3.forward; break;
            case RotationDirection.Right: _buildingVector = Vector3.right; break;
            case RotationDirection.Backward: _buildingVector = -Vector3.forward; break;
            case RotationDirection.Left: _buildingVector = -Vector3.right; break;
        }
        Physics.Raycast(_buildingRay.position, _buildingVector, out RaycastHit forwardHit, 1f);
        Physics.Raycast(transform.position, -transform.up, out RaycastHit underHit, 0.5f);
        Debug.DrawRay(_buildingRay.position, _buildingVector * 1f);
        Debug.DrawRay(transform.position, -transform.up * 0.5f);
        if (underHit.collider != null && underHit.collider.gameObject.GetComponent<ProtectionCube>() != null && isBuilding)
        {
            Debug.Log("FIRST");
            if (forwardHit.collider == null)
            {
                Debug.Log("SECOND");
                if (_displayedBlock == null)
                    GetNewDisplayedBlock();
                _displayedBlock.GetComponent<MeshRenderer>().enabled = true;
                Debug.Log(underHit.collider.transform.position);
                if (underHit.collider.transform.position.x % 1 == 0 && underHit.collider.transform.position.y % 1 == 0 && underHit.collider.transform.position.z % 1 == 0)
                    _displayedBlock.transform.position = underHit.collider.transform.position + _buildingVector;
                else
                    _displayedBlock.transform.position = new Vector3(Mathf.Round(underHit.collider.transform.position.x), underHit.collider.transform.position.y, Mathf.Round(underHit.collider.transform.position.z)) + _buildingVector;
                //_displayedBlock.transform.position = new Vector3(100, 0, 0);
            }
            else
            {
                if (_displayedBlock != null)
                    _displayedBlock.GetComponent<MeshRenderer>().enabled = false;   
            }
        }
    }

    public void Build()
    {
        Debug.Log("BUILD");
        if (!IsOwner) return;
        if (IsHost)
        {
            if (!_displayedBlock.GetComponent<MeshRenderer>().enabled) return;
            //obj.Build();
            _displayedBlock.GetComponent<ProtectionCube>().Build();
            Debug.Log("isspawned" + _displayedBlock.GetComponent<NetworkObject>().IsSpawned);
            Debug.Log("isvisible" + _displayedBlock.GetComponent<NetworkObject>().IsNetworkVisibleTo(1));
            if (_displayedBlock.GetComponent<NetworkObject>().IsSpawned)
                _displayedBlock.GetComponent<NetworkObject>().Despawn(false);
            if (!_displayedBlock.GetComponent<NetworkObject>().IsSpawned)
                _displayedBlock.GetComponent<NetworkObject>().Spawn();
            if (!_displayedBlock.GetComponent<NetworkObject>().IsNetworkVisibleTo(1))
                _displayedBlock.GetComponent<NetworkObject>().NetworkShow(1);
        }
        else
        {
            ObjectSpawnServerRpc(_displayedBlock.position);
            Destroy(_displayedBlock.gameObject);
        }
        GetNewDisplayedBlock();
    }

    //[ServerRpc]
    //private void SpawnObjServerRpc()
    //{
    //    Transform temp = Instantiate(_inventory.ElementAt(0).Key.transform, new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), Quaternion.identity);
    //    temp.GetComponent<NetworkObject>().SpawnWithOwnership(1);
    //    GetObjIDClientRpc(new NetworkObjectReference(temp.GetComponent<NetworkObject>()));
    //    temp.GetComponent<NetworkObject>().NetworkHide(0);
    //}

    [ServerRpc]
    private void ObjectSpawnServerRpc(Vector3 position)
    {
        Transform temp = Instantiate(Resources.Load<BlocksSO>("BlocksStorage").cubes[0].transform, new Vector3(transform.position.x, transform.position.y - 1, transform.position.z), Quaternion.identity);
        temp.position = position;
        temp.GetComponent<NetworkObject>().Spawn();
    }



    //[ClientRpc]
    //private void GetObjIDClientRpc(NetworkObjectReference reference)
    //{
    //    if (IsHost) return;
    //    if (reference.TryGet(out NetworkObject networkObj))
    //    {
    //        _displayedBlock = networkObj.GetComponent<Transform>();
    //        _displayedBlock.GetComponent<Collider>().enabled = _displayedBlockParams.Value._isCollider;
    //        _displayedBlock.GetComponent<MeshRenderer>().enabled = _displayedBlockParams.Value._isMeshRenderer;
    //        _displayedBlock.GetComponent<MeshRenderer>().material.color = _displayedBlockParams.Value._color;
    //        Debug.Log(_displayedBlock.GetComponent<MeshRenderer>());
    //    }
    //    Debug.Log(_displayedBlock.GetComponent<NetworkObject>());
    //    if (!_displayedBlock.GetComponent<MeshRenderer>().enabled) return;
    //    _displayedBlock.GetComponent<Collider>().enabled = true;
    //    _displayedBlock.GetComponent<MeshRenderer>().material.color = Color.white;
    //    _displayedBlock.GetComponent<NetworkObject>().NetworkShow(1);
    //    GetNew_displayedBlock();
    //}

}
