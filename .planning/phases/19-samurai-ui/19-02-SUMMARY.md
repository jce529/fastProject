---
phase: 19-samurai-ui
plan: 02
subsystem: ui
tags: [unity-ui, combat-module-selection, unlock-gating, editor-tool, tmp]

# Dependency graph
requires:
  - phase: 18-shared-infra
    provides: BossUnlockManager.IsUnlocked(bossId) 해금 조회 API
provides:
  - "CombatModuleRegistry.All — 3엔트리(기본전투모듈/Overclock/사무라이 전투형 모듈) 배열 기반 레지스트리, 향후 보스 슬롯 1줄 추가로 확장 가능"
  - "CombatModuleSelector.SelectedIndex — AttackTypeSelector와 동일한 static 선택 상태 컨벤션"
  - "AttackSelectController — 데이터 기반 N-way 모듈 선택 로직 (Linear/Fan 하드코딩 제거)"
  - "AttackSelectUIBuilder.cs (Fast/Phase19/Build AttackSelect UI) — 3버튼 씬 재구성 멱등적 에디터 도구 (미실행)"
affects: [19-03-combat-controller-module-swap, 19-06-integration-checkpoint]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "static 선택 상태 컨벤션 재사용 (AttackTypeSelector → CombatModuleSelector)"
    - "UI 레이어를 CombatModuleId enum + 문자열 requiredBossId로 전투 모듈 구현체와 decouple"

key-files:
  created:
    - Assets/Scripts/UI/CombatModuleRegistry.cs
    - Assets/Scripts/UI/CombatModuleSelector.cs
    - Assets/Editor/AttackSelectUIBuilder.cs
  modified:
    - Assets/Scripts/UI/AttackSelectController.cs

key-decisions:
  - "AttackTypeSelector(Linear/Fan 공격 형태) 자체는 무변경 — 모듈 선택과 직교하는 별도 책임"
  - "AttackSelectUIBuilder는 도구 작성만, 씬 반영은 19-06 checkpoint에서 일괄 실행"

patterns-established:
  - "신규 보스 모듈 추가 시 CombatModuleRegistry.All에 엔트리 1줄만 추가하면 되는 확장 구조"

requirements-completed: [UNLOCK-02, UNLOCK-03]

# Metrics
duration: 12min
completed: 2026-08-07
---

# Phase 19 Plan 02: Combat Module Selection UI Summary

**배열 기반 CombatModuleRegistry(3엔트리) + static CombatModuleSelector로 AttackSelectController를 하드코딩 2-way(Linear/Fan)에서 데이터 기반 N-way 모듈 선택 화면으로 재작성하고, BossUnlockManager 해금 상태에 따른 잠금 표시(D-13)를 구현했다.**

## Performance

- **Duration:** 12 min
- **Started:** 2026-08-07T06:33:00Z
- **Completed:** 2026-08-07T06:45:37Z
- **Tasks:** 3
- **Files modified:** 4 (3 created, 1 rewritten)

## Accomplishments
- `CombatModuleRegistry.All` 3엔트리(기본전투모듈=상시해금, Overclock="Fiora" 게이팅 유지, 사무라이 전투형 모듈="Samurai" 신규 게이팅) — 전투 모듈 구현 클래스와 완전히 decoupled
- `CombatModuleSelector` static 선택 상태(index 0 기본값 = 항상 해금된 기본전투모듈로 안전한 폴백)
- `AttackSelectController.Start()`가 레지스트리를 순회하며 `Button.interactable`/lock icon `enabled`/라벨 텍스트를 데이터 기반으로 설정, 클릭 시 `CombatModuleSelector.SetSelected(index)` 후 SampleScene 로드
- `AttackSelectUIBuilder.cs` 멱등적 에디터 도구 — Linear/Fan 2버튼을 3버튼(라벨+잠금 아이콘)으로 재구성하고 컨트롤러 배열에 배선 (아직 미실행, AttackSelect.unity 무변경 확인)

## Task Commits

Each task was committed atomically:

1. **Task 1: CombatModuleRegistry + CombatModuleSelector** - `396f713` (feat)
2. **Task 2: AttackSelectController N-way 재작성** - `44355a5` (feat)
3. **Task 3: AttackSelectUIBuilder 에디터 도구** - `da38fa5` (feat)

## Files Created/Modified
- `Assets/Scripts/UI/CombatModuleRegistry.cs` - CombatModuleId enum + CombatModuleEntry struct + 3엔트리 static 배열
- `Assets/Scripts/UI/CombatModuleSelector.cs` - static 선택 상태 (SelectedIndex/SelectedModuleId/SetSelected)
- `Assets/Scripts/UI/AttackSelectController.cs` - 하드코딩 OnLinearClicked/OnFanClicked 제거, 레지스트리 순회 N-way 로직으로 완전 교체
- `Assets/Editor/AttackSelectUIBuilder.cs` - Fast/Phase19/Build AttackSelect UI 메뉴, 3버튼 절차적 생성+배선, 멱등적 재실행 지원

## Decisions Made
- 계획대로 진행 — 별도 결정 없음. `AttackTypeSelector`(Linear/Fan 공격 형태)는 계획 명시대로 전혀 건드리지 않음.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

worktree(`worktree-agent-a8af4b1da1e5fe6b7`)가 세션 시작 시 main보다 크게 뒤처져 있어(`999.4-01`까지만 반영, `.planning/phases/19-samurai-ui/` 자체가 부재) Plan 파일을 찾을 수 없었음 — `git merge main --ff-only`로 무손실 fast-forward 동기화 후 진행(HEAD가 main의 ancestor임을 `git merge-base --is-ancestor`로 사전 확인, 워킹 트리 clean 상태였으므로 충돌 없음).

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- `CombatModuleSelector.SelectedIndex`/`SelectedModuleId`가 19-03(CombatController 모듈 스왑)이 읽어들일 선택 상태로 확정됨
- `AttackSelectUIBuilder.cs`는 19-06 checkpoint에서 Unity 에디터로 실제 실행 필요 (씬 저장 포함) — 이 Plan 범위 밖
- Overclock 게이팅("Fiora")은 기존 그대로 유지되어 회귀 없음

---
*Phase: 19-samurai-ui*
*Completed: 2026-08-07*

## Self-Check: PASSED

- FOUND: Assets/Scripts/UI/CombatModuleRegistry.cs
- FOUND: Assets/Scripts/UI/CombatModuleSelector.cs
- FOUND: Assets/Scripts/UI/AttackSelectController.cs
- FOUND: Assets/Editor/AttackSelectUIBuilder.cs
- FOUND commit: 396f713 (Task 1)
- FOUND commit: 44355a5 (Task 2)
- FOUND commit: da38fa5 (Task 3)
