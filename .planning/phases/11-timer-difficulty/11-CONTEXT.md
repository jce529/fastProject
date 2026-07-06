# Phase 11: 타이머 & 난이도 - Context

**Gathered:** 2026-07-07
**Status:** Ready for planning

<domain>
## Phase Boundary

층 진입 시 HUD에 제한시간 카운트다운이 표시되고(TIMER-01), 시간 초과 시 게임오버가 발생하며(TIMER-02), 층 번호가 올라갈수록 스포너에서 생성되는 몬스터 수가 증가한다(DIFF-01). 논의 중 사용자 결정으로 SCORE-01/02(남은 시간 비례 점수 누적 + HUD 표시)도 이번 Phase 범위에 포함한다 — 타이머 값이 점수 계산에 직접 필요하므로 자연스러운 확장.

**Requirements in scope:** TIMER-01, TIMER-02, DIFF-01, SCORE-01, SCORE-02 (REQUIREMENTS.md에 미매핑 상태였던 SCORE-01/02를 이번 Phase로 흡수 — 사용자 확정)
**Not in scope:** 층 전환/EXIT 포탈 로직 자체(Phase 10에서 완료), 새로운 사망 연출 UI(기존 DeathScreenController 재사용)

</domain>

<decisions>
## Implementation Decisions

### 제한시간 & 점수 공식
- **D-01:** 제한시간은 모든 층 동일하게 **60초 고정**. 층수에 따른 감소 없음.
- **D-02:** 남은 시간 → 점수 환산 공식: **남은 초 × 10점**. 예: 45초 남기고 클리어 시 +450점. `ScoreManager.KillScore`(100점)보다 낮은 스케일로 의도적으로 설정됨 — 시간 보너스가 킬 점수를 압도하지 않도록.
- **D-02b:** 점수 지급 시점은 EXIT 포탈 진입 순간(`WorldGenerator.EnterPortal`/`FloorTransitionSequence` 시작 시점) — 그 시점의 남은 시간 값을 사용.

### 난이도 스케일링 커브
- **D-03:** 몬스터 수 증가는 **기존 `FloorSpawner.GetEnemyCount(int floor)`의 계단식 테이블을 그대로 재사용**한다 (신규 설계 없음):
  - 1~5층: 근접 `Random.Range(2,4)` + 원거리 `Random.Range(0,2)`
  - 6~10층: 근접 2 + 원거리 `Random.Range(1,3)`
  - 11층+: 근접 2 + 원거리 `Random.Range(2,4)`
  - 이 메서드를 `WorldGenerator`로 이식(또는 호출 가능하게 이동)한다. `FloorSpawner.cs`는 Phase 9/10 결정대로 미사용 고아 코드로 그대로 둔다 — 로직만 참고해 복제.

### 적 스폰 활성화 타이밍
- **D-04:** 새 Room이 `WorldGenerator`에 의해 Instantiate되는 즉시 해당 Room의 `EnemySpawner` 마커에 대해 `Spawn(meleePrefab, rangedPrefab)` + `Activate()`를 바로 호출한다. Phase 5의 "플레이어 진입 시점 활성화" 패턴은 사용하지 않는다 — lookahead로 미리 생성된 룸이라도 적이 이미 순찰/대기 상태인 것을 허용.
- **D-04b:** 몇 개의 `EnemySpawner` 마커를 활성화할지는 D-03의 `(melee, ranged)` 카운트를 그 룸의 `EnemySpawner` 목록(타입별로 필터링) 앞에서부터 개수만큼 선택 — 마커 수보다 요청 카운트가 많으면 있는 만큼만 활성화(초과분 무시, 에러 없음).

### 시간초과 연출 & 사망 처리
- **D-05:** 타이머 임박 경고 — **남은 시간이 줄어들수록 HUD 타이머 텍스트가 빨간색으로, 점점 더 빠르게 점멸**한다. `InvincibilityHandler.cs`의 플리커 패턴(코루틴 + `WaitForSecondsRealtime` 간격 토글)을 참고하되, 간격이 남은 시간에 반비례해 좁아지도록(초반엔 느리게, 0에 가까워질수록 빠르게) 구현한다. 색상은 평소 흰색 → 빨간색으로 전환.
- **D-06:** 시간이 0에 도달하면 `PlayerController.OnPlayerDeath` 이벤트를 직접 invoke한다 — 기존 `PlayerDeathHandler`/`DeathScreenController` 사망 플로우를 그대로 재사용하며, 별도의 타임아웃 전용 사망 연출은 만들지 않는다.

### FloorTimer 아키텍처 (사전 결정, STATE.md)
- **D-07 (재확인):** `FloorTimer`는 `FloorManager`/`ScoreManager`와 동일하게 **정적 클래스**로 구현한다 — 씬 수명 불필요, 데이터 전용. `Time.unscaledDeltaTime` 또는 `Time.unscaledTime` 기반으로 슬로우모션(`Time.timeScale`)에 면역이어야 한다 (ScoreManager.StartRoomTimer 패턴과 동일).
- **D-08:** 층 전환마다(EXIT 포탈 진입 + 게임 시작 시) 타이머가 60초로 리셋되어야 한다 — `WorldGenerator.Start()`와 `FloorTransitionSequence()` 양쪽에 리셋 호출 필요 (EXIT-03 관련 기존 코드 지점과 동일한 훅 포인트).

### Claude's Discretion
- 타이머 점멸 간격의 정확한 수치 곡선(예: Lerp 함수, 최소/최대 간격 값)은 플래너/실행자 재량.
- `WorldGenerator`에 `_meleeEnemyPrefab`/`_rangedEnemyPrefab` Inspector 필드를 신규 추가하는 방식(FloorSpawner와 동일 패턴)은 실행자 재량이나, 반드시 필요함(현재 WorldGenerator에는 이 필드가 없음).
- HUD 타이머 표시 형식(초 단위 숫자만 vs MM:SS)은 실행자 재량 — 60초 고정이므로 단순 초 표시로 충분해 보이나 강제하지 않음.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements & Roadmap
- `.planning/ROADMAP.md` §Phase 11 — TIMER-01/02, DIFF-01 성공 기준 정의
- `.planning/REQUIREMENTS.md` §타이머 & 게임오버 (TIMER), §난이도 스케일링 (DIFF), §점수 시스템 (SCORE) — SCORE-01/02는 이번 Phase로 흡수됨(사용자 확정), REQUIREMENTS.md의 Traceability 테이블을 Phase 11로 갱신 필요
- `.planning/STATE.md` §Technical Constraints to Enforce Every Phase — 타이머는 반드시 `Time.unscaledDeltaTime` 사용
- `.planning/STATE.md` §Key Decisions for v3.0 — "FloorTimer = 정적 클래스" (ScoreManager 패턴 답습, 사전 확정)

### Existing Code Patterns (재사용 대상)
- `Assets/Scripts/World/FloorSpawner.cs` (line 208-217, `GetEnemyCount(int floor)`) — 난이도 스케일링 테이블 원본, 로직 복제 대상
- `Assets/Scripts/World/EnemySpawner.cs` — 적 마커 컴포넌트, `Spawn(meleePrefab, rangedPrefab)` + `Activate()` API, 현재 WorldGenerator가 호출하지 않는 상태
- `Assets/Scripts/World/ScoreManager.cs` — 정적 클래스 패턴, `Time.unscaledTime` 기반 타이머, `AddKillScore()`/`AddRoomClearBonus()` 참고
- `Assets/Scripts/World/FloorManager.cs` — 가장 단순한 정적 클래스 예시(`public static int CurrentFloor`)
- `Assets/Scripts/Player/InvincibilityHandler.cs` — 코루틴 기반 플리커(점멸) 패턴, `WaitForSecondsRealtime` 간격 토글 — 타이머 경고 점멸 구현 시 참고
- `Assets/Scripts/Player/PlayerController.cs` — `OnPlayerDeath` 정적 이벤트 정의부
- `Assets/Scripts/Player/PlayerDeathHandler.cs` — `OnPlayerDeath` 구독 후 사망 처리, 수정 불필요(그대로 재사용)
- `Assets/Scripts/UI/HUDController.cs` — 기존 HUD 슬롯(`_floorLabel`, `_scoreLabel`, `_gaugeFill`, `_attackTypeLabel`) 옆에 타이머 라벨 추가 필요
- `Assets/Scripts/World/WorldGenerator.cs` — `Start()`, `FloorTransitionSequence()`에 타이머 리셋 + 적 스폰 호출 훅 추가 지점 (`TrySpawnExitPortal()` 호출 패턴과 동일 위치에 적 스폰 로직 삽입 가능)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `HUDController._scoreLabel`: 이미 존재하는 TMP 슬롯 — `ScoreManager.Score`를 표시 중, SCORE-02는 이 필드가 이미 충족(수정 불필요할 수도 있음, 실행자 확인)
- `ScoreManager.Score` / `AddKillScore()`: SCORE 요구사항의 기존 누적 지점 — SCORE-01의 "남은 시간 비례 점수"는 여기에 새 메서드(`AddTimeBonus` 등)로 추가하는 형태가 자연스러움
- `EnemySpawner.Spawn()/Activate()`: 이미 완성된 API — WorldGenerator에서 호출만 추가하면 됨(재구현 불필요)

### Established Patterns
- 정적 클래스 데이터 전용 패턴(`FloorManager`, `ScoreManager`) — `FloorTimer`도 동일하게 구현
- 모든 타이머/쿨다운은 `Time.unscaledDeltaTime`/`Time.unscaledTime` — 슬로우모션(Time.timeScale≈0.2) 면역 필수
- 플리커(점멸) 구현은 코루틴 + `WaitForSecondsRealtime` 간격 토글(`InvincibilityHandler` 패턴)

### Integration Points
- `WorldGenerator.Start()` — 타이머 시작 호출 지점 추가 (게임 시작 시 60초 시작)
- `WorldGenerator.FloorTransitionSequence()` — 층 전환마다 타이머 리셋 호출 지점 추가 (기존 Step 구조에 삽입)
- `WorldGenerator.SpawnNextPair()`/`SpawnPrevPair()`/`Start()`의 room Instantiate 직후 — `TrySpawnExitPortal(room)` 호출과 나란히 신규 적 스폰 로직(`TrySpawnEnemies(room)` 등) 삽입
- `HUDController.Update()` — 타이머 라벨 갱신 + 점멸 색상/속도 로직 추가

</code_context>

<specifics>
## Specific Ideas

- 타이머 경고 점멸: "남은 시간이 적을수록 플레이어가 점점 빠르게 점멸했으면 좋겠어 빨간색으로" (사용자 원문) — 타이머 텍스트가 빨간색으로 바뀌고, 남은 시간이 줄어들수록 점멸 주기가 빨라지는 시각 효과.

</specifics>

<deferred>
## Deferred Ideas

없음 — 논의가 Phase 범위 내에 머무름. SCORE-01/02는 새 capability가 아니라 REQUIREMENTS.md에 이미 정의되어 있던 미매핑 요구사항이었으므로 scope creep이 아닌 정식 편입으로 처리.

</deferred>

---

*Phase: 11-timer-difficulty*
*Context gathered: 2026-07-07*
