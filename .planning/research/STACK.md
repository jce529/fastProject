# Technology Stack

**Project:** Fast (Unity 2D mobile platformer — slow-motion dash-attack prototype)
**Researched:** 2026-05-27
**Confidence:** MEDIUM-HIGH (Unity 6 LTS, knowledge through Aug 2025. Web verification unavailable; reasoning is from Unity 6 API knowledge + project file inspection.)

---

## Core Systems

### Engine and Renderer

| Technology | Version | Why |
|------------|---------|-----|
| Unity 6000.3.11f1 LTS | Already installed | LTS = stable; no upgrade needed or warranted for a prototype |
| URP 17.3.0 with 2D Renderer | Already configured | 2D Renderer is the only URP path that supports 2D Lights, Shadow Caster 2D, and the Pixel Perfect Camera — all relevant for silhouette art style |
| Scripting Backend: Mono | Default (editor) | IL2CPP required for Android shipping build; switch only at build time, not during prototype development |
| .NET Standard 2.1 / C# 9.0 | Locked by Unity 6 | No action needed |

**IL2CPP note (HIGH confidence):** Android builds must use IL2CPP + ARM64. The project already has `AndroidTargetArchitectures: 2` (ARM64) in ProjectSettings. Enable IL2CPP in Player Settings → Android → Scripting Backend before any device test build. Mono editor builds are fine for iteration.

### Slow-Motion System (Core Mechanic)

This is the most technically nuanced system. Get it wrong and every system in the game breaks subtly.

**Use `Time.timeScale` + paired `Time.fixedDeltaTime` adjustment. Confidence: HIGH.**

The canonical pattern for Unity slow-motion:

```csharp
// Entering slow motion
public void SetSlowMotion(float scale) // e.g., scale = 0.2f
{
    Time.timeScale = scale;
    // CRITICAL: fixedDeltaTime must be scaled identically
    // Default fixedDeltaTime is 0.02f (50Hz). Scale it proportionally.
    Time.fixedDeltaTime = 0.02f * scale;
}

// Restoring normal time
public void RestoreTime()
{
    Time.timeScale = 1f;
    Time.fixedDeltaTime = 0.02f;
}
```

**Why fixedDeltaTime must be adjusted:** FixedUpdate runs on a fixed real-time cadence. If `timeScale = 0.2f` but `fixedDeltaTime` remains at `0.02f`, Physics2D will run at normal speed while visuals slow — creating physics stutter and incorrect collision detection on the player's dash. Adjusting it proportionally keeps physics consistent with visual time.

**Recommended slow-motion scale:** `0.15f–0.25f`. Below `0.1f` you risk FixedUpdate calls dropping to near zero per frame on low-end Android, causing physics tunneling during the subsequent full-speed dash.

**UI/HUD exclusion from slow motion:** The time-stop gauge and on-screen buttons must update at real time. Use `Time.unscaledDeltaTime` in any HUD Update() loop. The Unity Input System 1.19.0 already runs on unscaled time by default — input callbacks fire at real-time cadence regardless of `timeScale`, so hold/release detection works correctly during slow motion.

**Audio pitch correction:** `AudioSource.pitch = Time.timeScale` gives the classic "slowed audio" feel. Set this on any SFX playing during slow motion.

**Do NOT use:** Coroutines with `WaitForSeconds` for slow-motion timing — they scale with `timeScale`. Use `WaitForSecondsRealtime` or `Time.unscaledDeltaTime` counters instead for cooldown timers that should count real time.

### Animation During Slow Motion

**Use Animator's `updateMode = AnimatorUpdateMode.Normal` (default) for player and enemies.** This means animations automatically slow with `timeScale`, which is exactly correct — you want the enemy's attack-windup animation to slow down visibly. No special configuration needed.

**Exception:** UI Animator components (if any) must be set to `AnimatorUpdateMode.UnscaledTime`.

---

## Input

### System

**Use Unity Input System 1.19.0 exclusively. Do NOT use `Input.GetKey()` / `Input.GetAxis()` (legacy). Confidence: HIGH.**

The action map `Assets/InputSystem_Actions.inputactions` already defines: Move (Vector2), Jump (Button), Attack (Button), Look (Vector2), Interact (Hold), Crouch. The Attack action is the direct hook for the slow-motion mechanic.

**Binding approach for attack hold/release:**

```csharp
// In PlayerController.cs — use the generated C# wrapper or direct callbacks
private InputAction _attackAction;

void Awake()
{
    var inputActions = new InputSystem_Actions(); // generated wrapper
    _attackAction = inputActions.Player.Attack;
    _attackAction.started  += _ => OnAttackHeld();   // button pressed
    _attackAction.canceled += _ => OnAttackReleased(); // button released
    inputActions.Player.Enable();
}
```

`started` fires on press. `canceled` fires on release. This is the correct pair for the hold-to-aim, release-to-dash pattern. Do NOT use `performed` for this mechanic — `performed` fires after an interaction completes (context-dependent) and creates race conditions with the slow-motion state.

### Mobile On-Screen Controls

**Use Unity Input System's built-in On-Screen Controls package (included in com.unity.inputsystem 1.19.0). Confidence: HIGH.**

- `OnScreenButton` component: attach to a UI Canvas Button, set Control Path to `<Gamepad>/buttonSouth` or the action binding path. This synthesizes gamepad input that routes through the action map — no separate mobile code path needed.
- `OnScreenStick` component: for the movement joystick. Use the Analog/Dynamic mode (the stick recenters to where the thumb touches, not a fixed screen position) for better mobile feel.

**Why this approach over TouchInput API directly:** The Input System on-screen controls route through the existing action map. The same `_attackAction.started / canceled` callbacks work identically for physical buttons (editor testing) and on-screen touch (device). No platform-branching code.

**Canvas setup:** Set Canvas Scaler to "Scale With Screen Size", Reference Resolution 1920x1080, Match = 0 (width). Place controls in the lower-left (movement) and lower-right (attack, roll) quadrants. Use anchors to pin them to screen corners so they stay correct at any aspect ratio.

**Do NOT use:** Unity's legacy `Input.touches` API or rolling your own `ITouchable` system. The new Input System handles multi-touch correctly through on-screen controls.

### Roll Action

Add a Roll action (Button type) to the Player action map in the inputactions asset. Wire it exactly like Attack. A Crouch action already exists in the asset — repurpose it or add a distinct Roll action. Repurposing Crouch risks confusion; add a dedicated Roll binding.

---

## Physics

### Rigidbody2D Configuration

**Use Rigidbody2D with `bodyType = Dynamic`, `collisionDetectionMode = CollisionDetectionMode2D.Continuous`. Confidence: HIGH.**

Continuous collision detection is mandatory for the dash-to-enemy mechanic. The player teleports/moves at high velocity during a dash. With Discrete detection, the player can tunnel through thin platforms or miss the enemy trigger collider at high speed.

```
Rigidbody2D settings (set in Inspector or code):
- Body Type: Dynamic
- Collision Detection: Continuous
- Interpolation: Interpolate  (smooths visual position between FixedUpdate steps)
- Freeze Rotation Z: YES  (prevent unwanted rotation on wall clips)
- Gravity Scale: 2.5–3.5  (fast-fall feel; default 1.0 is too floaty for action platformers)
```

**Gravity scale recommendation:** The default Physics2D gravity is -9.81. With `gravityScale = 3.0`, the player feels snappy. Tune `jumpForce` to compensate. This is a prototype — wire both to public fields and tune during play testing.

### Ground Detection

**Use a small CircleCast or OverlapCircle at the player's feet. Do NOT use OnCollisionStay2D. Confidence: HIGH.**

```csharp
bool IsGrounded()
{
    return Physics2D.OverlapCircle(
        _groundCheck.position,   // empty child Transform at feet
        0.1f,                    // radius
        _groundLayer             // LayerMask: "Ground"
    );
}
```

Call this in `FixedUpdate`. OnCollisionStay2D is unreliable for jump detection because it does not re-fire every frame when the velocity is zero (sleeping rigidbody) and gives false negatives at the start of a frame.

### Movement Implementation

**Apply horizontal movement by setting `Rigidbody2D.linearVelocity` directly, not by AddForce. Confidence: HIGH.**

```csharp
void FixedUpdate()
{
    float targetVelocityX = _moveInput.x * _moveSpeed;
    _rb.linearVelocity = new Vector2(targetVelocityX, _rb.linearVelocity.y);
}
```

This gives immediate directional response with no acceleration curve — appropriate for a fast action platformer. AddForce with linear drag produces "slide" which fights the snappy feel the game requires. If you want a subtle acceleration curve later, lerp the targetVelocityX toward the current velocity, but start without it.

**Note on `linearVelocity` vs `velocity`:** In Unity 6 (Physics2D API), the property is `Rigidbody2D.linearVelocity` (renamed from `velocity` to match Physics 3D naming conventions in Unity 6). Use `linearVelocity` — `velocity` still compiles but is marked obsolete.

### Dash-to-Enemy (Attack Dash)

**Use `Rigidbody2D.MovePosition()` for the dash movement, not teleporting transform.position. Confidence: MEDIUM.**

```csharp
IEnumerator DashToTarget(Vector2 targetPosition)
{
    float elapsed = 0f;
    Vector2 startPos = _rb.position;
    while (elapsed < _dashDuration)
    {
        elapsed += Time.unscaledDeltaTime; // dash runs at real speed even if time is restored during it
        float t = elapsed / _dashDuration;
        _rb.MovePosition(Vector2.Lerp(startPos, targetPosition, dashCurve.Evaluate(t)));
        yield return new WaitForFixedUpdate();
    }
    _rb.MovePosition(targetPosition);
}
```

`MovePosition` respects the physics simulation and triggers collision callbacks correctly — the player's invincibility frame logic via `Physics2D.IgnoreLayerCollision` will work. Direct `transform.position` assignment bypasses the physics engine and can cause missed collision events.

**Use `Time.unscaledDeltaTime` here:** The dash should execute at full real-time speed (it follows the slow-motion aiming phase). Even if the caller restores `timeScale = 1f` before starting the coroutine, using `unscaledDeltaTime` is safer against race conditions in the state machine.

### Collision Layer Matrix

No layers are defined yet (TagManager shows only Default). Define these layers explicitly before writing any collision code. Confidence: HIGH (this is a structural decision, not a Unity API uncertainty).

**Recommended layer setup:**

| Layer | Index | Purpose |
|-------|-------|---------|
| Default | 0 | Unused gameplay objects |
| Player | 8 | Player character body |
| PlayerHurtbox | 9 | Receives enemy attacks |
| PlayerInvincible | 10 | Active during dash and roll (swapped from PlayerHurtbox) |
| Enemy | 11 | Enemy bodies |
| EnemyProjectile | 12 | Ranged enemy bullets |
| Ground | 13 | Platforms and floor |
| AttackRange | 14 | Trigger collider showing attack detection area |
| UI | 5 | (already exists) |

**Collision matrix rules:**

| | Player | PlayerHurtbox | PlayerInvincible | Enemy | EnemyProjectile | Ground | AttackRange |
|---|---|---|---|---|---|---|---|
| Player | OFF | — | — | OFF | OFF | ON | OFF |
| PlayerHurtbox | — | — | — | ON (enemy melee) | ON | OFF | OFF |
| PlayerInvincible | — | — | — | OFF | OFF | OFF | OFF |
| Enemy | — | — | — | OFF | OFF | ON | — |
| EnemyProjectile | — | — | — | OFF | OFF | ON | — |
| Ground | ON | — | — | ON | ON | OFF | OFF |
| AttackRange | — | — | — | ON (target detection) | OFF | OFF | OFF |

**Invincibility frame implementation:** Swap the player's active collider layer between `PlayerHurtbox` (normal) and `PlayerInvincible` (during dash / roll). The collision matrix ensures `PlayerInvincible` collides with nothing harmful. This is cleaner than `Physics2D.IgnoreLayerCollision` calls at runtime, which modify global state.

### Physics2D Settings to Adjust

Current defaults from `ProjectSettings/Physics2DSettings.asset`:

- `m_VelocityIterations: 8` — acceptable, leave as-is
- `m_PositionIterations: 3` — increase to 6 for more accurate continuous collision during dash
- `m_SimulationMode: 0` — this is `SimulationMode2D.FixedUpdate` (default). Leave as-is; do not switch to Update mode
- `useMultithreading: 0` — leave off. Multithreaded physics in Unity 6's Box2D fork is stable but adds complexity; unnecessary for this scope
- `m_AutoSyncTransforms: 0` — correct. Manual sync only. Never set to true for a physics-driven game

---

## Performance

### Object Pooling

**Use Unity 6's built-in `ObjectPool<T>` (UnityEngine.Pool namespace). Do NOT implement a custom pool. Confidence: HIGH.**

```csharp
using UnityEngine.Pool;

private ObjectPool<EnemyProjectile> _projectilePool;

void Awake()
{
    _projectilePool = new ObjectPool<EnemyProjectile>(
        createFunc:    () => Instantiate(_projectilePrefab),
        actionOnGet:   p  => p.gameObject.SetActive(true),
        actionOnRelease: p => p.gameObject.SetActive(false),
        actionOnDestroy: p => Destroy(p.gameObject),
        collectionCheck: true,  // set false in release builds
        defaultCapacity: 10,
        maxSize: 50
    );
}
```

Pool candidates in this game: enemy projectiles, hit-effect particles, enemy spawn instances. The floor/platform preset instances use a different pattern (see Level Architecture below).

### Level Streaming (Floor Management)

The game keeps current floor + next floor only. Implement this with `GameObject.SetActive(false)` on the departing floor rather than `Destroy` + `Instantiate`. Rationale: instantiation of a complex floor (with colliders, tilemaps, enemies) causes a frame spike. SetActive(false) costs almost nothing and the memory stays allocated. Since only 2 floors are ever active, the fixed memory overhead is acceptable and eliminates GC pressure.

Destroy the floor two levels behind after the transition completes (not immediately) so there is always a safety buffer during the camera animation.

### Draw Call Budget

URP 2D Renderer with silhouette graphics means all objects share similar materials. Enable **Sprite Atlas** grouping: pack all silhouette sprites into a single Sprite Atlas asset. This reduces draw calls from N (one per unique sprite) to 1 per atlas page. On Android mid-range (2024), target 30 draw calls maximum per frame.

**Dynamic batching is not available for URP 2D SpriteRenderer** (it requires a mesh with compatible vertex format). GPU Instancing does not apply to 2D sprites. The only effective batching mechanism is the Sprite Atlas + same material/sort layer grouping that the SRP Batcher handles automatically.

### Particle VFX Constraint

Use the built-in Particle System (Shuriken) for hit effects and attack range indicators. Do NOT use VFX Graph — it requires Compute Shader support which is not guaranteed on Android minSdk 25 (Vulkan/Compute is available on Android 7.0+ in theory but not universally reliable below ARM Mali G52). Keep particle counts below 50 per burst on mid-range devices.

### Target Frame Rate

```csharp
// In a GameManager or Application initializer
Application.targetFrameRate = 60;
// Do NOT set QualitySettings.vSyncCount for mobile — it has no effect on Android/iOS
// (vSync is controlled by the OS display compositor)
```

The QualitySettings.asset shows `vSyncCount: 0` for Android's "Medium" quality level — that is correct. `Application.targetFrameRate = 60` is the correct mobile cap.

### Memory: Scripting Backend

Android build must use **IL2CPP** (not Mono). IL2CPP strips unused managed code and compiles to native ARM64. This is mandatory for performance and App Store acceptance. Mono on Android is deprecated by Unity and produces larger, slower builds.

Enable: Player Settings → Android → Other Settings → Scripting Backend → IL2CPP.
Enable Managed Stripping Level → Medium (High risks stripping reflection-used types).

---

## What to Avoid

### Do NOT use Coroutines for game-state logic driven by slow-motion

Coroutines with `yield return null` advance on frame cadence, which scales with `timeScale`. The slow-motion state machine (entered on Attack press, exited on release or gauge empty) must be driven by callbacks and state fields, not by coroutines waiting a fixed number of frames. Use coroutines only for fire-and-forget animations (e.g., the dash movement path above), not for state ownership.

### Do NOT use the Legacy Input Manager for anything

`Input.GetButtonDown("Attack")` is gone. The project already uses Input System 1.19.0. Mixing the two systems causes undefined behavior (they compete for the same device state). `ProjectSettings/ProjectSettings.asset` should have `activeInputHandler: 2` (Input System only) — verify this. If it shows `1` (Both), switch to `2`.

### Do NOT use Physics2D.gravity scaling for slow motion

Some tutorials suggest reducing gravity as a complement to slow motion. This breaks the dash-to-enemy trajectory calculation (which happens at normal speed) and produces inconsistent arc heights that vary based on how long the player was in slow motion. Use `Time.timeScale` exclusively. Physics2D global gravity stays at -9.81 always.

### Do NOT use 3D Physics components

`Rigidbody` (3D), `Collider` (3D) — none of these. This is a 2D game. All physics is `Rigidbody2D`, `Collider2D`, `Physics2D` queries. The project has both `com.unity.modules.physics` and `com.unity.modules.physics2d` installed. Using 3D physics accidentally (e.g., by selecting the wrong "Add Component" entry) will produce invisible interaction bugs.

### Do NOT use Unity.Netcode or the Multiplayer Center

`com.unity.multiplayer.center` is installed in the manifest. This is a single-player game. Ignore it entirely. Do not add any Netcode for GameObjects (NGO) components.

### Do NOT use Visual Scripting for gameplay logic

`com.unity.visualscripting` 1.9.10 is installed. Game logic belongs in C# MonoBehaviours. Bolt/Visual Scripting adds a reflection-heavy overhead on each node evaluation that is measurable on mobile and makes the code impossible to debug with breakpoints.

### Do NOT pre-implement features that are Out of Scope

The PROJECT.md explicitly excludes: double jump, wall jump, dash button, combat combos, leveling system, boss fights, ranking, ads, IAP. Do not architect for these. No abstract `IAbility` interface, no upgrade system hooks, no network-ready player state. Prototype scope only.

---

## Installed Packages — Usage Guidance

These are in `manifest.json` but may be misused:

| Package | Use in This Game | Caution |
|---------|-----------------|---------|
| com.unity.2d.animation 13.0.4 | Skeletal rigging for character sprites | Only if doing bone-based animation. For silhouette graphics, frame-by-frame via Animator is simpler |
| com.unity.2d.aseprite 3.0.1 | Import Aseprite files directly | Use if sprite assets are authored in Aseprite; skip otherwise |
| com.unity.2d.spriteshape 13.0.0 | Freeform terrain curves | Useful for organic platform edges; overkill for geometric floor presets — use Tilemap instead |
| com.unity.2d.tilemap + extras 6.0.1 | Floor layout construction | Use for the 3–5 preset floor layouts; Rule Tiles from tilemap.extras simplify platform auto-tiling |
| com.unity.timeline 1.8.11 | Scripted sequences | Use for the camera-ascend transition between floors. Avoids hand-coding the timed sequence in a coroutine |
| com.unity.visualscripting | Gameplay | Do not use |
| com.unity.multiplayer.center | Networking | Do not use |

---

## Confidence Assessment

| Area | Confidence | Basis |
|------|------------|-------|
| Time.timeScale + fixedDeltaTime pairing | HIGH | Documented Unity behavior; well-established pattern |
| Input System hold/release callbacks | HIGH | Verified against Input System 1.x API (started/canceled semantics) |
| On-Screen Controls routing through action map | HIGH | Core Input System feature since 1.0 |
| Rigidbody2D.linearVelocity (Unity 6 rename) | HIGH | Unity 6 renamed `velocity` to `linearVelocity` in Physics2D; confirmed in Unity 6 release notes |
| Continuous collision for dash | HIGH | Physics2D API, standard recommendation |
| ObjectPool<T> (UnityEngine.Pool) | HIGH | Introduced in Unity 2021.2, fully stable in Unity 6 |
| PositionIterations recommendation (increase to 6) | MEDIUM | General physics tuning guidance; validate empirically on device |
| Gravity scale 2.5–3.5 recommendation | LOW | Feel-based prototype recommendation; must be tuned to actual artist assets and level scale |
| VFX Graph exclusion on minSdk 25 Android | MEDIUM | Known limitation; Compute Shader support on Android 7.x is hardware-dependent; validate on target device |

---

*Sources: Unity 6 documentation knowledge (Aug 2025 cutoff), direct inspection of project files (`manifest.json`, `Physics2DSettings.asset`, `ProjectSettings.asset`, `TagManager.asset`, `QualitySettings.asset`). Web verification was unavailable during this research session — flag MEDIUM/LOW confidence items for spot-check against https://docs.unity3d.com/6000.0/ before implementation.*
