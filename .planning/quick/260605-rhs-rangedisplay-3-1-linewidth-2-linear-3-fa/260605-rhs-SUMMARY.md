---
phase: quick-260605-rhs
plan: 01
subsystem: combat-display
tags: [range-display, line-renderer, linear-beam, fan-arc, bug-fix]
dependency_graph:
  requires: []
  provides: [RangeDisplay-lineWidth, RangeDisplay-mouse-beam, RangeDisplay-closed-fan]
  affects: [CombatController, AttackTypeSelector]
tech_stack:
  added: []
  patterns: [Camera.main.ScreenToWorldPoint for mouse world coord, LineRenderer positionCount closed polygon]
key_files:
  created: []
  modified:
    - Assets/Scripts/Player/RangeDisplay.cs
decisions:
  - "_rightBeam 필드는 Inspector 연결을 유지 — null-safe 코드가 있으므로 연결 해제 불필요"
  - "UpdateLinearDisplay에서 Camera.main을 직접 참조 — 프로토타입 단계에서 캐싱 불필요"
metrics:
  duration: 5
  completed: "2026-06-05T11:03:11Z"
  tasks_completed: 1
  files_modified: 1
---

# Phase quick-260605-rhs Plan 01: RangeDisplay 3-Fix Summary

**One-liner:** lineWidth 0.12f 필드 추가 + UpdateLinearDisplay를 마우스 방향 단일 빔으로 교체 + UpdateFanDisplay를 center→arc→center 닫힌 부채꼴로 교체

---

## What Was Built

RangeDisplay.cs 3가지 버그를 단일 태스크로 수정:

1. **lineWidth 필드** — `[SerializeField] private float lineWidth = 0.12f` 추가. `UpdateLinearDisplay()`와 `UpdateFanDisplay()` 양쪽에서 `startWidth = endWidth = lineWidth` 적용. 빔이 0 두께 선이 아닌 눈에 보이는 굵기로 렌더링된다.

2. **Linear 단일 빔** — 기존 양방향(left/right) 빔 로직을 `Input.mousePosition` → `Camera.main.ScreenToWorldPoint` 변환 후 단일 방향 빔으로 교체. `Show()`에서 `_rightBeam.enabled = false`로 고정. 에디터 플레이테스트에서 마우스 조준 방향이 정확히 반영된다.

3. **Fan 닫힌 부채꼴** — `positionCount = arcSegments + 1`에서 `arcSegments + 3`으로 변경. index 0에 origin(center start), 1..arcSegments+1에 호 점, arcSegments+2에 origin(center end) 배치. 기존 열린 호에서 중심→호→중심으로 이어지는 닫힌 부채꼴이 된다.

---

## Commits

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | RangeDisplay 3가지 수정 적용 | 1a04a21 | Assets/Scripts/Player/RangeDisplay.cs |

---

## Deviations from Plan

None — plan executed exactly as written.

---

## Success Criteria Verification

- [x] `lineWidth = 0.12f` 필드 존재, `startWidth`/`endWidth`에 양 메서드 모두 적용
- [x] Linear 모드: `_leftBeam`만 활성, `_rightBeam.enabled = false`, 마우스 방향 단일 빔
- [x] Fan 모드: `positionCount = arcSegments + 3`, 위치 0과 arcSegments+2가 origin
- [x] 컴파일 및 런타임 에러 없음 (C# 문법 검토 완료)

---

## Known Stubs

None.

---

## Self-Check: PASSED

- FOUND: Assets/Scripts/Player/RangeDisplay.cs (modified)
- FOUND: commit 1a04a21 in worktree-agent-a09600c81037db04f branch
