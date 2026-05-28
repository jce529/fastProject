---
phase: 01-foundation-movement
plan: 01
subsystem: scene-setup
tags: [physics-layers, scene-layout, camera, unity-yaml]
dependency_graph:
  requires: []
  provides:
    - Platform layer (6), PlayerHurtbox layer (7), PlayerInvincible layer (8)
    - SampleScene with platform + fall triggers + Player placeholder
    - CameraFollow script (LateUpdate, no smoothing)
  affects:
    - 01-02 (PlayerController needs Platform layer and Player GameObject)
    - 01-03 (FallDetector needs FallZone triggers and Player GameObject)
tech_stack:
  added: []
  patterns:
    - Unity YAML scene editing via text
    - CameraFollow via LateUpdate (no Cinemachine)
key_files:
  created:
    - Assets/Scripts/Camera/CameraFollow.cs
    - Assets/Scripts.meta
    - Assets/Scripts/Camera.meta
    - Assets/Scripts/Camera/CameraFollow.cs.meta
  modified:
    - ProjectSettings/TagManager.asset
    - ProjectSettings/Physics2DSettings.asset
    - Assets/Scenes/SampleScene.unity
decisions:
  - "LateUpdate camera follow per D-11/D-12/D-13 -- no Cinemachine, no lead-ahead, no smoothing"
  - "Physics layer collision matrix: PlayerHurtbox (7) and PlayerInvincible (8) cannot collide with each other"
  - "Platform collider uses Transform.localScale (24x0.8) rather than BoxCollider2D.size -- single unit sprite scales to cover 24 units"
metrics:
  duration_minutes: 4
  completed_date: "2026-05-28"
  tasks_completed: 2
  files_modified: 7
---

# Phase 01 Plan 01: Scene Foundation & Physics Layers Summary

**One-liner:** Physics layers (Platform/PlayerHurtbox/PlayerInvincible) configured, SampleScene built with gray platform + fall triggers + Player placeholder, CameraFollow attached to Main Camera via LateUpdate with fixed offset.

---

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Configure physics layers and build SampleScene test layout | 3e98eb5 | TagManager.asset, Physics2DSettings.asset, SampleScene.unity |
| 2 | Implement CameraFollow script (LateUpdate, no lead-ahead) | fe46f3d | CameraFollow.cs, CameraFollow.cs.meta, Scripts.meta, Camera.meta, SampleScene.unity |

---

## What Was Built

### Physics Layers (TagManager.asset)

Slots 6, 7, 8 added to `ProjectSettings/TagManager.asset`:
- Layer 6: `Platform`
- Layer 7: `PlayerHurtbox`
- Layer 8: `PlayerInvincible`

Layers 0-5 (built-ins) preserved unchanged.

### Physics2D Collision Matrix (Physics2DSettings.asset)

Updated `m_LayerCollisionMatrix` so that PlayerHurtbox (7) and PlayerInvincible (8) do not collide with each other. All other layer pairs remain colliding by default. This prevents self-collision on the player object when layers are swapped for invincibility.

### SampleScene Layout

Scene hierarchy added to `Assets/Scenes/SampleScene.unity`:

```
Environment/                       (fileID 100000001 - empty container)
  Platform                         (fileID 100000010, Layer=6)
    Transform: pos(0,-3,0), scale(24,0.8,1)
    SpriteRenderer: gray (#808080), Unity default square sprite
    BoxCollider2D: size(1,1) -- effective 24x0.8 world units via scale
  FallZone_Left                    (fileID 100000020, Layer=0)
    Transform: pos(-14,-10,0)
    BoxCollider2D: size(4,20), IsTrigger=true
  FallZone_Right                   (fileID 100000030, Layer=0)
    Transform: pos(14,-10,0)
    BoxCollider2D: size(4,20), IsTrigger=true
Player                             (fileID 100000040, Tag=Player)
  Transform: pos(0,-2,0)
  SpriteRenderer: white, sorting order 1
Main Camera                        (fileID 519420028)
  Camera: orthographic, size=5.4
  CameraFollow component wired (target = Player Transform)
```

### CameraFollow.cs

`Assets/Scripts/Camera/CameraFollow.cs` — 14 lines. Direct position assignment in `LateUpdate`. No Cinemachine, no Lerp, no SmoothDamp. Fixed offset `(0, 1, -10)`.

---

## Decisions Made

1. **LateUpdate follow (D-11, D-12, D-13):** Camera update runs after physics to eliminate jitter. No smoothing ensures precise tracking. No Cinemachine to avoid Unity 6 API uncertainty.

2. **Collision matrix: PlayerHurtbox never collides with PlayerInvincible:** Same player object swaps layers for i-frame state. Self-collision between the two player layers would cause physics noise — disabled explicitly.

3. **Platform collider via Transform scale:** The BoxCollider2D has default size (1,1); the Transform scale (24, 0.8, 1) makes the effective collision area 24x0.8 world units. This is Unity's standard approach for box primitives using the default square sprite.

---

## Deviations from Plan

None - plan executed exactly as written.

---

## Known Stubs

- **Player GameObject** (fileID 100000040 in SampleScene.unity): Placeholder only. Has SpriteRenderer but no Rigidbody2D, no collider, no PlayerController script. Full implementation deferred to Plan 02 (PlayerController) and Plan 03 (FallDetector).
- **CameraFollow.target:** Wired to Player Transform fileID 100000042 in the scene YAML. Will work when Player has a proper Rigidbody2D and moves. No functional gap for Plan 01.

---

## Self-Check: PASSED

Files verified:
- `ProjectSettings/TagManager.asset` — contains Platform, PlayerHurtbox, PlayerInvincible
- `ProjectSettings/Physics2DSettings.asset` — collision matrix updated
- `Assets/Scenes/SampleScene.unity` — contains Platform, FallZone_Left, FallZone_Right, Player (Tag=Player), CameraFollow MonoBehaviour
- `Assets/Scripts/Camera/CameraFollow.cs` — exists, contains LateUpdate and target.position + offset
- Commits 3e98eb5 and fe46f3d confirmed in git log
