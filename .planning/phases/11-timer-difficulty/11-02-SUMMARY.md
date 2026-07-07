---
phase: 11-timer-difficulty
plan: 02
subsystem: gameplay
tags: [unity, c#, worldgenerator, enemyspawner, floortimer, scoremanager, difficulty-scaling]

# Dependency graph
requires:
  - phase: 11-01
    provides: FloorTimer static class (Reset/RemainingSeconds/Tick) and ScoreManager.AddTimeBonus(float)
provides:
  - "WorldGenerator now spawns enemies at every Room instantiation point (start/lookahead/lookbehind/standby) using a floor-number-based difficulty table"
  - "WorldGenerator drives the 60s floor timer lifecycle: reset on game start, reset on floor transition, tick every frame"
  - "WorldGenerator converts remaining floor time into score at portal-entry time"
affects: [11-03, 11-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "TrySpawnEnemies(room, floor) called immediately after every Room Instantiate — mirrors TrySpawnExitPortal's 'call right after Instantiate' convention"
    - "Difficulty table ported verbatim from FloorSpawner.GetEnemyCount(floor) into WorldGenerator.GetEnemyCount(floor) — no new design, same thresholds/ranges"

key-files:
  created: []
  modified:
    - Assets/Scripts/World/EnemySpawner.cs
    - Assets/Scripts/World/WorldGenerator.cs

key-decisions:
  - "Standby room's difficulty is computed from FloorManager.CurrentFloor + 1 (the floor it will actually be activated on), not the floor it was instantiated on"
  - "ScoreManager.AddTimeBonus(FloorTimer.RemainingSeconds) is called before FloorTimer.Reset() within FloorTransitionSequence() to capture the previous floor's remaining time before it resets (D-02b ordering)"

patterns-established:
  - "EnemySpawner marker count vs difficulty-table count mismatch is handled silently (spawn only as many as markers exist) — no error thrown"

requirements-completed: [TIMER-01, TIMER-02, DIFF-01, SCORE-01]

# Metrics
duration: 6min
completed: 2026-07-07
---

# Phase 11 Plan 02: Timer & Difficulty Integration Summary

**Wired FloorTimer/ScoreManager (Plan 01 output) and EnemySpawner (dormant since Phase 3) into WorldGenerator's room-generation and floor-transition loops — WorldGenerator.cs now enforces the 60s countdown, floor-based enemy difficulty scaling, and time-bonus scoring end-to-end.**

## Performance

- **Duration:** 6 min (git commit timestamps: 15:17:03 → 15:23:07 KST)
- **Started:** 2026-07-07T06:17:03Z
- **Completed:** 2026-07-07T06:23:07Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- DIFF-01: Every Room instantiation point (start room, `SpawnNextPair()`, `SpawnPrevPair()`, EXIT portal standby room) now immediately spawns and activates enemies via a floor-number-based tiered difficulty table
- TIMER-01/TIMER-02: `FloorTimer.Reset()` fires on game start and every floor transition; `FloorTimer.Tick()` runs unconditionally as the first statement of every `Update()` frame, guaranteeing expiry death detection even when the room chain is empty
- SCORE-01: Remaining floor time is converted to score via `ScoreManager.AddTimeBonus()` at the exact moment of portal entry, before the timer resets

## Task Commits

Each task was committed atomically:

1. **Task 1: DIFF-01 — EnemySpawner.Type getter + WorldGenerator difficulty spawn logic** - `d3d02dc` (feat)
2. **Task 2: TIMER-01/TIMER-02/SCORE-01 — FloorTimer reset/tick + ScoreManager time bonus wiring** - `f122e25` (feat)

_Note: no TDD tasks in this plan — both were plain `type="auto"` integration tasks._

## Files Created/Modified
- `Assets/Scripts/World/EnemySpawner.cs` - Added `public EnemyType Type => _type;` getter so WorldGenerator can filter spawners by type
- `Assets/Scripts/World/WorldGenerator.cs` - Added `_meleeEnemyPrefab`/`_rangedEnemyPrefab` Inspector fields, `GetEnemyCount(floor)` (ported from `FloorSpawner.cs` lines 208-217, unchanged), `TrySpawnEnemies(room, floor)`, 4 call sites (`Start()`, `SpawnNextPair()`, `SpawnPrevPair()`, `TrySpawnExitPortal()`'s standby room), `FloorTimer.Reset()` in `Start()` and `FloorTransitionSequence()`, `FloorTimer.Tick()` as first line of `Update()`, `ScoreManager.AddTimeBonus(FloorTimer.RemainingSeconds)` at the top of `FloorTransitionSequence()`

## Decisions Made
None beyond what the plan specified — followed plan exactly, including the explicit ordering constraints (AddTimeBonus before Reset; Tick before the chain-empty early return).

## Deviations from Plan

**Pre-execution environment fix (not a plan deviation, but required before any edits):** This worktree (`worktree-agent-abd9da7c38d7af152`) was 32 commits behind `main` with zero unique commits of its own — it predated all of Phase 10 and Plan 11-01, so `FloorTimer.cs` did not exist and `WorldGenerator.cs` still used the old `List`-based chain (no `TrySpawnExitPortal`/`FloorTransitionSequence` in their Phase-10-complete form). Fast-forwarded the worktree branch to `main` (`git merge --ff-only main`, safe since there were no unique worktree commits) before starting Task 1. This is an infrastructure/environment correction, not a code deviation — no plan logic was altered.

None - plan's code changes executed exactly as written; the WorldGenerator.cs on `main` already matched the `LinkedList`-based structure described in the "read this instead of the plan's embedded excerpt" note, so no further adaptation beyond fast-forwarding was necessary.

## Issues Encountered
- Worktree staleness (see above) — resolved via fast-forward merge, not a code issue.

## Next Phase Readiness
- WorldGenerator.cs now fully satisfies TIMER-01, TIMER-02, DIFF-01, and SCORE-01 at the code level
- `_meleeEnemyPrefab`/`_rangedEnemyPrefab` Inspector fields on `WorldGenerator` still need prefab assignment in the Unity Editor scene before this is playable end-to-end (Editor wiring is out of scope for this plan per 11-CONTEXT.md — expected to land in a later plan in this phase, e.g. 11-04)
- No blockers for Plan 11-03/11-04

---
*Phase: 11-timer-difficulty*
*Completed: 2026-07-07*

## Self-Check: PASSED

- FOUND: Assets/Scripts/World/EnemySpawner.cs
- FOUND: Assets/Scripts/World/WorldGenerator.cs
- FOUND: .planning/phases/11-timer-difficulty/11-02-SUMMARY.md
- FOUND: d3d02dc (Task 1 commit)
- FOUND: f122e25 (Task 2 commit)
