---
quick_id: 260626-msa
description: 아래 + 점프 드롭스루 구현
date: 2026-06-26
status: in_progress
---

# Quick Task 260626-msa: 아래 + 점프 드롭스루 구현

## Goal

`PlayerController.OnJumpPerformed`에서 수직 입력 < -0.5이고 지면에 서 있을 때,
위로 점프하는 대신 발 아래 PlatformEffector2D 콜라이더와의 충돌을 0.15초 무시 후 복구.

## Tasks

### T1 — PlayerController 드롭스루 로직 추가

**Files:** `Assets/Scripts/Player/PlayerController.cs`

**Action:**
1. `using System.Collections;` import 추가
2. 필드 추가: `[SerializeField] float _dropThroughDuration = 0.15f`, `bool _isDropping`, `Collider2D[] _dropBuffer = new Collider2D[8]`, `Collider2D _playerCollider`
3. `Awake()`에 `_playerCollider = GetComponent<Collider2D>()` 추가
4. `OnJumpPerformed`에서 사다리 처리 직후, 일반 점프 전에 드롭 분기 삽입:
   - 조건: `_moveAction.ReadValue<Vector2>().y < -0.5f && _isGrounded && !_isDropping`
   - 참이면 `StartCoroutine(DropThrough())` 후 return
5. `DropThrough()` 코루틴 구현:
   - `Physics2D.OverlapCircleNonAlloc`으로 발 아래 groundLayer 콜라이더 수집
   - `PlatformEffector2D` 보유 콜라이더에만 `Physics2D.IgnoreCollision(true)`
   - `_rb.linearVelocity.y = -2f` (중력 낙하 즉시 시작)
   - `WaitForSecondsRealtime(_dropThroughDuration)` 대기 (슬로모 면역)
   - 콜라이더 복구 `Physics2D.IgnoreCollision(false)`

**Verify:** 에디터에서 플레이 시 아래 + 점프로 one-way 플랫폼 통과, 위로 점프 시 정상 작동
