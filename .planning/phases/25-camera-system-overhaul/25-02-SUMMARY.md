---
phase: 25-camera-system-overhaul
plan: 02
subsystem: combat
tags: [unity, camera, dash, overclock, dynamic-zoom, aim-lead]

# Dependency graph
requires:
  - phase: 25-camera-system-overhaul
    plan: 01
    provides: "CameraFollow.SetAimLeadSuppressed(bool) / RequestDynamicZoom(float, float) / ReleaseDynamicZoom() public hooks"
provides:
  - "OverclockModule.Resolve() dash lifecycle wired to CameraFollow aim-lead suppression + dynamic zoom (D-04, D-05, D-08)"
affects: [25-03]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Dash speed signal computed as dashDistance/ctx.DashDuration (never Rigidbody2D.linearVelocity, which is pinned to zero during MovePosition-driven dash — Pitfall 1)"
    - "ctx.CameraFollow?. null-conditional chaining matches the existing ctx.CameraFollow?.Shake(...) contract — safe when no camera is present (DebugScene/tests)"

key-files:
  created: []
  modified:
    - Assets/Scripts/Player/Combat/OverclockModule.cs

key-decisions:
  - "Inserted all 4 new call sites (dashDistance/dashSpeed calc + 3 camera hook calls) purely additively — zero characters changed in any pre-existing line, per plan's explicit 'additive only' directive"
  - "No deviations from plan — code blocks (A)-(D) inserted verbatim at the exact anchor points specified"

patterns-established: []

requirements-completed: [D-04, D-05, D-08]

# Metrics
duration: 5min
completed: 2026-08-07
---

# Phase 25 Plan 02: Overclock Dash-to-Camera Wiring Summary

**Wired OverclockModule.Resolve()'s three dash lifecycle points (dash start, dash move completion, HitFreeze end) to Plan 25-01's CameraFollow hooks — aim-lead suppression during dash, dynamic zoom sized by real dash distance/speed (not the zeroed Rigidbody2D velocity), and zoom release right after HitFreeze.**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-08-07T02:29:24Z (session resume, per STATE.md)
- **Completed:** 2026-08-07T02:32:42Z
- **Tasks:** 1
- **Files modified:** 1

## Accomplishments
- Added `dashDistance`/`dashSpeed` calculation immediately after `dirToTarget`, using `Vector2.Distance(startPos, destination)` and `dashDistance / ctx.DashDuration` (guarded against `DashDuration <= 0`) — deliberately avoids `Rigidbody2D.linearVelocity`, which is forced to `Vector2.zero` for the duration of the `MovePosition`-driven dash loop (Pitfall 1 from 25-RESEARCH.md)
- `ctx.CameraFollow?.SetAimLeadSuppressed(true)` inserted right after `ctx.Rb.linearVelocity = Vector2.zero;`, before the dash movement while-loop begins (D-04: lead offset drops the instant the dash starts)
- `ctx.CameraFollow?.SetAimLeadSuppressed(false)` and `ctx.CameraFollow?.RequestDynamicZoom(dashDistance, dashSpeed)` inserted right after the final `ctx.Rb.MovePosition(destination);` (post-loop position lock), before the "Cleanup visual and animation" comment (D-04/D-05: lead resumes and zoom-out triggers the instant the dash motion is complete)
- `ctx.CameraFollow?.ReleaseDynamicZoom()` inserted right after `yield return HitFreeze(ctx.HitFreezeDuration);`, before `ctx.SetAttackCooldown(...)` (D-08: zoom-in begins the instant HitFreeze ends)
- All other logic in `Resolve()` (sprite flip, animator, invincibility, trail renderer, `target.OnDashHit()`, `AudioManager.PlaySfx`, `SpawnHitSpark`, the existing `Shake()` call, `SetAttackCooldown`, `Gauge.AddKillBonus`) and all other methods (`FindTarget`, `Whiff`, `HitFreeze`, `SpawnHitSpark`, `GetMouseWorldDirection`, `IsInAttackShape`) are byte-for-byte unchanged — confirmed via `git show` diff showing only 4 added lines (2 calc + hooks), zero removed/modified lines

## Task Commits

Each task was committed atomically:

1. **Task 1: 대시 생명주기 3개 지점에 카메라 훅 배선 (D-04, D-05, D-08)** - `d95f2e8` (feat)

**Plan metadata:** (this commit) - `docs(25-02): complete overclock camera wiring plan`

## Files Created/Modified
- `Assets/Scripts/Player/Combat/OverclockModule.cs` - Added dash distance/speed calc + 3 camera hook calls to `Resolve()`; no other changes

## Decisions Made
- Followed the plan's four insertion points (A/B/C/D) verbatim — no architectural or logic deviations were needed since Plan 25-01 already delivered stable, matching public API signatures

## Deviations from Plan

None — plan executed exactly as written. All code blocks were inserted verbatim as specified in the plan's `<action>` section.

**Note on verification tally discrepancy (non-functional, documentation-only):** The plan's `<verification>` section states `ctx.CameraFollow?.` should appear in "총 4곳 (Shake 1 + 신규 3)". Actual grep count is 5, because `SetAimLeadSuppressed` is called twice (once `true` at dash start, once `false` at dash move completion) — the plan's own tally undercounted this by one, counting `SetAimLeadSuppressed` as a single call site rather than two invocations. This is the same class of counting-only documentation slip noted in 25-01-SUMMARY.md's Task 1 acceptance criteria; no code change was made since the actual call count (existing `Shake` ×1 + new `SetAimLeadSuppressed` ×2 + `RequestDynamicZoom` ×1 + `ReleaseDynamicZoom` ×1 = 5) is exactly what D-04/D-05/D-08 require.

## Issues Encountered
None.

## User Setup Required

None — no external service configuration required. Compilation should be verified in Unity Editor (no C# errors expected — `CameraFollow.SetAimLeadSuppressed(bool)` / `RequestDynamicZoom(float, float)` / `ReleaseDynamicZoom()` all confirmed present with matching signatures in `Assets/Scripts/Camera/CameraFollow.cs` before this plan started editing).

## Next Phase Readiness
- D-04 (aim lead suppression during dash), D-05 (dynamic zoom sized by real dash distance/speed), and D-08 (zoom release timing relative to HitFreeze) are now fully wired into the live F.I.O.R.A dash-kill combat loop
- `CameraFollow.cs` (Plan 25-01's file) was not touched in this plan — file ownership separation maintained
- Recommend a Unity Editor Play-mode pass in DebugScene (dash into FioraBoss or a regular enemy) to visually confirm: lead offset vanishes during dash, camera zooms out proportional to dash distance/speed right as the dash lands, and zoom returns to room size right after the HitFreeze flinch — not a formal checkpoint in this plan, but a low-cost sanity check before Phase 25 is considered feature-complete
- No blockers for Plan 25-03 (if any remains) or Phase 25 wrap-up

---
*Phase: 25-camera-system-overhaul*
*Completed: 2026-08-07*

## Self-Check: PASSED

- FOUND: Assets/Scripts/Player/Combat/OverclockModule.cs
- FOUND: .planning/phases/25-camera-system-overhaul/25-02-SUMMARY.md
- FOUND commit: d95f2e8
