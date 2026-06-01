using UnityEngine;

/// <summary>
/// Attach to Player. Tracks last safe grounded position and handles fall recovery.
/// MOVE-02 implementation — per D-08 (no effect), D-09 (flicker), D-10 (1s unscaled).
///
/// Last safe position stored as Vector3 (value type) — NEVER a Transform reference.
/// This prevents null refs when the floor system recycles floor objects in v2 (Pitfall 14).
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(InvincibilityHandler))]
public class FallDetector : MonoBehaviour
{
    private PlayerController _controller;
    private InvincibilityHandler _invincibility;
    private Vector3 _lastSafePosition;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _invincibility = GetComponent<InvincibilityHandler>();

        // Initialise to spawn position — safe fallback before any ground contact.
        _lastSafePosition = transform.position;
    }

    private void FixedUpdate()
    {
        // Update last safe position every physics step while grounded.
        // Storing as Vector3 (value type) — no reference to any Transform or GameObject.
        if (_controller.IsGrounded)
        {
            _lastSafePosition = transform.position;
        }
    }

    /// <summary>
    /// Called by the FallZone trigger objects when the player enters them.
    /// Teleports the player to the last known safe position and starts invincibility.
    /// </summary>
    public void OnFall()
    {
        // Instant teleport — no effect, no animation (per D-08).
        transform.position = _lastSafePosition;

        // Zero out velocity so the player does not continue falling after recovery.
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Grant 1-second invincibility with sprite flicker (per D-09, D-10).
        _invincibility.StartInvincibility(1.0f);
    }
}
