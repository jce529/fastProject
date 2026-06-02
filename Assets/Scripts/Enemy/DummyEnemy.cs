using System.Collections;
using UnityEngine;

/// <summary>
/// Stationary combat dummy. Provides a hit target for CombatController's dash attack.
/// OnDashHit() is called explicitly by CombatController after arriving at this position —
/// NOT via a physics trigger (keeps kill sequence explicitly ordered).
///
/// D-08: Gray placeholder visual. D-09: 3-5 placed in scene. D-10: 2s real-time respawn.
/// </summary>
public class DummyEnemy : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 2f; // real seconds — WaitForSecondsRealtime

    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;
    private Vector3 _spawnPosition;

    /// <summary>False during the death+respawn window. CombatController checks this before targeting.</summary>
    public bool IsAlive { get; private set; } = true;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        _spawnPosition = transform.position;
    }

    /// <summary>
    /// Called by CombatController.ExecuteDash() after the player arrives at this position.
    /// Triggers death visual + respawn timer.
    /// </summary>
    public void OnDashHit()
    {
        if (!IsAlive) return;
        StartCoroutine(DeathAndRespawn());
    }

    /// <summary>
    /// Resets highlight color to white. Called by CombatController when this dummy is no longer targeted.
    /// </summary>
    public void ClearHighlight()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.color = Color.white;
    }

    private IEnumerator DeathAndRespawn()
    {
        IsAlive = false;
        _spriteRenderer.enabled = false;
        _collider.enabled = false;

        yield return new WaitForSecondsRealtime(respawnDelay);

        transform.position = _spawnPosition;
        _spriteRenderer.enabled = true;
        _spriteRenderer.color = Color.white;

        // Re-enable collider one frame after sprite appears to avoid immediate
        // physics re-overlap detection (prevents edge-case double-hit on respawn).
        yield return null;
        _collider.enabled = true;

        IsAlive = true;
    }
}
