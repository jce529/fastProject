# Phase 2: Combat Core - Context

**Gathered:** 2026-06-02
**Status:** Ready for planning

<domain>
## Phase Boundary

슬로우모션 조준(hold) → 돌진 킬(release) 루프와 게이지, 구르기, 히트프리즈를 스태셔너리 더미 상대로 완전히 플레이어블하게 만든다.

**Requirements in scope:** MOVE-03, ATCK-01, ATCK-02, ATCK-03, ATCK-04, ATCK-05, FEEL-01
**Not in scope:** 실제 적 AI (Phase 3), HUD/UI (Phase 4), 층 시스템 (v2)

</domain>

<decisions>
## Implementation Decisions

### 공격 범위 시각화
- **D-01:** 직선형 범위 — LineRenderer로 플레이어 양쪽 방향으로 레이저 빔 2줄 표시
- **D-02:** 부채꼴형 범위 — LineRenderer 와이어프레임으로 부채꼴 윤곽선만 표시 (성능 우선)
- **D-03:** 기본 범위 색상: 노란색 (Yellow)
- **D-04:** 범위 내 적 감지 시: 가장 가까운 적의 아웃라인/스프라이트를 빨간색으로 강조
- **D-05:** 범위 수치 (직선 길이, 부채꼴 각도/반지름) — Claude 초기값 결정 후 플레이테스트 조정

### 돌진 연출
- **D-06:** 돌진 중 Trail Renderer로 잔상 표시 — 속도감 시각화
- **D-07:** 카메라는 LateUpdate 추적 유지 — 돌진 시 별도 카메라 반응 없음 (Phase 1 D-11 일관성)

### 더미 적 구성
- **D-08:** 시각: 회색 실루엣 캡슐 또는 사각형 placeholder 스프라이트 — Phase 1 플랫폼과 동일한 스타일
- **D-09:** 수량: 씬에 3~5개 고정 배치 — 직선형/부채꼴형 범위 패턴 테스트에 충분한 수
- **D-10:** 처치 후 ~2초 뒤 제자리 부활 — 씬 재시작 없이 연속 테스트 가능

### Phase 1에서 이어받은 결정 (재확인)
- **D-11:** 구르기 무적: InvincibilityHandler 레이어 스왑 패턴 재사용 (PlayerHurtbox ↔ PlayerInvincible)
- **D-12:** 구르기 쿨타임 타이머: `Time.unscaledDeltaTime` 필수 (슬로우모션 timeScale 영향 없어야 함)
- **D-13:** 돌진 구현: `Rigidbody2D.MovePosition()` 2-3프레임, 속도 스파이크 금지

### Claude's Discretion
- 직선형 레이저 빔 길이 초기값 (권장: 8~12 units)
- 부채꼴형 각도/반지름 초기값 (권장: 90~120도, 반지름 6~8 units)
- Trail Renderer 길이 및 페이드아웃 시간
- 더미 부활 이펙트 유무
- 슬로우모션 timeScale 값 (ROADMAP 권장: 0.15~0.25x, STATE.md 참조)
- 게이지 드레인/회복 속도 초기값

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` §전투(Combat) — ATCK-01~05 상세 정의
- `.planning/REQUIREMENTS.md` §이동(Movement) — MOVE-03 (구르기) 상세 정의
- `.planning/REQUIREMENTS.md` §타격감(Game Feel) — FEEL-01 (히트프리즈 50-100ms) 정의

### Roadmap & State
- `.planning/ROADMAP.md` §Phase 2 — 성공 기준(SC 1~6) 및 Stack Constraints 전체 섹션
- `.planning/STATE.md` — 기술 제약사항 및 Accumulated Context (Key Decisions Locked 포함)

### Prior Phase Context
- `.planning/phases/01-foundation-movement/01-CONTEXT.md` — Phase 1 결정 (카메라, 무적 패턴, D-11~D-14)

### Technical Constraints (from ROADMAP.md Stack Constraints)
- `Time.timeScale` 변경 시 반드시 `Time.fixedDeltaTime = 0.02f * Time.timeScale` 함께 설정
- 슬로우 중 플레이어 속도 보상: `rb.linearVelocity *= (1f / Time.timeScale)` — PlayerController에 이미 구현됨
- 구르기/히트프리즈 타이머: `Time.unscaledDeltaTime` 필수
- 돌진: `Rigidbody2D.MovePosition()` 2-3프레임
- 히트프리즈: `Time.timeScale = 0`, 50-100ms, `WaitForSecondsRealtime` 사용
- 무적: 레이어 스왑 (PlayerHurtbox / PlayerInvincible)

### Existing Code (Integration Points)
- `Assets/Scripts/Player/PlayerController.cs` — `Time.timeScale` 보상 이미 구현, Phase 2 슬로우모션 추가 시 재작성 불필요
- `Assets/Scripts/Player/InputManager.cs` — `IsAttackDown`, `AttackReleased`, `RollPressed` 이미 정의
- `Assets/Scripts/Player/InvincibilityHandler.cs` — 무적 레이어 스왑 로직, 구르기 무적에 재사용
- `Assets/InputSystem_Actions.inputactions` — Attack(Button), Sprint(→Roll) 액션 이미 정의

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `InputManager.cs` — `IsAttackDown` (슬로우모션 유지 여부), `AttackReleased` (돌진 트리거), `RollPressed` (구르기 트리거) 모두 정의됨. 전투 시스템 연결 즉시 가능.
- `InvincibilityHandler.cs` — 레이어 스왑 기반 무적 처리. 구르기 무적에 동일 패턴 재사용.
- `PlayerController.cs` — FixedUpdate에 `1f / Time.timeScale` 보상 이미 구현. Phase 2 슬로우모션 추가 시 PlayerController 수정 불필요.

### Established Patterns
- 무적 처리: 레이어 스왑 (IgnoreLayerCollision 금지) — Phase 1에서 확립
- 타이머: `WaitForSecondsRealtime` for timeScale 영향받지 않는 딜레이
- 입력 처리: InputManager 싱글톤에서 one-frame flag LateUpdate 클리어

### Integration Points
- `PlayerController`에 `CombatController` 또는 `AttackSystem` 컴포넌트 추가 방식 (기존 파일 수정 최소화)
- 더미 적은 `Assets/Scripts/Enemy/` 폴더 신규 생성
- 공격 타입 선택 씬: `Assets/Scenes/` 에 별도 씬 또는 SampleScene 오버레이 UI

</code_context>

<specifics>
## Specific Ideas

- 범위 시각화 레퍼런스: 노란색 → 빨간색 상태 전환 (감지 여부 직관적 표시)
- 더미: 처치 후 ~2초 부활 — 씬 재설정 없이 반복 테스트 루프 완성
- 트레일 잔상은 돌진 시작/종료에만 활성화 (평상시 OFF)

</specifics>

<deferred>
## Deferred Ideas

None — discussion stayed within phase scope.

</deferred>

---

*Phase: 02-combat-core*
*Context gathered: 2026-06-02*
