---
phase: 15-fsm
plan: 01
subsystem: combat
tags: [score-system, death-effect, boss-prep, additive-api]

# Dependency graph
requires: []
provides:
  - "ScoreManager.AddBossKillScore() and SubtractScore(int) static methods"
  - "EnemyDeathEffect.ConfigureIntensity(maskRiseDuration, particleColor, particleBurstCount) additive method"
affects: [15-02-boss-enemy, 15-03, 15-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Additive-only API extension: new static methods/fields added without touching any existing call sites (diff 0 verified via git diff on MeleeEnemy.cs/RangedEnemy.cs)"
    - "Post-AddComponent configure-then-run: ConfigureIntensity() must be called after AddComponent<EnemyDeathEffect>() and before StartCoroutine(PlayDeathSequence(...)), mirroring existing MeleeEnemy/RangedEnemy call convention"

key-files:
  created: []
  modified:
    - Assets/Scripts/World/ScoreManager.cs
    - Assets/Scripts/Enemy/EnemyDeathEffect.cs

key-decisions:
  - "BossKillScore = 750 (D-09): meaningfully larger than KillScore(100), within the 500-1000 recommended range"
  - "SubtractScore(int) is a generic amount-based decrement, not boss-specific by name, so BossEnemy.OnDashHit() can offset CombatController's unconditional AddKillScore(false) call for non-lethal hits (D-12)"
  - "_particleBurstCount default kept at 12 (matching prior hardcoded value) so MeleeEnemy/RangedEnemy death VFX is byte-for-byte unchanged when ConfigureIntensity() is never called"

patterns-established:
  - "Interface-first prep plans: build the API surface a not-yet-existing consumer (BossEnemy.cs in 15-02) will call, verified via grep-based acceptance criteria rather than integration tests"

requirements-completed: [BOSS-04, BOSS-06]

# Metrics
duration: 12min
completed: 2026-07-15
---

# Phase 15 Plan 01: Boss Score & Death Effect Utilities Summary

**Added ScoreManager.AddBossKillScore()/SubtractScore(int) and EnemyDeathEffect.ConfigureIntensity() as pure-additive APIs for the not-yet-built BossEnemy.cs (15-02) to consume.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-07-15T16:34:00+09:00 (approx, following prior 16-03 commit)
- **Completed:** 2026-07-15T16:46:30+09:00
- **Tasks:** 2 completed
- **Files modified:** 2

## Accomplishments
- ScoreManager now exposes `BossKillScore` const (750) plus `AddBossKillScore()` and `SubtractScore(int)` static methods for boss kill scoring and non-lethal hit self-offset (D-09/D-12)
- EnemyDeathEffect gained a `_particleBurstCount` field (default 12) and a `ConfigureIntensity()` method letting a boss-specific death sequence override mask rise duration, particle color, and burst count after `AddComponent` — without touching existing MeleeEnemy/RangedEnemy call sites (D-08)

## Task Commits

Each task was committed atomically:

1. **Task 1: ScoreManager 보스 점수 유틸 추가 (BOSS-06, D-09, D-12)** - `7245d3c` (feat)
2. **Task 2: EnemyDeathEffect 보스 전용 강도 설정 메서드 추가 (D-08)** - `a856c36` (feat)

**Plan metadata:** (this commit)

## Files Created/Modified
- `Assets/Scripts/World/ScoreManager.cs` - Added `BossKillScore` const, `AddBossKillScore()`, `SubtractScore(int)` static methods
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` - Added `_particleBurstCount` field, `ConfigureIntensity()` method, replaced hardcoded burst count `12` with field reference

## Decisions Made
- BossKillScore fixed at 750 per D-09 recommendation range (500-1000)
- SubtractScore kept generic (amount parameter) rather than boss-specific naming, to stay a general utility while serving D-12's specific offset need
- Default _particleBurstCount = 12 preserves exact existing MeleeEnemy/RangedEnemy death VFX behavior when ConfigureIntensity() is not called

## Deviations from Plan

None - plan executed exactly as written. Both tasks matched the plan's action blocks verbatim; acceptance criteria (const/method existence, diff 0 on MeleeEnemy.cs/RangedEnemy.cs) verified via grep and git diff before each commit.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- 15-02 (BossEnemy.cs) can now compile against `ScoreManager.AddBossKillScore()`, `ScoreManager.SubtractScore(int)`, and `EnemyDeathEffect.ConfigureIntensity(float, Color, int)` immediately — no scavenger hunt for missing APIs.
- MeleeEnemy/RangedEnemy score and death VFX behavior is unaffected (verified diff 0).
- No blockers for 15-02.

---
*Phase: 15-fsm*
*Completed: 2026-07-15*

## Self-Check: PASSED

- FOUND: Assets/Scripts/World/ScoreManager.cs
- FOUND: Assets/Scripts/Enemy/EnemyDeathEffect.cs
- FOUND: .planning/phases/15-fsm/15-01-SUMMARY.md
- FOUND: commit 7245d3c
- FOUND: commit a856c36
