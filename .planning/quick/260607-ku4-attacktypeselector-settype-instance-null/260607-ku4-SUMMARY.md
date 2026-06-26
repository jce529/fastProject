---
phase: quick-260607-ku4
plan: 01
subsystem: UI/Combat
tags: [bugfix, AttackTypeSelector, AttackType, zone-trigger]
dependency_graph:
  requires: []
  provides: [AttackTypeSelector.Selected 항상 최신 zone 타입 반영]
  affects: [AttackTypeZone, AttackTypeDebugOverlay, CombatController]
tech_stack:
  added: []
  patterns: [null-conditional operator (?.) for optional UI refresh]
key_files:
  modified:
    - Assets/Scripts/UI/AttackTypeSelector.cs
decisions:
  - "_instance == null 가드를 SetType 밖으로 이동하지 않고, null-conditional 연산자로 RefreshHighlights 호출만 조건화 — 최소 변경 원칙"
metrics:
  duration: "< 5min"
  completed: "2026-06-07"
  tasks_completed: 1
  files_modified: 1
---

# Quick Task 260607-ku4: AttackTypeSelector SetType _instance null 버그 수정 Summary

**One-liner:** SetType의 `if (_instance == null) return;` 가드 제거 — Selected를 무조건 갱신하고 RefreshHighlights는 null-conditional로 선택적 호출

---

## What Was Done

`AttackTypeSelector.SetType`에 `if (_instance == null) return;` 가드가 있어, AttackTypeSelector MonoBehaviour가 씬에 없을 때 `Selected` 정적 필드가 전혀 갱신되지 않는 버그를 수정했다.

`AttackTypeZone.OnTriggerEnter2D`가 `SetType`을 호출하더라도 UI 오브젝트가 씬에 없으면 즉시 반환되어 `Selected`가 변경되지 않았고, 이로 인해 `AttackTypeDebugOverlay`를 포함한 모든 구독자가 잘못된 타입을 읽었다.

### 변경 내용

**Assets/Scripts/UI/AttackTypeSelector.cs** (1 삽입, 2 삭제)

BEFORE:
```csharp
public static void SetType(AttackType type)
{
    if (_instance == null) return;   // 이 줄이 Selected 갱신을 막음
    if (Selected == type) return;
    Selected = type;
    _instance.RefreshHighlights();
}
```

AFTER:
```csharp
public static void SetType(AttackType type)
{
    if (Selected == type) return;
    Selected = type;
    _instance?.RefreshHighlights();  // UI 없으면 조용히 스킵
}
```

---

## Tasks

| # | Name | Status | Commit |
|---|------|--------|--------|
| 1 | Fix SetType null-instance guard | Done | fb1e0b6 |

---

## Deviations from Plan

None - plan executed exactly as written.

---

## Verification

- `SetType` 내부에 `if (_instance == null) return;` 없음 (grep 확인)
- `_instance?.RefreshHighlights();` 정상 존재 (grep 확인)
- 런타임 검증: AttackTypeZone 진입 시 AttackTypeSelector GameObject 없이도 `AttackTypeDebugOverlay`가 "Fan"/"Linear" 즉시 표시 (에디터 플레이 필요)

---

## Self-Check: PASSED

- [FOUND] Assets/Scripts/UI/AttackTypeSelector.cs — 수정됨
- [FOUND] commit fb1e0b6 — fix(quick-260607-ku4): SetType null-instance guard 제거 — Selected 무조건 갱신
