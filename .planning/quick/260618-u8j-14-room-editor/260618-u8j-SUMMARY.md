---
quick_id: 260618-u8j
type: summary
date: 2026-06-18
files_created:
  - Assets/Editor/RoomPrefabBuilder.cs
commit: 98309c2
duration_minutes: ~15
---

# Quick Task 260618-u8j: RoomPrefabBuilder Editor Script

**One-liner:** Static Unity Editor script (Fast/Build Room Prefabs menu) that procedurally generates 14 Room prefabs with typed geometry, kill zones, and spawn markers from hardcoded design specs.

## What Was Built

`Assets/Editor/RoomPrefabBuilder.cs` — pure `static class` + `[MenuItem]` pattern (matching `SetupPhase1Animator.cs`).

Running **Fast/Build Room Prefabs** in the Unity Editor creates all 14 `.prefab` files under `Assets/Prefabs/Rooms/[RoomName]/[RoomName].prefab`.

### Room coverage

| Room | Geometry | KillZone | Enemy Spawns | Special |
|------|----------|----------|--------------|---------|
| Room_Combat | floor + 2 walls | - | 3 melee | - |
| Room_Hunt | floor + 3 platforms | - | 3 mixed | - |
| Room_Ladder | floor + ceiling + 2 walls | - | - | [Ladder] marker |
| Room_LadderDanger | floor + ceiling + 2 walls + 2 danger platforms | - | 2 mixed | colored hint platform |
| Room_Gap | 4 gap platforms | yes | - | - |
| Room_Fall | 3 height-varied platforms | yes | - | - |
| Room_Sniper | floor + cover + high ground | - | 3 mixed | - |
| Room_Stair | 5 ascending steps | - | - | - |
| Room_Crossroad | entry/exit floor + upper path + 3 lower platforms | yes | 3 melee (upper) | - |
| Room_Chase | floor + ceiling (corridor) | - | BossChaseSpawn | - |
| Room_Dodge | closed box (floor + ceiling + 2 walls) | - | BossDodgeSpawn | - |
| Room_Chain | 4 ascending steps | - | 4 melee (1 per step) | - |
| Room_Recovery | floor + 2 upper platforms | - | 2 mixed | 2 [MovingPlatform] markers (blue tint) |
| Room_Mixed | multi-path (upper+lower gap+chain stairs) | yes | 4 mixed + MixedBossSpawn | - |

### Helper methods

- `CreateMarker` — empty GameObject, no collider
- `CreatePlatform` — BoxCollider2D + SpriteRenderer(UISprite, Sliced drawMode, sized), layer=9
- `CreateKillZone` — BoxCollider2D(isTrigger=true) + FallZoneTrigger component
- `CreateMovingPlatformMarker` — platform with blue tint SpriteRenderer (placeholder for moving platform logic)
- `SavePrefab` — deletes existing prefab if present (idempotent), saves, DestroyImmediate root

## Key Implementation Notes

- `SpriteRenderer.drawMode = SpriteDrawMode.Sliced` set BEFORE `.size` assignment (required by Unity API)
- `AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")` used for all sprites
- `PLATFORM_LAYER = 9` matches TagManager.asset index (Platform entry)
- All KillZone rooms (Gap, Fall, Crossroad, Mixed) have `FallZoneTrigger` attached
- BossChaseSpawn / BossDodgeSpawn / MixedBossSpawn use custom names per spec
- Idempotent: each BuildRoom_xxx() deletes existing prefab before recreating

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check

- [x] `Assets/Editor/RoomPrefabBuilder.cs` written (455 lines)
- [x] Commit `98309c2` exists
- [x] 14 `BuildRoom_*` methods covering all rooms
- [x] KillZone rooms: Gap, Fall, Crossroad, Mixed — all call `CreateKillZone` with `FallZoneTrigger`
- [x] Pattern matches `SetupPhase1Animator.cs` (static class, no EditorWindow, MenuItem)
