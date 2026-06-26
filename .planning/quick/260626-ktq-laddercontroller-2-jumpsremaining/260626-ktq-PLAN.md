# Quick Task 260626-ktq: LadderController 버그 2개 수정

**Date:** 2026-06-26
**Status:** Ready for execution

## Description

LadderController 버그 2개 수정:
1. 위쪽 누른 채 점프 시 사다리 재진입 방지 — ExitLadder 후 트리거 이탈 전까지 재진입 차단 플래그 추가
2. 사다리 점프 후 jumpsRemaining 미처리 — PlayerController OnJumpPerformed에서 사다리 점프 시 jumpsRemaining = maxJumps - 1로 리셋

## Tasks

### Task 1: LadderController 재진입 방지 플래그 추가

**File:** `Assets/Scripts/Player/LadderController.cs`

**Bug:** ExitLadder() 호출 후 `_isClimbing = false`가 되지만 `_ladderOverlapCount > 0`이 유지되므로, 플레이어가 위를 누른 채로 점프하면 같은 FixedUpdate 사이클에서 즉시 EnterLadder()가 재호출됨.

**Fix:** `_jumpExited` 플래그 추가. 점프로 이탈 시 true, `OnTriggerExit2D`에서 완전히 벗어날 때 false로 해제.

**Action:**
- `_jumpExited` bool 필드 추가
- `ExitLadder(bool fromJump = false)` 파라미터 추가
- fromJump일 때 `_jumpExited = true`
- `FixedUpdate`의 EnterLadder 진입 조건에 `!_jumpExited` 추가
- `OnTriggerExit2D`에서 `_ladderOverlapCount == 0` 시 `_jumpExited = false` 리셋

**Verify:** 위쪽 누른 채 점프 시 사다리 재진입 없이 공중으로 이탈

### Task 2: PlayerController 사다리 점프 후 jumpsRemaining 리셋

**File:** `Assets/Scripts/Player/PlayerController.cs`

**Bug:** OnJumpPerformed의 사다리 점프 분기가 `_jumpsRemaining`을 건드리지 않아, 사다리 오르기 전에 더블점프를 소진한 경우 사다리 점프 후 공중점프 불가.

**Fix:** 사다리 점프 직후 `_jumpsRemaining = maxJumps - 1`로 설정 (점프 1회 소진 상태로 리셋, 공중점프 1회 보장).

**Action:**
- OnJumpPerformed의 `_onLadder` 분기에 `_jumpsRemaining = maxJumps - 1;` 추가 (jumpForce 설정 직후)

**Verify:** 사다리에서 점프 후 공중에서 더블점프 1회 사용 가능
