---
phase: quick-260626-il8
plan: 01
subsystem: camera-floor
tags: [camera, floor-spawner, snap, room-view]
dependency_graph:
  requires: []
  provides: [CameraFollow.SnapToRoom, FloorSpawner._cameraFollow]
  affects: [FloorSpawner, CameraFollow]
tech_stack:
  added: []
  patterns: [roomMode flag, null-conditional SnapToRoom call, orthographicSize override]
key_files:
  created: []
  modified:
    - Assets/Scripts/Camera/CameraFollow.cs
    - Assets/Scripts/World/FloorSpawner.cs
decisions:
  - "offset.z 재사용: SnapToRoom은 worldCenter.x/y를 받되 Z는 기존 offset.z(=-10)를 유지해 카메라 깊이를 보존한다"
  - "_roomMode 플래그는 단방향 — SnapToRoom 호출 후 플레이어 추적으로 돌아가는 API는 이번 범위에서 제외"
  - "_roomCameraOffsetY 기본값 6f — 룸 바닥(Y=0)에서 카메라 중심까지 약 1/3 높이 지점"
metrics:
  duration: "~5min"
  completed: "2026-06-26"
  tasks: 2
  files: 2
---

# Quick 260626-il8: CameraFollow SnapToRoom + FloorSpawner 연결 Summary

**One-liner:** CameraFollow에 SnapToRoom(_roomMode 플래그 + orthographicSize 고정) 추가 후 FloorSpawner Awake/FloorTransitionSequence 2곳에서 룸 중심 스냅 호출 연결.

---

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | CameraFollow SnapToRoom 모드 추가 | f37a748 | CameraFollow.cs |
| 2 | FloorSpawner _cameraFollow 연결 + SnapToRoom 호출 2곳 | 3b6e6aa | FloorSpawner.cs |

---

## What Was Built

**CameraFollow.cs** — 3개 필드 + Awake + SnapToRoom 메서드 추가:
- `roomOrthoSize` (SerializeField, 기본 7f): Inspector에서 조정 가능한 룸 뷰 orthographicSize
- `_roomMode` (bool): true이면 LateUpdate 플레이어 추적 건너뜀
- `_camera` (Camera): Awake에서 GetComponent 캐시
- `SnapToRoom(Vector3 worldCenter)`: `_roomMode=true`, 카메라 position을 `(worldCenter.x, worldCenter.y, offset.z)`로 즉시 이동, orthographicSize를 roomOrthoSize로 설정
- LateUpdate: `if (_roomMode) return;` 가드 추가

**FloorSpawner.cs** — 2개 SerializeField + 2곳 SnapToRoom 호출 추가:
- `_cameraFollow` (CameraFollow): Inspector Camera Follow 슬롯
- `_roomCameraOffsetY` (float, 기본 6f): 룸 원점 대비 카메라 중심 Y 오프셋
- Awake() 끝: `_cameraFollow?.SnapToRoom(new Vector3(0f, _roomCameraOffsetY, 0f))` — 1층 스폰 시 고정
- FloorTransitionSequence Step 2 직후: `_cameraFollow?.SnapToRoom(new Vector3(0f, newRoomBaseY + _roomCameraOffsetY, 0f))` — 층 전환 시 새 룸 중심으로 즉시 스냅
- Step 3 주석 갱신: "카메라 스냅 완료 — LateUpdate가 실행되도록 한 프레임 양보"

---

## Inspector 연결 필요 (수동)

Unity Editor에서 FloorSpawner GameObject 선택 후:
- **Camera Follow** 슬롯 → Main Camera GameObject 드래그 연결
- **Room Camera Offset Y** 기본값 6 확인 (룸 높이 18f 기준 1/3 지점)
- **Room Ortho Size** (CameraFollow Inspector) 기본값 7 확인; 룸 전체가 뷰에 안 들어오면 상향 조정

---

## Deviations from Plan

None — plan executed exactly as written.

---

## Known Stubs

None — SnapToRoom은 즉시 완전 동작. _cameraFollow가 null이면 null-conditional(?.)으로 안전하게 스킵.

---

## Self-Check: PASSED

- `Assets/Scripts/Camera/CameraFollow.cs` — FOUND (f37a748)
- `Assets/Scripts/World/FloorSpawner.cs` — FOUND (3b6e6aa)
- Commit f37a748 — confirmed in git log
- Commit 3b6e6aa — confirmed in git log
