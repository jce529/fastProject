---
phase: "04"
plan: "04-01"
title: "FloorManager + HUDController + Canvas HUD Build (UI-01)"
subsystem: "UI / World"
tags: [hud, ui, floor-manager, tmp, ugui]
status: partial
completed_tasks: 2
total_tasks: 3
dependency_graph:
  requires: []
  provides: [FloorManager, HUDController]
  affects: [SampleScene]
tech_stack:
  added: [TextMeshPro]
  patterns: [static-data-provider, dirty-check-polling, zero-alloc-SetText]
key_files:
  created:
    - Assets/Scripts/World/FloorManager.cs
    - Assets/Scripts/UI/HUDController.cs
  modified: []
  pending_human:
    - Assets/Scenes/SampleScene.unity
decisions:
  - "Static class (no MonoBehaviour) for FloorManager — data-only int needs no scene lifecycle"
  - "(AttackType)(-1) sentinel initializes dirty-check to fire on first Update() frame"
  - "SetText('{0}', int) over string interpolation — TMP internal char buffer, zero GC allocation"
  - "Poll not event for GaugeController — no OnValueChanged exists; polling is established codebase pattern (CombatController)"
metrics:
  duration_minutes: ~5
  completed_date: "2026-06-16"
  tasks_completed: 2
  tasks_total: 3
  files_created: 2
  files_modified: 0
---

# Phase 04 Plan 01: FloorManager + HUDController + Canvas HUD Build Summary

**One-liner:** FloorManager static int + HUDController zero-alloc TMP polling — C# half complete, Canvas hierarchy awaits Unity Editor.

---

## Tasks Completed

| Task | Name | Commit | Files |
|------|------|--------|-------|
| T1 | Create FloorManager static class | 2d800db | Assets/Scripts/World/FloorManager.cs |
| T2 | Create HUDController MonoBehaviour | 2c9af08 | Assets/Scripts/UI/HUDController.cs |

## Tasks Pending (Human)

| Task | Name | Reason |
|------|------|--------|
| T3 | Import TMP Essential Resources + build Canvas HUD hierarchy | Requires Unity Editor GUI interaction |

---

## What Was Built

### FloorManager.cs (T1)

Static class at `Assets/Scripts/World/FloorManager.cs`. Single public field `CurrentFloor = 1`. No MonoBehaviour, no namespace. Phase 4 scope always stays Floor 1 — DeathScreenController (Plan 04-02) will reset this on restart; a future v2 FloorProgressor will increment on floor transition.

### HUDController.cs (T2)

MonoBehaviour at `Assets/Scripts/UI/HUDController.cs`. Four `[SerializeField]` fields:
- `_floorLabel` (TextMeshProUGUI) — floor counter
- `_gaugeFill` (Image) — filled image for gauge bar
- `_attackTypeLabel` (TextMeshProUGUI) — LINEAR/FAN display
- `_gauge` (GaugeController) — reads `.Value` property

Update() behaviour:
1. `_floorLabel.SetText("Floor {0}", FloorManager.CurrentFloor)` — TMP's int overload, zero allocation
2. `_gaugeFill.fillAmount = _gauge.Value` — direct float assignment
3. Dirty-check: only calls `_attackTypeLabel.SetText(...)` when `AttackTypeSelector.Selected` changes — no TMP re-render every frame

---

## Checkpoint: T3 — Human Editor Task

**Type:** checkpoint:human-verify  
**Blocked by:** Unity Editor GUI — cannot be automated

### Required Human Steps

**Step 0 — Import TMP Essential Resources (MUST DO FIRST):**
- Unity Editor: `Window > TextMeshPro > Import TMP Essential Resources`
- Verify `Assets/TextMesh Pro/` folder appears in Project window
- Without this, all TMP components render magenta error quads

**Step 1 — Canvas:** Right-click Hierarchy → UI → Canvas
- Render Mode: Screen Space - Overlay
- CanvasScaler: Scale With Screen Size, 1920x1080, Match 0.5

**Step 2 — HUDPanel:** Child empty GameObject on Canvas
- RectTransform: stretch full (anchor min=(0,0) max=(1,1), offsets zero)
- No Image. Always active.

**Step 3 — FloorGroup (top-left):** Empty child on HUDPanel
- Anchor: Top-Left, Pivot (0,1), PosX=24, PosY=-24, W=160, H=44
- Image backing: Color (0,0,0, alpha=0.55)
- Child: FloorLabel TMP Text, "Floor 1", size 28, Bold, White, Middle-Left

**Step 4 — GaugeGroup (top-center):** Empty child on HUDPanel
- Anchor: Top-Center, Pivot (0.5,1), PosX=0, PosY=-24, W=216, H=32
- Image backing: Color (0,0,0, alpha=0.55)
- Child GaugeTrack Image: #222222, W=200, H=16, centered
- Child GaugeFill Image: Type=Filled, FillMethod=Horizontal, FillOrigin=Left, FillAmount=1, White, same size as GaugeTrack

**Step 5 — AttackTypeGroup (top-right):** Empty child on HUDPanel
- Anchor: Top-Right, Pivot (1,1), PosX=-24, PosY=-24, W=140, H=44
- Image backing: Color (0,0,0, alpha=0.55)
- Child: AttackTypeLabel TMP Text, "LINEAR", size 28, Bold, White, Middle-Right

**Step 6 — Wire HUDController:**
- Select HUDPanel → Add Component → HUDController
- `_floorLabel` → FloorLabel
- `_gaugeFill` → GaugeFill
- `_attackTypeLabel` → AttackTypeLabel
- `_gauge` → Player's GaugeController component
- Save scene (Ctrl+S)

**Step 7 — Play Mode Verify:**
- [ ] "Floor 1" visible top-left with semi-transparent backing
- [ ] Gauge fill bar visible top-center, starts full
- [ ] "LINEAR" visible top-right
- [ ] Hold Attack — gauge bar drains in real-time
- [ ] No magenta error quads in Game view
- [ ] No TMP errors in Console

---

## Deviations from Plan

None — T1 and T2 executed exactly as specified. T3 is a planned human checkpoint, not a deviation.

---

## Known Stubs

None. FloorManager.CurrentFloor is intentionally hardcoded to 1 for Phase 4 scope (single-room prototype). This is documented in the plan as by-design; v2 FloorProgressor will increment it.

---

## Self-Check

- [x] `Assets/Scripts/World/FloorManager.cs` created
- [x] `Assets/Scripts/UI/HUDController.cs` created
- [x] Commit 2d800db exists (T1)
- [x] Commit 2c9af08 exists (T2)
- [ ] T3 pending human action — Canvas hierarchy not yet in SampleScene.unity
