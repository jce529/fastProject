# Phase 16: 보스 룸 콘텐츠 & 생명주기 게이팅 (+ 관련 코드 리팩토링) - Context

**Gathered:** 2026-07-15
**Status:** Partial — 리팩토링 범위는 확정, 보스 룸 기능 자체(BOSS-01/02/07/09/10)의 그레이 에어리어는 **아직 미논의**

<domain>
## Phase Boundary

ROADMAP.md에서 2026-07-15 discuss-phase 세션 중 공식 확장됨(커밋 `0f5bee2`):

1. **보스 룸 콘텐츠 & 생명주기 게이팅** (BOSS-01, BOSS-02, BOSS-07, BOSS-09, BOSS-10, SFX-05) — 보스 룸이 EXIT 포탈처럼 확률적으로 스폰되어 솔로 전투를 보장하고, 입장 시 카메라 연출+전용 스폰 사운드가 재생되며, 층 타이머가 일시정지되고, 전투 중에는 WorldGenerator 정리 로직에서 예외 처리된다.
2. **관련 코드 리팩토링** — 위 기능이 통합되는 기존 코드(WorldGenerator, CombatController, MeleeEnemy/RangedEnemy 등)에서 발견된 중복/죽은 코드/설계 문제를 정리한다.

**중요:** 이번 세션은 사용자 요청으로 (2) 리팩토링 트랙에 집중했다. (1) 보스 룸 기능 자체의 그레이 에어리어(스폰 아키텍처, 입장 연출 스타일, 아레나 콘텐츠, 전투 판정/생명주기 게이팅 세부 조건)는 **처음에 4개 후보로 제시했으나 사용자가 리팩토링 논의로 방향을 튼 이후 다루지 않았다.** `/gsd:plan-phase 16` 실행 전에 별도 세션에서 이 부분을 마저 논의해야 한다.

</domain>

<decisions>
## Implementation Decisions — 리팩토링 트랙 (확정)

### 죽은 파일 삭제
- **D-01:** `TestWorldGenerator.cs` 삭제 — 씬/프리팹 어디에도 GUID 참조 없음(daef694ac78bbc74ab335152fd964b50), 100% 죽은 코드로 확인됨.
- **D-02:** `FloorSpawner.cs` 삭제 — WorldGenerator(Phase 9)로 대체됨. SampleScene.unity에 남은 유일한 참조는 `m_IsActive: 0`(비활성화)인 GameObject 하나뿐. **이 GameObject의 씬 제거는 사용자가 Unity 에디터에서 직접 처리** (Claude는 .cs 파일만 삭제).
- **D-03:** `RoomExit.cs` 삭제 — ExitPortal(Phase 10)로 대체됨. `FloorSpawner.Instance?.AdvanceFloor()`를 호출하는데 FloorSpawner가 항상 비활성이라 영구 no-op. 실제 컴포넌트는 구형 Room_*.prefab 14종 + Room_Debug.prefab에 여전히 부착돼 있으나, **그 프리팹들 자체를 사용자가 Unity 에디터에서 통째로 삭제하기로 결정**(아래 참고) — 따라서 스크립트만 지워도 됨.

### 죽은 상수 삭제
- **D-04:** `MeleeEnemy.cs`/`RangedEnemy.cs`의 `LayerPlayerHurtbox`/`LayerPlayerInvincible` 상수 삭제 — 어디서도 참조되지 않음. D-16 주석에 "Physics2D 충돌 매트릭스로 이미 처리되어 코드 체크 불필요"라고 되어 있어, 매트릭스 방식으로 전환되며 상수만 남은 것으로 확인.

### EnemyBase 공통 베이스 클래스 (최소 범위)
- **D-05:** `MeleeEnemy`/`RangedEnemy`가 100% 동일하게 복붙하고 있는 부분만 `EnemyBase` 추상 클래스로 추출한다 — 사용자 지시: "일단 서로 공유할 최소한의 내용들만 만들어줘" (풀 상속 리팩토링 아님, 최소 공통분모만).
  - 대상: `OnDashHit()`의 공통 부분(가드+IsAlive=false, rb 정지, 콜라이더 비활성화, animator isDead, EnemyDeathEffect 트리거), `ClearHighlight()`, `IsPlayerInRange()`, `OnEnable()`/`OnDisable()`의 `PlayerController.OnPlayerDeath` 구독/해제, `SetSpawnGate(bool)`.
  - MeleeEnemy 고유(텔레그래프 이동+공격 히트박스, 점프/gap 회피)와 RangedEnemy 고유(조준선+발사체, 카이팅)는 각 서브클래스에 그대로 남긴다.
  - `IEnemy`/`ISpawnGatable` 인터페이스 계약 자체는 변경하지 않는다 — `EnemyBase`가 두 인터페이스를 구현하고 Melee/Ranged가 상속.
  - Boss Enemy(Phase 15, 아직 미구현)도 이 `EnemyBase`를 상속하도록 향후 설계하면 3번째 복붙을 방지할 수 있음 — 단 이번 Phase 범위는 아님, Phase 15 계획 시 참고.

### CombatController 정리
- **D-06:** `FindNearestEnemyInRange()`에서 Linear/Fan 분기가 거의 동일하게 복붙한 마우스→월드 방향 계산(`mousePos`/`mouseWorld` vs `mousePos2`/`mouseWorld2`)을 헬퍼로 통합한다.
- **D-07:** `DashOrWhiff()`/`ExecuteDash()`에 남은 디버깅용 `Debug.Log` 호출들을 정리한다(콘솔 노이즈 — 예전 코루틴/타이밍 버그 추적 흔적으로 추정).

### 점수 시점 재설계 — "공격 시" → "사망 시"
- **D-08:** `ScoreManager.AddKillScore(isRespawnKill)` 호출을 `CombatController.ExecuteDash()`에서 제거하고, `MeleeEnemy.OnDashHit()`/`RangedEnemy.OnDashHit()`가 각자 `IsAlive = false`를 커밋하는 바로 그 지점(=진짜 사망이 확정되는 순간)에서 직접 호출하도록 이동한다.
  - `isRespawnKill` 판정(`GetComponent<RespawnedEnemyMarker>() != null`)도 각 적이 스스로 수행 — `CombatController`가 대신 판정해주던 것을 각 적이 자기 자신에 대해 판정.
  - `IEnemy` 3-member 계약(`IsAlive`, `OnDashHit()`, `ClearHighlight()`)은 변경 없음 — 시그니처 그대로.
  - 기존 두 적 타입은 "맞음 = 즉사"라 타이밍 변화 없음(동일 프레임, 동일 지점).
  - **동기:** Phase 15(미실행)의 15-CONTEXT.md D-12가 "보스는 매 비치명타마다 방금 CombatController가 적립한 KillScore를 스스로 상쇄(차감)한다"는 우회책을 이미 계획해뒀는데, 이 재설계로 그 우회책 자체가 불필요해짐 — BossEnemy는 7번째(치명타) 히트에서만 `IsAlive=false`+점수 호출을 하면 되고, 1~6회차는 아예 점수 관련 호출을 하지 않으면 됨. 이 결정은 15-CONTEXT.md에도 반영 필요(아래 `<canonical_refs>` 참고 — 실제 갱신은 이 discuss-phase 세션에서 직접 수행).

### Claude's Discretion
- `EnemyBase` 추출 시 정확한 메서드 시그니처/protected 필드 이름
- `CombatController`의 마우스 방향 헬퍼 메서드 이름/시그니처
- Debug.Log 정리 범위(전부 삭제 vs 핵심 몇 개만 유지) — 기본값은 전부 삭제, 남길 이유가 있으면 유지

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 로드맵/요구사항
- `.planning/ROADMAP.md` §Phase 16 — 2026-07-15 커밋 `0f5bee2`로 리팩토링 범위 공식 확장됨. Success Criteria 6번("기존 코드 안의 중복 로직/이상 패턴이 식별되어 정리된다")이 이 CONTEXT.md의 D-01~D-08을 가리킴.
- `.planning/REQUIREMENTS.md` §보스 룸(BOSS) — BOSS-01/02/07/09/10, SFX-05 (아직 미논의 — 아래 `<deferred>` 참고)

### 이번 세션에서 함께 갱신된 문서 (교차 참조 필수)
- `.planning/phases/15-fsm/15-CONTEXT.md` D-11 — Room_Debug.prefab 삭제로 인해 "Phase 15 FSM 검증은 DebugRoomTeleporter+Room_Debug로 진행" 전제가 깨짐. Phase 15 실행 전 재계획 필요하다고 갱신됨.
- `.planning/phases/15-fsm/15-CONTEXT.md` D-12 — 점수 상쇄 우회책이 D-08(본 문서) 재설계로 불필요해졌다고 갱신됨.

### 이미 완료된 선행 작업
- `.planning/quick/260715-kci-worldgenerator-cs-removetail-removehead-/` — WorldGenerator.cs의 RemoveTail/RemoveHead/FloorTransitionSequence 3중 중복 정리 완료(CleanupSection() 헬퍼). 커밋 `a57e847`, `646a5f4`, `f997a13`, `4ec0928`.

### 리팩토링 대상 파일
- `Assets/Scripts/Enemy/MeleeEnemy.cs`, `Assets/Scripts/Enemy/RangedEnemy.cs` — D-04(상수), D-05(EnemyBase), D-08(점수) 대상
- `Assets/Scripts/Enemy/IEnemy.cs` — 계약 변경 없음, 확인만
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` — EnemyBase 추출 시 `OnDashHit()`의 AddComponent+StartCoroutine 패턴 그대로 재사용
- `Assets/Scripts/Enemy/RespawnedEnemyMarker.cs` — D-08 isRespawnKill 판정에 사용
- `Assets/Scripts/World/ScoreManager.cs` — D-08, `AddKillScore(bool isRespawn=false)` 호출부만 이동, 메서드 자체는 무변경
- `Assets/Scripts/Player/CombatController.cs` — D-06, D-07, D-08
- `Assets/Scripts/World/TestWorldGenerator.cs`, `Assets/Scripts/World/FloorSpawner.cs`, `Assets/Scripts/World/RoomExit.cs` — D-01~D-03, 파일 삭제 대상

</canonical_refs>

<code_context>
## Existing Code Insights

### 죽은 코드 조사 방법론 (재사용 가능)
GUID 기반 교차 검증으로 "정말 안 쓰이는지"를 코드 레벨에서 확인했다:
1. `.cs.meta` 파일에서 스크립트의 GUID 추출
2. `grep -rl <guid> Assets/ --include="*.prefab" --include="*.unity" --include="*.asset"`로 실제 참조 여부 확인
3. 참조가 있다면 `m_IsActive`/`m_Enabled` 값과 부모 컨텍스트를 읽어 "활성 상태로 실제 동작 중인지" vs "비활성 잔해인지" 구분

이 방법으로 `Room_Debug.prefab`이 사실 14개의 `DebugRoomTeleporter` 컴포넌트를 가진 "테스트 허브"였다는 걸 발견함 — 각각 구형 `Room_*.prefab`을 `targetRoomPrefab`으로 가리키고 있었음. 겉보기엔 죽은 것 같던 14개 프리팹이 실제로는 이 허브에 연결되어 있었던 것 — 삭제 전 반드시 이런 교차 확인이 필요하다는 선례.

### Established Patterns
- `MeleeEnemy`/`RangedEnemy`의 `OnDashHit()` 보일러플레이트: 가드 → `IsAlive=false` → 코루틴/텔레그래프 정지 → rb 고정 → 콜라이더 비활성화 → `animator.SetBool("isDead", true)` → `EnemyDeathEffect` AddComponent+StartCoroutine. `EnemyBase`로 추출 시 이 순서를 그대로 유지해야 함(순서를 바꾸면 콜라이더 비활성화 전에 물리 이벤트가 한 번 더 발생하는 등의 회귀 가능).
- `AddKillScore`/`AddBossKillScore` 류 정적 메서드는 항상 `ScoreManager`에 추가 — 호출부만 이동, 메서드 자체 위치는 불변.

</code_context>

<specifics>
## Specific Ideas

- 사용자가 코드 리뷰를 요청한 계기: "코드 흐름들을 살펴보고 이상한 부분들에 대해 논의하고 리팩토링" — 사전에 정해진 리팩토링 목록이 있던 게 아니라, WorldGenerator.cs부터 시작해 대화 중 발견한 순서대로 CombatController.cs → MeleeEnemy/RangedEnemy.cs → (프리팹/씬 조사)로 확장됨.
- 점수 시점 변경(D-08)은 사용자가 먼저 제안: "적들의 사망시 이벤트에 따라서 점수를 얻도록 바꾸고싶어... 보스를 만들 때도 별도의 점수 제거용 코드를 넣지 않아도 잘 작동할거야" — Phase 15 미실행 시점에 발견되어 재작업 없이 바로 반영 가능했던 케이스.
- Unity 에디터가 필요한 작업(씬 GameObject 제거, 프리팹 삭제)은 명시적으로 사용자가 직접 처리하기로 함 — Unity MCP 연결이 "Connection revoked" 상태로 확인되어 Claude가 직접 실행 불가능했던 것도 배경 중 하나.

</specifics>

<deferred>
## Deferred Ideas

### 보스 룸 기능 자체의 그레이 에어리어 — 미논의, 다음 세션 필수
처음 4개 후보로 제시했으나 논의되지 않음. `/gsd:plan-phase 16` 전에 반드시 다뤄야 함:
- **보스 룸 스폰 아키텍처**: 체인 슬롯 교체(Complex_Room 대체) vs 브랜치 포탈 — BOSS-10(전투 중 정리 예외)이 `_chain` 노드 기준으로 동작하려면 이 결정이 구조를 좌우함.
- **입장 연출**: BOSS-09 카메라 "잠금/줄임" 스타일(정적 스냅 vs 애니메이션 줌) + SFX-05 전용 스폰 사운드(재사용/피치변형 vs 신규 클립).
- **보스 룸 콘텐츠 & 아레나 구조**: PROJECT.md가 "아레나 구조도 고유"라고 명시 — Complex_Room 대비 크기/레이아웃 차별화 방향.
- **전투 판정 & 생명주기 게이팅 세부조건**: BOSS-10 예외의 정확한 트리거 조건(전투 시작/종료 판정 기준), 타이머 재개 조건, 보스 룸 이탈/재진입 시나리오 처리.

### Phase 15 재계획 필요 (파생 이슈)
Room_Debug.prefab 삭제로 15-CONTEXT.md D-11의 "DebugRoomTeleporter+Room_Debug로 즉시 스폰/테스트" 전제가 깨짐. Phase 15를 실제로 계획/실행하기 전에 보스 FSM 격리 테스트 환경을 다시 정해야 함 (예: 새 미니멀 테스트 룸 제작, 또는 Complex_Room 중 하나를 임시 활용 등 — 옵션은 Phase 15 재논의 시 결정).

### Unity 에디터 측 정리 (사용자 직접 처리)
- SampleScene.unity의 비활성 "FloorSpawner" GameObject 제거
- `Room_Debug.prefab` 전체 삭제
- 구형 `Room_*.prefab` 14종(Room_Chase/Combat/Crossroad/Dodge/Fall/Gap/Hunt/Ladder/LadderDanger/Mixed/Recovery/Sniper/Stair/Chain) 전체 삭제

</deferred>

---

*Phase: 16-boss-room-lifecycle*
*Context gathered: 2026-07-15*
