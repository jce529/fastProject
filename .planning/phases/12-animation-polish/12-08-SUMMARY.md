---
phase: 12-animation-polish
plan: 08
subsystem: enemy
tags: [particle-system, spritemask, coroutine, unity]

# Dependency graph
requires:
  - phase: 12-animation-polish (12-01)
    provides: "RuntimeMaskSprite.CreateMaskSprite() shared static utility for runtime SpriteMask sprite generation"
provides:
  - "EnemyDeathEffect.cs -- PlayDeathSequence(Animator) coroutine: Die-wait -> particle burst -> bottom-up SpriteMask fade -> Destroy"
  - "MeleeEnemy/RangedEnemy OnDashHit() now actually Destroy()s the enemy GameObject after the death sequence (previously corpses persisted forever)"
affects: [12-09-playtest]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AddComponent-or-GetComponent runtime component attachment (no prefab/Inspector wiring needed for new visual effect components)"
    - "Runtime-generated ParticleSystem with ParticleSystemStopAction.Destroy + loop=false for self-cleaning one-shot VFX"

key-files:
  created:
    - Assets/Scripts/Enemy/EnemyDeathEffect.cs
  modified:
    - Assets/Scripts/Enemy/MeleeEnemy.cs
    - Assets/Scripts/Enemy/RangedEnemy.cs

key-decisions:
  - "_maskRiseDuration = 0.6f, _particleColor = (1, 0.3, 0.1) (orange-red) -- Claude's discretion per plan"
  - "Reused RuntimeMaskSprite.CreateMaskSprite() (Plan 12-01 output) instead of duplicating mask-sprite generation logic"

patterns-established:
  - "Death/transition VFX components attach via gameObject.AddComponent<T>() at the point of use rather than living on prefabs -- keeps prefabs untouched, no Inspector wiring required (matches FloorTransitionEffect precedent)"

requirements-completed: [D-09, D-11]

# Metrics
duration: 12min
completed: 2026-07-08
---

# Phase 12 Plan 08: Enemy Death Effect Summary

**Runtime-generated particle burst + bottom-up SpriteMask fade plays after the existing Die animation finishes, then the enemy GameObject is actually destroyed (previously corpses stayed disabled in the scene forever).**

## Performance

- **Duration:** 12 min
- **Started:** 2026-07-08T04:17:00Z
- **Completed:** 2026-07-08T04:29:00Z
- **Tasks:** 2
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments
- New `EnemyDeathEffect.cs`: `PlayDeathSequence(Animator)` coroutine waits for the Die animation's clip length, spawns a 12-particle orange-red burst, then rises a `SpriteMask` from the bottom of the sprite to the top over 0.6s (all `Time.unscaledDeltaTime`-driven, slow-mo immune), then destroys both the mask GameObject and the enemy GameObject.
- `MeleeEnemy.OnDashHit()` and `RangedEnemy.OnDashHit()` now attach `EnemyDeathEffect` via `AddComponent`-or-`GetComponent` and kick off the coroutine as the final step, after the existing `_animator?.SetBool("isDead", true)` line (unchanged).
- Behavior change: killed enemies are now actually removed from the scene via `Destroy(gameObject)`. Previously, `OnDashHit()` only disabled colliders/physics and set the death animation bool -- the disabled corpse GameObject remained in the scene permanently, which is a mobile memory concern per CLAUDE.md constraints.

## Task Commits

Each task was committed atomically:

1. **Task 1: EnemyDeathEffect.cs -- Die wait -> particle -> SpriteMask fade -> Destroy (D-09)** - `26bb340` (feat)
2. **Task 2: MeleeEnemy.cs + RangedEnemy.cs -- OnDashHit() wiring (D-09, D-11)** - `c30a328` (feat)

_Note: this plan executed in an isolated parallel-executor worktree whose base commit predates Phase 12's `.planning/` scaffolding (created before Plan 12-01 merged). The `.planning/phases/12-animation-polish/` directory did not exist in this worktree and was created solely to hold this SUMMARY.md. STATE.md/ROADMAP.md updates were skipped here per orchestrator instructions -- see "Next Phase Readiness" below._

## Files Created/Modified
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` - New `[RequireComponent(typeof(SpriteRenderer))]` MonoBehaviour with `PlayDeathSequence(Animator)` public coroutine and private `SpawnDeathParticles()` helper
- `Assets/Scripts/Enemy/MeleeEnemy.cs` - `OnDashHit()` appends `AddComponent`-or-`GetComponent<EnemyDeathEffect>()` + `StartCoroutine(deathEffect.PlayDeathSequence(_animator))`; no other method touched
- `Assets/Scripts/Enemy/RangedEnemy.cs` - Same pattern as MeleeEnemy; no other method touched

## Decisions Made
- `_maskRiseDuration = 0.6f` and `_particleColor = (1f, 0.3f, 0.1f)` chosen at Claude's discretion as specified by the plan (no existing convention to match against for enemy death VFX timing/color).
- Reused `RuntimeMaskSprite.CreateMaskSprite()` (Plan 12-01 static utility) rather than duplicating the 4x4 white-texture sprite generation, per the plan's explicit D-01/D-09 pattern-sharing directive.

## Deviations from Plan

None - plan executed exactly as written. Both tasks matched the plan's action blocks precisely (field names, coroutine structure, particle burst settings, mask math).

## Issues Encountered

**Stale worktree base:** This parallel-executor worktree (`worktree-agent-aca87164f6064af46`) forked from a commit predating Phase 12's `.planning/` scaffolding and predating Plan 12-01's `RuntimeMaskSprite.cs` file. `EnemyDeathEffect.cs` in this worktree therefore references `RuntimeMaskSprite.CreateMaskSprite()` without that file being physically present in this worktree's checkout -- it exists on `main` (commit `5353fb8`, Plan 12-01) and will resolve correctly once this worktree's commits are merged into `main`. No file was duplicated into this worktree to avoid a merge conflict with the real Plan 12-01 file. Isolated compilation of this worktree alone would fail; the merged result will not.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Code deliverables (`EnemyDeathEffect.cs`, `MeleeEnemy.cs`, `RangedEnemy.cs` changes) are complete and committed (`26bb340`, `c30a328`) and ready to merge into `main`.
- **Orchestrator action needed:** after merging this branch into `main`, reconcile `.planning/STATE.md` and `.planning/ROADMAP.md` there (this worktree's copies are stale/missing the Phase 12 section and were not modified by this plan run).
- Plan 12-09 (playtest) can proceed once merged -- no prefab or Inspector wiring is required since `EnemyDeathEffect` attaches at runtime via `AddComponent`.

---
*Phase: 12-animation-polish*
*Completed: 2026-07-08*

## Self-Check: PASSED

- FOUND: Assets/Scripts/Enemy/EnemyDeathEffect.cs
- FOUND: .planning/phases/12-animation-polish/12-08-SUMMARY.md
- FOUND: commit 26bb340 (Task 1)
- FOUND: commit c30a328 (Task 2)
