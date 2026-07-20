# Phase 18: 공유 인프라 — 전투 모듈 추상화 & 보스 베이스 - Context

**Gathered:** 2026-07-20
**Status:** Ready for planning

<domain>
## Phase Boundary

신규 보스 4종(SAMURAI/DeadEye/MAX/NOVA)과 그 전투 모듈을 얹을 수 있는 기반이 갖춰진다 — 기존 Overclock(F.I.O.R.A) 전투가 `IPlayerCombatModule` 인터페이스로 회귀 없이 모듈화되고, `BossEnemyBase`가 추출되어 향후 보스들이 상속하며, 보스 격파 시 `PlayerPrefs` 기반 영구 해금이 기록된다. (INFRA-01, INFRA-03, UNLOCK-01)

**이 Phase는 순수 리팩토링/인프라 구축에 집중한다.** 신규 보스 4종의 실제 구현(SAMURAI/DeadEye/MAX/NOVA 각각의 패턴/모듈)은 Phase 19-23의 범위 — 이번 Phase는 그들이 올라탈 뼈대만 만든다.

**중요 — 이번 세션에서 플랫폼 타겟이 전환됨:** 이 Phase는 원래 INFRA-02(터치 조준 입력, Mouse.current → Pointer.current/EnhancedTouch)도 포함했으나, 논의 중 사용자가 프로젝트 전체의 타겟 플랫폼을 Android/모바일에서 **PC(Standalone) 우선**으로 영구 재설정하기로 결정 — INFRA-02는 REQUIREMENTS.md/ROADMAP.md에서 완전히 제거(descoped)되었다. `Mouse.current` 기반 조준은 그대로 유지되며 변경 대상이 아니다. (자세한 배경은 아래 `<specifics>` 참고, 전체 프로젝트 영향 범위는 CLAUDE.md/PROJECT.md/REQUIREMENTS.md/ROADMAP.md에 이미 반영됨.)

</domain>

<decisions>
## Implementation Decisions

### 플랫폼 범위 재확정
- **D-01:** 프로젝트 타겟 플랫폼을 Android/모바일에서 PC(Standalone, 마우스+키보드)로 영구 재설정 — 기획/로드맵 문서 전체(CLAUDE.md, PROJECT.md, ROADMAP.md, REQUIREMENTS.md, 기획서.md, research/*.md)에 반영 완료. Unity 엔진 Player Settings(Android AndroidMinSdkVersion 25/ARM64, iOS 15.0 등)는 의도적으로 변경하지 않음 — 문서 우선순위만 재조정.
- **D-02:** INFRA-02(터치 조준 입력)는 이번 Phase 범위에서 완전히 제거된다. `CombatController.GetMouseWorldDirection()`의 기존 `Mouse.current` 기반 방식은 그대로 유지 — 마이그레이션 대상 아님.

### F.I.O.R.A 보스 정체성
- **D-03:** 기존 `BossEnemy.cs`(Phase 15에서 만든 유일한 보스 구현체)는 `BossEnemyBase` 추출과 동시에 `FioraBoss : BossEnemyBase`로 이름 붙여 명시적 정체성을 부여한다 — STORY.md/PROJECT.md가 F.I.O.R.A를 "이미 구현된 Overclock Mode의 원본"으로 이미 명시하고 있으므로, `PlayerPrefs` 언락키/향후 UI 표시 이름을 처음부터 일관되게 가져간다(예: boss id `"Fiora"`).

### INFRA-01 회귀 검증
- **D-04:** `IPlayerCombatModule` 마이그레이션의 "회귀 없음" 검증은 **수동 플레이테스트만**으로 진행한다 — 자동화 PlayMode 테스트(오래 미완료 상태인 02-04-PLAN의 CombatTests/RollTests)는 이번 Phase 범위에 포함하지 않는다. 마이그레이션이 기존 로직의 verbatim move(로직 변경 없는 단순 이동)이므로 리스크가 낮다고 판단.

### Claude's Discretion
- `BossEnemyBase`/`FioraBoss` 파일 배치(기존 `Assets/Scripts/Enemy/` 플랫 폴더 유지 vs `Assets/Scripts/Enemy/Boss/` 하위 폴더 신설)
- `IPlayerCombatModule`/`OverclockModule`/`CombatContext`의 정확한 메서드 시그니처 — ARCHITECTURE.md Question 1의 제안을 기본 출발점으로 사용
- `BossUnlockManager`의 정확한 API 형태(딕셔너리 캐시, enum vs string id 등) — ARCHITECTURE.md Question 3의 제안을 기본 출발점으로 사용
- 수동 플레이테스트 체크리스트의 구체적 항목 구성

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 로드맵/요구사항
- `.planning/ROADMAP.md` §Phase 18 — Goal/Depends on/Requirements(INFRA-01, INFRA-03, UNLOCK-01 — INFRA-02 제거됨)/Success Criteria 3개/Implementation Notes
- `.planning/REQUIREMENTS.md` §공유 인프라(INFRA), §보스 언락 진행(UNLOCK) — INFRA-02는 DESCOPED로 취소선 처리됨, 근거 명시
- `.planning/PROJECT.md` §Constraints, §Context — 플랫폼이 PC 우선으로 재설정된 근거와 날짜

### v4.0 연구 문서 (필수, 이 Phase의 아키텍처 결정 대부분이 이미 여기 담김)
- `.planning/research/ARCHITECTURE.md` Question 1 — `IPlayerCombatModule` 인터페이스 설계, `CombatController`를 host로 유지하는 근거, `OverclockModule` verbatim-move 전략 (단, 최상단 2026-07-20 AMENDED 노트 이후의 터치/Pointer 관련 서술은 이 Phase에 적용하지 않음)
- `.planning/research/ARCHITECTURE.md` Question 2 — `BossEnemyBase` 추출 범위(defeat-guard/사망 시퀀스/스폰 게이팅/하이라이트만, 패턴 루프는 추상으로 남김), SAMURAI 패링을 `IEnemy` 확장 없이 처리하는 방법, NOVA 이원화 설계는 이 Phase 범위 밖(Phase 23에서 결정)
- `.planning/research/ARCHITECTURE.md` Question 3 — `BossUnlockManager` PlayerPrefs 설계, `DeathScreenController.RestartGame()`과의 분리 원칙
- `.planning/research/PITFALLS.md` Pitfall 1 — 신규 보스/모듈 타이머는 반드시 `Time.unscaledDeltaTime`/`WaitForSecondsRealtime`
- `.planning/research/PITFALLS.md` Pitfall 2 — `BossEnemyBase` 추출은 2번째 보스가 생기기 *전에* 끝내야 함(지금이 정확히 그 시점)
- `.planning/research/PITFALLS.md` Pitfall 3 — 모듈 스왑(Boss Rush) 상태 누수는 **이번 v4.0 범위 밖**(RUSH-01로 이연)이므로 Phase 18의 모듈 추상화는 스왑-안전 enter/exit 훅을 만들 필요 없음, 단일 선택(Start 시점 고정)만 지원하면 충분
- `.planning/research/PITFALLS.md` Pitfall 6 — Unlock 영속성 스코프(세션 vs 앱 재시작)는 이미 UNLOCK-01에서 "앱 재시작 후에도 유지"로 명확히 확정됨(PlayerPrefs)
- `.planning/research/PITFALLS.md` Pitfall 5 — 2026-07-20 AMENDED 노트로 무효화됨(플랫폼이 PC로 재설정되어 터치 입력 문제 자체가 해당 없음)

### 재사용 대상 기존 코드
- `Assets/Scripts/Player/CombatController.cs` — `IPlayerCombatModule` 추출 대상 전체(슬로우모션 lifecycle/게이지/`_isBusy`/hit-freeze는 host에 유지, targeting+resolution만 이동)
- `Assets/Scripts/Enemy/BossEnemy.cs` — `BossEnemyBase`/`FioraBoss` 추출 원본, `_isDefeated` 가드 패턴이 핵심 보존 대상
- `Assets/Scripts/Enemy/EnemyBase.cs` — "최소 공통분모만 추출" 컨벤션의 선례(16-CONTEXT.md D-05) — `BossEnemyBase`도 동일 철학 적용
- `Assets/Scripts/Enemy/IEnemy.cs`, `ISpawnGatable.cs` — 계약 변경 금지, `FioraBoss`가 계속 구현
- `.planning/phases/16-boss-room-lifecycle/16-CONTEXT.md` D-05 — `EnemyBase` 추출 시 사용한 "최소 범위만" 원칙 — `BossEnemyBase`도 동일하게 적용

### 프로젝트 컨벤션
- 모든 신규 타이머(모듈/보스)는 `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` 필수 (전 마일스톤 공통 제약)
- `PlayerPrefs`는 이 프로젝트 최초 사용 — `DeathScreenController.RestartGame()`의 기존 리셋 스윕(`FloorManager`/`ScoreManager`)에 절대 포함시키지 않을 것

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `EnemyBase`의 "최소 공통분모만 추출" 패턴 — `BossEnemyBase` 추출 시 그대로 재적용 가능한 선례
- `ScoreManager.AddBossKillScore()`, `EnemyDeathEffect` — 이미 보스 사망 시퀀스에 연동되어 있음, `BossEnemyBase.Die()`가 그대로 재사용

### Established Patterns
- 데이터 전용 static 클래스(no MonoBehaviour lifecycle) — `FloorManager`/`ScoreManager` 컨벤션, `BossUnlockManager`가 따라야 할 패턴(`AudioManager`의 MonoBehaviour 싱글턴 패턴이 아님)
- `DeathScreenController.RestartGame()`은 "무조건 리셋" 지점 — 새 영속 상태(unlock)는 구조적으로 분리된 파일에 둬서 실수로 리셋되지 않게 할 것

### Integration Points
- `Assets/Scripts/Player/Combat/` (신규 폴더) — `IPlayerCombatModule.cs`, `OverclockModule.cs`
- `Assets/Scripts/Progression/` (신규 폴더) — `BossUnlockManager.cs`
- `Assets/Scripts/Enemy/BossEnemy.cs` → `BossEnemyBase.cs` + `FioraBoss.cs`로 분리(정확한 폴더 위치는 Claude's Discretion)

</code_context>

<specifics>
## Specific Ideas

- 사용자가 세션 중간에 "왜 갑자기 터치가 나오는거야? 지금 모바일 버전도 논의되고 있어?"라고 질문 — INFRA-02가 이전 `/gsd:new-milestone` 세션(2026-07-20, 이번과 같은 날 더 이른 시각)에서 이미 요구사항으로 확정되어 있었다는 배경을 설명한 뒤, 사용자가 명시적으로 "프로젝트 자체를 PC로 영구 재설정"을 선택함 — 이는 단순 Phase 18 스코프 조정이 아니라 프로젝트 전체 방향 전환.
- F.I.O.R.A에 정체성을 부여하기로 한 이유: STORY.md/PROJECT.md가 이미 F.I.O.R.A를 5개 보스 중 하나(이미 구현된 것)로 서술하고 있어서, 지금 이름을 붙여두지 않으면 나중에(Phase 19+에서 언락 UI에 5개 보스 이름을 나열할 때) 어색한 리네이밍 작업이 필요해짐.
- 회귀 검증을 수동 플레이테스트로 한정한 이유: verbatim move 리팩토링이라 리스크가 낮다고 판단, 자동 테스트 인프라 구축은 이번 Phase 스코프를 불필요하게 늘림.

</specifics>

<deferred>
## Deferred Ideas

- **자동화 PlayMode 회귀 테스트(02-04-PLAN 완성)** — 이번 Phase는 수동 플레이테스트만 채택(D-04). 향후 필요성이 커지면 별도 Phase/quick task로 재검토.
- **Android/모바일 재지원** — D-01로 보류. 재검토 시점/조건은 아직 정해지지 않음, 필요 시 사용자가 명시적으로 재논의.
- **보스 러시 모드의 모듈 스왑 안전장치** — v4.0 전체 범위 밖(RUSH-01), Phase 18의 모듈 추상화는 스왑을 고려할 필요 없음(Pitfall 3 참고).

</deferred>

---

*Phase: 18-shared-infra*
*Context gathered: 2026-07-20*
