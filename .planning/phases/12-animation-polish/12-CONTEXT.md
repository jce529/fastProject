# Phase 12: 포탈 진입/퇴장 애니메이션 구현 및 공격 애니메이션 개선 - Context

**Gathered:** 2026-07-07
**Status:** Ready for planning

<domain>
## Phase Boundary

`10-TRANSITION-DESIGN.md`에 문서화됐지만 실제로는 구현되지 않은 포탈 진입/퇴장 연출(SpriteMask 기반)을 코드로 구현하고, 발견된 애니메이터 트리거 버그(Whiff/Roll)를 수정하며, 플레이어 공격 히트 순간의 임팩트감(히트 스파크, 카메라 쉐이크, 적 사망 연출, 돌진 잔상)을 개선한다.

**Requirements in scope:** REQUIREMENTS.md에 매핑된 공식 요구사항 없음 — ROADMAP.md Phase 12는 "Requirements: TBD" 상태. 이번 Phase는 v3.0 핵심 검증 요구사항 외의 비주얼 폴리싱 작업이며, discuss-phase 논의로 범위가 확정됨.
**Not in scope:** 적(MeleeEnemy/RangedEnemy)의 공격 모션/이펙트 자체 수정(이미 정상 작동 중 — 필요시 별도 phase), ExitPortal.prefab에 이미 연결된 미사용 Animator("Portal" 단일 상태)의 활용, 신규 스프라이트/파티클 아트 제작(모두 기존 에셋 재활용).

</domain>

<decisions>
## Implementation Decisions

### 포탈 전환 연출 (10-TRANSITION-DESIGN.md 구현)
- **D-01:** `10-TRANSITION-DESIGN.md` 원안의 SpriteMask 런타임 스케일 애니메이션 방식을 코드로 구현한다. 단, `WorldGenerator.cs`에 직접 넣지 않고 **신규 컴포넌트로 분리**한다(예: `FloorTransitionEffect.cs`) — `WorldGenerator.cs`는 이미 492줄이며 더 커지지 않도록, `WorldGenerator`는 이 컴포넌트의 진입/퇴장 재생 메서드(예: `PlayEntry()`/`PlayExit()`)만 호출한다.
- **D-02:** 퇴장(EXIT) 이펙트용 `PortalEffect` 프리팹은 새 아트 없이 **기존 ExitPortal 스프라이트를 재활용**해 `localScale (0,0,0) → (1,1,1)` 애니메이션만 적용한다.
- **D-03:** 입장/퇴장 연출 지속시간은 `10-TRANSITION-DESIGN.md` 원안 수치를 그대로 사용한다: 입장 마스크 0.4s, 포탈 수축 0.3s, 퇴장 포탈 성장 0.4s, 퇴장 마스크 0.5s, 포탈 페이드 0.3s.
- **D-04:** `ExitPortal.prefab`에 이미 연결된 Animator(`ExitPortal.controller`, 파라미터 없는 "Portal" 단일 상태)는 이번 phase에서 사용하지 않고 그대로 방치한다 — 코드 기반 스케일/마스크 애니메이션이 우선.

### 애니메이터 트리거 버그 수정
- **D-05:** `CombatController.ExecuteWhiff()`의 `_animator?.SetTrigger("Whiff")`가 `FastPlayerAnimator.controller`에 정의되지 않은 파라미터라 무효 상태 — Whiff 트리거+상태를 컨트롤러에 추가해 실제로 재생되도록 수정한다. 헛베기와 처치가 애니메이션상 구분되어야 한다.
- **D-06:** `RollController`의 `SetTrigger("Roll")`도 동일 문제(컨트롤러엔 `IsRolling` bool만 존재, `Roll` 트리거 없음) — 실제로 동작하도록 수정한다.

### 히트 임팩트 개선
- **D-07:** 처치 순간 히트 스파크는 기존 DeadRevolver 에셋의 `GuardImpact01~03.png` + `SwordGuardImpact.anim`(검 맞부딪침/재링 스파크, 3프레임)을 재활용한다 — 신규 아트 불필요. (Hit01~03/HitDamage.anim은 원래 피격용이라 의미가 어긋나 배제, GunBulletImpactFX는 총알 피탄 이펙트라 검 공격과 스타일 불일치로 배제)
- **D-08:** 처치 순간 카메라 쉐이크를 추가한다.
- **D-09:** 적 처치 연출을 다음 순서로 구현한다: 기존 Die 애니메이션 재생 → 파티클 이펙트 재생 + **SpriteMask 방식으로 스프라이트를 아래에서 위로 가리며 사라짐** → GameObject Destroy. SpriteMask 기법은 D-01(포탈 전환)과 동일한 코드 패턴을 재사용한다.
- **D-10:** 공격(돌진) 모션의 잔상은 **기존 `CombatController`의 `TrailRenderer`를 강화**하는 방향으로 구현한다(두께/길이/색상 조정) — 스프라이트 고스트 잔상 등 신규 기법은 도입하지 않는다.

### 적용 범위
- **D-11:** 이번 phase는 플레이어 애니메이션/이펙트만 다룬다 — `MeleeEnemy`/`RangedEnemy`의 공격 모션 자체는 수정하지 않는다(이미 정상 작동 중).

### Claude's Discretion
- `FloorTransitionEffect`(신규 컴포넌트, D-01)의 정확한 파일명/클래스명, `WorldGenerator`와의 인터페이스 시그니처
- Whiff/Roll 애니메이터 상태 전환 세부(Transition Duration = 0 유지 등 STATE.md 기술 제약 준수 하에)
- GuardImpact 스파크(D-07)의 정확한 스폰 위치/크기/지속시간
- 카메라 쉐이크(D-08)의 정확한 진폭/지속시간/감쇠 곡선
- 적 사망 SpriteMask 기법(D-09)의 파티클 시스템 세부(색상/개수/수명), 마스크 이동 속도
- TrailRenderer 강화(D-10)의 정확한 수치(두께, Time, 색상 그라데이션)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 포탈 전환 연출 설계 원본
- `.planning/phases/10-exit-portal-floor-transition/10-TRANSITION-DESIGN.md` — SpriteMask 기반 입장/퇴장 연출 원안 설계 전체(단계별 시퀀스, 지속시간 수치, PortalEffect 프리팹 구성, SpriteMask 방향 로직 코드 스니펫 포함) — **이번 phase의 핵심 구현 대상**
- `.planning/phases/10-exit-portal-floor-transition/10-CONTEXT.md` — Phase 10 결정사항 (ExitPortal/WorldGenerator 통합 지점, D-04~D-08)

### 기술 제약
- `.planning/STATE.md` §Technical Constraints to Enforce Every Phase — `Time.unscaledDeltaTime`, Animator Transition Duration = 0, `WaitForSecondsRealtime` 필수

### 공격/애니메이터 관련 기존 코드
- `Assets/Scripts/Player/CombatController.cs` (427줄) — `ExecuteWhiff()`(line 310-317, `SetTrigger("Whiff")` 호출부), `ExecuteDash()`(line 250-308, `TrailRenderer.emitting` on/off, `HitFreeze()` 호출), `DashOrWhiff()`(line 223-248, `IsAttacking` bool 제어)
- `Assets/Scripts/Player/RollController.cs` (line 56-57, `SetTrigger("Roll")` 호출부)
- `Assets/Scripts/Player/PlayerAnimatorController.cs` — `IsRolling` bool 구동 로직(line 49), 기존 애니메이터 파라미터 전체 목록(`IsMoving`, `IsGrounded`, `VelocityY`, `IsSprinting`, `IsRolling`)
- `Assets/Player/Resource/Animation/FastPlayerAnimator.controller` — 플레이어 애니메이터 컨트롤러. 현재 params: `IsMoving, IsGrounded, VelocityY, IsAttacking, IsRolling, IsSprinting, IsDashing`; states: `Idle, Walk, JumpRise, JumpFall, Sprint, Dash, Attack, Roll, JumpMid`. **Whiff 파라미터/상태가 없음 — 신규 추가 필요(D-05)**. Roll 트리거 파라미터도 없음(IsRolling bool만 존재 — D-06)

### 포탈 관련 기존 코드
- `Assets/Scripts/World/WorldGenerator.cs` — `EnterPortal()`(line 431-434), `FloorTransitionSequence()`(line 436-491, 현재 6단계 단순 버전 — SpriteMask/Animator/PortalEffect 관련 코드 전무 확인됨) — 신규 컴포넌트 호출 지점 삽입 대상
- `Assets/Scripts/World/ExitPortal.cs` (31줄) — 포탈 트리거 스크립트, 현재 Animator 미참조
- `Assets/Prefabs/World/ExitPortal/ExitPortal.prefab` — SpriteRenderer + Animator(`ExitPortal.controller` 연결, 파라미터 없음) + CircleCollider2D(trigger) — Animator는 D-04에 따라 미사용 유지
- `Assets/Prefabs/World/PortalEffect.prefab` — **아직 존재하지 않음** (10-TRANSITION-DESIGN.md가 "신규"로 명시한 대로) — D-02에 따라 ExitPortal 스프라이트 재활용해 신규 생성 필요

### 히트 임팩트 재활용 에셋
- `Assets/DeadRevolver/PixelPrototypePlayerSprites/Art/Sprites/Combat/GuardImpact/GuardImpact01~03.png` + `Assets/DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/SwordGuardImpact.anim` — 히트 스파크 재활용 대상(D-07)
- `Assets/Scripts/Enemy/MeleeEnemy.cs`, `Assets/Scripts/Enemy/RangedEnemy.cs` — `isDead` bool 트리거 위치, 사망 시점 훅
- `Assets/Animations/Enemies/MeleeEnemyAnimator.controller`, `Assets/Animations/Enemies/RangedEnemyAnimator.controller` — `Die` 상태 존재 확인됨(params: `isMoving/isDead/isAttacking/isChasing`, 소문자 시작 — 플레이어 파라미터의 PascalCase와 다른 컨벤션이므로 혼동 주의)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `CombatController._trailRenderer` — 이미 대시 중 `emitting` on/off로 사용 중(D-10에서 강화 대상)
- `GuardImpact01~03.png` + `SwordGuardImpact.anim` — 검 히트 스파크로 즉시 재활용 가능(신규 아트 불필요)
- `ExitPortal` 스프라이트 — PortalEffect 프리팹의 비주얼 소스로 재활용

### Established Patterns
- 마커/이펙트 컴포넌트는 독립 파일로 분리하는 패턴이 프로젝트 전반에 확립됨(`RoomConnector`, `RoomEntry`, `EnemySpawner`, `ExitPortal` 등 각각 단일 책임) — `FloorTransitionEffect` 신규 컴포넌트도 이 패턴을 따름
- 애니메이터 파라미터 네이밍이 스크립트군마다 다름: 플레이어 계열은 PascalCase(`IsMoving`, `IsAttacking`), 적 계열은 lowerCamelCase(`isMoving`, `isDead`) — 신규 파라미터(Whiff) 추가 시 플레이어 컨트롤러의 PascalCase 컨벤션을 따를 것
- 타이머/코루틴은 전부 `WaitForSecondsRealtime` 또는 `Time.unscaledDeltaTime` 기반 — 포탈 전환 연출 코루틴도 동일 원칙 적용 필요(슬로우모션 면역)

### Integration Points
- `WorldGenerator.EnterPortal()` / `FloorTransitionSequence()` — 신규 `FloorTransitionEffect` 컴포넌트의 진입/퇴장 재생 메서드 호출 지점
- `CombatController.ExecuteDash()` 처치 성공 분기(line ~300, `target.OnDashHit()` 호출 직후) — 히트 스파크(D-07) + 카메라 쉐이크(D-08) 트리거 지점
- `MeleeEnemy.cs`/`RangedEnemy.cs`의 사망 처리부(`isDead` bool 설정 위치) — 적 사망 SpriteMask+파티클 연출(D-09) 트리거 지점
- `FastPlayerAnimator.controller` — Whiff 트리거/상태 신규 추가, Roll 트리거 신규 추가(또는 IsRolling 연결) 위치

</code_context>

<specifics>
## Specific Ideas

- "공격 모션이 너무 밋밋해서, 히트 순간에 임팩트가 더 확 느껴졌으면 좋겠어" — 사용자 원문, 히트 임팩트 개선(D-07~D-09)의 동기
- "플레이어의 공격모션에 잔상을 넣고싶어" (빠른 느낌) — 기존 TrailRenderer 강화로 구체화(D-10)
- "적이 파티클이 되어서 사라지거나, 갈라지는 연출을 하고싶어" → "Death애니메이션도 있던데 애니메이션 이후에 파티클이 재생되면서 스프라이트를 아래서부터 감추고 파괴하면 될것같아" — 사용자가 직접 제안한 구현 순서, D-09에 그대로 반영됨

</specifics>

<deferred>
## Deferred Ideas

- 적(MeleeEnemy/RangedEnemy) 공격 애니메이션/이펙트 개선 — 사용자가 "플레이어만"으로 범위를 명확히 좁힘. 향후 적 전투 연출 개선이 필요하면 별도 phase로.
- ExitPortal.prefab에 이미 연결된 미사용 Animator 활용 — 이번 phase는 코드 기반 방식(D-01) 채택, Animator 활용 재설계는 논의되지 않음.

없음 — 논의가 Phase 범위 내에 머무름.

</deferred>

---

*Phase: 12-animation-polish*
*Context gathered: 2026-07-07*
