---
quick_id: 260609-wvp
description: Enemy 오브젝트들이 죽으면 삭제하지 않고 Die 애니메이션 재생 후 죽은 상태로 씬에 남겨놓기
date: 2026-06-09
---

# Quick Task 260609-wvp Plan

## Goal
`OnDashHit()` 호출 시 `gameObject.SetActive(false)`로 오브젝트를 숨기는 대신, 물리를 동결하고 Die 애니메이션을 트리거한 뒤 씬에 시체로 남긴다.

## Tasks

### T1: MeleeEnemy.cs — OnDashHit() 수정
- **File:** `Assets/Scripts/Enemy/MeleeEnemy.cs`
- **Action:**
  1. `OnDashHit()`에서 `gameObject.SetActive(false)` 제거
  2. `_rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static;` 추가 (물리 동결)
  3. `GetComponent<Animator>()?.SetBool("isDead", true)` 추가 (Die 애니메이션 트리거)
- **Verify:** SetActive 호출이 없고, Static body type 설정과 isDead 설정이 있는지 확인

### T2: RangedEnemy.cs — OnDashHit() 수정
- **File:** `Assets/Scripts/Enemy/RangedEnemy.cs`
- **Action:**
  1. `OnDashHit()`에서 `gameObject.SetActive(false)` 제거
  2. `_rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static;` 추가
  3. `GetComponent<Animator>()?.SetBool("isDead", true)` 추가
- **Verify:** 동일

## Notes
- `IsAlive = false` 는 그대로 유지 — CombatController가 이를 보고 타겟 스킵
- `RigidbodyType2D.Static` 은 중력/이동 완전 동결 (corpse stays in place)
- Animator 없어도 null-conditional로 안전하게 처리됨
- Animator에 `isDead` Bool 파라미터 추가는 Unity 에디터에서 별도 작업 필요
