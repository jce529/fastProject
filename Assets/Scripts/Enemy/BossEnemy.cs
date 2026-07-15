using System.Collections;
using UnityEngine;

/// <summary>
/// BOSS-03/04/05/06 (Phase 15): 예고(Telegraph)→공격(Attack)→빈틈(Vulnerable) 단일 패턴 루프를 반복하는 보스 FSM.
/// IEnemy.IsAlive는 MeleeEnemy/RangedEnemy와 다르게 "생존 여부"가 아니라 "현재 타겟 가능(빈틈) 여부"로
/// 오버로드된다 — CombatController.FindNearestEnemyInRange()의 기존 !enemy.IsAlive 스킵 체크를 그대로
/// 재사용해 빈틈 상태에서만 돌진 대상이 되도록 한다(로드맵 Implementation Notes, D-locked). 실제 처치 여부는
/// 별도의 _isDefeated 플래그로 관리한다(15-RESEARCH.md Pitfall 2 — IsAlive를 처치 판정에 재사용하면
/// CombatController.ExecuteDash()의 ~0.15초 대시 이동 시간 동안 빈틈 창이 닫히는 레이스 컨디션으로 히트가
/// 무시될 수 있음).
/// D-10: 신규 아트 없음 — 기존 MeleeEnemy 스프라이트/애니메이터를 재사용하며 프리팹 단계(15-03)에서 크기/색조만 변형.
/// D-12 SUPERSEDED(15-CONTEXT.md): Phase 16(EnemyBase 리팩토링)이 점수 적립 시점을
/// CombatController.ExecuteDash()에서 각 적의 OnDashHit()로 옮기면서 CombatController는 더 이상
/// 무조건적으로 AddKillScore()를 호출하지 않는다 — BossEnemy는 EnemyBase를 상속하지 않으므로 이 흐름에
/// 아예 포함되지 않아 비치명타 상쇄 로직이 불필요해졌다(OnDashHit() 참조).
/// </summary>
public class BossEnemy : MonoBehaviour, IEnemy, ISpawnGatable
{
    private const int RequiredHits = 7; // BOSS-04: 정확히 7회 피격 시 처치

    // -- Telegraph & Attack (D-01, D-04) --------------------------------------------
    [SerializeField] private float moveSpeed = 4f;                  // Telegraph 이동 속도 기준값
    [SerializeField] private float telegraphSpeedMultiplier = 0.4f; // D-04: MeleeEnemy D-05(999.4)와 동일한 "이동하며 예고" 배율
    [SerializeField] private float telegraphDuration = 0.6f;        // 예고 이동 지속 시간
    [SerializeField] private float attackWindupDelay = 0.15f;       // 애니메이션 windup 이후 히트박스 활성화까지 대기
    [SerializeField] private float hitboxActiveDuration = 0.2f;     // 히트박스 활성 지속 시간
    [SerializeField] private SpriteRenderer _exclamationIcon;       // Child SpriteRenderer — 15-03 프리팹 빌더가 할당
    [SerializeField] private Collider2D     _meleeHitbox;           // Child Trigger Collider2D — 15-03 프리팹 빌더가 할당

    // -- Vulnerable window (D-02, D-03) ----------------------------------------------
    [SerializeField] private float vulnerableDuration = 1.0f;                        // D-03: 0.8~1.2초 범위 내
    [SerializeField] private Color vulnerableTintColor = new Color(1f, 0.85f, 0.1f);  // D-02: 정지+색상 이중 신호 중 색상 축

    // -- Hit reaction & pattern reset (D-06, D-07) ------------------------------------
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float hitFlashDuration = 0.15f;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float resetPauseDuration = 0.5f; // D-07: 히트 반응이 읽힐 시간 확보 후 리셋

    // -- Death sequence extension (D-08) ----------------------------------------------
    [SerializeField] private float bossMaskRiseDuration = 1.2f;                 // EnemyDeathEffect 기본값(0.6f) 대비 연장
    [SerializeField] private Color bossParticleColor = new Color(1f, 0.5f, 0.1f);
    [SerializeField] private int   bossParticleBurstCount = 30;                 // 기본값(12) 대비 확장
    [SerializeField] private float bossDeathShakeDuration = 0.4f;               // 일반 처치(0.15f) 대비 연장
    [SerializeField] private float bossDeathShakeAmplitude = 0.5f;              // 일반 처치(0.2f) 대비 확장

    // -- FSM state ---------------------------------------------------------------------
    private enum BossState { Telegraph, Attack, Vulnerable, HitReaction, Dead }
    private BossState _state = BossState.Telegraph;

    // -- Runtime refs --------------------------------------------------------------------
    private Rigidbody2D    _rb;
    private Animator       _animator;
    private SpriteRenderer _sr;
    private Transform      _playerTransform;
    private CameraFollow   _cameraFollow;
    private Coroutine      _patternCoroutine;
    private Color          _baseColor; // D-10: 프리팹에 지정된 보스 전용 색조 — Telegraph/Attack 복귀 시 기준색(Pitfall 3 일반화)

    private int  _hitCount;   // BOSS-05: private, UI/Inspector 바인딩 없음
    private bool _isDefeated; // OnDashHit()이 유일하게 참조하는 종료 플래그 — IsAlive(빈틈 여부)와 분리 (Pitfall 2)

    // -- IEnemy --------------------------------------------------------------------------
    public bool IsAlive { get; private set; } = true; // MeleeEnemy와 동일 기본값 — SetSpawnGate(true)가 스폰 중 false로 전환

    // -------------------------------------------------------------------------------

    private void Awake()
    {
        _rb        = GetComponent<Rigidbody2D>();
        _animator  = GetComponent<Animator>();
        _sr        = GetComponent<SpriteRenderer>();
        _baseColor = _sr.color; // D-10: BossEnemyPrefabBuilder(15-03)가 지정한 색조를 기준색으로 캡처

        var player = FindFirstObjectByType<PlayerController>();
        _playerTransform = player != null ? player.transform : null;

        var cam = Camera.main;
        _cameraFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;

        if (_meleeHitbox     != null) _meleeHitbox.enabled     = false;
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
    }

    private void Start()
    {
        // SPWN-02 호환: EnemySpawner.Activate() 경로는 SetSpawnGate(true)를 Awake보다 먼저 호출하므로
        // 이 시점에 IsAlive == false다 — 그 경우 패턴은 SetSpawnGate(false)가 열릴 때 시작된다.
        // D-11 DebugRoomTeleporter 직접 Instantiate 경로(15-03)는 SetSpawnGate 호출이 없어 IsAlive == true
        // 그대로이므로 여기서 즉시 시작한다.
        if (IsAlive && _patternCoroutine == null && !_isDefeated)
            _patternCoroutine = StartCoroutine(PatternLoop());
    }

    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += OnPlayerDied;
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDeath -= OnPlayerDied;
    }

    // -- ISpawnGatable (SPWN-01/02, Phase 14 파이프라인 재사용 전제) -------------------

    public void SetSpawnGate(bool isSpawning)
    {
        IsAlive = !isSpawning;
        if (!isSpawning && _patternCoroutine == null && !_isDefeated)
            _patternCoroutine = StartCoroutine(PatternLoop());
    }

    // -- Player death listener -------------------------------------------------------

    private void OnPlayerDied()
    {
        if (_patternCoroutine != null) { StopCoroutine(_patternCoroutine); _patternCoroutine = null; }
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
        if (_meleeHitbox     != null) _meleeHitbox.enabled     = false;
        _animator?.SetBool("isMoving", false);
    }

    // -- Pattern loop (BOSS-03: 예고→공격→빈틈 단일 패턴 반복, D-01/D-02/D-03/D-04/D-05) --

    private IEnumerator PatternLoop()
    {
        while (true)
        {
            // ---- Telegraph: 느려진 속도로 이동하며 예고 (D-04) ----
            _state = BossState.Telegraph;
            IsAlive = false; // 타겟 불가 — CombatController의 기존 !IsAlive 스킵 체크 재사용 (BOSS-03)
            _sr.color = _baseColor;
            if (_exclamationIcon != null) _exclamationIcon.enabled = true;
            _animator?.SetBool("isMoving", true);

            float telegraphElapsed = 0f;
            while (telegraphElapsed < telegraphDuration)
            {
                telegraphElapsed += Time.unscaledDeltaTime; // timeScale-immune
                if (_isDefeated) yield break;

                if (_playerTransform != null)
                {
                    float dirX = Mathf.Sign(_playerTransform.position.x - transform.position.x);
                    _rb.linearVelocity = new Vector2(dirX * moveSpeed * telegraphSpeedMultiplier, _rb.linearVelocity.y);
                    FlipSprite(dirX);
                }
                yield return null;
            }
            if (_isDefeated) yield break;

            // ---- Attack: 근접 히트박스 공격 (D-01 — MeleeEnemy.TelegraphAndAttack() windup+hitbox 재사용) ----
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            if (_exclamationIcon != null) _exclamationIcon.enabled = false;
            _state = BossState.Attack;
            _animator?.SetTrigger("isAttacking");

            if (attackWindupDelay > 0f)
                yield return new WaitForSecondsRealtime(attackWindupDelay);
            if (_isDefeated) yield break;

            if (_meleeHitbox != null)
            {
                _meleeHitbox.enabled = true;
                yield return new WaitForSecondsRealtime(hitboxActiveDuration);
                if (_meleeHitbox != null) _meleeHitbox.enabled = false;
            }
            if (_isDefeated) yield break;

            // ---- Vulnerable: 정지 + 색상 변화 (D-02), 0.8~1.2초 (D-03) ----
            _state = BossState.Vulnerable;
            IsAlive = true; // 타겟 가능 — CombatController가 이제 이 보스를 후보로 포함 (BOSS-03)
            _rb.linearVelocity = Vector2.zero;
            _sr.color = vulnerableTintColor;
            _animator?.SetBool("isMoving", false);

            float vulnerableElapsed = 0f;
            while (vulnerableElapsed < vulnerableDuration)
            {
                vulnerableElapsed += Time.unscaledDeltaTime;
                if (_isDefeated) yield break;
                yield return null;
            }

            // 빈틈 창이 히트 없이 종료 — 다시 Telegraph로 (D-05: 단일 패턴 반복)
            IsAlive = false;
            _sr.color = _baseColor;
        }
    }

    // -- IEnemy.OnDashHit (BOSS-04, D-12(SUPERSEDED), Pitfall 1/2/6) ------------------------------

    public void OnDashHit()
    {
        if (_isDefeated) return; // 오직 처치 여부만 가드 — IsAlive(빈틈 여부)는 절대 참조하지 않는다 (Pitfall 2)

        if (_patternCoroutine != null) { StopCoroutine(_patternCoroutine); _patternCoroutine = null; } // Pitfall 6

        _hitCount++;
        if (_hitCount >= RequiredHits) // Pitfall 1: 증가 후 >= 비교 — 정확히 7회째에 처치
        {
            _isDefeated = true;
            IsAlive = false;
            Die();
            return;
        }

        // D-12 SUPERSEDED(15-CONTEXT.md, Phase 16 discuss-phase 세션): 원안은 CombatController.ExecuteDash()가
        // OnDashHit() 직후 무조건 ScoreManager.AddKillScore(false)(+100)를 호출한다는 전제로, 비치명타마다
        // 그 +100을 SubtractScore()로 상쇄하는 설계였다. Phase 16(EnemyBase 리팩토링)이 점수 적립 호출을
        // CombatController에서 각 적의 OnDashHit()(EnemyBase 공통부)로 옮기면서 CombatController는 더 이상
        // 무조건적으로 점수를 더하지 않는다(CombatController.cs:291-293 확인) — BossEnemy는 EnemyBase를
        // 상속하지 않아 이 흐름 자체에 포함되지 않으므로, 상쇄할 점수가 애초에 존재하지 않는다.
        // 따라서 1~6회차 비치명타는 점수 관련 호출을 전혀 하지 않는다.

        _patternCoroutine = StartCoroutine(HitReactionAndReset());
    }

    // -- 피격 반응 + 리셋 공백 (D-06, D-07) --------------------------------------------

    private IEnumerator HitReactionAndReset()
    {
        _state = BossState.HitReaction;
        IsAlive = false; // 스태거 중에는 타겟 불가
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
        if (_meleeHitbox     != null) _meleeHitbox.enabled     = false;

        // D-06: 색상 플래시 + 넉백/스태거 (히트스파크는 CombatController.ExecuteDash()가 모든 적 공통으로
        // 이미 SpawnHitSpark(destination)를 호출하므로 여기서 별도 재생 불필요 — CombatController.cs:295)
        _sr.color = hitFlashColor;
        Vector2 knockbackDir = _playerTransform != null
            ? ((Vector2)transform.position - (Vector2)_playerTransform.position).normalized
            : Vector2.left;
        if (knockbackDir.sqrMagnitude < 0.001f) knockbackDir = Vector2.left;
        if (_rb != null) _rb.linearVelocity = knockbackDir * knockbackForce;

        float flashElapsed = 0f;
        while (flashElapsed < hitFlashDuration)
        {
            flashElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (_rb != null) _rb.linearVelocity = Vector2.zero;
        _sr.color = _baseColor;

        // D-07: 짧은 공백을 둔 뒤 패턴 처음부터 재시작
        yield return new WaitForSecondsRealtime(resetPauseDuration);

        _patternCoroutine = StartCoroutine(PatternLoop());
    }

    // -- 처치 (BOSS-04/06, D-08/D-09) --------------------------------------------------

    private void Die()
    {
        _state = BossState.Dead;
        if (_patternCoroutine != null) { StopCoroutine(_patternCoroutine); _patternCoroutine = null; }
        if (_meleeHitbox     != null) _meleeHitbox.enabled     = false;
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;

        if (_rb != null) { _rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static; }
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
        _animator?.SetBool("isDead", true);

        var deathEffect = GetComponent<EnemyDeathEffect>();
        if (deathEffect == null) deathEffect = gameObject.AddComponent<EnemyDeathEffect>();
        deathEffect.ConfigureIntensity(bossMaskRiseDuration, bossParticleColor, bossParticleBurstCount); // D-08
        StartCoroutine(deathEffect.PlayDeathSequence(_animator));

        _cameraFollow?.Shake(bossDeathShakeDuration, bossDeathShakeAmplitude); // D-08
        ScoreManager.AddBossKillScore(); // BOSS-06/D-09 — 7회째 히트 자체는 EnemyBase 경로를 거치지 않아 별도 KillScore 적립이 없으므로, 보스 보너스만 그대로 가산
    }

    // -- Physics / melee hit (플레이어 원샷킬 — MeleeEnemy.OnTriggerEnter2D()와 동일 패턴) --

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_meleeHitbox == null || !_meleeHitbox.enabled) return;
        if (other.CompareTag("Player"))
        {
            PlayerController.TriggerDeath();
        }
    }

    // -- IEnemy.ClearHighlight override (Pitfall 3 — 하드코딩된 흰색이 빈틈 색조를 지우는 문제 방지) ----

    public void ClearHighlight()
    {
        if (_sr == null) return;
        _sr.color = (_state == BossState.Vulnerable) ? vulnerableTintColor : _baseColor;
    }

    // -- Helpers --------------------------------------------------------------------

    private void FlipSprite(float dirX)
    {
        if (dirX == 0f) return;
        if (_sr != null) _sr.flipX = dirX < 0f;
    }
}
