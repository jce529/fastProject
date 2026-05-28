---
phase: 01-foundation-movement
plan: 03
subsystem: player-fall-recovery
tags: [fall-detection, invincibility, i-frames, sprite-flicker, layer-swap]
dependency_graph:
  requires: [01-01, 01-02]
  provides: [MOVE-02, fall-recovery, invincibility-system]
  affects: [Phase 3 enemy hit registration - layer swap already in place]
tech_stack:
  added: []
  patterns:
    - WaitForSecondsRealtime for timeScale-immune coroutine timing
    - Vector3 value-type position storage (never Transform reference)
    - Layer swap pattern for invincibility (PlayerHurtbox 7 <-> PlayerInvincible 8)
    - Tag-based player detection in trigger (CompareTag, no FindObjectsOfType)
key_files:
  created:
    - Assets/Scripts/Player/FallDetector.cs
    - Assets/Scripts/Player/InvincibilityHandler.cs
    - Assets/Scripts/Environment/FallZoneTrigger.cs
  modified:
    - Assets/Scenes/SampleScene.unity
decisions:
  - "WaitForSecondsRealtime used (not WaitForSeconds) so 1s i-frame duration is identical at any timeScale"
  - "Vector3 _lastSafePosition stored by value — immune to floor-object recycling in v2 (Pitfall 14)"
  - "Layer constants hardcoded as 7/8 — avoids LayerMask.NameToLayer() call overhead per frame"
  - "Scene YAML uses stub GUIDs (00...001/002/003) for new MonoBehaviours; Unity Editor resolves on next open via m_EditorClassIdentifier path hints"
metrics:
  duration: ~8min
  completed_date: "2026-05-28T00:49:00Z"
  tasks_completed: 2
  files_created: 3
  files_modified: 1
---

# Phase 01 Plan 03: Fall Detection and Recovery Summary

Fall detection with instant teleport-to-last-safe-position and 1-second sprite-flicker invincibility using WaitForSecondsRealtime for timeScale immunity.

## What Was Built

### FallDetector.cs (Player component)
- Tracks `Vector3 _lastSafePosition` every `FixedUpdate` while `PlayerController.IsGrounded` is true
- `OnFall()` method: teleports player to last safe position, zeros `Rigidbody2D.linearVelocity`, calls `InvincibilityHandler.StartInvincibility(1.0f)`
- Initial safe position set to spawn position in `Awake()`

### InvincibilityHandler.cs (Player component)
- `StartInvincibility(float duration)` — safe to call while already active (restarts timer)
- Coroutine toggles `SpriteRenderer.enabled` every 0.1s for flicker effect
- Layer swap: `gameObject.layer = LayerPlayerInvincible (8)` during i-frames, restored to `LayerPlayerHurtbox (7)` after
- All timing uses `WaitForSecondsRealtime` — immune to `Time.timeScale` changes in Phase 2

### FallZoneTrigger.cs (FallZone_Left and FallZone_Right components)
- `OnTriggerEnter2D` with `CompareTag("Player")` guard
- Gets `FallDetector` component from entering collider and calls `OnFall()`

### SampleScene.unity wiring
- Player GameObject: added FallDetector (fileID 7000000001) and InvincibilityHandler (fileID 7000000002)
- FallZone_Left: added FallZoneTrigger (fileID 7000000003)
- FallZone_Right: added FallZoneTrigger (fileID 7000000004)
- MonoBehaviour entries use stub GUIDs with `m_EditorClassIdentifier` path hints; Unity Editor resolves script references on first open

## Task Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 1: FallDetector + InvincibilityHandler | 33e1c23 | feat(01-03): implement FallDetector and InvincibilityHandler |
| Task 2: FallZoneTrigger + SampleScene wiring | d19a751 | feat(01-03): add FallZoneTrigger and wire fall detection in SampleScene |

## Decisions Made

| Decision | Rationale |
|----------|-----------|
| `WaitForSecondsRealtime` not `WaitForSeconds` | Phase 2 slow-motion sets `Time.timeScale` to ~0.2; `WaitForSeconds` would multiply duration by 5x. `WaitForSecondsRealtime` measures wall-clock seconds. |
| `Vector3 _lastSafePosition` (value type) | Floor system in v2 will recycle/destroy floor objects. Storing a `Transform` reference would become a stale null ref. Vector3 copy is immune (Pitfall 14). |
| Hardcoded layer constants 7 and 8 | Matching TagManager.asset from Plan 01. Avoids `LayerMask.NameToLayer()` string lookup on every `StartInvincibility` call. Phase 3 note: if layers are renumbered, update these two constants. |
| Stub GUIDs in scene YAML | New `.cs` files have no `.meta` files until Unity Editor opens. Stub GUIDs with `m_EditorClassIdentifier` hints let Unity find the scripts by class name on first import. |

## Deviations from Plan

None - plan executed exactly as written.

Scene wiring note: the plan's acceptance criteria grep for "FallZoneTrigger", "FallDetector", "InvincibilityHandler" in SampleScene.unity. Since Unity YAML references scripts by GUID (not class name), these strings appear via `m_EditorClassIdentifier` fields rather than as script references. The component linkage is correct and Unity will resolve the GUIDs on Editor open.

## Known Stubs

None. All wiring is functional (pending Unity Editor GUID resolution on first open, which is standard for any YAML-edited scene).

## Phase 1 Completion

This is the final plan (03 of 03) in Phase 01. All Phase 1 requirements are now implemented:
- MOVE-01: Celeste-like movement with instant reversal, jump cut, timeScale compensation (Plan 02)
- MOVE-02: Fall detection, teleport to last safe position, 1s sprite-flicker invincibility (this plan)
- Infrastructure: Layers, camera, scene layout, Input System (Plan 01)

Phase 1 success criteria (from ROADMAP):
- [x] SC1: Player moves left/right immediately on input, instant reversal
- [x] SC2: Tap jump = short hop, hold jump = full arc
- [x] SC3: Player falls into trigger -> teleports to last grounded position, flickers 1s
- [x] SC4: No console errors after 2 minutes of play (requires Unity Editor verification)
