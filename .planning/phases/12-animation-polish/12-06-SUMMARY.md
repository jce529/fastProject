---
phase: 12-animation-polish
plan: 06
subsystem: gameplay-feel
tags: [unity, animator, camera-shake, trailrenderer, editor-tooling]

# Dependency graph
requires:
  - phase: 02-combat-core
    provides: CombatController.ExecuteDash() dash-and-kill sequence, CameraFollow LateUpdate loop
provides:
  - AutoDestroySelf.cs reusable effect-cleanup component (Animator-clip-length or fallback timer)
  - HitSparkBuilder.cs editor tool -- builds HitSparkEffect.prefab from existing SwordGuardImpact.anim
  - PlayerTrailBuilder.cs editor tool -- attaches TrailRenderer to the Player GameObject (none existed in project)
  - CameraFollow.Shake(duration, amplitude) -- unscaledDeltaTime-based screen shake
  - CombatController hit-impact wiring -- SpawnHitSpark + camera shake + TrailRenderer visual config
affects: [12-07 (prefab build + Inspector wiring + playtest)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AutoDestroySelf: WaitForSecondsRealtime-based effect cleanup, timeScale=0-immune (matches HitFreeze pattern used elsewhere)"
    - "Editor builder tools (MenuItem) reuse existing sprite/anim assets instead of creating new art"

key-files:
  created:
    - Assets/Scripts/Effects/AutoDestroySelf.cs
    - Assets/Editor/HitSparkBuilder.cs
    - Assets/Editor/PlayerTrailBuilder.cs
  modified:
    - Assets/Scripts/Camera/CameraFollow.cs
    - Assets/Scripts/Player/CombatController.cs

key-decisions:
  - "TrailRenderer did not exist anywhere in the project prior to this plan -- PlayerTrailBuilder attaches a new component rather than 'enhancing' a nonexistent one; CombatController.ConfigureTrailVisuals() sets the enhanced visuals (width curve + color gradient) once attached"
  - "Camera shake decay uses Time.unscaledDeltaTime so it continues to resolve during HitFreeze (Time.timeScale=0)"
  - "Hit spark prefab reuses SwordGuardImpact.anim -- no new art assets created, per plan scope"

patterns-established:
  - "Hit-impact effects (D-07/D-08/D-10) are wired directly in CombatController.ExecuteDash() between target.OnDashHit() and ScoreManager.AddKillScore()"

requirements-completed: [D-07, D-08, D-10]

# Metrics
duration: ~15min
completed: 2026-07-08
---

# Phase 12 Plan 06: Hit Impact Polish (Hit Spark / Camera Shake / Trail) Summary

**Camera shake + reusable hit-spark builder (reusing SwordGuardImpact.anim) + newly-attached, gradient-styled TrailRenderer wired into CombatController's kill sequence.**

## Performance

- **Duration:** ~15 min
- **Completed:** 2026-07-08T04:13:22Z
- **Tasks:** 3
- **Files modified:** 5 (3 created, 2 modified) + 3 `.meta` files

## Accomplishments
- D-07: `AutoDestroySelf.cs` + `HitSparkBuilder.cs` -- self-cleaning hit spark effect built from the existing GuardImpact sprite animation, no new art
- D-08: `CameraFollow.Shake(duration, amplitude)` -- decaying screen shake immune to HitFreeze's `Time.timeScale=0`
- D-10: `PlayerTrailBuilder.cs` discovers TrailRenderer was absent project-wide and attaches one to the Player; `CombatController.ConfigureTrailVisuals()` applies a cyan-to-blue gradient and tapering width curve
- All three effects wired into `CombatController.ExecuteDash()` immediately after `target.OnDashHit()`

## Task Commits

Each task was committed atomically:

1. **Task 1: AutoDestroySelf.cs + HitSparkBuilder.cs (D-07)** - `c64c8a6` (feat)
2. **Task 2: PlayerTrailBuilder.cs -- attach TrailRenderer to Player (D-10 setup)** - `f532839` (feat)
3. **Task 3: CameraFollow.Shake() + CombatController wiring (D-07/D-08/D-10)** - `aaf6d67` (feat)

**Plan metadata:** (this commit)

## Files Created/Modified
- `Assets/Scripts/Effects/AutoDestroySelf.cs` - Destroys an effect GameObject after its Animator clip length (or a fallback), using `WaitForSecondsRealtime` so it survives `Time.timeScale=0`
- `Assets/Editor/HitSparkBuilder.cs` - `Fast/Phase12/Build Hit Spark Prefab` menu; builds `Assets/Prefabs/Effects/HitSparkEffect.prefab` from `SwordGuardImpact.anim`
- `Assets/Editor/PlayerTrailBuilder.cs` - `Fast/Phase12/Add Player Trail Renderer` menu; attaches a `TrailRenderer` to the scene's `CombatController` GameObject (idempotent)
- `Assets/Scripts/Camera/CameraFollow.cs` - Added `Shake(float duration, float amplitude)` and shake-offset application at the end of `LateUpdate()`
- `Assets/Scripts/Player/CombatController.cs` - Added `_hitSparkPrefab`/`_cameraShakeDuration`/`_cameraShakeAmplitude` fields, `_cameraFollow` reference, `ConfigureTrailVisuals()`, `SpawnHitSpark()`, and wired both into `ExecuteDash()`

## Decisions Made
- TrailRenderer was confirmed absent from the entire project (grep-verified) -- Task 2 attaches a new component instead of modifying a nonexistent one, and Task 3's `ConfigureTrailVisuals()` applies the "enhanced" visual settings (gradient + width curve) once the component exists
- Camera shake fields/decay live in `CameraFollow` (not `CombatController`) since `CameraFollow` already owns `LateUpdate()` positioning; `CombatController` only calls `Shake()` via a null-conditional reference resolved in `Awake()`

## Deviations from Plan

None - plan executed exactly as written. One environmental note (not a plan deviation): this executor's git worktree (`agent-a5ccff045fe73c056`) was branched before Phase 11/12 planning docs existed. It was fast-forward merged to `main` (`9b91afc` -> `7214065`, no divergent commits, no conflicts) at the start of this session so the Phase 12 plan files and prerequisite `CombatController.cs`/`CameraFollow.cs` code (including the Phase 11 timer work and the quick-260706-lj0 obstacle-linecast fix) were present before editing began.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required. However, Plan 12-07 requires manual Unity Editor steps before these effects are functionally live:
1. Run `Fast/Phase12/Build Hit Spark Prefab` menu to generate `Assets/Prefabs/Effects/HitSparkEffect.prefab`
2. Run `Fast/Phase12/Add Player Trail Renderer` menu to attach `TrailRenderer` to the Player GameObject in `SampleScene`
3. Assign the built `HitSparkEffect.prefab` to `CombatController._hitSparkPrefab` in the Inspector
4. Playtest to confirm hit spark, camera shake, and trail visuals all fire correctly on kill

## Next Phase Readiness
- All three D-07/D-08/D-10 code paths are implemented and compile-clean against current `CombatController.cs`/`CameraFollow.cs`
- Not yet functionally verified in-editor (no prefab exists yet, `_hitSparkPrefab` unassigned) -- this is explicitly deferred to Plan 12-07 per this plan's scope
- No blockers for 12-07

---
*Phase: 12-animation-polish*
*Completed: 2026-07-08*

## Self-Check: PASSED

All created/modified files verified present on disk; all three task commit hashes (c64c8a6, f532839, aaf6d67) verified present in git log.
