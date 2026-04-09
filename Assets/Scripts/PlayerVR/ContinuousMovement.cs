using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class ContinuousMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    [SerializeField] private float _moveSpeed = 2.5f;
    [SerializeField] private float _sprintSpeed = 5.0f;
    [SerializeField] private Transform _playerRig;

    private SteamVR_Action_Vector2 _moveAction;
    private SteamVR_Action_Boolean _sprintAction;
    private Hand _hand;

    private void Awake()
    {
        _hand = GetComponent<Hand>();

        // Пытаемся найти действия по разным путям
        _moveAction = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("Move");
        if (_moveAction == null)
            _moveAction = SteamVR_Input.GetAction<SteamVR_Action_Vector2>("default", "Move");

        _sprintAction = SteamVR_Input.GetBooleanAction("Grip");
    }

    private void Start()
    {
        if (_playerRig == null)
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
                _playerRig = player.trackingOriginTransform;
        }
    }

    private void Update()
    {
        if (_hand == null || _hand.handType != SteamVR_Input_Sources.LeftHand) return;
        if (_playerRig == null) return;

        if (_moveAction == null) return;

        Vector2 moveAxis = _moveAction.GetAxis(_hand.handType);

        if (moveAxis == Vector2.zero) return;

        float currentSpeed = _moveSpeed;
        if (_sprintAction != null && _sprintAction.GetState(_hand.handType))
            currentSpeed = _sprintSpeed;

        Transform hmd = Player.instance.hmdTransform;
        if (hmd == null) return;

        Vector3 forward = hmd.forward;
        Vector3 right = hmd.right;

        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * moveAxis.y + right * moveAxis.x) * currentSpeed * Time.deltaTime;

        _playerRig.position += moveDirection;
    }
}