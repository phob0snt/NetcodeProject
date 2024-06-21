using Unity.Netcode;
using UnityEngine;
using Zenject;

[RequireComponent(typeof(CharacterController), typeof(Player))]
public class PlayerNetworkController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [Inject] private FixedJoystick joystick;
    private Vector3 velocity;
    private CharacterController controller;
    private Animator animator;
    private string currAnimState;

    #region Anim_Consts
    private const string PLAYER_IDLE = "Player_Idle";
    private const string PLAYER_WALKING = "Player_Walking";
    private const string PLAYER_RUNNING = "Player_Running";
    #endregion

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        //if (OwnerClientId == 1)
        //    GetComponent<Builder>().enabled = false;
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;
        float rotationAngle = transform.rotation.eulerAngles.y;
        if (joystick.Vertical != 0 || joystick.Horizontal != 0)
        {
            velocity = new Vector3(joystick.Horizontal * moveSpeed * Time.deltaTime, 0, joystick.Vertical * moveSpeed * Time.deltaTime);
            rotationAngle = Mathf.Atan2(joystick.Horizontal, joystick.Vertical) * Mathf.Rad2Deg;
            if (Mathf.Abs(joystick.Vertical) > 0.6f || Mathf.Abs(joystick.Horizontal) > 0.6f)
                ChangeAnimState(PLAYER_RUNNING);
            else if (Mathf.Abs(joystick.Vertical) > 0f || Mathf.Abs(joystick.Horizontal) > 0f)
                ChangeAnimState(PLAYER_WALKING);
        }
        else
        {
            velocity = Vector3.zero;
            ChangeAnimState(PLAYER_IDLE);
        }
        transform.rotation = Quaternion.Euler(0, rotationAngle, 0);
        controller.Move(velocity);
        transform.position = new Vector3(transform.position.x, 1.05f, transform.position.z);
    }

    private void ChangeAnimState(string state)
    {
        if (currAnimState == state) return;
        animator.Play(state);
        animator.CrossFadeInFixedTime(state, 0.2f);
        currAnimState = state;
    }
}
