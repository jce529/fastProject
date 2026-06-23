# Phase 6: MainMenu Scene - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

앱(또는 에디터 Play) 실행 시 MainMenu.unity가 첫 화면으로 열리고, Start 버튼이 AttackSelect로, Quit 버튼이 앱 종료로 동작한다. AttackSelect 씬 자체 구현은 Phase 7 범위.

</domain>

<decisions>
## Implementation Decisions

### Start 버튼 목적지 (MENU-02)
- **D-01:** `MainMenuController.OnStartClicked()`를 `SceneManager.LoadScene("AttackSelect")`로 수정한다. AttackSelect 씬이 Phase 7에서 생성되기 전까지는 Start 클릭 시 에러가 발생하지만, 코드는 요구사항에 맞게 정확히 작성한다.

### MainMenu.unity 재빌드
- **D-02:** `OnStartClicked()` 코드 수정 후 `Fast/Build MainMenu Scene` EditorScript를 다시 실행해 MainMenu.unity를 재생성한다. 씬 파일의 버튼 onClick 연결이 새 코드로 갱신되도록 한다.

### DeathScreen 텍스트
- **D-03:** DeathScreen 재시작 버튼 텍스트(현재 "MAIN")는 Phase 7에서 "다시 선택"으로 교체할 때 함께 처리한다. Phase 6 범위에서 수정하지 않는다.

### Claude's Discretion
- Build Settings는 현재 MainMenu=0, SampleScene=1 상태. Phase 7에서 AttackSelect 씬 추가 시 순서를 MainMenu(0)→AttackSelect(1)→SampleScene(2)으로 업데이트한다. Phase 6에서는 현재 Build Settings를 그대로 유지한다.
- Quit 버튼 동작(#if UNITY_EDITOR / Application.Quit())은 이미 올바르게 구현되어 있어 수정 불필요.
- MainMenuSceneBuilder의 EditorScript 재실행 순서: (1) MainMenuController.cs 코드 수정, (2) Unity가 컴파일 완료 대기, (3) EditorScript 실행.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 기존 구현 (수정 대상)
- `Assets/Scripts/UI/MainMenuController.cs` — OnStartClicked/OnQuitClicked 구현. OnStartClicked를 AttackSelect로 변경할 파일.
- `Assets/Editor/MainMenuSceneBuilder.cs` — MainMenu.unity 씬 자동 생성 EditorScript. 재실행 로직 참고.

### 씬 & Build Settings
- `Assets/Scenes/MainMenu.unity` — 재생성 대상 씬 파일.
- `ProjectSettings/EditorBuildSettings.asset` — Build Settings 파일. 현재 MainMenu(0)/SampleScene(1) 확인용.

### 요구사항
- `.planning/REQUIREMENTS.md` §MENU-01, MENU-02, MENU-03 — Phase 6 성공 기준 상세.
- `.planning/ROADMAP.md` §v2.0 Implementation Notes — Start 버튼 타겟, Build Settings 최종 순서, DeathScreen 처리 방침.

### 기존 UI 패턴
- `Assets/Scripts/UI/DeathScreenController.cs` — SceneManager.LoadScene() 사용 패턴 참고. Phase 6에서 수정하지 않음.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `MainMenuController.cs`: 이미 존재. OnStartClicked만 수정, OnQuitClicked는 그대로.
- `MainMenuSceneBuilder.cs`: 이미 존재. 수정 없이 재실행만 필요.
- `MainMenu.unity`: 이미 생성됨. EditorScript 재실행으로 덮어씀.

### Established Patterns
- uGUI Button.onClick: Inspector에서 메서드 참조로 연결 — 씬 재생성 시 EditorScript의 AddListener() 로직이 자동 재연결.
- SceneManager.LoadScene(string): 씬 이름 기반 로드 패턴 (HUDController, DeathScreenController 공통).
- `#if UNITY_EDITOR / #else / #endif`: OnQuitClicked에서 이미 사용 중인 조건부 컴파일 패턴.

### Integration Points
- Build Settings (EditorBuildSettings.asset): MainMenuSceneBuilder가 이미 index 0에 MainMenu 등록. Phase 7 추가 시 index 재정렬 필요.
- MainMenuController.OnStartClicked() → AttackSelect: Phase 7이 씬을 생성하면 자동으로 동작.

</code_context>

<specifics>
## Specific Ideas

- EditorScript 재실행 전 Unity Editor에서 현재 씬 저장 확인 필요 (NewScene 호출 시 기존 씬 닫힘).
- MainMenu.unity 재생성 후 Build Settings에서 MainMenu가 index 0으로 유지되는지 확인할 것 (EditorScript가 이를 처리하나, 덮어쓰기 과정에서 index가 초기화될 수 있음).

</specifics>

<deferred>
## Deferred Ideas

- DeathScreen 버튼 텍스트 수정 ("MAIN" → "다시 선택") — Phase 7에서 SceneManager.LoadScene(1) 변경과 함께 처리.
- Build Settings 3-씬 순서 (MainMenu(0)→AttackSelect(1)→SampleScene(2)) — Phase 7에서 AttackSelect 씬 추가 시 반영.
- AttackSelect.unity 씬 구현 — Phase 7 범위.

</deferred>

---

*Phase: 06-mainmenu-scene*
*Context gathered: 2026-06-23*
