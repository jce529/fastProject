---
phase: 09-infinite-gen-cleanup
plan: "02"
subsystem: WorldGenerator
tags: [world-gen, infinite-gen, runtime, chain, GEN-01, GEN-02, GEN-03]
dependency_graph:
  requires:
    - RoomConnector.cs (Direction enum + GetComponentsInChildren 패턴)
    - CameraFollow.cs (SnapToRoom(Vector3) 시그니처)
    - TestWorldGenerator.cs (AlignByEntry/FindConnector 원본 패턴)
  provides:
    - WorldGenerator.cs (플레이어 위치 기반 무한 Room+Corridor 체인 생성/정리 MonoBehaviour)
    - SpawnNextFloorStandbyRoom() public stub (Phase 10 ExitPortal 연동용)
  affects:
    - SampleScene.unity (WorldGenerator 컴포넌트 배치 필요 — Phase 9 03에서 수행)
tech_stack:
  added: []
  patterns:
    - AlignByEntry: Instantiate(Vector3.zero) 직후 ENT 커넥터 위치로 정렬 (Phase 8 검증 패턴 이식)
    - chain List<(room, corridor)>: corridor = 해당 room의 왼쪽(ENT 방향) 길 (D-09 규칙)
    - SelectCorridor Y drift: _currentYDrift 기반 후보 필터링 후 랜덤 선택 (GEN-03 + D-01~D-03)
key_files:
  created:
    - Assets/Scripts/World/WorldGenerator.cs
  modified: []
decisions:
  - "List<(GameObject room, GameObject corridor)> 튜플 쌍 자료구조 선택: corridor = room의 왼쪽 길 보장으로 RemoveTail() 원자적 처리 가능"
  - "SelectCorridor() 후보 필터링: _currentYDrift < _maxYDrift 조건으로 Up 허용, > _minYDrift 조건으로 Down 허용 — Flat은 항상 허용"
  - "SpawnNextFloorStandbyRoom() 스텁: Phase 10 ExitPortal 연동 전 컴파일 오류 방지용 stub으로만 구현"
  - "UpdatePlayerIndex() 전진 전용: 뒤로 이동 인덱스 감소 없음 — 무한 체인은 앞으로만 진행"
metrics:
  duration: "~4 minutes"
  completed_date: "2026-07-01"
  tasks_completed: 2
  files_created: 1
  files_modified: 0
---

# Phase 09 Plan 02: WorldGenerator.cs Summary

**One-liner:** 플레이어 X 위치 기반 무한 Room+Corridor 체인을 동적 생성·정리하는 WorldGenerator MonoBehaviour — AlignByEntry 이식 + Y drift 제약 3종 Corridor 랜덤 선택(GEN-01/02/03) 구현.

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | WorldGenerator.cs — 클래스 선언 + Start() + 모든 헬퍼 메서드 | 85d4ed9 | Assets/Scripts/World/WorldGenerator.cs (신규, 151줄) |
| 2 | WorldGenerator.cs — Update() + UpdatePlayerIndex() 추가 | ce2ef2c | Assets/Scripts/World/WorldGenerator.cs (완성, 183줄) |

## What Was Built

`Assets/Scripts/World/WorldGenerator.cs` (183줄) — Phase 9 핵심 런타임 MonoBehaviour.

### 구현된 메서드 목록

| 메서드 | 반환 | 역할 |
|--------|------|------|
| `Start()` | void | 시작룸 1개 + lookahead 2쌍(총 GO 5개) 초기 체인 구성 |
| `Update()` | void | UpdatePlayerIndex -> GEN-01 스폰 루프 -> GEN-02 정리 루프 |
| `UpdatePlayerIndex()` | void | 플레이어 X > 룸 EXIT X 감지 -> _playerCurrentIndex 전진 |
| `SpawnNextPair()` | void | SelectCorridor + Instantiate/AlignByEntry 체인 1쌍 연장 |
| `RemoveTail()` | void | chain[0] Destroy + RemoveAt(0) |
| `SelectCorridor()` | GameObject | _currentYDrift 기반 후보 필터 + 랜덤 선택 + drift 누적 |
| `AlignByEntry()` | void | Instantiate(Vector3.zero) 직후 ENT 커넥터 기준 정렬 (Phase 8 이식) |
| `FindConnector()` | RoomConnector | GetComponentsInChildren으로 Direction 일치 커넥터 탐색 |
| `SpawnNextFloorStandbyRoom()` | void | Phase 10 연동 public stub |

### 핵심 설계 결정

**AlignByEntry 이식:** TestWorldGenerator.cs의 Phase 8 검증 패턴을 그대로 이식. Instantiate(prefab, Vector3.zero, Quaternion.identity) 직후에만 호출해야 하는 제약(Pitfall 2)을 코드 주석으로 명시.

**Y drift 공식 (GEN-03, D-01~D-03):**
- `_currentYDrift < _maxYDrift` -> `_corridorUp` 후보 추가 (+4f 누적)
- `_currentYDrift > _minYDrift` -> `_corridorDown` 후보 추가 (-4f 누적)
- `_corridorFlat` 항상 허용 (0f 변화)
- 기본값: minYDrift=-12f, maxYDrift=+12f (Inspector 조정 가능)

**chain 자료구조 (D-09):** `List<(GameObject room, GameObject corridor)>` 튜플 쌍. corridor = 해당 room의 왼쪽(ENT 방향) 길. chain[0]의 corridor는 null(첫 룸은 왼쪽 길 없음).

**Pitfall 4 방지:** `RemoveTail()` 직후 `_playerCurrentIndex--` 쌍으로 실행 보장. while 루프 안에서 두 문장이 항상 쌍으로 실행됨.

**Pitfall 7 방지:** Start()에서 `_cameraFollow.SnapToRoom(Vector3.zero)` 호출로 FloorSpawner가 남긴 _hasBounds 상태 초기화.

## Task 1 Verify 결과

```
36:    private List<(GameObject room, GameObject corridor)> _chain
69:    private void SpawnNextPair()
93:    private void RemoveTail()
101:    private GameObject SelectCorridor()
112:        if      (chosen == _corridorUp)   _currentYDrift += 4f;
113:        else if (chosen == _corridorDown) _currentYDrift -= 4f;
118:    private void AlignByEntry(GameObject go, Vector3 targetWorldPos)
147:    public void SpawnNextFloorStandbyRoom()
```

## Task 2 Verify 결과

```
152:    private void Update()
154:        if (_playerTransform == null || _chain.Count == 0) return;
158:        while (_chain.Count - 1 - _playerCurrentIndex < _lookaheadCount)
163:        while (_playerCurrentIndex > _lookbehindCount)
165:            RemoveTail();
166:            _playerCurrentIndex--;
170:    private void UpdatePlayerIndex()
177:            if (_playerTransform.position.x > exitConnector.transform.position.x)
```

## Deviations from Plan

**Plan verification note:** 계획의 overall verification에서 `grep -c "private void" >= 7`을 명시했으나, 실제 구현 코드(SelectCorridor=GameObject 반환, FindConnector=RoomConnector 반환)와 불일치. private void 메서드 수는 6개가 정확하며(Start, SpawnNextPair, RemoveTail, AlignByEntry, Update, UpdatePlayerIndex), 나머지 2개는 non-void 반환 메서드. 계획 코드 명세 자체의 counting 오류로 코드 결함 없음.

이 외 편차 없음 — 계획에 명시된 코드를 정확히 구현.

## Known Stubs

| Stub | File | 이유 |
|------|------|------|
| `SpawnNextFloorStandbyRoom()` | `Assets/Scripts/World/WorldGenerator.cs` line 147 | Phase 10 ExitPortal 연동 전 컴파일 오류 방지용. 실제 로직은 Phase 10 범위(D-06). |

## Self-Check: PASSED

- FOUND: `Assets/Scripts/World/WorldGenerator.cs` (183 lines)
- FOUND: `.planning/phases/09-infinite-gen-cleanup/09-02-SUMMARY.md`
- FOUND commit `85d4ed9` feat(09-02): WorldGenerator.cs — class + Start() + helpers
- FOUND commit `ce2ef2c` feat(09-02): WorldGenerator.cs — Update() + UpdatePlayerIndex()
