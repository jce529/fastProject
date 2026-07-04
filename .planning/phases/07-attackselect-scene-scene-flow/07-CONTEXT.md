# Phase 7: AttackSelect Scene & Scene Flow - Context

**Gathered:** 2026-06-23
**Status:** Ready for planning

<domain>
## Phase Boundary

AttackSelect.unity 씬을 새로 만들어 Linear / Fan 버튼을 제공한다. 버튼 클릭 시 선택값을 유지한 채 SampleScene을 로드한다. SampleScene의 기존 AttackTypeSelector Canvas 오버레이를 제거하고, 사망 후 AttackSelect로 복귀하도록 DeathScreenController를 수정한다. MainMenu 씬 자체는 이미 Phase 6에서 완료 — 변경하지 않는다.

</domain>

<decisions>
## Implementation Decisions

### 씬 간 데이터 전달 (ATKS-02)
- **D-01:** `AttackTypeSelector.Selected`의 기존 static 프로퍼티를 그대로 재사용한다. AttackSelect 씬의 컨트롤러가 `AttackTypeSelector.SetType(AttackType.Linear|Fan)` 을 호출한 뒤 SampleScene을 로드한다. Unity static 필드는 씬 전환 후에도 유지되므로 PlayerPrefs나 GameManager 추가 없이 동작한다.
- **D-02:** `CombatController`, `HUDController` 코드 변경 없음 — 두 컴포넌트 모두 이미 클래스 참조(`AttackTypeSelector.Selected`)로 읽고 있음.

### AttackTypeSelector 오버레이 제거 (ATKS-03)
- **D-03:** SampleScene에서 AttackTypeSelector Canvas GameObject를 씬에서 완전히 삭제한다. static `Selected` 프로퍼티는 클래스 수준이므로 인스턴스 없이도 CombatController / HUDController가 정상 읽는다.
- **D-04:** SampleScene 내 AttackTypeZone 콜라이더(게임 중 타입 전환 트리거)도 함께 제거한다. Phase 7 이후 공격 타입은 게임 시작 전 AttackSelect에서 고정되므로 게임 중 전환 불필요.

### AttackSelect 씬 UI (ATKS-01)
- **D-05:** UI는 버튼 2개만 — LINEAR / FAN. 타이틀 텍스트, 설명, 다이어그램 없음. MainMenu와 동일한 단색 어두운 배경. 최소한의 UI로 검증에 집중.
- **D-06:** 씬은 Unity Editor에서 직접 수동 제작한다. EditorScript(AttackSelectSceneBuilder) 불필요 — 한 번만 만들면 되는 씬.

### 사망 후 AttackSelect 복귀 (FLOW-01)
- **D-07:** `DeathScreenController.RestartGame()`의 `SceneManager.LoadScene(0)` → `SceneManager.LoadScene("AttackSelect")` 또는 `SceneManager.LoadScene(1)`로 변경.
- **D-08:** 재시작 버튼 텍스트를 "다시 선택"으로 변경 (Phase 6에서 이월된 사항).
- **D-09:** FloorManager.CurrentFloor = 1 리셋은 유지 — AttackSelect 복귀 후 SampleScene을 새로 로드하면 자연스럽게 1층부터 시작.

### Build Settings 순서
- **D-10:** MainMenu(0) → AttackSelect(1) → SampleScene(2). Phase 6에서 이미 결정됨. AttackSelect 씬 생성 후 EditorBuildSettings에 index 1로 추가.

### Claude's Discretion
- AttackSelectController.cs 신규 생성 — 버튼 onClick에서 SetType() 호출 + LoadScene("SampleScene") 패턴은 MainMenuController와 동일 구조로 작성.
- AttackSelect 씬의 EventSystem은 InputSystemUIInputModule 사용 (Phase 6 동일 — New Input System 전용 프로젝트).
- 사망 후 AttackSelect로 돌아갔을 때 이전 선택값 유지 여부는 구현 재량 — 기본 Linear 고정이 가장 단순.

</decisions>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### 수정 대상 파일
- `Assets/Scripts/UI/AttackTypeSelector.cs` — static Selected 프로퍼티 및 SetType() 구현 확인 필수. AttackSelect 컨트롤러가 이 메서드를 호출함.
- `Assets/Scripts/UI/DeathScreenController.cs` — RestartGame()의 LoadScene(0) → AttackSelect 변경 대상.
- `Assets/Scripts/UI/HUDController.cs` — AttackTypeSelector.Selected 읽기 패턴 참고 (변경 없음).
- `Assets/Scripts/Player/CombatController.cs` — AttackTypeSelector.Selected 읽기 패턴 참고 (변경 없음).

### 참고 패턴 (Phase 6)
- `Assets/Scripts/UI/MainMenuController.cs` — AttackSelectController 작성 시 동일 패턴 사용.
- `Assets/Editor/MainMenuSceneBuilder.cs` — 씬 생성 EditorScript 참고 (Phase 7은 수동 제작이지만 EventSystem / Canvas 설정 참고).
- `Assets/Scenes/MainMenu.unity` — UI 스타일 참고 (배경색, 버튼 크기).

### 요구사항
- `.planning/REQUIREMENTS.md` §ATKS-01, ATKS-02, ATKS-03, FLOW-01 — Phase 7 성공 기준 상세.
- `.planning/ROADMAP.md` §v2.0 Implementation Notes — Build Settings 순서, DeathScreen 처리 방침.

### Build Settings
- `ProjectSettings/EditorBuildSettings.asset` — AttackSelect(1) 추가 위치 확인.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `AttackTypeSelector.SetType(AttackType)` — public static 메서드. MonoBehaviour 인스턴스 없이 호출 가능. AttackSelect 씬에서 바로 사용.
- `MainMenuController.cs` — OnStartClicked 패턴 그대로 복사해 AttackSelectController 작성.
- `DeathScreenController.cs` — RestartGame() 내 LoadScene 한 줄만 변경.

### Established Patterns
- `SceneManager.LoadScene(string)` — 씬 이름 기반 로드 패턴 (MainMenuController, DeathScreenController 공통).
- `InputSystemUIInputModule` — Phase 6에서 확인된 New Input System용 EventSystem 모듈. AttackSelect 씬에도 동일 적용.
- `#if UNITY_EDITOR / #else` — Quit 버튼 조건부 컴파일 패턴 (AttackSelectController에는 불필요).

### Integration Points
- `AttackTypeSelector.Selected` → `CombatController.FindNearestEnemyInRange()` — 씬 로드 후 즉시 반영됨.
- `AttackTypeSelector.Selected` → `HUDController.Update()` — 첫 프레임에 레이블 갱신됨 (dirty-check sentinel 로직 존재).
- `DeathScreenController` → `FloorManager.CurrentFloor = 1` 리셋은 RestartGame()에 이미 존재 — 건드리지 않음.

</code_context>

<specifics>
## Specific Ideas

- AttackSelect 씬 UI는 MainMenu.unity를 참고해 동일한 Canvas/Button 구성으로 제작. 배경 단색 + 버튼 2개(LINEAR, FAN) 중앙 배치.
- AttackSelectController는 두 버튼 각각의 onClick에 `OnLinearClicked()` / `OnFanClicked()` 연결.
- 사망 후 AttackSelect로 돌아올 때 이전 선택값은 유지되나 버튼 하이라이트는 기본 상태 — 선택 안 한 것처럼 보여도 기능상 무관.

</specifics>

<deferred>
## Deferred Ideas

- AttackSelect 씬에 범위 형태 다이어그램 표시 — 플레이테스트 후 플레이어가 두 모드를 이해하기 어려워할 때 추가 검토.
- AttackSelect EditorScript(`AttackSelectSceneBuilder.cs`) — 현재는 수동 제작이지만 씬 구조가 자주 변경되면 추가 고려.
- 게임 중 AttackTypeZone 트리거로 공격 타입 변경 — 현재 Phase 7에서 제거, 향후 게임 메카닉 확장 시 재고.

</deferred>

---

*Phase: 07-attackselect-scene-scene-flow*
*Context gathered: 2026-06-23*
