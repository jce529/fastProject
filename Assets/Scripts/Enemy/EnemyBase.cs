using UnityEngine;

/// <summary>
/// D-05(Phase 16): MeleeEnemy/RangedEnemy가 100% 동일하게 구현하던 최소 공통분모만 추출한
/// 추상 베이스 클래스 — 풀 상속 리팩토링이 아니라 사용자 지시("일단 서로 공유할 최소한의
/// 내용들만 만들어줘")에 따른 최소 범위 추출이다. IEnemy/ISpawnGatable 계약은 변경하지
/// 않는다 — EnemyBase가 두 인터페이스를 구현하고 MeleeEnemy/RangedEnemy가 상속한다.
/// D-08: 사망 확정(IsAlive=false) 직후 각 적이 스스로 isRespawnKill을 판정해 점수를 적립한다
/// (기존에는 CombatController.ExecuteDash()가 대신 수행하던 로직 — 16-02에서 그 호출을 제거함).
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IEnemy, ISpawnGatable
{
    // -- Shared runtime refs (MeleeEnemy/RangedEnemy가 동일하게 선언하던 필드) --------
    protected readonly Collider2D[] _detectionBuffer = new Collider2D[4];
    protected ContactFilter2D _playerFilter;
    protected Transform _playerTransform;
    protected Rigidbody2D _rb;
    protected Animator _animator;

    // -- IEnemy -----------------------------------------------------------------------
    public bool IsAlive { get; protected set; } = true;

    protected virtual void OnEnable()
    {
        PlayerController.OnPlayerDeath += OnPlayerDied;
    }

    protected virtual void OnDisable()
    {
        PlayerController.OnPlayerDeath -= OnPlayerDied;
    }

    /// <summary>
    /// D-05 공통 부분 + D-08(점수 시점 재설계). 순서 고정(재정렬 금지 — 콜라이더 비활성화
    /// 전에 물리 이벤트가 재발생하는 회귀 방지, 16-CONTEXT.md code_context 참고):
    /// 가드 → IsAlive=false → 점수 적립(사망 확정 순간, D-08) → 서브클래스 고유 정지 로직 →
    /// rb 정지 → 콜라이더 비활성화 → animator isDead → EnemyDeathEffect 트리거.
    /// </summary>
    public void OnDashHit()
    {
        if (!IsAlive) return;
        IsAlive = false;

        // D-08: ScoreManager.AddKillScore 호출을 CombatController.ExecuteDash()에서 이 지점으로
        // 이동(16-02에서 호출부 제거 완료). isRespawnKill 판정도 각 적이 스스로 수행한다
        // (기존엔 CombatController가 GetComponent<RespawnedEnemyMarker>()로 대신 판정).
        bool isRespawnKill = GetComponent<RespawnedEnemyMarker>() != null;
        ScoreManager.AddKillScore(isRespawnKill);

        StopEnemySpecificState();

        if (_rb != null) { _rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static; }
        foreach (var c in GetComponents<Collider2D>()) c.enabled = false;
        _animator?.SetBool("isDead", true);

        var deathEffect = GetComponent<EnemyDeathEffect>();
        if (deathEffect == null) deathEffect = gameObject.AddComponent<EnemyDeathEffect>();
        StartCoroutine(deathEffect.PlayDeathSequence(_animator));
    }

    /// <summary>서브클래스 고유 사망 시 정지 로직. MeleeEnemy: _attackCoroutine 정지 +
    /// _meleeHitbox/_exclamationIcon 비활성화. RangedEnemy: _telegraphCoroutine 정지 +
    /// _aimLine 비활성화.</summary>
    protected abstract void StopEnemySpecificState();

    public void ClearHighlight()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }

    // -- ISpawnGatable implementation (SPWN-02) ----------------------------------------

    public void SetSpawnGate(bool isSpawning) => IsAlive = !isSpawning;

    // -- Player death listener ----------------------------------------------------------

    /// <summary>서브클래스가 구현: 플레이어 사망 시 FSM/코루틴/텔레그래프 UI 정지.</summary>
    protected abstract void OnPlayerDied();

    // -- Shared detection helper ----------------------------------------------------

    protected bool IsPlayerInRange(float radius)
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
}
