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

    // -- Internal state ----------------------------------------------------------
    private Rigidbody2D _rb;
    private InputAction _moveAction;
    private InputAction _jumpAction;
    private bool _isGrounded;
    private bool _jumpHeld;

    // -- Cached transform for ground check pivot ---------------------------------
    private Transform _transform;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
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
        if (!_isGrounded) return;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        _jumpHeld = true;
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
        ApplyMovement();
    }

    private void CheckGround()
    {
        // Overlap circle slightly below the player's feet pivot.
        Vector2 origin = (Vector2)_transform.position + Vector2.down * 0.05f;
        _isGrounded = Physics2D.OverlapCircle(origin, groundCheckRadius, groundLayer);
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
}
