---
phase: 12-animation-polish
plan: 09
subsystem: enemy
tags: [unity, animator, bugfix, checkpoint, playtest]

# Dependency graph
requires:
  - phase: 12-animation-polish
    provides: "12-08 EnemyDeathEffect.cs, MeleeEnemy/RangedEnemy OnDashHit() wiring"
provides:
  - "Working enemy death sequence: Die animation -> particles -> SpriteMask fade -> Destroy"
affects: []

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Animator normalizedTime polling instead of fixed WaitForSecondsRealtime(clip.length) -- avoids scaled-vs-realtime clock mismatch with HitFreeze/slow-mo"

key-files:
  created: []
  modified:
    - Assets/Scripts/Enemy/EnemyDeathEffect.cs
    - Assets/Animations/Enemies/MeleeEnemyAnimator.controller
    - Assets/Animations/Enemies/RangedEnemyAnimator.controller
    - Assets/DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/Die.anim

key-decisions:
  - "canTransitionToSelf=false on AnyState->Die (Bool condition never resets, was re-triggering the transition every single frame and resetting normalizedTime to 0 forever)"
  - "Die.anim Loop Time disabled (was wrapping normalizedTime past 1.0 back to the clip start, making the enemy look like it 'got back up' during the particle/mask fade)"
  - "animator.speed = 0 once Die finishes once, to hold the final pose for the rest of the death sequence"
---

## What Was Done

Playtest surfaced a real bug in 12-08's death sequence, diagnosed and fixed in three steps (verified empirically via `EditorApplication.Step()` frame-by-frame tracing + live playtest, not just static code review):

1. **Original bug:** Die animation never visibly played (enemy looked frozen, then cut straight to particles/mask). Root cause: `AnyState -> Die` transition had `canTransitionToSelf = true`; since `isDead` is a persistent Bool (not a Trigger), the Animator re-satisfied the condition and re-entered Die every frame, pinning `normalizedTime` at 0 forever. Fixed by setting `canTransitionToSelf = false` on both `MeleeEnemyAnimator.controller` and `RangedEnemyAnimator.controller`.
2. **Regression from first fix attempt:** Replacing the `WaitForSecondsRealtime(dieLength)` wait with a `normalizedTime >= 1` poll (to fix a scaled/realtime clock mismatch with `HitFreeze`) hung forever, since normalizedTime could never reach 1 while bug #1 was still present.
3. **Second symptom after fixing #1:** Die.anim has `Loop Time` enabled, so once `normalizedTime` crossed 1.0 it wrapped back to the clip's start (Die01, a standing-like pose) before `EnemyDeathEffect` could freeze it -- looked like the enemy "got back up." Fixed by disabling `Loop Time` on `Die.anim` (confirmed safe: only other reference is an unused DeadRevolver demo controller, not the live `FastPlayerAnimator.controller`).

Final playtest confirmed:
- D-09: Die animation (Die01->Die09) plays in full, holds on Die09, then particles + SpriteMask fade play, then Destroy -- **통과**
- D-11: MeleeEnemy/RangedEnemy chase/telegraph/attack behavior unchanged from pre-Phase-12 -- **통과**

## Issues

None remaining. Unrelated bug noted during testing (out of Phase 12 scope): `RollController.cs:69` attempted to assign a `Rigidbody2D.linearVelocity` containing `-Infinity`, logged as a Unity error during a roll input. Flagged to user for separate follow-up, not touched here.

## Phase 12 Status

All 9 plans (12-01 through 12-09) complete and verified. Phase 12 (animation-polish) is done.
