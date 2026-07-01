---
phase: 09-infinite-gen-cleanup
plan: 01
subsystem: world
tags: [unity, editor-tool, room-connector, prefab, bidirectional]

requires:
  - phase: 08-room-corridor-architecture
    provides: RoomConnector.cs with Direction enum and Door/ENT hierarchy in room prefabs

provides:
  - RoomMarkerTool에 14개 룸 전체 + Door/ENT(Left) + Door/EXIT(Right) 멱등 부착 로직
  - Fast/Phase9/Add Room Connectors 에디터 메뉴

affects:
  - 09-02 (WorldGenerator.cs): FindConnector(Direction.Right)가 null 반환 없이 동작하는 전제 조건

tech-stack:
  added: []
  patterns:
    - "PrefabUtility.LoadPrefabContents + AddComponent + SaveAsPrefabAsset 멱등 패턴 유지"
    - "transform.Find('Door/EXIT') 경로로 계층 탐색"

key-files:
  created: []
  modified:
    - Assets/Editor/RoomMarkerTool.cs

key-decisions:
  - "Door/ENT, Door/EXIT 경로 사용 (Door 부모 하위 자식 탐색) — Phase 8 구조 유지"
  - "Room_Stair는 RoomNames에 포함하되 WorldGenerator 풀 제외는 09-03에서 처리"

patterns-established:
  - "에디터 툴은 멱등적으로 동작 — 이미 컴포넌트가 있으면 건너뜀"

requirements-completed: [GEN-01, GEN-02, GEN-03]

duration: 2min
completed: 2026-07-01
---

# Phase 09 Plan 01: RoomMarkerTool 14-Room Bidirectional Connector Summary

**RoomMarkerTool을 4개 룸 ENT(Left) 전용에서 14개 룸 전체 ENT(Left)+EXIT(Right) 양방향 멱등 부착 도구로 확장**

## Performance

- **Duration:** 2 min
- **Started:** 2026-07-01T06:53:35Z
- **Completed:** 2026-07-01T06:55:54Z
- **Tasks:** 1/1
- **Files modified:** 1

## Accomplishments

- RoomNames 배열을 4개에서 14개 전체 룸으로 확장 (Room_Hunt, Room_Ladder, Room_LadderDanger, Room_Sniper, Room_Crossroad, Room_Chase, Room_Dodge, Room_Chain, Room_Recovery, Room_Mixed 추가)
- Door/EXIT에 Direction.Right RoomConnector를 부착하는 AddConnector 호출 추가 — WorldGenerator.FindConnector(Direction.Right) null 방지
- 메뉴 경로 Fast/Phase8 → Fast/Phase9/Add Room Connectors 업데이트

## Task Commits

1. **Task 1: RoomMarkerTool.cs — 14개 룸 + Door/EXIT Right 커넥터 추가** - `8264e12` (feat)

**Plan metadata:** (docs commit follows)

## Files Created/Modified

- `Assets/Editor/RoomMarkerTool.cs` - 4개 룸 ENT-only → 14개 룸 ENT(Left)+EXIT(Right) 양방향, Phase9 메뉴 경로

## Decisions Made

- Door/ENT와 Door/EXIT 경로(슬래시 포함)를 사용 — Phase 8에서 확립된 Door 부모 오브젝트 계층 구조 유지
- Room_Stair는 RoomNames에 포함 — 커넥터 부착 자체는 문제없으나, Pitfall 5(ENT/EXIT ΔY 불일치)로 인해 WorldGenerator 풀 제외는 09-03 SampleScene 설정에서 별도 처리

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None.

## User Setup Required

Unity Editor에서 Fast/Phase9/Add Room Connectors 메뉴를 실행해야 실제 프리팹에 커넥터가 부착된다. 이 도구는 에디터 전용이며 자동으로 실행되지 않는다.

## Next Phase Readiness

- 09-02: WorldGenerator.cs 구현 시 FindConnector(Direction.Right)가 null을 반환하지 않을 것임 (Unity Editor에서 09-01 메뉴 실행 전제)
- 09-03: SampleScene 설정에서 Room_Stair를 _roomPrefabs 풀에서 제외 처리 예정

## Known Stubs

None — RoomMarkerTool은 에디터 도구이며 런타임 데이터 플로우에 관여하지 않음.

## Self-Check

- [x] `Assets/Editor/RoomMarkerTool.cs` — 수정됨 (14개 RoomNames, Door/ENT+Door/EXIT 양방향)
- [x] Commit `8264e12` — 존재 확인

## Self-Check: PASSED

---
*Phase: 09-infinite-gen-cleanup*
*Completed: 2026-07-01*
