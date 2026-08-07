# Phase 19: SAMURAI 보스 & 패링 모듈 & 모듈 선택 UI 확장 - Context

**Gathered:** 2026-07-28
**Updated:** 2026-08-07 — 기본전투모듈 신설(D-15~D-18) + Overclock 게이팅 확정, `/gsd:plan-phase 19` 진행 중 사용자 clarification 반영
**Status:** Ready for planning

<domain>
## Phase Boundary

튜토리얼 보스 SAMURAI를 격파하면 패링 모듈이 최초로 해금되고, 플레이어는 확장된 N-way 선택 화면에서 해금/잠금 상태를 확인하며 모듈을 골라 시작할 수 있다. (SAMURAI-01~05, UNLOCK-02, UNLOCK-03)

**이 Phase의 범위:** (1) 기본전투모듈(신규, player-side, 패링 없는 탭 스윙 — 튜토리얼 및 로비 상시 해금 슬롯), (2) 사무라이 전투형 모듈(=패링 모듈, player-side, 기본전투모듈과 동일한 탭 스윙 + 패링 판정 추가), (3) SAMURAI 보스 FSM(할로우나이트 스타일 평시 콤보 + 간헐적 패링 리듬 구간 + 그로기 게이지), (4) 모듈 선택 UI를 N-way(현재는 3-way: 기본전투모듈/Overclock/사무라이 전투형 모듈)로 확장.

**게임 플로우 배경(2026-08-07 재확인, 빌드 범위는 아래 D-18 참고):** 사무라이와의 튜토리얼 전투는 피오라 보스전보다 먼저 발생한다. 플레이어는 튜토리얼에서 기본전투모듈(패링 없음)로 사무라이와 싸우고, 격파 시 사무라이 전투형 모듈(패링 포함)이 해금되어 이를 기본 장비로 로비에 진입한다. 로비에서는 그때까지 격파한 보스에 따라 해금된 모듈(기본전투모듈은 상시 해금, 이후 사무라이/피오라/데드아이 등) 중 하나를 골라 게임을 시작한다.

**범위 밖:** 실제 `WorldGenerator` 스폰 풀 통합(v3.1 Phase 16/17, 여전히 파킹 상태 — Phase 18/18.1 선례대로 DebugScene에서만 검증), DeadEye/MAX/NOVA 구현(각각 Phase 20/22/23), 게임 모드/모드 선택 화면(Phase 24).

</domain>

<decisions>
## Implementation Decisions

### 패링 모듈 — 평시 공격 (탭 베기)
- **D-01:** 탭 시 나가는 공격은 Overclock의 자동타겟팅 돌진이 아니라 **제자리 방향성 스윙(자유 타겟)** — 그 순간 스윙 범위(부채꼴/라인) 안의 적을 즉시 벤다. 방향은 **마우스 방향**으로 결정(`CombatController`의 기존 `GetMouseWorldDirection()` 계열 로직 재사용).
- **D-02:** 스윙은 **원샷킬**이며 **모든 적(MeleeEnemy/RangedEnemy/보스 공통)에게 통용** — 코어 밸류(원샷원킬)를 그대로 유지. SAMURAI 전용 메커닉이 아니다.
- **D-03:** 탭 공격 사이 락아웃은 **짧은 고정값**(히트/헛치기 무관, 스팸 난사 방지 목적) — Overclock의 `whiffLockout`/`postKillLockout` 개념을 재사용하되 값은 훨씬 짧게. 정확한 수치는 Claude's Discretion(플레이테스트 튜닝).

### 패링 판정 & 반사
- **D-04:** 패링 발동 입력은 **일반 스윙과 같은 Attack 탭**이다(새 버튼 없음). 단 SAMURAI의 "패링 전용 타이밍" 구간에서는 **타이밍 + 방향(공격 출처를 향해 조준) 둘 다** 맞아야 패링이 성공한다.
- **D-05:** 패링 전용 타이밍 구간에서 **무입력** 또는 **잘못된 타이밍/방향으로 Attack 입력** 시 플레이어는 **즉사**한다. 단, **`RollController`의 기존 무적 굴리기로도 이 구간을 회피 가능** — 즉 "정확한 패링" 또는 "구르기 회피" 둘 중 하나가 생존 수단이다(패링만이 유일한 생존 수단이 아님).
- **D-06:** 패링 성공은 **순수 방어**다 — 보스에게 직접 데미지(처치 카운트)를 주지 않는다. 반사된 투사체의 방향은 조준 방향으로 결정된다. 대신 패링 성공은 그로기 게이지를 채우는 데 기여한다(D-09).

### SAMURAI 보스 패턴 구조 (할로우나이트 스타일)
- **D-07:** **평시 구간**은 MeleeEnemy/FioraBoss와 동일한 **예고(Telegraph)→공격 콤보** 구조 — 맞으면 **즉사**(프로젝트의 원샷원킬 코어 그대로 적용, HP/비치명타 개념 없음), 플레이어는 **구르기로 회피**해야 한다. "패링 전용" 타이밍은 이 평시 콤보와는 **별도의 간헐적 리듬 구간**으로 삽입된다.
- **D-08:** 패링 전용 타이밍 구간의 생존 수단은 D-05와 동일(패링 성공 또는 구르기 회피, 그 외엔 즉사).
- **D-09:** **그로기(Groggy) 게이지** 신설 — 평시 구간에서 보스에게 타격 성공 **및** 패링 성공 **둘 다** 이 게이지를 채운다. 게이지는 **여러 번 누적되어야 가득 참**(1회 성공 = 즉시 그로기가 아님). 정확한 누적 임계치/두 소스(평시 타격 vs 패링) 간 가중치 차등 여부는 Claude's Discretion(플레이테스트 튜닝).
- **D-10:** 게이지가 가득 차면 **그로기 상태**에 진입 — 이 상태에서 플레이어의 공격 1회가 **처치 진행 1회**로 카운트된다(FioraBoss의 `RequiredHits` 카운터와 유사한 역할, 단 트리거 메커니즘이 그로기 게이지를 경유한다는 점이 다름).
- **D-11:** 그로기→공격 사이클을 **총 7회** 반복해야 SAMURAI가 완전히 처치된다(FioraBoss의 `RequiredHits=7`과 동일한 최종 숫자를 채택하되, 도달 메커니즘은 그로기 게이지 경유로 다름).

### 기본전투모듈 (신규, 튜토리얼 & 로비 상시 해금)
- **D-15:** **기본전투모듈**은 사무라이 전투형 모듈과 **동일한 탭 스윙 로직(D-01~D-03: 제자리 방향성 스윙, 원샷킬, 짧은 락아웃)을 공유**하되 **패링 판정(D-04~D-06)이 없는 하위 버전**이다 — 사무라이의 "패링 전용 타이밍" 구간에서 이 모듈 장비 시 유일한 생존 수단은 `RollController` 회피뿐(D-05의 "구르기 회피" 경로만 유효, 패링 성공 경로 없음).
- **D-16:** 기본전투모듈은 **튜토리얼 진입 시점부터 상시 해금**(어떤 보스도 격파하기 전부터 사용 가능 — 사무라이 튜토리얼 전투 자체가 이 모듈로 진행됨)이며, 사무라이 격파로 사무라이 전투형 모듈이 해금된 **이후에도 로비 N-way 선택지에서 계속 선택 가능**하다 — 사무라이 모듈로 대체되어 사라지지 않는다.
- **D-17:** **Overclock(피오라) 해금 게이팅은 기존 STORY.md/Phase 18 설정을 그대로 유지**한다 — 기본전투모듈과 달리 Overclock은 상시 해금이 아니라 `BossUnlockManager.IsUnlocked("Fiora")`(기존 `FioraBoss.Die()`가 이미 호출 중인 로직)에 계속 종속된다. *(이 결정으로 19-RESEARCH.md Open Question #1 — "Overclock을 상시 해금할지"는 "아니오, 게이팅 유지"로 확정됨.)*

### 모듈 선택 UI 확장
- **D-12 (개정):** 이번 Phase에서는 **3개 모듈(기본전투모듈/Overclock/사무라이 전투형 모듈)**을 슬롯으로 노출한다(당초 2개 계획에서 기본전투모듈이 추가됨). 향후 DeadEye(Phase 20)/MAX(Phase 22)/NOVA(Phase 23) 슬롯이 추가되기 쉽도록 **목록/배열 기반의 확장 가능한 구조**로 설계할 것(하드코딩 나열 지양) — 정확한 구현 방식은 Claude's Discretion. 각 항목의 `requiredBossId`: 기본전투모듈=`null`(상시), Overclock=`"Fiora"`, 사무라이 전투형 모듈=`"Samurai"`.
- **D-13:** 잠금된 모듈은 **버튼 비활성화 + 자물쇠 아이콘**으로 표시한다(클릭 자체가 안 되도록).

### 검증 방식 & 플로우 구축 범위
- **D-14:** SAMURAI 보스 실전 검증은 **Phase 18/18.1 선례대로 DebugScene(`DebugRoomTeleporter`/`DebugSceneBuilder`) 확장**으로 진행한다. 실제 `WorldGenerator` 스폰 풀 통합은 이번 Phase 범위 밖 — v3.1 Phase 16/17 파킹 범위를 그대로 유지한다.
- **D-18 (신규, 2026-08-07):** **튜토리얼→로비 실제 게임 플로우(진입 시퀀스, 씬 전환, 강제 순서 배선)는 이번 Phase 빌드 범위 밖**이다 — D-14와 동일한 선례로, 이번 Phase는 기본전투모듈/사무라이 전투형 모듈/SamuraiBoss FSM/N-way UI(3-way)를 각각 만들고 DebugScene에서 개별 검증하는 것까지만 다룬다. 실제 "게임 시작 시 사무라이 튜토리얼 강제 진입 → 격파 후 로비 씬 전환" 배선은 이후 별도 Phase에서 다룬다.

### Claude's Discretion
- 그로기 게이지의 정확한 누적 임계치/비율(평시 타격과 패링 성공의 가중치가 같은지 다른지 포함)
- 패링 판정 타이밍 윈도우 폭(SAMURAI-05, 반드시 실측 튜닝)
- 패링 전용 타이밍 발생 빈도/평시 콤보와의 교차 주기
- 탭 공격 사이 짧은 고정 락아웃의 정확한 값
- SAMURAI 보스 시각적 정체성 — FioraBoss 선례(D-10, 기존 스프라이트 재활용+크기/색조 변형)를 기본 출발점으로 사용
- `SamuraiParryModule`(또는 `ParryController`)의 파일 배치, `IEnemy` 확장 없이 `TryParry()` 등 사이드채널 메서드로 구현(research 권장안 그대로 채택)
- 모듈 선택 UI의 정확한 레이아웃/버튼 배치

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 로드맵/요구사항
- `.planning/ROADMAP.md` §Phase 19 — Goal/Depends on/Requirements(SAMURAI-01~05, UNLOCK-02/03)/Success Criteria 6개
- `.planning/REQUIREMENTS.md` §SAMURAI 보스 + 패링 모듈, §보스 언락 진행(UNLOCK)
- `.planning/PROJECT.md` §Current Milestone v4.0, §Out of Scope — "보스 HP바/멀티페이즈 전투" 항목(HP 시스템은 이 게임 어디에도 없음 — 그로기 게이지는 HP가 아니라 "공격 가능 타이밍 게이팅" 장치로 이해할 것)

### v4.0 연구 문서
- `.planning/research/ARCHITECTURE.md` Question 1 — `IPlayerCombatModule` 설계 배경, CombatController가 host로 남는 이유(슬로우모션 lifecycle/게이지/`_isBusy`는 그대로 유지, 패링 모듈도 이 구조 위에 얹힘). **단, 패링 모듈은 슬로우모션 없이 실시간 동작하므로 hold-slowmo→release-resolve 형태를 따르지 않는다 — MAX와 마찬가지로 이 인터페이스에 억지로 맞추지 말고 별도 host-hook이 필요한지 planning 단계에서 확인할 것(Q1의 "Risk called out explicitly"와 동일한 종류의 우려)**
- `.planning/research/ARCHITECTURE.md` Question 2 — `SAMURAI's parry: do not extend the IEnemy contract` 섹션: 패링은 `IEnemy`의 4번째 멤버가 아니라 `SamuraiBoss`의 별도 public 메서드(예: `TryParry()`)로 처리
- `.planning/research/ARCHITECTURE.md` Question 4 — 모듈/모드 선택 UI 순서 논의: 이번 Phase는 모드 선택 없이 **모듈 선택만** 확장(모드 선택은 Phase 24)
- `.planning/research/PITFALLS.md` Pitfall 1 — 신규 보스/모듈 타이머는 반드시 `Time.unscaledDeltaTime`/`WaitForSecondsRealtime`
- `.planning/research/PITFALLS.md` Pitfall 2 — "defeated" 조건은 보스마다 다른 것을 전제로 `BossEnemyBase`가 설계됨 — SAMURAI의 그로기 게이지 기반 처치 조건이 바로 그 사례. `BossEnemyBase.Die()`는 그대로 재사용하되 "언제 Die()를 호출할지 결정하는 로직"(그로기 게이지)은 `SamuraiBoss` 고유로 구현
- `.planning/research/PITFALLS.md` Pitfall 3 — 모듈 스왑(보스 러시) 안전장치는 이번 v4.0 범위 밖(RUSH-01) — 패링 모듈도 Start 시점 고정 선택만 지원하면 충분, 스왑-세이프 enter/exit 훅 불필요

### 재사용 대상 기존 코드
- `Assets/Scripts/Player/Combat/IPlayerCombatModule.cs`, `CombatContext.cs` — 패링 모듈이 구현할 인터페이스(단, D-01 근본 불일치 여부는 planning에서 재확인)
- `Assets/Scripts/Player/CombatController.cs` — `_activeModule = new OverclockModule()` 하드코딩 상태, 모듈 선택 배선이 아직 전혀 없음(Phase 19에서 신설 필요)
- `Assets/Scripts/Enemy/Boss/BossEnemyBase.cs` — defeat-guard/`Die()`/스폰게이팅/하이라이트, `SamuraiBoss`가 그대로 상속
- `Assets/Scripts/Enemy/Boss/FioraBoss.cs` — 패턴 루프 작성 컨벤션, Vulnerable 상태/색상 틴트 처리 방식 참고
- `Assets/Scripts/Progression/BossUnlockManager.cs` — string bossId 기반, `IsUnlocked`/`Unlock` 그대로 재사용(`FioraBoss.Die()`가 이미 호출 중인 것과 동일 패턴)
- `Assets/Scripts/UI/AttackSelectController.cs` — 현재 `OnLinearClicked`/`OnFanClicked` 2-way 하드코딩, N-way로 확장 대상
- `Assets/Scripts/Player/InputManager.cs` — `AttackHeld`/`AttackReleased`/`IsAttackDown` 이미 존재, 패링용 별도 입력 액션 불필요(D-04)
- `Assets/Scripts/Player/RollController.cs`(또는 동등 컴포넌트) — 기존 무적 굴리기, 패링 구간 대체 회피 수단으로 그대로 재사용(D-05)
- `Assets/Scripts/UI/DebugSceneBuilder.cs`, `DebugRoomTeleporter` — Phase 18/18.1에서 검증에 사용된 디버그 씬 확장 대상(D-14)

### 프로젝트 컨벤션
- 모든 신규 타이머(패링 윈도우/그로기 게이지/보스 패턴)는 `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` 필수
- `IEnemy` 계약은 3-member로 닫혀있음 — 확장 금지, 패링은 사이드채널 메서드로

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `BossEnemyBase`(defeat-guard, `Die()` 시퀀스, 스폰게이팅, 하이라이트) — `SamuraiBoss`가 그대로 상속
- `BossUnlockManager`(string bossId, PlayerPrefs) — `FioraBoss.Die()`가 이미 호출하는 패턴 그대로 `SamuraiBoss.Die()`에도 적용
- `RollController`의 기존 무적 i-frame — 패링 전용 타이밍의 대체 회피 수단(D-05)으로 재사용, 신규 구현 불필요
- `InputManager.AttackHeld/AttackReleased/IsAttackDown` — 패링 입력도 이 3개로 충분(D-04), 신규 액션맵 불필요

### Established Patterns
- `AttackTypeSelector`(static class, `Selected` 프로퍼티) — 모듈 선택 상태를 저장할 신규 static selector(예: `CombatModuleSelector`)의 컨벤션 선례로 재사용 가능
- `CombatController._activeModule`이 현재 `Awake()`에서 하드코딩(`new OverclockModule()`) — 실제로 선택된 모듈을 읽어오는 배선 자체가 없음, 이번 Phase에서 신설
- `FioraBoss`의 Vulnerable 상태 색상 틴트(`vulnerableTintColor`) 방식 — 그로기 상태 시각 신호에도 동일 패턴 적용 가능

### Integration Points
- `Assets/Scripts/Player/Combat/` (신규) — `SamuraiParryModule.cs`(또는 `ParryController.cs`, 사무라이 전투형 모듈=패링 포함), 그리고 **기본전투모듈**(신규, D-15~D-16) — 동일 탭 스윙 로직을 공유하는 두 모듈 관계이므로 공통 베이스 클래스(예: `SamuraiBaseCombatModule`) 추출 + 패링 서브클래스가 `TryParry()` 오버라이드/추가하는 구조를 고려할 것(정확한 공유 방식은 Claude's Discretion)
- `Assets/Scripts/Enemy/Boss/` (신규) — `SamuraiBoss.cs` (BossEnemyBase 상속)
- `Assets/Scripts/UI/AttackSelectController.cs` (수정) — N-way(3-way) 확장, `requiredBossId: null`인 기본전투모듈 슬롯 포함
- `DebugSceneBuilder.cs`/`DebugRoomTeleporter` (수정) — SAMURAI 테스트 룸 배선

</code_context>

<specifics>
## Specific Ideas

- 사용자가 **할로우나이트**를 명시적 레퍼런스로 지목 — "평시엔 근접 공격 위주로 싸우다가 간헐적으로 패링 위주 구간을 사용" — 이것이 D-07/D-08(평시 콤보와 패링 리듬 구간의 분리)의 직접적 근거.
- **그로기 게이지**는 사전 연구 문서에는 없던, 사용자가 논의 중 직접 제안한 신규 메커니즘 — "평시에 때릴때와 패링을 통해서 그로기 게이지를 채우고, 그로기 시에 한번씩 공격. 그렇게 7번을 채우기"가 원문. FioraBoss의 단순 히트카운터(D-04, 15-02)보다 한 단계 더 복잡한 2단계 구조(누적→그로기→처치카운트)이므로, planning 시 이 차이를 명확히 인지할 것.
- STORY.md의 SAMURAI 설정("리듬게임 프로게이머", "적의 공격을 리듬게임의 '노트'처럼 인식해 쳐내는 방식")이 패링 판정(D-04/D-05)의 플레이버 근거.
- **게임 플로우 재확인(2026-08-07)** — 사무라이 튜토리얼이 피오라 보스전보다 먼저 발생하며, 기본전투모듈(패링 없음)로 진행된다. 격파 시 사무라이 전투형 모듈(패링 포함)이 해금되어 기본 장비로 로비 진입. 로비에서는 기본전투모듈(상시)+그때까지 해금된 보스 모듈 중 하나를 골라 시작. 원문: "일단 기본적으로 피오라 보스와 전투 이전에 튜토리얼에서 사무라이보스와 전투할거야... 사무라이 보스 격파 시에 사무라이 전투형 모듈(패링 + 근접공격)이 해금되고 해당 모듈을 기본적으로 갖고 로비로 이동할거야... 튜토리얼에서 사무라이 보스와 전투할 때는 기본전투모듈을 만들자 사무라이와 똑같은 모션과 공격을 사용하지만 패링이 없는 그런 모듈로 만들어줘." 이 정보는 19-RESEARCH.md Open Question #1을 해소했다(D-17 참고) — 실제 튜토리얼→로비 씬 전환 배선 자체는 D-18에 따라 이번 Phase 범위 밖.

</specifics>

<deferred>
## Deferred Ideas

- **실제 `WorldGenerator` 보스 스폰 풀 통합** — v3.1 Phase 16/17 파킹 범위 유지(D-14). v4.0 완료 후 재검토.
- **게임 모드/모드 선택 화면** — Phase 24 범위(이번 Phase는 모듈 선택 UI만 확장).
- **DeadEye/MAX/NOVA 모듈 UI 슬롯 실제 콘텐츠** — 각 보스가 구현되는 Phase 20/22/23에서 슬롯만 추가 연결(D-12로 구조는 미리 대비).

None — 논의가 Phase 범위 내에 머묾. 대기 중인 todo 없음(cross_reference_todos 단계에서 매칭된 항목 없음).

</deferred>

---

*Phase: 19-samurai-ui*
*Context gathered: 2026-07-28*
