# Stack Research

**Domain:** Mobile touch input architecture for 4 distinct combat mechanics (DeadEye/SAMURAI/MAX/NOVA) + module-unlock progression, on top of existing Unity 6 / Input System 1.19.0 "Overclock Mode" combat.
**Researched:** 2026-07-20
**Confidence:** MEDIUM-HIGH (Input System APIs verified against official docs; mobile-feel/latency numbers are industry-general, not project-measured; no Context7 library available for `com.unity.inputsystem` — official docs used instead)

## Recommended Stack

### Core Technologies

No new Unity packages are required. Everything needed for all 4 mechanics is already inside the installed `com.unity.inputsystem@1.19.0` (confirmed in `Packages/manifest.json`) and core UnityEngine. Adding a virtual-joystick asset store package or a third-party "mobile input" plugin would duplicate functionality Unity already ships — avoid it (see "What NOT to Use").

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| `com.unity.inputsystem` — `EnhancedTouch` API (`UnityEngine.InputSystem.EnhancedTouch`) | 1.19.0 (installed) | Per-finger multi-touch tracking (`Touch.activeTouches`, `Touch.activeFingers`) | Required for NOVA's dual simultaneous streams and DeadEye's up-to-6-tap sequence — the default low-level `Touchscreen.touches[n]` array is positional/slot-based and reassigns slots as fingers lift, which is unusable for "this finger = drone, that finger = body." `EnhancedTouch` tracks touches by stable `touchId` across the gesture. Call `EnhancedTouchSupport.Enable()` once in a bootstrap `Awake()` (e.g. `InputManager`). |
| `com.unity.inputsystem` — On-Screen Controls (`UnityEngine.InputSystem.OnScreen.OnScreenStick` / `OnScreenButton`) | 1.19.0 (installed) | Drives a real `InputAction` binding (e.g. `<Gamepad>/leftStick` style virtual control) from a UI `Image`/`Button` the player drags/taps | These ship inside the core package (not a separate sample-only asset) — no extra install. Gives you a virtual joystick / virtual buttons that integrate with the *existing* `PlayerInput` + action map instead of a parallel bespoke touch system. |
| `UnityEngine.InputSystem.UI.InputSystemUIInputModule` — `pointerBehavior` property | 1.19.0 (installed) | Controls whether concurrent touches are collapsed into one pointer or tracked independently | **Verify this is set correctly** for NOVA's dual-stick UI (see Mechanic Feasibility below) — default already supports multi-touch, but must not be accidentally overridden to `SingleUnifiedPointer`. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `PlayerPrefs` (built-in, no package) | N/A | Persist "which boss modules are unlocked" + "last-selected module" flags | Simplest option for a handful of bool/int flags (4 modules + Overclock). The project currently has **zero** persistence code anywhere (`Grep` for `PlayerPrefs`/`JsonUtility`/`persistentDataPath` in `Assets/Scripts` returns no hits) — this milestone is the first to need save data at all. Don't reach for a save-system package for 5 flags. |
| `JsonUtility` + `Application.persistentDataPath` (built-in) | N/A | Fallback if unlock data grows beyond simple flags (e.g. per-boss best time, per-module stats later) | Only if `PlayerPrefs`' flat key-value model becomes awkward. Not needed for this milestone's stated scope (unlock flags + 1 selected module per mode). |
| None (Rigidbody2D `CollisionDetectionMode2D.Continuous`, already project-standard) | Unity 6000.3.11f1 built-in | MAX's "cannot stop, touching anything = instant outcome" movement | Already the project convention — `PlayerController.Awake()` already forces `CollisionDetectionMode2D.Continuous` on the player's `Rigidbody2D`. No new physics package needed; MAX just needs velocity-locking logic layered on the existing rigidbody, not a new collision system. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| Unity Remote / Device Simulator (`com.unity.device-simulator`, built into Editor via Window > General > Device Simulator) | Test multi-touch gestures without deploying to an Android device every iteration | Device Simulator can synthesize multi-touch in Editor; still budget real-device passes for SAMURAI's parry timing and NOVA's dual-stick feel — emulated touch latency ≠ real touchscreen latency. |
| Android Studio Logcat / `adb shell dumpsys gfxinfo` | Measure actual on-device input-to-frame latency for parry-window tuning | Needed once SAMURAI's parry window is implemented — tune the window in real device milliseconds, not editor frame counts (see SAMURAI feasibility note). |

## Installation

No `npm`/package-manager installs — this is Unity/UPM, and no version bumps or new packages are required.

```
# Nothing to add to Packages/manifest.json.
# com.unity.inputsystem is already pinned at 1.19.0 and contains everything needed:
#   - UnityEngine.InputSystem.EnhancedTouch (multi-touch)
#   - UnityEngine.InputSystem.OnScreen (OnScreenStick / OnScreenButton)
#   - UnityEngine.InputSystem.UI.InputSystemUIInputModule (pointerBehavior)
```

If a designer wants prettier joystick art/behavior than `OnScreenStick` gives out of the box (e.g. floating joystick that spawns at touch-down position, dead-zone tuning UI), it is reasonable to hand-roll a thin wrapper around `OnScreenStick` rather than pull in a marketplace asset — see "What NOT to Use."

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|--------------------------|
| `EnhancedTouch.Touch.activeTouches` for per-finger tracking | Raw `Touchscreen.current.touches[i]` | Only if you need zero-GC, absolute-minimum-overhead polling and are willing to hand-roll touch-ID continuity yourself. For 2 simultaneous streams (NOVA) or ≤6 sequential taps (DeadEye) the `EnhancedTouch` convenience API's overhead is irrelevant on Android ARM64 — don't hand-roll this. |
| `OnScreenStick`/`OnScreenButton` UI-driven virtual controls | Fully custom `IPointerDownHandler`/`IDragHandler` MonoBehaviours reading raw screen-space deltas | Use custom handlers only where the interaction genuinely isn't a "stick" or "button" (e.g. DeadEye's tap-up-to-6-points-in-a-fan needs custom placement/validation logic anyway — `OnScreenStick` doesn't fit that shape). For NOVA's drone-drag and MAX's directional-flick, `OnScreenStick`-style dragging is directly reusable. |
| `PlayerPrefs` for unlock flags | Third-party save-system asset (Easy Save, etc.) | Only if milestone scope grows to include cloud sync, encrypted saves, or complex nested progression data — none of which is in scope per `PROJECT.md`. |
| Interface-based module Strategy pattern (`ICombatModule` + sibling `MonoBehaviour`s, see Mechanic Feasibility) | ScriptableObject Strategy pattern (`CombatModuleSO : ScriptableObject`) | SO-strategy is attractive for designer-tunable data-only strategies, but 3 of these 4 mechanics need heavy per-frame physics/coroutine state (dash paths, parry windows, drone rigidbody) that fights ScriptableObject's "shared asset instance" model unless you manually `Instantiate()` it per player. Given the existing codebase's convention is already interface + sibling-`MonoBehaviour` (`IEnemy`, `ISpawnGatable`, `CombatController`/`RollController`/`ChronoGaugeController` as siblings), staying consistent is lower-risk than introducing a second pattern. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|--------------|
| Third-party mobile joystick/input asset packages (e.g. "Joystick Pack," "Easy Touch") | Duplicates `OnScreenStick`/`OnScreenButton`/`EnhancedTouch` which already ship in the installed Input System package; adds an unnecessary dependency and a second input pipeline to keep in sync with the existing `InputManager`/action-map architecture | `com.unity.inputsystem`'s own `OnScreen` namespace + `EnhancedTouch`, wired into the existing `InputSystem_Actions.inputactions` asset |
| Legacy `Input` class (`Input.GetTouch(0)`, `Input.touchCount`) — the pre-Input-System API some tutorials still show | Project has fully committed to the new Input System (`PlayerInput` component, `.inputactions` asset, `InputManager` singleton). Mixing legacy `Input.*` calls for touch alongside the new system for everything else creates two competing input pipelines and duplicate device polling | `EnhancedTouch.Touch` / `Touchscreen.current` (new Input System) exclusively |
| `Mouse.current` as the sole aim-direction source (**current codebase state**) | `CombatController.GetMouseWorldDirection()` and `RangeDisplay` (lines ~98, ~123) read `Mouse.current.position` directly. On Android there usually is no `Mouse` device, so aim direction silently falls back to `Vector2.right` every frame today. This is a **pre-existing gap**, not a v4.0 regression, but every one of the 4 new mechanics needs a working touch-based aim/target/tap system, so this must be fixed as a prerequisite, not worked around per-boss | Read `Pointer.current.position` instead of `Mouse.current.position` — `Touchscreen : Pointer` and `Mouse : Pointer`, so `Pointer.current` resolves to whichever device last produced input (mouse in Editor, touch on device) without branching code. For DeadEye's multi-tap-point specifically, use `EnhancedTouch.Touch.activeTouches[i].screenPosition` since you need *all* active taps, not just "the last one." |
| `UIPointerBehavior.SingleUnifiedPointer` on `InputSystemUIInputModule` | Would collapse NOVA's two simultaneous on-screen sticks (body-move + drone-move) into a single tracked pointer, making one of the two sticks unusable while the other is held | Leave the default `SingleMouseOrPenButMultiTouchAndTrack` (touches already tracked independently) or explicitly set `AllPointersAsIs` if you also want simultaneous mouse+touch in Editor testing to behave independently |

## Mechanic Feasibility & Integration Notes (mobile touch, not 1:1 ported from mouse/keyboard)

### DeadEye — tap up to 6 aim points, then confirm/release fires in sequence

**Feasibility: YES, with a dedicated custom input handler — not a reuse of `CombatController`'s single-target `Attack` binding.**

- Current `Attack` action is a single `Button` bound to `<Touchscreen>/primaryTouch/tap` (see `InputSystem_Actions.inputactions`). That binding model (one tap = one event) is fine for *triggering* Overclock but cannot represent "collect up to 6 taps, then a separate confirm gesture fires them." DeadEye needs a **new action set**, not a reinterpretation of `Attack`.
- Recommended pattern: while DeadEye's hold-to-slowmo gesture is active (reuse the existing hold/release plumbing from `InputManager`/`CombatController` — `AttackHeld`/`IsAttackDown`/`AttackReleased` already exist and work the same on touch since `Touchscreen/primaryTouch/tap` + press interactions map onto the same Button action type), read **secondary taps** via a second `InputAction` (e.g. `MarkPoint`, bound to `<Touchscreen>/touch1/tap` or, more robustly, via `EnhancedTouch.Touch.activeTouches` filtered to touches other than the one driving the hold). Each tap that lands inside the cone/fan (validate with the same `IsInAttackShape`-style dot-product check `CombatController` already uses for Linear/Fan) appends a screen-space/world-space point to a `List<Vector2>` (max 6, ignore taps beyond that or once/if you want a "re-tag last point" UX).
- **Release** (the existing `AttackReleased`/`ExitSlowMotion` path) becomes "fire all tagged points in sequence" instead of "dash to nearest enemy" — this is a drop-in replacement of `DashOrWhiff()`'s single-target branch with a coroutine that iterates the tagged-point list.
- Mobile-specific risk: **fat-finger precision.** 6 distinct tap points inside a cone on a ~5-6" phone screen, while slow-mo is active and the player is also holding down the first finger for the hold-gesture, is tight. Mitigate with (a) a generous per-point hit-test radius (screen-space, not world-space, so it's DPI-independent), (b) visually confirming each tag immediately (matches the project's existing `RangeDisplay`/highlight pattern), (c) letting a tap near an *already-tagged* point untag it (rapid corrections) rather than requiring precision on the first try. This is a design/UX tuning need, not a technical blocker — flag for phase-level playtesting, not architecture research.
- Confidence: MEDIUM — the touch/multi-tap mechanics are HIGH confidence (verified Input System capability), but the "6 taps fit comfortably in a cone on a phone screen while slow-mo is running" *feel* is unverified and should get a dedicated playtest pass early in that boss's phase.

### SAMURAI — real-time (no slowmo) directional slash + reflex-timing parry window

**Feasibility: YES for the slash; parry timing window needs explicit latency budgeting — this is the mechanic most at risk on mobile.**

- Directional slash-on-tap: reuse the `Pointer.current`/`EnhancedTouch` position-at-tap-time to compute a direction from player to tap point (same pattern as fixing `GetMouseWorldDirection`), no slowmo state needed — this is a new, simpler, always-real-time input path, independent of `CombatController`'s Overclock state machine. It should live in its own module (see Architecture note below), not be bolted onto `CombatController`.
- Parry window: this is a **reflex check against system latency**, and industry data is unambiguous that touch is worse than a physical button here — commercial touchscreens commonly show 50-200ms of latency, with tap-perception JND around 69-96ms even before your game's own frame-processing overhead (MEDIUM confidence — general HCI/mobile-latency literature, not Unity-specific benchmarks; no Unity-specific published number for this exact scenario was found). On top of that, Unity's Input System itself only guarantees input is read at the start of the frame it was polled in — under `Time.timeScale` unaffected by design here (SAMURAI is explicitly "no slowmo"), so the game-logic-side budget is a full frame (~16ms at 60fps) plus whatever the OS/touch-controller adds.
- Practical implication: **the parry window must be tuned in real device milliseconds, not "frames," and should be measured on the actual minSdk-25-class low end device tier**, not just a flagship test phone — low-end Android touch controllers can add materially more latency than a high-end device. Recommend prototyping the parry window at a generous 200-250ms real-world window initially (accounting for ~100ms of touch-system latency + human reaction variance) and tightening only after device-based playtesting, rather than porting a keyboard/mouse-tuned window (which could be 100-150ms) 1:1 — a keyboard-tuned window will feel "impossible" on touch and undermine the "tutorial boss, first unlock" onboarding goal.
- The "mistiming = instant death on a specific parry-only telegraph" design compounds this risk: an unfair-feeling death due to input latency (not player error) on the very first boss is a bad first impression. Strongly recommend a small forgiveness buffer (input buffering — accept a parry input a few frames *before* the exact ideal window in addition to during it; standard fighting-game "buffer window" technique) rather than a razor-thin exact-frame check.
- Confidence: MEDIUM-LOW on the exact millisecond number (needs device measurement), HIGH on the general risk/mitigation direction (buffering, real-device tuning, generous initial window) which is well-established in mobile/fighting-game design literature.

### MAX — unstoppable projectile-player, touch=kill, wall/attack=instant self-death

**Feasibility: YES, straightforward with existing Rigidbody2D setup — this is the least input-risky of the 4 mechanics; the risk here is physics/collision, not touch input.**

- `PlayerController.Awake()` already sets `CollisionDetectionMode2D.Continuous` + `RigidbodyInterpolation2D.Interpolate` on the player's `Rigidbody2D` — exactly the configuration continuous-collision documentation recommends for fast-moving objects that must not tunnel through walls. No new physics setup needed; MAX is an alternate **movement/state module**, not a new collision system.
- Recommended pattern: MAX's module locks `_rb.linearVelocity` to a constant magnitude every `FixedUpdate` (can't be zero, can't be player-adjustable in magnitude) while hold-to-slowmo lets the player steer *direction* only (rotate the velocity vector toward `Pointer.current`/touch-drag direction) — this is a variant of the existing slowmo-hold pattern (`EnterSlowMotion`/`ExitSlowMotion` in `CombatController` already exists and is reusable as-is for "hold = plan," since MAX still uses the same Attack-hold gesture, just repurposes what happens during the hold).
- Touch-input shape for "plan a path": a drag/aim gesture is a natural fit for `OnScreenStick`-style dragging (reuse the pattern, not necessarily the component, since MAX needs continuous direction rather than a joystick's magnitude+direction).
- Collision-outcome logic (touch enemy = kill, touch wall/enemy-attack = self-death) is a straightforward `OnCollisionEnter2D`/trigger check against layer masks — the project already has `Physics2D.Linecast`/`OverlapCircle` against cached `LayerMask`s (`_enemyLayerMask`, `_obstacleMask` in `CombatController`) as the established pattern; MAX's collision handler should follow the same "cache masks in `Awake()`" convention.
- One real physics caveat worth flagging for the phase that implements this: `Time.timeScale` compensation. `PlayerController.ApplyMovement()` already compensates player horizontal speed for `Time.timeScale` (`compensatedMax = moveSpeed * (1f / Time.timeScale)`) so movement speed feels constant across slowmo — MAX's constant-velocity lock needs the same compensation, or the "cannot stop" character will visibly slow down during its own hold-to-plan slowmo, undermining the "pure momentum" fantasy.
- Confidence: HIGH — this is applying an already-verified, already-used project pattern (Continuous CCD + timeScale compensation) to a new state, not introducing new technology.

### NOVA — dual simultaneous control (body + drone)

**Feasibility: YES on touch, but requires an explicit control-scheme decision — do not attempt literal "two independent virtual joysticks always on screen at once" without deliberately choosing the input-mapping approach below; it is the mechanic most likely to feel bad if the control scheme is copy-pasted from a PC dual-analog-stick design.**

- Two independent *simultaneous* touch streams are technically supported (`EnhancedTouch.Touch.activeTouches`, up to 10 concurrent touches per official docs; `InputSystemUIInputModule`'s default `pointerBehavior` already tracks touches independently — see "What NOT to Use" above for the one setting to avoid breaking this). The real design question is **which screen regions/gestures map to which stream**, not whether Unity can technically read two touches at once.
- Three concrete mobile-native options, ranked by recommended fit for a landscape phone screen (project is locked to landscape, 1920x1080 default per `PROJECT.md`):
  1. **Split-screen touch zones + auto-body / drag-drone** (recommended default): body auto-runs/auto-follows a simple forward-motion or is controlled by a small fixed on-screen d-pad/joystick in one bottom corner, while the *entire other half* of the screen is a free-drag zone for drone position (drone follows the finger's raw position, offset-relative or absolute-mapped to world space via `Camera.ScreenToWorldPoint`, same conversion `CombatController.GetMouseWorldDirection` already does). This avoids two literal joysticks fighting for thumb space and matches how most mobile twin-stick-shooter research (industry pattern search) recommends handling dual control on phones — full free-drag for precision aiming/positioning tends to feel better on touch than a second constrained virtual stick.
  2. **Two on-screen sticks, one per bottom corner** (`OnScreenStick` x2, each bound to a different composite in a NEW action, e.g. `Move` stays body-movement, add `DroneMove` as a second Vector2 action bound to a second `OnScreenStick`): closest port of a PC dual-stick layout, but consumes both thumbs entirely for movement, leaving no thumb free for anything else (attack execution, if NOVA needs a separate trigger, would have to be automatic/proximity-based). Only recommended if playtesting shows players want the "classic twin-stick" feel and NOVA has no separate attack-trigger input to fit.
  3. **Touch-and-hold to "possess" the drone temporarily** (single input stream, context-switches which entity it controls): simplest to implement (no simultaneous-touch complexity at all) but contradicts the stated design ("dual simultaneous control") — only listed for completeness/fallback if playtesting shows true simultaneous dual-touch is too demanding for the target audience on phone-sized screens.
- Whichever option is chosen, the actions belong in a **new action map or new actions within Player map** (e.g. `DroneMove`/`DronePosition`), not overloaded onto existing `Move`/`Look` — those stay owned by body movement so NOVA doesn't regress the base `PlayerController`.
- Confidence: MEDIUM — the technical multi-touch capability is HIGH confidence (verified via official docs), but which of the 3 control-scheme options "feels right" is a design/playtesting question, not a stack question; flag NOVA's phase for a dedicated early playable-prototype pass before committing to final control scheme, per this milestone's own stated prototype-validation philosophy in `PROJECT.md`.

## Architecture Integration: Unlocked-Modules Layer on Top of Existing `CombatController`

**Recommendation: extract an `ICombatModule` interface; keep `CombatController` as (or convert it into) the "Overclock" module implementation; add a thin `PlayerModuleController` that owns which module is active.**

- The existing codebase already uses interface-based extensibility for exactly this kind of "one shared contract, multiple concrete behaviors" problem (`IEnemy` + `MeleeEnemy`/`RangedEnemy`/`BossEnemy`; `ISpawnGatable`). Reusing that convention for combat modules is lower-risk and more consistent than introducing a new pattern (e.g. ScriptableObject strategies) for one part of the codebase.
- Concrete shape:
  - `ICombatModule` (or abstract `CombatModuleBase : MonoBehaviour`) defining lifecycle hooks the module manager calls: `OnModuleActivated()`, `OnModuleDeactivated()`, plus whatever subset of `AttackHeld`/`IsAttackDown`/`AttackReleased`/`RollPressed` each module cares about (they don't all need the same hooks — SAMURAI doesn't use slowmo at all, MAX repurposes the hold differently than DeadEye).
  - Each of the 5 combat behaviors (existing Overclock = `CombatController` today, + DeadEye/SAMURAI/MAX/NOVA) becomes its own sibling `MonoBehaviour` on the Player prefab, **disabled by default**, implementing the interface. This matches the current sibling-component pattern (`CombatController`, `RollController`, `ChronoGaugeController` already coexist as separate components on the player).
  - `PlayerModuleController` (new, small) holds: which modules are unlocked (persisted via `PlayerPrefs`), which one is currently active, and does `activeModule.enabled = true` / all others `= false` on activation switch. It is the *only* thing that reads/writes unlock+selection state — every other system (HUD, boss-defeat trigger, mode selection UI) talks to this controller, not to `PlayerPrefs` directly.
  - Boss-defeat → unlock: `BossEnemy`'s existing defeat/death path (already calls `ScoreManager.AddBossKillScore()`) gets one additional call — `PlayerModuleController.Unlock(moduleId)` — at the same point. No changes needed to `BossEnemy`'s FSM structure itself.
  - Two game modes' differing swap rules (한계 시험 = lock in one module for the whole run; 보스 러시 = free swap anytime among unlocked) are **policy, not architecture** — both modes call the same `PlayerModuleController.Activate(moduleId)` API; the mode controller just decides *when* it's allowed to call it (once at run-start vs. any time), and the UI just decides which modules the mode's picker shows as selectable at a given moment. This keeps the module system itself mode-agnostic.
- This design deliberately does **not** ask `CombatController` (Overclock) to become polymorphic/parametrized internally to support 4 more mechanics — the existing class is already large and heavily comment-annotated with hard-won pitfall fixes (obstacle linecasts, slow-mo timeout, hit-freeze sequencing). Bolting 4 unrelated mechanics into its `Update()` would risk regressing the validated, shipped Overclock feel. Keeping each mechanic in its own component, gated by a simple enable/disable switch, is the smaller, more surgical change.

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|------------------|-------|
| `com.unity.inputsystem@1.19.0` | Unity 6000.3.11f1 | Already installed and in active use — `EnhancedTouch`, `OnScreen.*`, and `InputSystemUIInputModule.pointerBehavior` have all been stable API surface since Input System 1.0-1.1; no version bump required to use any of them at 1.19.0. |
| `EnhancedTouchSupport.Enable()` | Must be called before any `EnhancedTouch.Touch` API is read (typically once in an `Awake()`/bootstrap) | Symmetric `Disable()` on shutdown is documented as good practice but not required for a single-scene-lifetime mobile game; low priority for this prototype. |

## Sources

- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Touch.html — EnhancedTouch API, multi-touch (up to 10 concurrent touches), `activeFingers`/`activeTouches` — HIGH confidence (official docs)
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/OnScreen.html — `OnScreenStick`/`OnScreenButton` behavior and setup — HIGH confidence (official docs)
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/UISupport.html — `InputSystemUIInputModule` multi-pointer handling, per-touch `pointerId` (deviceId+touchId) — HIGH confidence (official docs)
- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.13/api/UnityEngine.InputSystem.UI.UIPointerBehavior.html — `AllPointersAsIs` / `SingleMouseOrPenButMultiTouchAndTrack` / `SingleUnifiedPointer` enum semantics — HIGH confidence (official API docs)
- https://docs.unity3d.com/6000.2/Documentation/ScriptReference/CollisionDetectionMode2D.Continuous.html and https://docs.unity3d.com/2020.1/Documentation/Manual/ContinuousCollisionDetection.html — Continuous 2D collision detection semantics for fast-moving rigidbodies — HIGH confidence (official docs); project's own `PlayerController.cs` already applies this setting
- General mobile touch latency figures (50-200ms commercial touchscreen latency; tap-perception JND ~69-96ms) — MEDIUM confidence, general HCI/mobile literature via WebSearch, not Unity-specific or project-measured; flagged for real-device verification during SAMURAI's implementation phase
- Strategy-pattern-via-ScriptableObject vs. interface+MonoBehaviour discussion — MEDIUM confidence, community sources (Unity Learn tutorial, dev.to, Unity Discussions threads), cross-checked against this project's own existing `IEnemy`/`ISpawnGatable` convention (HIGH confidence — verified by reading the actual codebase) to reach the final recommendation
- Direct codebase inspection (HIGH confidence, primary source): `Assets/Scripts/Player/CombatController.cs`, `Assets/Scripts/Player/PlayerController.cs`, `Assets/Scripts/Player/InputManager.cs`, `Assets/Scripts/Player/ChronoGaugeController.cs`, `Assets/Scripts/Player/RangeDisplay.cs`, `Assets/InputSystem_Actions.inputactions`, `Packages/manifest.json`, `Assets/Scripts/Enemy/` directory listing, and a repo-wide grep for `PlayerPrefs`/`JsonUtility`/`persistentDataPath` (zero hits — confirms no existing persistence layer) and for `Mouse.current`/`Touchscreen`/`Pointer.current` (confirms the mouse-only aim-direction gap described above)

---
*Stack research for: Mobile touch input + module-unlock architecture for Fast v4.0 (DeadEye/SAMURAI/MAX/NOVA boss mechanics)*
*Researched: 2026-07-20*
