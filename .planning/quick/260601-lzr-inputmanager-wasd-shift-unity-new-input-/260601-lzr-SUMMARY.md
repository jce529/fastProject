---
phase: quick-260601-lzr
plan: 01
type: quick
tags: [input, singleton, facade, phase2-prep]
dependency_graph:
  requires: [Assets/InputSystem_Actions.inputactions]
  provides: [Assets/Scripts/Player/InputManager.cs]
  affects: [Phase 2 slow-mo, roll, dash systems]
tech_stack:
  added: []
  patterns: [singleton MonoBehaviour, named delegate unsubscription, LateUpdate flag reset]
key_files:
  created:
    - Assets/Scripts/Player/InputManager.cs
  modified: []
decisions:
  - "Named private methods for callbacks instead of lambdas — enables clean OnDisable unsubscription"
  - "LateUpdate clears flags — guarantees consumers running in Update/FixedUpdate see correct one-frame values"
  - "No DontDestroyOnLoad — prototype is single-scene, keeping it simple"
  - "Jump not added to InputManager — PlayerController already owns jump and refactoring is out of scope"
metrics:
  duration: ~5min
  completed: "2026-06-01"
  tasks_completed: 1
  files_created: 1
---

# Quick 260601-lzr: InputManager Singleton Facade

**One-liner:** Singleton input facade over Unity New Input System — exposes MoveInput (WASD), RollPressed (LeftShift), AttackHeld/AttackReleased/IsAttackDown (LMB) as clean read-only properties for Phase 2 consumers.

## What Was Built

`Assets/Scripts/Player/InputManager.cs` — a `MonoBehaviour` singleton that:

- Requires `PlayerInput` on the same GameObject via `[RequireComponent]`
- Caches `Player/Move`, `Player/Sprint`, `Player/Attack` actions in `OnEnable`
- Uses named private delegate methods (`OnSprintPerformed`, `OnAttackPerformed`, `OnAttackCanceled`) so `OnDisable` can cleanly unsubscribe — no lambda capture leak
- Resets one-frame boolean flags in `LateUpdate` so any consumer running in `Update` or `FixedUpdate` sees the correct value for exactly one frame
- Exposes five public properties: `MoveInput`, `RollPressed`, `AttackHeld`, `AttackReleased`, `IsAttackDown`

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create InputManager singleton | 1462a1e | Assets/Scripts/Player/InputManager.cs |

## Verification

- File exists at `Assets/Scripts/Player/InputManager.cs`
- `[RequireComponent(typeof(PlayerInput))]` confirmed on line 10
- All five properties present: `MoveInput` (line 78), `RollPressed` (line 81), `AttackHeld` (line 84), `AttackReleased` (line 87), `IsAttackDown` (line 90)
- `PlayerController.cs` is NOT modified
- Unity compile-time verification: requires opening in Unity Editor (no automated C# compile step available in CLI for Unity scripts)

## Deviations from Plan

None — plan executed exactly as written.

## Known Stubs

None. `InputManager` reads live data from `PlayerInput` actions — no hardcoded/empty values flow to any consumer.

## Self-Check: PASSED

- `Assets/Scripts/Player/InputManager.cs` — FOUND (created in this task)
- Commit `1462a1e` — FOUND (`feat(quick-260601-lzr-01): add InputManager singleton input facade`)
