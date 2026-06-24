---
phase: quick
plan: 260624-ml3
subsystem: Room
tags: [bugfix, room-clear, floor-spawner, enemy-detection]
dependency_graph:
  requires: [FloorSpawner, IEnemy, RoomClearCondition]
  provides: [RoomClearCondition 버그 수정]
  affects: [Room_Combat, Room_Chase, Room_Dodge, Room_Gap, Room_Mixed]
tech_stack:
  added: []
  patterns: [GetComponentsInChildren with includeInactive, List<IEnemy> to array]
key_files:
  created: []
  modified:
    - Assets/Scripts/Room/RoomClearCondition.cs
decisions:
  - "GetComponentsInChildren(includeInactive:true) — FloorSpawner가 SetActive(false)로 비활성 상태인 적도 탐색"
  - "enemies 배열 빈 경우 즉시 Activate() — Update() 분기는 변경하지 않아 _activated=true 후 자동 종료"
metrics:
  duration: "~5min"
  completed: "2026-06-24"
  tasks: 1
  files: 1
---

# Quick Task 260624-ml3: RoomClearCondition 버그 2개 수정 Summary

**One-liner:** enemies 배열이 비어있을 때 early return 제거 + GetComponentsInChildren(includeInactive:true)로 FloorSpawner 동적 스폰 적 자동 탐색

## What Was Done

`RoomClearCondition.cs`의 버그 2개를 수정했다.

**버그 1 — enemies 배열이 비어있을 때 targetObject 영구 잠김:**
- 기존 `Start()`의 26번 줄: `if (enemies == null || enemies.Length == 0) return;` → `_enemyCache = null` 상태로 early return
- `Update()`는 `_enemyCache == null` 조건으로 조기 리턴 → `Activate()` 미호출 → targetObject 영구 비활성
- 수정: `else` 분기로 동적 탐색 후 `_enemyCache`에 결과 할당. 탐색 결과가 비어있으면 즉시 `Activate()` 호출

**버그 2 — FloorSpawner 동적 스폰 적 미추적:**
- FloorSpawner는 `Instantiate()` 직후 적을 `SetActive(false)` 상태로 두고 `ActivateEnemies()`에서 활성화
- 기존 코드는 enemies 배열이 비어있는 경우 적을 전혀 탐색하지 않음
- 수정: `GetComponentsInChildren<MonoBehaviour>(true)` (`includeInactive:true` 필수)로 비활성 자식 포함 탐색, `IEnemy` 구현체만 필터링

## Commits

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | RoomClearCondition 버그 2개 수정 | 8181b9f | Assets/Scripts/Room/RoomClearCondition.cs |

## Deviations from Plan

None - plan executed exactly as written.

Note: `RoomClearCondition.cs`가 git 미추적 상태였으므로 worktree에 복사 후 커밋함. .meta 파일도 함께 추가됨.

## Self-Check: PASSED

- [x] `Assets/Scripts/Room/RoomClearCondition.cs` — created in worktree (commit 8181b9f)
- [x] `GetComponentsInChildren` — line 41
- [x] `using System.Collections.Generic` — line 1
- [x] `Activate();` — line 51, 63
- [x] Commit 8181b9f exists
