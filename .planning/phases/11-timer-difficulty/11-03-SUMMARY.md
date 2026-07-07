---
phase: 11-timer-difficulty
plan: 03
subsystem: ui
tags: [tmpro, coroutine, hud, timer, unity]

# Dependency graph
requires:
  - phase: 11-timer-difficulty (Plan 01)
    provides: FloorTimer.RemainingSeconds static property
provides:
  - HUDController._timerLabel slot displaying FloorTimer.RemainingSeconds (TIMER-01)
  - TimerFlickerLoop() coroutine — white/red flicker warning that speeds up as time runs out (D-05)
  - Confirmation that _scoreLabel already satisfies SCORE-02 (no code change needed)
affects: [11-04 (editor wiring — will need to assign _timerLabel slot in Inspector)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Variable-interval flicker coroutine: WaitForSecondsRealtime with Mathf.Lerp-computed interval, re-evaluated each cycle (adapted from InvincibilityHandler's fixed-interval flicker)"

key-files:
  created: []
  modified:
    - Assets/Scripts/UI/HUDController.cs

key-decisions:
  - "TimerFlickerLoop runs as an infinite while(true) coroutine for the HUD's full scene lifetime — no manual stop logic, relying on OnDestroy auto-cancellation, per plan instruction"
  - "Above-threshold check forces white color and polls at fixed 0.2s interval to avoid busy-looping every frame while not flickering"

patterns-established:
  - "Timer warning flicker: interval = Lerp(min, max, Clamp01(remaining/threshold)) — reusable for any remaining-time-based visual urgency cue"

requirements-completed: [TIMER-01, SCORE-02]

# Metrics
duration: 6min
completed: 2026-07-07
---

# Phase 11 Plan 03: HUD Timer Display + Flicker Warning Summary

**HUDController now displays FloorTimer.RemainingSeconds as a live integer countdown and flickers white/red with an accelerating interval below 20 seconds remaining (D-05); confirmed SCORE-02 already satisfied by pre-existing `_scoreLabel` code with no changes needed.**

## Performance

- **Duration:** 6 min
- **Started:** 2026-07-07T06:16:00Z
- **Completed:** 2026-07-07T06:22:00Z
- **Tasks:** 2 completed
- **Files modified:** 1

## Accomplishments
- Added `_timerLabel` TextMeshProUGUI slot and wired `Update()` to display `Mathf.CeilToInt(FloorTimer.RemainingSeconds)` (TIMER-01)
- Added `TimerFlickerLoop()` coroutine (D-05): white above `_flickerThreshold` (20s default), toggles white↔red below it with interval shrinking from 0.4s to 0.08s as `RemainingSeconds` approaches 0
- Verified `_scoreLabel?.SetText("{0}", ScoreManager.Score);` line preserved unchanged — SCORE-02 already satisfied, no regression

## Task Commits

Each task was committed atomically:

1. **Task 1: `_timerLabel` slot + Update() display logic (TIMER-01) + SCORE-02 confirmation** - `9e561fa` (feat)
2. **Task 2: TimerFlickerLoop() — inverse-proportional flicker warning (D-05)** - `51df533` (feat)

**Plan metadata:** (recorded in final commit alongside STATE.md/ROADMAP.md updates)

## Files Created/Modified
- `Assets/Scripts/UI/HUDController.cs` - Added `_timerLabel` field, timer display in `Update()`, flicker fields (`_flickerThreshold`, `_minFlickerInterval`, `_maxFlickerInterval`, `_flickerRed`), `StartCoroutine(TimerFlickerLoop())` in `Start()`, and the `TimerFlickerLoop()` coroutine itself

## Decisions Made
None beyond what's captured in frontmatter `key-decisions` — plan's exact code blocks were used verbatim.

## Deviations from Plan

None - plan executed exactly as written.

## Verify Results (grep summary)

**Task 1 verify** (`grep -n "using System.Collections;\|_timerLabel\|FloorTimer.RemainingSeconds\|_scoreLabel?.SetText"`):
```
1:using System.Collections;
13:    [SerializeField] private TextMeshProUGUI _timerLabel;
26:        _timerLabel?.SetText("{0}", Mathf.CeilToInt(FloorTimer.RemainingSeconds)); // TIMER-01
27:        _scoreLabel?.SetText("{0}", ScoreManager.Score); // SCORE-02: 기존 코드 — 변경 없음
```

**Task 2 verify** (`grep -n "private IEnumerator TimerFlickerLoop\|StartCoroutine(TimerFlickerLoop())\|WaitForSecondsRealtime\|Mathf.Lerp(_minFlickerInterval, _maxFlickerInterval"`):
```
29:        StartCoroutine(TimerFlickerLoop());
53:    private IEnumerator TimerFlickerLoop()
59:                yield return new WaitForSecondsRealtime(0.2f);
68:                yield return new WaitForSecondsRealtime(0.2f);
75:            float interval = Mathf.Lerp(_minFlickerInterval, _maxFlickerInterval, Mathf.Clamp01(remaining / _flickerThreshold));
76:            yield return new WaitForSecondsRealtime(interval);
```
No `WaitForSeconds(` (non-realtime) occurrences found — confirms timeScale-immune requirement met.

**Overall plan verification:**
- `_scoreLabel?.SetText` occurs exactly 1 time in the file (SCORE-02 regression check passed)
- `FloorTimer.Tick()` occurs 0 times in the file (HUD stays display-only, per plan's explicit prohibition — death trigger remains WorldGenerator's responsibility in Plan 02)

## SCORE-02 Confirmation

Existing `_scoreLabel?.SetText("{0}", ScoreManager.Score);` line was located, confirmed present and unmodified. SCORE-02 (HUD 실시간 점수 표시) was already satisfied prior to this plan; this plan performed a pure verification pass on that line with no code changes required.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required. Note: `_timerLabel` Inspector slot still needs to be assigned to a TextMeshProUGUI object in the HUD scene — this is Plan 11-04's (editor wiring) responsibility per the phase plan split, not this plan's scope.

## Next Phase Readiness

- `HUDController.cs` compiles cleanly against `FloorTimer.RemainingSeconds` (Plan 01 output), no dependency on Plan 02's `WorldGenerator` integration
- Plan 11-04 (editor wiring) can now assign the `_timerLabel` field in the Unity Inspector
- Plan 11-02 (WorldGenerator integration) remains independent — this plan never calls `FloorTimer.Tick()`

---
*Phase: 11-timer-difficulty*
*Completed: 2026-07-07*

## Self-Check: PASSED

- FOUND: Assets/Scripts/UI/HUDController.cs
- FOUND: .planning/phases/11-timer-difficulty/11-03-SUMMARY.md
- FOUND commit: 9e561fa
- FOUND commit: 51df533
