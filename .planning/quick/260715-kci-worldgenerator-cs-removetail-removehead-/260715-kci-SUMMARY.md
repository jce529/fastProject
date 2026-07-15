---
phase: quick
plan: 260715-kci
subsystem: world-generation
tags: [refactor, worldgenerator, dedup]

# Dependency graph
requires: []
provides:
  - "CleanupSection(GameObject room, GameObject corridor, ExitPortal excludePortal = null) private helper in WorldGenerator.cs"
affects: [16-boss-room-worldgenerator-integration]

# Tech tracking
tech-stack:
  added: []
  patterns: ["Shared cleanup helper extracted from 3 duplicated call sites (RemoveTail/RemoveHead/FloorTransitionSequence), with excludePortal parameter to support per-caller exceptions"]

key-files:
  created: []
  modified: ["Assets/Scripts/World/WorldGenerator.cs"]

key-decisions:
  - "CleanupSection() leaves _chain node removal (RemoveFirst/RemoveLast/Clear) to the caller since each site uses a different LinkedList operation"
  - "FloorTransitionSequence() keeps its unconditional _activeExitCount = 0 reset after the loop unchanged — CleanupSection()'s internal per-section decrement is overwritten by this reset, producing an equivalent end state to the pre-refactor code"

patterns-established:
  - "CleanupSection(room, corridor, excludePortal): destroys portal StandbyRoom (unless excluded) + decrements _activeExitCount, removes _activatedSections/_pendingSpawns entries, destroys corridor/room GameObjects"

requirements-completed: []

# Metrics
duration: 10min
completed: 2026-07-15
---

# Quick Task 260715-kci: WorldGenerator CleanupSection Refactor Summary

**Extracted duplicated room+corridor cleanup logic from RemoveTail()/RemoveHead()/FloorTransitionSequence() into a single CleanupSection() helper — pure structural refactor, no behavior change, done ahead of Phase 16 boss room integration.**

## Performance

- **Duration:** ~10 min
- **Tasks:** 2 completed
- **Files modified:** 1 (Assets/Scripts/World/WorldGenerator.cs)

## Accomplishments
- Added private `CleanupSection(GameObject room, GameObject corridor, ExitPortal excludePortal = null)` helper matching the pre-agreed interface exactly (including XML doc comment)
- `RemoveTail()` and `RemoveHead()` reduced from ~26 lines each to 3 lines each, delegating to `CleanupSection()`
- `FloorTransitionSequence()`'s old-chain cleanup loop now calls `CleanupSection(chainRoom, chainCorridor, excludePortal: portal)` per iteration instead of inlining destroy/decrement/removal logic
- `_chain.Clear()` and the unconditional `_activeExitCount = 0` reset (with Debug.Log) remain untouched immediately after the loop

## Task Commits

Each task was committed atomically:

1. **Task 1: Add CleanupSection() helper and refactor RemoveTail()/RemoveHead()** - `a57e847` (refactor)
2. **Task 2: Refactor FloorTransitionSequence() cleanup loop to use CleanupSection()** - `646a5f4` (refactor)

## Files Created/Modified
- `Assets/Scripts/World/WorldGenerator.cs` - Added `CleanupSection()` private helper; refactored `RemoveTail()`, `RemoveHead()`, and `FloorTransitionSequence()`'s old-chain cleanup loop to delegate to it

## Decisions Made
None - plan executed exactly as written, matching the pre-agreed `<interfaces>` block signature and body verbatim.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

Unity Editor was already open with this project, so a separate `-batchmode` compile check could not be run (Unity refuses multiple instances on the same project). Verification fell back to the plan's documented alternative: manual read-through of the full file confirming CleanupSection() exists once with the exact agreed signature/body, RemoveTail()/RemoveHead() are each 3 lines, FloorTransitionSequence()'s loop calls CleanupSection() with excludePortal: portal while _chain.Clear()/_activeExitCount = 0 remain unchanged after the loop, and no other method in the file was touched (git diff confirms scope limited to the new method + 3 call sites). The already-open Unity Editor will auto-recompile on the next focus/file-watch cycle; no new compile errors are expected since only method bodies were extracted verbatim with no signature or type changes.

## Next Phase Readiness

WorldGenerator.cs is now ready for Phase 16 (boss room WorldGenerator integration, BOSS-10) to add a fourth exception case to the shared CleanupSection() logic without further duplication.

---
*Quick task: 260715-kci*
*Completed: 2026-07-15*

## Self-Check: PASSED

- FOUND: Assets/Scripts/World/WorldGenerator.cs
- FOUND: .planning/quick/260715-kci-worldgenerator-cs-removetail-removehead-/260715-kci-SUMMARY.md
- FOUND: a57e847
- FOUND: 646a5f4
