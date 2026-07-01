---
phase: quick
plan: 260701-k1e
subsystem: editor-tools
tags: [corridor, tilemap, collider, prefab-builder]
dependency_graph:
  requires: [RoomCreator.cs pattern]
  provides: [CorridorBuilder TilemapCollider2D prefabs]
  affects: [Assets/Prefabs/Corridors/Corridor_Flat, Assets/Prefabs/Corridors/Corridor_Up, Assets/Prefabs/Corridors/Corridor_Down]
tech_stack:
  added: [UnityEngine.Tilemaps, TilemapCollider2D]
  patterns: [NewCorridor/PRow/EnsureSolidTile — identical to RoomCreator helpers]
key_files:
  modified:
    - Assets/Editor/CorridorBuilder.cs
decisions:
  - "Layer 9 (Platform) moved inline to NewCorridor() — no longer a class-level const"
  - "EnsureSolidTile() logs a warning instead of throwing if TileSquare.png is absent — same as RoomCreator"
metrics:
  duration: ~5m
  completed: 2026-07-01
  tasks_completed: 1
  files_modified: 1
---

# Quick 260701-k1e: CorridorBuilder BoxCollider2D → TilemapCollider2D Summary

CorridorBuilder.cs rewritten to use Grid→Tilemap_Solid hierarchy with TilemapCollider2D, matching RoomCreator.cs exactly so WorldGenerator can handle Room and Corridor prefabs identically.

## What Changed

`CreatePlatform()` (BoxCollider2D + SpriteRenderer sliced) and the `Geometry` parent object were replaced with three helpers copied verbatim from RoomCreator:

- `NewCorridor(corridorName)` — creates Root/Grid/Tilemap_Solid chain, returns `(GameObject, Tilemap)` tuple
- `PRow(tm, tile, x0, x1, y)` — fills a horizontal row of tiles
- `EnsureSolidTile()` — loads or creates `Assets/Tiles/Tile_Solid.asset`

`Run()` now calls `EnsureSolidTile()` once and passes the tile to all three builders (matching RoomCreator's `BuildAll()` pattern).

## Connector Positions (tile-surface aligned)

| Corridor | ENT | EXIT |
|----------|-----|------|
| Flat | (-6, 1) | (6, 1) |
| Up | (-6, 1) | (8, 5) |
| Down | (-7, 5) | (7, 1) |

All positions are at `tile_y + 1` (top surface of lowest tile in each step).

## Unchanged

`CreateMarker()`, `AddConnector()`, `CreateSpawnPoint()`, `SavePrefab()` — no changes.

## Deviations from Plan

None — plan executed exactly as written.

## Commits

| Hash | Description |
|------|-------------|
| b0d9e0c | feat(quick-k1e): rewrite CorridorBuilder — BoxCollider2D → TilemapCollider2D |

## Self-Check: PASSED

- `Assets/Editor/CorridorBuilder.cs` — file exists and written correctly
- Commit b0d9e0c present in git log
- No `CreatePlatform()` method in final file
- No `PLATFORM_LAYER` const in final file
- `using UnityEngine.Tilemaps;` present
- `NewCorridor`, `PRow`, `EnsureSolidTile` helpers present
