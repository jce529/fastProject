using System.Collections;
using UnityEngine;

/// <summary>
/// Phase 18(INFRA-03): BossEnemy.cs(Phase 15, F.I.O.R.A 전용 구현)에서 보스 범용 plumbing만 추출한
/// 추상 베이스 클래스. EnemyBase(Phase 16)를 상속하지 않는 독립 형제 클래스다 — 보스의 IsAlive는
/// "생존 여부"가 아니라 "현재 타겟 가능(빈틈) 여부"로 오버로드되어 EnemyBase의 생존/사망 시맨틱과
/// 구조적으로 다르기 때문이다(15-02 설계 그대로 유지). Phase 19-23(SAMURAI/DeadEye/MAX/NOVA)가
/// 이 클래스를 상속해 defeat-guard/사망시퀀스/스폰게이팅/피격하이라이트를 재작성 없이 사용한다.
/// </summary>
public abstract class BossEnemyBase : MonoBehaviour, IEnemy, ISpawnGatable
{
    protected Rigidbody2D _rb;
    protected Animator _animator;
    protected SpriteRenderer _sr;
    protected Transform _playerTransform;
    protected CameraFollow _cameraFollow;
    protected Coroutine _patternCoroutine;
    protected Color _baseColor;
    protected bool _isDefeated;

    public bool IsAlive { get; protected set; } = true;

    protected virtual void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _sr = GetComponent<SpriteRenderer>();
        _baseColor = _sr.color;
        var player = FindFirstObjectByType<PlayerController>();
        _playerTransform = player != null ? player.transform : null;
        var cam = Camera.main;
        _cameraFollow = cam != null ? cam.GetComponent<CameraFollow>() : null;
    }

    protected virtual void Start()
    {
        if (IsAlive && _patternCoroutine == null && !_isDefeated)
            _patternCoroutine = StartCoroutine(PatternLoop());
    }

    protected virtual void OnEnable() { PlayerController.OnPlayerDeath += OnPlayerDied; }
    protected virtual void OnDisable() { PlayerController.OnPlayerDeath -= OnPlayerDied; }

    public void SetSpawnGate(bool isSpawning)
    {
        IsAlive = !isSpawning;
        if (!isSpawning && _patternCoroutine == null && !_isDefeated)
            _patternCoroutine = StartCoroutine(PatternLoop());
    }

    private void OnPlayerDied()
    {
        if (_patternCoroutine != null) { StopCoroutine(_patternCoroutine); _patternCoroutine = null; }
        OnPlayerDiedCleanup();
    }

    public void ClearHighlight()
    {
        if (_sr == null) return;
        _sr.color = GetHighlightColor();
    }

    protected abstract IEnumerator PatternLoop();
    protected abstract void OnPlayerDiedCleanup();
    protected abstract Color GetHighlightColor();

    protected virtual void Die(float maskRiseDuration, Color particleColor, int particleBurstCount,
        float shakeDuration, float shakeAmplitude, string bossId)
    {
        _isDefeated = true;
        IsAlive = false;
        if (_patternCoroutine != null) { StopCoroutine(_patternCoroutine); _patternCoroutine = null; }
        if (_rb != null) { _rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static; }
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
        _animator?.SetBool("isDead", true);

        var deathEffect = GetComponent<EnemyDeathEffect>();
        if (deathEffect == null) deathEffect = gameObject.AddComponent<EnemyDeathEffect>();
        deathEffect.ConfigureIntensity(maskRiseDuration, particleColor, particleBurstCount);
        StartCoroutine(deathEffect.PlayDeathSequence(_animator));

        _cameraFollow?.Shake(shakeDuration, shakeAmplitude);
        ScoreManager.AddBossKillScore();
        BossUnlockManager.Unlock(bossId);
    }

    public abstract void OnDashHit();
}
