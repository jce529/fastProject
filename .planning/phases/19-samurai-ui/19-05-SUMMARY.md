---
phase: 19-samurai-ui
plan: 05
subsystem: infra
tags: [unity-editor-tools, debug-scene, input-system, prefab-builder]

# Dependency graph
requires:
  - phase: 19-samurai-ui (19-03, 19-04)
    provides: SamuraiParryModule/CombatController host-hook wiring (19-03), SamuraiBoss FSM + ParryableProjectile (19-04)
provides:
  - SamuraiBossPrefabBuilder.cs (Fast/Phase19/Build SamuraiBoss Prefab menu, unrun)
  - RoomSamuraiFsmTestBuilder.cs (Fast/Phase19/Build Room_SamuraiFsmTest menu, unrun)
  - DebugSceneBuilder.cs extension (Room_SamuraiFsmTest teleporter pad + DebugCombatModuleSwitcher wiring, unrun)
  - CombatController.DebugSetActiveModule(CombatModuleId) public debug hook
  - DebugCombatModuleSwitcher.cs (digit-key 1/2/3 module switching component)
affects: [19-06]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Editor prefab builder mirroring (BossEnemyPrefabBuilder.cs -> SamuraiBossPrefabBuilder.cs, RoomBossFsmTestBuilder.cs -> RoomSamuraiFsmTestBuilder.cs) for D-14 boss test-content parity"
    - "Tools-write-only plans: editor tools authored and committed without executing their menu items — asset generation deferred to a later checkpoint plan"

key-files:
  created:
    - Assets/Editor/SamuraiBossPrefabBuilder.cs
    - Assets/Editor/RoomSamuraiFsmTestBuilder.cs
    - Assets/Scripts/Debug/DebugCombatModuleSwitcher.cs
  modified:
    - Assets/Editor/DebugSceneBuilder.cs
    - Assets/Scripts/Player/CombatController.cs

key-decisions:
  - "SamuraiBoss.prefab uses 1.5x scale + blue-gray tint (0.25, 0.35, 0.6) vs Fiora's 1.6x + dark red, for at-a-glance visual distinction (Claude's Discretion per plan)"
  - "DebugSetActiveModule() is a 1-line public wrapper around the existing private BuildModule() factory — zero behavior change to the Awake()-time module selection flow"

patterns-established:
  - "D-18 substitute pattern: when a real lobby/tutorial flow is out of phase scope, a DebugScene-only debug component (not gated behind any production UI) provides equivalent manual test coverage"

requirements-completed: [SAMURAI-01, SAMURAI-05]

# Metrics
duration: 10min
completed: 2026-08-07
---

# Phase 19 Plan 05: SAMURAI DebugScene Tooling Summary

**Two editor prefab/room builders (SamuraiBossPrefabBuilder, RoomSamuraiFsmTestBuilder) mirroring the existing Fiora tooling, plus a DebugScene extension and digit-key module switcher (`DebugCombatModuleSwitcher`) — all authored but not yet executed, so no scene/prefab assets changed in this plan.**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-08-07T07:15:00Z (approx, after `git merge main --ff-only`)
- **Completed:** 2026-08-07T07:25:00Z
- **Tasks:** 2
- **Files modified:** 6 (4 created + 2 modified, plus 3 `.meta` files)

## Accomplishments
- `SamuraiBossPrefabBuilder.cs` mirrors `BossEnemyPrefabBuilder.cs`'s clone/component-swap/compound-collider pattern exactly, targeting `SamuraiBoss` instead of `FioraBoss`
- `RoomSamuraiFsmTestBuilder.cs` mirrors `RoomBossFsmTestBuilder.cs`'s room-construction convention (flat floor, RoomEntry+ExitSpawnPoint, Door/ENT+EXIT connectors, CameraBound, nested boss instance)
- `DebugSceneBuilder.cs` gained a pure-addition step between the existing Room_BossFsmTest placement and Canvas construction: loads `Room_SamuraiFsmTest.prefab`, spawns a `ToSamuraiRoom_Teleporter` pad, and attaches `DebugCombatModuleSwitcher` to the cloned debug player
- `CombatController.cs` gained a single public debug method (`DebugSetActiveModule`) with zero changes to any other line
- `DebugCombatModuleSwitcher.cs` created new, mapping digit keys 1/2/3 to Basic/Overclock/Samurai combat modules

## Task Commits

Each task was committed atomically:

1. **Task 1: SamuraiBossPrefabBuilder + RoomSamuraiFsmTestBuilder** - `90dcbdf` (feat)
2. **Task 2: DebugSceneBuilder extension + DebugCombatModuleSwitcher + CombatController debug hook** - `2cfac7f` (feat)

**Plan metadata:** (this commit) `docs(19-05): complete plan`

## Files Created/Modified
- `Assets/Editor/SamuraiBossPrefabBuilder.cs` - Fast/Phase19/Build SamuraiBoss Prefab menu, clones MeleeEnemy.prefab structure into SamuraiBoss.prefab
- `Assets/Editor/RoomSamuraiFsmTestBuilder.cs` - Fast/Phase19/Build Room_SamuraiFsmTest menu, builds isolated FSM test room with nested SamuraiBoss instance
- `Assets/Editor/DebugSceneBuilder.cs` - added Room_SamuraiFsmTest teleporter pad + DebugCombatModuleSwitcher wiring step (pure addition, existing logic untouched)
- `Assets/Scripts/Player/CombatController.cs` - added `DebugSetActiveModule(CombatModuleId)` public wrapper (1 line + doc comment)
- `Assets/Scripts/Debug/DebugCombatModuleSwitcher.cs` - new MonoBehaviour, digit-key 1/2/3 module switching for DebugScene testing

## Decisions Made
- SamuraiBoss.prefab scale/tint chosen as 1.5x + blue-gray (vs Fiora's 1.6x + dark red) purely for visual distinction between the two bosses in DebugScene — no gameplay implication (Claude's Discretion, as flagged in plan)
- No other deviations — plan's explicit "tools only, do not execute menus or save scene/prefab assets" instruction followed exactly; verified via `git diff --stat Assets/Prefabs/` and `git diff --stat Assets/Scenes/DebugScene.unity` returning empty output after both tasks

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- Worktree `agent-af54c53af825ee4bb` was significantly behind `main` at session start (last commit `cf33f38`, missing Phase 19-01 through 19-04 and all of Phase 15/16/18/18.1/25) — resolved via `git merge main --ff-only` before any edits, per the orchestrator's pre-flight instructions. No conflicts; fast-forward only.
- Generated `.meta` files manually for all 3 new `.cs` files (`SamuraiBossPrefabBuilder.cs.meta`, `RoomSamuraiFsmTestBuilder.cs.meta`, `DebugCombatModuleSwitcher.cs.meta`) using freshly generated 32-hex-char GUIDs, matching the simple `fileFormatVersion: 2\nguid: ...` format observed on existing sibling `.meta` files — done proactively per the orchestrator's warning that a prior wave's agent (19-02) forgot this step.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- All tooling for 19-06's checkpoint is in place and compiles cleanly against the merged main state (SamuraiBoss, CombatModuleRegistry, CombatController host-hooks, ParryableProjectile all present as of the pre-execution merge)
- 19-06 must run, in order: `Fast/Phase19/Build SamuraiBoss Prefab` -> `Fast/Phase19/Build Room_SamuraiFsmTest` -> `Fast/Debug/Build DebugScene`, then verify the DebugScene checklist items (SamuraiBoss.prefab exists, Room_SamuraiFsmTest.prefab has nested boss, teleporter reaches the new room, digit keys 1/2/3 swap combat modules during Play)
- No blockers — this plan intentionally left all scene/prefab assets unchanged (`git diff --stat Assets/Scenes/ Assets/Prefabs/` is empty), consistent with its "tools only" scope boundary

---
*Phase: 19-samurai-ui*
*Completed: 2026-08-07*

## Self-Check: PASSED

All created files verified present on disk; both task commits (`90dcbdf`, `2cfac7f`) verified present in `git log --oneline --all`.
