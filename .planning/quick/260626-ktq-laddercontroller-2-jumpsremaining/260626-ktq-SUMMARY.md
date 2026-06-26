# Quick Task 260626-ktq: SUMMARY

**Completed:** 2026-06-26
**Status:** Done

## Changes Made

### Assets/Scripts/Player/LadderController.cs

- `_jumpExited` bool 필드 추가 — 점프 이탈 직후 재진입 차단용
- `ExitLadder(bool fromJump = false)` 시그니처 변경 — fromJump=true이면 `_jumpExited = true` 설정
- `FixedUpdate` EnterLadder 조건에 `!_jumpExited` 추가 — 위쪽 누른 채 점프해도 재진입 불가
- `OnTriggerExit2D`에서 트리거 완전 이탈 시 `_jumpExited = false` 리셋

### Assets/Scripts/Player/PlayerController.cs

- `OnJumpPerformed` 사다리 분기에서 `ExitLadder(fromJump: true)` 호출로 변경
- `_jumpsRemaining = maxJumps - 1` 추가 — 사다리 점프 후 공중점프 1회 보장

## Bugs Fixed

1. **재진입 버그:** 위쪽을 누른 채 사다리 점프 시 즉시 사다리 모드 재진입하던 문제 수정
2. **점프 횟수 버그:** 사다리 진입 전 더블점프 소진 시 사다리 점프 후 공중점프 불가하던 문제 수정
