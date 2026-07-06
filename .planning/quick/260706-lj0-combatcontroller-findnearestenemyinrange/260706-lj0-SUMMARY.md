---
phase: quick
plan: 260706-lj0
subsystem: combat
tags: [physics2d, linecast, targeting, obstacle-check]
key-files:
  modified:
    - Assets/Scripts/Player/CombatController.cs
    - .planning/phases/02-combat-core/02-VERIFICATION.md
decisions:
  - "Obstacle check placed inside FindNearestEnemyInRange()'s candidate loop, not ExecuteDash — filters candidates before 'nearest' is ever selected, so a wall-blocked-nearer + clear-farther scenario naturally falls through to the farther enemy"
  - "_obstacleMask expanded to Default+Ground+Platform (matches existing MeleeEnemy.cs convention); Enemy layer deliberately excluded so enemies standing behind other enemies don't block targeting"
metrics:
  duration: "< 10 min"
  completed: "2026-07-06"
  tasks: 2
  files: 2
---

# Quick 260706-lj0: CombatController FindNearestEnemyInRange Wall-Obstacle Check Summary

Added a `Physics2D.Linecast` obstacle check as the last filter in `FindNearestEnemyInRange()`'s candidate loop, closing the HIGH-priority (Gemini) gap flagged in `02-VERIFICATION.md` where the previously-cached-but-unused `_obstacleMask` never actually blocked dash targeting through walls.

---

## Changes Made

### Task 1: Add Linecast obstacle check to FindNearestEnemyInRange()

**Files:** `Assets/Scripts/Player/CombatController.cs`
**Commit:** `b33f787`

**Edit A — `Awake()` (`_obstacleMask` expansion):**

Replaced:
```csharp
_obstacleMask   = LayerMask.GetMask("Default");
```
with:
```csharp
_obstacleMask   = LayerMask.GetMask("Default", "Ground", "Platform");
```
Matches the existing three-layer convention already used in `Assets/Scripts/Enemy/MeleeEnemy.cs`. `Ground` has no dedicated layer slot in this project's `TagManager.asset` and silently contributes 0 bits — this matches established codebase convention and was left as-is per plan instructions.

**Edit B — `FindNearestEnemyInRange()` candidate loop:**

Inserted after the existing `IsInAttackShape` filter, before the `sqDist`/`bestSqDist` comparison:
```csharp
// Wall/platform block check — physics query, so runs last (most expensive filter).
// Blocked candidates are skipped; the loop keeps scanning remaining candidates so a
// farther-but-unobstructed enemy can still be selected as `nearest`.
if (Physics2D.Linecast(origin, targetPos, _obstacleMask)) continue;
```

The `continue` skips only the blocked candidate — the `for` loop keeps iterating, so a farther unobstructed enemy can still become `nearest`. If every in-range candidate is blocked, `nearest` stays `null` and the existing whiff branch in `DashOrWhiff` fires unchanged. The `OverlapCircle` gathering, `IsAlive` filter, `IsInAttackShape` filter, and the Fan/Linear direction calculation above the loop were not touched.

### Task 2: Mark the "Dash obstacle check removed" gap resolved in 02-VERIFICATION.md

**Files:** `.planning/phases/02-combat-core/02-VERIFICATION.md`
**Commit:** `bdf062b`

Three targeted edits, original gap description text preserved as historical context in all three locations:
- Anti-Patterns table row: Severity `⚠️ Warning` → `✅ RESOLVED`, Impact cell appended with resolution note referencing this quick task.
- Key Link Verification table row: `✗ NOT_WIRED` → `✓ WIRED (resolved 2026-07-06)`, Details cell updated to clarify the check now lives in `FindNearestEnemyInRange()` (not `ExecuteDash`, which receives an already-filtered target).
- Gaps Summary bullet: prepended `**RESOLVED (2026-07-06, quick task 260706-lj0):**` ahead of the original "Dash obstacle/wall check removed" description.

---

## Deviations from Plan

None — plan executed exactly as written.

---

## State Updates

- STATE.md Quick Tasks Completed table: row added for 260706-lj0.

---

## Self-Check: PASSED

- `Assets/Scripts/Player/CombatController.cs` — modified and committed at `b33f787`
- `_obstacleMask` now evaluates to `LayerMask.GetMask("Default", "Ground", "Platform")` in `Awake()`
- `Physics2D.Linecast(origin, targetPos, _obstacleMask)` present as a `continue`-skip inside the `FindNearestEnemyInRange()` candidate loop, positioned after `IsInAttackShape` and before the `sqDist` comparison
- `.planning/phases/02-combat-core/02-VERIFICATION.md` — modified and committed at `bdf062b`; contains 3 occurrences of "260706-lj0" (Anti-Patterns row, Key Link Verification row, Gaps Summary bullet)
