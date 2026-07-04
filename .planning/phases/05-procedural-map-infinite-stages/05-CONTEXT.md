# Phase 5: 절차적 맵 생성 — 무한 스테이지 - Context

**Gathered:** 2026-06-17
**Status:** Ready for planning

<domain>
## Phase Boundary

FloorManager가 출구 트리거를 감지해 Room 프리팹을 순차적으로 스폰하고, 지나간 층을 파괴하여 플레이어가 무한히 탑을 올라갈 수 있는 스테이지를 구현한다. 층 번호 기반 난이도 스케일링과 FLOOR-02 전환 시퀀스 완전 구현 포함.

**Requirements in scope:** FLOOR-01, FLOOR-02, FLOOR-03, FLOOR-04
**Not in scope:** 모바일 온스크린 컨트롤(MOBI-01, MOBI-02 — v2), 보스전, 성장 시스템

</domain>

<decisions>
## Implementation Decisions

### 층 전환 트리거
- **D-01:** 출구 트리거(Trigger Collider2D)를 각 Room 프리팹의 위쪽에 자식 오브젝트로 배치. 플레이어가 밟으면 층 전환 시퀀스 시작.
- **D-02:** 적 처치 여부와 무관하게 언제든 출구를 밟으면 전환 — 플레이어가 빠르게 지나칠 수 있는 스타일 허용. 프로토타입 검증에 적합.

### Room 프리팹 구성
- **D-03:** 14개 Room 폴더 중 **4~5개**를 Unity Editor에서 수동으로 콘텐츠 채움. 나머지는 빈 폴더로 유지(향후 v2 확장용).
- **D-04:** 각 Room 프리팹에 포함할 요소: 플랫폼(Tilemap 또는 Sprite), 출구 트리거(자식 오브젝트), 적 스폰 포인트(빈 GameObject 태그). 1층(고정)은 가장 단순한 평지 구성.
- **D-05:** 1층은 항상 고정된 Room(Room_Combat 또는 전용 단순 Room) — 플레이어가 시스템에 적응할 시간 제공. 2층부터 가중치 랜덤 선택.

### 적 스폰 방식
- **D-06:** 스폰 포인트 기반 런타임 스폰 — Room 프리팹에 빈 `EnemySpawnPoint` 오브젝트만 배치, `FloorSpawner`(또는 확장된 `FloorManager`)가 층 번호를 읽어 MeleeEnemy/RangedEnemy를 동적 Instantiate.
- **D-07:** 난이도 스케일링 — 층 번호 증가 시 **적 총 수 증가 + RangedEnemy 비율 증가**. 예시 (Claude 재량으로 수치 결정):
  - 1~5층: 근접 위주 (MeleeEnemy 2~3마리, RangedEnemy 0~1마리)
  - 6~10층: 혼합 (MeleeEnemy 2마리, RangedEnemy 1~2마리)
  - 11층+: 원거리 비율 확대 (MeleeEnemy 2마리, RangedEnemy 2~3마리)

### 층 전환 연출 (FLOOR-02 완전 구현)
- **D-08:** 6단계 시퀀스 전체 구현:
  1. **조작 불가** — `PlayerController` 입력 잠금 (새 입력 무시, 속도 0 고정)
  2. **순간이동** — 플레이어를 새 층 스폰 포인트 위치로 즉시 이동 (`Transform.position`)
  3. **카메라 Y스냅** — 카메라를 새 층 Y 위치로 즉시 맞춤 (Phase 1 D-11 LateUpdate 유지 — Coroutine 애니메이션 없음)
  4. **가림막 해제** — FLOOR-03: 새 층의 적들 `gameObject.SetActive(true)` (이전엔 비활성 상태로 스폰됨)
  5. **적 인식 활성화** — 활성화된 적들 FSM이 플레이어를 인식하기 시작
  6. **조작 재개** — 입력 잠금 해제
- **D-09:** 전환 중 이전 층 파괴 — 순간이동 완료 직후 `Destroy(previousFloor)` (FLOOR-04 모바일 메모리 요건). 현재 층 + 다음 층(아직 스폰 안 됨)만 씬에 유지.

### 재시작 호환
- **D-10:** `DeathScreenController.RestartGame()`의 `SceneManager.LoadScene(0)` 유지 — 씬 재로드가 모든 스폰된 Room을 자동 파괴하고 `FloorManager.CurrentFloor = 1` 리셋도 이미 구현됨. 추가 코드 불필요.

### Claude's Discretion
- 각 층의 Room 높이 통일 여부 및 수치 (권장: 모든 Room 동일 높이 — Y 오프셋 계산 단순화)
- 스폰 Y 오프셋 계산 방식 (Room 높이 × 층 번호)
- 가중치 랜덤 선택 알고리즘 (단순 `Random.Range` 배열 인덱스로 충분)
- 다음 층 사전 스폰 타이밍 (플레이어가 현재 층 출구 X% 진입 시)
- 적 수 구체적 수치 (D-07 예시 범위 내)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` §v2 Requirements — FLOOR-01, FLOOR-02, FLOOR-03, FLOOR-04 상세 정의

### Roadmap & State
- `.planning/ROADMAP.md` §Phase 5 — Goal 및 Design Notes (4~6종 프리팹 풀, 가중치 랜덤, 메모리 제약)
- `.planning/STATE.md` — Key Decisions Locked (FloorManager static 필드, SceneManager.LoadScene 재시작)

### Prior Phase Context
- `.planning/phases/01-foundation-movement/01-CONTEXT.md` — D-11: 카메라 LateUpdate 직접 구현 (Cinemachine 미사용)
- `.planning/phases/03-enemy-system/03-CONTEXT.md` — D-14/D-15: OnPlayerDeath 이벤트 구조, D-16: PlayerInvincible 레이어 스왑

### Existing Code (Integration Points)
- `Assets/Scripts/World/FloorManager.cs` — `static int CurrentFloor` 필드. Phase 5에서 스폰 로직 추가 필요 (또는 별도 FloorSpawner 컴포넌트 분리)
- `Assets/Scripts/UI/HUDController.cs` — `FloorManager.CurrentFloor` 매 프레임 읽어 `_floorLabel` 업데이트. 자동 연동됨.
- `Assets/Scripts/UI/DeathScreenController.cs` — `RestartGame()`: `FloorManager.CurrentFloor = 1` + `SceneManager.LoadScene(0)`. 재시작 시 모든 스폰된 Room 자동 파괴됨.
- `Assets/Prefabs/Rooms/` — 14개 Room 폴더 (현재 빈 폴더). 4~5개에 프리팹 콘텐츠 채울 것.

No external specs — requirements fully captured in decisions above.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `FloorManager` (static class) — `CurrentFloor` 필드 이미 HUD, DeathScreen과 연결됨. `FloorSpawner` MonoBehaviour를 별도로 만들고 내부에서 `FloorManager.CurrentFloor`를 업데이트하는 패턴 추천.
- `PlayerController.OnPlayerDeath` static event — 전환 시퀀스 중 사망 처리는 이 이벤트 그대로 사용.
- `InvincibilityHandler` — 층 전환 시퀀스 중 플레이어 무적 부여에 재사용 가능 (적 활성화 직후 잠깐 무적 — 선택사항).
- `MeleeEnemy.cs`, `RangedEnemy.cs` — `Instantiate()` 대상. FSM은 이미 완성됨.

### Established Patterns
- 타이머: `WaitForSecondsRealtime` — 층 전환 시퀀스 Coroutine 내 딜레이에 사용 (시간정지 timeScale 영향 없어야 함)
- 적 비활성화 패턴: `gameObject.SetActive(false/true)` — Phase 3 FLOOR-03 요건 충족
- 정적 데이터 클래스: `FloorManager`처럼 데이터만 가진 static class — 씬 오브젝트 없이 전역 상태 관리

### Integration Points
- `FloorManager.CurrentFloor` 증가 → `HUDController.Update()`가 자동으로 floor label 갱신 (SetText로 zero-alloc)
- `PlayerController`에 입력 잠금 메서드 추가 필요 (`LockInput()` / `UnlockInput()`) — 전환 시퀀스 1단계/6단계
- `FloorSpawner`가 씬 Awake 시 1층 고정 Room 스폰 → 이후 플레이어 출구 트리거 감지 시 다음 층 스폰 루프

</code_context>

<specifics>
## Specific Ideas

- 1층 고정 Room: 가장 단순한 평지 구성 — 플레이어가 조작법을 익힐 여유
- 출구 트리거 자식 오브젝트: `RoomExit` 태그 또는 레이어로 구분, `OnTriggerEnter2D`에서 `FloorSpawner.AdvanceFloor()` 호출
- 다음 층은 현재 층 출구 바로 위에 스폰 (Y 오프셋 = Room 높이)
- 전환 시퀀스 시간 총합: Claude 재량 (권장 0.3~0.5초 — 너무 짧으면 원거리 적에게 맞을 수 있음)

</specifics>

<deferred>
## Deferred Ideas

- 모바일 온스크린 컨트롤 (MOBI-01, MOBI-02) — v2 Requirements
- 층 난이도 커브 세밀 조정 — 플레이테스트 후
- Room 레이아웃 14개 완전 채우기 — v2 콘텐츠 확장
- 복잡한 순찰 경로/웨이포인트 적 AI — v2 범위

</deferred>

---

*Phase: 05-procedural-map-infinite-stages*
*Context gathered: 2026-06-17*
