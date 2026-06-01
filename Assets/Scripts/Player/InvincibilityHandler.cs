using System.Collections;
using UnityEngine;

/// <summary>
/// Manages invincibility state: sprite flicker + layer swap.
/// Used by FallDetector (post-fall) and in Phase 3 by dash system (post-dash).
///
/// Layer swap pattern (per ROADMAP Stack Constraints):
///   Normal:      gameObject.layer = PlayerHurtbox (7)
///   Invincible:  gameObject.layer = PlayerInvincible (8)
/// NEVER use Physics2D.IgnoreLayerCollision — use the layer swap.
///
/// All timing uses Time.unscaledDeltaTime — immune to Time.timeScale slow-motion (Pitfall 4).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class InvincibilityHandler : MonoBehaviour
{
    /// <summary>Flicker interval in real seconds — 0.1s = 10 blinks per second.</summary>
    [SerializeField] private float flickerInterval = 0.1f;

    // Layer indices — must match ProjectSettings/TagManager.asset from Plan 01.
    private const int LayerPlayerHurtbox    = 7;
    private const int LayerPlayerInvincible = 8;

    private SpriteRenderer _spriteRenderer;
    private Coroutine _flickerCoroutine;

    /// <summary>True while the player is invincible.</summary>
    public bool IsInvincible { get; private set; }

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Start invincibility for the specified real-time duration.
    /// Safe to call while already invincible — restarts the timer.
    /// </summary>
    /// <param name="duration">Duration in real seconds (unscaled).</param>
    public void StartInvincibility(float duration)
    {
        if (_flickerCoroutine != null)
            StopCoroutine(_flickerCoroutine);

        _flickerCoroutine = StartCoroutine(InvincibilityCoroutine(duration));
    }

    private IEnumerator InvincibilityCoroutine(float duration)
    {
        IsInvincible = true;

        // Swap to invincible layer — stops enemy hurtboxes from hitting the player.
        gameObject.layer = LayerPlayerInvincible;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Toggle sprite visibility for flicker effect (per D-09).
            _spriteRenderer.enabled = !_spriteRenderer.enabled;

            // Wait using unscaled time — flicker is NOT affected by slow-motion (per D-10).
            yield return new WaitForSecondsRealtime(flickerInterval);
            elapsed += flickerInterval;
        }

        // Restore normal state.
        _spriteRenderer.enabled = true;
        gameObject.layer = LayerPlayerHurtbox;
        IsInvincible = false;
        _flickerCoroutine = null;
    }
}
