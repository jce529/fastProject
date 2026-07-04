# Phase 4: HUD & Game Loop - Research

**Researched:** 2026-06-16
**Domain:** Unity uGUI Canvas, TextMeshProUGUI, Scene Restart, Death Screen
**Confidence:** HIGH — all findings from direct codebase inspection and PackageCache source verification

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| UI-01 | HUD에 현재 층 번호, 시간정지 게이지, 선택한 공격 타입이 표시된다 | Canvas Screen Space Overlay; GaugeController.Value (float, 0-1) already public; AttackTypeSelector.Selected already public static; FloorManager (new, simple int) |
| UI-02 | 플레이어 사망 시 사망 화면과 재시작 버튼이 표시되며, 재시작 시 1층부터 시작한다 | PlayerController.OnPlayerDeath static event (existing); SceneManager.LoadScene(0) restart; Death Canvas SetActive pattern; Time.timeScale must be restored before reload |
</phase_requirements>

---

## Summary

Phase 4 is a pure Unity uGUI integration phase. No new physics, no new enemy logic — only Canvas wiring, TextMeshProUGUI text updates, and a scene reload for restart.

**Critical blocker identified:** TMP Essential Resources have NOT been imported into the project. The `TMP Essential Resources.unitypackage` exists at `Library/PackageCache/com.unity.ugui@f17df9b1ab21/Package Resources/` but has NOT been extracted into `Assets/`. Without this import (`Window > TextMeshPro > Import TMP Essential Resources`), any TextMeshProUGUI component will render a magenta error quad at runtime. This is Wave 0 task 1.

The three HUD elements (floor counter, gauge, attack type label) each have their data source already implemented: `FloorManager.CurrentFloor` (new static int, minimal), `GaugeController.Value` (public float, already on Player), `AttackTypeSelector.Selected` (public static enum, already in scene). The HUD update script (`HUDController`) subscribes to nothing — it reads these in `Update()` and pushes to TextMeshProUGUI/Image.fillAmount. Zero allocation because TMP `SetText("{0}", floatValue)` avoids string concatenation.

Death screen is wired to the existing `PlayerController.OnPlayerDeath` static event (D-15 explicitly reserved this for Phase 4). The handler shows a Canvas (SetActive), pauses time (`Time.timeScale = 0f`), and the restart button calls `SceneManager.LoadScene(0)` after restoring `Time.timeScale = 1f` and `Time.fixedDeltaTime = 0.02f`.

**Primary recommendation:** Decompose into 2 plans: (1) HUD Canvas + HUDController + FloorManager (UI-01), (2) DeathScreenController + restart flow (UI-02). Each plan is independently testable.

---

## Project Constraints (from CLAUDE.md)

- **Tech Stack:** Unity 6 LTS + C# only — no alternative UI frameworks
- **Platform:** Android (ARM64) — UI must use Screen Space Overlay Canvas for mobile
- **Scope:** HUD + death screen + restart only — no animations, no score system, no mobile virtual controls (v2)
- **Performance:** `TextMeshProUGUI.SetText("{0}", value)` — no string allocation in Update
- **Time:** `Time.unscaledDeltaTime` for all timers — but HUD Update is read-only so standard Update() is fine for polling
- **Conventions:** Not yet established — follow existing patterns (static events, MonoBehaviour components, SerializeField wiring)

---

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| com.unity.ugui | 2.0.0 (installed) | Canvas, Image, Button, Slider | Already in Packages/manifest.json |
| TextMeshProUGUI | bundled in ugui 2.0.0 | Zero-alloc text rendering | STATE.md mandates this for HUD text |
| UnityEngine.SceneManagement | built-in module | Scene reload on restart | Only way to reset full game state |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| UnityEngine.UI.Image | bundled in ugui | Gauge fill bar (fillAmount 0-1) | Simpler than Slider for read-only gauge display |
| UnityEngine.UI.Button | bundled in ugui | Restart button | Standard uGUI interactive element |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Image.fillAmount for gauge | Slider component | Slider adds interactive overhead and handle visuals; Image.fillAmount is simpler for read-only display |
| SceneManager.LoadScene(0) | Manual reset (re-enabling objects, resetting positions) | Scene reload is zero-risk: guaranteed clean state. Manual reset would need to touch PlayerController, GaugeController, FloorManager, all enemy states — brittle. Prototype: always reload. |
| TextMeshProUGUI | Legacy UI Text | Legacy Text has no zero-alloc SetText API; STATE.md mandates TMP |

**Installation:** No new packages needed. All required components are in `com.unity.ugui@2.0.0` already installed.

**TMP Essential Resources import (MANDATORY before any TMP works):**
`Window > TextMeshPro > Import TMP Essential Resources`
This unpacks fonts and shaders from:
`Library/PackageCache/com.unity.ugui@f17df9b1ab21/Package Resources/TMP Essential Resources.unitypackage`
into `Assets/TextMesh Pro/`

---

## Architecture Patterns

### Recommended Project Structure
```
Assets/Scripts/UI/
├── AttackTypeSelector.cs      # Existing — Selected static property
├── HUDController.cs           # New — reads GaugeController, FloorManager, AttackTypeSelector
└── DeathScreenController.cs   # New — subscribes to OnPlayerDeath, shows panel, restart

Assets/Scripts/World/
└── FloorManager.cs            # New — minimal static int CurrentFloor = 1
```

### Canvas Hierarchy (both HUD and Death Screen use ONE Canvas)
```
Canvas (Screen Space — Overlay, sort order 0)
├── HUDPanel (GameObject, always active)
│   ├── FloorLabel (TextMeshProUGUI)       — "Floor {0}"
│   ├── GaugeFill (Image, Type=Filled, FillMethod=Horizontal)
│   └── AttackTypeLabel (TextMeshProUGUI)  — "LINEAR" or "FAN"
└── DeathPanel (GameObject, starts inactive)
    └── RestartButton (Button + TextMeshProUGUI child)
```

**Why one Canvas:** Avoids multiple CanvasRenderer draw calls. Screen Space Overlay renders above everything automatically — no camera Z-ordering needed. Sort order is irrelevant with a single Canvas.

### Pattern 1: HUDController — Poll-in-Update, Zero Alloc

```csharp
// Source: STATE.md stack constraint + direct TMP_Text.cs source inspection
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _floorLabel;
    [SerializeField] private Image           _gaugeFill;      // Image.Type = Filled
    [SerializeField] private TextMeshProUGUI _attackTypeLabel;
    [SerializeField] private GaugeController _gauge;          // drag Player in Inspector

    private AttackType _lastAttackType = (AttackType)(-1);   // force first-frame refresh

    private void Update()
    {
        // Floor — SetText with float overload: no string allocation (STATE.md constraint)
        _floorLabel.SetText("Floor {0}", FloorManager.CurrentFloor);

        // Gauge — Image.fillAmount matches GaugeController.Value [0,1]
        _gaugeFill.fillAmount = _gauge.Value;

        // Attack type — only update when changed to avoid TMP re-render every frame
        AttackType current = AttackTypeSelector.Selected;
        if (current != _lastAttackType)
        {
            _lastAttackType = current;
            _attackTypeLabel.SetText(current == AttackType.Linear ? "LINEAR" : "FAN");
        }
    }
}
```

**Why poll instead of event:** No OnValueChanged event exists on GaugeController. Polling in Update is the established pattern in this codebase (CombatController polls GaugeController.Value). One Update call per frame with SetText is negligible.

**SetText allocation note:** `SetText("{0}", floatValue)` uses the float-arg overload (verified in TMP_Text.cs lines 2493-2496). `_floorLabel.SetText("Floor {0}", FloorManager.CurrentFloor)` — `CurrentFloor` is int, but the overload takes float. Cast is implicit. Zero GC per frame.

### Pattern 2: DeathScreenController — Event + Scene Reload

```csharp
// Source: PlayerController.cs (existing static event), EditorBuildSettings.asset (scene index 0)
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{
    [SerializeField] private GameObject _deathPanel;   // starts inactive
    [SerializeField] private Button     _restartButton;

    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += HandleDeath;
        _restartButton.onClick.AddListener(RestartGame);
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDeath -= HandleDeath;
        _restartButton.onClick.RemoveListener(RestartGame);
    }

    private void HandleDeath()
    {
        // Show death panel — D-15: Phase 4 subscribes alongside PlayerDeathHandler (no Phase 3 changes)
        _deathPanel.SetActive(true);

        // Pause world — timeScale may already be 0 from hit-freeze; set explicitly for certainty
        Time.timeScale      = 0f;
        Time.fixedDeltaTime = 0f;
    }

    private void RestartGame()
    {
        // CRITICAL: restore time before reload — otherwise next Play session starts paused
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;

        // FloorManager reset: handled automatically by scene reload (static field resets on domain reload)
        // NOTE: if domain reload is disabled in Editor settings, FloorManager.CurrentFloor must be reset here manually.
        FloorManager.CurrentFloor = 1;

        SceneManager.LoadScene(0);  // SampleScene is build index 0 (verified EditorBuildSettings.asset)
    }
}
```

### Pattern 3: FloorManager — Minimal Static (No MonoBehaviour Needed)

```csharp
// New file: Assets/Scripts/World/FloorManager.cs
// UI-01: provides "current floor number" for HUD
// v2 floor progression will update this value when floor changes
// Phase 4 scope: always floor 1, never changes (single-room prototype)

public static class FloorManager
{
    public static int CurrentFloor = 1;
}
```

**Why static class:** No MonoBehaviour needed. Phase 4 always shows "Floor 1" — prototype is a single room. v2 floor system (FLOOR-01) will write to this field. Zero coupling: HUDController reads it, v2 FloorProgressor writes it.

### Anti-Patterns to Avoid

- **string.Format or "$" interpolation in Update:** Never. `$"Floor {floor}"` allocates every frame. Use `SetText("Floor {0}", value)`.
- **Destroying and re-creating Canvas objects on death:** Never. Toggle SetActive(false/true) only.
- **Time.timeScale = 0f without matching fixedDeltaTime = 0f:** Never (STATE.md constraint). The pair must always be set together.
- **Loading scene without restoring timeScale:** If timeScale=0f at the moment of `SceneManager.LoadScene()`, the new scene starts paused. Always restore first.
- **Subscribing OnPlayerDeath in Start():** Use OnEnable/OnDisable pattern (established by PlayerDeathHandler.cs). Static events persist across Play Mode restarts — must unsubscribe in OnDisable.
- **FindObjectOfType in Update for gauge reference:** Cache `GaugeController` reference via SerializeField, not runtime Find.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Gauge bar | Custom mesh / GL drawing | `Image.fillAmount` (uGUI) | Built-in, hardware-accelerated, Inspector-configurable |
| No-alloc text update | `text = $"Floor {n}"` | `SetText("{0}", value)` | TMP internal char buffer — zero GC per call |
| Game restart | Manual state reset on all components | `SceneManager.LoadScene(0)` | Guaranteed clean state; prototype doesn't need to preserve any state |
| Death screen show/hide | Instantiate/Destroy | `SetActive(false/true)` on existing panel | Instantiate on death causes GC spike + 1-frame delay |
| Button interaction | Custom input polling | `Button.onClick` listener | Standard uGUI — touch and mouse both work on Android |

---

## Common Pitfalls

### Pitfall 1: TMP Missing Essential Resources
**What goes wrong:** TextMeshProUGUI components render magenta (pink error) quads in Play Mode. No text visible.
**Why it happens:** TMP requires font atlases and shaders from `TMP Essential Resources.unitypackage`, which must be imported once per project. The package exists in the cache but has NOT been extracted into `Assets/`.
**How to avoid:** Wave 0 task: `Window > TextMeshPro > Import TMP Essential Resources`. Creates `Assets/TextMesh Pro/` folder with default font, sprite assets, and shaders.
**Warning signs:** Magenta UI elements in Game view. Console: "You are using a TextMeshPro component without a TMP Settings asset."

### Pitfall 2: timeScale = 0 at Scene Load
**What goes wrong:** Player taps restart → `SceneManager.LoadScene(0)` is called while `Time.timeScale = 0f`. New scene begins but physics and coroutines never advance. Game appears frozen on reload.
**Why it happens:** The death handler sets `timeScale = 0f`. If restart is called before restoring time, Unity carries the timeScale into the next scene.
**How to avoid:** Always call `Time.timeScale = 1f; Time.fixedDeltaTime = 0.02f;` BEFORE `SceneManager.LoadScene()`.

### Pitfall 3: Static Event Double-Subscription
**What goes wrong:** After Play Mode restart (without domain reload), `DeathScreenController.OnEnable()` subscribes again. `OnPlayerDeath` fires twice — death screen shows twice, potentially calling `RestartGame` twice.
**Why it happens:** `PlayerController.OnPlayerDeath` is `static` — it persists across Play Mode cycles if domain reload is disabled.
**How to avoid:** Always subscribe in `OnEnable`, unsubscribe in `OnDisable`. This is the established pattern from `PlayerDeathHandler.cs`. Never subscribe in `Start()` or `Awake()`.

### Pitfall 4: FloorManager Static Field Not Reset on Reload
**What goes wrong:** Player dies, restarts. Scene reloads. But `FloorManager.CurrentFloor` still shows the old value (if somehow modified) because static fields survive scene loads in Unity unless domain reload is active.
**Why it happens:** `static` fields in C# are not reset by scene load — only by domain reload (which is enabled by default in Unity Editor, but NOT in builds).
**How to avoid:** Explicitly set `FloorManager.CurrentFloor = 1` inside `RestartGame()` before calling `SceneManager.LoadScene(0)`. In Phase 4 this is always 1, so the reset is trivial but must be explicit.

### Pitfall 5: Death Panel Appears Behind Game Objects
**What goes wrong:** Death panel Canvas is visible in the hierarchy but obscured by game sprites in Game view.
**Why it happens:** Screen Space Overlay is highest render order by default, but if the Canvas is set to Screen Space Camera or World Space, sprites may render in front.
**How to avoid:** Canvas Render Mode must be `Screen Space - Overlay`. This renders on top of all cameras unconditionally. Verify in Inspector when creating the Canvas.

### Pitfall 6: GaugeController Reference Lost After Restart
**What goes wrong:** After restart (scene reload), `HUDController._gauge` is null — `NullReferenceException` in Update.
**Why it happens:** Scene reload destroys ALL GameObjects and recreates them. The serialized reference is preserved because it's in the scene — BUT only if `HUDController` and `GaugeController` are BOTH in the same scene (which they are). This is actually safe. The risk is if the reference was obtained via `FindObjectOfType` at runtime.
**How to avoid:** Wire `_gauge` via SerializeField in the Inspector (drag Player onto the field). Scene serialization preserves cross-object references within the same scene.

---

## Code Examples

### HUDController — Full Implementation
```csharp
// Source: STATE.md (SetText constraint), TMP_Text.cs line 2493 (SetText(string, float) verified)
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _floorLabel;
    [SerializeField] private Image           _gaugeFill;
    [SerializeField] private TextMeshProUGUI _attackTypeLabel;
    [SerializeField] private GaugeController _gauge;

    private AttackType _lastType = (AttackType)(-1);

    private void Update()
    {
        _floorLabel.SetText("Floor {0}", FloorManager.CurrentFloor);
        _gaugeFill.fillAmount = _gauge.Value;

        AttackType t = AttackTypeSelector.Selected;
        if (t != _lastType)
        {
            _lastType = t;
            _attackTypeLabel.SetText(t == AttackType.Linear ? "LINEAR" : "FAN");
        }
    }
}
```

### DeathScreenController — Full Implementation
```csharp
// Source: PlayerController.cs (OnPlayerDeath event), EditorBuildSettings.asset (scene 0 = SampleScene)
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{
    [SerializeField] private GameObject _deathPanel;
    [SerializeField] private Button     _restartButton;

    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += HandleDeath;
        _restartButton.onClick.AddListener(RestartGame);
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDeath -= HandleDeath;
        _restartButton.onClick.RemoveListener(RestartGame);
    }

    private void HandleDeath()
    {
        _deathPanel.SetActive(true);
        Time.timeScale      = 0f;
        Time.fixedDeltaTime = 0f;
    }

    private void RestartGame()
    {
        Time.timeScale         = 1f;
        Time.fixedDeltaTime    = 0.02f;
        FloorManager.CurrentFloor = 1;
        SceneManager.LoadScene(0);
    }
}
```

### FloorManager — Minimal Static
```csharp
// Assets/Scripts/World/FloorManager.cs
public static class FloorManager
{
    /// <summary>
    /// Current floor number. Read by HUDController (UI-01).
    /// v2 FloorProgressor will write this on floor transition.
    /// Reset to 1 in DeathScreenController.RestartGame() before scene reload.
    /// </summary>
    public static int CurrentFloor = 1;
}
```

---

## Integration Analysis

### Existing Systems Phase 4 Connects To

| System | File | What Phase 4 Reads/Uses | No Modification Needed? |
|--------|------|--------------------------|-------------------------|
| `PlayerController.OnPlayerDeath` | PlayerController.cs | Subscribe in DeathScreenController.OnEnable | YES — D-15 explicitly reserved this |
| `GaugeController.Value` | GaugeController.cs | Read in HUDController.Update() | YES — `public float Value { get; private set; }` already exposed |
| `AttackTypeSelector.Selected` | AttackTypeSelector.cs | Read in HUDController.Update() | YES — `public static AttackType Selected` already public |
| `PlayerDeathHandler` | PlayerDeathHandler.cs | Coexists alongside DeathScreenController — both subscribe to OnPlayerDeath | YES — Phase 3 comment says "Phase 4's UIManager subscribes alongside this" |

### What Phase 4 Adds (no modifications to existing scripts)

1. `Assets/Scripts/World/FloorManager.cs` — new static class
2. `Assets/Scripts/UI/HUDController.cs` — new MonoBehaviour
3. `Assets/Scripts/UI/DeathScreenController.cs` — new MonoBehaviour
4. Canvas GameObject in SampleScene.unity
5. HUDPanel + DeathPanel as children
6. TMP Essential Resources import (asset import, not code)

### PlayerDeathHandler Coexistence

`PlayerDeathHandler` (Phase 3) sets the Player `GameObject.SetActive(false)` on death. `DeathScreenController` (Phase 4) shows the death panel and sets `timeScale = 0`. Both subscribe to the same event. **Execution order:** Unity fires static event subscribers in subscription order, but order is not guaranteed. This is safe because the two handlers are independent — neither depends on the other having fired first. The player being disabled does not affect the UI panel.

---

## State of the Art

| Old Approach | Current Approach | Impact |
|--------------|------------------|--------|
| `text = $"Floor {n}"` (string allocation) | `SetText("{0}", floatValue)` | Zero GC per frame — critical for 60fps mobile |
| `FindObjectOfType<GaugeController>()` in Update | `SerializeField` Inspector wire | Zero runtime overhead vs. O(n) scene search |
| Legacy UI.Text component | TextMeshProUGUI | Better rendering, zero-alloc API |

---

## Environment Availability

Step 2.6: Phase 4 is purely code + scene hierarchy changes within the existing Unity project. No external CLI tools or services required.

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity 6000.3.11f1 | All | Confirmed (CLAUDE.md) | 6000.3.11f1 | — |
| com.unity.ugui 2.0.0 | Canvas/TMP/Button | Confirmed (Packages/manifest.json) | 2.0.0 | — |
| TMP Essential Resources | TextMeshProUGUI rendering | NOT YET IMPORTED | — | Import via Window > TextMeshPro menu (Wave 0) |
| SceneManagement | Scene reload | Built-in Unity module | built-in | — |

**Missing dependencies with no fallback:**
- TMP Essential Resources must be imported before any TMP component works. Without this, all text fields render magenta error quads. This is a Wave 0 editor action, not a code task.

---

## Validation Architecture

`nyquist_validation: true` in `.planning/config.json`. However, per precedent from Phase 3 research, this project uses editor Play Mode verification rather than NUnit automated tests (quick task `260604-vst` removed the test runner).

### Test Framework Status

| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.6.0 (NUnit) — installed but deliberately bypassed (260604-vst) |
| Config file | None active |
| Quick run command | N/A — editor Play Mode |
| Full suite command | N/A |

### Phase Requirements → Verification Map

| Req ID | Behavior | Test Type | Verification Method |
|--------|----------|-----------|---------------------|
| UI-01 | Floor label shows "Floor 1" | Manual Play Mode | Start game — check HUD top area |
| UI-01 | Gauge fill matches actual slow-mo gauge | Manual Play Mode | Hold attack button — watch fill bar drain in sync |
| UI-01 | Attack type label shows "LINEAR" or "FAN" correctly | Manual Play Mode | Check label matches AttackTypeZone entered |
| UI-02 | Death screen appears within 1 second of player death | Manual Play Mode | Walk into enemy/fall — panel appears, game pauses |
| UI-02 | Restart button visible and tappable | Manual Play Mode | Tap restart button — scene reloads |
| UI-02 | After restart: gauge full, floor counter 1, HUD correct | Manual Play Mode | Restart → verify HUD state immediately after reload |
| UI-02 | Five consecutive die-restart cycles work without dev intervention | Manual Play Mode | Run loop 5 times with stopwatch — no errors, no stalls |

### Wave 0 Gaps

- [ ] Import TMP Essential Resources: `Window > TextMeshPro > Import TMP Essential Resources` — required before any TMP component renders
- [ ] `Assets/Scripts/World/FloorManager.cs` — new file (no prior version)
- [ ] `Assets/Scripts/UI/HUDController.cs` — new file
- [ ] `Assets/Scripts/UI/DeathScreenController.cs` — new file
- [ ] Canvas + HUDPanel + DeathPanel GameObject hierarchy in SampleScene — editor action (human required for scene wiring, Inspector field assignment)

---

## Open Questions

1. **Death screen design: should world be blurred or darkened behind the panel?**
   - What we know: Requirements say "shows a restart button and nothing else required to understand what to do" — minimal is correct
   - What's unclear: Whether a semi-transparent dark overlay Image behind the button is in scope
   - Recommendation: Add a full-screen semi-transparent black Image as a child of DeathPanel (Image, color = 0,0,0,0.7). Zero extra code. Improves readability massively on any background. This is not "polish" — it's legibility.

2. **HUD position on mobile: safe area (notch/punch-hole) handling?**
   - What we know: MOBI-02 (SafeArea) is v2 scope per REQUIREMENTS.md Out of Scope / v2 Requirements
   - What's unclear: Whether a notch-equipped test device would obscure the HUD during Phase 4 testing
   - Recommendation: Anchor HUD elements to screen corners with conservative padding (20px inset from edges). No SafeArea script needed for PC editor testing. Flag for v2.

3. **PlayerDeathHandler.SetActive(false) vs. death screen: which fires first?**
   - What we know: Both subscribe to `OnPlayerDeath`. C# multicast delegate fires in subscription order. PlayerDeathHandler subscribes in OnEnable. DeathScreenController also in OnEnable. Which fires first depends on GameObject activation order.
   - What's unclear: Whether the Player being disabled before DeathScreenController fires causes any issue
   - Recommendation: Not a problem. DeathScreenController only touches the Canvas (`_deathPanel.SetActive(true)` and `Time.timeScale = 0f`) — it does not reference the Player object. The two handlers are fully independent.

---

## Sources

### Primary (HIGH confidence)
- Direct code inspection: `Assets/Scripts/Player/PlayerController.cs` — `OnPlayerDeath` static event, `TriggerDeath()` method confirmed
- Direct code inspection: `Assets/Scripts/Player/GaugeController.cs` — `public float Value { get; private set; }` confirmed
- Direct code inspection: `Assets/Scripts/UI/AttackTypeSelector.cs` — `public static AttackType Selected` confirmed
- Direct code inspection: `Assets/Scripts/Player/PlayerDeathHandler.cs` — Phase 3 comment "Phase 4's UIManager subscribes alongside this" confirms D-15
- Direct source inspection: `Library/PackageCache/com.unity.ugui@f17df9b1ab21/Runtime/TMP/TMP_Text.cs` lines 2493-2496 — `SetText(string, float)` confirmed zero-alloc
- Direct source inspection: `Library/PackageCache/com.unity.ugui@f17df9b1ab21/Runtime/UGUI/UI/Core/Image.cs` lines 495-502 — `fillAmount` property confirmed
- Direct source inspection: `Library/PackageCache/com.unity.ugui@f17df9b1ab21/Runtime/UGUI/UI/Core/Button.cs` — `onClick` UnityEvent confirmed
- `ProjectSettings/EditorBuildSettings.asset` — SampleScene confirmed at build index 0
- `Packages/manifest.json` — `com.unity.ugui@2.0.0` confirmed installed
- `Library/PackageCache/com.unity.ugui@f17df9b1ab21/Package Resources/` — TMP Essential Resources NOT yet imported into Assets/ (no Assets/TextMesh Pro/ folder found)

### Secondary (MEDIUM confidence)
- `.planning/STATE.md` — `TextMeshProUGUI.SetText("{0}", value)` stack constraint, `Time.fixedDeltaTime` pairing constraint
- `.planning/phases/03-enemy-system/03-CONTEXT.md` — D-15 confirmation: "Phase 4에서 UIManager가 OnPlayerDeath 구독"

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all packages verified in PackageCache source
- Architecture: HIGH — patterns derived from existing codebase conventions
- TMP pitfall: HIGH — directly confirmed Assets/ has no TextMesh Pro folder
- Restart via SceneManager: HIGH — build settings verified, single scene confirmed
- Pitfalls: HIGH — derived from direct code reading + established patterns in codebase

**Research date:** 2026-06-16
**Valid until:** Stable — changes only if PlayerController or GaugeController API is modified before planning
