---
phase: 08-room-corridor-architecture
plan: "01"
subsystem: world
tags: [unity, csharp, room-connector, prefab, gizmo, editor-tool]

requires: []
provides:
  - RoomConnector MonoBehaviour with Direction enum and Gizmo visualization
  - RoomMarkerTool editor script for attaching RoomConnector to Room prefabs
affects:
  - 08-02-corridor-prefabs (Corridor ENT may reuse RoomConnector)
  - 09-world-generator (queries GetComponentsInChildren<RoomConnector>() to build Room-Corridor chains)

tech-stack:
  added: []
  patterns:
    - "Lightweight marker MonoBehaviour: minimal class with serialized fields + OnDrawGizmos, no lifecycle methods"
    - "PrefabUtility.LoadPrefabContents / SaveAsPrefabAsset / UnloadPrefabContents for modifying existing prefabs in editor tools"
    - "Idempotent AddComponent pattern: check GetComponent != null before adding"

key-files:
  created:
    - Assets/Scripts/World/RoomConnector.cs
    - Assets/Editor/RoomMarkerTool.cs
  modified: []

key-decisions:
  - "RoomConnector attaches directly to existing ENT/EXIT child GameObjects rather than new marker objects — Transform.position is the connector point"
  - "connectedObject field left null in Phase 8 — Phase 9 WorldGenerator populates at runtime"
  - "OnDrawGizmos (not OnDrawGizmosSelected) so Gizmos are always visible without selecting the object"

patterns-established:
  - "Marker component pattern: class extends MonoBehaviour with no lifecycle methods, only serialized data + Gizmos"
  - "Editor tool idempotency: GetComponent check prevents double-attaching components on re-runs"

requirements-completed: [ARCH-01, ARCH-03]

duration: 10min
completed: 2026-06-29
---

# Phase 08 Plan 01: Room Connector Architecture Summary

**RoomConnector direction marker (Left/Right Gizmo) + RoomMarkerTool editor script that attaches connectors to ENT/EXIT of 4 Room prefabs via PrefabUtility API**

## Performance

- **Duration:** ~10 min
- **Started:** 2026-06-29
- **Completed:** 2026-06-29
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- RoomConnector.cs: lightweight MonoBehaviour with Direction enum, two serialized fields, and always-visible OnDrawGizmos sphere (blue=Left, green=Right)
- RoomMarkerTool.cs: MenuItem "Fast/Phase8/Add Room Connectors" iterates Room_Combat/Fall/Gap/Stair, attaches RoomConnector to ENT and EXIT child objects idempotently
- Phase 9 WorldGenerator can now call GetComponentsInChildren<RoomConnector>() to locate Left/Right connection points on any Room prefab

## Task Commits

Each task was committed atomically:

1. **Task 1: RoomConnector.cs** - `3fc850b` (feat)
2. **Task 2: RoomMarkerTool.cs** - `e411d30` (feat)

**Plan metadata:** _(docs commit to follow)_

## Files Created/Modified
- `Assets/Scripts/World/RoomConnector.cs` - Direction enum, serialized direction + connectedObject, OnDrawGizmos
- `Assets/Editor/RoomMarkerTool.cs` - Editor tool; LoadPrefabContents loop over 4 rooms, AddConnector helper with idempotency guard

## Decisions Made
- Used `OnDrawGizmos` instead of `OnDrawGizmosSelected` so connectors are always visible in Scene View without requiring selection
- `connectedObject` field remains null in Phase 8 by design — Phase 9 WorldGenerator fills it at runtime chain assembly
- Editor tool targets exactly 4 rooms (Room_Combat, Room_Fall, Room_Gap, Room_Stair) per CONTEXT.md D-02; remaining 10 rooms deferred to Phase 9

## Deviations from Plan
None - plan executed exactly as written.

## Issues Encountered
None.

## User Setup Required
After running the editor tool in Unity ("Fast > Phase8 > Add Room Connectors"), verify:
- Open any of Room_Combat/Fall/Gap/Stair prefabs in the scene
- Select the ENT child — Inspector should show RoomConnector with Direction: Left
- Select the EXIT child — Inspector should show RoomConnector with Direction: Right
- Blue/green spheres appear in Scene View at each connector position

## Known Stubs
- `connectedObject` field on all RoomConnector instances will be null until Phase 9 WorldGenerator assigns it. This is intentional and documented in the field XML comment — it does not block Phase 8 verification.

## Next Phase Readiness
- Phase 08-02 (Corridor prefabs) can reuse RoomConnector on Corridor ENT/EXIT using the same pattern
- Phase 09 WorldGenerator has a stable API surface: `GetComponentsInChildren<RoomConnector>()` returns Left/Right connectors for chain assembly
- No blockers

---
*Phase: 08-room-corridor-architecture*
*Completed: 2026-06-29*

## Self-Check: PASSED
- FOUND: Assets/Scripts/World/RoomConnector.cs
- FOUND: Assets/Editor/RoomMarkerTool.cs
- FOUND: .planning/phases/08-room-corridor-architecture/08-01-SUMMARY.md
- FOUND commit: 3fc850b (feat: RoomConnector.cs)
- FOUND commit: e411d30 (feat: RoomMarkerTool.cs)
