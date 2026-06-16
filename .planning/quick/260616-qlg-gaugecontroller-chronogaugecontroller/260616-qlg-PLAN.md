---
phase: quick
plan: 260616-qlg
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/Player/GaugeController.cs
  - Assets/Scripts/Player/ChronoGaugeController.cs
  - Assets/Scripts/Player/GaugeController.cs.meta
  - Assets/Scripts/Player/ChronoGaugeController.cs.meta
  - Assets/Scripts/Player/CombatController.cs
  - Assets/Scripts/UI/HUDController.cs
  - .planning/phases/04-hud-game-loop/04-01-PLAN.md
autonomous: true
must_haves:
  truths:
    - "Class name ChronoGaugeController compiles — no build errors"
    - "CombatController and HUDController reference ChronoGaugeController, not GaugeController"
    - "Scene GUID reference intact — old .meta GUID preserved in new .meta file"
  artifacts:
    - path: "Assets/Scripts/Player/ChronoGaugeController.cs"
      provides: "Renamed MonoBehaviour class"
    - path: "Assets/Scripts/Player/ChronoGaugeController.cs.meta"
      provides: "GUID-preserved meta file (GUID: cf439c6a5829dc143838cab5e507036b)"
  key_links:
    - from: "Assets/Scripts/Player/ChronoGaugeController.cs"
      to: "Assets/Scripts/Player/CombatController.cs"
      via: "[RequireComponent(typeof(ChronoGaugeController))] + GetComponent<ChronoGaugeController>()"
    - from: "Assets/Scripts/Player/ChronoGaugeController.cs"
      to: "Assets/Scripts/UI/HUDController.cs"
      via: "[SerializeField] private ChronoGaugeController _gauge"
---

<objective>
Rename the `GaugeController` class and file to `ChronoGaugeController`. Update all references across the codebase. Preserve the .meta GUID so existing scene component bindings remain valid.

Purpose: Branding alignment — the gauge is the "크로노 게이지(Chrono Gauge)", and the class name should reflect it.
Output: ChronoGaugeController.cs (renamed from GaugeController.cs), updated CombatController.cs and HUDController.cs, GUID-intact .meta file.
</objective>

<context>
@D:/새 폴더/Fast/.planning/STATE.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Rename class, update file, preserve GUID</name>
  <files>
    Assets/Scripts/Player/ChronoGaugeController.cs
    Assets/Scripts/Player/ChronoGaugeController.cs.meta
    Assets/Scripts/Player/GaugeController.cs  (DELETE after creating new file)
    Assets/Scripts/Player/GaugeController.cs.meta  (DELETE after creating new meta)
  </files>
  <action>
**Step 1 — Create ChronoGaugeController.cs with renamed class and updated comment:**

Write `Assets/Scripts/Player/ChronoGaugeController.cs` with the full content of the existing `GaugeController.cs`, making exactly these changes:
- Line 4 summary: change "Time-stop gauge" → "크로노 게이지(Chrono Gauge)"
- Line 10: `public class GaugeController` → `public class ChronoGaugeController`
- All internal references remain identical (no other class names inside the file)

Full file content to write:

```csharp
using UnityEngine;

/// <summary>
/// ATCK-05: 크로노 게이지(Chrono Gauge) — drains while Attack is held and regens when released.
/// Exposed as a [0, 1] float for HUD (Phase 4) to read directly.
///
/// All timing uses Time.unscaledDeltaTime — drain rate is independent of timeScale.
/// This means the gauge drains at the same wall-clock speed during slow-motion as in normal time.
/// </summary>
public class ChronoGaugeController : MonoBehaviour
{
    [SerializeField] private float drainPerSecond = 0.25f; // 4 seconds to empty
    [SerializeField] private float regenPerSecond = 0.15f; // ~6.7 seconds to full
    [SerializeField] private float killBonus      = 0.20f; // +20% on kill

    /// <summary>Current gauge value in [0, 1]. Read by HUD (Phase 4) directly.</summary>
    public float Value { get; private set; } = 1f;

    /// <summary>True when the gauge has completely emptied.</summary>
    public bool IsEmpty => Value <= 0f;

    private bool _isDraining;

    /// <summary>Called by CombatController every Update. True while Attack is held.</summary>
    public void SetDraining(bool drain) => _isDraining = drain;

    /// <summary>Called by CombatController after a successful kill. Adds kill bonus to gauge.</summary>
    public void AddKillBonus() => Value = Mathf.Min(1f, Value + killBonus);

    private void Update()
    {
        if (_isDraining)
            Value = Mathf.Max(0f, Value - drainPerSecond * Time.unscaledDeltaTime);
        else
            Value = Mathf.Min(1f, Value + regenPerSecond * Time.unscaledDeltaTime);
    }
}
```

**Step 2 — Create ChronoGaugeController.cs.meta with the ORIGINAL GUID:**

The original GaugeController.cs.meta contains GUID `cf439c6a5829dc143838cab5e507036b`. Write `Assets/Scripts/Player/ChronoGaugeController.cs.meta` with that EXACT GUID:

```yaml
fileFormatVersion: 2
guid: cf439c6a5829dc143838cab5e507036b
MonoImporter:
  externalObjects: {}
  serializedVersion: 2
  defaultReferences: []
  executionOrder: 0
  icon: {instanceID: 0}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
```

**Step 3 — Delete the old files:**

Delete `Assets/Scripts/Player/GaugeController.cs` and `Assets/Scripts/Player/GaugeController.cs.meta`.

CRITICAL: The new .meta GUID must be `cf439c6a5829dc143838cab5e507036b` — this is what SampleScene.unity uses to find the component. Do NOT generate a new GUID.
  </action>
  <verify>
- `Assets/Scripts/Player/ChronoGaugeController.cs` exists, contains `public class ChronoGaugeController`
- `Assets/Scripts/Player/ChronoGaugeController.cs.meta` contains `guid: cf439c6a5829dc143838cab5e507036b`
- `Assets/Scripts/Player/GaugeController.cs` does NOT exist
- `Assets/Scripts/Player/GaugeController.cs.meta` does NOT exist
  </verify>
  <done>ChronoGaugeController.cs is the canonical file with the preserved GUID. Old GaugeController files gone.</done>
</task>

<task type="auto">
  <name>Task 2: Update all referencing scripts</name>
  <files>
    Assets/Scripts/Player/CombatController.cs
    Assets/Scripts/UI/HUDController.cs
    .planning/phases/04-hud-game-loop/04-01-PLAN.md
  </files>
  <action>
**CombatController.cs — make these targeted replacements:**

1. Comment line 11: `Does NOT own the gauge — GaugeController handles drain/regen.`
   → `Does NOT own the gauge — ChronoGaugeController handles drain/regen.`

2. Attribute line 22: `[RequireComponent(typeof(GaugeController))]`
   → `[RequireComponent(typeof(ChronoGaugeController))]`

3. Field declaration line 46: `private GaugeController      _gauge;`
   → `private ChronoGaugeController _gauge;`

4. Awake line 80: `_gauge                = GetComponent<GaugeController>();`
   → `_gauge                = GetComponent<ChronoGaugeController>();`

No other lines in CombatController.cs change.

---

**HUDController.cs — make this targeted replacement:**

Line 10: `[SerializeField] private GaugeController _gauge;`
→ `[SerializeField] private ChronoGaugeController _gauge;`

No other lines in HUDController.cs change.

---

**04-01-PLAN.md — update two references in the task descriptions:**

In T2 `<read_first>` block: 
`Assets/Scripts/Player/GaugeController.cs — confirms \`public float Value { get; private set; }\` (line 17)`
→ `Assets/Scripts/Player/ChronoGaugeController.cs — confirms \`public float Value { get; private set; }\` (line 17)`

In T2 `<action>` code block, line:
`[SerializeField] private GaugeController _gauge;`
→ `[SerializeField] private ChronoGaugeController _gauge;`

In T2 `<acceptance_criteria>`:
`` `[SerializeField] private GaugeController _gauge` present ``
→ `` `[SerializeField] private ChronoGaugeController _gauge` present ``
  </action>
  <verify>
Run grep to confirm no remaining `GaugeController` references in Assets/Scripts/:

  grep -r "GaugeController" "Assets/Scripts/" --include="*.cs"

Expected result: zero matches.
  </verify>
  <done>CombatController.cs and HUDController.cs compile cleanly with ChronoGaugeController. No GaugeController string remains in any .cs file.</done>
</task>

</tasks>

<verification>
1. `Assets/Scripts/Player/ChronoGaugeController.cs` exists — class name `ChronoGaugeController`, summary updated to "크로노 게이지(Chrono Gauge)"
2. `Assets/Scripts/Player/ChronoGaugeController.cs.meta` contains `guid: cf439c6a5829dc143838cab5e507036b` (original GUID preserved)
3. `GaugeController.cs` and `GaugeController.cs.meta` deleted
4. `CombatController.cs`: `[RequireComponent(typeof(ChronoGaugeController))]`, `private ChronoGaugeController _gauge`, `GetComponent<ChronoGaugeController>()`
5. `HUDController.cs`: `[SerializeField] private ChronoGaugeController _gauge`
6. Zero `GaugeController` matches in `grep -r "GaugeController" Assets/Scripts/ --include="*.cs"`
7. Unity reimports the renamed file — no missing script warnings on Player GameObject in scene
</verification>

<success_criteria>
- All code compiles without errors after rename
- Scene Inspector shows ChronoGaugeController on Player with no missing script icon
- Play Mode: gauge drains/regens correctly (behavior unchanged — only names changed)
- "Time-stop gauge" phrase replaced with "크로노 게이지(Chrono Gauge)" in the class summary
</success_criteria>

<output>
After completion, note in STATE.md Quick Tasks table:

| 260616-qlg | GaugeController → ChronoGaugeController 이름 변경 (GUID 보존) | 2026-06-16 | — | [260616-qlg](./quick/260616-qlg-gaugecontroller-chronogaugecontroller/) |
</output>
