---
phase: quick-260626-ox2
plan: 01
subsystem: debug-tooling
tags: [bug-fix, debug, camera, static-state]
dependency_graph:
  requires: []
  provides: [DebugRoomTeleporter-fixed]
  affects: [CameraFollow, DebugRoomTeleporter]
tech_stack:
  added: []
  patterns: [FloorSpawner.SnapCameraToRoom 패턴 재사용]
key_files:
  modified:
    - Assets/Scripts/World/DebugRoomTeleporter.cs
decisions:
  - "_debugRoom/_nextSpawnPos를 static으로 승격 — 14개 인스턴스 간 상태 공유가 유일한 올바른 설계"
  - "Camera.main.GetComponent<CameraFollow>() 직접 조회 — FloorSpawner와 동일 패턴, 추가 참조 직렬화 불필요"
metrics:
  duration: ~5min
  completed: "2026-06-26T09:01:05Z"
  tasks_completed: 1
  files_modified: 1
---

# Quick 260626-ox2: DebugRoomTeleporter 버그 2개 수정 Summary

One-liner: static 공유 상태로 다중 텔레포터 간 방 겹침 제거 + 텔레포트 직후 CameraFollow.SnapToRoom() 호출로 카메라 Room_Debug 고정 해결

---

## What Was Done

`DebugRoomTeleporter.cs` 단일 파일 수정으로 버그 2개를 동시 해결.

### Bug 1 수정: 카메라 미업데이트

`TeleportToRoom()` 내 `_playerTransform.position = entryPos` 직후에 카메라 스냅 로직 추가.

```csharp
CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
if (cam != null)
{
    CameraBound cb = s_lastDebugRoom.GetComponentInChildren<CameraBound>();
    if (cb != null)
        cam.SnapToRoom(cb.GetWorldBounds());
    else
        cam.SnapToRoom(entryPos);
}
```

FloorSpawner의 `SnapCameraToRoom()` 패턴과 동일하게 CameraBound 유무에 따라 두 오버로드를 분기.

### Bug 2 수정: 방 겹침 (다중 인스턴스 간 상태 비공유)

인스턴스 필드 → static 필드로 교체:

| 제거 (instance) | 추가 (static) |
|---|---|
| `private GameObject _debugRoom` | `private static GameObject s_lastDebugRoom` |
| `private Vector3 _nextSpawnPos` | `private static Vector3 s_nextSpawnPos` |

14개 텔레포터 인스턴스가 `s_lastDebugRoom`을 공유하므로, 어느 텔레포터를 사용하든 `Destroy(s_lastDebugRoom)`이 이전 방을 제거한다.

### 추가: spawnY → offsetX/offsetY + Enter 키

- 고정 Y 좌표(`spawnY`) 방식 → 누적 오프셋(`offsetX`/`offsetY`) 방식으로 교체 (방 겹침 방지 보강)
- 입력키 Up/W → Enter로 통일

---

## Commits

| Hash | Description |
|------|-------------|
| c5ae0bc | fix(quick-260626-ox2): DebugRoomTeleporter — static 공유 상태 + 카메라 스냅 |

---

## Deviations from Plan

**1. [Rule 3 - Blocking] worktree 버전이 PLAN 대상 버전과 달라 통합 적용**

- **Found during:** Task 1
- **Issue:** 워크트리의 커밋 버전은 구 버전(`spawnY`, Up/W 키)이고, 플랜은 메인 워킹트리의 미커밋 변경(`offsetX`/`offsetY`, Enter 키, `_nextSpawnPos`)을 전제로 작성됨
- **Fix:** 메인 워킹트리의 중간 변경사항(spawnY→offsetX/offsetY, Up/W→Enter, _nextSpawnPos 추가)과 플랜의 버그 수정(static 필드 + 카메라 스냅)을 단일 커밋으로 통합 적용
- **Files modified:** Assets/Scripts/World/DebugRoomTeleporter.cs
- **Commit:** c5ae0bc

---

## Known Stubs

None.

---

## Self-Check: PASSED

- [x] `Assets/Scripts/World/DebugRoomTeleporter.cs` — FOUND
- [x] commit `c5ae0bc` — FOUND
