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
        Vector2 origin = (Vector2)_transform.position + Vector2.down * 0.51f;
        _isGrounded = Physics2D.OverlapCircle(origin, groundCheckRadius, groundLayer);
    }

    private void ApplyMovement()
    {
        float horizontal = _moveAction.ReadValue<Vector2>().x;

        // Instant direction reversal -- set velocity directly, no acceleration accumulation (per D-04).
        // Air control is full and identical to ground control (per D-03).
        // Time.timeScale compensation for slow-motion: multiply by (1f / Time.timeScale).
        // Phase 1 has no slow-motion (timeScale = 1), but the pattern is established here
        // so Phase 2 slow-mo does not require a rewrite.
        float compensatedSpeed = moveSpeed * (1f / Time.timeScale);
        _rb.linearVelocity = new Vector2(horizontal * compensatedSpeed, _rb.linearVelocity.y);
    }

    // -- Public accessors for FallDetector (Plan 03) -----------------------------

    /// <summary>True when the player is touching a Platform layer collider.</summary>
    public bool IsGrounded => _isGrounded;
}
