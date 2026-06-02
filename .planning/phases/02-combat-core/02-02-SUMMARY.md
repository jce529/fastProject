---
phase: 02-combat-core
plan: "02-02"
subsystem: player, combat
tags: [unity, combat, slow-motion, dash, hit-freeze, gauge, whiff]

requires:
  - phase: 02-combat-core
    plan: "02-01"
    provides: DummyEnemy (IsAlive, OnDashHit, ClearHighlight), AttackTypeSelector (IsSelecting, Selected), Enemy layer 10

provides:
  - GaugeController.cs: Value [0,1], IsEmpty, SetDraining(bool), AddKillBonus() — drain/regen with unscaledDeltaTime
  - CombatController.cs: EnterSlowMotion, ExitSlowMotion, DashOrWhiff coroutine, ExecuteDash, ExecuteWhiff, HitFreeze, FindNearestEnemyInRange
  - RangeDisplay.cs stub: Show()/Hide() no-ops — replaced by plan 02-03

affects:
  - 02-03 (RangeDisplay stub provided — 02-03 replaces with full visual implementation)
  - 02-04 (RollController reads IsAttackDown; CombatController does not interfere with roll)

tech-stack:
  added: []
  patterns:
    - "GaugeController drain/regen via Time.unscaledDeltaTime — immune to slow-motion timeScale"
    - "EnterSlowMotion IsSelecting guard — prevents combat input during UI overlay"
    - "maxSlowMoDuration safety timeout — prevents stuck slow-mo on dropped input events"
    - "Physics2D.Linecast obstacle check before MovePosition loop — blocked path → whiff"
    - "_isBusy = true before first yield in DashOrWhiff — re-entrance lockout (Pitfall 4)"
    - "HitFreeze zeroes both timeScale and fixedDeltaTime; uses WaitForSecondsRealtime"
    - "OverlapCircleNonAlloc with pre-allocated Collider2D[16] — no per-frame GC"
    - "LayerMask.GetMask('Enemy') by name — immune to layer index deviations (02-01: index 10)"

key-files:
  created:
    - Assets/Scripts/Player/GaugeController.cs
    - Assets/Scripts/Player/CombatController.cs
    - Assets/Scripts/Player/RangeDisplay.cs  # stub only — plan 02-03 replaces this
  modified: []

key-decisions:
  - "RangeDisplay stub created in Player/ alongside CombatController — Rule 3 auto-fix for missing type reference that would prevent compilation before 02-03 executes"
  - "ExitSlowMotion called as first statement in ExecuteDash before any yield — ensures MovePosition runs at timeScale=1 (Pitfall 1)"
  - "HitFreeze zeroes fixedDeltaTime alongside timeScale — forgetting fixedDeltaTime stops physics permanently (Pitfall 5)"
  - "_obstacleMask = LayerMask.GetMask('Default') — prototype-sufficient obstacle detection; full layer matrix can be refined in tuning"

requirements-completed: [ATCK-02, ATCK-03, ATCK-04, ATCK-05, FEEL-01]

duration: 3min
completed: 2026-06-02
---

# Phase 02 Plan 02: CombatController + GaugeController + HitFreeze Summary

**Complete hold-to-aim / release-to-dash combat loop: slow-motion with IsSelecting guard and 5s safety timeout, nearest-enemy detection via OverlapCircleNonAlloc, MovePosition dash over 3 FixedUpdate frames with Linecast obstacle check, 75ms HitFreeze, whiff branch with 0.5s lockout, and gauge drain/regen via unscaledDeltaTime**

## Performance

- **Duration:** ~3 min
- **Started:** 2026-06-02T03:40:33Z
- **Completed:** 2026-06-02T03:43:24Z
- **Tasks:** 2
- **Files modified:** 4 (2 scripts + 2 stubs/meta)

## Accomplishments

- **GaugeController.cs**: Drain at 0.25/s, regen at 0.15/s, +0.20 kill bonus — all via `Time.unscaledDeltaTime` (immune to slow-motion)
- **CombatController.cs**: Complete state machine — slow-mo lifecycle, dash/whiff coroutine chain, HitFreeze, `_isBusy` lockout, `FindNearestEnemyInRange` with pre-allocated buffer
- Three review fixes fully incorporated: IsSelecting race condition guard, maxSlowMoDuration=5f safety timeout, Physics2D.Linecast obstacle check before dash
- **RangeDisplay.cs stub**: No-op Show()/Hide() allows CombatController to compile before plan 02-03 ships the visual implementation

## Task Commits

Each task was committed atomically:

1. **Task 1: GaugeController drain/regen/kill-bonus** — `a1c5f96` (feat)
2. **Task 2: CombatController + RangeDisplay stub** — `ba650fe` (feat)

## Files Created/Modified

- `Assets/Scripts/Player/GaugeController.cs` — Value, IsEmpty, SetDraining, AddKillBonus; unscaledDeltaTime
- `Assets/Scripts/Player/CombatController.cs` — Full combat state machine; all Gemini review fixes applied
- `Assets/Scripts/Player/RangeDisplay.cs` — Stub (no-op Show/Hide); plan 02-03 replaces with full visual

## Decisions Made

- **RangeDisplay stub (Rule 3):** CombatController references `RangeDisplay` type in `GetComponentInChildren<RangeDisplay>()`. Without a class declaration the script does not compile. Created a minimal no-op stub in the same directory so plan 02-02 can commit a compilable state. Plan 02-03 will replace this stub with the full circle/arc renderer.
- **ExitSlowMotion first in ExecuteDash:** Called before any `yield return` so the MovePosition loop runs at `timeScale=1`. If still in slow-mo, 3 FixedUpdate frames would take 15 real frames — the dash would feel broken.
- **_obstacleMask = LayerMask.GetMask("Default"):** The default Unity layer contains floor tiles and walls. A single linecast against Default is sufficient for prototype obstacle detection.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] RangeDisplay stub created to enable compilation**
- **Found during:** Task 2 (CombatController references RangeDisplay type before 02-03 creates it)
- **Issue:** `CombatController.Start()` calls `GetComponentInChildren<RangeDisplay>()` and `EnterSlowMotion()`/`ExitSlowMotion()` use `_rangeDisplay?.Show()` / `_rangeDisplay?.Hide()`. Without a `RangeDisplay` class definition, Unity cannot compile the project.
- **Fix:** Created `Assets/Scripts/Player/RangeDisplay.cs` with two empty public methods (`Show()`, `Hide()`). Null-conditional calls in CombatController are safe whether the component is absent or present.
- **Files modified:** Assets/Scripts/Player/RangeDisplay.cs (new)
- **Commit:** ba650fe (Task 2 commit)
- **Impact:** No behavior change. The stub is intentionally empty; plan 02-03 will provide the real visual renderer.

---

**Total deviations:** 1 auto-fixed (Rule 3 — blocking: missing type reference)
**Impact on plan:** Stub is required for compilation before 02-03. No scope creep — plan explicitly says CombatController finds RangeDisplay via GetComponentInChildren, implying its existence.

## Known Stubs

- `Assets/Scripts/Player/RangeDisplay.cs` lines 12-13: `Show()` and `Hide()` are empty. These are intentional stubs — plan 02-03 will replace this file with the full OverlapCircle/arc visual renderer. CombatController's null-conditional calls (`_rangeDisplay?.Show()`) are safe if no RangeDisplay component is attached to the Player child hierarchy.

## Self-Check: PASSED

- FOUND: Assets/Scripts/Player/GaugeController.cs
- FOUND: Assets/Scripts/Player/CombatController.cs
- FOUND: Assets/Scripts/Player/RangeDisplay.cs
- FOUND commit: a1c5f96 feat(02-02): add GaugeController
- FOUND commit: ba650fe feat(02-02): add CombatController

---
*Phase: 02-combat-core*
*Completed: 2026-06-02*
