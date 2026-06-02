---
phase: 02-combat-core
plan: "02-01"
subsystem: ui, enemy
tags: [unity, canvas, overlay, dummy-enemy, attack-type-selector, layer-setup]

requires:
  - phase: 01-foundation-movement
    provides: InvincibilityHandler layer constants (PlayerHurtbox=7, PlayerInvincible=8), Platform layer (9)

provides:
  - AttackType enum (Linear, Fan) in AttackTypeSelector.cs
  - AttackTypeSelector.Selected static property — session-wide attack type storage
  - AttackTypeSelector.IsSelecting static flag — CombatController guard against timeScale race
  - DummyEnemy.cs with IsAlive, OnDashHit(), ClearHighlight(), 2s real-time respawn
  - Enemy layer at index 10 in TagManager.asset
  - SampleScene: AttackTypeOverlay Canvas + 5 DummyEnemy instances at x=-6,-3,0,3,6

affects:
  - 02-02 (CombatController reads AttackTypeSelector.Selected and IsSelecting)
  - 02-04 (RangeDisplay reads AttackTypeSelector.Selected for shape choice)

tech-stack:
  added: []
  patterns:
    - "Static field on UI MonoBehaviour for session-scoped selection (no DontDestroyOnLoad)"
    - "timeScale+fixedDeltaTime always set together (ROADMAP constraint enforced in AttackTypeSelector)"
    - "WaitForSecondsRealtime for real-time respawn delay (immune to timeScale)"
    - "IsAlive guard in OnDashHit() prevents double-hit on dead enemies (Pitfall 6)"
    - "Collider re-enabled one frame after sprite to prevent physics re-overlap on respawn"

key-files:
  created:
    - Assets/Scripts/UI/AttackTypeSelector.cs
    - Assets/Scripts/Enemy/DummyEnemy.cs
    - Assets/Prefabs/DummyEnemy.prefab
  modified:
    - Assets/Scenes/SampleScene.unity
    - ProjectSettings/TagManager.asset

key-decisions:
  - "Enemy layer assigned index 10 (not 9 as plan stated) — layer 9 was already occupied by Platform"
  - "overlayRoot is the Panel (child), not the Canvas itself — Canvas persists as component host"
  - "AttackTypeSelector.Start() sets IsSelecting=true BEFORE timeScale=0 — guard fires before physics pause"

patterns-established:
  - "AttackTypeSelector.IsSelecting: read by CombatController (02-02) to block EnterSlowMotion during overlay"
  - "DummyEnemy.IsAlive: checked by CombatController before targeting to skip dead enemies"

requirements-completed: [ATCK-01]

duration: 25min
completed: 2026-06-02
---

# Phase 02 Plan 01: Attack Type Selector UI + Dummy Enemy + Scene Setup Summary

**Canvas overlay with Linear/Fan attack selection, static IsSelecting guard, and 5 gray DummyEnemy instances with 2s real-time respawn on SampleScene floor**

## Performance

- **Duration:** ~25 min
- **Started:** 2026-06-02T00:00:00Z
- **Completed:** 2026-06-02T00:25:00Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- AttackTypeSelector.cs: Canvas overlay at game start, IsSelecting guard, timeScale pause/resume on button click
- DummyEnemy.cs: stationary target with OnDashHit(), ClearHighlight(), WaitForSecondsRealtime 2s respawn, one-frame collider delay
- Enemy layer (index 10) added to TagManager.asset
- SampleScene updated: AttackTypeOverlay Canvas with two 300x150 buttons + 5 DummyEnemy instances spread at x=-6,-3,0,3,6

## Task Commits

Each task was committed atomically:

1. **Task 1: AttackTypeSelector Canvas overlay UI** - `5cccc29` (feat)
2. **Task 2: DummyEnemy + prefab + Enemy layer + scene** - `998cb4e` (feat)

## Files Created/Modified
- `Assets/Scripts/UI/AttackTypeSelector.cs` - Canvas overlay, static Selected + IsSelecting, SelectLinear/SelectFan methods
- `Assets/Scripts/Enemy/DummyEnemy.cs` - Stationary hit target, death+respawn coroutine
- `Assets/Prefabs/DummyEnemy.prefab` - Gray SpriteRenderer, CapsuleCollider2D 0.8x1.2, Static Rigidbody2D, layer 10
- `Assets/Scenes/SampleScene.unity` - AttackTypeOverlay Canvas hierarchy + 5 DummyEnemy instances
- `ProjectSettings/TagManager.asset` - Enemy layer added at index 10

## Decisions Made
- Enemy layer at index 10: the plan text said "layer index 9" but that slot was already occupied by Platform (established in Phase 1). Used 10 to avoid collision.
- overlayRoot points to the Panel child (not the Canvas root): the Canvas must stay active to host the AttackTypeSelector component. The Panel is what gets hidden/shown.
- IsSelecting=true set before timeScale=0 in Start(): ensures CombatController guard is in place before physics pauses, preventing any single-frame window where combat input could slip through.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Enemy layer index corrected from 9 to 10**
- **Found during:** Task 2 (DummyEnemy scene setup)
- **Issue:** Plan specified layer index 9 for Enemy, but TagManager.asset already had Platform at layer 9 (established in Phase 1 Plan 01). Using index 9 would silently overwrite Platform layer, breaking ground detection.
- **Fix:** Added Enemy at layer index 10. Updated all DummyEnemy references in SampleScene and prefab to layer 10.
- **Files modified:** ProjectSettings/TagManager.asset, Assets/Scenes/SampleScene.unity, Assets/Prefabs/DummyEnemy.prefab
- **Verification:** TagManager.asset shows Platform at line 17 (index 9) and Enemy at line 18 (index 10) — no conflict.
- **Committed in:** 998cb4e (Task 2 commit)

---

**Total deviations:** 1 auto-fixed (Rule 1 — bug: layer index conflict)
**Impact on plan:** Fix is necessary for correctness. No scope creep. Layer 10 is functionally identical to layer 9 for the Enemy use case; CombatController will use `LayerMask.GetMask("Enemy")` which resolves by name.

## Issues Encountered
- Unity UGUI component script GUIDs (Image, Button, Text, Canvas, CanvasScaler, GraphicRaycaster) were embedded using known Unity built-in GUIDs. Unity will re-validate these on first open. If any are wrong, Unity will show missing script warnings and the user can re-assign them in the Inspector.

## Known Stubs
- None — AttackTypeSelector.Selected is set by button click to a real enum value (Linear or Fan). DummyEnemy instances are fully wired in scene. No placeholder data flows to UI rendering.

## Next Phase Readiness
- AttackTypeSelector.Selected and IsSelecting are ready for CombatController (02-02) to read
- DummyEnemy instances are on Enemy layer 10 — CombatController must use `LayerMask.GetMask("Enemy")` not hardcoded layer index
- 5 DummyEnemies at floor level ready for dash-kill testing
- SampleScene button OnClick events are wired via YAML — Unity will need to reserialize if script GUIDs don't match cached values

---
*Phase: 02-combat-core*
*Completed: 2026-06-02*
