---
phase: 12-animation-polish
plan: 04
subsystem: animation
tags: [unity-editor-tool, animatorcontroller, combat, roll]

# Dependency graph
requires:
  - phase: 12-animation-polish (earlier plans in phase)
    provides: FastPlayerAnimator.controller current state (IsMoving/IsGrounded/VelocityY/IsAttacking/IsRolling/IsSprinting/IsDashing params, Idle/Walk/JumpRise/JumpMid/JumpFall/Sprint/Attack/Roll/Dash states)
provides:
  - "PlayerAnimatorPatcher.cs editor tool (menu item, not yet run) that will add Whiff trigger+state and Roll trigger+AnyState transition to FastPlayerAnimator.controller"
affects: [12-05 (runs the menu item created here, playtests result)]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AnimatorController programmatic patching via UnityEditor.Animations API, idempotent guard pattern (HasParameter/FindState/HasAnyStateTransitionTo) for safe re-runs"

key-files:
  created: [Assets/Editor/PlayerAnimatorPatcher.cs]
  modified: []

key-decisions:
  - "Whiff state motion reuses AirSlash.anim (Claude's Discretion per plan) -- no new art needed, fits the '헛베기' (whiff/air-slash) theme"
  - "Roll trigger transition added via AnyState alongside the existing IsRolling bool-driven transitions, not replacing them -- avoids touching Idle/Walk/Sprint->Roll conditions that already work"

patterns-established:
  - "Idempotent AnimatorController patch tools: guard every AddParameter/AddState/AddAnyStateTransition call with a existence check so re-running the menu item is always safe"

requirements-completed: [D-05, D-06]

# Metrics
duration: 5min
completed: 2026-07-08
---

# Phase 12 Plan 04: Player Animator Patcher (Whiff/Roll Triggers) Summary

**New idempotent Unity Editor tool (Assets/Editor/PlayerAnimatorPatcher.cs) that will add the missing "Whiff" trigger+state (reusing AirSlash.anim) and "Roll" trigger+AnyState transition to FastPlayerAnimator.controller -- not yet executed, no gameplay C# code touched.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-07-08T04:04:00Z
- **Completed:** 2026-07-08T04:07:42Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Created `Assets/Editor/PlayerAnimatorPatcher.cs` with `Fast/Phase12/Patch Player Animator (Whiff+Roll Triggers)` menu item
- Tool adds the "Whiff" Trigger parameter + Whiff state (motion = AirSlash.anim) + AnyState->Whiff transition + Whiff->Idle exit transition (D-05)
- Tool adds the "Roll" Trigger parameter + AnyState->Roll transition, coexisting with the existing IsRolling bool-driven transitions into Roll (D-06)
- All additions are guarded by idempotency checks (`HasParameter`, `FindState` null-check, `HasAnyStateTransitionTo`) so re-running the menu never creates duplicates
- Confirmed `CombatController.cs` (`SetTrigger("Whiff")`, line 313) and `RollController.cs` (`SetTrigger("Roll")`, line 57) were already correct -- the bug was purely a missing parameter/state on the controller asset, not the C# call sites

## Task Commits

1. **Task 1: PlayerAnimatorPatcher.cs -- Whiff/Roll 트리거+상태 패치 에디터 도구** - `a6fc240` (feat)

**Plan metadata:** (pending — this SUMMARY commit)

## Files Created/Modified
- `Assets/Editor/PlayerAnimatorPatcher.cs` - New idempotent editor tool that patches FastPlayerAnimator.controller with Whiff/Roll triggers, states, and AnyState transitions

## Decisions Made
- AirSlash.anim reused as Whiff motion (D-05, Claude's Discretion per plan interfaces) -- matches the "헛베기" (whiff attack) theme without requiring new art assets
- Roll AnyState transition added alongside (not replacing) the existing IsRolling bool-driven Idle/Walk/Sprint->Roll transitions, per plan instruction to not modify existing behavior

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required

None - no external service configuration required. However, the menu item created here has **not been run yet**. The controller asset (`FastPlayerAnimator.controller`) is unchanged by this plan. Running the menu item and verifying in-editor is the responsibility of Plan 12-05 (checkpoint plan).

## Next Phase Readiness

**Manual execution procedure for Plan 12-05:**
1. Open Unity Editor with this project.
2. Menu: `Fast > Phase12 > Patch Player Animator (Whiff+Roll Triggers)`.
3. Verify console log: `[PlayerAnimatorPatcher] Whiff/Roll 트리거+상태 패치 완료.`
4. Open `Assets/Player/Resource/Animation/FastPlayerAnimator.controller` in the Animator window and confirm:
   - New `Whiff` Trigger parameter and `Whiff` state (motion = AirSlash.anim) exist, with an AnyState->Whiff transition gated on the `Whiff` trigger, and a Whiff->Idle exit transition (exitTime 0.9).
   - New `Roll` Trigger parameter exists, with an AnyState->Roll transition gated on the `Roll` trigger, coexisting with the pre-existing IsRolling bool-driven transitions.
5. Playtest: trigger a whiff (attack with no enemy in range) and a roll, confirm both animations now play (they previously silently failed to fire due to missing controller parameters).
6. Re-running the menu item a second time should be a no-op (idempotency check) -- verify no duplicate states/transitions appear.

No blockers for Plan 12-05.

---
*Phase: 12-animation-polish*
*Completed: 2026-07-08*

## Self-Check: PASSED

- FOUND: Assets/Editor/PlayerAnimatorPatcher.cs
- FOUND: commit a6fc240
