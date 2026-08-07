---
phase: 19-samurai-ui
plan: 04
subsystem: enemy-ai
tags: [boss-fsm, parry, physics-trigger, samurai]

# Dependency graph
requires:
  - phase: 19-01
    provides: "IParryable contract (Assets/Scripts/Player/Combat/IParryable.cs)"
  - phase: 18-shared-infra
    provides: "BossEnemyBase abstract class (Die/OnDashHit/PatternLoop contract)"
provides:
  - "ParryableProjectile — physics-trigger projectile implementing IParryable, delivers death only via OnTriggerEnter2D (roll i-frame safe)"
  - "SamuraiBoss — BossEnemyBase subclass implementing normal-combo/parry-window/groggy 3-stage FSM, 7-cycle kill unlocking BossUnlockManager.Unlock(\"Samurai\")"
affects: [19-05, 19-06]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Procedural GameObject projectile spawn from boss pattern coroutine (Rigidbody2D+CircleCollider2D+SpriteRenderer+ParryableProjectile, no prefab)"

key-files:
  created:
    - Assets/Scripts/Enemy/ParryableProjectile.cs
    - Assets/Scripts/Enemy/Boss/SamuraiBoss.cs
  modified: []

key-decisions:
  - "SamuraiBoss keeps IsAlive=true through Normal/ParryWindow (unlike FioraBoss's dash/vulnerable dichotomy) per 19-CONTEXT.md D-09 — this plan follows CONTEXT.md over 19-RESEARCH.md Pitfall 4's stale premise, as documented in the plan objective"
  - "OnDashHit() branches purely on _state == BossState.Groggy to separate gauge-fill (normal/parry hits) from kill-progress (groggy hits) — Pitfall 4 avoidance"

requirements-completed: [SAMURAI-01, SAMURAI-03, SAMURAI-04, SAMURAI-05]

# Metrics
duration: 25min
completed: 2026-08-07
---

# Phase 19 Plan 04: SAMURAI Boss FSM & Parryable Projectile Summary

**SamuraiBoss FSM (normal combo / parry-timing / groggy) with a physics-trigger ParryableProjectile that lets roll i-frames auto-evade parry-window deaths without any death-check code in SamuraiBoss.**

## Performance

- **Duration:** 25 min
- **Started:** 2026-08-07T06:41:00Z (approx, worktree merge included)
- **Completed:** 2026-08-07T07:04:02Z
- **Tasks:** 2/2 completed
- **Files modified:** 2 created (+2 .meta)

## Accomplishments
- `ParryableProjectile.cs`: physics trigger projectile implementing `IParryable`, single death-delivery path (`OnTriggerEnter2D`), single `OnParried()` callback with `_consumed` re-entry guard on both paths
- `SamuraiBoss.cs`: full `BossEnemyBase` subclass — `PatternLoop()` alternates `NormalComboSegment()` (D-07 telegraph→hitbox) and `ParryTimingSegment()` (D-08 telegraph→procedural projectile spawn), groggy gauge fills from both normal hits and successful parries, groggy-state hits count toward the 7-cycle kill via `Die(..., BossId)`

## Task Commits

Each task was committed atomically:

1. **Task 1: ParryableProjectile — 패링 가능한 물리 투사체** - `c2ac20e` (feat)
2. **Task 2: SamuraiBoss — 평시/패링/그로기 3단계 FSM** - `1f29aa5` (feat)

**Plan metadata:** (this commit, docs: complete plan)

## Files Created/Modified
- `Assets/Scripts/Enemy/ParryableProjectile.cs` - IParryable physics-trigger projectile, death only via OnTriggerEnter2D
- `Assets/Scripts/Enemy/Boss/SamuraiBoss.cs` - Normal/ParryWindow/Groggy FSM, 7-cycle kill, BossUnlockManager.Unlock("Samurai")

## Decisions Made
- Followed the plan's explicit override of 19-RESEARCH.md Pitfall 4's premise: SamuraiBoss keeps `IsAlive = true` through Normal and ParryWindow states (never toggled false/true like FioraBoss's dash-phase targeting gate) — this is intentional per 19-CONTEXT.md D-09's literal reading ("평시에 때릴 때와 패링을 통해서... 그로기 시에 한번씩 공격" implies the boss is always targetable outside groggy windows)
- No chase/patrol logic added — plan explicitly scoped this out (1v1 boss room premise, Claude's Discretion minimization)

## Deviations from Plan

None - plan executed exactly as written. Both files match the plan's provided code blocks with only cosmetic organization (SerializeField grouped under `[Header]` attributes for inspector readability, not present in the plan's literal snippet but not prohibited and doesn't change behavior/values).

## Issues Encountered

Worktree was 1 commit behind main (missing the `fd24e13` merge that brought in Phase 19 Plans 01/02, `BossEnemyBase`, `IParryable`, etc. — all direct dependencies of this plan). Ran `git merge main --ff-only` before any edits, per the parallel-execution protocol; fast-forwarded cleanly with no conflicts.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

`ParryableProjectile.cs` and `SamuraiBoss.cs` provide the code-level SAMURAI FSM. Both files are new — no prefab/scene wiring yet (deferred to 19-05 per plan scope: `SamuraiBoss` needs `_meleeHitbox`/`_exclamationIcon` serialized field assignment via a prefab builder, same convention as `FioraBoss`/`BossEnemyPrefabBuilder.cs`). Actual timing tuning and playtest verification deferred to 19-06 per the plan's stated success criteria.

---
*Phase: 19-samurai-ui*
*Completed: 2026-08-07*

## Self-Check: PASSED

- FOUND: Assets/Scripts/Enemy/ParryableProjectile.cs
- FOUND: Assets/Scripts/Enemy/Boss/SamuraiBoss.cs
- FOUND: c2ac20e
- FOUND: 1f29aa5
