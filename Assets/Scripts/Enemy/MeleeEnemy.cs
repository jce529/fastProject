using System.Collections;
using UnityEngine;

/// <summary>
/// ENMY-01: Melee enemy with 4-state FSM.
/// States: Idle (patrol) → Chase (close distance) → Telegraph (0.8s ! icon) → Attack (hitbox).
/// One-shot-kill both ways: player dash → OnDashHit(); enemy hitbox → OnPlayerDeath.
/// D-03, D-04, D-05, D-06, D-07, D-08, D-16.
/// </summary>
public class MeleeEnemy : MonoBehaviour, IEnemy, ISpawnGatable
{
    // -- Tunable values (Inspector) ------------------------------------------------
    [SerializeField] private float detectionRadius      = 10f;   // Chase trigger distance (D-04)
    [SerializeField] private float attackRange          = 1.5f;  // Chase→Telegraph trigger distance
    [SerializeField] private float chaseSpeed           = 4f;    // Move speed during Chase
    [SerializeField] private float patrolSpeed          = 2f;    // Idle patrol speed
    [SerializeField] private float patrolHalfRange      = 3f;    // Half-width of patrol walk
    [SerializeField] private float hitboxActiveDuration = 0.15f; // Seconds hitbox stays active
    [SerializeField] private float attackWindupDelay = 0.1f;     // 애니메이션 windup 이후 히트박스 활성화까지 대기 (Inspector 조정)
    [SerializeField] private float telegraphDuration       = 0.45f; // D-04: 0.8s→0.4~0.5s 단축 (원본 근거: Phase 3 D-06 "구르기로 회피 가능한 여유" 유지)
    [SerializeField] private float telegraphSpeedMultiplier = 0.4f;  // D-05: chaseSpeed 대비 이동 속도 배율 (Claude's Discretion: 30~50% 범위 중 40%)
    [SerializeField] private SpriteRenderer _exclamationIcon;    // Child SpriteRenderer with "!" sprite — assign in Inspector
    [SerializeField] private Collider2D     _meleeHitbox;        // Child Trigger Collider2D — assign in Inspector
    [SerializeField] private float jumpForce     = 13f;   // 점프 초속 (D-01 하드닝: 8f→13f, Room_Gap 3유닛 클리어 추정치 — RESEARCH.md 물리 계산, 플레이테스트로 재조정 가능)
    [SerializeField] private float wallCheckDist = 0.4f;  // 앞 벽 감지 거리
    [SerializeField] private float gapCheckDist  = 0.6f;  // 바닥 끊김 감지 거리 (발 앞으로)
    [SerializeField] private float maxJumpableGapWidth = 3.2f; // D-01: 이 거리보다 먼 곳까지 바닥이 없으면 점프로 건널 수 없는 gap → 턴어라운드 (Room_Fall 5.5유닛을 걸러내고 Room_Gap 3유닛은 통과시키는 값)

    // -- FSM state ------------------------------------------------------------------
    private enum EnemyState { Idle, Chase, Telegraph, Attack }
    private EnemyState _state = EnemyState.Idle;

    // -- Detection buffer (pre-allocated — no GC per frame, ROADMAP Stack Constraint) --
    private readonly Collider2D[] _detectionBuffer = new Collider2D[4];
    private ContactFilter2D _playerFilter;

    // -- Runtime refs ---------------------------------------------------------------
    private Rigidbody2D _rb;
    private Animator     _animator;
    private SpriteRenderer _sr;
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
        _sr            = GetComponent<SpriteRenderer>();
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
        if (Mathf.Abs(transform.position.x - _spawnPosition.x) >= patrolHalfRange)
            _patrolDir *= -1f;
        _rb.linearVelocity = new Vector2(_patrolDir * patrolSpeed, _rb.linearVelocity.y);
        FlipSprite(_patrolDir);
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

        // Move toward player (x only — Y left to Unity 2D physics)
        float dirX = Mathf.Sign(_playerTransform.position.x - transform.position.x);

        // D-01 하드닝 (RESEARCH.md Pitfall 2): 점프로 건널 수 없을 만큼 넓은 gap이면 뛰어들지 않고 턴어라운드
        if (IsGrounded() && GapAhead(dirX) && GapTooWideToCross(dirX))
            dirX = -dirX;

        _rb.linearVelocity = new Vector2(dirX * chaseSpeed, _rb.linearVelocity.y);
        FlipSprite(dirX);
        TryJump(dirX);
        _animator?.SetBool("isMoving", true);
        _animator?.SetBool("isChasing", true);
    }

    private IEnumerator TelegraphAndAttack()
    {
        _state = EnemyState.Telegraph;
        _animator?.SetBool("isMoving", true);   // D-05: 이동을 계속하므로 walk-cycle 애니메이션 유지 (기존 false에서 변경 — 미확인 시 Task 2에서 시각 검증)
        _animator?.SetBool("isChasing", false);

        // Show "!" icon (D-05)
        if (_exclamationIcon != null) _exclamationIcon.enabled = true;

        // D-04/D-05: RangedEnemy.TelegraphAndFire()의 per-frame 루프 형태를 mirror —
        // 단일 블로킹 WaitForSecondsRealtime(0.8f) 대신 매 프레임 이동 + FlipSprite 갱신.
        float elapsed = 0f;
        while (elapsed < telegraphDuration)
        {
            elapsed += Time.unscaledDeltaTime; // timeScale-immune (ROADMAP Stack Constraint)

            if (!IsAlive) yield break; // 가드: 루프 도중 언제든 처치될 수 있음 (기존엔 대기 종료 후 1회만 체크)

            if (_playerTransform != null)
            {
                float dirX = Mathf.Sign(_playerTransform.position.x - transform.position.x);
                _rb.linearVelocity = new Vector2(dirX * chaseSpeed * telegraphSpeedMultiplier, _rb.linearVelocity.y);
                FlipSprite(dirX); // D-05: 기존 TelegraphAndAttack()은 FlipSprite를 호출하지 않았음 — 이동 방향과 스프라이트 일치를 위해 추가
            }

            yield return null;
        }

        // Guard: enemy may have been killed during the telegraph window
        // This is the critical guard preventing attack on already-dead enemy
        if (!IsAlive) yield break;

        // Telegraph 종료 — Attack 상태로 전환하며 이동 정지 (기존 Attack 단계는 정지 상태 유지, D-04/D-05는 Telegraph만 대상)
        _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

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

    /// <summary>땅에 있을 때만 점프. 앞에 벽 또는 바닥 끊김 감지 시 발동.</summary>
    private void TryJump(float moveDir)
    {
        if (!IsGrounded()) return;
        if (WallAhead(moveDir) || GapAhead(moveDir))
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
    }

    /// <summary>발 아래 짧은 Raycast — 땅 위에 있으면 true.</summary>
    private bool IsGrounded()
    {
        // 발 위치: 콜라이더 하단 기준 (0.1f 여유)
        Vector2 origin = (Vector2)transform.position + Vector2.down * 0.55f;
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.15f,
            LayerMask.GetMask("Default", "Ground", "Platform"));
        return hit.collider != null;
    }

    /// <summary>이동 방향 앞 벽 Raycast — 충돌체 있으면 true.</summary>
    private bool WallAhead(float moveDir)
    {
        Vector2 origin = (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(origin, new Vector2(moveDir, 0f), wallCheckDist,
            LayerMask.GetMask("Default", "Ground", "Platform"));
        return hit.collider != null;
    }

    /// <summary>이동 방향 발 앞 아래 Raycast — 바닥 없으면 gap → true.</summary>
    private bool GapAhead(float moveDir)
    {
        // 발 앞 수평으로 gapCheckDist 이동한 지점에서 아래로 Ray
        Vector2 origin = (Vector2)transform.position + new Vector2(moveDir * gapCheckDist, -0.5f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.5f,
            LayerMask.GetMask("Default", "Ground", "Platform"));
        return hit.collider == null;
    }

    /// <summary>D-01 하드닝(Pitfall 2): maxJumpableGapWidth보다 먼 지점까지도 바닥이 없으면 점프로 건널 수 없는 gap → true.</summary>
    private bool GapTooWideToCross(float moveDir)
    {
        Vector2 origin = (Vector2)transform.position + new Vector2(moveDir * maxJumpableGapWidth, -0.5f);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, 0.5f,
            LayerMask.GetMask("Default", "Ground", "Platform"));
        return hit.collider == null;
    }

    /// <summary>이동 방향에 따라 스프라이트 좌우 반전.</summary>
    private void FlipSprite(float dirX)
    {
        if (dirX == 0f) return;
        if (_sr != null) _sr.flipX = dirX < 0f;
    }
}
