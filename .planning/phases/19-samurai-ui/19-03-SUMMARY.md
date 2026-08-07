---
phase: 19-samurai-ui
plan: 03
subsystem: gameplay-combat
tags: [unity, csharp, combat-module, parry, physics2d]

# Dependency graph
requires:
  - phase: 19-01
    provides: "TapSwingCombatModuleBase, IRealtimeCombatModule, IParryable, AimUtil, BasicCombatModule, CombatContext.SwingRadius/SwingHalfAngleDeg/TapLockout"
  - phase: 19-02
    provides: "CombatModuleRegistry (CombatModuleId enum), CombatModuleSelector.SelectedModuleId"
provides:
  - "SamuraiParryModule — D-04~D-06 parry judgement, falls back to base one-shot swing (D-02)"
  - "CombatController host-hook wiring 3 combat modules (Basic/Overclock/Samurai) via BuildModule()"
  - "UNLOCK-02 completed: CombatController.Awake() now honors CombatModuleSelector.SelectedModuleId"
affects: ["19-04 (SamuraiBoss projectile IParryable implementer)", "19-06 (integration playtest checkpoint)"]

# Tech tracking
tech-stack:
  added: []
  patterns: ["additive early-return host-hook for IRealtimeCombatModule to bypass Overclock's hold-slowmo state machine with zero byte changes to existing code path"]

key-files:
  created: [Assets/Scripts/Player/Combat/SamuraiParryModule.cs, Assets/Scripts/Player/Combat/SamuraiParryModule.cs.meta]
  modified: [Assets/Scripts/Player/CombatController.cs]

key-decisions:
  - "Reworded a code comment from '보스 OnDashHit 호출 없음' to '보스 피격 처리 호출 없음' to avoid the literal substring 'OnDashHit' appearing in SamuraiParryModule.cs, since the plan's own acceptance criteria (grep -c OnDashHit == 0) conflicted with its own action-block code template which included that string in a comment"

patterns-established:
  - "Realtime combat modules implement IRealtimeCombatModule; CombatController.Update() checks `_activeModule is IRealtimeCombatModule` immediately after the input-lock/_isBusy guards and early-returns via Tick(ctx), leaving all downstream Overclock logic untouched"

requirements-completed: [SAMURAI-02, SAMURAI-03]

# Metrics
duration: 15min
completed: 2026-08-07
---

# Phase 19 Plan 03: SamuraiParryModule + CombatController Host-Hook Summary

**SamuraiParryModule adds parry-first swing resolution (IParryable OverlapCircle scan, falls back to base one-shot swing), and CombatController.Awake()/Update() now host all 3 combat modules via an additive early-return hook that leaves Overclock's existing hold-slowmo path byte-identical.**

## Performance

- **Duration:** ~15 min
- **Started:** 2026-08-07
- **Completed:** 2026-08-07
- **Tasks:** 2/2 completed
- **Files modified:** 3 (2 created, 1 modified)

## Accomplishments
- `SamuraiParryModule` implements D-04~D-06: scans an `IParryable` filter (Enemy layer) inside the swing arc first; on a match it calls `OnParried(dir)` (pure defense, no kill credit) and returns, otherwise falls back to `base.ResolveSwing(ctx)` for the standard one-shot kill (D-02 fallback)
- `CombatController.Awake()` now builds `_activeModule` via a new `BuildModule(CombatModuleId)` factory keyed off `CombatModuleSelector.SelectedModuleId` (UNLOCK-02) instead of hardcoding `OverclockModule`
- `CombatController.Update()` gained a 7-line additive host-hook right after the `_isBusy` guard: if `_activeModule is IRealtimeCombatModule`, it calls `Tick(ctx)` and returns, bypassing Overclock's hold-slowmo→release-resolve state machine entirely for Basic/Samurai modules
- Verified via `git diff` that no existing line in `CombatController.cs` was altered outside the four additive insertion points (3 new tunable fields, `BuildModule()` call site, 3 new `CombatContext` field assignments, `BuildModule()` method definition, and the host-hook block) — INFRA-01 zero-regression requirement satisfied at the diff level

## Task Commits

Each task was committed atomically:

1. **Task 1: SamuraiParryModule — parry judgement (D-04~D-06)** - `6b29ce4` (feat)
2. **Task 2: CombatController — host-hook + module selection wiring** - `c197e4e` (feat)

**Plan metadata:** (this commit)

## Files Created/Modified
- `Assets/Scripts/Player/Combat/SamuraiParryModule.cs` - New class overriding `TapSwingCombatModuleBase.ResolveSwing()` with parry-first judgement
- `Assets/Scripts/Player/Combat/SamuraiParryModule.cs.meta` - Hand-authored minimal meta (no Unity Editor session available), matching repo convention
- `Assets/Scripts/Player/CombatController.cs` - Added 3 tunables, `BuildModule()` factory, 3 `CombatContext` field assignments, and the `IRealtimeCombatModule` host-hook in `Update()`

## Decisions Made
- Reworded one code comment in `SamuraiParryModule.cs` (`보스 OnDashHit 호출 없음` → `보스 피격 처리 호출 없음`) because the plan's literal action-block template included the substring "OnDashHit" inside a comment, which would have failed the plan's own `grep -c "OnDashHit" ... == 0` acceptance check. The functional requirement (D-06: no call to any kill-crediting method) was already satisfied — this was purely a comment wording fix to align with the plan's verification script, with zero behavior change.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug in plan's verify script] Reworded comment to avoid false-positive grep match**
- **Found during:** Task 1 (SamuraiParryModule.cs)
- **Issue:** Plan's `<action>` code template included the comment `// D-06: 순수 방어 — 보스 OnDashHit 호출 없음`, but the plan's own `<verify>`/`<acceptance_criteria>` required `grep -c "OnDashHit" SamuraiParryModule.cs` to equal 0. The literal template as given would have produced 1, failing the plan's own acceptance gate.
- **Fix:** Changed the comment text to `보스 피격 처리 호출 없음` (same meaning, no literal "OnDashHit" substring). No functional/behavioral change — `OnDashHit()` was never called from this file either way.
- **Files modified:** `Assets/Scripts/Player/Combat/SamuraiParryModule.cs`
- **Verification:** `grep -c "OnDashHit" Assets/Scripts/Player/Combat/SamuraiParryModule.cs` → `0`
- **Committed in:** `6b29ce4` (Task 1 commit)

---

**Total deviations:** 1 auto-fixed (1 bug in plan verification script, zero behavior change)
**Impact on plan:** Trivial comment wording fix required to pass the plan's own acceptance gate. No scope creep, no functional change.

## Issues Encountered
- This worktree started ~180 commits behind `main` (missing all of Phase 15/16/18/18.1/19-01/19-02/25 work). Per the parallel-execution instructions, ran `git merge main --ff-only` before any edits — clean fast-forward, no conflicts. `TapSwingCombatModuleBase`, `IParryable`, `AimUtil`, `CombatModuleRegistry`, `CombatModuleSelector`, `BasicCombatModule` from 19-01/19-02 were then present and used as-is.

## User Setup Required

None - no external service configuration required. Note: `swingRadius`/`swingHalfAngleDeg`/`tapLockout` are `[SerializeField]` on the `CombatController` prefab/scene component and will use their C# default values (3f/50f/0.12f) until a Unity Editor session re-serializes the prefab with explicit values — this does not block compilation or the plan's acceptance criteria, but is worth confirming during 19-06's integration playtest.

## Next Phase Readiness
- SAMURAI-02/03 player-side parry logic is code-complete; boss-side projectile (`IParryable` implementer) is 19-04's responsibility, running in parallel
- UNLOCK-02 wiring complete — `CombatModuleSelector.SelectedModuleId` now actually determines the equipped module at runtime
- Full functional/playtest verification (Overclock zero-regression, Basic/Samurai instant-swing at Time.timeScale=1, parry catching a projectile) is deferred to 19-06's integration checkpoint, consistent with this plan's `<success_criteria>`

---
*Phase: 19-samurai-ui*
*Completed: 2026-08-07*

## Self-Check: PASSED

- FOUND: Assets/Scripts/Player/Combat/SamuraiParryModule.cs
- FOUND: Assets/Scripts/Player/Combat/SamuraiParryModule.cs.meta
- FOUND: commit 6b29ce4
- FOUND: commit c197e4e
