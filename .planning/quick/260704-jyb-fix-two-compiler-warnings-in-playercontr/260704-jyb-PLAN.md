---
phase: quick
plan: 260704-jyb
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/Player/PlayerController.cs
autonomous: true
requirements: []
must_haves:
  truths:
    - "Unity console shows zero CS0619 warnings about Physics2D.OverlapCircleNonAlloc"
    - "Unity console shows zero CS0414 warnings about unused _jumpHeld field"
    - "DropThrough behaviour is identical: finds one-way platform colliders, ignores them for dropThroughDuration, then re-enables them"
  artifacts:
    - path: "Assets/Scripts/Player/PlayerController.cs"
      provides: "Compiler-warning-free PlayerController"
      changes: "OverlapCircleNonAlloc -> OverlapCircle+ContactFilter2D; _jumpHeld removed"
  key_links:
    - from: "DropThrough coroutine"
      to: "Physics2D.OverlapCircle (ContactFilter2D overload)"
      via: "pre-allocated _dropBuffer array — no heap allocation"
      pattern: "Physics2D\\.OverlapCircle\\(origin.*_dropBuffer"
---

<objective>
Fix two compiler warnings in PlayerController.cs with surgical, zero-behaviour-change edits.

Purpose: Keep the console clean so real warnings are not hidden behind noise.
Output: PlayerController.cs with no CS0619 / CS0414 warnings; all runtime behaviour identical.
</objective>

<execution_context>
@C:/Users/MSI/Projeect_A.E/fastProject/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@C:/Users/MSI/Projeect_A.E/fastProject/.planning/STATE.md
@C:/Users/MSI/Projeect_A.E/fastProject/Assets/Scripts/Player/PlayerController.cs
</context>

<tasks>

<task type="auto">
  <name>Task 1: Remove _jumpHeld and replace OverlapCircleNonAlloc in PlayerController.cs</name>
  <files>Assets/Scripts/Player/PlayerController.cs</files>
  <action>
Two independent edits to the same file. Apply both before saving.

**Edit A — Remove unused _jumpHeld field (CS0414)**

Delete the field declaration on line 46:
  `private bool _jumpHeld;`

Delete all three assignment sites (they have no reads, so removal is safe):
  Line 109 (inside ladder-jump branch): `_jumpHeld = true;`
  Line 122 (inside normal-jump branch): `_jumpHeld = true;`
  Line 163 (inside OnJumpCanceled):     `_jumpHeld = false;`

Do NOT touch anything else in those methods — only remove the `_jumpHeld = ...;` lines.

**Edit B — Replace deprecated OverlapCircleNonAlloc (CS0619)**

In the DropThrough coroutine, replace:

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

ContactFilter2D is a struct (stack allocated) so creating it inline in the coroutine adds zero heap allocation. The pre-allocated `_dropBuffer` array is still reused exactly as before. The rest of DropThrough (the ignore/restore loop and WaitForSecondsRealtime) is unchanged.

Do NOT touch CheckGround — it already uses the non-deprecated single-return overload `Physics2D.OverlapCircle(origin, groundCheckRadius, groundLayer)` which is fine.
  </action>
  <verify>
    <automated>
Open Unity Editor and check the Console window. Both of the following must be absent:
  - Any warning containing "OverlapCircleNonAlloc" or "CS0619"
  - Any warning containing "_jumpHeld" or "CS0414"
If Unity is not already open, the file change will be picked up on next Editor focus (auto-refresh).
    </automated>
  </verify>
  <done>
PlayerController.cs compiles with zero warnings related to these two issues. DropThrough still correctly ignores one-way platforms for dropThroughDuration then restores them. Jump input still works normally (OnJumpPerformed / OnJumpCanceled).
  </done>
</task>

</tasks>

<verification>
After applying both edits, confirm:
1. No CS0619 ("obsolete") warning for Physics2D.OverlapCircleNonAlloc in Console
2. No CS0414 ("assigned but value never used") warning for _jumpHeld in Console
3. Down+Jump drop-through still works in Play Mode (player briefly passes through one-way platforms)
4. Normal jump and double-jump still function
</verification>

<success_criteria>
Unity Editor Console is free of both targeted warnings. All existing player movement (run, jump, double-jump, drop-through, ladder exit-jump) behaves identically to before.
</success_criteria>

<output>
No SUMMARY.md needed for quick tasks. State update: add row to STATE.md Quick Tasks Completed table.
</output>
