---
phase: 16-boss-room-lifecycle
plan: 01
subsystem: cleanup
tags: [dead-code-removal, unity, refactoring]

# Dependency graph
requires: []
provides:
  - "Assets/Scripts/World free of TestWorldGenerator.cs, FloorSpawner.cs, RoomExit.cs (0% referenced dead code removed)"
  - "MeleeEnemy.cs/RangedEnemy.cs free of unused LayerPlayerHurtbox/LayerPlayerInvincible constants"
affects: [16-03-enemybase-extraction]

# Tech tracking
tech-stack:
  added: []
  patterns: []

key-files:
  created: []
  modified:
    - Assets/Scripts/Enemy/MeleeEnemy.cs
    - Assets/Scripts/Enemy/RangedEnemy.cs

key-decisions:
  - "Deleted TestWorldGenerator.cs/FloorSpawner.cs/RoomExit.cs (+.meta) after GUID cross-check confirmed 100% dead code per 16-CONTEXT.md D-01/D-02/D-03"
  - "Left comment references to FloorSpawner/RoomExit in WorldGenerator.cs/CameraFollow.cs/PlayerController.cs/ScoreManager.cs untouched — documentation-only, no compile impact, out of scope for surgical change"
  - "Removed LayerPlayerHurtbox/LayerPlayerInvincible duplicated dead constants from MeleeEnemy.cs and RangedEnemy.cs (D-04) while leaving the actively-used copies in InvincibilityHandler.cs intact"

requirements-completed: [D-01, D-02, D-03, D-04]

# Metrics
duration: 5min
completed: 2026-07-15
---

# Phase 16 Plan 01: Dead File & Dead Constant Removal Summary

**Deleted 3 confirmed-dead world scripts (TestWorldGenerator.cs, FloorSpawner.cs, RoomExit.cs + .meta) and removed unused LayerPlayerHurtbox/LayerPlayerInvincible constants from MeleeEnemy.cs/RangedEnemy.cs, clearing the way for Wave 2's EnemyBase extraction.**

## Performance

- **Duration:** 5 min
- **Started:** 2026-07-15T07:23:00Z
- **Completed:** 2026-07-15T07:27:46Z
- **Tasks:** 2
- **Files modified:** 8 (6 deleted, 2 edited)

## Accomplishments
- Removed 3 dead world scripts (`TestWorldGenerator.cs`, `FloorSpawner.cs`, `RoomExit.cs`) plus their `.meta` files, confirmed dead via GUID cross-check documented in 16-CONTEXT.md
- Removed duplicated, unreferenced `LayerPlayerHurtbox`/`LayerPlayerInvincible` constants from `MeleeEnemy.cs` and `RangedEnemy.cs`, superseded by the Physics2D collision matrix
- Verified `RangedEnemy.TelegraphDuration` constant (still actively used) was preserved untouched

## Task Commits

Each task was committed atomically:

1. **Task 1: 죽은 파일 3종 삭제 (D-01, D-02, D-03)** - `4abe0a3` (feat)
2. **Task 2: MeleeEnemy.cs/RangedEnemy.cs 죽은 레이어 상수 제거 (D-04)** - `4b67053` (refactor)

_No TDD tasks in this plan._

## Files Created/Modified
- `Assets/Scripts/World/TestWorldGenerator.cs` (+.meta) - deleted, 0 GUID references anywhere
- `Assets/Scripts/World/FloorSpawner.cs` (+.meta) - deleted, superseded by WorldGenerator (Phase 9)
- `Assets/Scripts/World/RoomExit.cs` (+.meta) - deleted, superseded by ExitPortal (Phase 10)
- `Assets/Scripts/Enemy/MeleeEnemy.cs` - removed 3-line dead layer-constant block
- `Assets/Scripts/Enemy/RangedEnemy.cs` - removed 2 dead layer-constant lines, kept `TelegraphDuration`

## Decisions Made
- Deleted only `.cs`/`.cs.meta` files for FloorSpawner/RoomExit — the corresponding inactive scene GameObject and legacy `Room_*.prefab`/`Room_Debug.prefab` references are explicitly deferred to the user to remove in the Unity Editor (per 16-CONTEXT.md D-02/D-03, Unity MCP was unavailable this session)
- Left comment-only references to the deleted class names in `WorldGenerator.cs`, `CameraFollow.cs`, `PlayerController.cs`, `ScoreManager.cs` untouched — no compile-time impact, editing them would violate the surgical-change principle for a plan scoped to file/constant deletion only

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

None - no external service configuration required. Note: two follow-up Unity Editor-only cleanups remain deferred to the user per 16-CONTEXT.md (`<deferred>` section): removing the inactive "FloorSpawner" GameObject from SampleScene.unity, and deleting `Room_Debug.prefab` + the 14 legacy `Room_*.prefab` files. These are out of scope for this plan.

## Next Phase Readiness
- `MeleeEnemy.cs`/`RangedEnemy.cs` are now clean of dead constants, ready for Wave 2's (16-03) `EnemyBase` extraction to proceed on a clean base
- No blockers introduced by this plan

---
*Phase: 16-boss-room-lifecycle*
*Completed: 2026-07-15*

## Self-Check: PASSED

- CONFIRMED_DELETED: Assets/Scripts/World/TestWorldGenerator.cs
- CONFIRMED_DELETED: Assets/Scripts/World/FloorSpawner.cs
- CONFIRMED_DELETED: Assets/Scripts/World/RoomExit.cs
- FOUND: Assets/Scripts/Enemy/MeleeEnemy.cs
- FOUND: Assets/Scripts/Enemy/RangedEnemy.cs
- FOUND commit: 4abe0a3
- FOUND commit: 4b67053
