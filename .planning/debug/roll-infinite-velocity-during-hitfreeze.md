---
status: awaiting_human_verify
trigger: "roll-infinite-velocity-during-hitfreeze"
created: 2026-07-09T00:00:00Z
updated: 2026-07-09T00:00:00Z
---

## Current Focus

hypothesis: CONFIRMED. RollController.RollCoroutine() computed `rollSpeed * (1f / Time.timeScale)` every `yield return null` frame; during CombatController's HitFreeze coroutine, Time.timeScale is set to 0f for 75ms real-time, and RollController's Update-cadence coroutine is NOT gated by fixedDeltaTime/timeScale the way FixedUpdate-driven PlayerController is. Division by 0 -> Infinity -> assigned directly to Rigidbody2D.linearVelocity -> Box2D corruption -> console error + freeze.
test: Fix applied — guarded the division+assignment behind `if (Time.timeScale > 0f)`. Verified via manual code trace (dotnet build not usable standalone for this Unity project — generated csproj requires Unity Editor's own project-generation/asset-resolution step, unrelated to this change).
expecting: Roll during a HitFreeze/timeScale=0 window now holds last velocity for that frame instead of writing Infinity; roll resumes normal compensated velocity once timeScale restores to 1.
next_action: Awaiting human playtest confirmation (dash-kill + immediate roll, portal-entry + immediate roll, repeated several times) before archiving.

## Symptoms

expected: Roll (Shift/Sprint action, RollController.cs) always moves the player at a normal, finite lateral speed and never breaks physics.
actual: Occasionally, rolling immediately after killing an enemy (dash-kill) or immediately after a portal/floor transition causes a console error and the game freezes.
errors: User confirms a console error appears and the game freezes (exact text not captured; consistent with Unity/Box2D "Invalid Rigidbody2D linearVelocity... (Infinity/NaN)" assertion when velocity is corrupted).
reproduction: (1) Kill an enemy with dash attack, then immediately press Roll. (2) Enter a portal (floor transition) and immediately press Roll. Both witnessed during actual play.
started: Discovered during gameplay testing, not tied to a specific recent commit.

## Eliminated

(none — hypothesis confirmed on first pass)

## Evidence

- timestamp: investigation start
  checked: Assets/Scripts/Player/RollController.cs lines 62-73
  found: `while (elapsed < rollDuration) { float compensated = rollSpeed * (1f / Time.timeScale); _rb.linearVelocity = new Vector2(dir * compensated, _rb.linearVelocity.y); elapsed += Time.unscaledDeltaTime; yield return null; }` — uses `yield return null` (per-rendered-frame), not gated by any timeScale/fixedDeltaTime check. `1f / Time.timeScale` evaluates to `Infinity` in C# (float division, no exception) when `Time.timeScale == 0f`. Assigned directly to `_rb.linearVelocity`.
  implication: Confirms deterministic divide-by-zero -> Infinity -> Rigidbody2D velocity corruption. Matches preliminary hypothesis exactly.

- timestamp: investigation continued
  checked: Assets/Scripts/Player/CombatController.cs lines 306-338 (ExecuteDash tail, HitFreeze coroutine)
  found: On every successful dash-kill, `ExecuteDash()` calls `target.OnDashHit()` then `yield return StartCoroutine(HitFreeze(hitFreezeDuration))` (hitFreezeDuration = 0.075f). `HitFreeze()` sets `Time.timeScale = 0f; Time.fixedDeltaTime = 0f;` then `yield return new WaitForSecondsRealtime(realSeconds);` then restores both to 1f/0.02f.
  implication: Confirms a real, deterministic 75ms real-time window after every kill where Time.timeScale == 0f. If RollController's coroutine's `yield return null` loop executes during this window, it divides by zero.

- timestamp: investigation continued
  checked: Assets/Scripts/Player/PlayerController.cs lines 167-192 (FixedUpdate/ApplyMovement)
  found: `ApplyMovement()` has the IDENTICAL pattern `float compensatedMax = moveSpeed * (1f / Time.timeScale);` but is only called from `FixedUpdate()`. Since `HitFreeze()` also zeroes `Time.fixedDeltaTime`, Unity's physics step stops firing FixedUpdate during the freeze window, so PlayerController never evaluates this division while timeScale is actually 0.
  implication: Confirms the asymmetry explaining why only Roll (Update-cadence coroutine) exhibits the bug and not normal movement (FixedUpdate-cadence). This is the key differentiator between the two systems using the same "compensation" pattern.

- timestamp: investigation continued
  checked: Assets/Scripts/World/FloorSpawner.cs lines 63-127 (AdvanceFloor / FloorTransitionSequence), Assets/Scripts/World/FloorTransitionEffect.cs (PlayEntry/PlayExit/ScaleTransform)
  found: FloorTransitionEffect uses only `Time.unscaledDeltaTime` throughout (explicitly documented as HitFreeze/slow-mo immune) — it does NOT independently create any Time.timeScale == 0 window. FloorSpawner.FloorTransitionSequence Step 1 calls `_combatController?.ForceExitCombatState()` then `_player.LockInput()`. `ForceExitCombatState()` -> `ExitSlowMotion()` + `ExitAttackPending()`; `ExitSlowMotion()` has a guard `if (!_isSlowMo) return;` — it only undoes EnterSlowMotion's timeScale, and does nothing if a HitFreeze coroutine (a completely separate coroutine, not gated by _isSlowMo) is still actively counting down Time.timeScale = 0f from the kill that cleared the room.
  implication: The "portal entry" repro case is NOT an independent timeScale-zeroing bug in the transition system. It is the SAME HitFreeze race: killing the last enemy in a room (very common right at the portal, since portals typically activate on room-clear) starts a 75ms real-time HitFreeze window; if the player reaches/triggers the portal and presses Roll within that ~75ms window, Time.timeScale is still 0 and FloorSpawner's transition lock does nothing to prevent or restore it.

- timestamp: investigation continued
  checked: Assets/Scripts/Player/RollController.cs Update() vs Assets/Scripts/Player/CombatController.cs:110 (`if (_player != null && _player.InputLocked) return;`) vs Assets/Scripts/Player/LadderController.cs:53 (`if (_ladderOverlapCount == 0 || _player.InputLocked) return;`)
  found: RollController.Update() checks only `_cooldownRemaining` and `_isRolling` — it has NO check against `_player.InputLocked`, unlike CombatController and LadderController which both explicitly gate on it.
  implication: Secondary contributing factor for the portal case specifically — even during FloorSpawner's "input locked" window (Step 1-6 of FloorTransitionSequence), Roll can still be triggered by the player, whereas normal movement/jump and combat cannot. This widens the window in which a lingering HitFreeze timeScale=0 can coincide with a Roll press right at portal entry. (Out of scope for this surgical fix per fix_guidance — RollController-only, HitFreeze/lock timing not to be altered — but noted for future consideration.)

## Resolution

root_cause: "RollController.RollCoroutine() divides by Time.timeScale every Update-cadence frame (`rollSpeed * (1f / Time.timeScale)`) to compensate roll speed for slow-motion. During CombatController's HitFreeze coroutine (fired on every dash-kill, 75ms real-time), Time.timeScale is deliberately set to 0f. Unlike PlayerController.ApplyMovement (which runs in FixedUpdate and is naturally skipped because HitFreeze also zeroes Time.fixedDeltaTime), RollController's coroutine uses `yield return null` and keeps executing every rendered frame regardless of timeScale/fixedDeltaTime. When timeScale is exactly 0 during this window, `1f / Time.timeScale` evaluates to Infinity (C# float division does not throw), which is assigned directly to Rigidbody2D.linearVelocity, corrupting Box2D's physics step and freezing the game. The portal-entry repro case is the same race — the kill that clears the room can leave HitFreeze's timeScale=0 window open right as the player reaches the portal; FloorSpawner's ForceExitCombatState() does not cancel an in-flight HitFreeze coroutine (it only clears slow-motion state, guarded by _isSlowMo), so the window persists independent of the floor transition lock."
fix: "Guard RollController.RollCoroutine()'s per-frame velocity write: only assign compensated Rigidbody2D.linearVelocity when Time.timeScale > 0f. When Time.timeScale <= 0f (mid-HitFreeze), skip the assignment for that frame (hold last velocity) — do not divide by zero. Roll duration accumulation continues to use Time.unscaledDeltaTime unchanged, so roll timing itself is unaffected; only the velocity write is skipped during the freeze micro-window, and resumes normally once HitFreeze restores timeScale to 1."
verification: "Self-verified via code trace: the added `if (Time.timeScale > 0f)` guard wraps both the division (`1f / Time.timeScale`) and the `_rb.linearVelocity` assignment, so no frame can ever compute or assign Infinity/NaN regardless of how long HitFreeze holds timeScale at 0. `elapsed` accumulation (Time.unscaledDeltaTime) and roll duration/cooldown/i-frame timing are untouched, so roll feel and duration are unaffected outside the freeze micro-window. Standalone `dotnet build` is not viable for this Unity project (requires Unity Editor's own project-generation step) so compile-correctness was verified by manual review of C# syntax/braces instead. AWAITING human playtest confirmation before marking resolved."
files_changed:
  - Assets/Scripts/Player/RollController.cs
