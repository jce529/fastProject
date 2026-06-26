---
phase: quick-260623-ntw
plan: 01
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/UI/MainMenuController.cs
  - Assets/Editor/MainMenuSceneBuilder.cs
autonomous: false
requirements: []
must_haves:
  truths:
    - "게임 실행 시 MainMenu 씬이 가장 먼저 뜬다"
    - "타이틀 텍스트 'Fast'가 화면에 표시된다"
    - "Start 버튼을 누르면 SampleScene이 로드된다"
    - "Quit 버튼을 누르면 앱이 종료된다"
    - "Build Settings에서 MainMenu = index 0, SampleScene = index 1로 설정된다"
  artifacts:
    - path: "Assets/Scripts/UI/MainMenuController.cs"
      provides: "Start/Quit 버튼 로직"
    - path: "Assets/Editor/MainMenuSceneBuilder.cs"
      provides: "Editor 메뉴로 MainMenu.unity 씬을 프로그래매틱하게 생성"
    - path: "Assets/Scenes/MainMenu.unity"
      provides: "MainMenu 씬 파일 (EditorScript 실행 후 생성됨)"
  key_links:
    - from: "MainMenuController.cs"
      to: "SampleScene"
      via: "SceneManager.LoadScene(\"SampleScene\")"
    - from: "MainMenuSceneBuilder.cs"
      to: "Assets/Scenes/MainMenu.unity"
      via: "EditorSceneManager.NewScene + SaveScene"
---

<objective>
MainMenu 씬을 추가한다. 타이틀 텍스트("Fast") + Start 버튼(SampleScene 로드) + Quit 버튼(Application.Quit).

Purpose: 앱 실행 진입점을 MainMenu로 설정해 플레이테스트 흐름을 자연스럽게 만든다.
Output: MainMenuController.cs, MainMenuSceneBuilder.cs (EditorScript), 실행 후 생성되는 MainMenu.unity
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/STATE.md

기존 UI 스크립트 패턴:
- Assets/Scripts/UI/HUDController.cs — MonoBehaviour, SerializeField 참조, Start()에서 캐시
- Assets/Scripts/UI/DeathScreenController.cs — SceneManager.LoadScene 사용 패턴 확인 가능
- uGUI (com.unity.ugui 2.0.0) + TextMeshPro 사용 중
</context>

<tasks>

<task type="auto">
  <name>Task 1: MainMenuController.cs 작성</name>
  <files>Assets/Scripts/UI/MainMenuController.cs</files>
  <action>
Assets/Scripts/UI/MainMenuController.cs를 새로 생성한다.

요구사항:
- namespace 없음 (프로젝트 기존 패턴 따름)
- using: UnityEngine, UnityEngine.SceneManagement, UnityEngine.UI
- 클래스: MainMenuController : MonoBehaviour
- SerializeField 없음 — 버튼 콜백은 OnStartClicked() / OnQuitClicked() public 메서드로 Inspector에서 연결
- OnStartClicked(): SceneManager.LoadScene("SampleScene") 호출
- OnQuitClicked(): 에디터에서는 UnityEditor.EditorApplication.isPlaying = false, 빌드에서는 Application.Quit() 호출
  - #if UNITY_EDITOR / #else / #endif 조건부 컴파일 사용

전체 구현 (50줄 이내):

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void OnStartClicked()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
```
  </action>
  <verify>파일이 Assets/Scripts/UI/MainMenuController.cs에 존재하고, Unity Editor에서 컴파일 에러 없음</verify>
  <done>MainMenuController.cs 저장 완료, 컴파일 통과</done>
</task>

<task type="auto">
  <name>Task 2: MainMenuSceneBuilder EditorScript 작성</name>
  <files>Assets/Editor/MainMenuSceneBuilder.cs</files>
  <action>
Assets/Editor/ 폴더가 없으면 생성한다. 그 안에 MainMenuSceneBuilder.cs를 작성한다.

이 스크립트는 Unity Editor 메뉴 "Fast/Build MainMenu Scene"을 추가한다. 실행 시:

1. EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single)로 빈 씬 생성
2. 카메라 생성: new GameObject("Main Camera"), Camera 컴포넌트, AudioListener, tag = "MainCamera", clearFlags = CameraClearFlags.SolidColor, backgroundColor = Color.black
3. Canvas 생성: GameObject + Canvas(renderMode = ScreenSpaceOverlay) + CanvasScaler(uiScaleMode = ScaleWithScreenSize, referenceResolution = (1920,1080)) + GraphicRaycaster
4. EventSystem 생성: GameObject("EventSystem") + EventSystem + StandaloneInputModule 컴포넌트 추가
5. 타이틀 텍스트 생성:
   - Canvas 자식으로 GameObject("TitleText")
   - RectTransform: anchorMin=(0.5,0.7), anchorMax=(0.5,0.7), anchoredPosition=(0,0), sizeDelta=(600,120)
   - TextMeshProUGUI: text="Fast", fontSize=96, alignment=TextAlignmentOptions.Center, color=Color.white
6. Start 버튼 생성:
   - Canvas 자식 GameObject("StartButton")
   - RectTransform: anchorMin=(0.5,0.45), anchorMax=(0.5,0.45), anchoredPosition=(0,0), sizeDelta=(400,80)
   - Image 컴포넌트 (흰색 배경)
   - Button 컴포넌트
   - 자식 GameObject("Text") + RectTransform(stretch all) + TextMeshProUGUI: text="Start", fontSize=48, color=black
7. Quit 버튼 생성 (Start 버튼과 동일 구조):
   - anchoredPosition=(0,-120), text="Quit"
8. MainMenuController GameObject:
   - Canvas 자식 GameObject("MainMenuController")
   - MainMenuController 컴포넌트 추가
   - Start 버튼 OnClick에 MainMenuController.OnStartClicked 연결 (UnityAction)
   - Quit 버튼 OnClick에 MainMenuController.OnQuitClicked 연결
9. EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity")
10. Build Settings 업데이트:
    - EditorBuildSettings.scenes를 읽어 MainMenu(index 0), SampleScene(index 1) 순서로 재설정
    - 기존 씬 목록에서 SampleScene 항목은 유지, MainMenu는 새로 추가
    - EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { mainMenuEntry, sampleSceneEntry }

using 목록:
- UnityEngine, UnityEditor, UnityEditor.SceneManagement
- UnityEngine.UI, TMPro
- UnityEngine.Events (UnityAction용)

메서드에 [MenuItem("Fast/Build MainMenu Scene")] 어트리뷰트 적용.

주의사항:
- TextMeshProUGUI는 TMPro 네임스페이스
- Button.onClick.AddListener()로 UnityAction 연결
- 씬 저장 경로: "Assets/Scenes/MainMenu.unity"
- SampleScene 기존 경로: "Assets/Scenes/SampleScene.unity"
  </action>
  <verify>
Assets/Editor/MainMenuSceneBuilder.cs 파일 존재, Unity Editor 컴파일 통과.
Unity Editor 상단 메뉴에 "Fast" > "Build MainMenu Scene" 항목이 나타남.
  </verify>
  <done>EditorScript 컴파일 완료, 메뉴 항목 확인됨</done>
</task>

<task type="checkpoint:human-verify" gate="blocking">
  <what-built>
    - MainMenuController.cs (Start/Quit 버튼 로직)
    - MainMenuSceneBuilder.cs (Editor 메뉴 → MainMenu.unity 씬 자동 생성 스크립트)
  </what-built>
  <how-to-verify>
    1. Unity Editor가 컴파일 에러 없이 로드되는지 확인
    2. 상단 메뉴 "Fast" > "Build MainMenu Scene" 클릭
    3. Assets/Scenes/MainMenu.unity 파일이 생성되는지 확인
    4. Project 패널에서 MainMenu.unity를 더블클릭해 씬 열기
    5. 씬에 "Fast" 타이틀 텍스트, Start 버튼, Quit 버튼이 Canvas에 존재하는지 확인
    6. Build Settings(File > Build Settings) 확인 — MainMenu index 0, SampleScene index 1
    7. 에디터에서 Play: Start 버튼 누르면 SampleScene 로드되는지, Quit 버튼 누르면 Play 모드 종료되는지 확인
  </how-to-verify>
  <resume-signal>"approved" 입력 또는 발견된 문제 설명</resume-signal>
</task>

</tasks>

<verification>
- [ ] Assets/Scripts/UI/MainMenuController.cs 존재
- [ ] Assets/Editor/MainMenuSceneBuilder.cs 존재
- [ ] Unity Editor 컴파일 에러 없음
- [ ] "Fast/Build MainMenu Scene" 메뉴 실행 후 Assets/Scenes/MainMenu.unity 생성
- [ ] Build Settings: MainMenu = 0, SampleScene = 1
- [ ] Start 버튼 → SampleScene 로드 동작
- [ ] Quit 버튼 → 에디터 Play 모드 종료 동작
</verification>

<success_criteria>
MainMenu 씬이 빌드 인덱스 0으로 등록되고, 에디터 Play 시 타이틀/버튼이 표시되며 버튼 동작이 올바르게 실행된다.
</success_criteria>

<output>
완료 후 .planning/quick/260623-ntw-mainmenu-fast-samplescene-application-qu/260623-ntw-SUMMARY.md 작성
</output>
