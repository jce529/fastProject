---
quick: 260624-e5y
phase: quick
subsystem: scene-flow
tags: [bootstrapper, scene-management, runtime-initialize]
dependency_graph:
  requires: [MainMenu scene at Build index 0]
  provides: [automatic MainMenu redirect on any Play]
  affects: [SampleScene, AttackSelect — both now redirect to MainMenu on Play]
tech_stack:
  added: []
  patterns: [RuntimeInitializeOnLoadMethod(BeforeSceneLoad), static class bootstrapper]
key_files:
  created:
    - Assets/Scripts/World/GameBootstrapper.cs
  modified: []
decisions:
  - Static class with no MonoBehaviour — matches FloorManager pattern in World/; no scene coupling needed
  - BeforeSceneLoad load type — only valid choice; AfterSceneLoad fires after scene is already active
  - Name check via SceneManager.GetActiveScene().name — guards against infinite reload when already on MainMenu
metrics:
  duration: "<5 min"
  completed: "2026-06-24"
  tasks_completed: 1
  files_changed: 1
---

# Quick Task 260624-e5y: GameBootstrapper Summary

**One-liner:** `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` static bootstrapper that redirects any Play session to MainMenu unless already there.

---

## Tasks Completed

| # | Task | Commit | Files |
|---|------|--------|-------|
| 1 | GameBootstrapper.cs 생성 | 90f9e2c | Assets/Scripts/World/GameBootstrapper.cs |

---

## What Was Built

`Assets/Scripts/World/GameBootstrapper.cs` — a static class with a single `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]` method `EnsureMainMenu()`.

Logic:
- Runs before any scene is loaded, every time Play is pressed in the Editor or at app startup.
- If the active scene name is **not** `"MainMenu"`, calls `SceneManager.LoadScene("MainMenu")`.
- If the active scene is already `"MainMenu"`, does nothing — preventing any infinite reload loop.

No MonoBehaviour, no scene objects, no lifecycle methods. Follows the same static-class pattern as `FloorManager.cs`.

---

## Verification

Unity Editor manual verification required:
- SampleScene open → Play → should redirect to MainMenu automatically (no errors in Console)
- MainMenu open → Play → should remain on MainMenu without reloading (no infinite loop)

---

## Deviations from Plan

None — plan executed exactly as written.

---

## Known Stubs

None.

---

## Self-Check

- [x] `Assets/Scripts/World/GameBootstrapper.cs` — FOUND
- [x] Commit `90f9e2c` — FOUND

## Self-Check: PASSED
