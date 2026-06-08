# Phase 3: Enemy System - Context

**Gathered:** 2026-06-08
**Status:** Ready for planning

<domain>
## Phase Boundary

두 종류의 적(근접/원거리)이 FSM으로 순찰·탐지·예고·공격하며, 플레이어 대시 공격에 즉사한다. 플레이어도 적 공격 한 방에 즉사. FEEL-01 히트프리즈는 모든 킬에 동일하게 발동.

**Requirements in scope:** ENMY-01, ENMY-02
**Not in scope:** HUD/사망화면(Phase 4), 층 시스템(v2), HP 시스템(Out of Scope), Patrol 복잡도 향상(v2)

</domain>

<decisions>
## Implementation Decisions

### 적 구조 & CombatController 연결
- **D-01:** `IEnemy` 인터페이스 도입 — `IsAlive`, `OnDashHit()`, `ClearHighlight()` 세 멤버로 구성. `CombatController`는 `DummyEnemy` 직접 참조 → `IEnemy` 참조로 교체 필요.
- **D-02:** `DummyEnemy`도 `IEnemy` 구현 (하위 호환 유지 — 씬에 존재하는 더미 유지 가능).
- **D-03:** FSM 4상태: `Idle(Patrol)` → `Chase` → `Telegraph` → `Attack`. 상태 전환은 각 Enemy 컴포넌트 내부에서 관리.
- **D-04:** Idle 상태: 좌우 순찰 왕복. 플레이어가 탐지 반경 내 진입 시 Chase 전환.

### 근접 적 (MeleeEnemy) — ENMY-01
- **D-05:** Telegraph 시각 표현: 적 머리 위 `!` 아이콘(UI WorldSpace 또는 SpriteRenderer). 공격 직전 0.8초 실시간(unscaledDeltaTime) 동안 표시.
- **D-06:** Telegraph 지속 시간: **0.8초 실시간** — 플레이어가 구르기로 회피 가능한 충분한 여유.
- **D-07:** 공격은 플레이어 위치로 짧은 돌진 또는 히트박스 활성화로 구현 — Claude 재량 (애니메이션 없는 placeholder 환경이므로 단순하게).
- **D-08:** 근접 공격 히트박스: Trigger Collider2D, 공격 시작 프레임에 잠깐 활성화.

### 원거리 적 (RangedEnemy) — ENMY-02
- **D-09:** 조준선: `LineRenderer` 빨간 실선. Telegraph 시작 시 알파 0 → 1로 점진(0.8초 동안), 발사 직전 알파 최대. Phase 2 `RangeDisplay`와 동일한 LineRenderer 패턴 재사용.
- **D-10:** 이동 패턴: 이동 코드 구현하되 초기 `moveSpeed = 0f`. 제자리 고정 상태로 조준 → 발사. 플레이테스트 후 속도 조정.
- **D-11:** 투사체(Projectile): Rigidbody2D 직선 등속. Trigger Collider2D로 플레이어 충돌 감지. 별도 `ProjectileController` 스크립트.
- **D-12:** 투사체는 일정 거리 이상 이동 시 또는 Platform 충돌 시 Destroy.

### 플레이어 즉사 처리
- **D-13:** `PlayerController` (또는 별도 `PlayerHealth` 컴포넌트)에 `static event Action OnPlayerDeath` 선언.
- **D-14:** Phase 3 임시 동작: 플레이어 피격/낙사 시 `OnPlayerDeath` 발동 → GameObject.SetActive(false) + `Debug.Log("Player died")`. 에디터에서 Play Mode 종료로 재시작.
- **D-15:** Phase 4에서 UIManager/DeathScreen이 `OnPlayerDeath`를 구독 — Phase 3 코드 수정 없음.
- **D-16:** 피격 조건: 플레이어가 `PlayerInvincible` 레이어일 때 적 Trigger 무시 (기존 레이어 스왑 패턴 활용).
- **D-17:** 낙사 = 즉사. `FallDetector.cs` 직접 수정 — 순간이동(회복) 로직 제거, `OnPlayerDeath` 발동으로 교체. Phase 1의 마지막 발판 저장 로직도 제거.

### Claude's Discretion
- 탐지 반경 수치 (권장: 8~12 units) — 플레이테스트 후 조정
- 근접 적의 Chase 이동 속도 (권장: 3~5 units/s)
- 원거리 적의 탐지/공격 사거리 (권장: 10~15 units)
- 투사체 속도 (권장: 8~12 units/s)
- `!` 아이콘 구현 방법 (WorldSpace Canvas 또는 자식 SpriteRenderer — 더 간단한 것 선택)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Requirements
- `.planning/REQUIREMENTS.md` §적(Enemies) — ENMY-01, ENMY-02 상세 정의
- `.planning/REQUIREMENTS.md` §Out of Scope — HP 시스템 없음 확인 필수

### Roadmap & State
- `.planning/ROADMAP.md` §Phase 3 — 성공 기준(SC 1~4) 전체 (특히 SC-1: "설명 없이 구르기로 피할 수 있을 만큼" 예고 길이)
- `.planning/STATE.md` — 기술 제약사항 및 Key Decisions Locked

### Prior Phase Context
- `.planning/phases/01-foundation-movement/01-CONTEXT.md` — 무적 레이어 스왑 패턴 (D-11~D-13)
- `.planning/phases/02-combat-core/02-CONTEXT.md` — IEnemy 연결 대상인 CombatController 설계 (D-01~D-10), RangeDisplay LineRenderer 패턴 (D-09 재사용)

### Existing Code (Integration Points)
- `Assets/Scripts/Enemy/DummyEnemy.cs` — IEnemy 구현 참고 패턴 + 씬 내 더미 유지
- `Assets/Scripts/Player/CombatController.cs` — `FindNearestEnemyInRange()` DummyEnemy→IEnemy 교체 대상, `UpdateHighlight()` IEnemy 참조 교체 대상
- `Assets/Scripts/Player/InvincibilityHandler.cs` — PlayerHurtbox/PlayerInvincible 레이어 스왑, D-16 피격 조건에 활용
- `Assets/Scripts/Player/PlayerController.cs` — `OnPlayerDeath` 이벤트 추가 위치 (또는 별도 PlayerHealth)
- `Assets/Scripts/Player/FallDetector.cs` — 순간이동 로직 제거 후 `OnPlayerDeath` 발동으로 교체 (D-17)

### Technical Constraints (from ROADMAP.md Stack Constraints)
- `Time.unscaledDeltaTime` — 모든 쿨다운/예고 타이머 (슬로우모션 중에도 실시간 유지)
- `Physics2D.OverlapCircleNonAlloc()` — 탐지 쿼리 (Update 내 LINQ/FindObjectsOfType 금지)
- `Rigidbody2D`: Continuous collision detection + Interpolate mode
- 무적: 레이어 스왑 (PlayerHurtbox / PlayerInvincible)
- FEEL-01 히트프리즈: `Time.timeScale = 0`, 50-100ms, `WaitForSecondsRealtime` — 적 처치 시 동일하게 발동

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `DummyEnemy.cs` — `IsAlive`, `OnDashHit()`, `ClearHighlight()` 패턴을 IEnemy 인터페이스 기준으로 정의 가능. DummyEnemy가 IEnemy를 구현하도록 수정하면 씬 내 더미 제거 없이 유지.
- `InvincibilityHandler.cs` — 피격 무효화에 레이어 스왑 패턴 그대로 재사용. 구르기/대시 무적과 동일 방식.
- `RangeDisplay.cs` + Phase 2 LineRenderer 패턴 — 원거리 조준선(D-09) 구현 시 참고.
- `CombatController._hitBuffer` (pre-allocated Collider2D[16]) — 적 탐지 버퍼 패턴 동일하게 Enemy 탐지에 사용.

### Established Patterns
- FSM: `enum EnemyState` + `switch` 기반 Update() 처리 — 가장 단순한 Unity 적합 패턴
- 타이머: `WaitForSecondsRealtime` — Telegraph/Attack 딜레이 Coroutine에 필수 (timeScale 영향 없음)
- 적 처치 시 호출 체인: `target.OnDashHit()` → `HitFreeze()` — CombatController 기존 흐름 유지
- 물리: Trigger Collider2D로 피격 감지 (OnTriggerEnter2D), Rigidbody2D 투사체

### Integration Points
- `CombatController.FindNearestEnemyInRange()` — `GetComponent<DummyEnemy>()` → `GetComponent<IEnemy>()` 교체 (한 곳)
- `CombatController.UpdateHighlight()` — `DummyEnemy` 타입 → `IEnemy` 타입으로 교체
- `CombatController._lastHighlighted` 필드 타입 변경 필요
- `Assets/Scripts/Enemy/` 폴더에 `IEnemy.cs`, `MeleeEnemy.cs`, `RangedEnemy.cs`, `ProjectileController.cs` 신규 추가

</code_context>

<specifics>
## Specific Ideas

- 원거리 적 이동코드: `moveSpeed = 0f` 직렬화 필드로 시작. Inspector에서 0 → 원하는 값으로 즉시 테스트 가능.
- `!` 아이콘: WorldSpace Canvas보다 자식 SpriteRenderer에 `!` 텍스처가 더 단순함 — placeholder 환경에서 Canvas 오버헤드 불필요.
- Phase 4와의 경계: `OnPlayerDeath` static event는 `PlayerController` 또는 `PlayerHealth`에 선언. Phase 4 UIManager가 구독만 추가하면 됨 — Phase 3 코드 무수정.

</specifics>

<deferred>
## Deferred Ideas

- 원거리 적 이동(Chase 속도 > 0) — v2 또는 플레이테스트 후 조정
- 복잡한 순찰 경로 (웨이포인트 기반) — v2 범위
- 적 사망 이펙트 (파티클) — Phase 4 polish 또는 v2

</deferred>

---

*Phase: 03-enemy-system*
*Context gathered: 2026-06-08*
