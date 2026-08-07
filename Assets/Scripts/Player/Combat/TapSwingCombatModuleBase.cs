using System.Collections;
using UnityEngine;

/// <summary>
/// D-01~D-03: 기본전투모듈/사무라이 전투형 모듈이 공유하는 탭 스윙 로직.
/// IRealtimeCombatModule.Tick()에서 매 프레임 락아웃을 감소시키고, 탭(AttackHeld 1프레임)
/// 시 ResolveSwing()을 실행한다. 슬로우모션/게이지/HitFreeze를 절대 건드리지 않는다(SAMURAI-02).
/// </summary>
public abstract class TapSwingCombatModuleBase : IPlayerCombatModule, IRealtimeCombatModule
{
    private float _lockoutRemaining;

    public void Tick(CombatContext ctx)
    {
        if (_lockoutRemaining > 0f)
            _lockoutRemaining -= Time.unscaledDeltaTime; // D-03: 슬로우모션 무관 — 항상 실시간

        var input = InputManager.Instance;
        if (input.AttackHeld && _lockoutRemaining <= 0f)
        {
            ResolveSwing(ctx);
            _lockoutRemaining = ctx.TapLockout;
        }
    }

    /// <summary>D-01: 제자리 방향성 스윙(마우스 방향) — D-02: 부채꼴 안 가장 가까운 IEnemy 원샷킬.
    /// SamuraiParryModule(19-03)이 이 메서드를 오버라이드해 패링 판정을 먼저 검사하고,
    /// 패링 대상이 없으면 base.ResolveSwing(ctx)로 폴백한다.</summary>
    protected virtual void ResolveSwing(CombatContext ctx)
    {
        Vector2 origin = ctx.Rb.position;
        Vector2 dir = AimUtil.GetMouseWorldDirection(origin, ctx.MainCamera);
        ctx.SpriteRenderer.flipX = dir.x < 0f;

        int count = Physics2D.OverlapCircle(origin, ctx.SwingRadius, ctx.EnemyFilter, ctx.HitBuffer);
        IEnemy nearest = null;
        float bestSqDist = float.MaxValue;
        float cosHalf = Mathf.Cos(ctx.SwingHalfAngleDeg * Mathf.Deg2Rad);

        for (int i = 0; i < count; i++)
        {
            var enemy = ctx.HitBuffer[i].GetComponent<IEnemy>();
            if (enemy == null || !enemy.IsAlive) continue;

            Vector2 targetPos = (Vector2)ctx.HitBuffer[i].transform.position;
            Vector2 toTarget = targetPos - origin;
            float dist = toTarget.magnitude;
            if (dist > ctx.SwingRadius) continue;
            if (Vector2.Dot(dir, toTarget.normalized) < cosHalf) continue;

            float sqDist = dist * dist;
            if (sqDist < bestSqDist) { bestSqDist = sqDist; nearest = enemy; }
        }

        if (nearest != null)
        {
            nearest.OnDashHit(); // D-02: 원샷킬, 모든 적(MeleeEnemy/RangedEnemy/보스) 공통
            AudioManager.PlaySfx(Sfx.Slash); // 기존 SFX 자산 재사용 — 전용 SFX 없음(Claude's Discretion)
        }
    }

    // IPlayerCombatModule — CombatController의 IRealtimeCombatModule 조기 반환(19-03에서 배선)으로
    // 인해 실행 도달 불가능한 dead stub. _activeModule 필드의 정적 타입을 만족시키기 위해서만 존재한다.
    public IEnemy FindTarget(Vector2 origin, CombatContext ctx) => null;
    public IEnumerator Resolve(IEnemy target, CombatContext ctx) { yield break; }
    public IEnumerator Whiff(CombatContext ctx) { yield break; }
}
