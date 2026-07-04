---
phase: 10-exit-portal-floor-transition
plan: 02
subsystem: world-generation
tags: [unity, coroutine, singleton, tilemap-chain, floor-transition]

# Dependency graph
requires:
  - phase: 10-exit-portal-floor-transition (Plan 01)
    provides: ExitPortal.cs (StandbyRoom property, OnTriggerEnter2D -> WorldGenerator.Instance.EnterPortal), ExitSpawnPoint.cs (marker component)
provides:
  - WorldGenerator.Instance static singleton (Awake-initialized)
  - TrySpawnExitPortal(GameObject room) — EXIT-01/EXIT-02 probability + max-active-count portal spawning
  - EnterPortal(ExitPortal) public API + FloorTransitionSequence() 6-step coroutine — EXIT-03 floor transition
  - RemoveTail() D-08 cleanup of unused portal standby rooms during lookbehind teardown
affects: [10-03 (Complex_Room RoomEntry/ExitSpawnPoint placement), 10-04 (scene wiring/inspector references, playtest verification)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Persistent-singleton-owns-coroutine: transition coroutines that Destroy their own trigger's containing GameObject must run on a MonoBehaviour that survives the Destroy, never on the triggering component itself (Pitfall 1)"
    - "Per-instance state over shared field: replaced a single _nextFloorRoom field with per-portal ExitPortal.StandbyRoom to avoid overwrite when multiple portals are active concurrently (Pitfall 4)"

key-files:
  created: []
  modified:
    - Assets/Scripts/World/WorldGenerator.cs

key-decisions:
  - "WorldGenerator.Instance singleton assigned in Awake() rather than a lazy getter — ExitPortal.OnTriggerEnter2D needs a guaranteed non-null reference at trigger time"
  - "_activeExitCount reset to 0 unconditionally at floor-transition time — every portal in the destroyed chain (entered + orphaned) is accounted for by the chain teardown loop, so a blanket reset is correct rather than error-prone incremental decrements"
  - "orphanPortal != portal guard in chain teardown loop — prevents destroying the StandbyRoom the player is about to teleport into, since that GameObject has already been reassigned to newRoom before the loop runs conceptually (portal.StandbyRoom is captured via local var before Destroy)"

patterns-established:
  - "6-step floor transition: lock input -> destroy old chain (incl. orphan portal cleanup) -> activate standby room -> ENT teleport -> camera snap -> unlock input, using WaitForSecondsRealtime only"

requirements-completed: [EXIT-01, EXIT-02, EXIT-03]

# Metrics
duration: 12min
completed: 2026-07-03
---

# Phase 10 Plan 02: EXIT Portal Spawn Logic + Floor Transition Coroutine Summary

**WorldGenerator.cs gains a singleton Instance, probability/max-count gated portal spawning (TrySpawnExitPortal), and a 6-step FloorTransitionSequence coroutine that destroys the old room-corridor chain and teleports the player into a pre-spawned standby room.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-07-03T11:16:00Z
- **Completed:** 2026-07-03T11:28:11Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- WorldGenerator now exposes `public static WorldGenerator Instance` via `Awake()`, resolving the compile-time dependency `ExitPortal.OnTriggerEnter2D()` (Plan 01) had on it
- `TrySpawnExitPortal()` implements EXIT-01 (probability roll via `_exitSpawnChance`) and EXIT-02 (max concurrent active portals via `_maxExitsActive`), replacing the Phase 9 `SpawnNextFloorStandbyRoom()` stub entirely
- `EnterPortal()` + `FloorTransitionSequence()` implement EXIT-03: increment `FloorManager.CurrentFloor`, destroy the entire old chain (including orphaned portals' standby rooms per the review-fix D-08 requirement), activate the entered portal's pre-spawned standby room, teleport the player via `RoomEntry`, snap the camera via `CameraBound`, and unlock input — using `WaitForSecondsRealtime` exclusively per project convention
- `RemoveTail()` extended for D-08: GEN-02 lookbehind cleanup now destroys an unused portal's `StandbyRoom` and decrements `_activeExitCount`, freeing a portal spawn slot

## Task Commits

Each task was committed atomically:

1. **Task 1: EXIT-01/EXIT-02 — portal spawn fields + TrySpawnExitPortal() + RemoveTail() D-08 cleanup** - `ae395bb` (feat)
2. **Task 2: EXIT-03 — EnterPortal() + FloorTransitionSequence() 6-step coroutine** - `ddcc741` (feat)

**Plan metadata:** (this commit, docs: complete plan)

## Files Created/Modified
- `Assets/Scripts/World/WorldGenerator.cs` - Added Exit Portal fields (`_exitPortalPrefab`, `_exitSpawnChance`, `_maxExitsActive`), Phase 10 references (`_player`, `_combatController`), `Instance` singleton + `Awake()`, `TrySpawnExitPortal()` (replaces removed `SpawnNextFloorStandbyRoom()` stub and removed `_nextFloorRoom` field), `RemoveTail()` D-08 cleanup, `EnterPortal()` + `FloorTransitionSequence()` 6-step coroutine. 183 -> 310 lines (+127).

## Decisions Made
- No new architectural decisions — plan's `key_links`/`must_haves` were followed exactly, including the reviewer-added `orphanPortal != portal` guard in the chain-teardown loop (see key-decisions in frontmatter for rationale).

## Deviations from Plan

None - plan executed exactly as written. All code blocks in the plan's `<action>` sections were inserted verbatim at the specified anchor points; no additional fixes, blockers, or ambiguities were encountered.

## Issues Encountered

**Worktree staleness (resolved before task execution, not a plan deviation):** This worktree was based on an older `main` commit that predated Plan 01's `ExitPortal.cs`/`ExitSpawnPoint.cs` and the `10-02-PLAN.md` file itself. Per the orchestrator's explicit instructions, ran `git rebase main` before starting — completed cleanly with no conflicts, bringing in commits up to `67578c3` (Plan 01 complete). No code changes resulted from this step beyond the rebase itself.

## User Setup Required

None - no external service configuration required. Note for Plan 03/04: `WorldGenerator._exitPortalPrefab`, `_player`, and `_combatController` Inspector fields are new and currently unassigned in the scene — these must be wired in a later plan (out of this plan's scope per its `files_modified` declaration, which is limited to the script file).

## Next Phase Readiness

- `WorldGenerator.cs` compiles cleanly against Plan 01's `ExitPortal.cs`/`ExitSpawnPoint.cs` — the `WorldGenerator.Instance` reference that was previously undefined (compile error) in Plan 01's output is now resolved by this plan's `Awake()` singleton assignment.
- All 6 `<must_haves><truths>` items and all 4 `<key_links>` from this plan's frontmatter are satisfied and grep-verified (see Task 1/Task 2 `<verify>` output below).
- Task 1 verify (grep on `_exitSpawnChance|_maxExitsActive|_activeExitCount|public static WorldGenerator Instance|private void Awake|TrySpawnExitPortal|GetComponentInChildren<ExitPortal>`): all 11 acceptance-criteria lines found — field declarations, singleton, Awake, TrySpawnExitPortal guards + Debug.Log calls, RemoveTail's ExitPortal lookup, Start()/SpawnNextPair() call sites.
- Task 2 verify (grep on `using System.Collections;|public void EnterPortal|IEnumerator FloorTransitionSequence|WaitForSecondsRealtime(0.05f)|_player.UnlockInput()|EnterPortal → Floor|orphanPortal != portal`): all 7 acceptance-criteria lines found, including the reviewer-mandated `orphanPortal != portal` guard in the chain-teardown loop.
- Confirmed via negative grep that `yield return new WaitForSeconds(` (non-realtime) does not appear anywhere in the file.
- Confirmed via grep that `SpawnNextFloorStandbyRoom` and `_nextFloorRoom` no longer appear anywhere in `Assets/` — clean stub replacement, no orphaned references.
- Ready for Plan 03 (Complex_Room RoomEntry/ExitSpawnPoint placement) and Plan 04 (scene/inspector wiring + playtest verification of the full EXIT-01/02/03 loop).

---
*Phase: 10-exit-portal-floor-transition*
*Completed: 2026-07-03*

## Self-Check: PASSED

- FOUND: Assets/Scripts/World/WorldGenerator.cs
- FOUND: .planning/phases/10-exit-portal-floor-transition/10-02-SUMMARY.md
- FOUND: ae395bb (Task 1 commit)
- FOUND: ddcc741 (Task 2 commit)
