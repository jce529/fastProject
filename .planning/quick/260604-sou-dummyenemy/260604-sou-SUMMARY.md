---
phase: quick
plan: 260604-sou
subsystem: enemy
tags: [prefab, scale, dummyenemy]
key-files:
  modified:
    - Assets/Prefabs/DummyEnemy.prefab
decisions:
  - CapsuleCollider2D m_Size 미변경 — Unity는 Collider2D m_Size를 로컬 공간으로 저장하므로 Transform scale 2배 시 세계 공간 콜라이더도 자동으로 2배, 별도 수정 불필요
metrics:
  completed: "2026-06-04"
  tasks_completed: 1
  files_modified: 1
---

# Quick Task 260604-sou: DummyEnemy Scale 2배 Summary

**One-liner:** DummyEnemy.prefab Transform m_LocalScale을 (0.8, 1.2, 1)에서 (1.6, 2.4, 1)로 수정해 적의 시각적 존재감 2배 확대.

## Task Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | DummyEnemy Transform scale 2배 적용 | 3e831f3 | Assets/Prefabs/DummyEnemy.prefab |

## Change Detail

**File:** `Assets/Prefabs/DummyEnemy.prefab`

**Before:**
```yaml
m_LocalScale: {x: 0.8, y: 1.2, z: 1}
```

**After:**
```yaml
m_LocalScale: {x: 1.6, y: 2.4, z: 1}
```

**Unchanged (intentional):**
- `CapsuleCollider2D.m_Size: {x: 0.8, y: 1.2}` — 로컬 공간 기준값이므로 Transform scale 2배 시 세계 공간 콜라이더 크기도 자동으로 2배가 됨. 별도로 수정하면 콜라이더가 시각 크기의 4배가 되는 버그 발생.
- `SpriteRenderer.m_Size: {x: 0.8, y: 1.2}` — 스프라이트 draw size는 Transform scale에 의해 자동 반영됨.

## Deviations from Plan

None — plan executed exactly as written.

## Self-Check: PASSED

- Assets/Prefabs/DummyEnemy.prefab: m_LocalScale {x: 1.6, y: 2.4, z: 1} confirmed
- Commit 3e831f3 exists
- CapsuleCollider2D m_Size unchanged at {x: 0.8, y: 1.2}
