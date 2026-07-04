---
phase: 10-exit-portal-floor-transition
plan: 01
subsystem: world-gen
tags: [unity, monobehaviour, editor-tooling, prefab-builder, exit-portal]

# Dependency graph
requires:
  - phase: 09-infinite-gen-cleanup
    provides: WorldGenerator MonoBehaviour skeleton (Instance/EnterPortal added in Plan 02, not yet present)
provides:
  - ExitSpawnPoint empty marker component (Room-child spawn-candidate contract)
  - ExitPortal trigger component with per-instance StandbyRoom and WorldGenerator.Instance.EnterPortal call contract
  - ExitPortalBuilder editor menu (prefab not yet generated -- deferred to Plan 03)
affects: [10-02-worldgenerator-integration, 10-03-editor-manual-setup, 10-04]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Coroutine-ownership contract: trigger MonoBehaviours that may be destroyed mid-sequence must delegate StartCoroutine to a persistent singleton (WorldGenerator.Instance), never themselves"
    - "Per-instance auto-property (StandbyRoom) instead of a static field, to support multiple simultaneously-active portals safely"

key-files:
  created:
    - Assets/Scripts/World/ExitSpawnPoint.cs
    - Assets/Scripts/World/ExitPortal.cs
    - Assets/Editor/ExitPortalBuilder.cs
  modified: []

key-decisions:
  - "ExitPortal starts the floor-transition coroutine from WorldGenerator.Instance rather than itself, to avoid the sequence aborting when the portal's Room is Destroyed mid-transition (D-07)"
  - "StandbyRoom is a per-instance property (not static) so _maxExitsActive > 1 never causes portals to overwrite each other's target room (D-08)"
  - "ExitPortalBuilder intentionally omits any sprite/VFX component per REQUIREMENTS.md Out of Scope -- visual confirmation relies solely on ExitPortal.OnDrawGizmos()"

patterns-established:
  - "Empty marker component pattern (ExitSpawnPoint) ports RoomEntry.cs exactly -- GetComponentsInChildren<T>(true) discovery convention"

requirements-completed: [EXIT-01, EXIT-03]

# Metrics
duration: 12min
completed: 2026-07-03
---

# Phase 10 Plan 01: EXIT Portal Base Components Summary

**Three new files establishing the EXIT portal code contracts (ExitSpawnPoint marker, ExitPortal trigger with WorldGenerator-owned coroutine delegation, ExitPortalBuilder prefab-builder menu) -- no WorldGenerator wiring or prefab generation yet, both deferred to Plans 02/03.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-07-03T11:16:00Z
- **Completed:** 2026-07-03T11:21:04Z
- **Tasks:** 2
- **Files modified:** 3 (all new)

## Accomplishments
- `ExitSpawnPoint` empty marker component for EXIT portal spawn candidates (D-01), following the `RoomEntry.cs` pattern exactly
- `ExitPortal` trigger collider component: per-instance `StandbyRoom` property (D-08), `_triggered` double-fire guard, `OnTriggerEnter2D` delegating to `WorldGenerator.Instance.EnterPortal(this)` instead of starting its own coroutine (avoids Pitfall 1 -- coroutine abort on mid-sequence Destroy)
- `ExitPortalBuilder` editor menu (`Fast/Phase10/Build Exit Portal Prefab`) that will generate a BoxCollider2D(isTrigger, size 1.5x2.5, offset y=1.25) + ExitPortal prefab at `Assets/Prefabs/World/ExitPortal/ExitPortal.prefab`, porting `CorridorBuilder.cs`'s idempotent `SavePrefab` convention

## Task Commits

Each task was committed atomically:

1. **Task 1: ExitSpawnPoint.cs + ExitPortal.cs -- marker/trigger components** - `1fd181c` (feat)
2. **Task 2: ExitPortalBuilder.cs -- ExitPortal prefab-builder editor tool** - `da1a685` (feat)

_Note: this worktree branch had diverged from `main` since Phase 8 (missing all of Phase 9's WorldGenerator/Room-tilemap work and the Phase 10 planning docs). Before committing, the branch was rebased onto `main` (clean, no conflicts) so that both task commits sit on top of the current `WorldGenerator.cs` and `.planning/phases/10-exit-portal-floor-transition/` state. Commit hashes above reflect the post-rebase hashes._

## Files Created/Modified
- `Assets/Scripts/World/ExitSpawnPoint.cs` (15 lines) - Empty marker component for EXIT spawn candidate points
- `Assets/Scripts/World/ExitPortal.cs` (41 lines) - Trigger collider + per-portal StandbyRoom + WorldGenerator-delegated transition start
- `Assets/Editor/ExitPortalBuilder.cs` (50 lines) - `Fast/Phase10/Build Exit Portal Prefab` menu, BoxCollider2D+ExitPortal only, no sprite/VFX

## Decisions Made
- Coroutine ownership: `ExitPortal.OnTriggerEnter2D` calls `WorldGenerator.Instance.EnterPortal(this)` rather than `StartCoroutine` on itself, because D-07's transition sequence Destroys the portal's Room (and the portal itself) mid-sequence -- a self-owned coroutine would be silently aborted by Unity the instant its owning GameObject is destroyed.
- `StandbyRoom` is an instance property, not static, so that `_maxExitsActive > 1` (multiple portals active at once) never causes one portal's pre-spawned next-floor room to overwrite another's.
- No sprite/VFX added to the portal prefab per REQUIREMENTS.md Out of Scope ("포탈 이펙트/사운드") -- gizmo-only visual confirmation in the editor, consistent with 10-RESEARCH.md's Open Question 3 recommendation.

## Deviations from Plan

None in terms of code -- both files were created exactly as specified in the plan's `<action>` blocks, and both tasks' acceptance criteria were verified via grep before committing.

**Infrastructure note (not a plan deviation, but material to review):** This plan executed in an isolated git worktree (`worktree-agent-a251d0b0bae5e15fa`) that had not been kept in sync with `main` since Phase 8 -- it was missing `WorldGenerator.cs` entirely, all of Phase 9's WorldGenerator/Room-tilemap work, and the Phase 10 planning docs (including this very `10-01-PLAN.md`, which was only readable via the shared main checkout path, not the worktree). Before the plan's tasks could produce a coherent result (correct `.planning/phases/10-exit-portal-floor-transition/` directory to write this SUMMARY into, and a `WorldGenerator.cs` for Plan 02 to extend), the worktree branch was rebased onto `main` (`git rebase main`, no conflicts -- the two new task commits touched only brand-new files not present in `main`). This is an execution-environment fix, not a code change, and does not affect the plan's `files_modified` list or acceptance criteria.

## Issues Encountered
- Worktree branch out-of-sync with `main` (see above) -- resolved via a clean, conflict-free `git rebase main` before the final task commit and SUMMARY/STATE update step.

## Next Phase Readiness
- Plan 02 (WorldGenerator integration) can now add `Instance` (singleton) and `EnterPortal(ExitPortal)` to `WorldGenerator.cs` -- `ExitPortal.cs` already references `WorldGenerator.Instance.EnterPortal(this)` with the exact expected signature.
- Plan 03 (manual editor work) can run `Fast/Phase10/Build Exit Portal Prefab` once Plan 02 lands, since the compile error currently blocking a full build is scoped solely to the two symbols Plan 02 adds.
- No blockers.

---
*Phase: 10-exit-portal-floor-transition*
*Completed: 2026-07-03*

## Self-Check: PASSED

- FOUND: Assets/Scripts/World/ExitSpawnPoint.cs
- FOUND: Assets/Scripts/World/ExitPortal.cs
- FOUND: Assets/Editor/ExitPortalBuilder.cs
- FOUND commit: 1fd181c
- FOUND commit: da1a685
