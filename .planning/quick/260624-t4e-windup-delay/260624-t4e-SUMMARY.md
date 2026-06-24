---
phase: quick-260624-t4e
plan: 01
subsystem: enemy
tags: [enemy, melee, ranged, hitbox, animation, windup, timing]
dependency_graph:
  requires: []
  provides: [attackWindupDelay-MeleeEnemy, attackWindupDelay-RangedEnemy]
  affects: [MeleeEnemy, RangedEnemy]
tech_stack:
  added: []
  patterns: [WaitForSecondsRealtime for timeScale-immune windup delay]
key_files:
  modified:
    - Assets/Scripts/Enemy/MeleeEnemy.cs
    - Assets/Scripts/Enemy/RangedEnemy.cs
decisions:
  - "WaitForSecondsRealtime used (not WaitForSeconds) — timeScale-immune, consistent with existing coroutine pattern"
  - "IsAlive re-checked after windup delay — player can kill enemy mid-windup via dash"
  - "Default 0.1f — minimal delay that separates animation trigger from damage frame"
metrics:
  duration: ~5min
  completed: "2026-06-24T12:14:10Z"
  tasks: 2
  files: 2
---

# Phase quick-260624-t4e Plan 01: Attack Windup Delay Summary

## One-liner

`attackWindupDelay` SerializeField inserted into both enemy coroutines — hitbox/projectile now fire `0.1f` seconds after animation trigger, Inspector-adjustable.

## What Was Built

공격 애니메이션 트리거와 실제 피해 판정 사이의 타이밍 어긋남을 수정했다. 두 적 클래스 모두 동일한 패턴으로:

1. **MeleeEnemy** — `TelegraphAndAttack()`: `SetTrigger("isAttacking")` 직후 `WaitForSecondsRealtime(attackWindupDelay)` 삽입, 이후 `IsAlive` 재체크, 그 다음 `_meleeHitbox.enabled = true`
2. **RangedEnemy** — `TelegraphAndFire()`: `SetTrigger("isAttacking")` 직후 `WaitForSecondsRealtime(attackWindupDelay)` 삽입, 이후 `IsAlive` 재체크, 그 다음 `FireProjectile()`

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | MeleeEnemy attackWindupDelay 필드 + 히트박스 지연 | f06cda7 | Assets/Scripts/Enemy/MeleeEnemy.cs |
| 2 | RangedEnemy attackWindupDelay 필드 + 발사 지연 | 454405e | Assets/Scripts/Enemy/RangedEnemy.cs |

## Changes Made

### MeleeEnemy.cs
- Line 19: `[SerializeField] private float attackWindupDelay = 0.1f;` 추가 (hitboxActiveDuration 바로 아래)
- Line 185-188: `TelegraphAndAttack()` 코루틴 내 `SetTrigger` 이후 windup delay + IsAlive 가드 삽입

### RangedEnemy.cs
- Line 22: `[SerializeField] private float attackWindupDelay = 0.1f;` 추가 (firePoint 바로 아래)
- Line 223-228: `TelegraphAndFire()` 코루틴 내 `SetTrigger` 이후 windup delay + IsAlive 가드 삽입

## Deviations from Plan

None — plan executed exactly as written.

## Verification

Unity Editor에서 확인해야 할 사항:
1. Inspector에 "Attack Windup Delay" 필드 노출 — 기본값 0.1
2. MeleeEnemy: 공격 애니메이션 시작 후 약 0.1s 뒤 히트박스 활성화
3. RangedEnemy: 발사 애니메이션 시작 후 약 0.1s 뒤 발사체 생성
4. 값 변경(예: 0.3f) 즉시 반영
5. windup 중 플레이어 대시 처치 시 hitbox/발사체 생성 없음

## Known Stubs

None.

## Self-Check: PASSED

- Assets/Scripts/Enemy/MeleeEnemy.cs: modified with attackWindupDelay
- Assets/Scripts/Enemy/RangedEnemy.cs: modified with attackWindupDelay
- Commit f06cda7: MeleeEnemy task
- Commit 454405e: RangedEnemy task
