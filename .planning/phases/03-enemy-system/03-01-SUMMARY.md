---
phase: "03"
plan: "03-01"
subsystem: "enemy-system"
title: "IEnemy Interface + DummyEnemy + CombatController Refactor"
tags: [interface, refactor, enemy, combat]
dependency_graph:
  requires: []
  provides: [IEnemy-interface, CombatController-IEnemy-migration]
  affects: [Assets/Scripts/Enemy/IEnemy.cs, Assets/Scripts/Enemy/DummyEnemy.cs, Assets/Scripts/Player/CombatController.cs]
tech_stack:
  added: []
  patterns: [interface-abstraction, MonoBehaviour-cast-pattern]
key_files:
  created:
    - Assets/Scripts/Enemy/IEnemy.cs
  modified:
    - Assets/Scripts/Enemy/DummyEnemy.cs
    - Assets/Scripts/Player/CombatController.cs
decisions:
  - "IEnemy has exactly 3 members (D-01): IsAlive, OnDashHit(), ClearHighlight() — no GetComponent or transform in interface, accessed via (target as MonoBehaviour) cast"
  - "No namespace on IEnemy — matches existing codebase convention (DummyEnemy, CombatController are all in global namespace)"
  - "SpriteRenderer access in UpdateHighlight uses (nearest as MonoBehaviour)?.GetComponent<SpriteRenderer>() — keeps highlight logic in CombatController without adding Highlight() to IEnemy (prototype scope)"
metrics:
  duration: "~2.5 minutes"
  completed: "2026-06-08T10:43:17Z"
  tasks_completed: 2
  files_modified: 3
---

# Phase 03 Plan 01: IEnemy Interface + DummyEnemy + CombatController Refactor Summary

**One-liner:** IEnemy interface with 3 members defined; DummyEnemy implements it via one-line change; CombatController fully migrated with zero DummyEnemy type references remaining.

---

## What Was Built

### Task 03-01-T1: Create IEnemy interface
Created `Assets/Scripts/Enemy/IEnemy.cs` — a clean C# interface with the three members CombatController already called on DummyEnemy:
- `bool IsAlive { get; }` — liveness gate
- `void OnDashHit();` — kill trigger called by CombatController after arriving
- `void ClearHighlight();` — visual reset called on deselect/exit

No namespace, no Unity-specific base type. All implementors are MonoBehaviours — callers cast via `(target as MonoBehaviour)` when transform/component access is needed.

### Task 03-01-T2: DummyEnemy implements IEnemy + CombatController migration
**DummyEnemy (one line):** Class declaration updated from `MonoBehaviour` to `MonoBehaviour, IEnemy`. All three IEnemy members were already public — zero body changes needed.

**CombatController (5 substitutions + 2 MonoBehaviour casts):**

| Location | Before | After |
|---|---|---|
| Field (line 74) | `private DummyEnemy _lastHighlighted` | `private IEnemy _lastHighlighted` |
| Update() local var (line 147) | `DummyEnemy cachedTarget` | `IEnemy cachedTarget` |
| DashOrWhiff signature (line 201) | `DashOrWhiff(DummyEnemy cachedTarget = null)` | `DashOrWhiff(IEnemy cachedTarget = null)` |
| ExecuteDash signature (line 228) | `ExecuteDash(DummyEnemy target)` | `ExecuteDash(IEnemy target)` |
| FindNearestEnemyInRange return + body | `private DummyEnemy`, `GetComponent<DummyEnemy>()`, `DummyEnemy nearest`, `nearest = dummy` | `private IEnemy`, `GetComponent<IEnemy>()`, `IEnemy nearest`, `nearest = enemy` |
| UpdateHighlight signature (line 372) | `UpdateHighlight(DummyEnemy nearest)` | `UpdateHighlight(IEnemy nearest)` |

Additional casts required by IEnemy having no Unity members:
- `((MonoBehaviour)target).transform.position` in ExecuteDash
- `((MonoBehaviour)target).name` in DashOrWhiff debug log
- `(nearest as MonoBehaviour)?.GetComponent<SpriteRenderer>()` in UpdateHighlight

---

## Commits

| Task | Commit | Description |
|---|---|---|
| 03-01-T1 | `2d44d09` | feat(03-01): create IEnemy interface |
| 03-01-T2 | `49b9115` | feat(03-01): DummyEnemy implements IEnemy + CombatController migration |

---

## Verification Results

```
grep "DummyEnemy" Assets/Scripts/Player/CombatController.cs  => 0 matches (PASS)
grep "IEnemy" Assets/Scripts/Player/CombatController.cs      => 8 matches (PASS)
DummyEnemy class declaration: "public class DummyEnemy : MonoBehaviour, IEnemy" (PASS)
IEnemy.cs: 3 members only, no namespace (PASS)
```

Unity Editor compile and Play Mode verification are manual steps (no CI in this project).

---

## Deviations from Plan

None — plan executed exactly as written.

---

## Known Stubs

None. All changes are type migrations — no placeholder values or data-less components.

---

## Impact

Plans 03-03 (MeleeEnemy) and 03-04 (RangedEnemy) can now implement IEnemy and will automatically work with CombatController's targeting, highlight, and dash-kill pipeline without any further CombatController changes.
