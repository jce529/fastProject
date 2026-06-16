---
quick_id: 260617-0el
status: completed
commit: bd63875
date: 2026-06-17
---

# Quick Task 260617-0el: RangedEnemy detectionRadius/aimLineLength 분리

## What Changed

`Assets/Scripts/Enemy/RangedEnemy.cs` — `UpdateChase()` 메서드 교체 (13줄 추가, 6줄 제거)

### Before
Chase 진입 즉시 `TelegraphAndFire()` 코루틴 시작 → detectionRadius 안에 들어오면 바로 공격 텔레그래프 발동

### After
세 단계 로직:
1. `_playerTransform` 확보 (없으면 Idle 복귀)
2. `detectionRadius` 이탈 체크 → 이탈 시 Idle 복귀
3. `aimLineLength` 진입 + `_telegraphCoroutine == null` 시에만 텔레그래프 시작

## Verification

- [x] `IsPlayerInRange(detectionRadius)` 이탈 체크 존재
- [x] `IsPlayerInRange(aimLineLength)` 진입 조건부 공격 트리거 존재
- [x] `_telegraphCoroutine == null` 중복 실행 방지 가드 존재
- [x] `TelegraphAndFire()`, `FireProjectile()`, `IsPlayerInRange()` 등 다른 메서드 변경 없음
- [ ] 플레이테스트 (Inspector: aimLineLength=5f, detectionRadius=12f 설정 후 동작 확인) — Unity Editor 필요

## Inspector Setup Required

```
detectionRadius = 12f  (감지 — 이 범위 안에 있으면 Chase 유지)
aimLineLength   = 5f   (공격 트리거 — 이 범위 안에 들어와야 텔레그래프 시작)
```
