---
phase: 11-timer-difficulty
plan: 01
subsystem: gameplay-systems
tags: [unity, csharp, static-class, timer, score]

# Dependency graph
requires:
  - phase: 04-hud-game-loop
    provides: "FloorManager static class pattern (data-only, no scene lifecycle) — ported directly"
  - phase: 04-hud-game-loop
    provides: "ScoreManager.cs base implementation (Time.unscaledTime timer pattern)"
provides:
  - "FloorTimer static class: Duration=60f, RemainingSeconds, Reset(), Tick() — timeScale-immune floor countdown"
  - "ScoreManager.AddTimeBonus(remainingSeconds) + TimeBonusPerSecond=10 constant"
affects: [11-02-worldgenerator-integration, 11-03-hud-display, 11-04-editor-wiring]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Static data-only class with _expired guard bool to fire an event exactly once (mirrors FloorManager/ScoreManager convention)"

key-files:
  created: [Assets/Scripts/World/FloorTimer.cs]
  modified: [Assets/Scripts/World/ScoreManager.cs]

key-decisions:
  - "FloorTimer.Tick() calls PlayerController.TriggerDeath() directly (no event indirection) — matches plan's key_links contract for Plan 02 to call from WorldGenerator.Update()"
  - "AddTimeBonus uses Mathf.RoundToInt(remainingSeconds) * TimeBonusPerSecond exactly per D-02 formula"

patterns-established:
  - "Phase 11 static API contracts (FloorTimer, ScoreManager.AddTimeBonus) defined before any call sites — Plan 02/03 consume without further code archaeology"

requirements-completed: [TIMER-01, TIMER-02, SCORE-01]

# Metrics
duration: 3min
completed: 2026-07-07
---

# Phase 11 Plan 01: FloorTimer & ScoreManager Time Bonus Summary

**FloorTimer static class (60s fixed countdown, Time.unscaledTime-based, single-fire death trigger) plus ScoreManager.AddTimeBonus(remainingSeconds) scoring 10 points per remaining second.**

## Performance

- **Duration:** 3 min
- **Started:** 2026-07-07T06:11:59Z
- **Completed:** 2026-07-07T06:14:28Z
- **Tasks:** 2 completed
- **Files modified:** 2 (1 created, 1 modified)

## Accomplishments
- `FloorTimer.cs` created as a new static class following the `FloorManager`/`ScoreManager` data-only convention — no MonoBehaviour, no scene lifecycle
- `RemainingSeconds` computed from `Time.unscaledTime`, making the timer immune to slow-motion `Time.timeScale` changes (D-07)
- `Tick()` guards with a private `_expired` bool so `PlayerController.TriggerDeath()` fires exactly once when the timer reaches zero (D-06, TIMER-02)
- `ScoreManager.AddTimeBonus(remainingSeconds)` added alongside a new `TimeBonusPerSecond = 10` constant, implementing the D-02 formula without touching any existing kill/clear-bonus logic

## Task Commits

Each task was committed atomically:

1. **Task 1: FloorTimer.cs — 정적 타이머 클래스** - `d3c6d6a` (feat)
2. **Task 2: ScoreManager.cs — AddTimeBonus(remainingSeconds) 추가** - `2b143eb` (feat)

**Plan metadata:** (pending — see Final Commit below)

## Files Created/Modified
- `Assets/Scripts/World/FloorTimer.cs` - New static class: `Duration` (60f), `RemainingSeconds` (Time.unscaledTime-based), `Reset()`, `Tick()`
- `Assets/Scripts/World/ScoreManager.cs` - Added `TimeBonusPerSecond` constant and `AddTimeBonus(float remainingSeconds)` method; all pre-existing members (`KillScore`, `FastClearBonus`, `NormalClearBonus`, `SlowClearBonus`, `AddKillScore`, `StartRoomTimer`, `AddRoomClearBonus`, `Reset`) untouched

## Verify Results (grep summary)

**Task 1** — `Assets/Scripts/World/FloorTimer.cs`:
```
8:public static class FloorTimer
10:    public const float Duration = 60f;
16:    public static void Reset()
23:    public static float RemainingSeconds => Mathf.Max(0f, Duration - (Time.unscaledTime - _floorStartTime));
29:    public static void Tick()
35:            PlayerController.TriggerDeath();
```
All acceptance criteria matched: class declared, `Duration = 60f`, `Reset()` sets `_floorStartTime`/`_expired`, `RemainingSeconds` uses `Time.unscaledTime` (no `Time.deltaTime`), `Tick()` has the `_expired` guard and calls `PlayerController.TriggerDeath()`.

**Task 2** — `Assets/Scripts/World/ScoreManager.cs`:
```
17:    public const int   TimeBonusPerSecond = 10;
42:    public static void AddTimeBonus(float remainingSeconds)
44:        Score += Mathf.RoundToInt(remainingSeconds) * TimeBonusPerSecond;
```
Existing symbols (`KillScore`, `FastClearBonus`, `NormalClearBonus`, `SlowClearBonus`, `AddKillScore`, `AddRoomClearBonus`, `StartRoomTimer`, `Reset`) all confirmed still present and unmodified.

## Static API Reference for Plan 02/03

```csharp
// FloorTimer.cs
public static class FloorTimer
{
    public const float Duration; // 60f
    public static void Reset();
    public static float RemainingSeconds { get; } // Time.unscaledTime-based
    public static void Tick(); // fires PlayerController.TriggerDeath() once at 0
}

// ScoreManager.cs (addition)
public const int TimeBonusPerSecond; // 10
public static void AddTimeBonus(float remainingSeconds);
```

Plan 02 (WorldGenerator integration) calls `FloorTimer.Reset()` on floor entry and `FloorTimer.Tick()` every `Update()`, and calls `ScoreManager.AddTimeBonus(FloorTimer.RemainingSeconds)` at `FloorTransitionSequence()` start (D-02b). Plan 03 (HUD) reads `FloorTimer.RemainingSeconds` for display.

## Decisions Made
None beyond what's captured in plan frontmatter (D-01, D-02, D-06, D-07, D-08) — implementation followed the plan's provided code verbatim.

## Deviations from Plan

None - plan executed exactly as written.

One operational note (not a plan deviation): the assigned git worktree (`worktree-agent-af8d52df8f3b40eb1`) was stale (HEAD at `9b91afc`, missing the Phase 11 planning commits). Fast-forwarded the worktree branch to `main`@`2bbf010` (a clean ancestor fast-forward, no conflicts, no divergent work lost) before executing tasks, so that `11-01-PLAN.md` and current `ScoreManager.cs`/`PlayerController.cs` state were available.

## Issues Encountered
None beyond the worktree staleness noted above, resolved via fast-forward merge.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `FloorTimer` and `ScoreManager.AddTimeBonus` are compilable, unused-by-design (no call sites yet) static APIs ready for Plan 02 (WorldGenerator integration) and Plan 03 (HUD display) to consume directly.
- No stubs, no placeholder UI — this plan only adds backend data-contract code with zero gameplay-visible effect, as specified in its objective.

---
*Phase: 11-timer-difficulty*
*Completed: 2026-07-07*

## Self-Check: PASSED

- FOUND: Assets/Scripts/World/FloorTimer.cs
- FOUND: Assets/Scripts/World/ScoreManager.cs
- FOUND: .planning/phases/11-timer-difficulty/11-01-SUMMARY.md
- FOUND commit: d3c6d6a (Task 1)
- FOUND commit: 2b143eb (Task 2)
