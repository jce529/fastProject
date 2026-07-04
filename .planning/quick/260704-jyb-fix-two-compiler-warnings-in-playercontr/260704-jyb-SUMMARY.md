---
phase: quick
plan: 260704-jyb
subsystem: player
tags: [compiler-warning, physics2d, cleanup]
key-files:
  modified:
    - Assets/Scripts/Player/PlayerController.cs
decisions:
  - "ContactFilter2D overload of Physics2D.OverlapCircle replaces deprecated OverlapCircleNonAlloc — same pre-allocated _dropBuffer, zero extra heap allocation"
  - "_jumpHeld removed entirely: field was assigned in three places but never read"
metrics:
  duration: "< 5 min"
  completed: "2026-07-04"
  tasks: 1
  files: 1
---

# Quick 260704-jyb: Fix Two Compiler Warnings in PlayerController Summary

Removed unused `_jumpHeld` bool (CS0414) and replaced deprecated `Physics2D.OverlapCircleNonAlloc` with the `ContactFilter2D` overload of `Physics2D.OverlapCircle` (CS0619) — zero behaviour change, console now clean.

---

## Changes Made

### Task 1: Remove _jumpHeld and replace OverlapCircleNonAlloc

**Files:** `Assets/Scripts/Player/PlayerController.cs`  
**Commit:** `dd59e5a`

**Edit A — CS0414 (_jumpHeld unused field)**

Removed:
- Field declaration: `private bool _jumpHeld;`
- Assignment in ladder-jump branch (`OnJumpPerformed`): `_jumpHeld = true;`
- Assignment in normal-jump branch (`OnJumpPerformed`): `_jumpHeld = true;`
- Assignment in `OnJumpCanceled`: `_jumpHeld = false;`

The field had no reads anywhere in the file; all four sites were pure write-only dead code.

**Edit B — CS0619 (OverlapCircleNonAlloc deprecated)**

In `DropThrough` coroutine, replaced:
```csharp
int count = Physics2D.OverlapCircleNonAlloc(origin, groundCheckRadius + 0.05f, _dropBuffer, groundLayer);
```
with:
```csharp
var dropFilter = new ContactFilter2D();
dropFilter.SetLayerMask(groundLayer);
dropFilter.useTriggers = false;
int count = Physics2D.OverlapCircle(origin, groundCheckRadius + 0.05f, dropFilter, _dropBuffer);
```

`ContactFilter2D` is a struct allocated on the stack inside the coroutine frame. The pre-allocated `_dropBuffer` array is reused identically to before. The ignore/restore loop and `WaitForSecondsRealtime` are untouched.

`CheckGround` was not changed — it already uses the non-deprecated single-result overload `Physics2D.OverlapCircle(origin, radius, layerMask)`.

---

## Deviations from Plan

None — plan executed exactly as written.

---

## State Updates

- STATE.md Technical Constraints updated: `Physics2D.OverlapCircleNonAlloc()` entry replaced with `Physics2D.OverlapCircle(ContactFilter2D, results[])` to prevent future CS0619 recurrence.
- STATE.md Quick Tasks Completed table: row added for 260704-jyb.

---

## Self-Check: PASSED

- `Assets/Scripts/Player/PlayerController.cs` — modified and committed at `dd59e5a`
- `_jumpHeld` field and all three assignment sites removed; no reads existed
- `OverlapCircleNonAlloc` replaced with `ContactFilter2D` overload; `_dropBuffer` still reused
- `CheckGround` (non-deprecated overload) untouched
