---
phase: quick-260601-lzr
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/Player/InputManager.cs
autonomous: true
requirements: []

must_haves:
  truths:
    - "MoveInput (Vector2) returns the current WASD / Arrow axis each frame"
    - "RollPressed (bool) returns true on the frame LeftShift is pressed"
    - "AttackHeld (bool) and AttackReleased (bool) expose the Attack action phase for slow-mo / dash logic"
    - "PlayerController (and future Phase 2 scripts) can read input without touching PlayerInput directly"
  artifacts:
    - path: "Assets/Scripts/Player/InputManager.cs"
      provides: "Singleton input facade reading from the existing InputSystem_Actions asset"
      exports: ["MoveInput", "RollPressed", "AttackHeld", "AttackReleased"]
  key_links:
    - from: "Assets/Scripts/Player/InputManager.cs"
      to: "Assets/InputSystem_Actions.inputactions"
      via: "PlayerInput component — actions[\"Player/Move\"], actions[\"Player/Sprint\"], actions[\"Player/Attack\"]"
      pattern: "playerInput\\.actions\\[\"Player/"
---

<objective>
Create InputManager.cs — a lightweight singleton facade that centralises all player input reading for the "Fast" prototype.

Purpose: Phase 2 (slow-mo + dash + roll) needs to inspect Attack held vs. released and Roll pressed from multiple scripts. Without a facade, each script would need its own PlayerInput reference and duplicated callback wiring. The InputManager eliminates that coupling.

Output: Assets/Scripts/Player/InputManager.cs — a MonoBehaviour singleton that reads from the existing InputSystem_Actions asset and exposes clean properties.
</objective>

<execution_context>
@C:/Users/MSI/Projeect_A.E/fastProject/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@C:/Users/MSI/Projeect_A.E/fastProject/.planning/STATE.md

# Existing input action asset — all bindings already defined:
# Player/Move    → WASD + Arrow keys (Vector2 composite)
# Player/Sprint  → LeftShift (Button) — this IS the Roll trigger
# Player/Attack  → Mouse leftButton + Gamepad buttonWest + Touchscreen tap (Button)
# Player/Jump    → Space (already handled by PlayerController)

<interfaces>
<!-- From Assets/Scripts/Player/PlayerController.cs — existing input pattern to match -->
```csharp
// PlayerController retrieves actions like this — InputManager must use the same pattern:
var playerInput = GetComponent<PlayerInput>();
_moveAction  = playerInput.actions["Player/Move"];
_jumpAction  = playerInput.actions["Player/Jump"];
// Sprint and Attack not yet wired — InputManager owns those
```
</interfaces>
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create InputManager singleton</name>
  <files>Assets/Scripts/Player/InputManager.cs</files>
  <action>
Create Assets/Scripts/Player/InputManager.cs with the following exact design:

**Class:** `public class InputManager : MonoBehaviour`
**Namespace:** none (matches existing scripts)

**Singleton pattern:**
```csharp
public static InputManager Instance { get; private set; }
private void Awake()
{
    if (Instance != null && Instance != this) { Destroy(gameObject); return; }
    Instance = this;
    // Do NOT call DontDestroyOnLoad — prototype stays in one scene
}
```

**Require PlayerInput on the same GameObject:**
```csharp
[RequireComponent(typeof(PlayerInput))]
```

**Private fields (cached in OnEnable, cleared in OnDisable):**
```csharp
private InputAction _moveAction;
private InputAction _sprintAction;   // Sprint in asset = Roll in game
private InputAction _attackAction;
```

**Transient state (reset each frame in Update — NOT in callbacks, to guarantee one-frame lifetime):**
```csharp
private bool _rollPressedThisFrame;
private bool _attackHeldThisFrame;
private bool _attackReleasedThisFrame;
```

**OnEnable — wire callbacks:**
```csharp
var pi = GetComponent<PlayerInput>();
_moveAction   = pi.actions["Player/Move"];
_sprintAction = pi.actions["Player/Sprint"];
_attackAction = pi.actions["Player/Attack"];

_sprintAction.performed += ctx => _rollPressedThisFrame  = true;
_attackAction.performed += ctx => _attackHeldThisFrame   = true;
_attackAction.canceled  += ctx => _attackReleasedThisFrame = true;

_moveAction.Enable();
_sprintAction.Enable();
_attackAction.Enable();
```

**OnDisable — unwire:**
```csharp
_sprintAction.performed -= ...;   // store lambdas in fields to allow removal
_attackAction.performed -= ...;
_attackAction.canceled  -= ...;
```

Because lambdas cannot be unsubscribed directly, store delegates as named private methods instead:
- `private void OnSprintPerformed(InputAction.CallbackContext ctx)  => _rollPressedThisFrame   = true;`
- `private void OnAttackPerformed(InputAction.CallbackContext ctx)  => _attackHeldThisFrame    = true;`
- `private void OnAttackCanceled (InputAction.CallbackContext ctx)  => _attackReleasedThisFrame = true;`

**Update — reset one-frame flags AFTER reading (consumers read in Update or FixedUpdate before this runs, so clear at END of Update):**
```csharp
private void LateUpdate()
{
    _rollPressedThisFrame    = false;
    _attackHeldThisFrame     = false;
    _attackReleasedThisFrame = false;
}
```

**Public read-only properties:**
```csharp
/// <summary>Horizontal/vertical movement axis. x = horizontal only used by PlayerController.</summary>
public Vector2 MoveInput => _moveAction.ReadValue<Vector2>();

/// <summary>True for exactly one frame when LeftShift is pressed (Roll trigger).</summary>
public bool RollPressed => _rollPressedThisFrame;

/// <summary>True for exactly one frame when Attack button is pressed (slow-mo start).</summary>
public bool AttackHeld => _attackHeldThisFrame;

/// <summary>True for exactly one frame when Attack button is released (dash trigger).</summary>
public bool AttackReleased => _attackReleasedThisFrame;

/// <summary>Raw IsPressed state — use for continuous slow-mo scaling in Phase 2.</summary>
public bool IsAttackDown => _attackAction.IsPressed();
```

**XML doc comment on class:**
```csharp
/// <summary>
/// Singleton input facade for Fast prototype.
/// Reads from InputSystem_Actions Player map: Move (WASD), Sprint/LeftShift (Roll), Attack (LMB).
/// All consumers read properties — no direct PlayerInput access needed outside this class.
/// Phase 2 slow-mo and dash logic depends on AttackHeld / AttackReleased / IsAttackDown.
/// </summary>
```

**Do NOT add Jump to InputManager** — PlayerController already owns Jump callbacks and the pattern works. Merging would require refactoring PlayerController (out of scope for this quick task).

**File path:** `Assets/Scripts/Player/InputManager.cs` (same folder as PlayerController.cs — confirmed existing location)
  </action>
  <verify>
    <automated>
      Open project in Unity 6 Editor and check Console for compile errors.
      Alternatively, run: grep -n "public static InputManager Instance" "Assets/Scripts/Player/InputManager.cs" returns line 1 match, confirming singleton property exists.
      Manual smoke test: Add InputManager component to Player GameObject alongside PlayerInput, enter Play mode, press WASD → no console errors, press LeftShift → RollPressed accessible.
    </automated>
  </verify>
  <done>
    InputManager.cs compiles without errors.
    MoveInput returns Vector2 from WASD.
    RollPressed is true for one frame on LeftShift press.
    AttackHeld is true for one frame on LMB press.
    AttackReleased is true for one frame on LMB release.
    IsAttackDown is true continuously while LMB is held.
    PlayerController.cs is NOT modified.
  </done>
</task>

</tasks>

<verification>
1. File exists: `Assets/Scripts/Player/InputManager.cs`
2. Unity Editor Console shows zero compile errors after script import
3. Class has `[RequireComponent(typeof(PlayerInput))]` attribute
4. All five public properties present: `MoveInput`, `RollPressed`, `AttackHeld`, `AttackReleased`, `IsAttackDown`
5. `PlayerController.cs` is unchanged (diff shows no modifications)
</verification>

<success_criteria>
InputManager.cs exists at Assets/Scripts/Player/InputManager.cs, compiles cleanly in Unity 6, and exposes the five input properties listed above. No existing scripts are modified.
</success_criteria>

<output>
No SUMMARY required for quick tasks — this is a standalone script creation.
</output>
