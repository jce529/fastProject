using System.Collections;
using UnityEngine;

/// <summary>
/// F.I.O.R.A(Overclock) 전투 로직 — Phase 18 마이그레이션으로 CombatController에서
/// IPlayerCombatModule 뒤로 이관된 최초(이자 이번 Phase에서는 유일한) 구현체.
/// verbatim move — 로직 변경 없음(D-04, 18-CONTEXT.md).
/// </summary>
public class OverclockModule : IPlayerCombatModule
{
    public IEnemy FindTarget(Vector2 origin, CombatContext ctx)
    {
        // D-06(Phase 16): Linear/Fan 분기가 거의 동일했던 마우스→월드 방향 계산을 헬퍼로 통합.
        Vector2 attackDir = GetMouseWorldDirection(origin, ctx);
        float currentMaxDist = (AttackTypeSelector.Selected == AttackType.Linear) ? ctx.SearchRadius : ctx.FanRadius;

        // Pre-allocated buffer — no GC (ROADMAP Stack Constraint)
        int count = Physics2D.OverlapCircle(origin, ctx.SearchRadius, ctx.EnemyFilter, ctx.HitBuffer);

        IEnemy nearest    = null;
        float  bestSqDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var enemy = ctx.HitBuffer[i].GetComponent<IEnemy>();
            // Skip dead enemies — physics broadphase may lag behind collider.enabled=false (Pitfall 6)
            if (enemy == null || !enemy.IsAlive) continue;

            Vector2 targetPos = (Vector2)ctx.HitBuffer[i].transform.position;
            Vector2 toTarget = targetPos - origin;
            float dist = toTarget.magnitude;

            // Shape and distance filter: checks specific arc/beam and radius
            if (!IsInAttackShape(toTarget / dist, dist, attackDir, currentMaxDist, ctx)) continue;

            // Wall/platform block check — physics query, so runs last (most expensive filter).
            // Blocked candidates are skipped; the loop keeps scanning remaining candidates so a
            // farther-but-unobstructed enemy can still be selected as `nearest`.
            if (Physics2D.Linecast(origin, targetPos, ctx.ObstacleMask)) continue;

            // SqrMagnitude avoids sqrt — sufficient for closest-enemy comparison
            float sqDist = dist * dist;
            if (sqDist < bestSqDist)
            {
                bestSqDist = sqDist;
                nearest    = enemy;
            }
        }

        return nearest;
    }

    public IEnumerator Resolve(IEnemy target, CombatContext ctx)
    {
        if (target == null)
        {
            Debug.LogError("[Combat] ExecuteDash: TARGET IS NULL at start! Aborting.");
            yield break;
        }

        Vector2 startPos    = ctx.Rb.position;
        Vector2 destination = (Vector2)((MonoBehaviour)target).transform.position;
        Vector2 dirToTarget = (destination - startPos).normalized;

        // 3. 대상 방향으로 스프라이트 전환
        ctx.SpriteRenderer.flipX = destination.x < startPos.x;

        // 4. Setup visual, animation, invincibility
        ctx.Animator?.SetBool("IsDashing", true);
        ctx.Invincibility.StartInvincibility(ctx.DashDuration + 0.05f);
        if (ctx.TrailRenderer != null) ctx.TrailRenderer.emitting = true;
        ctx.Rb.linearVelocity = Vector2.zero;

        // 5. 대시 이동 (smoothstep 보간으로 가속-감속 느낌)
        float elapsed = 0f;
        while (elapsed < ctx.DashDuration)
        {
            float t = elapsed / ctx.DashDuration;
            float smooth = t * t * (3f - 2f * t); // smoothstep
            ctx.Rb.MovePosition(Vector2.Lerp(startPos, destination, smooth));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        ctx.Rb.MovePosition(destination);

        // 6. Cleanup visual and animation
        ctx.Animator?.SetBool("IsDashing", false);
        if (ctx.TrailRenderer != null) ctx.TrailRenderer.emitting = false;

        // 6. Kill and effects — D-08(Phase 16): 점수 적립 호출은 각 적의 OnDashHit()(EnemyBase 공통 부분,
        // 16-03에서 구현)로 이동했다 — 여기서는 더 이상 점수 적립 API를 직접 호출하지 않는다.
        target.OnDashHit();
        AudioManager.PlaySfx(Sfx.Slash); // SFX-03/D-05: 처치 확정 순간 슬래시 — HitFreeze 이전 호출, DSP는 timeScale=0 중에도 계속 재생
        SpawnHitSpark(destination, ctx);
        ctx.CameraFollow?.Shake(ctx.CameraShakeDuration, ctx.CameraShakeAmplitude);
        yield return HitFreeze(ctx.HitFreezeDuration);

        ctx.SetAttackCooldown(ctx.PostKillLockout);
        ctx.Gauge.AddKillBonus();
    }

    public IEnumerator Whiff(CombatContext ctx)
    {
        Debug.Log("[Combat] Executing Whiff (Penalty)");
        ctx.Animator?.SetTrigger("Whiff");

        // Longer lockout than kill — ATCK-04: whiff penalty must be clearly longer
        yield return new WaitForSecondsRealtime(ctx.WhiffLockout);
    }

    private IEnumerator HitFreeze(float realSeconds)
    {
        // FEEL-01: world freeze. Both timeScale AND fixedDeltaTime must be zeroed.
        Time.timeScale      = 0f;
        Time.fixedDeltaTime = 0f;
        // WaitForSecondsRealtime is mandatory — WaitForSeconds never resumes when timeScale=0 (Pitfall 2)
        yield return new WaitForSecondsRealtime(realSeconds);
        // Restore both — forgetting fixedDeltaTime causes physics to stop permanently (Pitfall 5)
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    /// <summary>D-07: 처치 위치에 히트 스파크 이펙트를 재생한다.</summary>
    private void SpawnHitSpark(Vector2 position, CombatContext ctx)
    {
        if (ctx.HitSparkPrefab == null) return;
        Object.Instantiate(ctx.HitSparkPrefab, position, Quaternion.identity);
    }

    /// <summary>D-06(Phase 16): FindNearestEnemyInRange()의 Linear/Fan 두 분기가 거의 동일하게
    /// 복붙하던 마우스→월드 방향 계산을 통합. 마우스가 origin과 거의 겹치는 예외 상황
    /// (sqrMagnitude 낮음)에는 Vector2.right로 폴백 — 기존 Fan 분기에만 있던 안전장치를
    /// Linear 분기에도 동일하게 적용한다(정상 플레이 동작 변경 없음, 극단적 edge case만 보강).</summary>
    private Vector2 GetMouseWorldDirection(Vector2 origin, CombatContext ctx)
    {
        UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
        Vector2 mousePos = mouse != null ? mouse.position.ReadValue() : (Vector2)ctx.MainCamera.WorldToScreenPoint(origin);
        Vector3 mouseWorld = ctx.MainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(ctx.MainCamera.transform.position.z)));
        Vector2 dir = (Vector2)mouseWorld - origin;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
    }

    /// <summary>
    /// Filter enemies by attack shape and distance.
    /// Linear mode: checks narrow beam towards aim.
    /// Fan mode: checks forward-facing arc and fan radius.
    /// </summary>
    private bool IsInAttackShape(Vector2 normalizedToTarget, float distance, Vector2 attackDir, float maxDistance, CombatContext ctx)
    {
        if (distance > maxDistance) return false;

        float dot = Vector2.Dot(attackDir, normalizedToTarget);
        float thresholdAngle = (AttackTypeSelector.Selected == AttackType.Linear) ? ctx.LinearHalfAngleDeg : ctx.FanHalfAngleDeg;
        float cosHalf = Mathf.Cos(thresholdAngle * Mathf.Deg2Rad);

        return dot >= cosHalf;
    }
}
