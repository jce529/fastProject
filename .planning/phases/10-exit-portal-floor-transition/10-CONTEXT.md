# Phase 10: EXIT 포탈 & 층 전환 - Context

**Gathered:** 2026-07-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Room 스폰 시 확률적으로 EXIT 포탈이 생성되고(EXIT-01, EXIT-02), 플레이어가 포탈에 진입하면 층 번호가 올라가고 WorldGenerator의 수평 체인이 초기화된다(EXIT-03). 층 전환 시 플레이어의 수직(Y) 이동은 RoomEntry(ENT) 마커 기반 텔레포트로 처리한다 — 룸 내부 수평 이동은 기존처럼 걸어서 진행.

**Requirements in scope:** EXIT-01, EXIT-02, EXIT-03
**Not in scope:** 타이머/난이도(Phase 11), 포탈 이펙트/사운드(REQUIREMENTS.md Out of Scope)

</domain>

<decisions>
## Implementation Decisions

### 포탈 스폰 위치 마커
- **D-01:** `ExitSpawnPoint` 신규 마커 컴포넌트를 만든다 — `EnemySpawner.cs` 패턴과 동일 (빈 컴포넌트, 자식 오브젝트 위치가 스폰 후보 지점).
- **D-02:** Complex_Room 6종(AllInOne/EdgeRun/GaugeOutpost/LastStand/RiskCrossing/Vertical_Gauntlet) 전부에 배치. 룸당 2~3개 지점.
- **D-03:** 배치는 사용자가 에디터에서 직접 수동으로 진행한다 — 자동 배치 도구는 만들지 않는다. (플랜/실행 단계에서 사용자 액션 항목으로 명시할 것)

### 층 전환 시퀀스 & ENT 텔레포트
- **D-04:** 옛 `FloorSpawner.FloorTransitionSequence()`의 6단계(입력잠금 → ENT 텔레포트 → 카메라 스냅 → 프레임 대기 → 적 활성화 → 조작 재개)를 그대로 재사용한다. 전부 `WaitForSecondsRealtime` 기반으로 timeScale 면역 유지.
- **D-05:** 수평 체인 진행(같은 층 내 룸→길→룸 이동)은 지금처럼 플레이어가 직접 걸어서 이동한다 — 텔레포트는 오직 층 전환(수직 이동) 순간에만 적용.
- **D-06 (folded todo, 2026-07-03-complex-room-ent):** RoomEntry(ENT) 마커가 없는 4개 Complex_Room(AllInOne/EdgeRun/LastStand/Vertical_Gauntlet)에 `RoomEntry` 컴포넌트를 직접 추가한다 — 코드 폴백을 유지하는 대신 근본 해결. GaugeOutpost/RiskCrossing은 이미 보유.

### WorldGenerator 리셋 범위
- **D-07:** 포탈 진입 시 기존 수평 체인(`_chain` 리스트의 모든 room+corridor)을 즉시 전부 Destroy하고, 활성화된 대기룸(`_nextFloorRoom`) 하나를 새 체인의 시작점으로 삼아 재시작한다. GEN-02의 점진적 lookbehind 정리에 맡기지 않는다.

### 미사용 포탈 소멸 처리
- **D-08 (09-CONTEXT D-07 후속 결정):** GEN-02(lookbehind 정리)가 플레이어가 진입하지 않은 포탈을 보유한 룸을 Destroy할 때, 해당 포탈에 연결된 대기룸(`_nextFloorRoom`)도 함께 Destroy하고 활성 포탈 카운트(`_maxExitsActive` 카운터)를 감소시킨다 — 대기룸 메모리 누수 방지 + 신규 포탈 스폰 기회 복원.

### Claude's Discretion
- ExitPortal 컴포넌트의 트리거 콜라이더 크기/모양, EXIT 포탈 스폰 확률 롤 발생 시점(룸 스폰 직후 vs 특정 프레임) 등 세부 구현은 플래너/실행자 재량.
- `FloorSpawner.cs`, `RoomExit.cs` (Phase 5 유산, 현재 씬에서 미사용 고아 코드)는 이번 Phase에서 생성한 코드가 아니므로 삭제하지 않는다 — 언급만 하고 그대로 둔다. 신규 `ExitPortal.cs`가 `RoomExit.cs`의 역할을 대체한다.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements & Roadmap
- `.planning/ROADMAP.md` §Phase 10 — EXIT-01/02/03 성공 기준 정의
- `.planning/REQUIREMENTS.md` §EXIT 포탈 (EXIT) — 요구사항 상세 및 Out of Scope 목록

### Prior Phase Decisions
- `.planning/STATE.md` §Key Decisions for v3.0 — EXIT 포탈 기본 확률 0.15f, 최대 동시 활성 1개 (기존 확정값)
- `.planning/phases/09-infinite-gen-cleanup/09-CONTEXT.md` §다음 층 대기룸 — D-04~D-07 사전 결정 (대기룸 스폰/파괴 규칙의 원본)
- `.planning/phases/09-infinite-gen-cleanup/09-02-SUMMARY.md` — WorldGenerator.SpawnNextFloorStandbyRoom() 스텁 상세, _floorHeight=40f

### Folded Todo
- `.planning/todos/pending/2026-07-03-complex-room-ent.md` — ENT 마커 부재 4개 룸 문제, Phase 10 통합 결정

### Existing Code Patterns
- `Assets/Scripts/World/WorldGenerator.cs` — 체인 관리, SpawnNextFloorStandbyRoom() 스텁 (line 147)
- `Assets/Scripts/World/RoomEntry.cs` — ENT 마커 컴포넌트 (빈 클래스)
- `Assets/Scripts/World/RoomConnector.cs` — Left/Right 방향 마커 패턴 (ExitSpawnPoint 설계 참고)
- `Assets/Scripts/World/DebugRoomTeleporter.cs` — RoomEntry 기반 텔레포트 + 카메라 스냅 참고 구현
- `Assets/Scripts/World/FloorSpawner.cs` — 6단계 전환 시퀀스 원본 (재사용 대상), 미사용 고아 코드
- `Assets/Scripts/World/RoomExit.cs` — 옛 출구 트리거, ExitPortal로 대체 예정, 미사용 고아 코드
- `Assets/Scripts/World/FloorManager.cs` — CurrentFloor 정적 카운터 (HUD 연동)
- `Assets/Scripts/World/EnemySpawner.cs` — ExitSpawnPoint 마커 컴포넌트 설계 패턴 참고

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `RoomEntry.cs` / `RoomConnector.cs`: 마커 컴포넌트 패턴 그대로 `ExitSpawnPoint`에 적용 가능
- `DebugRoomTeleporter.cs`: ENT 위치 텔레포트 + CameraBound 스냅 로직 참고
- `WorldGenerator.SpawnNextFloorStandbyRoom()`: Phase 10에서 실제 구현할 스텁, `_floorHeight` 필드 이미 존재
- `FloorManager.CurrentFloor`: 정적 int, ++ 만 호출하면 HUD 자동 갱신 (Phase 4에서 이미 연동)

### Established Patterns
- 마커 컴포넌트는 빈 클래스 + `GetComponentsInChildren<T>(true)` 탐색 방식 (RoomConnector, RoomEntry, EnemySpawner 공통)
- 층 전환/텔레포트 관련 코루틴은 전부 `WaitForSecondsRealtime` — `Time.timeScale`이 0이어도 진행
- Y drift 체인 관리는 튜플 리스트(`List<(GameObject room, GameObject corridor)>`) — 체인 전체 Destroy 시 이 구조 순회

### Integration Points
- `ExitPortal.OnTriggerEnter2D()` → `WorldGenerator.SpawnNextFloorStandbyRoom()` 호출 지점 (기존 스텁 주석에 명시됨)
- `FloorManager.CurrentFloor++`는 HUD와 이미 연동되어 있어 Phase 10에서 호출만 추가하면 됨
- Complex_Room 6종 프리팹에 `ExitSpawnPoint` 자식 오브젝트 수동 추가 필요 (Assets/Prefabs/Rooms/Complex_Room/*)
- Complex_Room 4종(AllInOne/EdgeRun/LastStand/Vertical_Gauntlet)에 `RoomEntry` 자식 오브젝트 수동 추가 필요

</code_context>

<specifics>
## Specific Ideas

없음 — 표준 접근으로 충분. 기존 Phase 9 pre-decision(D-04~D-07)과 today's todo가 대부분의 세부사항을 이미 결정해뒀음.

</specifics>

<deferred>
## Deferred Ideas

없음 — 논의가 Phase 범위 내에 머무름.

### Reviewed Todos (not folded)
없음 — 매칭된 유일한 todo(ENT 텔레포트)를 이번 Phase에 포함함.

</deferred>

---

*Phase: 10-exit-portal-floor-transition*
*Context gathered: 2026-07-03*
