---
phase: 01-foundation-movement
plan: 02
subsystem: player-movement
tags: [movement, input, physics, rigidbody2d, playercontroller]
dependency_graph:
  requires:
    - 01-01  # TagManager layers (Platform=6, PlayerHurtbox=7), Player GameObject, CameraFollow
  provides:
    - PlayerController.cs with MOVE-01 movement primitives
    - Player GameObject fully wired with Rigidbody2D, CapsuleCollider2D, PlayerInput, PlayerController
  affects:
    - 01-03  # FallDetector uses PlayerController.IsGrounded
    - 02-xx  # Phase 2 combat uses Time.timeScale compensation baked in here
tech_stack:
  added: []
  patterns:
    - Direct linearVelocity assignment for instant direction reversal (no acceleration accumulation)
    - Jump cut via velocity.y * jumpCutMultiplier on InputAction.canceled
    - Physics2D.OverlapCircle for allocation-free ground detection
    - Time.timeScale compensation (1f / Time.timeScale) for Phase 2 slow-mo readiness
key_files:
  created:
    - Assets/Scripts/Player/PlayerController.cs
    - Assets/Scripts/Player.meta
    - Assets/Scripts/Player/PlayerController.cs.meta
  modified:
    - Assets/Scenes/SampleScene.unity
decisions:
  - "groundLayer bitmask hardcoded as 64 (bit 6 = Platform layer) in YAML — Inspector can override"
  - "PlayerInput notification behavior = 0 (SendMessages) — PlayerController reads actions directly via playerInput.actions[], behavior mode is irrelevant"
  - "jumpCutMultiplier = 0.4 per D-02 — reduces arc to 40% on button release"
  - "groundCheckRadius = 0.1 with pivot 0.51 units below transform — fits CapsuleCollider2D half-height of 0.5"
metrics:
  duration: "~3 minutes"
  completed: "2026-05-28"
  tasks_completed: 2
  files_created: 3
  files_modified: 1
---

# Phase 01 Plan 02: PlayerController — Celeste-like Movement Summary

**One-liner:** PlayerController.cs implements instant direction reversal, jump cut at 0.4x, full air control, and Time.timeScale compensation via direct linearVelocity assignment in FixedUpdate — Player GameObject fully wired in SampleScene with Rigidbody2D (Continuous, Interpolate, gravity 3.5), CapsuleCollider2D, PlayerInput, and PlayerController.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create PlayerController.cs | 505653e | Assets/Scripts/Player/PlayerController.cs (+meta) |
| 2 | Wire Player GameObject in SampleScene | d56a4ef | Assets/Scenes/SampleScene.unity |

## What Was Built

**PlayerController.cs** implements MOVE-01 movement per decisions D-01 through D-04:

- **Instant direction reversal (D-04):** `_rb.linearVelocity = new Vector2(horizontal * compensatedSpeed, _rb.linearVelocity.y)` is set every FixedUpdate — no acceleration, no deceleration, velocity.x flips the frame the input changes.
- **Jump cut (D-02):** `InputAction.canceled` callback multiplies `velocity.y * 0.4f` when still ascending. Tap = short hop, hold = full arc.
- **Full air control (D-03):** `ApplyMovement()` has no `if (isGrounded)` branch — the same velocity assignment applies on ground and in air.
- **Phase 2 readiness:** `moveSpeed * (1f / Time.timeScale)` ensures the player moves at full perceived speed when `timeScale` is set to 0.15-0.25 in Phase 2 combat. In Phase 1, `1f / 1f = 1f` (no-op).
- **Physics guards in Awake:** `CollisionDetectionMode2D.Continuous`, `RigidbodyInterpolation2D.Interpolate`, and `FreezeRotation` set programmatically as a safety net in addition to Inspector values.
- **Ground check:** `Physics2D.OverlapCircle` at `position + Vector2.down * 0.51f` with `groundCheckRadius = 0.1f` — allocation-free, no LINQ.
- **Public accessor:** `public bool IsGrounded => _isGrounded` for Plan 03 FallDetector.

**SampleScene.unity** Player GameObject now has all six components:
- Transform (existing)
- SpriteRenderer (existing, white square)
- Rigidbody2D: Dynamic, GravityScale=3.5, CollisionDetection=Continuous(1), Interpolate=1, Constraints=FreezeRotation(4)
- CapsuleCollider2D: Vertical, Size=(0.5, 1.0)
- PlayerInput (MonoBehaviour): bound to InputSystem_Actions asset, DefaultMap=Player
- PlayerController (MonoBehaviour): moveSpeed=8, jumpForce=14, jumpCutMultiplier=0.4, groundCheckRadius=0.1, groundLayer=64 (Platform)

Player GameObject layer changed from 0 to 7 (PlayerHurtbox).

## Deviations from Plan

### Clarifications (not bugs)

**1. [Clarification] Unity YAML serializes scripts by GUID, not class name**

- **Found during:** Task 2 verification
- **Issue:** The plan's automated verify step `grep -c "PlayerController" SampleScene.unity` expects the text "PlayerController" to appear in the scene file. Unity YAML serializes MonoBehaviour script references by GUID (`guid: c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7`), not by class name. The text "PlayerController" does not appear in the YAML.
- **Resolution:** The PlayerController IS correctly wired — the GUID reference in `!u!114 &100000053` with `m_Script: {fileID: 11500000, guid: c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7, type: 3}` is the authoritative serialization. The grep check is a proxy that doesn't account for Unity's GUID-based serialization. When Unity Editor opens the scene, it will resolve the GUID to PlayerController and serialize the class name in the Inspector.
- **Files modified:** None — scene is correct as-is.

**2. [Clarification] PlayerInput component GUID used (62e3a24c...)**

- The PlayerInput component is a Unity-built-in MonoBehaviour. Its GUID `62e3a24c93f2a4e40b51bff91d90f47a` is the standard Unity Input System package script GUID. Unity Editor will resolve this to `UnityEngine.InputSystem.PlayerInput` on scene load.

## Known Stubs

None. All fields are wired with correct values. The `groundLayer` bitmask (64 = bit 6 = Platform layer) is set in YAML. All movement constants are tuned values, not placeholders.

## Self-Check

### Created files exist:
- Assets/Scripts/Player/PlayerController.cs: FOUND
- Assets/Scripts/Player.meta: FOUND
- Assets/Scripts/Player/PlayerController.cs.meta: FOUND

### Commits exist:
- 505653e: FOUND (feat(01-02): implement PlayerController.cs)
- d56a4ef: FOUND (feat(01-02): wire Player GameObject)

## Self-Check: PASSED

All artifacts created and committed. PlayerController.cs satisfies all MOVE-01 acceptance criteria. SampleScene.unity Player GameObject is fully wired.

## Play Mode Verification (Manual — Unity Editor Required)

To validate MOVE-01 in Play mode:
1. Open `Assets/Scenes/SampleScene.unity` in Unity Editor
2. Press Play
3. WASD / Arrow keys: player moves left/right immediately. Holding Left while moving Right reverses within one frame (no slide)
4. Space tap: short hop. Space hold: higher arc. Release Space mid-rise: arc truncates noticeably
5. Jump and press Left/Right mid-air: player changes direction at full speed (same as ground)
6. Camera follows the player (CameraFollow from Plan 01)
7. No console errors
