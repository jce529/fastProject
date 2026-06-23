---
phase: 07-attackselect-scene-scene-flow
plan: 01
subsystem: ui
tags: [scene-management, attack-type, unity-ui]

# Dependency graph
requires:
  - phase: 06-mainmenu-scene
    provides: MainMenuController pattern, MainMenu.unity at Build index 0
  - phase: 04-hud-game-loop
    provides: AttackTypeSelector.SetType() static API, AttackType enum, DeathScreenController
provides:
  - AttackSelectController.cs — LINEAR/FAN 버튼 클릭 핸들러 (SetType + LoadScene)
  - DeathScreenController.RestartGame() FLOW-01 수정 — AttackSelect 복귀
affects:
  - 07-02 (AttackSelect 씬 빌드 세팅 등록 및 UI 배선 시 이 컨트롤러를 참조)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "AttackSelectController: MonoBehaviour + SceneManagement, public void 버튼 핸들러 — MainMenuController 패턴 동일"

key-files:
  created:
    - Assets/Scripts/UI/AttackSelectController.cs
  modified:
    - Assets/Scripts/UI/DeathScreenController.cs

key-decisions:
  - "AttackTypeSelector.SetType()는 AttackSelect 씬에 AttackTypeSelector 인스턴스가 없어도 static Selected를 갱신하므로 별도 persist 로직 불필요"
  - "RestartGame()은 LoadScene(0) → LoadScene('AttackSelect') 단순 문자열 교체로 FLOW-01 달성 — 다른 코드 무변경"

patterns-established:
  - "씬 이동 컨트롤러는 public void 메서드로 버튼 onClick에 직렬화 — 에디터 직접 연결 패턴"

requirements-completed: [ATKS-01, ATKS-02, FLOW-01]

# Metrics
duration: 5min
completed: 2026-06-23
---

# Phase 7 Plan 01: AttackSelect Scene Flow Summary

**AttackSelectController (신규) + DeathScreenController.RestartGame() 수정으로 LINEAR/FAN 선택 → SampleScene 진입 및 사망 후 AttackSelect 복귀 씬 플로우 코드 완성**

## Performance

- **Duration:** ~5 min
- **Started:** 2026-06-23T13:49:00Z
- **Completed:** 2026-06-23T13:51:30Z
- **Tasks:** 2
- **Files modified:** 2

## Accomplishments
- AttackSelectController.cs 신규 생성 — OnLinearClicked()/OnFanClicked() 버튼 핸들러, SetType() + LoadScene("SampleScene") 호출
- DeathScreenController.RestartGame()의 LoadScene(0) → LoadScene("AttackSelect") 단일 라인 수정으로 FLOW-01 달성
- ATKS-01, ATKS-02, FLOW-01 요구사항 코드 수준 완성

## Task Commits

Each task was committed atomically:

1. **Task 1: AttackSelectController.cs 신규 생성** - `4106930` (feat)
2. **Task 2: DeathScreenController RestartGame() 씬 로드 타겟 변경** - `f9bf13c` (fix)

## Files Created/Modified
- `Assets/Scripts/UI/AttackSelectController.cs` - LINEAR/FAN 버튼 클릭 핸들러 (AttackType 설정 후 SampleScene 로드)
- `Assets/Scripts/UI/DeathScreenController.cs` - RestartGame() LoadScene(0) → LoadScene("AttackSelect")

## Decisions Made
- AttackTypeSelector.SetType()의 static 특성 덕분에 씬 전환 후에도 Selected 값이 유지되어 별도 DontDestroyOnLoad 또는 PlayerPrefs 없이 SampleScene에서 타입 읽기 가능
- DeathScreenController 수정은 1줄 교체로 완료 — 나머지 RestartGame() 로직(timeScale, fixedDeltaTime, FloorManager) 완전히 보존

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- AttackSelectController.cs 작성 완료, 07-02에서 AttackSelect.unity 씬 생성 및 UI 버튼과 OnLinearClicked/OnFanClicked 연결 필요
- DeathScreenController 수정 완료, RestartLabel 텍스트는 quick task 260623-t6i에서 이미 "메인 메뉴"로 변경됨 (AttackSelect 복귀 후 재확인 필요)

---
*Phase: 07-attackselect-scene-scene-flow*
*Completed: 2026-06-23*
