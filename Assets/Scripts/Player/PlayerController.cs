using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Celeste-like player movement: instant direction reversal, jump cut, full air control.
/// MOVE-01 implementation — per D-01, D-02, D-03, D-04.
///
/// Required Rigidbody2D settings (set in Inspector):
///   Collision Detection = Continuous  (prevents tunneling — ROADMAP Stack Constraint)
///   Interpolation = Interpolate       (eliminates camera jitter — Pitfall 6)
///   Gravity Scale = 3.5               (snappy fall arc)
///   Constraints: Freeze Rotation Z    (no rotation)
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    // -- Movement constants ------------------------------------------------------
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private int maxJumps = 2;

    /// <summary>
    /// On jump-button release while ascending: multiply upward velocity by this.
    /// 0.4 = velocity drops to 40% -> clear short-hop vs. hold-jump difference (per D-02).
    /// </summary>
    [SerializeField] private float jumpCutMultiplier = 0.4f;
    [SerializeField] private float acceleration = 60f;
    [SerializeField] private float deceleration = 80f;

    /// <summary>Small overlap circle below feet to detect ground contact.</summary>
    [SerializeField] private float groundCheckRadius = 0.1f;

    /// <summary>LayerMask for ground check -- assign "Platform" layer in Inspector.</summary>
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private float dropThroughDuration = 0.3f;

    // -- Internal state ----------------------------------------------------------
    private Rigidbody2D _rb;
    private Collider2D _playerCollider;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private bool _isGrounded;
    private bool _jumpHeld;
    private bool _inputLocked;
    private bool _onLadder;
    private bool _isDropping;
    private int _jumpsRemaining;
    private readonly Collider2D[] _dropBuffer = new Collider2D[8];

    // -- Phase 3: Player death notification (D-13) ----------------------------
    /// <summary>
    /// Fired by FallDetector (D-17), enemy hitbox, and projectile on player death.
    /// Phase 4 UIManager subscribes to this — Phase 3 code never needs modification (D-15).
    /// Static event: unsubscribe in OnDisable to prevent stale subscriptions on Play Mode restart.
    /// </summary>
    public static event Action OnPlayerDeath;

    public static void TriggerDeath() => OnPlayerDeath?.Invoke();

    // -- Cached transform for ground check pivot ---------------------------------
    private Transform _transform;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerCollider = GetComponent<Collider2D>();
        _transform = transform;

        // Enforce required physics settings programmatically as a safety net.
        // These MUST also be set in the Inspector (the programmatic set is a guard).
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable()
    {
        // Locate actions in the "Player" action map of the existing asset.
        var playerInput = GetComponent<PlayerInput>();
        _moveAction = playerInput.actions["Player/Move"];
        _jumpAction = playerInput.actions["Player/Jump"];

        _jumpAction.performed += OnJumpPerformed;
        _jumpAction.canceled  += OnJumpCanceled;
        _jumpAction.Enable();
    }

    private void OnDisable()
    {
        _jumpAction.performed -= OnJumpPerformed;
        _jumpAction.canceled  -= OnJumpCanceled;
    }

    // -- Input callbacks ---------------------------------------------------------

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (_inputLocked) return;
        if (_onLadder)
        {
            // 사다리에서 점프: LadderController에 이탈 신호 후 위로 도약
            GetComponent<LadderController>()?.ExitLadder(fromJump: true);
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
            // 사다리 진입 전 점프를 소진했어도 공중점프 1회 보장
            _jumpsRemaining = maxJumps - 1;
            _jumpHeld = true;
            return;
        }
        // 아래 + 점프: one-way 플랫폼 통과
        if (_moveAction.ReadValue<Vector2>().y < -0.5f && _isGrounded && !_isDropping)
        {
            StartCoroutine(DropThrough());
            return;
        }

        if (_jumpsRemaining <= 0) return;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        _jumpsRemaining--;
        _jumpHeld = true;
    }

    private IEnumerator DropThrough()
    {
        _isDropping = true;

        Vector2 origin = (Vector2)_transform.position + Vector2.down * 0.05f;
        int count = Physics2D.OverlapCircleNonAlloc(origin, groundCheckRadius + 0.05f, _dropBuffer, groundLayer);

        for (int i = 0; i < count; i++)
        {
            if (_dropBuffer[i].GetComponent<PlatformEffector2D>() != null)
                Physics2D.IgnoreCollision(_playerCollider, _dropBuffer[i], true);
            else
                _dropBuffer[i] = null;
        }

        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -2f);

        yield return new WaitForSecondsRealtime(dropThroughDuration);

        for (int i = 0; i < count; i++)
        {
            if (_dropBuffer[i] != null)
                Physics2D.IgnoreCollision(_playerCollider, _dropBuffer[i], false);
        }

        _isDropping = false;
    }

    private void OnJumpCanceled(InputAction.CallbackContext ctx)
    {
        // Jump cut: if still ascending and button released, reduce upward velocity (per D-02).
        if (_rb.linearVelocity.y > 0f)
        {
            _rb.linearVelocity = new Vector2(
                _rb.linearVelocity.x,
                _rb.linearVelocity.y * jumpCutMultiplier
            );
        }
        _jumpHeld = false;
    }

    // -- Physics update ----------------------------------------------------------

    private void FixedUpdate()
    {
        CheckGround();
        if (!_inputLocked && !_onLadder) ApplyMovement();
    }

    private void CheckGround()
    {
        // Overlap circle slightly below the player's feet pivot.
        Vector2 origin = (Vector2)_transform.position + Vector2.down * 0.05f;
        _isGrounded = Physics2D.OverlapCircle(origin, groundCheckRadius, groundLayer);
        if (_isGrounded)
            _jumpsRemaining = maxJumps;
    }

    private void ApplyMovement()
    {
        float horizontal = _moveAction.ReadValue<Vector2>().x;

        // Time.timeScale compensation — Phase 2 slow-mo requires no rewrite (per D-04).
        float compensatedMax = moveSpeed * (1f / Time.timeScale);
        float target = horizontal * compensatedMax;
        float rate = (Mathf.Abs(horizontal) > 0.01f) ? acceleration : deceleration;
        float newX = Mathf.MoveTowards(_rb.linearVelocity.x, target, rate * Time.fixedDeltaTime);
        _rb.linearVelocity = new Vector2(newX, _rb.linearVelocity.y);
    }

    // -- Public accessors for FallDetector (Plan 03) -----------------------------

    /// <summary>True when the player is touching a Platform layer collider.</summary>
    public bool IsGrounded => _isGrounded;

    /// <summary>최고 이동 속도. PlayerAnimatorController가 스프린트 비율 계산에 사용.</summary>
    public float MoveSpeed => moveSpeed;

    /// <summary>FloorSpawner가 층 전환 시퀀스 중 읽는다. CombatController가 Update 진입 차단에 사용.</summary>
    public bool InputLocked => _inputLocked;

    /// <summary>층 전환 시퀀스 1단계 — 이동/점프 입력 잠금, 속도 즉시 0.</summary>
    public void LockInput()
    {
        _inputLocked = true;
        _rb.linearVelocity = Vector2.zero;
    }

    /// <summary>층 전환 시퀀스 6단계 — 입력 잠금 해제.</summary>
    public void UnlockInput() => _inputLocked = false;

    /// <summary>
    /// LadderController가 사다리 등반 진입/이탈 시 호출.
    /// true: 수평 이동 차단. false: 일반 이동 복구.
    /// </summary>
    public void SetLadderMode(bool active) => _onLadder = active;
}
