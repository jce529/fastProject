---
phase: 19-samurai-ui
plan: 01
subsystem: combat
tags: [unity, csharp, combat-module, interfaces, input-system]

# Dependency graph
requires:
  - phase: 18-shared-infra
    provides: "IPlayerCombatModule contract + OverclockModule as CombatController's first module implementation"
provides:
  - "IRealtimeCombatModule additive marker interface (CombatController wiring deferred to 19-03)"
  - "IParryable side-channel contract for parryable projectiles"
  - "AimUtil.GetMouseWorldDirection() shared static helper"
  - "CombatContext.SwingRadius/SwingHalfAngleDeg/TapLockout tunables"
  - "TapSwingCombatModuleBase abstract class implementing D-01~D-03 tap-swing logic"
  - "BasicCombatModule concrete no-parry implementation (D-15/D-16)"
affects: [19-02, 19-03, 19-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Additive marker interface (IRealtimeCombatModule) alongside IPlayerCombatModule instead of replacing it"
    - "Side-channel interface (IParryable) instead of extending the closed 3-member IEnemy contract"

key-files:
  created:
    - Assets/Scripts/Player/Combat/IRealtimeCombatModule.cs
    - Assets/Scripts/Player/Combat/IParryable.cs
    - Assets/Scripts/Player/Combat/AimUtil.cs
    - Assets/Scripts/Player/Combat/TapSwingCombatModuleBase.cs
    - Assets/Scripts/Player/Combat/BasicCombatModule.cs
  modified:
    - Assets/Scripts/Player/Combat/CombatContext.cs

key-decisions:
  - "Worktree branch was 170 commits behind main (missing entire Phase 18 Combat module refactor this plan depends on) -- fast-forwarded via `git merge main --ff-only` before editing, matching the established precedent from Phase 15-05"
  - "OverclockModule.cs left completely untouched -- AimUtil.cs duplicates its private GetMouseWorldDirection logic in a new file rather than extracting/refactoring the already-verified Overclock path (INFRA-01 regression avoidance, explicit plan instruction)"
  - "New .cs files paired with minimal 2-line .meta files (fileFormatVersion + guid) matching this repo's existing convention for Claude-authored scripts, since no Unity Editor session is available to auto-generate them"

requirements-completed: [SAMURAI-02]

duration: 10min
completed: 2026-08-07
---

# Phase 19 Plan 01: SAMURAI-02 Realtime Combat Contracts + Basic Combat Module Summary

**IRealtimeCombatModule/IParryable additive interfaces + TapSwingCombatModuleBase shared tap-swing logic (D-01~D-03) + BasicCombatModule (D-15/D-16), all built without touching CombatController.cs or OverclockModule.cs**

## Performance

- **Duration:** 10 min (git commit span; excludes worktree sync investigation)
- **Started:** 2026-08-07T15:38Z (approx, first file read)
- **Completed:** 2026-08-07T15:46Z
- **Tasks:** 2
- **Files modified:** 6 (5 created + 1 modified), plus 5 accompanying `.meta` files

## Accomplishments
- Defined `IRealtimeCombatModule`/`IParryable` additive marker/side-channel interfaces per 19-RESEARCH.md §1's host-hook pattern, without altering `IPlayerCombatModule` or `IEnemy`
- Added `AimUtil.GetMouseWorldDirection()` as an independent public helper mirroring `OverclockModule`'s existing private method — zero diff on `OverclockModule.cs`
- Extended `CombatContext` with `SwingRadius`/`SwingHalfAngleDeg`/`TapLockout` as a pure addition (21 existing fields untouched)
- Implemented `TapSwingCombatModuleBase` (D-01~D-03): tap-triggered in-place directional swing, nearest-enemy one-shot kill within a fan arc, fixed unscaled-time lockout between taps — verified zero uses of `Time.timeScale`/scaled `WaitForSeconds(`
- Implemented `BasicCombatModule` (D-15/D-16) as an empty subclass with no parry override

## Task Commits

Each task was committed atomically:

1. **Task 1: 계약 정의 — IRealtimeCombatModule + IParryable + AimUtil + CombatContext 확장** - `9de25e0` (feat)
2. **Task 2: TapSwingCombatModuleBase + BasicCombatModule 구현** - `39b1d96` (feat)

**Plan metadata:** (this commit)

## Files Created/Modified
- `Assets/Scripts/Player/Combat/IRealtimeCombatModule.cs` - Additive marker interface, single `Tick(CombatContext)` member
- `Assets/Scripts/Player/Combat/IParryable.cs` - `Position` property + `OnParried(Vector2)` side-channel contract
- `Assets/Scripts/Player/Combat/AimUtil.cs` - Static `GetMouseWorldDirection(Vector2, Camera)` helper
- `Assets/Scripts/Player/Combat/CombatContext.cs` - Added `SwingRadius`/`SwingHalfAngleDeg`/`TapLockout` fields
- `Assets/Scripts/Player/Combat/TapSwingCombatModuleBase.cs` - Abstract base implementing D-01~D-03 tap-swing logic, dead `IPlayerCombatModule` stub members for static-type satisfaction
- `Assets/Scripts/Player/Combat/BasicCombatModule.cs` - Empty subclass, no parry override

## Decisions Made
- Fast-forward merged the isolated worktree branch onto `main` (170 commits behind, missing all of Phase 18's Combat module extraction that this plan builds on) before making any edits — same recovery pattern already documented in STATE.md for the 15-05 plan's worktree divergence
- Hand-authored minimal `.meta` files (`fileFormatVersion: 2` + `guid`) for all 5 new scripts, matching the existing repo convention for previously Claude-authored Combat scripts (`IPlayerCombatModule.cs.meta`, `OverclockModule.cs.meta`, `CombatContext.cs.meta` all use the same 2-line format)

## Deviations from Plan

None (beyond the worktree sync, which is an environment/infrastructure fix, not a plan deviation) - plan executed exactly as written. `OverclockModule.cs` diff is empty, `CombatContext.cs` diff is a pure 3-line addition, both confirmed via `git diff`.

## Issues Encountered
- The assigned worktree (`agent-ac8e9d22c6af7b682`) was on a stale branch missing the entire `Assets/Scripts/Player/Combat/` directory (Phase 18's IPlayerCombatModule/OverclockModule/CombatContext refactor) and all Phase 19 planning docs. Resolved via `git merge main --ff-only`, which cleanly fast-forwarded with no conflicts (worktree had no local commits ahead of the merge base other than what main already contained).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `IRealtimeCombatModule`/`IParryable`/`AimUtil`/`TapSwingCombatModuleBase` contracts are now available for 19-02 (unlock UI/registry), 19-03 (CombatController wiring + SamuraiParryModule inheriting `TapSwingCombatModuleBase`), and 19-04 to build on independently
- `CombatController.cs` was not modified by this plan — actual wiring of `BasicCombatModule`/`IRealtimeCombatModule.Tick()` into the controller's Update loop remains 19-03's responsibility
- Manual in-Editor playtest of `BasicCombatModule` (temporarily hardcoded into `CombatController.Awake()`) is deferred to 19-03/19-06 per the plan's acceptance criteria, since this plan does not touch `CombatController.cs`

---
*Phase: 19-samurai-ui*
*Completed: 2026-08-07*

## Self-Check: PASSED

All 5 created source files + SUMMARY.md confirmed present on disk. Both task commits (`9de25e0`, `39b1d96`) confirmed present in `git log`.
