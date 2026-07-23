using System.Collections;
using UnityEngine;

/// <summary>
/// BOSS-03/04/05/06 (Phase 15) + Phase 18.1(FIORA.md 재설계): 룸 양끝을 왕복하는 연속 돌진(Dash)을 N회
/// 반복한 뒤 빈틈(Vulnerable) 상태에 진입하는 패턴 루프를 반복하는
/// F.I.O.R.A 전용 보스 FSM. Phase 18(INFRA-03)에서 범용 plumbing(defeat-guard/사망시퀀스/스폰게이팅/
/// 피격하이라이트)이 BossEnemyBase로 추출되고 이 클래스(BossEnemy.cs에서 rename)는 F.I.O.R.A 고유
/// 패턴 루프만 담는다.
/// IEnemy.IsAlive는 MeleeEnemy/RangedEnemy와 다르게 "생존 여부"가 아니라 "현재 타겟 가능(빈틈) 여부"로
/// 오버로드된다 — CombatController.FindNearestEnemyInRange()의 기존 !enemy.IsAlive 스킵 체크를 그대로
/// 재사용해 빈틈 상태에서만 돌진 대상이 되도록 한다(로드맵 Implementation Notes, D-locked). 실제 처치 여부는
/// 별도의 _isDefeated 플래그(BossEnemyBase.Die()가 설정)로 관리한다(15-RESEARCH.md Pitfall 2 — IsAlive를
/// 처치 판정에 재사용하면 CombatController.ExecuteDash()의 ~0.15초 대시 이동 시간 동안 빈틈 창이 닫히는
/// 레이스 컨디션으로 히트가 무시될 수 있음).
/// D-10: 신규 아트 없음 — 기존 MeleeEnemy 스프라이트/애니메이터를 재사용하며 프리팹 단계(15-03)에서 크기/색조만 변형.
/// </summary>
public class FioraBoss : BossEnemyBase
{
    public const string BossId = "Fiora"; // D-03(18-02): BossUnlockManager.Unlock/IsUnlocked 키

    private const int RequiredHits = 7; // BOSS-04: 정확히 7회 피격 시 처치

    // -- Dash pattern (D-01~D-08, Phase 18.1 FIORA.md 재설계) --------------------------
    [SerializeField] private float baseDashSpeed = 16f;               // D-05: _hitCount 0~1 구간 기준 속도 (units/sec)
    private const float DashSpeedTier2Multiplier = 1.25f;              // D-03: _hitCount 2~4 구간 -> 20 u/s
    private const float DashSpeedTier3Multiplier = 1.5f;               // D-03: _hitCount 5~6 구간 -> 24 u/s
    [SerializeField] private float firstDashTelegraphDuration = 0.25f; // D-07: 0.2~0.3초, 사이클당 첫 돌진에만
    private const float WallDwellMin = 0.1f;                           // D-06
    private const float WallDwellMax = 0.2f;                           // D-06
    [SerializeField] private SpriteRenderer _exclamationIcon;          // Child SpriteRenderer - 15-03 프리팹 빌더가 할당 (그대로 유지)
    [SerializeField] private Collider2D     _meleeHitbox;              // Child Trigger Collider2D - 15-03 프리팹 빌더가 할당 (그대로 유지)

    // -- Room bounds cache (D-04) ------------------------------------------------------
    private float _leftBoundX;
    private float _rightBoundX;
    private bool  _boundsResolved;

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
    private enum BossState { Dash, Vulnerable, HitReaction, Dead }
    private BossState _state = BossState.Dash;

    private int _hitCount; // BOSS-05: private, UI/Inspector 바인딩 없음

    // -------------------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();
        if (_meleeHitbox     != null) _meleeHitbox.enabled     = false;
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
    }

    // -- Player death listener cleanup hook (base.OnPlayerDied 골격 재사용) -----------

    protected override void OnPlayerDiedCleanup()
    {
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
        if (_meleeHitbox     != null) _meleeHitbox.enabled     = false;
        _animator?.SetBool("isMoving", false);
    }

    // -- Pattern loop (BOSS-03: 룸 경계 기반 연속 돌진 -> 빈틈 반복, D-01~D-08) --

    private void CacheRoomBounds()
    {
        if (_boundsResolved) return;
        var searchRoot = transform.parent != null ? transform.parent : transform;
        foreach (RoomConnector rc in searchRoot.GetComponentsInChildren<RoomConnector>(true))
        {
            if (rc.direction == RoomConnector.Direction.Left)  _leftBoundX  = rc.transform.position.x;
            if (rc.direction == RoomConnector.Direction.Right) _rightBoundX = rc.transform.position.x;
        }
        _boundsResolved = true;
    }

    private void GetDashTier(int hitCount, out int minDashes, out int maxDashes, out float speed)
    {
        if (hitCount <= 1)      { minDashes = 3; maxDashes = 5; speed = baseDashSpeed; }                           // D-02 초반
        else if (hitCount <= 4) { minDashes = 4; maxDashes = 6; speed = baseDashSpeed * DashSpeedTier2Multiplier; } // D-02 중반
        else                    { minDashes = 5; maxDashes = 7; speed = baseDashSpeed * DashSpeedTier3Multiplier; } // D-02 막판 (5~6)
    }

    protected override IEnumerator PatternLoop()
    {
        CacheRoomBounds();

        while (true)
        {
            // ---- Dash: 룸 경계 왕복 연속 돌진, N회 (D-01/D-02), 속도 티어 (D-03/D-05) ----
            GetDashTier(_hitCount, out int minDashes, out int maxDashes, out float dashSpeed);
            int dashCount = Random.Range(minDashes, maxDashes + 1); // int 오버로드는 max 미포함 - +1로 양 끝 포함

            _state = BossState.Dash;
            IsAlive = false; // 타겟 불가 - 돌진 시퀀스 전체 동안 유지 (BOSS-03)

            float midX = (_leftBoundX + _rightBoundX) * 0.5f;
            float dirX = transform.position.x <= midX ? 1f : -1f; // 룸 기하학 기준 - 플레이어 위치를 절대 참조하지 않는다 (D-04)

            for (int pass = 0; pass < dashCount; pass++)
            {
                // D-07: 이 사이클의 첫 돌진 패스에만 짧은 예고
                if (pass == 0)
                {
                    if (_exclamationIcon != null) _exclamationIcon.enabled = true;
                    float teleElapsed = 0f;
                    while (teleElapsed < firstDashTelegraphDuration)
                    {
                        teleElapsed += Time.unscaledDeltaTime;
                        if (_isDefeated) yield break;
                        yield return null;
                    }
                    if (_exclamationIcon != null) _exclamationIcon.enabled = false;
                    if (_isDefeated) yield break;
                }

                // ---- 돌진 패스: 전체 구간 히트박스 지속 활성화 (D-08) ----
                FlipSprite(dirX);
                _animator?.SetBool("isMoving", true);
                if (_meleeHitbox != null) _meleeHitbox.enabled = true;

                float targetX = dirX > 0f ? _rightBoundX : _leftBoundX;
                while ((dirX > 0f && transform.position.x < targetX) ||
                       (dirX < 0f && transform.position.x > targetX))
                {
                    _rb.linearVelocity = new Vector2(dirX * dashSpeed, _rb.linearVelocity.y);
                    if (_isDefeated) yield break;
                    yield return null;
                }

                // 벽 도착 - 오버슈트 스냅(고속 티어에서 물리 스텝 오버슈트 방지) + 정지 + 히트박스 비활성화
                _rb.position = new Vector2(targetX, _rb.position.y);
                _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
                if (_meleeHitbox != null) _meleeHitbox.enabled = false;
                _animator?.SetBool("isMoving", false);
                if (_isDefeated) yield break;

                // D-06: 0.1~0.2초 안반품 후 방향 전환
                float dwell = Random.Range(WallDwellMin, WallDwellMax);
                float dwellElapsed = 0f;
                while (dwellElapsed < dwell)
                {
                    dwellElapsed += Time.unscaledDeltaTime;
                    if (_isDefeated) yield break;
                    yield return null;
                }
                dirX = -dirX;
            }

            // ---- Vulnerable: 정지 + 색상 변화, 기존 그대로 유지 (D-locked, 변경 없음) ----
            _state = BossState.Vulnerable;
            IsAlive = true; // 타겟 가능 (BOSS-03)
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

            // 빈틈 창이 히트 없이 종료 - 다시 Dash로 (BOSS-04: 패턴 반복)
            IsAlive = false;
            _sr.color = _baseColor;
        }
    }

    // -- IEnemy.OnDashHit (BOSS-04, Pitfall 1/2/6) ------------------------------

    public override void OnDashHit()
    {
        if (_isDefeated) return; // 오직 처치 여부만 가드 — IsAlive(빈틈 여부)는 절대 참조하지 않는다 (Pitfall 2)

        if (_patternCoroutine != null) { StopCoroutine(_patternCoroutine); _patternCoroutine = null; } // Pitfall 6

        _hitCount++;
        if (_hitCount >= RequiredHits) // Pitfall 1: 증가 후 >= 비교 — 정확히 7회째에 처치
        {
            Die(bossMaskRiseDuration, bossParticleColor, bossParticleBurstCount,
                bossDeathShakeDuration, bossDeathShakeAmplitude, BossId);
            return;
        }

        // 1~6회차 비치명타는 점수 관련 호출을 전혀 하지 않는다 (15-CONTEXT.md D-12 SUPERSEDED).
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

    protected override Color GetHighlightColor() => (_state == BossState.Vulnerable) ? vulnerableTintColor : _baseColor;

    // -- Helpers --------------------------------------------------------------------

    private void FlipSprite(float dirX)
    {
        if (dirX == 0f) return;
        if (_sr != null) _sr.flipX = dirX < 0f;
    }
}
