---
phase: 25-camera-system-overhaul
plan: 01
subsystem: camera
tags: [unity, camera, smoothdamp, aim-lead, dynamic-zoom, tension-catchup]

# Dependency graph
requires:
  - phase: 18-shared-infra
    provides: base CameraFollow.cs (SnapToRoom snap-follow, bounds clamp, additive shake) and CameraBound.cs
provides:
  - "CameraFollow.RequestDynamicZoom(distance, speed) / ReleaseDynamicZoom() public hooks (D-05~D-08)"
  - "CameraFollow.SetAimLeadSuppressed(bool) public hook (D-04)"
  - "SmoothDamp-based position tracking with mouse-direction aim lead offset (D-01~D-03) and exponential tension catch-up (D-09/D-10)"
affects: [25-02-overclock-camera-wiring, 25-03]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "Fixed LateUpdate composition order: zoom SmoothDamp -> lead+tension+position SmoothDamp -> bounds clamp -> additive shake"
    - "All new SmoothDamp calls pass Time.unscaledDeltaTime explicitly (hitfreeze/timeScale=0 immunity convention)"
    - "Dual-oscillator design: lead offset and position tracking use independent Vector2/Vector3 SmoothDamp state, composed before bounds clamp reads the summed transform.position"

key-files:
  created: []
  modified:
    - Assets/Scripts/Camera/CameraFollow.cs

key-decisions:
  - "Followed 25-RESEARCH.md recommended LateUpdate order verbatim to satisfy Pitfall 2 (bounds clamp must read post-SmoothDamp orthographicSize, not pre-update value)"
  - "leadSmoothTime defaults to 0 (instant response) per D-03 — lead offset itself doesn't lag, only the composed position does via positionSmoothTime"
  - "GetMouseWorldDirection() duplicated locally (not extracted to shared util) to avoid CombatContext coupling — mirrors OverclockModule's existing pattern per plan's explicit CLAUDE.md minimal-surface-area directive"

patterns-established:
  - "Dynamic zoom target (_zoomTargetSize) reset to roomOrthoSize inside both SnapToRoom overloads to prevent zoom creep across room-to-room teleports"

requirements-completed: [D-01, D-02, D-03, D-04, D-05, D-06, D-07, D-08, D-09, D-10]

# Metrics
duration: 10min
completed: 2026-08-07
---

# Phase 25 Plan 01: Camera SmoothDamp Tracking + Aim Lead + Tension Catch-up + Dynamic Zoom Summary

**CameraFollow.cs rewritten from instant-snap LateUpdate to a single SmoothDamp pipeline combining mouse-direction aim lead offset, viewport-edge exponential tension catch-up, and dash-driven asymmetric dynamic zoom — three new public hooks (SetAimLeadSuppressed/RequestDynamicZoom/ReleaseDynamicZoom) ready for 25-02 to wire into OverclockModule's dash lifecycle.**

## Performance

- **Duration:** 10 min
- **Started:** 2026-08-07T02:20:00Z (approx, per STATE.md session start)
- **Completed:** 2026-08-07T02:28:34Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Dynamic zoom system (D-05~D-08): `RequestDynamicZoom(distance, speed)` computes a distance+speed blended zoom target (max 9 vs room default 7), asymmetric SmoothDamp (0.1s zoom-out, 0.5s zoom-in), `ReleaseDynamicZoom()` returns to room size
- Aim lead offset (D-01~D-04): mouse-direction offset up to 15-25% of screen half-width, independent SmoothDamp (default instant, `leadSmoothTime = 0`), suppressible via `SetAimLeadSuppressed(bool)`
- Tension catch-up (D-09/D-10): exponential SmoothTime reduction (down to 0.03s) only when target enters the outer 25% viewport bands, no intervention inside the center 50%
- Position tracking converted from direct assignment to `Vector3.SmoothDamp`, composed with lead offset before the existing bounds-clamp block (which now reads post-SmoothDamp `transform.position` instead of the raw `desired` value)
- `SnapToRoom(Vector3)`/`SnapToRoom(Bounds)` signatures and instant-snap behavior preserved byte-for-byte (DebugSceneCameraBinder.cs regression guard); both now also reset `_zoomTargetSize` to prevent zoom creep across room transitions

## Task Commits

Each task was committed atomically:

1. **Task 1: Dynamic Zoom System (D-05~D-08)** - `d701602` (feat)
2. **Task 2: Aim Lead Offset + Tension Catch-up + SmoothDamp Position Tracking (D-01~D-04, D-09~D-10)** - `96112a7` (feat)

**Plan metadata:** (this commit) - `docs(25-01): complete camera SmoothDamp tracking plan`

## Files Created/Modified
- `Assets/Scripts/Camera/CameraFollow.cs` - Full LateUpdate rewrite: zoom SmoothDamp -> lead+tension+position SmoothDamp -> bounds clamp -> shake, plus 3 new public hooks and 3 new private helper methods

## Decisions Made
- Followed 25-RESEARCH.md's recommended fixed LateUpdate execution order (zoom -> lead/tension/position -> bounds clamp -> shake) exactly as specified — no deviation needed
- Kept `GetMouseWorldDirection()` as a private method local to CameraFollow (duplicating OverclockModule's pattern) rather than extracting a shared utility, per plan's explicit instruction to avoid CombatContext coupling and keep the change surface to a single file

## Deviations from Plan

None - plan executed exactly as written. All code blocks were inserted verbatim as specified in the plan's `<action>` sections.

**Note on acceptance criteria discrepancy (non-functional, documentation-only):** Task 1's acceptance criteria states `grep -c "_zoomTargetSize = roomOrthoSize"` should equal exactly 3 ("Awake 1회 + SnapToRoom(Vector3) 1회 + SnapToRoom(Bounds) 1회"). The actual count is 4, because `ReleaseDynamicZoom()` (added in the same task, per D-08) also contains the identical line `_zoomTargetSize = roomOrthoSize;`. This is correct, intended behavior (D-08 requires exactly this assignment) — the plan's verification tally simply omitted counting `ReleaseDynamicZoom()`'s occurrence when it wrote the expected count. No code change was made in response since the underlying functionality (Awake init + Release hook + both SnapToRoom resets) is exactly what D-05~D-08 requires; only the plan's own comment/tally undercounts by one.

## Issues Encountered
None.

## User Setup Required

None - no external service configuration required. Compilation should be verified in Unity Editor (no C# errors expected — all referenced APIs `Mathf.SmoothDamp`, `Vector2.SmoothDamp`, `Vector3.SmoothDamp`, `Camera.WorldToViewportPoint`, `UnityEngine.InputSystem.Mouse.current` are already used elsewhere in the codebase, e.g. OverclockModule.cs).

## Next Phase Readiness
- Three stable public hooks confirmed for 25-02: `SetAimLeadSuppressed(bool)`, `RequestDynamicZoom(float distance, float speed)`, `ReleaseDynamicZoom()`
- 25-02 can now wire OverclockModule's dash lifecycle (dash start -> SetAimLeadSuppressed(true) + RequestDynamicZoom, dash/hitfreeze end -> SetAimLeadSuppressed(false) + ReleaseDynamicZoom) without touching CameraFollow.cs again
- No blockers. Recommend a quick Unity Editor open + Play in DebugScene to visually confirm SmoothDamp tracking feels correct before 25-02 begins (not a formal checkpoint in this plan, but low-cost sanity check)

---
*Phase: 25-camera-system-overhaul*
*Completed: 2026-08-07*

## Self-Check: PASSED

- FOUND: Assets/Scripts/Camera/CameraFollow.cs
- FOUND: .planning/phases/25-camera-system-overhaul/25-01-SUMMARY.md
- FOUND commit: d701602 (Task 1)
- FOUND commit: 96112a7 (Task 2)
