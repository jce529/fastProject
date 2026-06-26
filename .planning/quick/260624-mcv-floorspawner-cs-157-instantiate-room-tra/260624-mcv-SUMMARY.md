---
phase: quick
plan: 260624-mcv
subsystem: world/floor-spawner
tags: [floor-system, memory-management, enemy-lifecycle]
key-files:
  modified:
    - Assets/Scripts/World/FloorSpawner.cs
decisions:
  - "Unity Instantiate(prefab, pos, rot, parent) 오버로드 사용 — 월드 좌표 유지 + parent 계층 편입 동시 달성"
metrics:
  duration: "< 5min"
  completed: "2026-06-24"
  tasks: 1
  files: 1
requirements: [FLOOR-03]
---

# Phase quick Plan 260624-mcv: FloorSpawner Instantiate room.transform parent 추가 Summary

**One-liner:** SpawnRoom() 적 Instantiate에 room.transform을 parent로 추가해 Room 파괴 시 적이 자동으로 함께 정리된다.

---

## What Was Built

`FloorSpawner.cs` `SpawnRoom()` 메서드 내 157번째 줄의 적 Instantiate 호출에 `room.transform`을 4번째 인수(parent)로 추가했다.

**변경 전:**
```csharp
GameObject enemy = Instantiate(enemyPrefab, child.position, Quaternion.identity);
```

**변경 후:**
```csharp
GameObject enemy = Instantiate(enemyPrefab, child.position, Quaternion.identity, room.transform);
```

Unity의 `Instantiate(prefab, position, rotation, parent)` 오버로드는 월드 좌표계 position/rotation을 그대로 유지하면서 생성된 오브젝트를 지정된 parent 계층에 편입한다. `child.position`은 이미 월드 좌표이므로 적의 실제 위치는 변하지 않는다.

---

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | SpawnRoom() Instantiate에 room.transform parent 추가 | ada935e | Assets/Scripts/World/FloorSpawner.cs |

---

## Verification

- `Assets/Scripts/World/FloorSpawner.cs` 157번째 줄에 `Instantiate(enemyPrefab, child.position, Quaternion.identity, room.transform)` 문자열 존재 확인
- 생성된 적이 Room 계층 자식으로 편입 — `Destroy(_currentRoom)` 호출 시 적도 자동으로 함께 파괴됨 (FLOOR-04 지원)
- `enemy.SetActive(false)` (FLOOR-03) 로직 그대로 유지됨

---

## Deviations from Plan

None - plan executed exactly as written.

---

## Self-Check: PASSED

- [x] `Assets/Scripts/World/FloorSpawner.cs` 수정 확인
- [x] 커밋 `ada935e` 존재 확인
- [x] `room.transform` 패턴 grep 확인 (line 157)
