---
phase: 03-enemy-system
plan: "03-02"
subsystem: player
tags: [unity, c#, events, physics2d, layer-matrix, fall-detection]

# Dependency graph
requires:
  - phase: 03-01-enemy-system
    provides: IEnemy interface and DummyEnemy implementing it — CombatController targets IEnemy

provides:
  - PlayerController.OnPlayerDeath static event (public static event Action)
  - PlayerDeathHandler MonoBehaviour — subscribes/unsubscribes in OnEnable/OnDisable, SetActive(false) on death
  - FallDetector rewritten — OnFall() is single line invoking OnPlayerDeath (D-17, no teleport recovery)
  - EnemyProjectile layer at index 11 in TagManager.asset
  - Physics2D collision matrix — Enemy and EnemyProjectile do not collide with PlayerInvincible

affects:
  - 03-03 MeleeEnemy (melee hitbox targets Player layer; must NOT fire when player is rolling)
  - 03-04 RangedEnemy + ProjectileController (EnemyProjectile layer configured and collision matrix ready)
  - 04 HUD & Game Loop (Phase 4 UIManager subscribes to OnPlayerDeath alongside PlayerDeathHandler)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Static event subscription: OnEnable += / OnDisable -= to prevent stale subscriptions across Play Mode restarts"
    - "Death notification via static event Action — subscribers never need to modify the event source"

key-files:
  created:
    - Assets/Scripts/Player/PlayerDeathHandler.cs
  modified:
    - Assets/Scripts/Player/PlayerController.cs (using System + OnPlayerDeath event added)
    - Assets/Scripts/Player/FallDetector.cs (Phase 1 teleport recovery removed; OnFall = one line)
    - ProjectSettings/TagManager.asset (EnemyProjectile added at layer 11)
    - ProjectSettings/Physics2DSettings.asset (collision matrix configured)

key-decisions:
  - "Static event Action for OnPlayerDeath — Phase 4 UIManager subscribes alongside PlayerDeathHandler without any modification to Phase 3 code (D-15)"
  - "FallDetector no longer stores last-safe-position — Phase 3 semantics are fall = death, no recovery (D-17)"
  - "Physics2D matrix via Editor checkpoint — four pairs disabled: Enemy/PlayerInvincible, EnemyProjectile/PlayerInvincible, EnemyProjectile/Enemy, EnemyProjectile/EnemyProjectile"

patterns-established:
  - "Static event lifecycle: always unsubscribe in OnDisable — static events persist across Play Mode restarts when domain reload is disabled"

requirements-completed: [ENMY-01, ENMY-02]

# Metrics
duration: ~20min (split over two sessions with checkpoint)
completed: 2026-06-08
---

# Phase 03 Plan 02: PlayerDeath Event + FallDetector Rewrite + EnemyProjectile Layer Summary

**OnPlayerDeath static event wired through PlayerController, FallDetector rewritten to instant-death (D-17), EnemyProjectile layer 11 added, and Physics2D collision matrix configured for invincibility correctness**

## Performance

- **Duration:** ~20 min (includes checkpoint for Editor collision matrix configuration)
- **Started:** 2026-06-08
- **Completed:** 2026-06-08
- **Tasks:** 3 (T1: event + handler, T2: FallDetector rewrite + layer, T3: Physics2D matrix checkpoint)
- **Files modified:** 5

## Accomplishments

- Added `public static event Action OnPlayerDeath` to PlayerController with `using System;` — single insertion point for all death triggers across Phase 3 and 4
- Created PlayerDeathHandler with correct OnEnable/OnDisable subscription guard pattern — disables Player GameObject on death
- Rewrote FallDetector from Phase 1's teleport-recovery logic to one-line instant death: `PlayerController.OnPlayerDeath?.Invoke()`
- Added EnemyProjectile layer at index 11 in TagManager.asset
- Configured Physics2D collision matrix: Enemy x PlayerInvincible, EnemyProjectile x PlayerInvincible, EnemyProjectile x Enemy, EnemyProjectile x EnemyProjectile — all four pairs disabled

## Task Commits

Each task was committed atomically:

1. **T1: OnPlayerDeath event + PlayerDeathHandler** - `79e78c5` (feat)
2. **T2: FallDetector rewrite + EnemyProjectile layer** - `bbf5d99` (feat)
3. **T3: Physics2D collision matrix** - `e09ce5b` (feat)

## Files Created/Modified

- `Assets/Scripts/Player/PlayerController.cs` - Added `using System;` and `public static event Action OnPlayerDeath;`
- `Assets/Scripts/Player/PlayerDeathHandler.cs` - New file: subscribes OnPlayerDeath, SetActive(false) + Debug.Log on death
- `Assets/Scripts/Player/FallDetector.cs` - Rewritten: all Phase 1 recovery fields/methods removed, OnFall() is single-line event invoke
- `ProjectSettings/TagManager.asset` - EnemyProjectile added at layer index 11
- `ProjectSettings/Physics2DSettings.asset` - Four collision pairs disabled via Editor Physics 2D project settings

## Decisions Made

- **Static event vs instance event:** Static event chosen so Phase 4 UIManager can subscribe without any reference to the Player GameObject. Subscribers handle their own lifecycle via OnEnable/OnDisable (D-15).
- **FallDetector complete rewrite:** Phase 1 last-safe-position tracking (Vector3 field, FixedUpdate, RequireComponent attributes) entirely removed per D-17 decision. Phase 3 death semantics have no recovery path.
- **Physics2D matrix checkpoint:** Editor interaction required — cannot configure Physics2DSettings.asset by text editing alone without risking bit-field corruption in the collision mask YAML. Checkpoint was the correct approach.

## Deviations from Plan

None - plan executed exactly as written. T3 was a checkpoint by design; user configured the matrix and approved.

## Issues Encountered

None. Physics2DSettings.asset appeared as modified after user ran File > Save Project, confirming the four pairs were correctly written to disk.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Plan 03-03 (MeleeEnemy FSM) is fully unblocked: OnPlayerDeath event exists, PlayerInvincible collision pair disabled so rolling player is immune
- Plan 03-04 (RangedEnemy + ProjectileController) is fully unblocked: EnemyProjectile layer 11 configured, matrix prevents friendly-fire and roll-bypass
- Phase 4 UIManager can subscribe to `PlayerController.OnPlayerDeath` alongside `PlayerDeathHandler` with no modification to any Phase 3 script

---
*Phase: 03-enemy-system*
*Completed: 2026-06-08*
