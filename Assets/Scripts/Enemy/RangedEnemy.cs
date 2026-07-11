using System.Collections;
using UnityEngine;

/// <summary>
/// ENMY-02: Ranged enemy. Detects player, displays LineRenderer aim telegraph (0→1 alpha over 0.8s),
/// then fires a ProjectileController-driven projectile along the locked aim direction.
/// D-09: LineRenderer reuse from RangeDisplay pattern.
/// D-10: moveSpeed=0f default — stationary. Inspector-adjustable post-playtest.
/// D-11, D-12: Projectile via ProjectileController.
/// D-10: 0 = stationary. Chase waits for player to enter aimLineLength before telegraphing.
/// </summary>
public class RangedEnemy : MonoBehaviour, IEnemy, ISpawnGatable
{
    // -- Tunable values (Inspector) ------------------------------------------------
    [SerializeField] private float detectionRadius = 12f;     // Detection + telegraph trigger (Claude's discretion: 10-15 units)
    [SerializeField] private float moveSpeed       = 0f;      // D-10: 0 = stationary. Increase post-playtest.
    [SerializeField] private float aimLineLength   = 15f;     // LineRenderer endpoint length
    [SerializeField] private float kitingRetreatRange = 7.5f; // D-10: aimLineLength(15f)의 약 50% — 이 거리보다 가까워지면 후퇴 시작
    [SerializeField] private float kitingSpeed         = 3f;  // D-07: 후퇴 이동 속도 (Claude's Discretion — chaseSpeed 4f보다 약간 느리게 시작, 플레이테스트로 조정)
    [SerializeField] private float patrolSpeed     = 1.5f;
    [SerializeField] private float patrolHalfRange = 3f;
    [SerializeField] private GameObject projectilePrefab;     // Assign Projectile prefab in Inspector
    [SerializeField] private Transform  firePoint;            // Assign child FirePoint transform in Inspector
    [SerializeField] private float attackWindupDelay = 0.1f; // 발사 애니메이션 트리거 이후 실제 발사까지 대기 (Inspector 조정)

    // -- Layer constants ------------------------------------------------------------
    private const int LayerPlayerHurtbox    = 7;
    private const int LayerPlayerInvincible = 8;
    private const float TelegraphDuration   = 0.8f;

    // -- FSM ------------------------------------------------------------------------
    private enum EnemyState { Idle, Chase, Telegraph, Attack }
    private EnemyState _state = EnemyState.Idle;

    // -- Detection buffer (pre-allocated — no GC, ROADMAP Stack Constraint) ---------
    private readonly Collider2D[] _detectionBuffer = new Collider2D[4];
    private ContactFilter2D _playerFilter;

    // -- Runtime refs ---------------------------------------------------------------
    private Rigidbody2D  _rb;
    private Animator     _animator;
    private LineRenderer _aimLine;
    private Transform    _playerTransform;
    private Vector3      _spawnPosition;
    private float        _patrolDir = 1f;
    private Coroutine    _telegraphCoroutine;

    // -- IEnemy ---------------------------------------------------------------------
    public bool IsAlive { get; private set; } = true;

    // -------------------------------------------------------------------------------

    private void Awake()
    {
        _rb            = GetComponent<Rigidbody2D>();
        _animator      = GetComponent<Animator>();
        _aimLine       = GetComponent<LineRenderer>();
        _spawnPosition = transform.position;

        // Cache filter once in Awake — avoids string lookup in Update (ROADMAP constraint)
        _playerFilter.SetLayerMask(LayerMask.GetMask("PlayerHurtbox"));
        _playerFilter.useTriggers  = false;
        _playerFilter.useLayerMask = true;

        // Configure LineRenderer (D-09 — reuse RangeDisplay pattern)
        if (_aimLine != null)
        {
            _aimLine.positionCount = 2;
            _aimLine.useWorldSpace = true;
            _aimLine.startWidth    = 0.05f;
            _aimLine.endWidth      = 0.05f;
            _aimLine.startColor    = new Color(1f, 0f, 0f, 0f); // red, alpha=0
            _aimLine.endColor      = new Color(1f, 0f, 0f, 0f);
            _aimLine.enabled       = false;
        }
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
        if (_telegraphCoroutine != null) StopCoroutine(_telegraphCoroutine);
        if (_aimLine != null) _aimLine.enabled = false;

        // Freeze physics so corpse stays in place
        if (_rb != null) { _rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static; }

        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
        _animator?.SetBool("isDead", true);

        var deathEffect = GetComponent<EnemyDeathEffect>();
        if (deathEffect == null) deathEffect = gameObject.AddComponent<EnemyDeathEffect>();
        StartCoroutine(deathEffect.PlayDeathSequence(_animator));
    }

    public void ClearHighlight()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }

    // -- ISpawnGatable implementation (SPWN-02, IEnemy와 별도 추가 인터페이스) ------------

    public void SetSpawnGate(bool isSpawning) => IsAlive = !isSpawning;

    // -- Player death listener ------------------------------------------------------

    private void OnPlayerDied()
    {
        if (_telegraphCoroutine != null) StopCoroutine(_telegraphCoroutine);
        if (_aimLine != null) _aimLine.enabled = false;
        _animator?.SetBool("isMoving", false);
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
        // Patrol left/right (same pattern as MeleeEnemy)
        if (moveSpeed > 0f) // only patrol if given a speed (D-10: default 0)
        {
            float newX = transform.position.x + _patrolDir * patrolSpeed * Time.deltaTime;
            if (Mathf.Abs(newX - _spawnPosition.x) >= patrolHalfRange)
                _patrolDir *= -1f;
            _rb.MovePosition(new Vector2(newX, _rb.position.y));
            _animator?.SetBool("isMoving", true);
        }
        else
        {
            _animator?.SetBool("isMoving", false);
        }

        if (IsPlayerInRange(detectionRadius))
        {
            _state = EnemyState.Chase;
        }
    }

    private void UpdateChase()
    {
        if (_playerTransform == null)
        {
            FindPlayerTransform();
            if (_playerTransform == null) { _state = EnemyState.Idle; return; }
        }

        if (!IsPlayerInRange(detectionRadius))
        {
            _playerTransform = null;
            _state = EnemyState.Idle;
            return;
        }

        // D-07/D-08/D-10: Chase 진입 즉시, 거리가 kitingRetreatRange 미만이면 반대 방향으로 후퇴.
        // 근접 적 위치는 참조하지 않는 단순 거리-유지 이동(D-07 명시 범위).
        float dist = Vector2.Distance(transform.position, _playerTransform.position);
        if (dist < kitingRetreatRange)
        {
            float dirAway = Mathf.Sign(transform.position.x - _playerTransform.position.x);
            _rb.linearVelocity = new Vector2(dirAway * kitingSpeed, _rb.linearVelocity.y);
            _animator?.SetBool("isMoving", true);
        }
        else
        {
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y); // 임계값 밖 — 수평 이동 정지 (무한 후퇴 방지)
            _animator?.SetBool("isMoving", false);
        }

        // D-09: 조준/발사는 위 후퇴 이동과 무관하게 매 프레임 그대로 체크 — StartCoroutine은 non-blocking이므로 병행 보장.
        if (IsPlayerInRange(aimLineLength) && _telegraphCoroutine == null)
        {
            _telegraphCoroutine = StartCoroutine(TelegraphAndFire());
        }
    }

    private IEnumerator TelegraphAndFire()
    {
        _state = EnemyState.Telegraph;
        _animator?.SetBool("isMoving", false);

        // Telegraph line starts from firePoint (if assigned), matching where the projectile actually fires from.
        Vector2 origin    = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        Vector2 aimDir    = (_playerTransform != null)
            ? ((Vector2)_playerTransform.position - origin).normalized
            : Vector2.right;

        // Enable and fade LineRenderer from alpha 0 → 1 over 0.8 real seconds
        if (_aimLine != null) _aimLine.enabled = true;

        float elapsed = 0f;
        while (elapsed < TelegraphDuration)
        {
            elapsed += Time.unscaledDeltaTime; // timeScale-immune (ROADMAP Stack Constraint)
            float alpha = Mathf.Clamp01(elapsed / TelegraphDuration);

            if (_aimLine != null)
            {
                Color c = new Color(1f, 0f, 0f, alpha);
                _aimLine.startColor = c;
                _aimLine.endColor   = c;
                _aimLine.SetPosition(0, origin);
                _aimLine.SetPosition(1, origin + aimDir * aimLineLength);
            }

            yield return null; // yield null: resumes every frame, unscaledDeltaTime tracks real time
        }

        // Guard: enemy may have been dash-killed during telegraph
        if (!IsAlive)
        {
            if (_aimLine != null) _aimLine.enabled = false;
            yield break;
        }

        // Hide aim line, fire projectile
        if (_aimLine != null) _aimLine.enabled = false;

        _animator?.SetTrigger("isAttacking");

        // Wait for animation windup before firing — timeScale-immune (ROADMAP Stack Constraint)
        if (attackWindupDelay > 0f)
            yield return new WaitForSecondsRealtime(attackWindupDelay);

        // Guard: player may have killed enemy during windup
        if (!IsAlive) yield break;

        FireProjectile(aimDir, origin);

        _state = EnemyState.Idle;
        _telegraphCoroutine = null;
    }

    private void FireProjectile(Vector2 direction, Vector2 origin)
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[RangedEnemy] projectilePrefab not assigned in Inspector!");
            return;
        }

        // Instantiate at fire point position
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject proj  = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Init must be called on the same frame as Instantiate to set velocity before first FixedUpdate
        var controller = proj.GetComponent<ProjectileController>();
        if (controller != null)
            controller.Init(direction);
        else
            Debug.LogError("[RangedEnemy] Projectile prefab is missing ProjectileController component!");
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
        IsPlayerInRange(detectionRadius);
    }
}
