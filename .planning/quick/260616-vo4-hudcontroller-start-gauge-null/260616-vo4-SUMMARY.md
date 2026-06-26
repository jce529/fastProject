---
phase: quick
plan: 260616-vo4
subsystem: UI
tags: [diagnostics, hud, null-check, chrono-gauge]
dependency_graph:
  requires: []
  provides: [HUDController diagnostic logs]
  affects: [Assets/Scripts/UI/HUDController.cs]
tech_stack:
  added: []
  patterns: [Debug.Log diagnostic with DIAG marker, frameCount throttle]
key_files:
  modified:
    - Assets/Scripts/UI/HUDController.cs
decisions:
  - "30프레임 throttle 선택 — 60fps 기준 약 0.5초, 매 프레임 로그 없이 충분한 추적 가능"
  - "[DIAG] 주석 마커 부착 — 임시 진단 코드임을 명시하여 향후 검색/제거 용이"
metrics:
  duration: ~3min
  completed: 2026-06-16
  tasks: 1
  files: 1
---

# Quick 260616-vo4: HUDController null 체크 진단 로그 추가 Summary

**One-liner:** HUDController.Start()/Update()에 _gauge/UI ref null 여부를 콘솔에 즉시 출력하는 임시 DIAG 로그 삽입.

---

## What Was Done

GaugeController → ChronoGaugeController 리네임 후 씬 저장이 미흡할 경우 `_gauge` 직렬화 레퍼런스가 null로 깨질 가능성을 Play Mode 콘솔에서 확인하기 위한 임시 진단 코드를 `HUDController.cs`에 추가했다.

### Changes

**`Assets/Scripts/UI/HUDController.cs`**

- `Start()`: `FindFirstObjectByType` 폴백 직후에 `_gauge`, `_gaugeFill`, `_floorLabel`, `_attackTypeLabel` 4개 필드의 null 여부를 한 줄 로그로 출력. (`[HUD] Start: _gauge=OK or NULL, ...`)
- `Update()`: 기존 `_gauge != null` 가드 블록 내부를 `{ }` 블록으로 변환하고, `Time.frameCount % 30 == 0` 조건으로 30프레임마다 `_gauge.Value`와 `_gaugeFill.fillAmount`를 출력. (`[HUD] Update: _gauge.Value=X.XXX, fillAmount=X.XXX`)
- 모든 진단 로그에 `// [DIAG] 임시 진단 로그 — 원인 파악 후 제거` 주석 부착.
- 기존 로직(폴백, null 가드, _lastType dirty-check)은 일체 변경하지 않음.

---

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| T1   | 2d463da | fix(quick-260616-vo4): HUDController Start/Update에 null 체크 진단 로그 추가 |

---

## Deviations from Plan

None - plan executed exactly as written.

---

## How to Interpret Console Output

| Console 출력 | 의미 |
|---|---|
| `[HUD] Start: _gauge=OK, ...` | 직렬화 레퍼런스 정상 — 씬 저장 문제 없음 |
| `[HUD] Start: _gauge=NULL, ...` | 직렬화 레퍼런스 깨짐 — ChronoGaugeController Inspector 재연결 필요 |
| `[HUD] Update:` 로그 반복 출력 | _gauge 정상 연결, Value가 실시간 반영 |
| `[HUD] Update:` 로그 없음 | _gauge=NULL이므로 Update 경로 미진입 |

---

## Known Stubs

None — 진단 코드는 의도적으로 임시이며 `[DIAG]` 마커로 추적 가능.

---

## Self-Check: PASSED

- [x] `Assets/Scripts/UI/HUDController.cs` 수정 확인
- [x] DIAG 문자열 2개 이상 포함 확인 (grep count: 2)
- [x] Commit 2d463da 존재 확인
