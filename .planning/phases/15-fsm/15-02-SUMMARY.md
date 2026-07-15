---
phase: 15-fsm
plan: 02
subsystem: enemy-ai
tags: [boss-fsm, ienemy, ispawngatable, score-system, death-effect]

# Dependency graph
requires:
  - phase: 15-01
    provides: "ScoreManager.AddBossKillScore()/SubtractScore(int), EnemyDeathEffect.ConfigureIntensity()"
provides:
  - "BossEnemy.cs — full Telegraph->Attack->Vulnerable single-pattern FSM implementing IEnemy+ISpawnGatable"
  - "IsAlive reused as vulnerable-window signal (not alive/dead), decoupled _isDefeated flag for race-safe hit registration"
affects: [15-03, 15-04, 16-boss-room-lifecycle]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IsAlive property overload: true means 'currently targetable (vulnerable)', not 'not dead' — mirrors CombatController's existing !IsAlive skip check with zero CombatController changes"
    - "_isDefeated as the sole death guard in OnDashHit(), decoupled from IsAlive, to survive the ~0.15s ExecuteDash() dash-travel race window (15-RESEARCH.md Pitfall 2)"

key-files:
  created:
    - Assets/Scripts/Enemy/BossEnemy.cs
  modified: []

key-decisions:
  - "D-12 offset logic (ScoreManager.SubtractScore(KillScore) on every non-lethal hit) removed from OnDashHit() — Phase 16 (16-02/16-03, executed after 15-02-PLAN.md was authored) moved ScoreManager.AddKillScore() out of CombatController.ExecuteDash() into EnemyBase.OnDashHit(); since BossEnemy does not inherit EnemyBase, CombatController no longer grants +100 on boss hits in the first place, so there is nothing left to offset. This matches 15-CONTEXT.md's own SUPERSEDED note for D-12."
  - "BossEnemy does not inherit EnemyBase (per plan) — it independently implements IEnemy+ISpawnGatable to keep the Telegraph/Attack/Vulnerable FSM self-contained per 15-CONTEXT.md D-01~D-09"

patterns-established:
  - "Boss-specific ClearHighlight() override returns to vulnerableTintColor instead of hardcoded white while in Vulnerable state (Pitfall 3 generalization)"

requirements-completed: [BOSS-03, BOSS-04, BOSS-05, BOSS-06]

# Metrics
duration: 25min
completed: 2026-07-15
---

# Phase 15 Plan 02: Boss FSM (Telegraph/Attack/Vulnerable Loop) Summary

**BossEnemy.cs single-file FSM implementing IEnemy+ISpawnGatable — repeating Telegraph(move+windup)->Attack(hitbox)->Vulnerable(stop+tint) pattern, 7-hit kill with per-hit pattern reset, boss-extended death sequence, and score bonus, with zero changes to CombatController/IEnemy/ISpawnGatable contracts.**

## Performance

- **Duration:** 25 min
- **Started:** 2026-07-15T16:40:00+09:00 (approx)
- **Completed:** 2026-07-15T17:05:00+09:00 (approx)
- **Tasks:** 1 completed
- **Files modified:** 1 created

## Accomplishments
- `BossEnemy : MonoBehaviour, IEnemy, ISpawnGatable` implements the full pattern loop: Telegraph (slowed movement toward player + "!" icon), Attack (melee hitbox windup/active window), Vulnerable (stop + yellow tint for 1.0s) — repeats indefinitely (BOSS-03)
- `IsAlive` is overloaded as "currently targetable (vulnerable)" rather than "alive" — false during Telegraph/Attack/HitReaction, true only during Vulnerable — reusing `CombatController.FindNearestEnemyInRange()`'s existing `!enemy.IsAlive` skip check with zero CombatController changes
- `OnDashHit()` guards solely on a separate `_isDefeated` flag (never on `IsAlive`), avoiding the ~0.15s `ExecuteDash()` dash-travel race where the vulnerable window could close mid-dash (15-RESEARCH.md Pitfall 2)
- Exactly 7 hits (`_hitCount++` then `>= RequiredHits`, Pitfall 1) trigger `Die()`: static rigidbody, disabled colliders, `EnemyDeathEffect.ConfigureIntensity()` with boss-extended mask/particle/shake values, `CameraFollow.Shake()`, and `ScoreManager.AddBossKillScore()` (BOSS-04/06)
- `_hitCount` stays a private field with no UI/Canvas/TextMeshPro reference anywhere in the file (BOSS-05, verified via grep)
- Non-lethal hits (1-6) trigger `HitReactionAndReset()`: color flash + knockback, then a 0.5s real-time pause before the pattern restarts from Telegraph (D-06/D-07)

## Task Commits

Each task was committed atomically:

1. **Task 1: BossEnemy.cs 전체 구현 — Telegraph→Attack→Vulnerable 패턴 루프 + 피격/처치/하이라이트** - `27ebf7f` (feat)

**Plan metadata:** (this commit)

## Files Created/Modified
- `Assets/Scripts/Enemy/BossEnemy.cs` - New boss FSM component: Telegraph/Attack/Vulnerable pattern loop, race-safe hit registration, 7-hit kill with per-hit reset, boss-extended death sequence + score bonus

## Decisions Made
- Kept `BossEnemy` as a standalone `IEnemy`+`ISpawnGatable` implementation rather than inheriting `EnemyBase` — the plan explicitly designed the FSM as a single self-contained file (compile-atomicity across the 3-member `IEnemy` contract), and `EnemyBase.OnDashHit()`'s common death path (unconditional `IsAlive=false` + immediate `ScoreManager.AddKillScore()`) is incompatible with the boss's multi-hit/pattern-reset requirements
- Removed the D-12 score self-offset call (`ScoreManager.SubtractScore(ScoreManager.KillScore)` per non-lethal hit) — see Deviations below

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Removed stale D-12 score self-offset call from `OnDashHit()`'s non-lethal branch**
- **Found during:** Task 1 (reading current `CombatController.cs` per the plan's `<read_first>` instruction, before writing `BossEnemy.cs`)
- **Issue:** The plan's `<action>` code block (and its `<interfaces>` section) assumes `CombatController.ExecuteDash()` unconditionally calls `ScoreManager.AddKillScore(isRespawnKill)` right after `target.OnDashHit()` — a premise from before Phase 16. Phase 16 (16-02/16-03, both already committed on `main` — `d8e0577`, `07af4c8`, `a307e7f`) removed that call from `CombatController.ExecuteDash()` and moved kill scoring into `EnemyBase.OnDashHit()` instead (confirmed by reading current `CombatController.cs:291-293`, which now contains only a comment stating the removal, and `EnemyBase.cs:39-59`). `BossEnemy` does not inherit `EnemyBase`, so it never receives this scoring call in the first place. Implementing the plan's `ScoreManager.SubtractScore(ScoreManager.KillScore)` call literally would have silently deducted 100 points from the player's score on every one of the boss's 6 non-lethal hits, with nothing having added it — a real scoring bug. This exact supersession is independently documented in `15-CONTEXT.md` D-12 ("SUPERSEDED 2026-07-15, Phase 16 discuss-phase 세션") which states the offset workaround is no longer needed once scoring moved to each enemy's own `OnDashHit()`.
- **Fix:** Removed the `ScoreManager.SubtractScore(ScoreManager.KillScore);` line and its stale comment from the non-lethal branch of `OnDashHit()`, replacing the comment with an explanation referencing the supersession and `CombatController.cs:291-293`. The 7th (lethal) hit still calls `ScoreManager.AddBossKillScore()` in `Die()` exactly as planned — that part of D-09 is unaffected.
- **Files modified:** `Assets/Scripts/Enemy/BossEnemy.cs` (this is the only file the plan touches; no other files were changed)
- **Verification:** Confirmed via direct read of `Assets/Scripts/Player/CombatController.cs` (lines 291-293 show the AddKillScore call was already removed with an explanatory comment) and `Assets/Scripts/Enemy/EnemyBase.cs` (lines 39-59 show `OnDashHit()` now owns the `AddKillScore` call). All other acceptance criteria from the plan (class declaration, `RequiredHits = 7`, `if (_isDefeated) return;` guard, `_hitCount++` then `>=` comparison, `Die()` calling `AddBossKillScore()`/`ConfigureIntensity()`/`Shake()`, `ClearHighlight()` using `_baseColor`/`vulnerableTintColor` instead of `Color.white`, no `UnityEngine.UI`/`TextMeshPro` references) verified via grep and pass unchanged.
- **Committed in:** `27ebf7f` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug fix — Rule 1)
**Impact on plan:** Necessary correctness fix; without it the boss would silently drain 600 points (6 x 100) from the player's own score over a single kill cycle for no reason. No scope creep — same single file (`BossEnemy.cs`) touched, same task, all other plan content implemented verbatim.

## Issues Encountered

None beyond the deviation above — no build/compile verification was run in this session (Unity Editor was not launched to compile); the acceptance-criteria greps confirm the code shape but not a live compiler pass. Full Unity console compile-error check remains scheduled for 15-04 per the plan's own `<verification>` section ("Unity 콘솔 에러 0건은 15-04에서 최종 확인").

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `Assets/Scripts/Enemy/BossEnemy.cs` exists and is committed — 15-03 (prefab builder + `DebugRoomTeleporter` wiring) can immediately `AddComponent<BossEnemy>()` / `GetComponent<ISpawnGatable>()` on a boss GameObject.
- `IEnemy.cs`, `ISpawnGatable.cs`, `CombatController.cs` are all untouched (git diff 0 on all three) — roadmap lock respected.
- Open item for 15-03/15-04: `_exclamationIcon` and `_meleeHitbox` are `[SerializeField]` references that must be assigned on the boss prefab (child GameObjects with SpriteRenderer / Trigger Collider2D) — this is explicitly deferred to the 15-03 prefab-builder plan per the plan's own design (`_exclamationIcon`/`_meleeHitbox` comments say "15-03 프리팹 빌더가 할당").
- No compile verification performed this session — first Unity Editor open (15-03 or 15-04) should confirm 0 console errors before further boss-room integration work.

---
*Phase: 15-fsm*
*Completed: 2026-07-15*

## Self-Check: PASSED

- FOUND: Assets/Scripts/Enemy/BossEnemy.cs
- FOUND: .planning/phases/15-fsm/15-02-SUMMARY.md
- FOUND: commit 27ebf7f
