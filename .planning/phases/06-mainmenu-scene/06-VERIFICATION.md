---
phase: 06-mainmenu-scene
verified: 2026-06-23T14:00:00Z
status: passed
score: 4/4 must-haves verified
re_verification: false
---

# Phase 6: MainMenu Scene — Verification Report

**Phase Goal:** 앱을 실행하면 MainMenu가 첫 화면으로 열리고, Start/Quit 버튼이 동작한다
**Verified:** 2026-06-23
**Status:** PASSED
**Re-verification:** No — initial verification

---

## Goal Achievement

### Observable Truths

| # | Truth | Status | Evidence |
|---|-------|--------|----------|
| 1 | 앱(에디터 Play) 실행 시 MainMenu.unity가 첫 화면으로 열린다 — SampleScene이 먼저 뜨지 않는다 | VERIFIED | EditorBuildSettings.asset: `Assets/Scenes/MainMenu.unity` at index 0, `Assets/Scenes/SampleScene.unity` at index 1. Human playtest confirmed. |
| 2 | Start 버튼 onClick이 MainMenuController.OnStartClicked()로 연결되고 SceneManager.LoadScene("AttackSelect")를 호출한다 | VERIFIED | MainMenuController.cs line 8: `SceneManager.LoadScene("AttackSelect")`. MainMenu.unity serializes `m_MethodName: OnStartClicked` on StartButton's onClick (line 1027). Human test: "AttackSelect couldn't be loaded" error confirms correct wiring. |
| 3 | Quit 버튼 onClick이 MainMenuController.OnQuitClicked()로 연결되고 에디터에서 PlayMode를 종료한다 | VERIFIED | MainMenuController.cs lines 13-17: `#if UNITY_EDITOR UnityEditor.EditorApplication.isPlaying = false`. MainMenu.unity serializes `m_MethodName: OnQuitClicked` on QuitButton's onClick (line 665). Human test confirmed Play mode exits. |
| 4 | Build Settings에서 MainMenu.unity가 index 0이다 | VERIFIED | ProjectSettings/EditorBuildSettings.asset: first `m_Scenes` entry is `path: Assets/Scenes/MainMenu.unity` with `enabled: 1`. |

**Score: 4/4 truths verified**

---

### Required Artifacts

| Artifact | Expected | Status | Details |
|----------|----------|--------|---------|
| `Assets/Scripts/UI/MainMenuController.cs` | OnStartClicked() → AttackSelect, OnQuitClicked() → 앱 종료 | VERIFIED | 19 lines, substantive. `SceneManager.LoadScene("AttackSelect")` present; `SampleScene` absent. Editor/build conditional quit implemented. |
| `Assets/Scenes/MainMenu.unity` | MainMenu 씬 파일 — EditorScript 재실행 후 갱신됨 | VERIFIED | File exists. Contains Canvas, StartButton, QuitButton, MainMenuController, EventSystem with InputSystemUIInputModule. Persistent onClick listeners serialized. |
| `Assets/Editor/MainMenuSceneBuilder.cs` | Fast > Build MainMenu Scene 메뉴 — 씬 재생성 및 Build Settings 업데이트 | VERIFIED | 113 lines, substantive. Uses `InputSystemUIInputModule` (not StandaloneInputModule). Uses `UnityEventTools.AddPersistentListener()` (not AddListener). Saves to `Assets/Scenes/MainMenu.unity` and updates EditorBuildSettings. |
| `ProjectSettings/EditorBuildSettings.asset` | MainMenu at index 0, SampleScene at index 1 | VERIFIED | YAML confirms order: MainMenu (enabled: 1) → SampleScene (enabled: 1). |

---

### Key Link Verification

| From | To | Via | Status | Details |
|------|----|-----|--------|---------|
| MainMenuController.OnStartClicked | AttackSelect 씬 | `SceneManager.LoadScene("AttackSelect")` | WIRED | Line 8 of MainMenuController.cs. Confirmed by human: "AttackSelect couldn't be loaded" error in console — proves the call is made. |
| StartButton.onClick | MainMenuController.OnStartClicked | `UnityEventTools.AddPersistentListener` + scene serialization | WIRED | MainMenu.unity line 1027: `m_MethodName: OnStartClicked`, `m_TargetAssemblyTypeName: MainMenuController, Assembly-CSharp` |
| QuitButton.onClick | MainMenuController.OnQuitClicked | `UnityEventTools.AddPersistentListener` + scene serialization | WIRED | MainMenu.unity line 665: `m_MethodName: OnQuitClicked`, `m_TargetAssemblyTypeName: MainMenuController, Assembly-CSharp` |
| EditorBuildSettings | MainMenu.unity | index 0 entry | WIRED | EditorBuildSettings.asset: first scene entry is `Assets/Scenes/MainMenu.unity` with `enabled: 1` |

---

### Data-Flow Trace (Level 4)

Not applicable. MainMenuController has no dynamic data rendering — it is a button handler only. No state variables, no fetch calls, no rendering pipeline to trace.

---

### Behavioral Spot-Checks

Human verification was performed (Task 2 checkpoint in 06-01-PLAN.md) and documented in 06-01-SUMMARY.md:

| Behavior | Method | Result | Status |
|----------|--------|--------|--------|
| Editor Play opens MainMenu as first screen | Human: Unity Editor Play | MainMenu 씬(타이틀 "Fast" + Start + Quit 버튼) 첫 화면 확인 | PASS |
| Start button triggers AttackSelect load | Human: Click Start button | Console: "Scene 'AttackSelect' couldn't be loaded" error (expected — proves correct wiring) | PASS |
| Quit button exits Play mode | Human: Click Quit button | Play mode terminated | PASS |
| No compile errors | Human: Unity Editor compilation | No errors | PASS |

---

### Requirements Coverage

| Requirement | Source Plan | Description | Status | Evidence |
|-------------|------------|-------------|--------|----------|
| MENU-01 | 06-01-PLAN.md | MainMenu.unity at Build index 0 (first screen) | SATISFIED | EditorBuildSettings.asset: MainMenu at index 0; human Play test confirmed first screen |
| MENU-02 | 06-01-PLAN.md | Start button navigates to AttackSelect | SATISFIED | `SceneManager.LoadScene("AttackSelect")` in OnStartClicked(); onClick serialized in scene; human test produced "AttackSelect couldn't be loaded" (AttackSelect scene is Phase 7 scope — error proves correct wiring) |
| MENU-03 | 06-01-PLAN.md | Quit button exits app | SATISFIED | `EditorApplication.isPlaying = false` in editor / `Application.Quit()` in build; onClick serialized in scene; human test confirmed Play mode exit |

---

### Anti-Patterns Found

| File | Line | Pattern | Severity | Impact |
|------|------|---------|----------|--------|
| None | — | — | — | — |

No TODO/FIXME/placeholder comments found in modified files. No empty return values. No stub handlers. No hardcoded empty data.

---

### Human Verification Required

None. All success criteria were verified by human playtest documented in 06-01-SUMMARY.md. Automated checks confirm code and scene asset correctness.

---

### Gaps Summary

No gaps. All 4 must-have truths are verified. All 4 artifacts pass existence, substance, and wiring checks. All 3 requirements (MENU-01, MENU-02, MENU-03) are satisfied. Three commits (06d0efe, 432e56b, 2d32404) are confirmed in git history. Human playtest confirmed all three success criteria from ROADMAP.md.

One note: the ROADMAP.md Progress Table still shows Phase 6 as "Not started" — STATE.md should be updated to reflect Phase 6 complete, but this is a documentation gap, not a goal-achievement gap.

---

_Verified: 2026-06-23_
_Verifier: Claude (gsd-verifier)_
