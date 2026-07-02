---
phase: quick
plan: 260629-jmp
status: completed
---

## 완료 요약

**목표:** TestWorldGenerator.cs — Room→Corridor→Room 수평 자동 배치
**결과:** `Assets/Scripts/World/TestWorldGenerator.cs` 신규 생성

## 구현 내용

- Inspector 필드: `_roomPrefabs[]`, `_corridorPrefabs[]`
- `Start()` 체인: Room[0] → Corridor[0] → Room[last] 순서로 자동 배치
- `FindConnector()`: `GetComponentsInChildren<RoomConnector>(true)` 기반 방향 탐색
- `AlignByEntry()`: Left 커넥터 위치를 이전 Right 커넥터 위치에 맞춰 이동

## 씬 설정 방법

1. 빈 GameObject → TestWorldGenerator 컴포넌트 부착
2. `_roomPrefabs[0]` = Room_Combat (등), `_corridorPrefabs[0]` = Corridor_Flat (등)
3. FloorSpawner 컴포넌트 disable
4. Play → Scene View에서 RoomConnector Gizmo(구)가 연결부에서 겹치는지 확인

## 불변 사항

- FloorSpawner.cs 수정 없음
- 기존 RoomConnector.cs API 그대로 사용
