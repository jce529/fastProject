---
phase: quick-260605-tss
plan: 01
subsystem: combat
tags: [highlight, slowmo, bug-fix, CombatController]
key-files:
  modified:
    - Assets/Scripts/Player/CombatController.cs
decisions:
  - "UpdateHighlight()를 별도 메서드로 분리해 FindNearestEnemyInRange()를 순수 탐색 함수로 유지"
  - "하이라이트 갱신 책임을 Update()의 _isSlowMo 구간으로 단일화"
metrics:
  duration: ~5min
  completed: 2026-06-05
  tasks: 1
  files: 1
---

# Quick Task 260605-tss: CombatController 슬로우모션 하이라이트 버그 수정 Summary

**One-liner:** Update()에 슬로우모션 중 매 프레임 UpdateHighlight() 호출 추가 + FindNearestEnemyInRange()에서 하이라이트 사이드이펙트 제거

---

## What Was Done

두 가지 하이라이트 버그를 CombatController.cs 단일 파일에서 수정했다.

**버그 1 수정 — 슬로우모션 중 하이라이트 없음:**
`FindNearestEnemyInRange()`는 `DashOrWhiff()` (AttackReleased 시 1회)에서만 호출됐기 때문에, 슬로우모션 유지 중에는 하이라이트가 전혀 갱신되지 않았다. `Update()`에서 `_isSlowMo && !_isBusy` 조건 아래 매 프레임 `UpdateHighlight(FindNearestEnemyInRange())`를 호출해 수정했다.

**버그 2 수정 — ExitSlowMotion() 이중 호출로 인한 하이라이트 즉시 소멸:**
기존 흐름: `AttackReleased` → `ExitSlowMotion()` (하이라이트 클리어) → `DashOrWhiff()` → `FindNearestEnemyInRange()` (하이라이트 재설정) → `ExecuteDash()` → `ExitSlowMotion()` (하이라이트 재클리어). 결국 대시가 시작되는 순간 하이라이트가 사라졌다. `FindNearestEnemyInRange()`에서 하이라이트 업데이트 블록을 완전히 제거해 해당 경로의 하이라이트 재설정을 차단했다.

**구조 변경:**
- `FindNearestEnemyInRange()`: 순수 탐색 함수로 복원 — 가장 가까운 DummyEnemy만 반환, 사이드이펙트 없음
- `UpdateHighlight(DummyEnemy nearest)`: 신설 — `_lastHighlighted` 비교, ClearHighlight(), Color.red 설정 담당
- `Update()`: `_isSlowMo` 유지 구간에 하이라이트 갱신 추가 (safety timeout 체크 직후, gauge-empty 체크 직전)
- `ExitSlowMotion()`: 기존 클리어 로직 유지 — 슬로우모션 종료 시 정상 정리

---

## Tasks

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Update() 슬로우모션 하이라이트 + FindNearestEnemyInRange 사이드이펙트 제거 | ac19ef4 |

---

## Deviations from Plan

None — plan executed exactly as written.

---

## Self-Check

- [x] `Assets/Scripts/Player/CombatController.cs` — modified (worktree)
- [x] Commit ac19ef4 exists
- [x] `UpdateHighlight()` 메서드 추가됨
- [x] `FindNearestEnemyInRange()`에서 하이라이트 블록 제거됨
- [x] `Update()`의 `_isSlowMo` 구간에 `UpdateHighlight()` 호출 추가됨

## Self-Check: PASSED
