# Phase 14: 적 등장 스폰 연출 - Context

**Gathered:** 2026-07-10
**Status:** Ready for planning

<domain>
## Phase Boundary

근접형/원거리형 적(추후 보스도 재사용)이 스폰될 때 플레이어처럼 포탈을 타고 등장하는 연출이 재생되고, 연출이 끝나기 전까지 감지/공격 대상이 되지 않는다. (SPWN-01, SPWN-02)

이 Phase는 **적 스폰 연출**에만 집중한다. 플레이어 자신의 층 전환 포탈 연출(FloorTransitionEffect, Phase 12에서 완성)을 개선하는 작업과 적 무한 리스폰 메커니즘은 범위 밖 — 아래 `<deferred>` 참고.

</domain>

<decisions>
## Implementation Decisions

### 스폰 트리거 타이밍
- **D-01:** 스폰 포탈은 플레이어가 해당 룸/Corridor에 실제로 진입할 때 나타난다. 현재 `WorldGenerator.TrySpawnEnemies()`는 룸이 플레이어보다 2개 앞서 미리 생성(`SpawnNextPair`)될 때 `Spawn()+Activate()`를 즉시 함께 호출해 화면 밖에서 스폰이 끝나버린다 — 이 아키텍처를 분리해야 한다. `Spawn()`(비활성 인스턴스 생성)은 사전 생성 시점에 유지 가능하지만, `Activate()`(및 스폰 VFX 트리거)는 플레이어가 실제로 그 룸/Corridor 구간에 도달하는 시점으로 지연되어야 한다. 룸 진입 감지는 `WorldGenerator`가 이미 추적하는 `_playerCurrentIndex` 변경 시점을 활용할 수 있다.
- **D-02:** 이미 스폰 연출을 재생한 룸/구간은 재진입해도 다시 재생하지 않는다 — 1회성 스폰이며 무한 리스폰이 아니다 (아래 `<deferred>` 참고).
- **D-03:** Room뿐 아니라 Corridor 3종(상승/직진/하강, 모두 전투 구간)에도 동일한 스폰 연출 로직을 적용한다 — Room/Corridor 구분 없이 EnemySpawner 마커 기준으로 통일 처리.

### 포탈 개수 및 순서
- **D-04:** 한 룸/Corridor 내 여러 마리가 있을 경우 각 `EnemySpawner` 마커 위치마다 개별 포탈이 생성된다 (마커 하나 = 포탈 하나, 기존 마커 배치를 그대로 사용).
- **D-05:** 여러 포탈은 룸 진입과 동시에 한꺼번에 뜨지 않고, 약간의 시차를 두고 순차적으로 나타난다 ("배열에 넣고 하나씩 배출"). 구체적 스태거 간격은 Claude's Discretion.

### 비주얼 스타일
- **D-06:** `FloorTransitionEffect.PlayExit()`과 동일한 구조적 패턴(PortalEffect 프리팹 성장 → SpriteMask 수축 페이드인 → 포탈 축소 후 Destroy, 총 ~1.2초 기준)을 재사용한다. 기존 `Assets/Prefabs/World/PortalEffect/PortalEffect.prefab`(비주얼 전용, Collider 없음)과 `RuntimeMaskSprite.CreateMaskSprite()`를 그대로 활용 가능.
- **D-07:** 단, 기존 플레이어 퇴장 방식(제자리 고정 + 마스크만 걷힘)과 달리, 적은 포탈 중심에서 실제로 걸어나오는 움직임(Rigidbody2D 이동)이 추가되어야 한다 — 마스크 수축과 실제 위치 이동이 함께 진행되어 "포탈에서 걸어나온다"는 느낌을 실질적으로 준다. (플레이어 쪽 버전은 정적이라는 게 사용자가 지적한 불만이며, 적 스폰에는 이 개선을 바로 반영한다.)
- **D-08:** 포탈 크기는 적 스프라이트 크기에 맞춰 자동 스케일되어야 한다 — 크기 독립적으로 설계해 Phase 16 보스 재사용 시 별도 작업이 필요 없도록 한다.

### 사운드
- **D-09:** 일반 적(근접/원거리) 스폰 연출에도 사운드를 재생한다 — 기존 `AudioManager.PlaySfx(Sfx.PortalEnter)` / `Sfx.PortalExit`를 재사용(신규 클립 임포트 불필요). SFX-05(보스 전용 스폰 사운드, Phase 16 범위)와는 별개의 폴리싱이지만 트리비얼하게 구현 가능해 이번 Phase에 포함한다.

### 조작 및 게이팅
- **D-10:** 스폰 연출 중 플레이어 조작에는 제약이 없다 — 이동/공격/구르기 모두 자유롭게 가능하다. (층 전환 시퀀스의 입력 잠금과는 다른 성격 — 이건 전투 진입 연출이지 씬 전환이 아니다.)
- **D-11:** 스폰 연출 재생 중인 적은 `CombatController.FindNearestEnemyInRange()`의 타겟 후보에서 제외되고, 감지/추격/공격 FSM도 동작하지 않아야 한다 (SPWN-02). 구현 방식(예: 기존 `IEnemy.IsAlive` 게이트를 스폰 중에도 재사용할지, 별도 `IsSpawning` 플래그를 신설할지)은 Claude's Discretion — 단, `IEnemy` 3-member 계약(`IsAlive`, `OnDashHit()`, `ClearHighlight()`) 자체는 변경하지 않아야 Phase 15/16의 BossEnemy 통합 전제("IEnemy 계약 변경 없음")가 깨지지 않는다.

### Claude's Discretion
- 포탈 간 스태거 간격 구체적 수치 (예: 0.2~0.4초 범위)
- 감지/타겟팅 차단 구현 방식 (IsAlive 재사용 vs 신규 플래그) — 단 IEnemy 계약 불변 조건 준수
- 룸/Corridor 진입 감지 훅의 정확한 구현 위치 (WorldGenerator 내부)
- 다중 적 배출 순서 (마커 컴포넌트 순회 순서 vs 랜덤)

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 요구사항 / 로드맵
- `.planning/ROADMAP.md` §Phase 14 — 목표, Depends on Phase 13, Success Criteria 5개
- `.planning/REQUIREMENTS.md` — SPWN-01, SPWN-02 (요구사항 원문), Out of Scope 표
- `.planning/STATE.md` §Key Decisions Locked for v3.1 — "스폰 VFX(포탈 스타일)는 일반 적+보스 공통, 연출 중 감지/타겟팅 차단 — EnemySpawner.Activate() 시점에서만 트리거하는 적 타입 비종속 컴포넌트로 설계 — 보스 통합 시 재사용" (이번 논의로 트리거 시점 세부는 D-01로 구체화됨)

### 포탈 연출 설계 원안 (재사용 대상)
- `.planning/phases/10-exit-portal-floor-transition/10-TRANSITION-DESIGN.md` — 포탈 입출 애니메이션 원 설계 문서 (E1-E4/X1-X4 시퀀스, SpriteMask 방향 로직)
- `Assets/Scripts/World/FloorTransitionEffect.cs` — `PlayExit()` 메서드가 재사용 기준 패턴 (X1: 포탈 성장 → X2: 마스크 수축 페이드인 → X3: 마스크 정리 → X4: 포탈 페이드아웃)
- `Assets/Scripts/World/RuntimeMaskSprite.cs` — `CreateMaskSprite()` 런타임 마스크 스프라이트 생성 헬퍼
- `Assets/Editor/PortalEffectBuilder.cs` + `Assets/Prefabs/World/PortalEffect/PortalEffect.prefab` — 기존 포탈 비주얼 프리팹 (Collider 없음, 재사용 대상)

### 적 스폰 아키텍처 (변경 대상)
- `Assets/Scripts/World/EnemySpawner.cs` — Spawn()/Activate() 마커 컴포넌트 (2단계 분리 필요, D-01)
- `Assets/Scripts/World/WorldGenerator.cs` — `TrySpawnEnemies()`(386-411행 부근, Instantiate 직후 즉시 Spawn+Activate 호출), `SpawnNextPair()`, `_playerCurrentIndex` 추적 로직 (Update() 내 GEN-01/GEN-02/GEN-05 처리부)
- `Assets/Scripts/Enemy/IEnemy.cs` — 3-member 계약 (`IsAlive`, `OnDashHit()`, `ClearHighlight()`) — 변경 금지
- `Assets/Scripts/Enemy/MeleeEnemy.cs`, `Assets/Scripts/Enemy/RangedEnemy.cs` — `IsAlive` 프로퍼티, `Update()` 최상단 `if (!IsAlive) return;` 가드 (감지/타겟팅 차단 재사용 후보)
- `Assets/Scripts/Player/CombatController.cs` — `FindNearestEnemyInRange()` (363-410행 부근), `!enemy.IsAlive` 스킵 체크 (400행)
- `Assets/Scripts/Enemy/EnemyDeathEffect.cs` — 병렬 참고 사례: `AddComponent` 후 `StartCoroutine(...)`로 이펙트 컴포넌트를 붙이는 기존 컨벤션

### 오디오
- AudioManager 관련 문서/코드는 `.planning/phases/13-audio-foundation-sound-polish/` 참고 (SFX-02 PortalEnter/PortalExit 클립 배선 사례)

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `PortalEffect.prefab` — 스케일 애니메이션 기반 포탈 비주얼, 신규 프리팹 제작 불필요
- `RuntimeMaskSprite.CreateMaskSprite()` — SpriteMask 페이드 연출의 마스크 텍스처 생성 공용 헬퍼
- `FloorTransitionEffect.PlayExit()` 코드 구조 — 포탈 성장/마스크 수축/페이드아웃 3단계 시퀀스를 그대로 참고해 적 버전으로 확장(+실제 이동) 가능
- `AudioManager.PlaySfx(Sfx.PortalEnter/PortalExit)` — 신규 오디오 클립 임포트 없이 즉시 재사용 가능
- `IEnemy.IsAlive` 게이트 — `CombatController.FindNearestEnemyInRange()`와 각 적 FSM의 `Update()`가 이미 이 프로퍼티 하나로 "타겟 제외 + FSM 정지"를 동시에 처리하는 재사용 후보

### Established Patterns
- 이펙트 컴포넌트는 `AddComponent<T>()` 후 `StartCoroutine(...)`으로 붙이는 방식 (`EnemyDeathEffect` 선례) — 스폰 이펙트도 동일 컨벤션 예상
- 모든 타이밍은 `Time.unscaledDeltaTime`/`WaitForSecondsRealtime` 사용 — 슬로우모션/HitFreeze 면역 (프로젝트 전역 제약)
- `EnemySpawner`는 Room/Corridor 프리팹에 마커로 배치되어 `WorldGenerator`가 순회하며 제어 — Corridor도 동일 마커 구조 사용 (D-03 근거)

### Integration Points
- `WorldGenerator.TrySpawnEnemies()` 호출 시점/방식 자체가 이번 Phase의 핵심 변경 지점 (D-01) — `Spawn()`과 `Activate()`의 분리, 그리고 "플레이어가 실제로 그 구간에 도달했는가"를 판정하는 훅 필요
- `EnemySpawner.Activate()`가 향후 "스폰 VFX 재생 컴포넌트 부착 + IsAlive 초기값 제어"까지 담당하게 될 가능성 — 적 타입(Melee/Ranged, 추후 Boss)에 독립적인 컴포넌트로 설계해야 함 (STATE.md 기존 결정, D-08과 연결)

</code_context>

<specifics>
## Specific Ideas

- 세계관 연결: 플레이어(F.A.S.T.)뿐 아니라 적도 "HELIX가 배치한 시뮬레이션 NPC"이므로, 포탈을 타고 등장하는 연출은 세계관상 자연스럽다 (PROJECT.md Story & World 참고).
- "배열에 넣어놓고 하나씩 배출" — 사용자가 명시한 구현 이미지: 스폰이 필요한 적들을 큐/배열에 담아두고 순차적으로 하나씩 포탈에서 내보내는 방식.
- 적 스폰 포탈은 ExitPortal(층 전환용)과 반드시 동일한 프리팹일 필요는 없다 — 사용자가 "필요하면 ExitPortal이 아닌 다른 프리팹 생성도 가능"이라고 명시. 다만 이번 논의에서는 기존 `PortalEffect.prefab` 재사용으로 결정됨(D-06).
- 적 스폰 시 실제 이동(D-07)을 넣기로 한 이유는 플레이어 쪽 기존 연출(제자리 마스크만 걷힘)의 한계를 사용자가 직접 지적했기 때문 — "포탈의 중심으로부터 걸어나오는 연출인데 실제로 걸어나오는 움직임이 안 됨"이라는 피드백이 그대로 반영된 결정.

</specifics>

<deferred>
## Deferred Ideas

### 적 무한 리스폰 메커니즘
룸/Corridor을 플레이어가 반복 재진입할 때마다 적이 계속 새로 생성되는 아이디어. "가상 시뮬레이션에서 끝없이 싸우는 AI"라는 스토리 설정과 잘 맞아떨어지고, WorldGenerator가 이미 좌우 2개 룸까지만 유지하므로 무한 재생성이 성능상 크게 부담되지 않을 것이라는 논리도 있었음. 다만 밸런스(엔드리스 파밍으로 인한 타이머 긴장감 훼손 가능성)와 정확한 트리거 규칙(정확히 언제 재생성?) 설계가 필요한 별도 게임플레이 메커니즘 — 이번 Phase 14는 1회성 스폰(D-02)으로 진행하고, 이 아이디어는 향후 별도 마일스톤/Phase 후보로 백로그에 남긴다.

### 플레이어 포탈 연출 재작업 (FloorTransitionEffect 개선)
이미 완성된 Phase 12 기능에 대한 개선 아이디어:
- **진입 시:** 포탈 중심을 기준으로 일정 범위를 빨아들이는 흡입(suction) 이펙트 추가
- **퇴장 시:** 현재의 정적 마스크 페이드인 대신, 플레이어의 기존 대쉬(Dash) 애니메이션/모션을 활용해 포탈에서 한 번에 튀어나오는 연출로 교체

핵심 불만: 현재 `FloorTransitionEffect`는 SpriteMask만 움직이고 플레이어 Transform 자체는 제자리에 고정되어 있어 "걸어 들어가고 나온다"는 느낌이 실제 이동으로 반영되지 않음. 이 피드백은 Phase 14의 적 스폰 연출 설계(D-07 실제 이동 추가)에 직접 반영되었지만, 플레이어 쪽 기존 연출 자체를 고치는 작업은 범위 밖 — 별도 Phase에서 다룬다.

</deferred>

---

*Phase: 14-enemy-spawn-vfx*
*Context gathered: 2026-07-10*
