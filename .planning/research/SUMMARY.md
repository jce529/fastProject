# Research Summary

**Project:** Fast (가칭) — Mobile 2D Platformer, Slow-Motion Dash-Attack Prototype
**Synthesized:** 2026-05-27

---

## Stack

- **Time scaling is the central technical spine.** `Time.timeScale = 0.2f` + `Time.fixedDeltaTime = 0.02f * scale` must be set together. The non-obvious consequence: enemy physics slow correctly, but the player's own physics also slow — requiring explicit velocity compensation in FixedUpdate (`rb.linearVelocity *= (1f / Time.timeScale)`) so the player feels responsive during the slow window. All i-frame timers and UI updates must use `Time.unscaledDeltaTime`; all enemy/world systems use scaled `Time.deltaTime`.

- **Input System hold/release via `started`/`canceled` callbacks — not `performed`.** The `InputSystem_Actions.inputactions` asset is already installed. The attack hold-to-aim, release-to-dash pattern maps to `attackAction.started` (enter slow-mo) and `attackAction.canceled` (fire dash). On-Screen Controls route through the same action map — no platform-branching code needed. Canvas Scaler: Scale With Screen Size, 1920x1080, plus a `SafeAreaAdjuster` script for Android notch handling.

- **Rigidbody2D must be Continuous collision detection + Interpolate mode.** The dash teleports the player at high velocity. Discrete detection tunnels through platforms. `Rigidbody2D.MovePosition()` over 2-3 frames (not a velocity spike) is the correct dash implementation — it respects physics callbacks that drive the invincibility layer swap. Player layer swaps between `PlayerHurtbox` and `PlayerInvincible` in the collision matrix; this is cleaner than `Physics2D.IgnoreLayerCollision` calls.

---

## Table Stakes

The following must be present for any playtest session to be valid:

- Responsive left/right movement with immediate directional response (set `linearVelocity` directly, not `AddForce`)
- Single jump with variable height (tap = short hop, hold = higher)
- Attack button produces INSTANT visible feedback on press (no perceptible input lag)
- Slow-motion is genuinely slow (0.15–0.25x timeScale) with a distinct audio/visual cue on entry
- Dash-to-target is near-instant on release — followed by hit-freeze (50–100ms timeScale=0) and sharp translational screen shake
- Enemy telegraph before attack is unambiguous (melee: wind-up animation; ranged: aim indicator line)
- On-screen controls placed bottom-left (movement) and bottom-right (attack/roll), minimum 40px gap between all button RectTransforms
- Floor counter HUD and time-stop gauge always visible
- Death + single-tap restart within 3 seconds
- Camera follow in LateUpdate with Rigidbody2D Interpolation=Interpolate (eliminates jitter)

---

## Watch Out For

Top 5 pitfalls by severity and likelihood:

1. **Player physics slows with the world during slow-motion.** The most common slow-mo mistake in Unity. Fix immediately: compensate player velocity in FixedUpdate by inverse timeScale. Set `animator.updateMode = UnscaledTime` on the player Animator. Without this, the attack aiming phase feels like the player is stuck in mud.

2. **I-frame timers using `Time.deltaTime` — duration varies by timeScale and frame rate.** Roll cooldown of 1.0s becomes 5.0 real seconds at 0.2x timeScale if using scaled deltaTime. And at 30fps vs 60fps frame counts differ 2x. Rule: every timer that must be real-time (roll cooldown, dash i-frames, post-fall grace) uses `Time.unscaledDeltaTime` only.

3. **Animator transition duration defaults to 0.25s — kills action responsiveness.** Death, dash, roll, and hit animations must snap, not blend. Set Transition Duration=0 for all action transitions from day one. For critical state changes (death), call `animator.Play("DeathState", 0, 0f)` directly. Prefer `SetBool()`/`SetInteger()` over `SetTrigger()` to avoid trigger-loss mid-transition.

4. **GC allocations in per-frame enemy queries on Android.** `FindObjectsOfType`, LINQ, and `new List<Enemy>()` in Update paths generate heap allocations causing 2-10ms frame spikes on mid-range Android. Use `Physics2D.OverlapCircleNonAlloc()` with a pre-allocated fixed array. For HUD text, use `TextMeshProUGUI.SetText("{0}", floorNumber)`.

5. **Destroyed floor GameObject references cause MissingReferenceException on event fire.** After `Destroy(oldFloor)`, immediately null the reference and remove from any list. Store the player's last-safe-platform position as a `Vector3` value — never a `Transform` reference. Use `WaitForSecondsRealtime` (not `WaitForSeconds`) in all floor-transition coroutines.

---

## Recommended Build Order

1. **Infrastructure** — `GameEvents` static event bus + `PlayerState` enum. Zero dependencies; everything else wires through these.

2. **Core player movement** — `PlayerController`: movement (linearVelocity direct-set), jump, grounded check (OverlapCircle at feet), fall detection, last-safe-position tracker (Vector3), input lock/unlock via `_inputLocked` bool. Validate on a single static test floor.

3. **Slow-motion + attack system** — `TimeSlowManager` (timeScale + unscaled gauge drain/regen) then `AttackController` (hold/release callbacks, range indicator, target search with OverlapCircleNonAlloc, dash via MovePosition, whiff delay) + `RollController` (unscaled cooldown, invincibility layer swap). This is the core mechanic. Validate against stationary dummies on a flat floor.

4. **Enemy system** — `EnemyController` (Dormant/Aware/Attacking FSM, melee + ranged variants, OnEnemyKilled event). One-hit-kill both ways. Validate: telegraph clarity, camera-gated activation.

5. **Floor system** — `FloorManager` (2-slot preset pool, spawn/destroy) + `FloorTransitionManager` (exit trigger to event chain: input lock, teleport, camera pan, old floor destroy, enemy activate, input unlock). All coroutines use WaitForSecondsRealtime. Validate: 10 consecutive transitions with no MissingReferenceException in logcat.

6. **Camera** — `CameraController` in LateUpdate, player follow with slight forward lead, unscaled upward pan for transitions. Validate: no jitter at 30fps on device.

7. **HUD + death flow** — `UIManager` (subscribes to events only): floor counter, gauge bar, attack type label, death screen, restart. Validate: complete loop from floor 1 to death to restart.

8. **Polish pass** — Hit-freeze, translational screen shake, audio pitch correction, particle useUnscaledTime settings, SafeAreaAdjuster for notch, IL2CPP dev build on device.

---

## Open Questions

Items requiring empirical testing, not resolvable by research:

- **Optimal slow-motion timeScale value.** Research suggests 0.15–0.25x. The exact value determines whether the planning window feels deliberate or trivially easy. Needs playtest with at least 5 people.
- **Gauge drain rate vs. auto-regen balance.** Drain/regen numbers must be tuned empirically — cannot be predicted analytically.
- **One-hit-kill on mobile — fairness perception.** Research confidence MEDIUM. If playtests surface "couldn't react" complaints, increase telegraph duration, do not add HP bars.
- **Linear vs. fan attack shape preference.** Q2 of validation goals. Only playtest data resolves this.
- **Cinemachine 3.x API compatibility.** Version not confirmed in manifest. If uncertain, implement CameraController manually in LateUpdate — simpler and no API risk.
- **PositionIterations=6 performance cost on low-end Android.** Validate that increasing from 3 to 6 does not push frame time over budget on minSdk 25 hardware.
