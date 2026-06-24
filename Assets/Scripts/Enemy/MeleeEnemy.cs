using System.Collections;
using UnityEngine;

/// <summary>
/// ENMY-01: Melee enemy with 4-state FSM.
/// States: Idle (patrol) → Chase (close distance) → Telegraph (0.8s ! icon) → Attack (hitbox).
/// One-shot-kill both ways: player dash → OnDashHit(); enemy hitbox → OnPlayerDeath.
/// D-03, D-04, D-05, D-06, D-07, D-08, D-16.
/// </summary>
public class MeleeEnemy : MonoBehaviour, IEnemy
{
    // -- Tunable values (Inspector) ------------------------------------------------
    [SerializeField] private float detectionRadius      = 10f;   // Chase trigger distance (D-04)
    [SerializeField] private float attackRange          = 1.5f;  // Chase→Telegraph trigger distance
    [SerializeField] private float chaseSpeed           = 4f;    // Move speed during Chase
    [SerializeField] private float patrolSpeed          = 2f;    // Idle patrol speed
    [SerializeField] private float patrolHalfRange      = 3f;    // Half-width of patrol walk
    [SerializeField] private float hitboxActiveDuration = 0.15f; // Seconds hitbox stays active
    [SerializeField] private float attackWindupDelay = 0.1f;     // 애니메이션 windup 이후 히트박스 활성화까지 대기 (Inspector 조정)
    [SerializeField] private SpriteRenderer _exclamationIcon;    // Child SpriteRenderer with "!" sprite — assign in Inspector
    [SerializeField] private Collider2D     _meleeHitbox;        // Child Trigger Collider2D — assign in Inspector

    // -- Layer constants (hardcoded — matches TagManager.asset, established pattern) --
    private const int LayerPlayerHurtbox    = 7;
    private const int LayerPlayerInvincible = 8;

    // -- FSM state ------------------------------------------------------------------
    private enum EnemyState { Idle, Chase, Telegraph, Attack }
    private EnemyState _state = EnemyState.Idle;

    // -- Detection buffer (pre-allocated — no GC per frame, ROADMAP Stack Constraint) --
    private readonly Collider2D[] _detectionBuffer = new Collider2D[4];
    private ContactFilter2D _playerFilter;

    // -- Runtime refs ---------------------------------------------------------------
    private Rigidbody2D _rb;
    private Animator     _animator;
    private Transform   _playerTransform;
    private Vector3     _spawnPosition;
    private float       _patrolDir = 1f;
    private Coroutine   _attackCoroutine;

    // -- IEnemy ---------------------------------------------------------------------
    public bool IsAlive { get; private set; } = true;

    // -------------------------------------------------------------------------------

    private void Awake()
    {
        _rb            = GetComponent<Rigidbody2D>();
        _animator      = GetComponent<Animator>();
        _spawnPosition = transform.position;

        // Cache filter once — avoids LayerMask.GetMask() string lookup in Update (ROADMAP constraint)
        _playerFilter.SetLayerMask(LayerMask.GetMask("PlayerHurtbox"));
        _playerFilter.useTriggers  = false;
        _playerFilter.useLayerMask = true;

        // Start with hitbox and icon disabled
        if (_meleeHitbox      != null) _meleeHitbox.enabled      = false;
        if (_exclamationIcon  != null) _exclamationIcon.enabled  = false;
    }

    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += OnPlayerDied;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDeath -= OnPlayerDied;
    }

    // -- IEnemy implementation ------------------------------------------------------

    public void OnDashHit()
    {
        if (!IsAlive) return;
        IsAlive = false;
        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        if (_meleeHitbox     != null) _meleeHitbox.enabled    = false;
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;

        // Freeze physics so corpse stays in place
        if (_rb != null) { _rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static; }

        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
        _animator?.SetBool("isDead", true);
    }

    public void ClearHighlight()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }

    // -- Player death listener (stop chasing dead player) ---------------------------

    private void OnPlayerDied()
    {
        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
        if (_meleeHitbox     != null) _meleeHitbox.enabled     = false;
        _animator?.SetBool("isMoving", false);
        _animator?.SetBool("isChasing", false);
        _state = EnemyState.Idle;
    }

    // -- FSM Update -----------------------------------------------------------------

    private void Update()
    {
        if (!IsAlive) return;

        switch (_state)
        {
            case EnemyState.Idle:      UpdateIdle();  break;
            case EnemyState.Chase:     UpdateChase(); break;
            case EnemyState.Telegraph: break; // coroutine owns this state
            case EnemyState.Attack:    break; // coroutine owns this state
        }
    }

    // -- State logic ----------------------------------------------------------------

    private void UpdateIdle()
    {
        // Patrol: left/right bounce around spawn position
        float newX = transform.position.x + _patrolDir * patrolSpeed * Time.deltaTime;
        if (Mathf.Abs(newX - _spawnPosition.x) >= patrolHalfRange)
            _patrolDir *= -1f;
        _rb.MovePosition(new Vector2(newX, _rb.position.y));
        _animator?.SetBool("isMoving", true);
        _animator?.SetBool("isChasing", false);

        // Detection check every frame (buffer pre-allocated — no alloc)
        if (IsPlayerInRange(detectionRadius))
            _state = EnemyState.Chase;
    }

    private void UpdateChase()
    {
        if (_playerTransform == null)
        {
            // Find player transform once; use cached reference after
            FindPlayerTransform();
            if (_playerTransform == null) { _state = EnemyState.Idle; return; }
        }

        // Within attack range → start telegraph
        float dist = Vector2.Distance(transform.position, _playerTransform.position);
        if (dist <= attackRange)
        {
            _attackCoroutine = StartCoroutine(TelegraphAndAttack());
            return;
        }

        // Move toward player
        Vector2 dir = ((Vector2)_playerTransform.position - _rb.position).normalized;
        _rb.MovePosition(_rb.position + dir * chaseSpeed * Time.deltaTime);
        _animator?.SetBool("isMoving", true);
        _animator?.SetBool("isChasing", true);
    }

    private IEnumerator TelegraphAndAttack()
    {
        _state = EnemyState.Telegraph;
        _animator?.SetBool("isMoving", false);
        _animator?.SetBool("isChasing", false);

        // Show "!" icon (D-05)
        if (_exclamationIcon != null) _exclamationIcon.enabled = true;

        // 0.8 real seconds — WaitForSecondsRealtime is timeScale-immune (D-06, ROADMAP Stack Constraint)
        yield return new WaitForSecondsRealtime(0.8f);

        // Guard: enemy may have been killed during the telegraph window
        // This is the critical guard preventing attack on already-dead enemy
        if (!IsAlive) yield break;

        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
        _state = EnemyState.Attack;
        _animator?.SetTrigger("isAttacking");

        // Wait for animation windup before activating hitbox — timeScale-immune (ROADMAP Stack Constraint)
        if (attackWindupDelay > 0f)
            yield return new WaitForSecondsRealtime(attackWindupDelay);

        // Guard: player may have killed enemy during windup
        if (!IsAlive) yield break;

        // Activate melee hitbox briefly (D-07, D-08)
        if (_meleeHitbox != null)
        {
            _meleeHitbox.enabled = true;
            yield return new WaitForSecondsRealtime(hitboxActiveDuration);
            if (_meleeHitbox != null) _meleeHitbox.enabled = false;
        }

        // Return to Chase or Idle based on player proximity
        _state = IsPlayerInRange(detectionRadius) ? EnemyState.Chase : EnemyState.Idle;
        _attackCoroutine = null;
    }

    // -- Physics / melee hit --------------------------------------------------------

    /// <summary>
    /// Called when the melee hitbox (child Trigger Collider2D) overlaps the player.
    /// Fires OnPlayerDeath — one-shot kill (no HP system per REQUIREMENTS.md Out of Scope).
    /// D-16: PlayerInvincible layer is excluded from the hitbox via Physics2D collision matrix
    ///       (configured in Plan 03-02). No additional layer check needed here.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only react when the melee hitbox is active
        if (_meleeHitbox == null || !_meleeHitbox.enabled) return;
        if (other.CompareTag("Player"))
        {
            PlayerController.TriggerDeath();
        }
    }

    // -- Helpers --------------------------------------------------------------------

    private bool IsPlayerInRange(float radius)
    {
        int count = Physics2D.OverlapCircle(
            (Vector2)transform.position,
            radius,
            _playerFilter,
            _detectionBuffer);
        if (count > 0 && _detectionBuffer[0] != null)
            _playerTransform = _detectionBuffer[0].transform;
        return count > 0;
    }

    private void FindPlayerTransform()
    {
        int count = Physics2D.OverlapCircle(
            (Vector2)transform.position,
            detectionRadius,
            _playerFilter,
            _detectionBuffer);
        if (count > 0 && _detectionBuffer[0] != null)
            _playerTransform = _detectionBuffer[0].transform;
    }
}
