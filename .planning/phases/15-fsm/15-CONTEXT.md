# Phase 15: 보스 FSM & 빈틈 타겟팅 - Context

**Gathered:** 2026-07-15
**Status:** Ready for planning

<domain>
## Phase Boundary

보스가 예고(텔레그래프)→빈틈(피격 가능 창) 공격 패턴을 반복하고, 빈틈 상태에서만 플레이어의 돌진 공격 대상이 되며, 7회 피격 시 처치되고(매 피격마다 패턴이 처음부터 재시작, 진행률은 화면에 노출되지 않음), 처치 시 ScoreManager에 점수 보너스가 지급된다. (BOSS-03, BOSS-04, BOSS-05, BOSS-06)

**이 Phase는 보스 FSM 자체에만 집중한다.** 보스 룸의 확률적 스폰, 솔로 전투 보장, 카메라 연출, 층 타이머 일시정지, WorldGenerator 정리 예외(Phase 16), WorldGenerator 실통합 및 보스 처치 후 EXIT 흐름(Phase 17)은 범위 밖 — 이번 Phase는 `DebugRoomTeleporter` 확장을 통한 격리 테스트로 검증한다. 보스 전용 아트(스프라이트/애니메이션) 제작도 범위 밖 — 기존 적 스프라이트를 크기/색상만 변형해 재활용한다.

</domain>

<decisions>
## Implementation Decisions

### 공격 패턴 & 빈틈 디자인
- **D-01:** 보스의 예고(텔레그래프) 공격은 근접형(돌진/휘두르기) — `MeleeEnemy`의 근접 히트박스 공격 패턴을 참고해 구현한다.
- **D-02:** '빈틈' 상태는 정지(움직임 멈춤) + 색상 변화(스프라이트 색상/하이라이트) 이중으로 시각 표현한다 — 둘 중 하나만으로는 불충분하다는 것이 사용자 판단.
- **D-03:** 빈틈 상태 지속시간은 넉넉하게(~0.8~1.2초)로 설정 — `MeleeEnemy` 기존 텔레그래프(0.45초)보다 길게, 보스전 특유의 "읽기 쉬운" 리듬을 준다. 정확한 수치는 Claude's Discretion.
- **D-04:** 예고(텔레그래프) 중에는 `MeleeEnemy` D-05(999.4)와 동일한 방식으로 — 정지하지 않고 느려진 속도로 이동하며 예고한다.
- **D-05:** 예고→빈틈 루프는 **단일 패턴 반복**으로 구현한다 — 여러 공격 패턴 종류의 순환은 이번 Phase 범위 밖(향후 프레임워크 확장 후보). Claude's Discretion은 반복 주기/수치 조정에만 적용된다.

### 피격 & 패턴 리셋 피드백
- **D-06:** 빈틈 상태에서 플레이어의 돌진 공격이 적중하면 보스 전용 피격 반응 연출이 재생된다 — **프로그래매틱 연출**(코드로 구현: 색상 플래시 + 짧은 넉백/스태거 + 기존 히트스파크 조합 등)이며, 보스 전용 스프라이트/애니메이션 클립은 필요하지 않다(현재 보스 아트 자체가 없으므로).
- **D-07:** 피격 후 공격 패턴이 처음부터 재시작될 때는 **짧은 공백을 둔 뒤 리셋**한다 — 즉시 리셋이 아니라 히트 반응 연출(D-06)이 재생될 시간을 확보한 후 새 사이클을 시작한다. 정확한 공백 시간은 Claude's Discretion.

### 처치 연출 & 점수 보너스
- **D-08:** 7회째 피격으로 처치될 때는 기존 `EnemyDeathEffect` 시퀀스(Die 애니메이션 → 파티클 → SpriteMask 상승 페이드 → Destroy)를 기반으로 재사용하되, 보스 전용으로 연출을 연장(더 길거나 과장됨) — 구체적 연장 방식(파티클 강화, 지속시간 증가, 카메라 쉐이크 추가 등)은 Claude's Discretion.
- **D-09:** 보스 처치 시 점수 보너스는 `ScoreManager`에 신규 상수/메서드로 추가하며, 정확한 수치는 Claude's Discretion — 일반 `KillScore`(100) 대비 유의미하게 큰 값(가이드: 500~1000 사이 권장)을 사용한다.

### 테스트 환경 & 보스 비주얼
- **D-10:** 보스 스프라이트/애니메이션은 **기존 적 스프라이트를 재활용**한다(크기를 키우거나 색조를 변형하는 정도) — 신규 아트 제작(Unity_AssetGeneration 등)은 이번 Phase에서 진행하지 않는다. 실제 보스 전용 아트는 보스 룸(Phase 16) 이후로 미룬다.
- **D-11:** Phase 15의 FSM 검증은 `DebugRoomTeleporter`를 확장해 보스 프리팹 스폰 필드를 추가하는 방식으로 진행한다 — 기존 `_meleePrefab`/`_rangedPrefab` Inspector 필드와 동일한 패턴으로 보스 프리팹 필드를 추가해 `Room_Debug`에서 즉시 스폰/테스트할 수 있게 한다. 보스 룸 콘텐츠(Phase 16)나 WorldGenerator 통합(Phase 17)은 필요하지 않다.
- **D-12:** 보스의 6회 비치명타(1~6회)는 일반 `KillScore`(+100) 점수를 적립하지 않는다 — 오직 7회째(처치) 시에만 D-09 보너스가 적립된다. `CombatController.ExecuteDash()`는 매 `OnDashHit()` 호출 후 무조건 `AddKillScore()`를 호출하므로(락된 "CombatController 무변경" 전제 유지), `BossEnemy.OnDashHit()`이 비치명타를 받을 때마다 방금 적립된 `KillScore`만큼을 스스로 상쇄(차감)하는 방식으로 구현한다 — `CombatController`/`IEnemy` 계약은 손대지 않는다. 정확한 상쇄 메커니즘(예: `ScoreManager`에 `SubtractScore` 유틸 추가)은 Claude's Discretion.

### Claude's Discretion
- 빈틈 상태 정확한 지속시간 수치 (0.8~1.2초 범위 내)
- 피격 반응 연출의 구체적 구현(색상 플래시 강도, 넉백 거리, 히트스파크 배치 등)
- 피격 후 리셋 전 공백 시간 정확한 수치
- 사망 연출 "보스 전용 연장"의 구체적 방식(파티클 규모, 지속시간, 카메라 쉐이크 강도 등)
- 처치 점수 보너스 정확한 수치 (500~1000 권장 범위 내)
- 기존 적 스프라이트를 보스처럼 보이게 하는 크기/색상 변형 값
- 텔레그래프 이동 속도 배율, 돌진/휘두르기 공격의 정확한 구현 방식(단순 돌진 vs 히트박스 스윕)
- D-12 비치명타 점수 상쇄의 정확한 구현 방식(예: `ScoreManager.SubtractScore(int)` 신규 메서드 vs 기존 `Score` 프로퍼티 직접 조작)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 요구사항 / 로드맵
- `.planning/ROADMAP.md` §Phase 15 — 목표, Depends on Phase 14, Success Criteria 5개(BOSS-03~06 대응), Implementation Notes(보스는 `MonoBehaviour, IEnemy`로 IEnemy 계약 변경 없이 대시킬 가능, 빈틈 타겟팅은 `FindNearestEnemyInRange()`의 기존 `!IsAlive` 스킵 체크 재사용, 보스는 플레이어를 원샷킬하되 텔레그래프만 보스 전용으로 더 길게)
- `.planning/REQUIREMENTS.md` §보스 룸(BOSS) — BOSS-03(예고→빈틈 루프), BOSS-04(7회 피격+패턴 리셋), BOSS-05(진행률 비노출), BOSS-06(점수 보너스), §Out of Scope 표(보스 HP바/멀티페이즈 전투 명시적 배제)
- `.planning/STATE.md` §Key Decisions Locked for v3.1 — "보스는 7회 피격으로 처치(HP바 없음, 진행률 비노출)", "빈틈 상태에서만 타겟팅 허용 — `!IsAlive` 스킵 체크 재사용", "빌드 순서: Audio → Spawn VFX → Boss FSM → Boss Room → WorldGenerator 통합"

### 재사용 대상 기존 코드 (근접 FSM 패턴)
- `Assets/Scripts/Enemy/MeleeEnemy.cs` — 4상태 FSM(`Idle→Chase→Telegraph→Attack`) 전체 구조, 특히 `TelegraphAndAttack()` 코루틴(D-04/D-05 999.4 "이동하며 예고" 패턴 — 본 Phase D-01/D-04가 이 구조를 보스의 예고 단계에 재사용), `OnDashHit()`(사망 트리거 → `EnemyDeathEffect` 연결 패턴)
- `Assets/Scripts/Enemy/IEnemy.cs` — 3-member 계약(`IsAlive`, `OnDashHit()`, `ClearHighlight()`) — **변경 금지**, `BossEnemy`가 이 인터페이스를 구현
- `Assets/Scripts/Enemy/ISpawnGatable.cs` — 스폰 연출 게이팅 계약(Phase 14) — 보스도 스폰 연출을 거치므로 동일 구현 필요
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` — 사망 시퀀스(Die 애니메이션 대기 → 파티클 → SpriteMask 페이드 → Destroy) — D-08 보스 전용 연장의 베이스
- `Assets/Scripts/Player/CombatController.cs` — `FindNearestEnemyInRange()`(363행 부근)의 `!enemy.IsAlive` 스킵 체크(401행) — 빈틈 게이팅 재사용 지점(빈틈이 아닐 때 `IsAlive=false`로 만들어 타겟 후보에서 자동 제외)

### 점수 시스템
- `Assets/Scripts/World/ScoreManager.cs` — `KillScore=100`, `AddKillScore(bool isRespawn=false)` 패턴 — D-09 보스 보너스 신규 상수/메서드 추가 지점

### 테스트 환경
- `Assets/Scripts/World/DebugRoomTeleporter.cs` — `_meleePrefab`/`_rangedPrefab` Inspector 필드 + `TeleportToRoom()`에서 `EnemySpawner.Spawn()/Activate()` 호출 패턴 — D-11 보스 프리팹 필드 추가 지점
- `Assets/Prefabs/Rooms/Room_Debug/Room_Debug.prefab` — 격리 테스트용 룸 프리팹

### 프로젝트 컨벤션
- `.planning/phases/03-enemy-system/03-CONTEXT.md` — 원본 FSM 설계 근거(D-03 4상태 FSM, D-06 텔레그래프 회피 가능성 근거)
- `.planning/phases/999.4-enemy-ai-enhancement-pack/999.4-CONTEXT.md` D-04/D-05 — "이동하며 예고" 패턴이 확정된 배경(정적인 예고의 문제점) — 본 Phase D-04가 동일 논리를 보스에 적용
- `.planning/phases/14-enemy-spawn-vfx/14-CONTEXT.md` — 보스가 재사용할 스폰 VFX 파이프라인(`EnemySpawnEffect`, `ISpawnGatable`) 설계 배경

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `MeleeEnemy.TelegraphAndAttack()` 코루틴 구조 — 매 프레임 이동+`FlipSprite` 갱신 후 히트박스 활성화 패턴을 보스의 예고→빈틈 루프 뼈대로 재사용 가능(빈틈 상태 진입/색상 변화만 추가)
- `EnemyDeathEffect` — `AddComponent` 후 `StartCoroutine(PlayDeathSequence(...))` 컨벤션 그대로, `_maskRiseDuration`/`_particleColor` 필드 값만 보스용으로 확장(D-08)
- `EnemySpawnEffect`/`ISpawnGatable` — Phase 14에서 이미 "적 타입 비종속" 설계로 만들어짐 — 보스도 `ISpawnGatable` 구현만 추가하면 스폰 연출 그대로 적용됨(추가 구현 불필요, Phase 14 D-11 명시적 전제)
- `ScoreManager.AddKillScore(bool isRespawn=false)` 옆에 `AddBossKillScore()` 같은 신규 메서드 추가 패턴 — 기존 호출부 무변경 유지

### Established Patterns
- FSM: `enum EnemyState { ... }` + `switch` 기반 `Update()`, 상태 전환은 컴포넌트 내부 관리 — 보스도 동일 컨벤션(예: `Idle/Chase/Telegraph/Vulnerable/Hit/Dead` 등 세부 상태는 Claude's Discretion)
- 모든 타이머는 `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` — 빈틈 지속시간, 리셋 공백, 피격 반응 지속시간 전부 이 컨벤션 적용 필수
- 피격 판정: `IEnemy.OnDashHit()` → 즉시 로직 처리, `CombatController.ExecuteDash()`가 호출자
- 빈틈 게이팅: 신규 인터페이스 없이 `IsAlive` 프로퍼티를 "빈틈 상태 여부"로 오버로드 재사용(로드맵 Implementation Notes 명시) — 빈틈이 아닐 때 `IsAlive=false`로 두면 `FindNearestEnemyInRange()`가 자동으로 타겟에서 제외

### Integration Points
- `BossEnemy : MonoBehaviour, IEnemy, ISpawnGatable` 신규 컴포넌트 — `Assets/Scripts/Enemy/` 폴더에 추가
- `DebugRoomTeleporter.TeleportToRoom()` — 보스 프리팹 필드 추가 후 `EnemySpawner.Spawn(bossPrefab, ...)` 호출 지점 확장 필요(정확한 오버로드/필드 설계는 planning 단계에서 결정)
- `ScoreManager` — 보스 처치 시 `CombatController` 또는 `BossEnemy.OnDashHit()`에서 신규 메서드 호출

</code_context>

<specifics>
## Specific Ideas

- 사용자는 빈틈 상태를 "정지 + 색상 변화" 이중으로 표현하길 원함 — 하나만으로는 "지금 때릴 수 있다"는 신호가 약하다고 판단.
- 피격 반응은 "보스답게" 느껴지되 아트 리소스 없이 프로그래매틱하게(색상 플래시, 넉백/스태거, 기존 히트스파크 재사용 조합)로 명확히 범위를 좁힘 — 애니메이션 클립 제작은 이번 Phase에 없음.
- 사망 연출은 "기존 것 + 보스 전용 연장" — 완전히 새로운 연출 시스템이 아니라 기존 `EnemyDeathEffect`를 베이스로 강도/길이만 확장하는 방향.
- 보스 비주얼은 이번 Phase에서 전혀 새로 만들지 않는다 — 기존 적 스프라이트 재활용(크기/색상 변형)으로 FSM 검증에 집중, 진짜 "고유 스프라이트"(PROJECT.md가 명시한 v3.1 목표)는 보스 룸 완성 이후로 미뤄짐.
- 테스트는 `DebugRoomTeleporter`가 이미 가진 "임시 적 프리팹 스폰 후 즉시 Play 검증" 패턴을 그대로 확장 — Phase 16/17 완성을 기다리지 않고 이번 Phase에서 바로 FSM을 눈으로 확인/조정할 수 있음.

</specifics>

<deferred>
## Deferred Ideas

- **다양한 공격 패턴 순환(2~3종 이상)** — 이번 Phase는 단일 패턴 반복만 구현(D-05). 프레임워크 확장 검증 후 향후 Phase/보스 타입 추가 시 후보.
- **보스 전용 피격 애니메이션 클립** — 이번엔 프로그래매틱 연출로 대체(D-06). 실제 보스 아트가 준비되면 애니메이션 기반으로 교체 가능.
- **보스 전용 스프라이트/아트 제작** — 이번 Phase는 기존 적 스프라이트 변형으로 대체(D-10). REQUIREMENTS.md v2 Requirements의 BOSS-11/12(보스 콘텐츠 확장)와 함께 향후 검토.

</deferred>

---

*Phase: 15-fsm*
*Context gathered: 2026-07-15*
