---
phase: "03"
plan: "03-03"
subsystem: "enemy-system"
status: "checkpoint"
checkpoint_task: "03-03-T2"
tags: [enemy, FSM, melee, ENMY-01, C#]
dependency_graph:
  requires: ["03-01 (IEnemy interface)", "03-02 (OnPlayerDeath event, Physics2D matrix)"]
  provides: ["MeleeEnemy.cs — 4-state FSM melee enemy"]
  affects: ["SampleScene (T2/T3 pending human)"]
tech_stack:
  added: []
  patterns:
    - "Pre-allocated Collider2D[4] buffer for OverlapCircle (no GC in Update)"
    - "WaitForSecondsRealtime for all timers (timeScale-immune)"
    - "IsAlive guard after telegraph yield (prevents dead-enemy attack)"
    - "OnEnable/OnDisable subscription guard for static events"
key_files:
  created:
    - "Assets/Scripts/Enemy/MeleeEnemy.cs"
  modified: []
decisions:
  - "OnTriggerEnter2D on MeleeEnemy root checks _meleeHitbox.enabled as a guard; Physics2D matrix (03-02) handles PlayerInvincible immunity"
  - "FindPlayerTransform() kept as a separate helper (not inlined in UpdateChase) to match CombatController pattern"
metrics:
  duration_seconds: 98
  completed_date: "2026-06-09"
  tasks_completed: 1
  tasks_total: 3
  files_created: 1
  files_modified: 0
---

# Phase 03 Plan 03: MeleeEnemy FSM (ENMY-01) Summary

**One-liner:** MeleeEnemy 4-state FSM (patrol/chase/telegraph/attack) implementing IEnemy with 0.8s WaitForSecondsRealtime telegraph and pre-allocated detection buffer.

---

## Status

**CHECKPOINT reached at T2.** T1 (code) is complete and committed. T2 and T3 require Unity Editor interaction by a human developer.

---

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| T1 | Create MeleeEnemy.cs with full FSM | 623910c | Assets/Scripts/Enemy/MeleeEnemy.cs |

---

## Tasks Pending (Human Required)

### T2 — Set up MeleeEnemy GameObject in SampleScene

Must be done in Unity Editor. Steps:

**A. Create MeleeEnemy root GameObject:**
1. Hierarchy → right-click → Create Empty → rename "MeleeEnemy"
2. Add Component: MeleeEnemy (script)
3. Add Component: Rigidbody2D → Body Type: Kinematic, Collision Detection: Continuous, Constraints: Freeze Rotation Z
4. Add Component: CapsuleCollider2D (non-trigger) → Size: ~(0.8, 1.2)
5. Add SpriteRenderer → any visible color (placeholder)
6. Layer: Enemy (layer 10), Tag: Enemy
7. Position on the test floor near player spawn

**B. Create "!" icon child:**
1. Right-click MeleeEnemy → Create Empty → rename "ExclamationIcon"
2. Add SpriteRenderer → any sprite, Color: yellow (1, 1, 0)
3. Position offset: (0, 1.5, 0) above enemy head
4. Sorting Layer: Default, Order in Layer: 10
5. Drag this SpriteRenderer → MeleeEnemy._exclamationIcon Inspector field

**C. Create melee hitbox child:**
1. Right-click MeleeEnemy → Create Empty → rename "MeleeHitbox"
2. Add BoxCollider2D → isTrigger: true, Size: (1.5, 1.0), Offset: (0.75, 0)
3. Layer: Enemy (layer 10)
4. Drag this BoxCollider2D → MeleeEnemy._meleeHitbox Inspector field

**D.** Place MeleeEnemy on test floor surface. File > Save (Ctrl+S).

### T3 — Play Mode Verification (checkpoint:human-verify)

After T2, enter Play Mode and verify ENMY-01 success criteria:
1. Patrol (Idle): enemy bounces left/right within ~3 units of spawn
2. Chase: enemy moves toward player when within ~10 units
3. Telegraph (critical): "!" icon appears for visible 0.8s; rolling during this window prevents damage
4. Melee kill: standing still during telegraph → player is disabled after 0.8s
5. Dash-kill: player dash → MeleeEnemy disappears + FEEL-01 hit-freeze fires
6. Enemy reacts to player death: stops chasing, returns to Idle

Type "approved" if all 6 checks pass.

---

## What Was Built (T1)

`Assets/Scripts/Enemy/MeleeEnemy.cs` — 221 lines implementing:

- `IEnemy` interface: `IsAlive`, `OnDashHit()`, `ClearHighlight()`
- 4-state FSM enum: `Idle`, `Chase`, `Telegraph`, `Attack`
- Patrol via `Rigidbody2D.MovePosition()` — bounces within `patrolHalfRange` of spawn
- Player detection: `Physics2D.OverlapCircle` with pre-allocated `Collider2D[4]` buffer and cached `ContactFilter2D` (PlayerHurtbox layer mask)
- `TelegraphAndAttack()` coroutine: shows `_exclamationIcon`, waits `WaitForSecondsRealtime(0.8f)`, checks `if (!IsAlive) yield break`, then enables `_meleeHitbox` for `hitboxActiveDuration`
- `OnTriggerEnter2D`: fires `PlayerController.OnPlayerDeath?.Invoke()` when `_meleeHitbox.enabled` and other has tag "Player"
- `OnDashHit()`: sets `IsAlive=false`, stops attack coroutine, calls `gameObject.SetActive(false)`
- `OnEnable/OnDisable`: subscribes/unsubscribes `PlayerController.OnPlayerDeath += OnPlayerDied` (prevents stale subscription on domain reload)
- `OnPlayerDied()`: resets enemy to Idle state when player dies (stop chasing dead player)

All layer constants hardcoded (7=PlayerHurtbox, 8=PlayerInvincible) matching TagManager.asset pattern from Plan 01.

---

## Deviations from Plan

None — plan executed exactly as written for T1.

---

## Known Stubs

None in MeleeEnemy.cs. The `_exclamationIcon` and `_meleeHitbox` SerializeFields are null-guarded; they are populated in T2 (Unity Editor step).

---

## Self-Check: PASSED

- [x] `Assets/Scripts/Enemy/MeleeEnemy.cs` exists at worktree path
- [x] Commit `623910c` exists: `feat(03-03): create MeleeEnemy.cs with 4-state FSM`
- [x] All T1 acceptance criteria verified via grep:
  - `public class MeleeEnemy : MonoBehaviour, IEnemy` — line 10
  - `public bool IsAlive { get; private set; } = true;` — line 42
  - `IsAlive = false` + `gameObject.SetActive(false)` — lines 76, 78
  - `if (!IsAlive) yield break` — line 161
  - `WaitForSecondsRealtime` (all timers, no plain `WaitForSeconds(`)
  - `_detectionBuffer` pre-allocated array — line 31
  - `_rb.MovePosition` for movement — lines 120, 146
  - `OnPlayerDeath +=` in OnEnable — line 63
  - `OnPlayerDeath -=` in OnDisable — line 68
