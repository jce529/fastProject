using UnityEngine;

/// <summary>
/// Phase 1 only: drives Animator from PlayerController state.
/// States: Idle, Run, JumpRise, JumpFall.
/// Transition duration must be 0 on all states (per ROADMAP Stack Constraint).
/// </summary>
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAnimatorController : MonoBehaviour
{
    private static readonly int IsMoving    = Animator.StringToHash("IsMoving");
    private static readonly int IsGrounded  = Animator.StringToHash("IsGrounded");
    private static readonly int VelocityY   = Animator.StringToHash("VelocityY");
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int IsRolling   = Animator.StringToHash("IsRolling");
    private static readonly int IsSprinting = Animator.StringToHash("IsSprinting");

    private Animator         _animator;
    private PlayerController _controller;
    private Rigidbody2D      _rb;
    private SpriteRenderer   _spriteRenderer;

    private void Awake()
    {
        _animator       = GetComponent<Animator>();
        _controller     = GetComponent<PlayerController>();
        _rb             = GetComponent<Rigidbody2D>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        bool  moving   = Mathf.Abs(_rb.linearVelocity.x) > 0.05f;
        bool  grounded = _controller.IsGrounded;
        float velY     = _rb.linearVelocity.y;

        bool isSprinting = moving && Mathf.Abs(_rb.linearVelocity.x) / _controller.MoveSpeed > 0.6f;

        _animator.SetBool(IsMoving,    moving);
        _animator.SetBool(IsGrounded,  grounded);
        _animator.SetFloat(VelocityY,  velY);
        _animator.SetBool(IsSprinting, isSprinting);

        // InputManager가 씬에 있을 때만 공격/구르기 파라미터 구동
        if (InputManager.Instance != null)
        {
            _animator.SetBool(IsAttacking, InputManager.Instance.AttackHeld);
            _animator.SetBool(IsRolling,   InputManager.Instance.RollPressed);
        }

        // Flip sprite based on movement direction.
        if (_rb.linearVelocity.x > 0.05f)
            _spriteRenderer.flipX = false;
        else if (_rb.linearVelocity.x < -0.05f)
            _spriteRenderer.flipX = true;
    }
}
