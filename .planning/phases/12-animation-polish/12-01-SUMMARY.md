---
phase: 12-animation-polish
plan: 01
subsystem: world
tags: [unity, spritemask, coroutine, floor-transition, portal-animation]

# Dependency graph
requires:
  - phase: 10-exit-portal-floor-transition
    provides: "10-TRANSITION-DESIGN.md sequence spec (E1-E4/X1-X4), ExitSpawnPoint-based spawn logic"
provides:
  - "RuntimeMaskSprite.CreateMaskSprite() -- cached 4x4 white SpriteMask sprite factory"
  - "FloorTransitionEffect.PlayEntry(Transform portal) / PlayExit(Vector3 spawnWorldPos, GameObject portalEffectPrefab) coroutine contract"
affects: [12-02-worldgenerator-wiring, 12-08-enemy-death-effect]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "SpriteMask 런타임 생성 + 캐시 -- 에셋 없이 코드에서 마스크 스프라이트 생성"
    - "Time.unscaledDeltaTime 기반 코루틴 Lerp -- 슬로우모션/HitFreeze(timeScale=0) 면역 연출"

key-files:
  created:
    - Assets/Scripts/World/RuntimeMaskSprite.cs
    - Assets/Scripts/World/FloorTransitionEffect.cs
  modified: []

key-decisions:
  - "RuntimeMaskSprite를 별도 정적 클래스로 분리 -- Plan 12-08 EnemyDeathEffect(D-09)도 동일 마스크 생성 로직 재사용 예정"
  - "FloorTransitionEffect는 WorldGenerator를 참조하지 않는 자기완결형 컴포넌트 -- Plan 12-02가 배선만 담당"

patterns-established:
  - "SpriteMask 방향 로직: direction = player.x > targetX ? +1 : -1; maskCenter.x = targetX + (width*0.5f*direction)"

requirements-completed: [D-01, D-03, D-04]

# Metrics
duration: 8min
completed: 2026-07-08
---

# Phase 12 Plan 01: FloorTransitionEffect Component Contract Summary

**RuntimeMaskSprite 캐시 유틸 + FloorTransitionEffect.PlayEntry()/PlayExit() 코루틴 컴포넌트 신규 생성 — 10-TRANSITION-DESIGN.md의 SpriteMask 포탈 입/퇴장 연출을 Player 부착형 자기완결 컴포넌트로 구현**

## Performance

- **Duration:** 8 min
- **Started:** 2026-07-08T04:00:00Z (approx, session start)
- **Completed:** 2026-07-08T04:07:26Z
- **Tasks:** 2
- **Files modified:** 2 (both new)

## Accomplishments
- `RuntimeMaskSprite.CreateMaskSprite()` — 4x4 흰색 텍스처 기반 SpriteMask 스프라이트를 정적 캐시로 생성, 반복 호출 시 GC 압박 없음
- `FloorTransitionEffect.PlayEntry(Transform portal)` — E1-E4 시퀀스: 마스크 성장(0→플레이어 전체 너비, 0.4s) → 포탈 수축(0.3s) → SpriteRenderer 비활성화
- `FloorTransitionEffect.PlayExit(Vector3 spawnWorldPos, GameObject portalEffectPrefab)` — X1-X4 시퀀스: 포탈 성장(0.4s) → 마스크 수축(startWidth→0, 0.5s) → 포탈 페이드(0.3s)
- 모든 타이밍 루프가 `Time.unscaledDeltaTime` 기반 — 슬로우모션/HitFreeze(timeScale=0) 중에도 정상 진행 (STATE.md 기술 제약 준수)

## Task Commits

Each task was committed atomically:

1. **Task 1: RuntimeMaskSprite.cs — 공용 SpriteMask 스프라이트 생성 유틸** - `4c7332c` (feat)
2. **Task 2: FloorTransitionEffect.cs — 포탈 입장/퇴장 SpriteMask 애니메이션 컴포넌트** - `26e994e` (feat)

_Note: parallel executor agent — commits made with --no-verify per orchestrator instructions; hooks validated once after all agents complete._

## Files Created/Modified
- `Assets/Scripts/World/RuntimeMaskSprite.cs` (24 lines) - 캐시된 정적 `CreateMaskSprite()` 팩토리
- `Assets/Scripts/World/FloorTransitionEffect.cs` (125 lines) - `PlayEntry()`/`PlayExit()`/`ScaleTransform()` 코루틴 3개, `[RequireComponent(typeof(SpriteRenderer))]`

## Interface for Plan 12-02

Plan 12-02(WorldGenerator 배선)가 그대로 호출할 정확한 계약:

```csharp
// Player GameObject에 FloorTransitionEffect 컴포넌트 부착 필요 (SpriteRenderer 필수)
FloorTransitionEffect fx = player.GetComponent<FloorTransitionEffect>();

// ENTRY (기존 FloorTransitionSequence E1-E4 대체)
yield return fx.PlayEntry(exitPortalTransform);

// EXIT (기존 FloorTransitionSequence X1-X4 대체)
yield return fx.PlayExit(spawnWorldPos, portalEffectPrefabOrNull);
```

`portalEffectPrefab`이 `null`이면 X1/X4 포탈 이펙트 단계는 완전히 스킵된다 (방어적 null 체크 내장).

## RuntimeMaskSprite 캐싱 전략

- 첫 호출 시 `Texture2D(4,4)` + `Sprite.Create()` 1회 생성, 정적 필드에 캐시
- 이후 모든 호출(같은 프레임이든 다른 코루틴이든)은 캐시된 `Sprite` 인스턴스를 즉시 반환 — 텍스처 재할당 없음
- `FloorTransitionEffect`(이 Plan)와 `EnemyDeathEffect`(D-09, **Plan 12-08 예정**)가 동일 캐시를 공유해 모바일 GC 압박을 이중으로 줄인다

## Decisions Made
- RuntimeMaskSprite를 별도 정적 클래스로 분리 (Plan 12-08 재사용 대비) — 플랜 지시대로
- FloorTransitionEffect는 WorldGenerator에 대한 컴파일 타임 참조 없이 독립 컴포넌트로 작성 — Plan 12-02가 배선 전담

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

**Worktree path mismatch:** 초기 Write 시도가 공유 체크아웃 경로(`D:\새 폴더\Fast\Assets\...`)를 대상으로 하여 격리 오류가 발생. 병렬 실행 에이전트 워크트리 경로(`D:\새 폴더\Fast\.claude\worktrees\agent-ab12e46139dbcb250\Assets\...`)로 전환해 해결. 코드 내용에는 영향 없음.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Plan 12-02가 `WorldGenerator.FloorTransitionSequence()`에서 `FloorTransitionEffect.PlayEntry()`/`PlayExit()`를 호출하도록 배선 가능
- Player 프리팹에 `FloorTransitionEffect` 컴포넌트를 수동으로 부착하는 에디터 작업이 Plan 12-02 또는 별도 EDITOR_TASKS 항목으로 필요할 수 있음 (이 Plan은 컴포넌트 코드만 제공, Player GameObject에 실제로 부착하지 않음)
- Plan 12-08(EnemyDeathEffect, D-09)이 `RuntimeMaskSprite.CreateMaskSprite()`를 즉시 재사용 가능

---
*Phase: 12-animation-polish*
*Completed: 2026-07-08*

## Self-Check: PASSED

- FOUND: Assets/Scripts/World/RuntimeMaskSprite.cs
- FOUND: Assets/Scripts/World/FloorTransitionEffect.cs
- FOUND: commit 4c7332c
- FOUND: commit 26e994e
