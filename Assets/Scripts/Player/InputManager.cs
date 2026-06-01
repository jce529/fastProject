using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private InputAction _moveAction;
    private InputAction _sprintAction;   // Sprint in asset = Roll in game
    private InputAction _attackAction;

    private bool _rollPressedThisFrame;
    private bool _attackHeldThisFrame;
    private bool _attackReleasedThisFrame;

    private void OnEnable()
    {
        var pi = GetComponent<PlayerInput>();
        _moveAction   = pi.actions["Player/Move"];
        _sprintAction = pi.actions["Player/Sprint"];
        _attackAction = pi.actions["Player/Attack"];

        _sprintAction.performed += OnSprintPerformed;
        _attackAction.performed += OnAttackPerformed;
        _attackAction.canceled  += OnAttackCanceled;

        _moveAction.Enable();
        _sprintAction.Enable();
        _attackAction.Enable();
    }

    private void OnDisable()
    {
        _sprintAction.performed -= OnSprintPerformed;
        _attackAction.performed -= OnAttackPerformed;
        _attackAction.canceled  -= OnAttackCanceled;
    }

    // Clear one-frame flags after consumers have had their Update/FixedUpdate.
    private void LateUpdate()
    {
        _rollPressedThisFrame    = false;
        _attackHeldThisFrame     = false;
        _attackReleasedThisFrame = false;
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)  => _rollPressedThisFrame   = true;
    private void OnAttackPerformed(InputAction.CallbackContext ctx)  => _attackHeldThisFrame    = true;
    private void OnAttackCanceled (InputAction.CallbackContext ctx)  => _attackReleasedThisFrame = true;

    /// <summary>WASD 이동 벡터. x만 사용 (수평 이동).</summary>
    public Vector2 MoveInput => _moveAction.ReadValue<Vector2>();

    /// <summary>Shift가 눌린 프레임 단 1회 true (구르기 트리거).</summary>
    public bool RollPressed => _rollPressedThisFrame;

    /// <summary>마우스 좌클릭이 눌린 프레임 단 1회 true (공격/슬로우모션 시작).</summary>
    public bool AttackHeld => _attackHeldThisFrame;

    /// <summary>마우스 좌클릭이 떼어진 프레임 단 1회 true (돌진 트리거).</summary>
    public bool AttackReleased => _attackReleasedThisFrame;

    /// <summary>좌클릭 지속 누름 여부. 슬로우모션 스케일링에 사용.</summary>
    public bool IsAttackDown => _attackAction.IsPressed();
}
