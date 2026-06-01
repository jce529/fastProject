# Architecture Patterns: Unity 2D Action Platformer

**Project:** Fast (가칭)
**Researched:** 2026-05-27
**Confidence:** HIGH (well-established Unity patterns; no contradicting current sources found — search tools unavailable, based on Unity 6 LTS documentation and established community patterns)

---

## Recommended Architecture

**Pattern: Hybrid — ScriptableObject Event Channels for cross-domain events, direct MonoBehaviour references for tight intra-domain coupling.**

This is not an either/or choice. The project's systems divide cleanly into two categories:

- **Tight coupling is fine** when systems are on the same GameObject or have a clear owner (e.g., PlayerController ↔ AttackController ↔ RollController live on the Player prefab and reference each other directly via `[SerializeField]`).
- **Event channels are required** when systems need to communicate across ownership boundaries without creating brittle scene-wiring (e.g., FloorTransitionManager → CameraController → EnemyController → UIManager — none of these "own" each other).

---

## Component Boundaries

| Component | Responsibility | Owns | Does NOT Own |
|-----------|---------------|------|--------------|
| **PlayerController** | Rigidbody2D movement, jump, fall detection, last-platform save, fall-recovery invincibility frames | Player Rigidbody2D, Collider2D, Animator | AttackController, RollController (referenced, not owned) |
| **AttackController** | Attack button hold/release, range display (linear/fan), target search, dash-attack execution, whiff delay, dash invincibility | Attack range visualization objects | PlayerController movement state (reads it, does not set it) |
| **RollController** | Roll button input, invincibility frame window, cooldown timer | Roll cooldown state | TimeSlowManager (receives unscaled time from it indirectly) |
| **TimeSlowManager** | Time.timeScale manipulation, gauge float, gauge auto-regen, gauge kill-regen, slow-release trigger | Gauge value (authoritative source) | Player, Enemy, UI (notified via event) |
| **EnemyController** | Awareness state machine (dormant / aware / attacking), movement, attack, hit/death | Per-enemy state | Floor awareness: receives activation event from FloorTransitionManager |
| **FloorManager** | Floor preset pool (current + next), next-floor spawn, previous-floor removal, floor counter int | Floor GameObjects, floor counter value | Camera, Player (notified via event) |
| **FloorTransitionManager** | Exit trigger detection, input-lock sequence, teleport call, event dispatch to CameraController | Transition sequence logic | Camera movement itself (tells CameraController to pan, does not move it) |
| **CameraController** | Cinemachine Virtual Camera or manual Camera follow, upward pan during transition, transition-complete event | Camera component | Player positioning (follows, does not control) |
| **UIManager** | Canvas elements: floor counter, time gauge bar, attack type label, death screen, restart button | All HUD GameObjects | Game state (reads events, does not drive them) |

---

## Data Flow

### Attack + Slow-Motion Flow (primary mechanic)

```
[Input System] Attack button held
    → AttackController.OnAttackHeld()
        → TimeSlowManager.BeginSlow()        [direct call — AttackController holds reference]
            → Time.timeScale = 0.2f
            → Fires event: OnSlowBegin
                → UIManager (gauge display activates)
        → AttackController shows range indicator (uses Time.unscaledDeltaTime for UI anim)

[Input System] Attack button released
    → AttackController.OnAttackReleased()
        → Raycast / OverlapCircle for targets in range
        → If target found:
            → PlayerController.BeginDash(target)   [direct call]
                → PlayerController enters DASHING state (invincible)
                → On arrival: EnemyController.TakeDamage()
                    → Enemy dies
                    → Fires event: OnEnemyKilled
                        → TimeSlowManager.OnKillGaugeRegen()  [event → direct call]
                        → UIManager (score/floor counter update if needed)
            → AttackController.EndAttack(success)
                → TimeSlowManager.EndSlow()
        → If no target:
            → AttackController.Whiff()   (longer delay, no dash)
            → TimeSlowManager.EndSlow()
```

### Roll Flow

```
[Input System] Roll button pressed
    → RollController.OnRollPressed()
        → Check cooldown (using Time.unscaledTime — works during slow-mo)
        → If ready: PlayerController.BeginRoll()
            → Player enters ROLLING state (invincible window)
            → After roll duration (unscaled): exits ROLLING, starts cooldown
```

### Floor Transition Flow

```
Player enters exit trigger collider
    → FloorTransitionManager.OnExitReached()
        → Fires event: OnTransitionBegin
            → PlayerController receives: input locked, movement frozen
            → EnemyController(s) on current floor: deactivate awareness
        → FloorManager.SpawnNextFloor()
            → Returns next floor reference
        → PlayerController.Teleport(entryPoint)
        → Fires event: OnCameraPanBegin
            → CameraController receives: begin upward pan to next floor
                → On pan complete: CameraController fires OnCameraPanComplete
                    → FloorTransitionManager receives:
                        → FloorManager.RemovePreviousFloor()
                        → Fires event: OnTransitionComplete
                            → PlayerController: input unlocked
                            → EnemyController(s) on NEW floor: activate awareness
                            → UIManager: increment floor counter display
```

---

## Key Architectural Decisions

### 1. Event Bus Pattern: C# Action Events on a Static/Singleton GameEventBus

**Recommendation: A single static `GameEvents` class holding C# `event Action<T>` delegates.**

Do NOT use Unity's `UnityEvent` (serialized in Inspector) for cross-system events. They create implicit scene coupling that makes refactoring painful.

Do NOT use a full ScriptableObject event channel system for a prototype of this scope — it adds asset management overhead (one .asset file per event type) that slows iteration.

Use a simple static event bus:

```csharp
// GameEvents.cs — no MonoBehaviour, no inheritance
public static class GameEvents
{
    public static event Action OnSlowBegin;
    public static event Action OnSlowEnd;
    public static event Action OnEnemyKilled;
    public static event Action OnTransitionBegin;
    public static event Action<Transform> OnCameraPanBegin;  // carries next floor target
    public static event Action OnCameraPanComplete;
    public static event Action OnTransitionComplete;
    public static event Action OnPlayerDied;
    public static event Action<int> OnFloorChanged;          // carries floor number

    // Invoke helpers (keep invocation in GameEvents, not scattered in callers)
    public static void SlowBegin() => OnSlowBegin?.Invoke();
    public static void SlowEnd() => OnSlowEnd?.Invoke();
    // ... etc
}
```

**Why not ScriptableObject channels:** Correct pattern for large teams / asset-heavy projects. For a single-developer prototype with 9 systems, the asset management friction outweighs the testability benefit. Upgrade after prototype validates.

**Why not direct references everywhere:** FloorTransitionManager would need references to CameraController, all active EnemyControllers, PlayerController, UIManager, FloorManager — a 5-way coupling that makes the transition sequence fragile and untestable. Events decouple the sequence steps cleanly.

### 2. Player State Machine: Enum-Driven Manual FSM

**Recommendation: A plain C# enum + switch statement in PlayerController. No external FSM library.**

```csharp
public enum PlayerState { Idle, Moving, Jumping, Falling, Dashing, Rolling, Dead, TransitionLocked }
```

PlayerController maintains `_currentState` and gates all input/physics in `Update()` and `FixedUpdate()` based on it. State transitions are explicit method calls (`EnterState(PlayerState next)`).

**Why not Unity Animator state machine:** Animator is designed for animation blending, not logic gating. Using it as a logic FSM creates a hidden second FSM that fights your code FSM. Use Animator for visuals only — drive it from code state, not vice versa.

**Why not a full FSM framework (e.g., stateless, xstate-style):** 6 states for a prototype. Manual switch is readable in 5 seconds, a framework requires onboarding overhead. The states also have few transitions — this is not a complex FSM.

**State transition table (what gates what):**

| From State | Allowed Transitions |
|------------|---------------------|
| Idle | Moving, Jumping, Dashing, Rolling, Dead, TransitionLocked |
| Moving | Idle, Jumping, Dashing, Rolling, Dead, TransitionLocked |
| Jumping | Falling, Dashing, Rolling, Dead |
| Falling | Idle, Moving, Dead (fall recovery) |
| Dashing | Idle (on arrival), Dead |
| Rolling | Idle, Moving (after duration) |
| Dead | (none — wait for restart event) |
| TransitionLocked | Idle (on OnTransitionComplete event) |

### 3. Time Scale: `Time.timeScale` + `Time.unscaledDeltaTime` Partitioning

**Recommendation: Set `Time.timeScale = 0.2f` for slow-motion. Systems that must be unaffected use `Time.unscaledDeltaTime` explicitly.**

**Which systems use unscaled time:**

| System | Why Unscaled |
|--------|-------------|
| RollController cooldown | Roll must work during slow-mo; cooldown measured in real seconds |
| RollController roll duration | The roll invincibility window is a real-time window |
| UIManager gauge bar animation | Gauge drain visual should feel responsive, not sluggish |
| AttackController range indicator animation | Range display UI should animate at full speed |
| CameraController pan speed | Camera pan during transition happens after slow ends, but defensive |
| Input System | Inherently unscaled — no action needed |

**Which systems use scaled time (and benefit from slow-mo):**

| System | Why Scaled |
|--------|-----------|
| PlayerController movement | Player moves in slow-mo world |
| EnemyController movement/attack | Enemies visually slow down — this is the effect |
| Rigidbody2D physics | Physics.gravity affected by timeScale automatically |
| Animator speed | Animations slow with timeScale by default |

**Physics note:** `Rigidbody2D.MovePosition` and `AddForce` in `FixedUpdate` use `Time.fixedDeltaTime`, which scales with `timeScale`. This is correct — player and enemies slow down in physics too. Do not use `Time.fixedUnscaledDeltaTime` for gameplay physics.

**Gauge drain implementation:**

```csharp
// TimeSlowManager.Update() — gauge drains in real time, not game time
void Update()
{
    if (_isSlowing)
    {
        _gauge -= drainRatePerSecond * Time.unscaledDeltaTime;
        if (_gauge <= 0f) EndSlow();
    }
    else
    {
        _gauge = Mathf.Min(maxGauge, _gauge + regenRatePerSecond * Time.unscaledDeltaTime);
    }
    GameEvents.GaugeChanged(_gauge / maxGauge);  // normalized 0-1 for UI
}
```

### 4. Floor Management: Two-Slot Pool

**Recommendation: FloorManager maintains exactly two Floor references — `_currentFloor` and `_nextFloor`. On transition, `_currentFloor` is destroyed/disabled, `_nextFloor` becomes `_currentFloor`, and a new `_nextFloor` is spawned.**

```
State before transition:  [_currentFloor=Floor_3] [_nextFloor=Floor_4]
Transition triggered:
  1. Player teleports to Floor_4 entry
  2. Camera pans up
  3. Floor_3 disabled (or destroyed — pooling is premature for prototype)
  4. Floor_5 spawned above Floor_4 → _nextFloor
  5. _currentFloor = Floor_4
```

**Preset selection:** `FloorPreset[]` ScriptableObject array on FloorManager. Random selection with simple anti-repeat (don't pick same preset twice in a row). Do not implement weighted probability for prototype.

**Floor vertical positioning:** Each floor prefab has a standardized height (e.g., 18 units). NextFloor spawns at `_currentFloor.transform.position.y + FloorHeight`. No dynamic height calculation.

**Why destroy instead of pool:** Object pooling adds implementation complexity (reset all enemy states, reset trigger states, reset tile states). For a prototype where only 2 floors exist simultaneously and transitions are infrequent, Instantiate/Destroy is correct. Pool only if profiling shows GC spikes.

---

## Component Communication Map

```
                        [GameEvents static bus]
                               |
     ┌─────────────────────────┼──────────────────────────┐
     |                         |                           |
[PlayerController]    [FloorTransitionManager]       [UIManager]
     |   ^                     |                      (reads events)
     |   |             ┌───────┴──────────┐
     |   |         [FloorManager]   [CameraController]
     |   |                               |
     |   └─── OnCameraPanComplete ────────┘
     |
[AttackController] ──direct──→ [TimeSlowManager]
     |                               |
[RollController]              (GameEvents.GaugeChanged)
     |                               |
     └──────────────────────────[UIManager]

[EnemyController(s)] ←── OnTransitionComplete / OnTransitionBegin (activation)
     |
     └──→ GameEvents.OnEnemyKilled → TimeSlowManager (gauge regen)
```

**Direct references (same GameObject or clear single-owner):**
- PlayerController → AttackController (sibling component, `GetComponent<>` in Awake)
- PlayerController → RollController (sibling component)
- AttackController → TimeSlowManager (singleton reference via `TimeSlowManager.Instance`)
- AttackController → PlayerController (sibling)
- FloorTransitionManager → FloorManager (scene singleton reference)
- FloorTransitionManager → PlayerController (scene singleton reference)
- FloorTransitionManager → CameraController (scene singleton reference)

**Events (cross-ownership, dynamic listener sets):**
- `GameEvents.OnTransitionBegin` → PlayerController (locks input), EnemyController[] (deactivate)
- `GameEvents.OnCameraPanComplete` → FloorTransitionManager (fires next step)
- `GameEvents.OnTransitionComplete` → PlayerController (unlock), EnemyController[] (activate), UIManager (floor++), FloorManager (remove old floor)
- `GameEvents.OnEnemyKilled` → TimeSlowManager (gauge regen)
- `GameEvents.OnPlayerDied` → UIManager (show death screen)
- `GameEvents.GaugeChanged(float)` → UIManager (gauge bar fill)
- `GameEvents.OnFloorChanged(int)` → UIManager (floor counter text)

---

## Build Order (Dependency Graph)

Systems listed in the order they should be implemented. Each phase builds on prior phases being stable.

```
Layer 0 — Infrastructure (no dependencies)
  └── GameEvents.cs (static event bus — no MonoBehaviour dependencies)
  └── PlayerState enum

Layer 1 — Core Player (depends on: GameEvents)
  └── PlayerController
       - Movement, jump, Rigidbody2D integration
       - State machine (enum FSM)
       - Fall detection + last-platform save
       - Input lock/unlock handlers (subscribe to GameEvents)

Layer 2 — Combat Input (depends on: PlayerController, GameEvents)
  └── TimeSlowManager
       - Time.timeScale manipulation
       - Gauge with unscaled drain/regen
       - Fires GaugeChanged, no dependencies on Player or Enemy
  └── AttackController (depends on: PlayerController, TimeSlowManager)
       - Hold/release input
       - Range display
       - Target search (Physics2D.OverlapCircle / CircleCastAll)
       - Initiates dash via PlayerController.BeginDash()
  └── RollController (depends on: PlayerController)
       - Unscaled cooldown
       - Invincibility window

Layer 3 — Enemy (depends on: GameEvents, PlayerController for awareness target)
  └── EnemyController
       - Awareness FSM (Dormant / Aware / Attacking)
       - Subscribes to OnTransitionBegin (deactivate) and OnTransitionComplete (activate)
       - Fires OnEnemyKilled on death

Layer 4 — Floor System (depends on: GameEvents)
  └── FloorManager
       - Preset array, two-slot management
       - Spawn/destroy logic
  └── FloorTransitionManager (depends on: FloorManager, PlayerController, CameraController)
       - Exit trigger detection
       - Orchestrates transition sequence via events + direct calls

Layer 5 — Camera (depends on: GameEvents)
  └── CameraController
       - Player follow (Cinemachine VCam or manual)
       - Subscribes to OnCameraPanBegin
       - Fires OnCameraPanComplete when pan arrives

Layer 6 — UI (depends on: GameEvents only)
  └── UIManager
       - Subscribes to: GaugeChanged, OnFloorChanged, OnPlayerDied, OnTransitionComplete
       - Restart button fires scene reload (SceneManager.LoadScene)
       - Uses Time.unscaledDeltaTime for any UI animations
```

**Critical ordering constraint:** TimeSlowManager must exist before AttackController. FloorTransitionManager must exist after FloorManager AND CameraController. UIManager can be stubbed at any layer (just subscribes to events).

---

## Scalability Considerations

| Concern | Prototype (now) | Post-validation |
|---------|----------------|----------------|
| Enemy count per floor | 2-4 enemies, direct reference list manageable | If > 20 enemies, use spatial query instead of stored list |
| Floor count | Destroy/Instantiate per transition | Object pool if GC spikes on low-end Android |
| Event subscription leak | Manual unsubscribe in OnDestroy | Same — always unsubscribe in OnDestroy or switch to WeakReference events |
| Singleton abuse | TimeSlowManager.Instance acceptable for 1 manager | Extract to dependency injection if managers multiply |

---

## Anti-Patterns to Avoid

### Anti-Pattern 1: Using Animator as Logic FSM
**What:** Driving `PlayerController` state (input gating, invincibility) from Animator state transitions.
**Why bad:** Animator state changes are one-frame delayed; gating logic on visual state causes invincibility windows that are 1 frame off. Debugging is in the Animator window, not code.
**Instead:** Drive Animator from code state. `_animator.SetTrigger("Roll")` when entering ROLLING state. Never read Animator state to make logic decisions.

### Anti-Pattern 2: Time.deltaTime in Roll/UI During Slow-Motion
**What:** Using `Time.deltaTime` for roll cooldown timer or gauge drain rate.
**Why bad:** At `timeScale = 0.2`, `Time.deltaTime` is 5x smaller. A 0.5s cooldown becomes a 2.5s real-time cooldown. A 3-second gauge becomes 15 real seconds.
**Instead:** `Time.unscaledDeltaTime` for all timers that must be real-time. Document which timers use which at the field declaration site.

### Anti-Pattern 3: EnemyController Querying All Enemies via FindObjectsOfType
**What:** FloorTransitionManager calling `FindObjectsOfType<EnemyController>()` to activate/deactivate all enemies.
**Why bad:** Expensive O(n) scene search; called during transition frame; unreliable ordering.
**Instead:** FloorManager owns a `List<EnemyController>` for the current floor's enemies. FloorTransitionManager asks FloorManager for the list. Enemies register themselves with FloorManager on spawn.

### Anti-Pattern 4: Input Lock via Disabling Input System
**What:** Disabling the InputActionAsset or PlayerInput component to lock input during transitions.
**Why bad:** If other systems (e.g., pause menu) also use InputActionAsset, disabling it affects them. Re-enabling requires careful restore.
**Instead:** PlayerController checks `_inputLocked` bool in its input callback methods. Lock/unlock the bool, not the InputActionAsset. InputSystem callbacks still fire — they just early-return.

### Anti-Pattern 5: FloorTransitionManager Coroutine Sequence Fragility
**What:** One long coroutine with hardcoded `WaitForSeconds` for each transition step.
**Why bad:** Adding a step (e.g., screen flash before camera pan) requires modifying the coroutine timing. Steps become implicitly time-coupled.
**Instead:** Each step fires when the previous step's event arrives (event-driven chain, as described in Data Flow above). The only WaitForSeconds in the sequence is if a deliberate pause is needed for feel-tuning.

---

## Sources

**Confidence note:** WebSearch, WebFetch, Exa, Brave, and Firecrawl tools were unavailable during this research session. All findings are based on:

- Unity 6 LTS documentation knowledge (cutoff August 2025) — HIGH confidence for well-established APIs (`Time.timeScale`, `Time.unscaledDeltaTime`, `Physics2D`, `Rigidbody2D`, `Animator`, `SceneManager`)
- Established Unity community patterns (ScriptableObject channels, static event bus, enum FSM) that have been stable since Unity 2019 LTS — HIGH confidence (these patterns predate training cutoff by years and are broadly documented)
- Unity Input System 1.x patterns for action callbacks — HIGH confidence (1.19.0 is in the project, API is stable)

**Areas where external verification would increase confidence:**
- Cinemachine 3.x API specifics for Unity 6 (Cinemachine package version in project not confirmed from STACK.md — verify `com.unity.cinemachine` version before using VirtualCamera2D API)
- Unity 6 specific changes to `Physics2D` query behavior (unlikely to have changed fundamentally)
