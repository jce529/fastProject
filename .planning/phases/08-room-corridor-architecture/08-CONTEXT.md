# Phase 8: 룸-길 아키텍처 - Context

**Gathered:** 2026-06-29
**Status:** Ready for planning

<domain>
## Phase Boundary

Room 프리팹에 END_Left/END_Right 마커를 추가하고, Corridor 3종 프리팹을 제작하며, RoomConnector 컴포넌트를 구현한다. Room→Corridor→Room 체인이 물리적으로 막힘 없이 연결되는 아키텍처 기반을 갖추는 것이 목적.

**Requirements in scope:** ARCH-01, ARCH-02, ARCH-03
**Not in scope:** 자동 생성 로직(GEN-01~03 — Phase 9), EXIT 포탈(Phase 10), 타이머/난이도(Phase 11)

</domain>

<decisions>
## Implementation Decisions

### 기존 Room 프리팹 처리 (ARCH-01)
- **D-01:** 기존 15개 Room 프리팹(Room_Chain, Room_Combat 등)을 신규 제작하지 않고, 기존 프리팹에 END 마커를 추가해 v3.0 수평 체인 아키텍처에 활용한다.
- **D-02:** Phase 8에서는 4-5개 Room 프리팹에만 먼저 마커를 추가해 아키텍처를 검증한다. 나머지는 Phase 9 생성 로직이 필요할 때 마무리.

### RoomConnector 컴포넌트 구조 (ARCH-03)
- **D-03:** 별도 빈 마커 오브젝트를 추가하지 않는다. Room 내에 이미 존재하는 가장자리 오브젝트(바닥/벽 끝 오브젝트) 자체에 `RoomConnector` 컴포넌트를 직접 부착한다. 해당 오브젝트의 `Transform.position`이 곧 연결 마커 위치가 된다.
- **D-04:** `RoomConnector`는 두 필드를 직렬화: `Direction` 열거형(Left / Right)과 연결된 `GameObject` 참조. Phase 9 WorldGenerator가 `GetComponentsInChildren<RoomConnector>()`로 Left/Right를 탐색한다.
- **D-05:** Corridor의 ENT 쪽에도 동일하게 RoomConnector를 부착해 Direction: Entry로 구분하거나, 별도 `CorridorEntry` 컴포넌트로 분리한다 — 구현 Claude 재량.
- **D-06:** ARCH-01 Gizmo: RoomConnector의 `OnDrawGizmos()` 또는 `OnDrawGizmosSelected()`에서 위치 표시. Gizmo 시각 스타일(구/화살표/색상)은 Claude 재량.

### Corridor 프리팹 구성 (ARCH-02)
- **D-07:** Corridor 3종(상승/직진/하강). 상승/하강은 **계단 플랫폼** 방식 — 단차로 이어진 여러 플랫폼으로 구성해 기존 점프 컨트롤러와 자연스럽게 맞물린다. 경사로나 허공 점프 발판은 사용하지 않는다.
- **D-08:** 각 Corridor의 콘텐츠는 최소 수준: 계단/바닥 플랫폼 + `EnemySpawnPoint` 태그 자식 오브젝트. 추가 장애물/함정은 Phase 8 범위 밖.
- **D-09:** Corridor 너비(가로 길이) 및 높이차 수치는 Claude 재량. Room과 자연스럽게 이어질 수 있는 크기로 설정.

### Phase 8 검증 방법
- **D-10:** 별도 테스트 씬 없이 SampleScene에 Room 프리팹 1개 + Corridor 프리팹 1개 + Room 프리팹 1개를 에디터에서 수동으로 순서대로 배치한다. 플레이어가 왼쪽에서 오른쪽으로 이동하며 막힘 없이 통과하는지 플레이 테스트.

### Claude's Discretion
- RoomConnector Gizmo 시각 표현 방식 (구/화살표, 색상 코드)
- Corridor ENT 마커 컴포넌트 이름 (`RoomConnector`의 Direction.Entry 재사용 vs 별도 `CorridorEntry`)
- Corridor 너비, 상승/하강 높이차 수치 (Room과 물리적으로 잘 맞는 값으로 결정)
- 4-5개 Room 프리팹 중 어느 것을 먼저 마커 추가할 것인가 (권장: Room_Combat, Room_Fall, Room_Gap, Room_Stair — 다양한 지형 포함)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 설계 문서
- `층설계.md` — 사용자 직접 작성한 v3.0 룸-길 구조 설계 메모. END 마커, EXIT 포탈, 뒤 2개 유지, 타이머, 점수 시스템 전체 개요.

### 요구사항
- `.planning/REQUIREMENTS.md` §룸-길 아키텍처 (ARCH) — ARCH-01, ARCH-02, ARCH-03 상세 정의
- `.planning/ROADMAP.md` §Phase 8 — Goal, 성공 기준 4개, 상위 Phase 의존성

### 상태
- `.planning/STATE.md` §Key Decisions for v3.0 — WorldGenerator(FloorSpawner 대체), FloorTimer 등 v3.0 전체 아키텍처 결정

### 기존 코드 (수정 또는 대체 대상)
- `Assets/Scripts/World/RoomEntry.cs` — 기존 ENT 마커 컴포넌트. Phase 8에서 RoomConnector로 대체 또는 확장 검토.
- `Assets/Scripts/World/FloorSpawner.cs` — 기존 수직 층 전환 MonoBehaviour. Phase 8에서 건드리지 않음; Phase 9에서 WorldGenerator로 대체 예정.
- `Assets/Scripts/World/RoomExit.cs` — 기존 출구 트리거. Phase 10에서 ExitPortal로 교체 예정; Phase 8에서 건드리지 않음.

### Room 프리팹
- `Assets/Prefabs/Rooms/` — 15개 Room 프리팹 보관 위치. Phase 8에서 4-5개에 END_Left/END_Right(RoomConnector) 추가.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RoomEntry.cs` — 단 한 줄 MarkerMonoBehaviour. `RoomConnector`도 동일한 경량 패턴으로 구현 가능.
- `EnemySpawnPoint` 태그 — Corridor 내 적 스폰 포인트에 동일 태그 재사용. FloorSpawner.SpawnRoom()이 이미 이 태그로 탐색함.
- `Room_Combat`, `Room_Fall`, `Room_Stair` 등 — 다양한 지형 구조를 포함한 기존 프리팹. 가장자리 오브젝트 확인 후 RoomConnector 부착.

### Established Patterns
- 경량 마커 MonoBehaviour: `RoomEntry : MonoBehaviour {}` 패턴 — `RoomConnector`도 동일 경량화.
- `GetComponentInChildren<T>(true)` — FloorSpawner에서 이미 사용 중. RoomConnector 탐색에 동일 적용.
- `EnemySpawnPoint` 태그 기반 탐색 — `child.CompareTag("EnemySpawnPoint")` 패턴 재사용.

### Integration Points
- Phase 9 WorldGenerator가 `room.GetComponentsInChildren<RoomConnector>()` 호출해 Left/Right 마커를 탐색, Corridor.ENT 위치에 정렬할 예정.
- FloorSpawner는 Phase 8 동안 기존 코드 유지 — SampleScene에 여전히 배치되어 있으나 Phase 8 아키텍처 검증에는 개입하지 않음.

</code_context>

<specifics>
## Specific Ideas

- 상승/하강 Corridor의 계단: 2~3단 단차 플랫폼, 한 칸씩 점프로 오르거나 내려갈 수 있는 너비와 높이.
- RoomConnector Gizmo: 씬 뷰에서 Left는 파란 구(sphere), Right는 초록 구로 항상 표시 — 색상 코드로 방향 구분.
- 검증용 SampleScene 수동 배치 순서: Room_Combat(1) → Corridor_Flat → Room_Fall(2) 정렬 후 플레이어 좌→우 이동 테스트.

</specifics>

<deferred>
## Deferred Ideas

- Corridor 장애물/함정 콘텐츠 확장 — 플레이테스트 후 난이도 조정 필요 시
- 나머지 10개 Room 프리팹 마커 추가 — Phase 9에서 생성 풀이 필요할 때
- Corridor 종류 추가(분기형, 보스 전용 등) — 프로토타입 검증 완료 후
- 양방향 이동 시 Corridor에서의 카메라 처리 — Phase 9 WorldGenerator 범위

</deferred>

---

*Phase: 08-room-corridor-architecture*
*Context gathered: 2026-06-29*
