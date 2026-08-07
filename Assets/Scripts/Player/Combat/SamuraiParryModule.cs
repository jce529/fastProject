using UnityEngine;

/// <summary>
/// D-04~D-06: 사무라이 전투형 모듈(패링 포함). TapSwingCombatModuleBase의 스윙을 그대로 쓰되,
/// 스윙 범위 안에 IParryable(투사체)이 있으면 먼저 패링을 시도한다 — 있으면 패링(순수 방어,
/// 처치 카운트 없음, D-06), 없으면 base.ResolveSwing()으로 폴백해 일반 원샷킬을 그대로 수행한다(D-02).
/// 투사체 레이어는 "Enemy"(SamuraiBoss.cs가 생성 시 설정, 19-04) — CombatController의 기존
/// ctx.EnemyFilter와 같은 레이어를 재사용하되 이 클래스 전용 ContactFilter2D/버퍼로 별도 조회한다.
/// </summary>
public class SamuraiParryModule : TapSwingCombatModuleBase
{
    private readonly Collider2D[] _parryBuffer = new Collider2D[4];
    private ContactFilter2D _parryFilter;
    private bool _filterInitialized;

    private void EnsureFilter()
    {
        if (_filterInitialized) return;
        _parryFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
        _parryFilter.useTriggers = true;
        _filterInitialized = true;
    }

    protected override void ResolveSwing(CombatContext ctx)
    {
        EnsureFilter();
        Vector2 origin = ctx.Rb.position;
        Vector2 dir = AimUtil.GetMouseWorldDirection(origin, ctx.MainCamera);
        float cosHalf = Mathf.Cos(ctx.SwingHalfAngleDeg * Mathf.Deg2Rad);

        int count = Physics2D.OverlapCircle(origin, ctx.SwingRadius, _parryFilter, _parryBuffer);
        for (int i = 0; i < count; i++)
        {
            var parryable = _parryBuffer[i].GetComponent<IParryable>();
            if (parryable == null) continue;

            Vector2 toTarget = parryable.Position - origin;
            float dist = toTarget.magnitude;
            if (dist > ctx.SwingRadius) continue;
            if (Vector2.Dot(dir, toTarget.normalized) < cosHalf) continue;

            // D-04: 타이밍(탭 시점) + 방향(스윙 부채꼴) 동시 충족 -> 패링 성공
            parryable.OnParried(dir); // D-06: 순수 방어 — 보스 피격 처리 호출 없음
            ctx.SpriteRenderer.flipX = dir.x < 0f;
            AudioManager.PlaySfx(Sfx.Slash); // 전용 SFX 없음(Claude's Discretion) — 기존 자산 재사용
            return; // 패링이 이 탭을 전부 소비 — 동시에 다른 적을 베지 않는다
        }

        base.ResolveSwing(ctx); // 패링 대상 없음 -> 일반 스윙(D-02, 원샷킬)으로 폴백
    }
}
