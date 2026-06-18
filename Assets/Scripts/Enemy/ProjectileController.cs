using UnityEngine;

/// <summary>
/// ENMY-02: Straight-line projectile fired by RangedEnemy.
/// D-11: Rigidbody2D Dynamic, Gravity=0, Continuous, Interpolate. Init(direction) sets velocity.
/// D-12: Destroys on Platform contact OR when distance exceeds maxDistance.
/// D-16: PlayerInvincible layer excluded via Physics2D matrix (Plan 03-02) — no code check needed.
/// One-shot kill: player hit → PlayerController.OnPlayerDeath?.Invoke().
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ProjectileController : MonoBehaviour
{
    [SerializeField] private float speed       = 10f;  // Units per second (Claude's discretion: 8-12)
    [SerializeField] private float maxDistance = 20f;  // Self-destruct distance

    // Layer constants — hardcoded to match TagManager.asset (established pattern, no string lookup)
    private const int LayerPlatform = 9;

    private Rigidbody2D _rb;
    private Vector2     _startPosition;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// Called by RangedEnemy after instantiating the projectile.
    /// Must be called on the frame of instantiation before the first FixedUpdate.
    /// </summary>
    public void Init(Vector2 direction)
    {
        _startPosition = _rb.position;
        _rb.linearVelocity = direction.normalized * speed;
    }

    private void FixedUpdate()
    {
        // Distance-based lifetime — SqrMagnitude avoids sqrt (faster than Distance)
        if ((_rb.position - _startPosition).sqrMagnitude >= maxDistance * maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Platform contact — destroy projectile (D-12)
        if (other.gameObject.layer == LayerPlatform)
        {
            Destroy(gameObject);
            return;
        }

        // Player contact — one-shot kill (D-11)
        // PlayerInvincible layer is excluded by Physics2D matrix (Plan 03-02, D-16).
        // CompareTag is faster than GetComponent for kill check.
        if (other.CompareTag("Player"))
        {
            PlayerController.TriggerDeath();
            Destroy(gameObject);
        }
    }
}
