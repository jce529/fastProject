---
phase: 11-timer-difficulty
verified: 2026-07-07T16:30:00Z
status: passed
score: 5/5 must-haves verified
---

# Phase 11: 타이머 & 난이도 Verification Report

**Phase Goal:** 층마다 HUD에 카운트다운이 표시되고 시간 초과 시 게임오버가 발생하며, 층이 올라갈수록 몬스터 수가 증가하고, 남은 시간에 비례한 점수가 누적된다
**Verified:** 2026-07-07T16:30:00Z
**Status:** passed
**Re-verification:** No — initial verification

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | HUD 타이머가 60초에서 즉시 카운트다운을 시작하고, 슬로우모션 중에도 실시간으로 감소한다 | VERIFIED | `FloorTimer.RemainingSeconds` computed from `Time.unscaledTime` (immune to `Time.timeScale`); `WorldGenerator.Start()` calls `FloorTimer.Reset()` (line 77); `HUDController.Update()` displays `Mathf.CeilToInt(FloorTimer.RemainingSeconds)` (line 35). Human-confirmed via playtest (11-04 Task 3). |
| 2 | 타이머가 0에 도달하는 순간 PlayerController.OnPlayerDeath가 발동해 기존 사망 화면이 표시된다 | VERIFIED | `FloorTimer.Tick()` (called unconditionally as first line of `WorldGenerator.Update()`, line 403 — before the chain-empty early-return at line 405) calls `PlayerController.TriggerDeath()` exactly once via `_expired` guard. `PlayerController.TriggerDeath()` invokes `OnPlayerDeath`, subscribed by `DeathScreenController.HandleDeath` and `PlayerDeathHandler.HandleDeath` (pre-existing, unmodified). Human-confirmed via playtest. |
| 3 | 3층에서 스폰되는 총 몬스터 수가 1층보다 눈에 띄게 많다 | VERIFIED | `WorldGenerator.GetEnemyCount(floor)` (lines 366-371) ports `FloorSpawner.GetEnemyCount` verbatim (floor<=5: melee Random(2,4)+ranged Random(0,2); floor<=10: melee 2+ranged Random(1,3); floor>10: melee 2+ranged Random(2,4)). Called at all 4 Room-instantiation points (`Start()` line 100, `SpawnNextPair()` line 145, `SpawnPrevPair()` line 180, standby room line 351 using `CurrentFloor+1`). User explicitly re-tested and confirmed floor 1 vs floor 3 enemy count difference ("1층과 3층 스폰된 적 수 확인해봤어 확실히 많아진것같아"). |
| 4 | EXIT 포탈 진입 시 남은 시간(초)×10점이 ScoreManager.Score에 누적되고 HUD에 실시간 반영된다 | VERIFIED | `WorldGenerator.FloorTransitionSequence()` calls `ScoreManager.AddTimeBonus(FloorTimer.RemainingSeconds)` (line 494) *before* `FloorTimer.Reset()` (line 526) — correct D-02b ordering preserves the pre-reset remaining time. `ScoreManager.AddTimeBonus` computes `Score += Mathf.RoundToInt(remainingSeconds) * TimeBonusPerSecond` (TimeBonusPerSecond=10). `HUDController.Update()` displays `ScoreManager.Score` via pre-existing `_scoreLabel` line (unmodified, confirmed present). Human-confirmed via playtest. |
| 5 | Editor scene wiring — HUD TimerLabel and WorldGenerator enemy prefabs connected in SampleScene | VERIFIED | `SampleScene.unity`: `_timerLabel: {fileID: 1083812912}` resolves to the `TimerLabel` GameObject's TextMeshProUGUI component (created by `HUDTimerLabelBuilder.cs`). `_meleeEnemyPrefab`/`_rangedEnemyPrefab` GUIDs (`9f7e98af...`/`6683746b...`) resolve to `Assets/Prefabs/Enemies/MeleeEnemy.prefab.meta`/`RangedEnemy.prefab.meta` respectively. |

**Score:** 5/5 truths verified

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Assets/Scripts/World/FloorTimer.cs` | Static class: Duration/RemainingSeconds/Reset()/Tick() | VERIFIED | All members present exactly as specified; `Tick()` uses `_expired` guard, calls `PlayerController.TriggerDeath()` once; `RemainingSeconds` uses `Time.unscaledTime` (no `Time.deltaTime`) |
| `Assets/Scripts/World/ScoreManager.cs` | `AddTimeBonus(float)` + `TimeBonusPerSecond` const | VERIFIED | Line 17: `TimeBonusPerSecond = 10`; lines 42-45: `AddTimeBonus` implements `Score += Mathf.RoundToInt(remainingSeconds) * TimeBonusPerSecond`. Pre-existing members (KillScore, FastClearBonus, etc.) untouched |
| `Assets/Scripts/World/WorldGenerator.cs` | GetEnemyCount + TrySpawnEnemies + FloorTimer/ScoreManager integration | VERIFIED | `GetEnemyCount(floor)` matches `FloorSpawner` original table verbatim; `TrySpawnEnemies` filters `EnemySpawner` markers by `Type`, spawns via `_meleeEnemyPrefab`/`_rangedEnemyPrefab`; called at all 4 room-instantiation points; `FloorTimer.Reset()`×2, `FloorTimer.Tick()`×1, `ScoreManager.AddTimeBonus()`×1 all present with correct ordering |
| `Assets/Scripts/World/EnemySpawner.cs` | `public EnemyType Type => _type;` getter | VERIFIED | Line 14, exact match |
| `Assets/Scripts/UI/HUDController.cs` | `_timerLabel` display + flicker | VERIFIED | `_timerLabel` field (line 13), display line 35, `TimerFlickerLoop()` coroutine (lines 53-78) with `Mathf.Lerp`-based variable interval, `WaitForSecondsRealtime` only (no non-realtime `WaitForSeconds`), `_scoreLabel` line unmodified (SCORE-02 regression-checked) |
| `Assets/Editor/HUDTimerLabelBuilder.cs` | Menu tool: TimerLabel creation + auto-wire | VERIFIED | `[MenuItem("Fast/Phase11/Add Timer Label To HUD")]`, `SerializedObject`-based `_timerLabel` connection, `MarkSceneDirty` present |
| `Assets/Scenes/SampleScene.unity` | TimerLabel + prefab wiring | VERIFIED | `_timerLabel` fileID resolves to real TimerLabel component; enemy prefab GUIDs resolve to correct prefab .meta files; both `WorldGenerator` instances in scene (main + prefab variant reference at line 4181-4182) carry the wiring |

### Key Link Verification

| From | To | Via | Status | Details |
|------|-----|-----|--------|---------|
| `FloorTimer.Tick()` | `PlayerController.TriggerDeath()` | direct static call, `_expired` guard, fires once | WIRED | `FloorTimer.cs:35` |
| `ScoreManager.AddTimeBonus()` | `ScoreManager.Score` | `Score += Mathf.RoundToInt(...) * TimeBonusPerSecond` | WIRED | `ScoreManager.cs:44` |
| `WorldGenerator.Start()/SpawnNextPair()/SpawnPrevPair()` | `TrySpawnEnemies(room, FloorManager.CurrentFloor)` | direct call after `TrySpawnExitPortal` | WIRED | lines 100, 145, 180 |
| `WorldGenerator.TrySpawnExitPortal()` | `TrySpawnEnemies(standbyRoom, CurrentFloor+1)` | direct call before `SetActive(false)` | WIRED | line 351 |
| `WorldGenerator.TrySpawnEnemies()` | `EnemySpawner.Spawn()/Activate()` | `GetComponentsInChildren<EnemySpawner>(true)` + `Type` filter | WIRED | lines 384-398 |
| `WorldGenerator.Update()` | `FloorTimer.Tick()` | first statement, before early-return guard | WIRED | line 403 (before line 405 guard) |
| `WorldGenerator.FloorTransitionSequence()` | `ScoreManager.AddTimeBonus(FloorTimer.RemainingSeconds)` → `FloorTimer.Reset()` | sequential calls, correct order preserved | WIRED | line 494 (AddTimeBonus) precedes line 526 (Reset) |
| `HUDController.Update()` | `FloorTimer.RemainingSeconds` | `_timerLabel?.SetText(...)` | WIRED | line 35 |
| `HUDController.TimerFlickerLoop()` | `_timerLabel.color` | coroutine loop, `WaitForSecondsRealtime(interval)` inversely proportional to remaining time | WIRED | lines 53-78 |
| `SampleScene HUDController` | `TimerLabel` component | `_timerLabel: {fileID: 1083812912}` | WIRED | confirmed fileID resolves to real component |
| `SampleScene WorldGenerator` | `MeleeEnemy.prefab`/`RangedEnemy.prefab` | Inspector GUID references | WIRED | GUIDs resolve to correct prefab .meta files |

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|-------------|-------------|--------|----------|
| TIMER-01 | 11-01, 11-02, 11-03, 11-04 | 층 진입 시 HUD에 남은 제한 시간이 카운트다운으로 표시된다 | SATISFIED | `FloorTimer` + `HUDController._timerLabel` + scene wiring, human-playtest confirmed |
| TIMER-02 | 11-01, 11-02, 11-04 | 제한 시간 초과 시 게임오버가 발생한다 | SATISFIED | `FloorTimer.Tick()` → `PlayerController.TriggerDeath()` → `DeathScreenController`/`PlayerDeathHandler`, human-playtest confirmed |
| DIFF-01 | 11-02, 11-04 | 층 번호가 올라갈수록 스포너에서 생성되는 몬스터 수가 증가한다 | SATISFIED | `GetEnemyCount`/`TrySpawnEnemies` ported and wired at all instantiation points; user explicitly re-confirmed floor 1 vs 3 difference |
| SCORE-01 | 11-01, 11-02, 11-04 | EXIT 포탈 도달 시 남은 제한 시간(초)에 비례한 점수가 누적된다 | SATISFIED | `ScoreManager.AddTimeBonus` + correct call ordering in `FloorTransitionSequence()`, human-playtest confirmed |
| SCORE-02 | 11-03, 11-04 | HUD에 현재 누적 점수가 실시간으로 표시된다 | SATISFIED | Pre-existing `_scoreLabel?.SetText(...)` line confirmed unmodified and present |

All 5 requirement IDs declared across the phase's 4 plans (TIMER-01, TIMER-02, DIFF-01, SCORE-01, SCORE-02) match REQUIREMENTS.md's Phase 11 traceability table exactly (lines 66-70, all marked "Complete"). No orphaned requirements found.

### Anti-Patterns Found

None. Full read of all 7 modified/created files (`FloorTimer.cs`, `ScoreManager.cs`, `EnemySpawner.cs`, `WorldGenerator.cs`, `HUDController.cs`, `HUDTimerLabelBuilder.cs`, `SampleScene.unity`) plus targeted grep for TODO/FIXME/placeholder/stub patterns returned zero matches. No empty-return stubs, no hardcoded static data flowing to render, no orphaned imports.

### Human Verification Required

None outstanding. All 5 success criteria (TIMER-01, TIMER-02, DIFF-01, SCORE-01, SCORE-02) were already confirmed via human playtest in Plan 11-04 Task 3, including a follow-up round where the user specifically re-tested and confirmed DIFF-01 (floor 1 vs floor 3 enemy count difference). This verification pass cross-checked those human-confirmed behaviors against the actual source code and scene file, confirming the code paths that produced the confirmed behavior are real (not stubbed) and correctly wired.

### Gaps Summary

No gaps found. All observable truths verified at the code level, all key links wired with correct ordering (critical ordering constraints — `AddTimeBonus` before `Reset`, `Tick()` before early-return guard — both confirmed correct in the actual file, not just in the plan's stated intent), all artifacts substantive (no stubs), and all 5 requirement IDs traced to REQUIREMENTS.md with "Complete" status. Editor/scene wiring (Plan 04) confirmed present in `SampleScene.unity` via fileID/GUID resolution — not merely claimed in SUMMARY.md.

---

*Verified: 2026-07-07T16:30:00Z*
*Verifier: Claude (gsd-verifier)*
