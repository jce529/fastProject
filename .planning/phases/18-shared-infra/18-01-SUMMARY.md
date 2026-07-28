---
phase: 18-shared-infra
plan: 01
subsystem: player-combat
tags: [unity, csharp, interface-extraction, refactor]

# Dependency graph
requires: []
provides:
  - "IPlayerCombatModule 인터페이스 (FindTarget/Resolve/Whiff 계약)"
  - "CombatContext 순수 데이터 홀더 (모듈이 필요로 하는 공유 참조/튜너블/콜백)"
  - "OverclockModule — IPlayerCombatModule 최초 구현체, 기존 Overclock 로직 verbatim 이관"
  - "CombatController를 _activeModule/_ctx 경유 host로 재배선 (ExecuteDash/ExecuteWhiff/FindNearestEnemyInRange/IsInAttackShape/GetMouseWorldDirection/HitFreeze/SpawnHitSpark 직접 소유 제거)"
affects: [19-samurai, 20-deadeye, 22-max, 23-nova]

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "IPlayerCombatModule 뒤로 전투 로직을 모듈화 — CombatController는 입력 폴링/슬로우모션 lifecycle/게이지/_isBusy 락아웃만 host로 유지"

key-files:
  created:
    - Assets/Scripts/Player/Combat/IPlayerCombatModule.cs
    - Assets/Scripts/Player/Combat/CombatContext.cs
    - Assets/Scripts/Player/Combat/OverclockModule.cs
  modified:
    - Assets/Scripts/Player/CombatController.cs

key-decisions:
  - "Task 1/2 모두 verbatim move — 로직 변경 없이 이관 (18-CONTEXT.md D-04)"

requirements-completed: [INFRA-01]

# Metrics
completed: 2026-07-22
---

# Phase 18 Plan 01: IPlayerCombatModule 추출 & OverclockModule 이관 Summary

**CombatController에 하드코딩되어 있던 Overclock 전투 로직(타겟팅 + 대시처치/헛치기 판정)을 IPlayerCombatModule 인터페이스 뒤로 무손상 이관, CombatController는 _activeModule/_ctx를 통해서만 위임하는 host로 축소.**

## Performance

- **Commits:** 2 (93a19c9, 0a4748f), 2026-07-22 11:49~11:51 KST

## Accomplishments

- `IPlayerCombatModule`(FindTarget/Resolve/Whiff) + `CombatContext`(순수 데이터 홀더, WhiffLockout 포함) 계약 정의 — Task 1
- `OverclockModule`이 `FindNearestEnemyInRange`/`IsInAttackShape`/`GetMouseWorldDirection`→`FindTarget()`, `ExecuteDash`→`Resolve()`, `ExecuteWhiff`→`Whiff()`, `HitFreeze`/`SpawnHitSpark`를 private 헬퍼로 verbatim 이관 — Task 2
- `CombatController`는 `_activeModule`/`_ctx` 필드를 통해 4개 호출 지점(Update() FindTarget, DashOrWhiff() fallback FindTarget, Resolve, Whiff)에서만 위임, 기존 7개 private 메서드 완전 제거

## Verification (post-hoc, 2026-07-28 문서 정합화 세션)

이 SUMMARY는 Phase 18.1 작업 도중 커밋만 되고 문서화가 누락된 것을 뒤늦게 발견해 작성됨. 코드 레벨 acceptance_criteria는 현재 코드베이스 기준으로 전부 재확인됨:

| Criterion | Result |
|---|---|
| `IPlayerCombatModule`에 FindTarget/Resolve/Whiff 3개 멤버 | PASS (grep count 3) |
| `CombatContext`에 `SetAttackCooldown`/`WhiffLockout` 필드 | PASS |
| `OverclockModule : IPlayerCombatModule` | PASS |
| `OverclockModule.Whiff()`가 `ctx.WhiffLockout` 사용 | PASS |
| `CombatController.cs`에 구 private 메서드 잔존 0건 | PASS |
| `_activeModule.` 호출 4곳 | PASS |
| `new CombatContext` 1곳 (Awake) | PASS |

**Task 3 (checkpoint:human-verify) 상태:** 2026-07-28에 뒤늦게 실제 플레이테스트로 확인 완료 — 체크리스트 5개 항목(홀드 시 슬로우모션+범위표시 / 릴리즈 시 대시-원샷처치+히트스파크/쉐이크/힛프리즈 / 범위 밖 릴리즈 시 헛치기+긴 락아웃 / 게이지 소진 후에도 릴리즈 대시 가능 / 콘솔 에러 없음) 전부 통과 승인. DebugScene 재빌드 과정에서 별도 버그(보스 중복 스폰, `DebugSceneBuilder.cs` — Room_BossFsmTest.prefab에 이미 nested된 BossEnemy를 모르고 한 번 더 Instantiate하던 문제) 발견 및 수정.

## Files Created/Modified

- `Assets/Scripts/Player/Combat/IPlayerCombatModule.cs` (신규)
- `Assets/Scripts/Player/Combat/CombatContext.cs` (신규)
- `Assets/Scripts/Player/Combat/OverclockModule.cs` (신규)
- `Assets/Scripts/Player/CombatController.cs` (수정 — host로 축소)

## User Setup Required

None.

---
*Phase: 18-shared-infra*
*Completed (code): 2026-07-22*
*Documented retroactively: 2026-07-28*
