# Quick Task 260609-wvp Summary

**Task:** Enemy 오브젝트들이 죽으면 삭제하지 않고 Die 애니메이션 재생 후 죽은 상태로 씬에 남겨놓기
**Date:** 2026-06-09
**Status:** Complete

## Changes

### MeleeEnemy.cs — OnDashHit()
- `gameObject.SetActive(false)` 제거
- 물리 동결: `_rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static`
- Die 애니메이션 트리거: `GetComponent<Animator>()?.SetBool("isDead", true)`
- 기존에 없던 hitbox/icon 비활성화 코드도 OnDashHit에 추가 (이전엔 SetActive가 암묵적으로 처리)

### RangedEnemy.cs — OnDashHit()
- `gameObject.SetActive(false)` 제거
- 물리 동결: `_rb.linearVelocity = Vector2.zero; _rb.bodyType = RigidbodyType2D.Static`
- Die 애니메이션 트리거: `GetComponent<Animator>()?.SetBool("isDead", true)`

## Notes
- `IsAlive = false` 유지 — CombatController가 dead enemy 스킵하는 기존 로직 그대로 동작
- Animator 없어도 null-conditional 처리로 에러 없음
- **Unity 에디터 후속 작업 필요:** AnimatorController에 `isDead` Bool 파라미터 추가 + Dead 스테이트/트랜지션 연결 (RangedEnemyAnimator는 이미 트랜지션이 있으나 파라미터 누락 상태)
