# Domain Pitfalls: Unity 2D Mobile Platformer Action Game

**Project:** Fast (가칭)
**Domain:** Unity 6 LTS / URP 2D / Android ARM64 / mobile action platformer
**Researched:** 2026-05-27
**Confidence:** HIGH for Unity API behavior (official docs + well-documented community issues); MEDIUM for mobile-specific feel issues (empirical, device-dependent)

---

## Critical Pitfalls

Mistakes that require rewrites or fundamentally break the core mechanic.

---

### Pitfall 1: Time.timeScale Breaks FixedUpdate Physics Timing

**What goes wrong:**
`Time.timeScale = 0.2f` scales both `Time.deltaTime` and `Time.fixedDeltaTime`. FixedUpdate steps become less frequent in wall-clock time, but each step still sees the scaled `Time.fixedDeltaTime`. This is correct for game-world physics — but if you use `Rigidbody2D` velocity directly (`rb.velocity = dir * speed`) in FixedUpdate, the player's perceived movement during slow-motion will feel sluggish even if you intend the player to move at "real time" speed during the slow window.

The core problem: this game wants the player to move at (near) normal speed during slow-motion while enemies are slowed. You cannot achieve this by simply changing `Time.timeScale` without compensating the player's physics.

**Root cause:**
All `Rigidbody2D` simulation runs on the same `Physics2D` fixed timestep, which is scaled by `Time.timeScale`. There is no per-object timeScale in Unity 2D physics.

**Consequences:**
- Player character feels "stuck in mud" during slow-motion — same timeScale that slows enemies also slows player physics
- Gravity during slow-mo is also reduced, making the player float unrealistically
- Dash/rush velocity during the "release" moment is applied in a slow-physics frame, arriving late

**Prevention:**
Two valid approaches:
1. **Manual velocity scaling for player only:** Keep `Time.timeScale` at 0.2 for the world. In the player's FixedUpdate, multiply intended velocity by `(1f / Time.timeScale)` to compensate: `rb.velocity = direction * speed * (1f / Time.timeScale)`. Apply gravity compensation similarly by increasing `Physics2D.gravity` scale on the player Rigidbody2D, OR use `rb.gravityScale` adjusted inversely.
2. **Animator speed compensation:** For player animations, set `animator.updateMode = AnimatorUpdateMode.UnscaledTime` so player animations play at normal speed while enemy animators run on scaled time.

Use approach 1 (velocity scaling) for physics feel. Use approach 2 for animation.

**Warning signs:**
- Player moves in slow-motion during attack hold
- Player floats slowly after releasing attack button
- Dash arrival feels delayed

**Phase:** Core combat mechanic implementation (Phase 1/2). Must be solved before any feel tuning.

---

### Pitfall 2: Time.timeScale = 0 Breaks AudioSource Completely

**What goes wrong:**
`AudioSource.Play()` does not respect `Time.timeScale` by default — audio continues at normal pitch. However, if you want slow-motion audio effect (pitch down), you must manually set `AudioSource.pitch = Time.timeScale`. But if `Time.timeScale` reaches exactly `0` (full pause), audio cuts out entirely on some Android devices because the audio thread starves.

For this game (slow-mo down to ~0.2, not 0), the bigger issue is: **SFX triggered during slow-motion play at wrong pitch** unless explicitly compensated.

**Root cause:**
`AudioSource` pitch is independent of `Time.timeScale` unless you link them in code.

**Prevention:**
- Never use `Time.timeScale = 0` for pause; use a custom pause mechanism that sets `Time.timeScale = 0.0001f` or handle pause at the game logic layer
- For slow-mo audio: create an `AudioManager` that sets pitch on all active `AudioSource` components whenever `Time.timeScale` changes
- For the dash impact SFX: trigger it with `AudioSource.PlayOneShot()` after restoring timeScale to 1.0, not before

**Warning signs:**
- SFX sound chipmunk-fast or robot-slow during slow-motion
- Audio cuts out completely on specific Android devices during action sequences

**Phase:** Audio integration phase. Flag during slow-motion system implementation.

---

### Pitfall 3: ParticleSystem Ignores Time.timeScale Unless Explicitly Set

**What goes wrong:**
`ParticleSystem` components have their own `simulationSpace` and `useUnscaledTime` property. By default, particles DO respect `Time.timeScale` — but Shuriken particle systems that were baked in older Unity versions may behave differently. More critically: **trail renderers and VFX Graph effects each have independent time scaling settings** that must be set per-asset.

For this game: the attack range indicator and dash VFX must be visible and animated at player-speed during slow-motion. If they run on scaled time, they will appear frozen.

**Root cause:**
`ParticleSystem.main.useUnscaledTime` defaults to `false` (uses scaled time). For UI particles and player-attached VFX that should run at real time during slow-mo, this must be set to `true`.

**Prevention:**
- For all player-side VFX (attack indicator, dash trail): set `useUnscaledTime = true` in the ParticleSystem Main module
- For enemy hit VFX that should appear slowed: leave default (scaled time)
- Create a helper that iterates all ParticleSystem components on the player prefab and sets `useUnscaledTime = true` at initialization

**Warning signs:**
- Attack range circle appears frozen/stuttering during slow-motion
- Dash trail doesn't animate during the dash

**Phase:** VFX implementation. Check during slow-motion prototype validation.

---

### Pitfall 4: Invincibility Frames Are Frame-Rate Dependent if Not Time-Based

**What goes wrong:**
The most common mistake: implementing i-frames as a frame counter (`iFrameCount--`). At 60fps on a flagship device, 10 frames = 0.167s. At 30fps on a budget Android device, 10 frames = 0.333s — double the invincibility duration. During slow-motion (`Time.timeScale = 0.2`), if using `Time.deltaTime`, 10 "game seconds" of i-frames becomes 50 real seconds.

This game has roll i-frames AND dash i-frames AND post-fall i-frames. All three must be consistent regardless of frame rate and timeScale.

**Root cause:**
Frame counter-based i-frames do not account for variable frame rate. `Time.deltaTime`-based timers that run during slow-motion get inflated by the timeScale factor.

**Prevention:**
- Use `Time.unscaledDeltaTime` for all i-frame timers (roll cooldown, dash invincibility, post-fall grace period)
- Pattern: `iFrameTimer -= Time.unscaledDeltaTime; if (iFrameTimer <= 0) isInvincible = false;`
- The roll's cooldown timer also runs on `unscaledDeltaTime` — the player should be able to roll again after 1 real-world second regardless of whether slow-motion is active

**Warning signs:**
- Roll cooldown feels different depending on whether player is in slow-motion
- Invincibility lasts too long on slow devices
- Testing on Editor (uncapped fps) gives different i-frame feel than device build

**Phase:** Player combat system implementation. Define i-frame duration constants in a single config file; never hardcode frame counts.

---

### Pitfall 5: Rigidbody2D Tunneling on Fast Dash

**What goes wrong:**
The dash/rush attack moves the player from current position to enemy position. If implemented as `rb.velocity = direction * 50f` (large velocity spike), the `Rigidbody2D` will tunnel through thin platforms and walls between the player and target in a single `FixedUpdate` step. Collision detection may miss the platform entirely.

**Root cause:**
Unity's default `Rigidbody2D` collision detection mode is `Discrete`. At high velocity, the object can "jump over" a thin collider between physics steps.

**Prevention:**
- Set the player `Rigidbody2D` `collisionDetectionMode` to `Continuous` permanently (small CPU cost, essential for fast dash)
- Alternatively, for the dash: teleport to a position slightly before the enemy using `rb.MovePosition()` over 2-3 frames rather than a single velocity spike
- Set a `maxDepenetrationVelocity` cap in Project Settings > Physics 2D if jitter occurs post-collision

**Warning signs:**
- Player passes through platforms during dash on certain level layouts
- Player gets stuck inside enemy collider after dash
- Dash sometimes skips past the target enemy

**Phase:** Dash/combat system. Test specifically with enemies near platform edges.

---

### Pitfall 6: Camera Follow Produces Jitter Due to Update Order Mismatch

**What goes wrong:**
If camera follow runs in `Update()` and `Rigidbody2D` physics runs in `FixedUpdate()`, the camera position is calculated between physics steps. The visual result is sub-pixel jitter on the camera at stable frame rates, and visible judder at 30fps.

This is worsened in this game because floor transitions involve sudden camera movement (camera "rises" to next floor) — if the transition lerp runs in `Update()` while physics runs in `FixedUpdate()`, the player character and camera desync visually.

**Root cause:**
`Update()` runs every rendered frame; `FixedUpdate()` runs at a fixed physics rate. Interpolating between these two timelines causes visual stutter when frame rate != physics rate.

**Prevention:**
- Use `LateUpdate()` for all camera follow logic (runs after all `Update()` calls in the same frame)
- Enable `Rigidbody2D` interpolation: set `Interpolation = Interpolate` on the player Rigidbody2D — Unity will interpolate the rendered position between physics steps, eliminating physics jitter on the camera
- For the floor transition camera rise: drive it with a Coroutine using `Time.unscaledDeltaTime` so it is unaffected by slow-motion state

**Warning signs:**
- Camera feels slightly floaty or laggy behind the player
- Floor transition camera rise stutters on device
- Fine jitter visible at 60fps on screen recording

**Phase:** Camera system. Fix interpolation setting before any camera feel tuning.

---

## Moderate Pitfalls

---

### Pitfall 7: Mobile Touch Input Latency from New Input System Misconfiguration

**What goes wrong:**
Unity's New Input System (1.19.0) has multiple update modes: `Dynamic Update`, `Fixed Update`, `Manual Update`. The default `Dynamic Update` processes input once per rendered frame, which is correct. However, if someone switches to `Fixed Update` mode (thinking it syncs with physics), touch input can feel laggy on mobile because `FixedUpdate` runs at 50Hz default while the display runs at 60Hz or 120Hz.

The second issue: on-screen buttons using `UI/Button` components with `EventSystem` have a built-in multi-tap protection delay (`InputSystemUIInputModule.moveRepeatDelay`). For an action game with rapid attack button presses, this delay causes missed inputs.

**Root cause:**
Input system update mode mismatch with display refresh rate. EventSystem tap protection designed for navigation, not action game buttons.

**Prevention:**
- In Project Settings > Input System: set `Update Mode` to `Dynamic Update`
- For on-screen attack/roll buttons: do NOT use Unity UI Button's `onClick`. Instead, use `IPointerDownHandler` and `IPointerUpHandler` interfaces directly on the button component — this gives frame-accurate press and release events with no EventSystem delay
- Use `EnhancedTouchSupport.Enable()` at startup for the lowest-latency touch reading path

**Warning signs:**
- Attack button requires a noticeable "hold" before slow-motion triggers
- Release-to-dash feels delayed by 1-2 frames
- Roll button occasionally misses rapid presses

**Phase:** Input system implementation. Test on physical device at 60Hz and 120Hz refresh rates.

---

### Pitfall 8: GC Allocation in Update Loops Causing Android Frame Spikes

**What goes wrong:**
Common allocating patterns in Unity that cause GC spikes every few seconds on Android (which uses incremental GC but still hitches on large allocations):
- `GetComponentsInChildren<T>()` called in `Update()` — allocates a new array each frame
- `FindObjectsOfType<Enemy>()` for enemy detection — O(n) scan + allocation
- String formatting for HUD (`"Floor: " + floorNumber`) — allocates a new string every frame
- LINQ in any hot path (`enemies.Where(e => e.IsInRange()).OrderBy(...)`) — multiple allocations per call
- `new Vector2(...)` is fine (struct, stack-allocated), but `new List<Enemy>()` in Update is not

For this game, the enemy detection during slow-motion (finding nearest enemy in attack range) is a likely hotspot if implemented naively with LINQ or allocating queries.

**Root cause:**
Android's JIT (or IL2CPP) does not eliminate these allocations. GC runs are triggered by accumulated heap pressure and cause 2-10ms frame spikes that break the 60fps target.

**Prevention:**
- Cache `GetComponentsInChildren<T>()` results at initialization; update only when enemies spawn/die via events
- Pre-allocate a fixed `List<Enemy>` for range queries; clear and reuse each frame instead of `new List<Enemy>()`
- For HUD text: use `TextMeshProUGUI.SetText("{0}", floorNumber)` (TMP's allocation-free formatting) instead of string concatenation
- No LINQ in Update, FixedUpdate, or any per-frame method
- Use Unity's `Physics2D.OverlapCircleNonAlloc()` for enemy range detection (writes into a pre-allocated array, zero GC)

**Warning signs:**
- Frame time spikes visible in Unity Profiler's GC Alloc column during combat
- Occasional 100-200ms freeze on mid-range Android device during enemy-dense floors

**Phase:** Any system that queries enemies. Establish the pre-allocated array pattern in Phase 1 before adding more enemies.

---

### Pitfall 9: IL2CPP Build Reveals Reflection/Generic Errors Hidden in Editor

**What goes wrong:**
Unity Editor and Mono builds allow reflection and unstripped generics. IL2CPP (required for Android ARM64 Release builds) performs aggressive code stripping. Common failures:
- `JsonUtility.FromJson<T>()` fails at runtime if type T's fields are stripped (no `[Preserve]` attribute)
- `GetComponent<T>()` with T resolved at runtime via `Type.GetType()` string — fails silently or throws on IL2CPP
- Serialized delegates or Events with anonymous lambdas can be stripped

For this prototype, the risk is lower (minimal serialization), but Android Release builds should be tested early.

**Root cause:**
IL2CPP's linker strips types/methods it cannot statically trace. The Editor uses Mono which does not strip.

**Prevention:**
- Build a `link.xml` file that preserves assemblies used with reflection
- Test IL2CPP Development build on device by Phase 2 (not just Editor play mode)
- Avoid `Type.GetType()` string-based lookups; use generic versions `GetComponent<T>()` with compile-time-known T
- Add `[Preserve]` to any class used with `JsonUtility`

**Warning signs:**
- Game works in Editor/Mono but crashes or has missing behavior in device build
- `MissingMethodException` or `ExecutionEngineException` in device logs

**Phase:** First device build milestone. Do not wait until the end of prototype to test IL2CPP.

---

### Pitfall 10: Infinite Floor System Memory Leak from Unreleased References

**What goes wrong:**
The floor system keeps only current + next floor active. The common mistake: destroying the old floor `GameObject` with `Destroy()` but holding a C# reference to it in a `List<FloorInstance>` or as a field. The managed C# object is not collected until the reference is cleared, and any event subscriptions on the destroyed floor's components will throw `MissingReferenceException` errors that silently swallow exceptions on device.

Second issue: if floor prefabs contain `AudioSource` or `ParticleSystem` components with persistent handles, destroying the GameObject without stopping them first can leave orphaned audio channels.

**Root cause:**
`Destroy()` destroys the Unity engine object but does not null the C# reference. Static event subscriptions on destroyed objects cause null-ref exceptions on the next event fire.

**Prevention:**
- After calling `Destroy(oldFloor)`, immediately set `oldFloor = null` and remove from any list
- Use `UnityEvent` instead of raw C# events for floor-lifetime callbacks, so destroyed subscribers are automatically cleaned up
- Before destroying a floor: call `StopAllCoroutines()` on any MonoBehaviour on it, and stop any `AudioSource`
- Object pool pattern: instead of `Instantiate`/`Destroy`, recycle 2 floor prefab instances by repositioning them — eliminates instantiation GC cost and avoids the destruction reference problem entirely

**Warning signs:**
- `MissingReferenceException` in logcat after floor transitions
- Memory usage climbs steadily (visible in Android Studio Memory Profiler) over 20+ floors
- Occasional frame spike exactly when floor transition occurs (GC collecting dead floor references)

**Phase:** Floor system implementation. The object pool pattern should be the initial design, not a retrofit.

---

### Pitfall 11: Unity 2D Animator State Machine Transition Lag in Action Games

**What goes wrong:**
Unity Animator transitions have a default `Transition Duration` of 0.25 seconds (25% of a 1-second clip). For action game animations (dash start, roll, death), this creates a "blending lag" where the previous animation bleeds into the new one. In a one-hit-kill game, the death animation starting 0.25 seconds late is very noticeable.

Second issue: `SetTrigger()` calls can queue up if the state machine is mid-transition. If the player dies while a transition is in progress, the death trigger may be consumed by the current transition, not the death state. Result: death animation never plays, player appears frozen in mid-roll.

**Root cause:**
Animator state machine's transition system is designed for smooth blending (cutscenes, traversal), not instant-response action game states.

**Prevention:**
- Set `Transition Duration = 0` for all action transitions (dash, roll, attack, death, hit)
- Prefer `SetBool()` or `SetInteger()` over `SetTrigger()` for state changes that need to "stick" — triggers are consumed once and can be lost
- For critical state changes (death, hit): call `animator.Play("DeathState", 0, 0f)` directly, bypassing the transition system entirely
- Keep the Animator graph simple: idle, run, jump, attack_windup, dash, roll, death — no blend trees for prototype

**Warning signs:**
- Animation transitions look delayed by ~3-5 frames after input
- Death animation occasionally does not play (player freezes in roll pose)
- Roll animation blends with idle instead of snapping

**Phase:** Animation system implementation. Set transition duration to 0 as the default from the start.

---

### Pitfall 12: On-Screen Button Dead Zone and Overlap Issues

**What goes wrong:**
Mobile action games with separate movement joystick + jump + attack + roll buttons frequently suffer from accidental cross-input: thumb resting on the joystick also partially touches the jump button's touch area. Unity UI's `RectTransform` hit testing uses rectangular bounds even for circular buttons, so a touch at the corner of the attack button rectangle can activate both attack and roll if their rectangles overlap.

Second issue: the `Canvas Scaler` in "Scale With Screen Size" mode with reference resolution 1920x1080 will scale UI incorrectly on phones with notches/cutouts unless `Match Width or Height` and safe area are explicitly handled.

**Root cause:**
Unity UI default hit testing is axis-aligned bounding box, not shape-based. Canvas scaling does not automatically respect Android display cutouts.

**Prevention:**
- Ensure a minimum 40px gap (in reference resolution units) between all button RectTransforms — do not rely on visual size alone
- Implement `IPointerDownHandler.OnPointerDown` per button with a `Canvas.ForceUpdateCanvases()` call at startup to validate layout
- Use `Screen.safeArea` to offset the UI canvas: create a `SafeAreaAdjuster` script that sets the `RectTransform.anchorMin/Max` based on `Screen.safeArea` at `Awake()`
- Test on a device with a notch (or use Unity's Device Simulator with a notched profile)

**Warning signs:**
- Players accidentally trigger roll when trying to press attack (visible in playtesting)
- Buttons appear cut off on notch-equipped Android devices
- UI looks correct in Game View but wrong on device

**Phase:** UI/input implementation. Safe area handling must be in place before first device playtesting session.

---

## Minor Pitfalls

---

### Pitfall 13: Time Gauge Auto-Recovery Rate Is TimeScale-Sensitive

**What goes wrong:**
If the time gauge recovery logic uses `Time.deltaTime`, it recovers slowly during slow-motion (as intended — the gauge is a resource). But the "kill recovery" instant refill also needs to happen at real-time speed so it does not feel delayed. Mixing `Time.deltaTime` and `Time.unscaledDeltaTime` in the same gauge system causes inconsistency.

**Prevention:**
Explicit design decision: the gauge drains and auto-recovers using `Time.deltaTime` (world time — recovery is slow during slow-mo, which is correct). Kill recovery is an instant add (`currentGauge += killRefillAmount`) — no deltaTime involved, so no scaling issue.

**Phase:** Time gauge system.

---

### Pitfall 14: Post-Fall Recovery Position Uses Stale Platform Reference

**What goes wrong:**
"Return to last platform on fall" requires storing the last safe platform the player stood on. If stored as a `Transform` reference and the floor system destroys/recycles that floor's `GameObject`, the stored `Transform` becomes null. Player "returns" to world origin or throws null ref.

**Prevention:**
Store the last safe position as a `Vector3` (value type, not reference), updated every frame the player is grounded. Never store a `Transform` reference for cross-frame persistence when the floor system recycles objects.

**Phase:** Player controller + floor system integration.

---

### Pitfall 15: Enemy Awareness Activation Race Condition

**What goes wrong:**
The design specifies "enemies activate only after camera transition completes." If activation is implemented as `enemy.SetActive(true)` inside a camera transition Coroutine, and the Coroutine's timing is tied to `WaitForSeconds` (which uses scaled time), slow-motion active during floor transition will delay enemy activation indefinitely.

**Prevention:**
All floor-transition Coroutines use `WaitForSecondsRealtime` (unscaled) to guarantee real-world timing. Or: drive the transition as a state machine where the "transition complete" event fires only after the camera has physically reached its target position (checked by distance threshold), regardless of time.

**Phase:** Floor transition system.

---

## Phase-Specific Warning Map

| Phase / System | Likely Pitfall | Mitigation |
|---|---|---|
| Slow-motion core mechanic | Player physics also slows; audio pitch wrong; particles frozen | Velocity compensation + `unscaledTime` for player systems |
| Player i-frames (roll + dash) | Frame-rate/timeScale dependent duration | Use `Time.unscaledDeltaTime` for all i-frame timers |
| Dash/rush attack | Tunneling through platforms; stale position calculation | `Continuous` collision detection; `MovePosition()` over frames |
| Enemy range query | GC allocations per frame from LINQ/FindObjectsOfType | `Physics2D.OverlapCircleNonAlloc()` with pre-allocated array |
| Camera system | Jitter from Update/FixedUpdate mismatch | `LateUpdate` + Rigidbody2D `Interpolate` mode |
| Floor system | Memory leak from destroyed object references | Pool 2 floor instances; store position as `Vector3` not `Transform` |
| Animation system | Transition delay kills action feel; trigger loss | `Transition Duration = 0`; prefer `animator.Play()` for critical states |
| On-screen UI | Button overlap; notch cutoff | 40px minimum gap; `Screen.safeArea` adjuster |
| First device build | IL2CPP strips types used in Editor Mono | Test IL2CPP dev build early; add `link.xml` |
| Time gauge | Mixed scaled/unscaled time for drain vs. recovery | Consistent policy: drain uses `deltaTime`, kill refill is instant |
| Post-fall recovery | Stale `Transform` reference after floor recycle | Store last safe position as `Vector3` |
| Floor transition completion | `WaitForSeconds` slows with timeScale | Use `WaitForSecondsRealtime` in all transition Coroutines |

---

## Sources

- Unity Manual: Time.timeScale — "FixedUpdate is also affected by timeScale"
- Unity Manual: Rigidbody2D.collisionDetectionMode — Continuous vs Discrete
- Unity Manual: ParticleSystem.MainModule.useUnscaledTime
- Unity Manual: Animator.Play() vs SetTrigger() state machine behavior
- Unity Manual: Physics2D.OverlapCircleNonAlloc (zero-allocation physics query)
- Unity Manual: Input System Update Modes (Dynamic vs Fixed Update)
- Unity Manual: Canvas Scaler + Screen.safeArea for Android cutouts
- Unity Manual: IL2CPP managed code stripping, link.xml
- Community pattern: `Time.unscaledDeltaTime` for i-frames in slow-motion games (well-established in action game development)
- Community pattern: Object pooling for infinite level generation (standard mobile optimization)
