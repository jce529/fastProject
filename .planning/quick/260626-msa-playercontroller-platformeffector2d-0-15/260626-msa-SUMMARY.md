---
quick_id: 260626-msa
description: 아래 + 점프 드롭스루 구현
date: 2026-06-26
status: complete
---

# Quick Task 260626-msa: Summary

## What Changed

**`Assets/Scripts/Player/PlayerController.cs`**

- `using System.Collections;` 추가 (IEnumerator 사용)
- 필드 추가: `dropThroughDuration = 0.15f`, `_playerCollider`, `_isDropping`, `_dropBuffer[8]`
- `Awake()`: `_playerCollider = GetComponent<Collider2D>()` 캐싱
- `OnJumpPerformed`: 사다리 분기 직후, 수직 입력 < -0.5 && 지면 && !_isDropping 조건으로 `DropThrough()` 코루틴 실행
- `DropThrough()` 코루틴 신규:
  - `OverlapCircleNonAlloc`으로 발 아래 groundLayer 콜라이더 수집
  - `PlatformEffector2D` 보유 콜라이더만 `Physics2D.IgnoreCollision(true)`
  - `_rb.linearVelocity.y = -2f`로 즉시 낙하 시작
  - `WaitForSecondsRealtime(0.15f)` 대기 (슬로모 중에도 실제 0.15초)
  - 충돌 복구 `Physics2D.IgnoreCollision(false)`

## Behavior

- **아래 + 점프**: one-way 플랫폼(PlatformEffector2D) 통과
- **솔리드 바닥**: 영향 없음 (PlatformEffector2D 없으면 무시)
- **슬로모 중**: 0.15초는 실제 시간 기준 (`WaitForSecondsRealtime`)
- **이중 실행**: `_isDropping` 플래그로 방지
