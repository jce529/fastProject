---
phase: 14-enemy-spawn-vfx
plan: 01
subsystem: enemy-vfx
tags: [unity, coroutine, sprite-mask, spawn-vfx, enemy-fsm]

# Dependency graph
requires:
  - phase: 13-audio-foundation
    provides: AudioManager singleton + Sfx.PortalEnter clip hook
provides:
  - ISpawnGatable additive interface (SetSpawnGate bridge, IEnemy contract untouched)
  - MeleeEnemy/RangedEnemy ISpawnGatable implementations (IsAlive toggle)
  - EnemySpawnEffect.PlaySpawnSequence coroutine (portal grow -> walk-out+mask shrink -> portal fade -> gate release)
affects: [14-02-enemy-spawner-wiring, 14-03-standby-room-integration, 15-boss-fsm, 16-boss-room]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Additive interface bridge (ISpawnGatable) to extend enemy behavior without touching the frozen IEnemy 3-member contract"
    - "SpriteMask-based walk-out reveal wipe, reusing RuntimeMaskSprite cache and FloorTransitionEffect/EnemyDeathEffect ScaleTransform pattern"

key-files:
  created:
    - Assets/Scripts/Enemy/ISpawnGatable.cs
    - Assets/Scripts/Enemy/EnemySpawnEffect.cs
  modified:
    - Assets/Scripts/Enemy/MeleeEnemy.cs
    - Assets/Scripts/Enemy/RangedEnemy.cs

key-decisions:
  - "ISpawnGatable kept as a separate additive interface from IEnemy, preserving the frozen 3-member IEnemy contract for Phase 15/16 BossEnemy integration"
  - "EnemySpawnEffect kept self-contained (not extracted to shared utility) matching EnemyDeathEffect's existing convention"
  - "Portal size computed from SpriteRenderer.bounds (not Vector3.one) so spawn VFX scales correctly per enemy sprite"

patterns-established:
  - "Additive interface bridge pattern: new cross-cutting behavior (spawn gating) added via a second interface instead of modifying the stable IEnemy contract"

requirements-completed: [SPWN-01, SPWN-02]

# Metrics
duration: 10min
completed: 2026-07-10
---

# Phase 14 Plan 01: Spawn Gating Bridge & Spawn VFX Summary

**ISpawnGatable additive interface plus EnemySpawnEffect portal-grow/walk-out/mask-shrink/portal-fade coroutine, wired into MeleeEnemy and RangedEnemy without touching IEnemy or CombatController.**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-07-10T04:59:00Z (approx.)
- **Completed:** 2026-07-10T05:09:34Z
- **Tasks:** 2
- **Files modified:** 4 (2 created, 2 modified)

## Accomplishments
- SPWN-02: `ISpawnGatable.SetSpawnGate(bool)` lets any spawn-VFX caller force an enemy's `IsAlive` false during the spawn sequence and restore it afterward, without CombatController or IEnemy changes — existing `!enemy.IsAlive` skip checks and `Update()` guards transparently gate detection/targeting.
- SPWN-01: `EnemySpawnEffect.PlaySpawnSequence()` plays a full portal-grow → walk-out (with concurrent SpriteMask shrink) → portal-fade sequence (0.4s + 0.5s + 0.3s = 1.2s), sized from the enemy's actual `SpriteRenderer.bounds` rather than a hardcoded scale, and calls `gate?.SetSpawnGate(false)` on completion.
- IEnemy.cs and CombatController.cs remain untouched (git diff 0 lines each) — Phase 15/16 BossEnemy integration precondition preserved.

## Task Commits

Each task was committed atomically:

1. **Task 1: ISpawnGatable interface + MeleeEnemy/RangedEnemy wiring (SPWN-02)** - `ec70cef` (feat)
2. **Task 2: EnemySpawnEffect portal spawn VFX (SPWN-01, D-06~D-09)** - `a4ccdda` (feat)
3. **Unity meta file for new ISpawnGatable.cs asset** - `ded131b` (chore)

**Plan metadata:** pending (docs: complete plan)

## Files Created/Modified
- `Assets/Scripts/Enemy/ISpawnGatable.cs` - New additive interface: `void SetSpawnGate(bool isSpawning)`
- `Assets/Scripts/Enemy/EnemySpawnEffect.cs` - New self-contained spawn VFX coroutine component
- `Assets/Scripts/Enemy/MeleeEnemy.cs` - Implements ISpawnGatable (`SetSpawnGate(bool) => IsAlive = !isSpawning`)
- `Assets/Scripts/Enemy/RangedEnemy.cs` - Implements ISpawnGatable (same pattern)

## Decisions Made
- Followed plan exactly — code blocks in the plan were used verbatim for both ISpawnGatable and EnemySpawnEffect, since the plan pre-specified full implementations including sizing/duration constants and the timeScale-immune coroutine loop pattern.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None. Unity did not auto-generate the `.meta` file for `EnemySpawnEffect.cs` during this session (editor likely not open); only `ISpawnGatable.cs.meta` appeared and was committed. The missing `EnemySpawnEffect.cs.meta` will be auto-generated and should be committed the next time the Unity Editor opens this project — flagged here so it isn't mistaken for an untracked stray file in a future session.

## User Setup Required

None - no external service configuration required. Note: open the Unity Editor at least once before the next execution session so it generates `Assets/Scripts/Enemy/EnemySpawnEffect.cs.meta`; commit that file when it appears.

## Next Phase Readiness
- `ISpawnGatable` and `EnemySpawnEffect.PlaySpawnSequence(GameObject, ISpawnGatable)` form a stable, consumable contract for Plan 14-02 (EnemySpawner wiring) and 14-03 (standby room integration).
- No blockers. EnemySpawner/WorldGenerator were intentionally left untouched per plan scope — 14-02 will wire `AddComponent<EnemySpawnEffect>()` + `StartCoroutine` at `EnemySpawner.Activate()`.

---
*Phase: 14-enemy-spawn-vfx*
*Completed: 2026-07-10*

## Self-Check: PASSED

All created files and task commits verified present.
