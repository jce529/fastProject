using System.Collections;
using UnityEngine;

/// <summary>
/// SAMURAI-01/03/04/05 (Phase 19): BossEnemyBase를 상속하는 SAMURAI 전용 FSM.
/// D-07(평시 콤보)/D-08(패링 전용 타이밍)을 번갈아 반복하고, 그로기 게이지가 임계치에 도달하면
/// D-09(그로기 2단계 누적)/D-10(그로기 중 피격만 처치 카운트)/D-11(7회 처치)로 처치된다.
/// FioraBoss와 달리 평시 구간에도 IsAlive=true를 유지한다(19-CONTEXT.md D-09, 19-04-PLAN.md 전제 정정) —
/// 별도 chase/patrol 없이 고정된 로컬 히트박스로 텔레그래프→공격을 반복하는 1v1 보스룸 전제(Claude's Discretion).
/// 패링 전용 타이밍의 공격은 물리 투사체(ParryableProjectile)로 전달되어 죽음 판정을 코드로 직접 하지 않는다(Pitfall 2).
/// </summary>
public class SamuraiBoss : BossEnemyBase
{
    public const string BossId = "Samurai"; // SAMURAI-01: BossUnlockManager.Unlock/IsUnlocked 키

    private const int RequiredCycles = 7; // D-11: 정확히 7회 처치 진행 시 처치

    [SerializeField] private Collider2D _meleeHitbox;
    [SerializeField] private SpriteRenderer _exclamationIcon;

    [Header("평시 콤보 (D-07)")]
    [SerializeField] private float normalTelegraphDuration = 0.5f;
    [SerializeField] private float hitboxActiveDuration = 0.15f;

    [Header("패링 전용 타이밍 (D-08)")]
    [SerializeField] private float parryTelegraphDuration = 0.3f;
    [SerializeField] private float parryProjectileSpeed = 8f;
    [SerializeField] private float parryProjectileMaxDistance = 15f;

    [Header("그로기 게이지 (D-09/D-10)")]
    [SerializeField] private float groggyThreshold = 3f; // Claude's Discretion — 플레이테스트 튜닝 대상
    [SerializeField] private float groggyPerHit = 1f;
    [SerializeField] private float groggyPerParry = 1f;
    [SerializeField] private float groggyWindowDuration = 2.5f;
    [SerializeField] private Color groggyTintColor = new Color(0.3f, 0.8f, 1f);

    [Header("피격 반응 & 리셋")]
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float hitFlashDuration = 0.15f;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float resetPauseDuration = 0.5f;

    [Header("사망 연출")]
    [SerializeField] private float bossMaskRiseDuration = 1.2f;
    [SerializeField] private Color bossParticleColor = new Color(0.25f, 0.5f, 0.9f); // Fiora의 주황과 구분되는 파란색 계열
    [SerializeField] private int bossParticleBurstCount = 30;
    [SerializeField] private float bossDeathShakeDuration = 0.4f;
    [SerializeField] private float bossDeathShakeAmplitude = 0.5f;

    private enum BossState { Normal, ParryWindow, Groggy, HitReaction, Dead }
    private BossState _state = BossState.Normal;
    private float _groggyGauge;
    private int _hitCount;

    protected override void Awake()
    {
        base.Awake();
        if (_meleeHitbox != null) _meleeHitbox.enabled = false;
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
    }

    protected override IEnumerator PatternLoop()
    {
        while (true)
        {
            yield return NormalComboSegment(); // D-07
            if (_isDefeated) yield break;
            yield return ParryTimingSegment(); // D-08
            if (_isDefeated) yield break;

            if (_groggyGauge >= groggyThreshold)
            {
                _state = BossState.Groggy;
                _sr.color = groggyTintColor;
                float g = 0f;
                while (g < groggyWindowDuration)
                {
                    g += Time.unscaledDeltaTime;
                    if (_isDefeated) yield break;
                    yield return null;
                }
                // 그로기 창이 히트 없이 종료 -> 게이지 리셋, 평시 복귀 (D-09 재도전 허용)
                _groggyGauge = 0f;
                _state = BossState.Normal;
                _sr.color = _baseColor;
            }
        }
    }

    private IEnumerator NormalComboSegment()
    {
        _state = BossState.Normal;
        if (_exclamationIcon != null) _exclamationIcon.enabled = true;

        float elapsed = 0f;
        while (elapsed < normalTelegraphDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (_isDefeated) yield break;
            yield return null;
        }

        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
        if (_meleeHitbox != null) _meleeHitbox.enabled = true;
        yield return new WaitForSecondsRealtime(hitboxActiveDuration);
        if (_meleeHitbox != null) _meleeHitbox.enabled = false;
    }

    private IEnumerator ParryTimingSegment()
    {
        _state = BossState.ParryWindow;
        if (_exclamationIcon != null) _exclamationIcon.enabled = true;

        float elapsed = 0f;
        while (elapsed < parryTelegraphDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (_isDefeated) yield break;
            yield return null;
        }

        if (_exclamationIcon != null) _exclamationIcon.enabled = false;

        Vector2 dirToPlayer = _playerTransform != null
            ? ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized
            : Vector2.left;
        if (dirToPlayer.sqrMagnitude < 0.001f) dirToPlayer = Vector2.left;

        var projGO = new GameObject("SamuraiParryProjectile");
        projGO.layer = LayerMask.NameToLayer("Enemy"); // SamuraiParryModule(19-03)이 이 레이어로 조회한다
        projGO.transform.position = transform.position;
        var projSr = projGO.AddComponent<SpriteRenderer>();
        projSr.color = new Color(0.9f, 0.9f, 1f);
        var rb = projGO.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = projGO.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;
        var parryable = projGO.AddComponent<ParryableProjectile>();
        parryable.Init(dirToPlayer, parryProjectileSpeed, parryProjectileMaxDistance, OnParryProjectileParried);

        // 투사체 비행 시간 + 여유 — SamuraiBoss는 결과를 폴링하지 않는다(투사체가 스스로 처리)
        float flightWait = parryProjectileMaxDistance / parryProjectileSpeed + 0.5f;
        float waitElapsed = 0f;
        while (waitElapsed < flightWait)
        {
            waitElapsed += Time.unscaledDeltaTime;
            if (_isDefeated) yield break;
            yield return null;
        }
    }

    /// <summary>D-06: 순수 방어 — 그로기 게이지만 채운다.</summary>
    private void OnParryProjectileParried(Vector2 reflectDirection)
    {
        _groggyGauge += groggyPerParry;
    }

    public override void OnDashHit()
    {
        if (_isDefeated) return;
        if (_patternCoroutine != null) { StopCoroutine(_patternCoroutine); _patternCoroutine = null; }

        if (_state == BossState.Groggy)
        {
            _hitCount++;
            _groggyGauge = 0f;
            if (_hitCount >= RequiredCycles) // D-11: 증가 후 >= 비교 — 정확히 7회째에 처치
            {
                Die(bossMaskRiseDuration, bossParticleColor, bossParticleBurstCount,
                    bossDeathShakeDuration, bossDeathShakeAmplitude, BossId);
                return;
            }
        }
        else
        {
            _groggyGauge += groggyPerHit; // D-09: 평시 타격 성공 -> 게이지만 채움, 처치 카운트 아님 (Pitfall 4 방지)
        }

        _patternCoroutine = StartCoroutine(HitReactionAndReset());
    }

    private IEnumerator HitReactionAndReset()
    {
        _state = BossState.HitReaction;
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
        if (_meleeHitbox != null) _meleeHitbox.enabled = false;

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

        yield return new WaitForSecondsRealtime(resetPauseDuration);

        _patternCoroutine = StartCoroutine(PatternLoop());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_meleeHitbox == null || !_meleeHitbox.enabled) return;
        if (other.CompareTag("Player"))
        {
            PlayerController.TriggerDeath();
        }
    }

    protected override void OnPlayerDiedCleanup()
    {
        if (_exclamationIcon != null) _exclamationIcon.enabled = false;
        if (_meleeHitbox != null) _meleeHitbox.enabled = false;
        _animator?.SetBool("isMoving", false);
    }

    protected override Color GetHighlightColor() => _state == BossState.Groggy ? groggyTintColor : _baseColor;
}
