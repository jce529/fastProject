---
phase: "03"
plan: "03-04"
subsystem: "enemy-system"
tags: ["ranged-enemy", "projectile", "fsm", "line-renderer", "telegraph"]
dependency_graph:
  requires: ["03-01", "03-02"]
  provides: ["ENMY-02-code"]
  affects: ["CombatController (IEnemy target)", "PlayerController (OnPlayerDeath subscriber)"]
tech_stack:
  added: []
  patterns:
    - "LineRenderer alpha fade (0→1) via Time.unscaledDeltaTime — timeScale-immune telegraph"
    - "Physics2D.OverlapCircle with ContactFilter2D + pre-allocated buffer — no GC in Update"
    - "Coroutine-owned states (Telegraph/Attack) with IsAlive guard after wait"
    - "Init(direction) pattern: called immediately after Instantiate, sets linearVelocity before first FixedUpdate"
key_files:
  created:
    - "Assets/Scripts/Enemy/ProjectileController.cs"
    - "Assets/Scripts/Enemy/RangedEnemy.cs"
  modified: []
decisions:
  - "Physics2D.OverlapCircle (non-NonAlloc variant) used in RangedEnemy — ContactFilter2D overload requires this variant; pre-allocated buffer still prevents GC"
  - "OnPlayerDied resets _state to Idle but does not null _playerTransform — transform remains valid for re-detection on next play"
  - "TelegraphAndFire: yield return null (frame-by-frame) with Time.unscaledDeltaTime accumulation — matches RangeDisplay pattern, timeScale-immune"
metrics:
  duration: "~8 minutes"
  completed_date: "2026-06-09T13:51:18Z"
  tasks_completed: 2
  tasks_total: 4
  files_created: 2
  files_modified: 0
---

# Phase 03 Plan 04: RangedEnemy FSM + ProjectileController (ENMY-02) Summary

**One-liner:** RangedEnemy FSM with LineRenderer aim telegraph (0→1 alpha / 0.8s real-time) + ProjectileController straight-line projectile with distance lifetime and player/platform kill logic.

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| T1 | Create ProjectileController.cs | 64c7fa3 | Assets/Scripts/Enemy/ProjectileController.cs |
| T2 | Create RangedEnemy.cs | 1967761 | Assets/Scripts/Enemy/RangedEnemy.cs |

## Pending Tasks (Require Unity Editor)

| Task | Name | Reason |
|------|------|--------|
| T3 | Create Projectile prefab + set up RangedEnemy in SampleScene | Requires Unity Editor — Hierarchy, Inspector, prefab drag-drop operations |
| T4 | checkpoint:human-verify — Play Mode ENMY-02 checks | Requires human to enter Play Mode and verify 8 behavioral checks |

## What Was Built

### ProjectileController.cs
Rigidbody2D-driven straight-line projectile for RangedEnemy.

Key behaviors:
- `Init(Vector2 direction)`: called immediately after `Instantiate()`, sets `_rb.linearVelocity = direction.normalized * speed` (Unity 6 API — `linearVelocity`, not `velocity`)
- `FixedUpdate()`: distance-based self-destruct via `sqrMagnitude >= maxDistance * maxDistance` (no sqrt overhead)
- `OnTriggerEnter2D`: platform contact (layer 9 const) destroys projectile; `CompareTag("Player")` fires `PlayerController.OnPlayerDeath?.Invoke()` then destroys
- PlayerInvincible layer immunity fully delegated to Physics2D matrix (Plan 03-02, D-16) — no code check needed

Required Inspector configuration (T3 human task):
- Rigidbody2D: Dynamic, Gravity=0, Continuous, Interpolate, Freeze Rotation Z
- CircleCollider2D: radius=0.15, isTrigger=true
- Layer: EnemyProjectile (11)

### RangedEnemy.cs
Full FSM enemy with LineRenderer telegraph and IEnemy interface.

FSM states: Idle → Chase → Telegraph (coroutine) → Attack (coroutine) → Idle

Key behaviors:
- `moveSpeed = 0f` default (D-10): stationary at start, Inspector-adjustable
- Detection via `Physics2D.OverlapCircle` with pre-allocated `_detectionBuffer[4]` and cached `ContactFilter2D` — no GC per frame
- `TelegraphAndFire()` coroutine:
  - Locks aim direction at start (player can dodge after seeing line)
  - Fades LineRenderer alpha 0→1 over 0.8s using `Time.unscaledDeltaTime` (timeScale-immune)
  - `if (!IsAlive) yield break` guard after 0.8s wait (prevents fire after dash-kill during telegraph)
  - Calls `FireProjectile(aimDir, origin)` then returns state to Idle
- `OnDashHit()`: sets `IsAlive = false`, stops telegraph coroutine, hides aim line, `SetActive(false)`
- `OnEnable`/`OnDisable`: subscribe/unsubscribe `PlayerController.OnPlayerDeath` (prevents stale subscription on domain reload, per D-15 pattern)
- `OnPlayerDied()`: stops coroutine, hides aim line, resets state to Idle

## Deviations from Plan

None — both scripts implemented exactly per plan specification.

## Known Stubs

**T3 Inspector references on RangedEnemy are NOT yet assigned** (pending human Unity Editor step):
- `projectilePrefab` field: null → `Debug.LogWarning` guard present in `FireProjectile()`, prevents null ref crash
- `firePoint` field: null → null-conditional fallback to `transform.position` in `TelegraphAndFire()` and `FireProjectile()`

These are intentional pending stubs — T3 resolves them. The game will log a warning but not crash if T3 is skipped.

## Checkpoint Status

**STOPPED AT T3** — Unity Editor interaction required.

T3 requires the human developer to:
1. Create a Projectile prefab with Rigidbody2D/CircleCollider2D/ProjectileController/SpriteRenderer
2. Place a RangedEnemy GameObject in SampleScene with all components
3. Create a FirePoint child Transform at offset (0.5, 0, 0)
4. Assign projectilePrefab and firePoint Inspector references
5. Save scene

After T3, T4 (Play Mode verification) can proceed per the 8 behavioral checks in the plan.

## Self-Check: PASSED

Files verified:
- FOUND: Assets/Scripts/Enemy/ProjectileController.cs (commit 64c7fa3)
- FOUND: Assets/Scripts/Enemy/RangedEnemy.cs (commit 1967761)
