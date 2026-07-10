---
phase: 14-enemy-spawn-vfx
plan: 03
subsystem: world-generation
tags: [unity, coroutine, worldgenerator, enemy-spawn, corridor, floor-transition]

# Dependency graph
requires:
  - phase: 14-enemy-spawn-vfx (14-01)
    provides: ISpawnGatable additive interface + EnemySpawnEffect.PlaySpawnSequence coroutine contract
  - phase: 14-enemy-spawn-vfx (14-02)
    provides: EnemySpawner two-stage Spawn()/Activate(GameObject portalEffectPrefab = null) API with HasActivated one-shot gating
provides:
  - WorldGenerator wired end-to-end to the D-01 pre-generate/real-entry-split architecture — TrySpawnEnemies() now collects only, TryActivateSection()/ActivateStaggered() trigger real spawn VFX at actual room/corridor entry
  - CheckCorridorEntry() Pattern 3 threshold detection so Corridor entry (which is always crossed before the next Room's boundary) triggers its own spawns independently of Room node transitions
  - New floor start room (FloorTransitionSequence Step 4) explicitly activated via TryActivateSection(newRoom), replacing the prior permanent no-op
  - _activatedSections/_pendingSpawns per-entry cleanup in RemoveTail()/RemoveHead()/FloorTransitionSequence() old-chain destroy loop (no blanket Clear() — preserves standbyRoom's pre-registered pending spawn across the floor transition)
affects: [14-04-corridor-tool-execution-and-playtest, 15-boss-fsm, 16-boss-room]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Collect-then-activate split: Spawn() at Instantiate time (pre-generation, off-screen), Activate() deferred to a HashSet-gated TryActivateSection() call at actual player entry — decouples world-gen lookahead from spawn VFX timing"
    - "Threshold-based entry detection (CheckCorridorEntry) supplementing node-transition-based detection (UpdatePlayerIndex) for sub-Room-granularity triggers that must fire before the coarser Room-boundary check"
    - "Per-entry Dictionary/HashSet cleanup mirrored across all four teardown sites (RemoveTail/RemoveHead/FloorTransitionSequence loop) instead of blanket Clear() to protect state that outlives the _chain lifecycle (standbyRoom)"

key-files:
  modified:
    - Assets/Scripts/World/WorldGenerator.cs

key-decisions:
  - "Followed plan verbatim — plan pre-specified full code blocks for every edit point (fields, TrySpawnEnemies split, TryActivateSection/ActivateStaggered, CheckCorridorEntry, all 4 cleanup sites, Step 4 replacement), so no interpretation was needed"
  - "Did not touch the two pending prior-wave .meta files (EnemySpawnEffect.cs.meta, CorridorEnemySpawnerTool.cs.meta) that appeared as untracked during this session, or any of the pre-existing unrelated uncommitted changes (.vscode/settings.json, prefabs, RollController.cs) — explicitly out of scope per plan note, deferred to 14-04"

patterns-established:
  - "Dual-detection entry system: coarse Room-boundary detection (UpdatePlayerIndex, existing) + fine Corridor-threshold detection (CheckCorridorEntry, new) both funnel into the same idempotent TryActivateSection() gate"

requirements-completed: [SPWN-01, SPWN-02]

# Metrics
duration: 5min
completed: 2026-07-10
---

# Phase 14 Plan 03: WorldGenerator Spawn-Gating Wiring Summary

**WorldGenerator's TrySpawnEnemies() split into collect-only (Spawn at Instantiate) + TryActivateSection/ActivateStaggered (Activate at real Room/Corridor entry, D-05 staggered via WaitForSecondsRealtime), with Pattern-3 Corridor threshold detection and explicit new-floor-start-room activation replacing the prior permanent no-op.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-07-10T05:15:08Z
- **Completed:** 2026-07-10T05:17:53Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- SPWN-01/SPWN-02 runtime trigger path completed for both Room and Corridor: `TrySpawnEnemies()` now only `Spawn()`s and records filtered `EnemySpawner` lists in `_pendingSpawns`; `TryActivateSection()` (idempotent via `_activatedSections` HashSet) drains that list through `ActivateStaggered()`, which calls `spawner.Activate(_portalEffectPrefab)` on a `WaitForSecondsRealtime(_spawnStaggerInterval)` cadence (D-05, timeScale-immune).
- Pitfall 2 (Corridor spawn VFX exposed too early) resolved via `CheckCorridorEntry()` — a Pattern 3 threshold check called every `Update()` right after `UpdatePlayerIndex()`, detecting the Corridor's own ENT/EXIT connector crossing (which always precedes the next Room's boundary) in both forward and backward directions.
- Pitfall 5 / Open Question 3 (new floor start room enemies never activating) resolved: `FloorTransitionSequence()` Step 4's "intentional no-op" placeholder comment was replaced with `TryActivateSection(newRoom)`. Verified the load-bearing invariant holds: the old-chain destroy `foreach` loop only removes `_activatedSections`/`_pendingSpawns` entries for rooms/corridors that were actually in `_chain` — `standbyRoom` (= `newRoom`) was never added to `_chain`, so its `_pendingSpawns` entry (registered earlier in `TrySpawnExitPortal()`) survives untouched through to Step 4.
- `dotnet build Fast.slnx` (Assembly-CSharp + Assembly-CSharp-Editor) succeeds with 0 errors / 0 warnings after both tasks.

## Task Commits

Each task was committed atomically:

1. **Task 1: TrySpawnEnemies collection-only + Corridor collection + Room entry activation wiring (D-01/D-02/D-03/D-05)** - `91dc2c8` (feat)
2. **Task 2: Corridor entry detection (Pattern 3) + FloorTransitionSequence Step 4 fix + RemoveTail/RemoveHead cleanup (D-01/D-03, Pitfall 2/5)** - `084b0c2` (feat)

**Plan metadata:** pending (docs: complete plan)

## Files Created/Modified
- `Assets/Scripts/World/WorldGenerator.cs` - `_spawnStaggerInterval` field + `_activatedSections`/`_pendingSpawns` runtime state; `TrySpawnEnemies()` split into collect-only; new `TryActivateSection()`/`ActivateStaggered()`; `SpawnNextPair()`/`SpawnPrevPair()` now also collect corridor spawners; `Start()`/`UpdatePlayerIndex()` call `TryActivateSection()` on real entry; new `CheckCorridorEntry()` called from `Update()`; `RemoveTail()`/`RemoveHead()`/`FloorTransitionSequence()` old-chain loop clean up per-entry state; Step 4 no-op replaced with `TryActivateSection(newRoom)`

## Decisions Made
Followed plan exactly — every edit point (fields, method bodies, call-site insertions) was pre-specified verbatim in the plan's `<action>` blocks, so no interpretation was required.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. The plan's own verification command for the "no blanket Clear()" acceptance criterion (`grep -c "_activatedSections.Clear()\|_pendingSpawns.Clear()"`) matches the explanatory code comment text itself (which mentions the blanket-Clear anti-pattern by name to warn against it), returning a count of 1. Confirmed by inspection this is the comment, not an actual `.Clear()` call — no `_activatedSections.Clear()`/`_pendingSpawns.Clear()` invocation exists anywhere in the file. Documented here since the literal grep count differs from the intended "0 blanket-Clear calls" semantic.

## User Setup Required

None - no external service configuration required. Note (carried over from 14-01/14-02): `Assets/Scripts/Enemy/EnemySpawnEffect.cs.meta` and `Assets/Editor/CorridorEnemySpawnerTool.cs.meta` are still pending auto-generation/commit — left untouched this session per plan scope, deferred to 14-04.

## Next Phase Readiness
- WorldGenerator's runtime wiring for SPWN-01/SPWN-02 is complete for both Room and Corridor entry paths, including the new-floor-start-room edge case — ready for 14-04's editor execution + playtest checkpoint to verify actual portal timing, detection gating, and the floor-transition Activate path in the running game.
- `CorridorEnemySpawnerTool.cs` menu execution (14-02 output) is still pending — 14-04 must run it before Corridor enemies have any `EnemySpawner` markers to collect via this plan's `TrySpawnEnemies(corridor, ...)` calls.
- No blockers.

---
*Phase: 14-enemy-spawn-vfx*
*Completed: 2026-07-10*

## Self-Check: PASSED

All modified files and task commits verified present.
