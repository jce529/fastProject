---
phase: 16-boss-room-lifecycle
plan: 03
subsystem: enemy
tags: [refactor, inheritance, unity, csharp, scoring]

# Dependency graph
requires:
  - phase: 16-02
    provides: "CombatController.ExecuteDash() no longer calls ScoreManager.AddKillScore() or checks RespawnedEnemyMarker — CombatController-side half of D-08 complete"
provides:
  - "EnemyBase abstract class implementing IEnemy/ISpawnGatable — shared OnDashHit() common part, ClearHighlight(), IsPlayerInRange(), OnEnable/OnDisable PlayerController.OnPlayerDeath subscription, SetSpawnGate()"
  - "MeleeEnemy/RangedEnemy inherit EnemyBase, no longer duplicate common logic"
  - "D-08 complete: each enemy self-scores via ScoreManager.AddKillScore(isRespawnKill) inside EnemyBase.OnDashHit(), right after IsAlive=false commit"
affects: [15-fsm]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "EnemyBase abstract class with protected abstract StopEnemySpecificState()/OnPlayerDied() hooks for subclass-specific death/player-death behavior"
    - "Self-scoring pattern: each IEnemy implementor calls ScoreManager.AddKillScore() itself at the moment IsAlive is committed to false, instead of the caller (CombatController) doing it centrally"

key-files:
  created:
    - Assets/Scripts/Enemy/EnemyBase.cs
  modified:
    - Assets/Scripts/Enemy/MeleeEnemy.cs
    - Assets/Scripts/Enemy/RangedEnemy.cs

key-decisions:
  - "EnemyBase is a minimal-scope extraction (only 100%-identical members), not a full inheritance refactor — type-specific logic (telegraph/hitbox/jump-gap avoidance in MeleeEnemy; aim-line/projectile/kiting in RangedEnemy) stays in subclasses, per user instruction (D-05)."
  - "IEnemy/ISpawnGatable interface contracts are unchanged — EnemyBase implements both, subclasses inherit them transitively with no signature changes."

requirements-completed: [D-05, D-08]

# Metrics
duration: 5min
completed: 2026-07-15
---

# Phase 16 Plan 03: EnemyBase Extraction & Self-Scoring Summary

**Extracted `EnemyBase` abstract class holding the 100%-duplicated `OnDashHit()` common part, `ClearHighlight()`, `IsPlayerInRange()`, `OnEnable`/`OnDisable` player-death subscription, and `SetSpawnGate()` from `MeleeEnemy`/`RangedEnemy`, and moved kill-score awarding into each enemy's own death-commit moment via `EnemyBase.OnDashHit()` — completing D-08 (CombatController half was done in 16-02).**

## Performance

- **Duration:** 5 min
- **Started:** 2026-07-15T07:29:57Z
- **Completed:** 2026-07-15T07:34:03Z
- **Tasks:** 2/2 completed
- **Files modified:** 3 (1 created, 2 modified)

## Accomplishments
- D-05: New `EnemyBase : MonoBehaviour, IEnemy, ISpawnGatable` abstract class holds the shared runtime refs (`_detectionBuffer`, `_playerFilter`, `_playerTransform`, `_rb`, `_animator`), the `IEnemy`/`ISpawnGatable` implementation, and `IsPlayerInRange()`.
- D-08: `EnemyBase.OnDashHit()` now calls `ScoreManager.AddKillScore(isRespawnKill)` immediately after `IsAlive = false` is committed, with `isRespawnKill` self-judged via `GetComponent<RespawnedEnemyMarker>() != null` — completing the score-timing redesign started in 16-02 (which removed the equivalent call from `CombatController.ExecuteDash()`).
- `MeleeEnemy`/`RangedEnemy` now inherit `EnemyBase` and implement `protected override StopEnemySpecificState()` (subclass-specific death cleanup) and `protected override OnPlayerDied()` (subclass-specific player-death cleanup) — all other type-specific behavior (FSM, telegraph, hitbox, jump/gap avoidance, aim-line, projectile firing, kiting) is unchanged.

## Task Commits

Each task was committed atomically:

1. **Task 1: EnemyBase.cs 신설 (D-05 공통부 + D-08 점수 적립)** - `e435c09` (feat)
2. **Task 2: MeleeEnemy/RangedEnemy가 EnemyBase를 상속하도록 리팩토링 (D-05, D-08)** - `07af4c8` (refactor)

**Plan metadata:** (this commit) `docs(16-03): complete EnemyBase extraction plan`

## Files Created/Modified
- `Assets/Scripts/Enemy/EnemyBase.cs` - New abstract base holding common IEnemy/ISpawnGatable implementation, shared runtime refs, `OnDashHit()` common sequence (guard → IsAlive=false → score → subclass stop hook → rb freeze → collider disable → animator isDead → death effect), `ClearHighlight()`, `SetSpawnGate()`, `IsPlayerInRange()`
- `Assets/Scripts/Enemy/MeleeEnemy.cs` - Now `: EnemyBase`; removed duplicated `IsAlive`/`OnDashHit()`/`ClearHighlight()`/`SetSpawnGate()`/`OnEnable`/`OnDisable`/`IsPlayerInRange()`; added `StopEnemySpecificState()`/`OnPlayerDied()` overrides. FSM/telegraph/hitbox/jump-gap logic untouched.
- `Assets/Scripts/Enemy/RangedEnemy.cs` - Same pattern; `StopEnemySpecificState()` stops `_telegraphCoroutine` + disables `_aimLine`, `OnPlayerDied()` stops coroutine + hides aim line + resets FSM. FSM/telegraph/projectile/kiting logic untouched.

## Decisions Made
- EnemyBase kept intentionally minimal — no attempt to also share FSM state enum, patrol logic, or detection-radius fields, per 16-CONTEXT.md D-05 user instruction ("일단 서로 공유할 최소한의 내용들만 만들어줘").
- `_detectionBuffer`/`_playerFilter`/`_rb`/`_animator`/`_playerTransform` promoted to `protected` fields on `EnemyBase` rather than kept private-and-duplicated, since both subclasses assigned/read them identically in `Awake()`/state methods — this is within the "100% identical" extraction scope, not new abstraction.

## Deviations from Plan

None - plan executed exactly as written. Verified no other files in the codebase reference `MeleeEnemy`/`RangedEnemy` as concrete types (only in comments), so no downstream call sites needed updates.

## Issues Encountered
None.

## User Setup Required
None - no external service configuration required. Note: `EnemyBase.cs` has no `.meta` file yet since it was created outside the Unity Editor — Unity will auto-generate one (with a fresh GUID) the next time the project is opened, consistent with how prior plans in this phase handled new/deleted files requiring editor-side finalization.

## Next Phase Readiness
- D-05 and D-08 are now fully complete (D-08 spanned 16-02 CombatController-side + this plan's Enemy-side).
- `EnemyBase` is positioned for Phase 15 (BossEnemy, not yet implemented) to inherit from, avoiding a third copy-paste and eliminating the score-offset workaround previously planned in 15-CONTEXT.md D-12 (already marked SUPERSEDED).
- Plan 16-04 (if scoped) or Phase 16 verification can proceed — no blockers. Note: pre-existing uncommitted changes to `Assets/Materials/PortalVortex.mat` and `Assets/Scenes/SampleScene.unity` exist in the working tree from prior sessions; they are out of scope for this plan and were left untouched.

---
*Phase: 16-boss-room-lifecycle*
*Completed: 2026-07-15*

## Self-Check: PASSED

- FOUND: Assets/Scripts/Enemy/EnemyBase.cs
- FOUND: Assets/Scripts/Enemy/MeleeEnemy.cs
- FOUND: Assets/Scripts/Enemy/RangedEnemy.cs
- FOUND: commit e435c09 (Task 1)
- FOUND: commit 07af4c8 (Task 2)
