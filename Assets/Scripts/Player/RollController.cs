using System.Collections;
using UnityEngine;

/// <summary>
/// MOVE-03: Roll mechanic.
/// Activates on Shift (Sprint action = Roll in game), moves the player laterally,
/// grants i-frames via InvincibilityHandler, and enforces a real-time cooldown
/// that is NOT affected by slow-motion timeScale.
///
/// D-11: Reuses InvincibilityHandler layer swap — no new invincibility system.
/// D-12: Cooldown uses Time.unscaledDeltaTime — 0.8s real-time even during 0.2x slow-mo.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InvincibilityHandler))]
public class RollController : MonoBehaviour
{
    [SerializeField] private float rollSpeed       = 12f;  // units/s during roll (compensated for timeScale)
    [SerializeField] private float rollDuration    = 0.3f; // real seconds of lateral movement
    [SerializeField] private float rollCooldown    = 1.0f; // real seconds before next roll allowed
    [SerializeField] private float iFrameDuration  = 0.4f; // real seconds of invincibility (> rollDuration)

    private InvincibilityHandler _invincibility;
    private Rigidbody2D          _rb;
    private SpriteRenderer       _spriteRenderer;

    private float _cooldownRemaining;
    private bool  _isRolling;

    private void Awake()
    {
        _invincibility   = GetComponent<InvincibilityHandler>();
        _rb              = GetComponent<Rigidbody2D>();
        _spriteRenderer  = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Cooldown drains in REAL time — unaffected by slow-motion (D-12, Pitfall 3)
        if (_cooldownRemaining > 0f)
            _cooldownRemaining -= Time.unscaledDeltaTime;

        // Roll input: one-frame flag from InputManager (Shift key = Sprint action)
        if (InputManager.Instance.RollPressed && !_isRolling && _cooldownRemaining <= 0f)
            StartCoroutine(RollCoroutine());
    }

    private IEnumerator RollCoroutine()
    {
        _isRolling = true;
        _cooldownRemaining = rollCooldown;

        // Roll direction follows the player's current facing (sprite flipX)
        float dir = (_spriteRenderer != null && _spriteRenderer.flipX) ? -1f : 1f;

        // Trigger Roll animation BEFORE applying velocity — animator needs one frame to respond
        var animator = GetComponent<Animator>();
        if (animator != null) animator.SetTrigger("Roll");

        // Grant i-frames immediately at roll start — i-frame window is longer than roll movement
        _invincibility.StartInvincibility(iFrameDuration);

        // Apply lateral velocity over the roll duration using real-time ticks
        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            // Compensate velocity for current timeScale so roll feels the same speed during slow-mo
            // Same pattern as PlayerController.ApplyMovement: speed * (1f / Time.timeScale)
            float compensated = rollSpeed * (1f / Time.timeScale);
            _rb.linearVelocity = new Vector2(dir * compensated, _rb.linearVelocity.y);

            elapsed += Time.unscaledDeltaTime; // accumulate real time (not scaled)
            yield return null;
        }

        _isRolling = false;
    }
}
