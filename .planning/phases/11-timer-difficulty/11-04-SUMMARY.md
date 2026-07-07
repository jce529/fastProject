---
phase: 11-timer-difficulty
plan: 04
subsystem: ui
tags: [unity-editor-tool, tmp, scene-wiring, playtest]

# Dependency graph
requires:
  - phase: 11-01
    provides: FloorTimer static class (RemainingSeconds, Reset, Tick)
  - phase: 11-02
    provides: WorldGenerator difficulty spawn + FloorTimer/ScoreManager integration, _meleeEnemyPrefab/_rangedEnemyPrefab fields
  - phase: 11-03
    provides: HUDController._timerLabel field + TimerFlickerLoop() coroutine
provides:
  - HUDTimerLabelBuilder.cs editor menu tool (Fast > Phase11 > Add Timer Label To HUD)
  - SampleScene TimerLabel GameObject wired to HUDController._timerLabel
  - SampleScene WorldGenerator._meleeEnemyPrefab/_rangedEnemyPrefab confirmed wired
  - Playtest confirmation of all Phase 11 success criteria
affects: [phase-12]

# Tech tracking
tech-stack:
  added: []
  patterns: [SerializedObject-based editor wiring tool, clone-existing-label pattern for new TMP UI elements]

key-files:
  created: [Assets/Editor/HUDTimerLabelBuilder.cs]
  modified: [Assets/Scenes/SampleScene.unity]

key-decisions:
  - "WorldGenerator._meleeEnemyPrefab/_rangedEnemyPrefab in SampleScene already carried correct serialized values before Task 2 — confirmed as orphaned Unity serialization data left over from an earlier field of the same name, not new wiring; no scene edit was needed for that half of Task 2."

patterns-established:
  - "Editor menu tools that add new UI elements should clone a sibling with matching style/anchoring (here: clone _scoreLabel to create TimerLabel) rather than constructing RectTransform layout from scratch."

requirements-completed: [TIMER-01, TIMER-02, DIFF-01, SCORE-01, SCORE-02]

# Metrics
duration: ~20min (execution + human checkpoint round-trips)
completed: 2026-07-07
---

# Phase 11: 타이머 & 난이도 Summary

**HUD 카운트다운 타이머(슬로우모션 면역) + 층별 점멸 경고 + 층 번호 기반 난이도 스케일링 + EXIT 포탈 시간 비례 점수, 전부 플레이테스트로 확인 완료**

## Performance

- **Duration:** ~20 min (Task 1 자동 실행 + Task 2/3 human checkpoint 왕복 포함)
- **Tasks:** 3/3
- **Files modified:** 2 (HUDTimerLabelBuilder.cs 신규, SampleScene.unity)

## Accomplishments
- `HUDTimerLabelBuilder.cs` 에디터 메뉴 도구 신규 생성 — 기존 `_scoreLabel`을 클론해 `TimerLabel`을 만들고 `SerializedObject`로 `HUDController._timerLabel`에 자동 연결
- SampleScene에 `TimerLabel`(HUDController의 ScoreGroup 하위) 생성 및 연결 확인
- `WorldGenerator._meleeEnemyPrefab`/`_rangedEnemyPrefab` Inspector 연결 확인 (MeleeEnemy.prefab/RangedEnemy.prefab)
- 플레이테스트로 Phase 11 성공 기준 5개 항목(TIMER-01, TIMER-02, DIFF-01, SCORE-01, SCORE-02) 전부 통과 확인

## Task Commits

1. **Task 1: HUDTimerLabelBuilder.cs 에디터 도구 생성** - `b47795c` (feat)
2. **Task 2: SampleScene TimerLabel 생성 + Inspector 연결** - `a6c4502` (feat)
3. **Task 3: 플레이테스트 검증** - 코드 변경 없음 (human-verify 체크포인트만 해당)

**Plan metadata:** (this commit) — docs: complete plan

## Files Created/Modified
- `Assets/Editor/HUDTimerLabelBuilder.cs` - `_scoreLabel`을 클론해 `TimerLabel` TMP 오브젝트 생성 + `HUDController._timerLabel` SerializedObject 연결 메뉴 도구
- `Assets/Scenes/SampleScene.unity` - `TimerLabel` GameObject 추가, `HUDController._timerLabel` 필드 연결 확인

## Decisions Made
- `WorldGenerator._meleeEnemyPrefab`/`_rangedEnemyPrefab`가 씬에서 이미 올바른 값으로 직렬화되어 있음을 확인 — Plan 11-02에서 신규 추가된 필드명과 동일한 이름의 과거 필드가 남긴 잔여 직렬화 데이터로 판단됨. 별도 드래그 연결 작업 없이 그대로 사용.

## Deviations from Plan

None - plan executed exactly as written. (환경 설정상 worktree가 stale했던 부분은 Rule 3에 따라 각 executor가 `git merge main --ff-only`로 자체 해결 — 실제 코드/씬 변경에는 영향 없음)

## Issues Encountered
- Task 2 진행 중 최초 확인 시점에 `SampleScene.unity`에서 `TimerLabel`/`_timerLabel` 문자열이 발견되지 않아 메뉴 실행이 누락된 것으로 의심되었음 — 사용자가 재확인 후 실제로는 저장이 뒤늦게 반영된 것으로 확인되어 정상 진행.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Phase 11의 모든 요구사항(TIMER-01, TIMER-02, DIFF-01, SCORE-01, SCORE-02)이 코드와 씬 배선, 플레이테스트로 모두 확인됨.
- Phase 12(애니메이션 폴리시)로 진행 가능한 상태.

---
*Phase: 11-timer-difficulty*
*Completed: 2026-07-07*
