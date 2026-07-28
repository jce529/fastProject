---
phase: 18-shared-infra
plan: 02
subsystem: enemy-boss
tags: [unity, csharp, class-extraction, playerprefs, rename]

# Dependency graph
requires: []
provides:
  - "BossUnlockManager — PlayerPrefs 기반 보스 해금 영구 저장소 (Unlock/IsUnlocked만 노출, reset 없음 — DeathScreenController.RestartGame()과 구조적으로 격리)"
  - "BossEnemyBase — EnemyBase와 별개인 독립 abstract 형제 클래스, defeat-guard/사망시퀀스/스폰게이팅/하이라이트 공용 plumbing"
  - "BossEnemy.cs → FioraBoss.cs rename (git mv로 GUID cb839023c498e514cab6bb76ab11cde9 보존), F.I.O.R.A 전용 패턴 루프만 남김"
  - "BossEnemyPrefabBuilder.cs — AddComponent<FioraBoss>로 갱신"
affects: [19-samurai, 20-deadeye, 22-max, 23-nova]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "BossEnemyBase는 EnemyBase를 상속하지 않는 독립 형제 클래스 — IsAlive가 '빈틈 여부'로 오버로드되어 EnemyBase의 생존/사망 시맨틱과 구조적으로 다르기 때문"
    - "PlayerPrefs 기반 static class, in-memory 캐시 + 즉시 Save() — 프로젝트 최초의 디스크 영속 저장소"

key-files:
  created:
    - Assets/Scripts/Progression/BossUnlockManager.cs
    - Assets/Scripts/Enemy/Boss/BossEnemyBase.cs
  modified:
    - Assets/Scripts/Enemy/Boss/FioraBoss.cs (renamed from Assets/Scripts/Enemy/BossEnemy.cs)
    - Assets/Editor/BossEnemyPrefabBuilder.cs

key-decisions:
  - "BossUnlockManager는 reset/clear 메서드를 절대 노출하지 않음 — DeathScreenController.RestartGame() 리셋 스윕에서 구조적으로 격리 (Pitfall 6)"
  - "git mv로 rename하여 프리팹 GUID 참조(cb839023c498e514cab6bb76ab11cde9)를 삭제+재생성 대신 보존"
  - "boss id 상수 FioraBoss.BossId = \"Fiora\" (D-03)"

requirements-completed: [INFRA-03, UNLOCK-01]

# Metrics
completed: 2026-07-22
---

# Phase 18 Plan 02: BossUnlockManager & BossEnemyBase 추출 Summary

**BossEnemy.cs(Phase 15 유일 구현체)에서 보스 범용 plumbing을 BossEnemyBase로 추출하고 FioraBoss로 rename, 프로젝트 최초의 PlayerPrefs 기반 영구 저장소 BossUnlockManager를 신설.**

## Performance

- **Commits:** 2 (74b11aa, afd85e2), 2026-07-22 11:53~11:56 KST

## Accomplishments

- `BossUnlockManager`(PlayerPrefs 기반 static class, `Unlock`/`IsUnlocked`만 노출, reset 없음) 신설 — Task 1
- `BossEnemyBase`(abstract, `IEnemy`+`ISpawnGatable` 구현) 추출 — `_isDefeated` 가드, `Die()` 공용 시퀀스(rb 정지/콜라이더 비활성화/animator/EnemyDeathEffect/카메라 쉐이크/ScoreManager 보너스/BossUnlockManager.Unlock 호출), `SetSpawnGate`, `OnPlayerDied` 플러밍
- `BossEnemy.cs` → `Assets/Scripts/Enemy/Boss/FioraBoss.cs` git mv rename, GUID 보존 확인, `FioraBoss : BossEnemyBase`로 F.I.O.R.A 전용 `PatternLoop()`/`OnDashHit()`/`GetHighlightColor()`만 남김
- `BossEnemyPrefabBuilder.cs`의 `AddComponent<BossEnemy>()` → `AddComponent<FioraBoss>()` 갱신 — Task 2

## Verification (post-hoc, 2026-07-28 문서 정합화 세션)

이 SUMMARY는 커밋만 되고 문서화가 누락된 것을 뒤늦게 발견해 작성됨. 코드 레벨 acceptance_criteria는 현재 코드베이스 기준으로 전부 재확인됨:

| Criterion | Result |
|---|---|
| `BossUnlockManager` public static class, Unlock/IsUnlocked 2+ 매치 | PASS |
| `BossUnlockManager`에 reset/clear 미노출 | PASS (0 매치) |
| `DeathScreenController.cs`에서 `BossUnlockManager` 미참조 | PASS (0 매치) |
| `FioraBoss.cs.meta`에 GUID `cb839023c498e514cab6bb76ab11cde9` 보존 | PASS |
| 코드베이스에 `class BossEnemy` 잔존 0건 | PASS |
| `FioraBoss : BossEnemyBase` | PASS |
| `BossEnemyBase`가 `EnemyBase` 미상속 (독립 형제 클래스) | PASS |
| `BossEnemyPrefabBuilder.cs`가 `AddComponent<FioraBoss>` 사용, 구 타입 참조 0건 | PASS |
| `BossEnemyBase.cs`에 `BossUnlockManager.Unlock` 호출 | PASS |

**Task 3 (checkpoint:human-verify) 상태:** 기록 없음 — 4개 커밋(18-01 2개 + 18-02 2개)이 7분 이내(11:49~11:56)에 연속 커밋되어, 계획서가 요구하는 실제 Unity Play 플레이테스트(7회 피격 처치/패턴 리셋/사망 연출/PlayerPrefs 영속성 재시작 확인 체크리스트 6개 항목)가 수행되었다는 근거가 없다. **주의**: 18.1-VERIFICATION.md가 이후(2026-07-24) 별도로 발견한 바, `DebugScene.unity` 작업트리 상태에 보스 인스턴스가 없거나 stale — 이 Task 3의 "7회 피격 처치" 플레이테스트가 실제로 수행됐다면 그 이후 씬 편집 과정에서 보스가 제거된 것으로 추정되나 확인 불가. **Phase 19(SAMURAI) 착수 전 이 Task 3 체크리스트의 실제 플레이테스트를 권장.**

## Files Created/Modified

- `Assets/Scripts/Progression/BossUnlockManager.cs` (신규)
- `Assets/Scripts/Enemy/Boss/BossEnemyBase.cs` (신규)
- `Assets/Scripts/Enemy/Boss/FioraBoss.cs` (rename from `Assets/Scripts/Enemy/BossEnemy.cs`, GUID 보존)
- `Assets/Editor/BossEnemyPrefabBuilder.cs` (수정)

## User Setup Required

None.

---
*Phase: 18-shared-infra*
*Completed (code): 2026-07-22*
*Documented retroactively: 2026-07-28*
