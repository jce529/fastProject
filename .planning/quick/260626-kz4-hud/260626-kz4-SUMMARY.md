---
phase: quick-260626-kz4-hud
plan: 01
subsystem: score-system
tags: [score, hud, combat, quick]
depends_on: []
provides: [ScoreManager]
affects: [CombatController, RoomExit, FloorSpawner, DeathScreenController, HUDController]
tech-stack:
  added: []
  patterns: [FloorManager static class pattern]
key-files:
  created:
    - Assets/Scripts/World/ScoreManager.cs
  modified:
    - Assets/Scripts/Player/CombatController.cs
    - Assets/Scripts/World/RoomExit.cs
    - Assets/Scripts/World/FloorSpawner.cs
    - Assets/Scripts/UI/DeathScreenController.cs
    - Assets/Scripts/UI/HUDController.cs
decisions:
  - "Time.unscaledTime 기반 타이머 — timeScale 변화(슬로우모션, 히트프리즈)에 면역"
  - "data-only static class 패턴 — FloorManager와 동일, MonoBehaviour 불필요"
  - "_scoreLabel?.SetText(\"{0}\", int) — TMP int overload 사용, 프레임당 할당 없음"
metrics:
  duration: "~8 minutes"
  completed: "2026-06-26T06:14:05Z"
  tasks_completed: 1
  tasks_total: 2
  files_created: 1
  files_modified: 5
---

# Quick Task 260626-kz4: Score System + HUD Display Summary

**One-liner:** data-only ScoreManager (kill +100, room-clear +300/150/50, unscaledTime timer) wired into 5 existing files with 1-line hooks each.

---

## Task 1: ScoreManager 생성 + 5개 파일 훅 연결

**Status:** COMPLETE
**Commit:** `00323bd`
**Files:** 6 (1 created, 5 modified)

### Changes Applied

| File | Change |
|------|--------|
| `ScoreManager.cs` (NEW) | data-only static class — Score, AddKillScore(), AddRoomClearBonus(), StartRoomTimer(), Reset() |
| `CombatController.cs` | `target.OnDashHit();` 직후 `ScoreManager.AddKillScore();` 1줄 추가 |
| `RoomExit.cs` | `AdvanceFloor()` 직전 `ScoreManager.AddRoomClearBonus();` 1줄 추가 |
| `FloorSpawner.cs` | `ActivateEnemies()` 루프 완료 후 `ScoreManager.StartRoomTimer();` 1줄 추가 |
| `DeathScreenController.cs` | `SceneManager.LoadScene()` 직전 `ScoreManager.Reset();` 1줄 추가 |
| `HUDController.cs` | `_scoreLabel` 직렬화 필드 추가 + Update()에 `_scoreLabel?.SetText("{0}", ScoreManager.Score);` 1줄 추가 |

---

## Task 2: Unity Editor에서 HUD Score 레이블 생성 및 연결

**Status:** AWAITING HUMAN ACTION (checkpoint:human-verify)
**Blocked by:** Unity Editor GUI 작업 — Claude 불가

### 필요한 Editor 작업

1. Unity Editor에서 SampleScene 열기
2. Hierarchy에서 HUD Canvas 하위 구조 확인 (FloorLabel, AttackTypeLabel 등)
3. Canvas 하위에 새 TextMeshPro - Text (UI) GameObject 추가 (이름: "ScoreLabel")
4. ScoreLabel RectTransform을 원하는 위치로 배치 (예: 우상단, FloorLabel 아래)
5. 기본 텍스트 "0", 폰트 크기는 기존 레이블과 동일하게 설정
6. HUDController Inspector에서 "Score Label" 슬롯에 ScoreLabel 드래그 연결
7. Play 모드에서 검증:
   - 적 처치 시 점수 +100
   - 방 출구 진입 시 보너스 점수 (+300 빠름 / +150 보통 / +50 느림)
   - 사망 후 재시작 시 점수 0으로 초기화

---

## Deviations from Plan

None — plan executed exactly as written.

---

## Known Stubs

- `HUDController._scoreLabel` is a serialized field with no scene object connected yet. Score value is computed correctly by ScoreManager at runtime, but will not render until Task 2 (Editor connection) is completed. This is intentional — Task 2 is a pending human-verify checkpoint.

---

## Self-Check: PASSED

- `Assets/Scripts/World/ScoreManager.cs` — FOUND (created in worktree commit 00323bd)
- Commit `00323bd` — FOUND in worktree git log
- 5 modified files all staged and committed in `00323bd`
