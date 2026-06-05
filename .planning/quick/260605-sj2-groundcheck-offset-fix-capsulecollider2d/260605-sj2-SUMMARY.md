---
phase: quick
plan: 260605-sj2
subsystem: player-movement
tags: [ground-check, capsulecollider2d, bug-fix]
dependency_graph:
  requires: []
  provides: [IsGrounded-reliable]
  affects: [PlayerController.CheckGround, animator-state-machine]
tech_stack:
  added: []
  patterns: []
key_files:
  modified:
    - Assets/Scripts/Player/PlayerController.cs
decisions:
  - "0.05f offset chosen: keeps sample point just inside platform surface regardless of CapsuleCollider2D pivot alignment"
metrics:
  duration: "< 1 min"
  completed: "2026-06-05"
  tasks: 1
  files_modified: 1
---

# Quick 260605-sj2: CheckGround Offset Fix (CapsuleCollider2D) Summary

One-liner: Changed CheckGround origin offset from 0.51f to 0.05f so the overlap circle hits the platform surface instead of passing through empty space below it.

## What Was Done

`PlayerController.CheckGround()` used `Vector2.down * 0.51f` as the sample origin offset. With CapsuleCollider2D configured so its bottom edge is exactly at the pivot (`offset.y == half_height ~0.64f`), the sample point landed 0.51 units below the pivot — roughly 0.35 units below the capsule bottom — missing the platform collider entirely. `_isGrounded` was always false while standing, causing the falling animation to persist and blocking jump input.

Changed to `Vector2.down * 0.05f`: the sample point now sits 0.05 units below the pivot, well within the platform top surface. `_isGrounded` becomes true on contact.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Fix ground-check offset constant | 0e42dba | Assets/Scripts/Player/PlayerController.cs |

## Deviations from Plan

None — plan executed exactly as written. One constant changed, no other lines touched.

## Known Stubs

None.

## Self-Check: PASSED

- Assets/Scripts/Player/PlayerController.cs: contains `Vector2.down * 0.05f`
- Commit 0e42dba: exists
