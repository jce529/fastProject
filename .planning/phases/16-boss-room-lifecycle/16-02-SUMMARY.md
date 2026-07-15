---
phase: 16-boss-room-lifecycle
plan: 02
subsystem: combat
tags: [refactor, dead-code-cleanup, unity, csharp]

# Dependency graph
requires: []
provides:
  - "GetMouseWorldDirection() shared helper in CombatController.cs (replaces duplicated Linear/Fan mouse->world direction code)"
  - "CombatController.cs free of leftover Debug.Log noise in DashOrWhiff()/ExecuteDash()"
  - "CombatController.ExecuteDash() no longer calls ScoreManager.AddKillScore() or checks RespawnedEnemyMarker — CombatController-side half of D-08 complete"
affects: [16-03]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Shared private helper methods over branch-duplicated logic (GetMouseWorldDirection)"

key-files:
  created: []
  modified:
    - Assets/Scripts/Player/CombatController.cs

key-decisions:
  - "Fan branch's near-origin fallback (Vector2.right when dir.sqrMagnitude <= 0.001f) now applies uniformly to the Linear branch too, since both funnel through the same GetMouseWorldDirection() helper — no observed behavior change in normal play, only a defensive edge-case improvement."
  - "Plan's suggested D-08 comment text literally contained the string 'ScoreManager.AddKillScore()', which would have failed the plan's own acceptance criteria (that string must not exist in the file). Reworded the comment to describe the same intent without the literal API name."

requirements-completed: [D-06, D-07, D-08]

# Metrics
duration: 12min
completed: 2026-07-15
---

# Phase 16 Plan 02: CombatController Refactor Batch Summary

**Consolidated the duplicated Linear/Fan mouse-direction calculation into a single `GetMouseWorldDirection()` helper, stripped 12 leftover debug `Debug.Log` calls from the dash coroutine chain, and removed the `ScoreManager.AddKillScore()`/`RespawnedEnemyMarker` scoring logic from `ExecuteDash()` ahead of its relocation to each enemy's `OnDashHit()` in plan 16-03.**

## Performance

- **Duration:** 12 min
- **Tasks:** 2/2 completed
- **Files modified:** 1

## Accomplishments
- D-06: `FindNearestEnemyInRange()`'s Linear and Fan branches now share one `GetMouseWorldDirection(Vector2 origin)` helper instead of two near-identical copy-pasted blocks
- D-07: `DashOrWhiff()`/`ExecuteDash()` debug console noise (12 `Debug.Log` calls) removed; the null-guard `Debug.LogError` and the two out-of-scope logs in `Update()`/`ExecuteWhiff()` were left untouched per plan scope
- D-08: `ExecuteDash()` no longer computes `isRespawnKill` or calls `ScoreManager.AddKillScore()` — the CombatController-side half of the score-timing redesign is complete; the remaining half (each enemy's self-judged `OnDashHit()` scoring) is deferred to plan 16-03

## Task Commits

Each task was committed atomically:

1. **Task 1: FindNearestEnemyInRange() mouse-direction helper consolidation (D-06)** - `bd8ecd4` (refactor)
2. **Task 2: DashOrWhiff()/ExecuteDash() debug log cleanup + score call removal (D-07, D-08)** - `72a1119` (refactor)

**Plan metadata:** (this commit) `docs(16-02): complete CombatController refactor batch plan`

## Files Created/Modified
- `Assets/Scripts/Player/CombatController.cs` - Added `GetMouseWorldDirection()` helper; removed 12 debug logs; removed `ScoreManager.AddKillScore()`/`RespawnedEnemyMarker` check from `ExecuteDash()`

## Decisions Made
- Reworded the D-08 cleanup comment to avoid literally embedding the `ScoreManager.AddKillScore()` API name, since the plan's own acceptance criteria required that exact string to no longer exist anywhere in the file (see Deviations below).
- Kept the plan-specified unused `dirToTarget` local variable in `ExecuteDash()` untouched — the plan's own final code listing retains it (its only prior use, a `Debug.Log`, was removed by D-07), and per CLAUDE.md's "surgical changes" rule only orphans caused by *this* change should be cleaned up; since the plan explicitly re-lists it as-is in the target end state, it was left in place rather than second-guessing the plan.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Plan's suggested replacement comment would have failed its own acceptance criteria**
- **Found during:** Task 2 (D-08 score call removal)
- **Issue:** The plan's suggested replacement comment text for the "Kill and effects" section literally contained the substring `ScoreManager.AddKillScore()`, but the plan's own verification command (`! grep -n "RespawnedEnemyMarker\|ScoreManager.AddKillScore" ...`) and acceptance criteria required that string to be completely absent from the file. Using the plan's literal suggested text would have made the task fail its own verification.
- **Fix:** Reworded the comment to convey the same intent ("scoring moved to each enemy's OnDashHit(), no longer called directly here") without using the literal `ScoreManager.AddKillScore()` name.
- **Files modified:** `Assets/Scripts/Player/CombatController.cs`
- **Verification:** Re-ran the plan's verify command after the reword — passed (`PASS` output, zero matches for the forbidden strings)
- **Committed in:** `72a1119` (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (1 bug — self-contradictory plan text)
**Impact on plan:** Minor wording fix only; no behavior or scope change. All D-06/D-07/D-08 acceptance criteria satisfied exactly as specified.

## Issues Encountered
None beyond the deviation documented above.

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- D-06 and D-07 are fully complete.
- D-08's CombatController-side half (call removal) is complete. Plan 16-03 (Wave 2) must implement the remaining half — each enemy's own `OnDashHit()` performing self-judged respawn-kill scoring via `ScoreManager` — for D-08 to be fully complete.
- No blockers for 16-03; `CombatController.cs` changes in this plan do not touch `MeleeEnemy.cs`/`RangedEnemy.cs`, so no merge conflicts expected.

---
*Phase: 16-boss-room-lifecycle*
*Completed: 2026-07-15*

## Self-Check: PASSED

- FOUND: Assets/Scripts/Player/CombatController.cs (modified)
- FOUND: .planning/phases/16-boss-room-lifecycle/16-02-SUMMARY.md
- FOUND: commit bd8ecd4 (Task 1)
- FOUND: commit 72a1119 (Task 2)
