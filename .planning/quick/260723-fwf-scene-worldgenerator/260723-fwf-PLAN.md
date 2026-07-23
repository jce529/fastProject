---
phase: quick
plan: 260723-fwf
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Editor/DebugSceneBuilder.cs
  - Assets/Scripts/Debug/ReturnToMainSceneButton.cs
  - Assets/Scenes/DebugScene.unity
  - ProjectSettings/EditorBuildSettings.asset
autonomous: false
requirements: []
must_haves:
  truths:
    - "DebugScene.unity를 열고 Play하면 WorldGenerator 없이 즉시 고정 좌표에서 Player와 FioraBoss가 존재해 층 이동 좌표 변화 없이 전투를 반복 디버깅할 수 있다"
    - "DebugScene에서 화면 우측 하단 버튼을 누르면 즉시 SampleScene(메인 씬)으로 전환된다"
    - "SampleScene.unity는 이 작업 전후로 git diff 없이 완전히 동일하다 (읽기 전용 참고만, 수정 대상 아님)"
    - "DebugScene.unity가 Build Settings(Scenes In Build)에 등록되어 SceneManager.LoadScene(\"SampleScene\") 문자열 로드가 정상 동작한다"
  artifacts:
    - path: "Assets/Editor/DebugSceneBuilder.cs"
      provides: "Fast/Debug/Build DebugScene 메뉴 — Room_BossFsmTest(바닥+RoomEntry+nested BossEnemy)+SampleScene에서 읽기전용 복제한 Player+정적 카메라+우측 하단 복귀 버튼을 절차적으로 생성해 DebugScene.unity로 저장, Build Settings에 등록"
      contains: "DebugSceneBuilder"
    - path: "Assets/Scripts/Debug/ReturnToMainSceneButton.cs"
      provides: "SceneManager.LoadScene(\"SampleScene\") 한 줄 호출 — 버튼에 배선되는 최소 범위 컴포넌트"
      contains: "ReturnToMain"
    - path: "Assets/Scenes/DebugScene.unity"
      provides: "WorldGenerator/층 체인 생성 로직이 전혀 없는 고정 좌표 디버그 전용 씬 (Task 2 메뉴 실행 산출물)"
    - path: "ProjectSettings/EditorBuildSettings.asset"
      provides: "DebugScene 항목이 기존 MainMenu/AttackSelect/SampleScene 3개 항목 뒤에 추가됨 (Scenes In Build)"
  key_links:
    - from: "DebugSceneBuilder.Build()"
      to: "Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab"
      via: "PrefabUtility.InstantiatePrefab — 바닥+RoomEntry+nested BossEnemy를 통째로 재사용, WorldGenerator/EnemySpawner 경유 없음"
      pattern: "InstantiatePrefab"
    - from: "DebugSceneBuilder.Build()"
      to: "SampleScene.unity의 Player GameObject"
      via: "EditorSceneManager.OpenScene(Additive)로 읽기 전용 로드 후 Object.Instantiate로 복제, 원본 씬은 변경 없이 CloseScene"
      pattern: "OpenScene"
    - from: "ReturnButton(Button).onClick"
      to: "ReturnToMainSceneButton.ReturnToMain()"
      via: "UnityEventTools.AddPersistentListener — MainMenuSceneBuilder.cs와 동일 컨벤션"
      pattern: "AddPersistentListener"
    - from: "ReturnToMainSceneButton.ReturnToMain()"
      to: "UnityEngine.SceneManagement.SceneManager.LoadScene"
      via: "직접 호출 (필드/매개변수 없는 단일 라인)"
      pattern: "LoadScene(\"SampleScene\")"
---

<objective>
층 이동(WorldGenerator 체인 생성)으로 좌표가 계속 바뀌어 FioraBoss 등 적/보스 디버깅이 불편한 문제를 해결하기 위해, WorldGenerator/층 체인 생성 로직과 완전히 분리된 디버그 전용 Scene(`Assets/Scenes/DebugScene.unity`)을 절차적 에디터 도구로 신설한다. 이 씬은 Player + FioraBoss(기존 `Room_BossFsmTest.prefab`에 이미 nested 배치되어 있음)를 고정 좌표에 배치하고, WorldGenerator GameObject를 전혀 포함하지 않는다.

**설계 결정 (task_requirements에서 이미 확정):** "씬에 따라 Play Mode Start Scene을 동적으로 분기"하는 방식(EditorSceneManager.playModeStartScene 전역 강제)은 SampleScene 자체의 개발/테스트를 막는 부작용이 크므로 채택하지 않는다. 대신 DebugScene 우측 하단에 "메인 씬으로" 버튼을 배치해 `SceneManager.LoadScene("SampleScene")`으로 수동 복귀하는 방식을 채택한다 — 각 씬을 열고 Play하면 그 씬만 실행되는 것이 Unity 기본 동작이므로, 이것만으로 "디버그 씬 Play → 디버그 씬만 실행 / 메인 씬 Play → 메인 흐름 실행"이라는 핵심 니즈는 이미 충족된다.

**Unity 에디터 조작(새 GameObject 씬 배치, Build Settings 등록)은 이 프로젝트의 기존 컨벤션(`MainMenuSceneBuilder.cs`, `RoomBossFsmTestBuilder.cs`, `BossEnemyPrefabBuilder.cs`)을 따라 `[MenuItem]` 에디터 도구로 절차적으로 생성한다.** 씬 파일을 텍스트로 직접 작성하지 않는다 — 실제 씬 저장/메뉴 실행은 Task 2(checkpoint:human-action)에서 사용자가 Unity 에디터에서 직접 수행한다.

Purpose: 보스/적 디버깅 시 반복되는 좌표 드리프트 문제를 근본적으로 제거하고, 언제든 Play 한 번으로 고정된 전투 환경에 도달할 수 있게 한다.
Output: `DebugSceneBuilder.cs`(에디터 메뉴 도구) + `ReturnToMainSceneButton.cs`(런타임 복귀 버튼 로직). 사용자가 메뉴를 실행하면 `Assets/Scenes/DebugScene.unity` + Build Settings 갱신이 산출된다.
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@D:/새 폴더/Fast/.planning/STATE.md
@D:/새 폴더/Fast/Assets/Scripts/World/DebugRoomTeleporter.cs
@D:/새 폴더/Fast/Assets/Editor/MainMenuSceneBuilder.cs
@D:/새 폴더/Fast/Assets/Editor/RoomBossFsmTestBuilder.cs
</context>

<interfaces>
<!-- Room_BossFsmTest.prefab 구조 (RoomBossFsmTestBuilder.cs 산출물, 이미 존재 — 재생성 불필요) -->
<!-- 루트 기준: 바닥 Tilemap(x:-14~14, y:0), RoomEntry+ExitSpawnPoint(로컬 0,1,0),
     Door/ENT(Left, -14,1,0)/EXIT(Right, 14,1,0) RoomConnector, nested BossEnemy(FioraBoss) 인스턴스(로컬 6,1,0).
     DebugScene에서는 ENT/EXIT RoomConnector는 사용하지 않지만 그대로 두어도 부작용 없음(WorldGenerator가 이 씬에 없으므로 아무도 참조하지 않음). -->

<!-- MainMenuSceneBuilder.cs의 검증된 씬 생성 패턴 — 그대로 재사용 -->
```csharp
EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
// ... GameObject 생성 ...
EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/DebugScene.unity");
var mainMenuEntry = new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true);
EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { mainMenuEntry, ... };
```

<!-- 현재 ProjectSettings/EditorBuildSettings.asset 실측값 (2026-07-23) — Task 1의 append 로직이
     이 3개 항목을 그대로 보존한 채 DebugScene을 4번째로 추가해야 한다 (덮어쓰기 금지) -->
```
0: Assets/Scenes/MainMenu.unity
1: Assets/Scenes/AttackSelect.unity
2: Assets/Scenes/SampleScene.unity
```

<!-- SampleScene.unity의 Player GameObject — DebugSceneBuilder가 읽기 전용으로 복제할 대상.
     PlayerController, InputManager, Animator, CapsuleCollider2D, Rigidbody2D, CombatController,
     ChronoGaugeController, InvincibilityHandler, FallDetector, PlayerAnimatorController 등을
     루트에 보유. 자식 GameObject(RangeDisplay, TrailRenderer 등)도 함께 존재 — 계층 전체를
     Object.Instantiate()로 한 번에 복제하면 내부 참조가 자동으로 재매핑되므로 컴포넌트를
     하나씩 재조립할 필요가 없다. -->
```csharp
// PlayerController 보유 여부로 루트 GameObject를 찾는다 (프리팹이 아니라 씬 오브젝트이므로 이름 매칭 대신 컴포넌트로 식별)
GameObject playerSource = null;
foreach (var root in sampleScene.GetRootGameObjects())
    if (root.GetComponent<PlayerController>() != null) { playerSource = root; break; }
```
</interfaces>

<tasks>

<task type="auto">
  <name>Task 1: Create ReturnToMainSceneButton.cs + DebugSceneBuilder.cs</name>
  <files>Assets/Scripts/Debug/ReturnToMainSceneButton.cs, Assets/Editor/DebugSceneBuilder.cs</files>
  <action>
Create `Assets/Scripts/Debug/ReturnToMainSceneButton.cs` — minimal runtime component, `SceneManager.LoadScene` 한 줄 호출 수준 (task_requirements 6번):

```csharp
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 디버그 씬(DebugScene.unity) 전용 — 우측 하단 버튼 onClick에 배선되어 즉시 SampleScene(메인 씬)으로 전환한다.
/// </summary>
public class ReturnToMainSceneButton : MonoBehaviour
{
    private const string MainSceneName = "SampleScene";

    public void ReturnToMain()
    {
        SceneManager.LoadScene(MainSceneName);
    }
}
```

Create `Assets/Editor/DebugSceneBuilder.cs` — 메뉴 `Fast/Debug/Build DebugScene`. `MainMenuSceneBuilder.cs`(씬 생성+저장+Build Settings 패턴)와 `RoomBossFsmTestBuilder.cs`/`BossEnemyPrefabBuilder.cs`(프리팹 재사용+헬퍼 인라인 컨벤션)를 그대로 따른다:

```csharp
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu: Fast/Debug/Build DebugScene
/// WorldGenerator/층 체인 생성과 완전히 분리된 디버그 전용 씬을 절차적으로 생성한다.
/// Room_BossFsmTest.prefab(바닥+RoomEntry+nested BossEnemy 포함, RoomBossFsmTestBuilder.cs 산출물)을
/// 원점에 배치하고, SampleScene.unity의 Player GameObject를 additive로 열어 읽기 전용 복제한 뒤
/// RoomEntry 위치에 놓는다. SampleScene.unity는 변경 없이 CloseScene되므로 절대 수정되지 않는다.
/// 우측 하단 버튼(ReturnToMainSceneButton)으로 SampleScene 복귀 가능.
/// </summary>
public static class DebugSceneBuilder
{
    private const string DebugScenePath  = "Assets/Scenes/DebugScene.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RoomPrefabPath  = "Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab";

    [MenuItem("Fast/Debug/Build DebugScene")]
    public static void Build()
    {
        var roomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
        if (roomPrefab == null)
        {
            Debug.LogError($"[DebugSceneBuilder] {RoomPrefabPath} not found — run 'Fast/Phase15/Build Room_BossFsmTest' first.");
            return;
        }

        // 1. 새 빈 씬 생성 (단일 모드 — 현재 열린 씬 대체, 미저장 변경 있으면 에디터가 저장 여부를 물음)
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. SampleScene을 additive로 열어 Player를 읽기 전용으로 복제 — SampleScene 자체는 절대 수정/저장하지 않는다.
        //    활성 씬은 여전히 새로 만든 빈 씬이므로 Instantiate 결과가 자동으로 그쪽에 들어간다.
        var sampleScene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Additive);
        GameObject playerSource = null;
        foreach (var root in sampleScene.GetRootGameObjects())
        {
            if (root.GetComponent<PlayerController>() != null) { playerSource = root; break; }
        }
        if (playerSource == null)
        {
            Debug.LogError("[DebugSceneBuilder] SampleScene.unity에서 PlayerController를 가진 GameObject를 찾지 못했습니다.");
            EditorSceneManager.CloseScene(sampleScene, true);
            return;
        }
        var playerCopy = Object.Instantiate(playerSource);
        playerCopy.name = "Player";
        EditorSceneManager.CloseScene(sampleScene, true); // SampleScene은 변경사항 없이 그대로 닫힘 — 디스크에 저장하지 않음

        // 3. Room_BossFsmTest 인스턴스 배치 (바닥+RoomEntry+nested BossEnemy 이미 포함, 재조립 불필요)
        var roomInstance = (GameObject)PrefabUtility.InstantiatePrefab(roomPrefab);
        roomInstance.transform.position = Vector3.zero;

        var entry = roomInstance.GetComponentInChildren<RoomEntry>(true);
        Vector3 entryPos = entry != null ? entry.transform.position : Vector3.up;
        playerCopy.transform.position = entryPos;
        var rb = playerCopy.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // 4. 고정 카메라 — CameraFollow 없이 이 격리된 소구역(x:0~6 근방)만 정적으로 프레이밍
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        camGO.transform.position = entryPos + new Vector3(4f, 3f, -10f);
        camGO.AddComponent<AudioListener>();

        // 5. Canvas + EventSystem + 우측 하단 "메인 씬으로" 버튼
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        var returnGO = new GameObject("ReturnToMainSceneButton");
        returnGO.transform.SetParent(canvasGO.transform, false);
        var returnComponent = returnGO.AddComponent<ReturnToMainSceneButton>();

        var btnGO = new GameObject("ReturnButton", typeof(RectTransform));
        btnGO.transform.SetParent(canvasGO.transform, false);
        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot     = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-40f, 40f);
        rt.sizeDelta = new Vector2(240f, 70f);
        var img = btnGO.AddComponent<Image>();
        img.color = Color.white;
        var button = btnGO.AddComponent<Button>();

        var textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(btnGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "메인 씬으로";
        tmp.fontSize = 28f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        UnityEventTools.AddPersistentListener(button.onClick, returnComponent.ReturnToMain);

        // 6. 저장
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), DebugScenePath);

        // 7. Build Settings — 기존 항목 유지, DebugScene 없으면 추가 (멱등적 재실행)
        var existing = EditorBuildSettings.scenes;
        bool alreadyPresent = false;
        foreach (var s in existing) if (s.path == DebugScenePath) alreadyPresent = true;
        if (!alreadyPresent)
        {
            var updated = new EditorBuildSettingsScene[existing.Length + 1];
            existing.CopyTo(updated, 0);
            updated[existing.Length] = new EditorBuildSettingsScene(DebugScenePath, true);
            EditorBuildSettings.scenes = updated;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[DebugSceneBuilder] Created {DebugScenePath} — Room_BossFsmTest + cloned Player(from SampleScene, read-only) + static camera + bottom-right return button. Build Settings updated (idempotent). SampleScene.unity was opened additively and closed without saving — untouched.");
    }
}
```

WorldGenerator는 어디에도 Instantiate/참조하지 않는다 — DebugScene에는 WorldGenerator GameObject가 존재하지 않는다(task_requirements 1/2번). `EditorSceneManager.OpenScene(..., OpenSceneMode.Additive)` + 변경 없는 `CloseScene(sampleScene, true)` 조합으로 SampleScene.unity 파일 자체는 절대 저장/수정되지 않는다(task_requirements 2번).
  </action>
  <verify>
    <automated>grep -c "MenuItem(\"Fast/Debug/Build DebugScene\")" "Assets/Editor/DebugSceneBuilder.cs" ; grep -c "SceneManager.LoadScene(MainSceneName)" "Assets/Scripts/Debug/ReturnToMainSceneButton.cs" ; grep -c "WorldGenerator" "Assets/Editor/DebugSceneBuilder.cs"</automated>
  </verify>
  <done>두 파일 모두 생성되어 있고, DebugSceneBuilder.cs에는 `[MenuItem("Fast/Debug/Build DebugScene")]`가, ReturnToMainSceneButton.cs에는 `SceneManager.LoadScene(MainSceneName)`이 정확히 존재한다. DebugSceneBuilder.cs 어디에도 `WorldGenerator` 문자열이 등장하지 않는다(grep 결과 0) — WorldGenerator를 전혀 참조/Instantiate하지 않았음을 증명. Unity 컴파일 에러 없음(Task 2에서 Unity 에디터가 자동 확인).</done>
</task>

<task type="checkpoint:human-action" gate="blocking">
  <name>Task 2: Run "Fast/Debug/Build DebugScene" menu and verify isolation + return button</name>
  <files>없음 (Task 1 산출물을 Unity 에디터에서 실행하는 조작 단계 — 코드 변경 없음)</files>
  <action>
Task 1의 두 스크립트는 코드만 작성되었을 뿐 아직 아무 씬도 생성하지 않았다. `[MenuItem]` 메뉴 실행과 씬 저장은 Unity 에디터 GUI 조작이 필요해 Claude가 대신 실행할 수 없다 — 사용자가 직접 아래 절차를 수행해야 한다.
  </action>
  <what-built>
`Fast/Debug/Build DebugScene` 메뉴 — 실행하면 새 빈 씬에 Room_BossFsmTest.prefab(바닥+RoomEntry+FioraBoss)을 원점에 배치하고, SampleScene.unity에서 Player를 읽기 전용으로 복제해 RoomEntry 위치에 놓고, 정적 카메라 + 우측 하단 "메인 씬으로" 버튼을 추가한 뒤 `Assets/Scenes/DebugScene.unity`로 저장하고 Build Settings에 등록한다.
  </what-built>
  <how-to-verify>
1. Unity 에디터를 열고 컴파일 에러가 없는지 Console 창에서 확인한다(DebugSceneBuilder.cs/ReturnToMainSceneButton.cs 추가 후 자동 컴파일).
2. 메뉴 `Fast > Debug > Build DebugScene`을 실행한다. Console에 `[DebugSceneBuilder] Created Assets/Scenes/DebugScene.unity ...` 로그가 에러 없이 출력되는지 확인한다.
3. Project 창에서 `Assets/Scenes/DebugScene.unity`가 생성되었는지 확인하고 더블클릭해서 연다. Hierarchy에 Player, Room_BossFsmTest(바닥+FioraBoss 포함), Main Camera, Canvas(우측 하단 버튼 포함), EventSystem이 보이고 **WorldGenerator GameObject가 없는지** 확인한다.
4. DebugScene을 Play 모드로 실행한다. 별도 조작 없이 즉시 Player와 FioraBoss가 고정 좌표에 보이고, 공격 버튼을 눌러 슬로우모션+대시 손맛이 정상 동작하는지 확인한다(층 이동/포탈 없음).
5. 화면 우측 하단 버튼을 클릭한다. SampleScene(메인 씬)으로 즉시 전환되고 정상적으로 플레이되는지 확인한다.
6. Play 모드를 종료하고, `git status`로 `Assets/Scenes/SampleScene.unity`가 변경 목록에 없는지 확인한다(diff 없어야 함).
7. `Edit > Project Settings > ... > Scene List`(또는 File > Build Profiles의 Scene List)에서 DebugScene이 MainMenu/AttackSelect/SampleScene 뒤에 4번째 항목으로 등록되어 있는지 확인한다.
8. 메뉴를 한 번 더 실행해도(멱등성) DebugScene 항목이 중복 추가되지 않는지 확인한다(선택 사항).
  </how-to-verify>
  <verify>
    <automated>git diff --stat -- "Assets/Scenes/SampleScene.unity"</automated>
  </verify>
  <acceptance_criteria>
    - DebugScene.unity 생성됨, WorldGenerator GameObject 없음
    - Play 모드에서 Player+FioraBoss가 고정 좌표에 즉시 존재, 전투 정상 동작
    - 우측 하단 버튼 클릭 시 SampleScene으로 정상 전환
    - SampleScene.unity git diff 없음(수정되지 않음)
    - Build Settings에 DebugScene 4번째 항목으로 등록됨
  </acceptance_criteria>
  <done>사용자가 메뉴 실행 결과를 확인하고 위 5개 기준이 모두 통과함을 확인한다.</done>
  <resume-signal>Type "approved" once DebugScene.unity is built and all 5 acceptance criteria are confirmed, or describe any issue (e.g., compile error, WorldGenerator accidentally present, button not wired, SampleScene modified).</resume-signal>
</task>

</tasks>

<verification>
1. `grep -c "MenuItem(\"Fast/Debug/Build DebugScene\")" Assets/Editor/DebugSceneBuilder.cs` returns 1.
2. `grep -c "WorldGenerator" Assets/Editor/DebugSceneBuilder.cs` returns 0 — DebugScene 생성 도구가 WorldGenerator를 전혀 참조하지 않음.
3. `git diff --stat -- Assets/Scenes/SampleScene.unity` — 빈 출력 (수정 없음).
4. Task 2 체크리스트(Play 모드 실제 실행) 전부 통과 — 자동화 불가, 사용자 수동 확인.
</verification>

<success_criteria>
사용자가 `Assets/Scenes/DebugScene.unity`를 열고 Play를 누르면 WorldGenerator/층 체인 생성 로직과 완전히 무관하게 Player와 FioraBoss가 고정 좌표에서 즉시 전투 가능한 상태로 존재한다. 화면 우측 하단 버튼을 누르면 SampleScene(메인 씬)으로 즉시 전환된다. SampleScene.unity는 이 작업으로 전혀 수정되지 않는다(git diff 없음). Build Settings에 DebugScene이 등록되어 `SceneManager.LoadScene("SampleScene")` 문자열 로드가 정상 동작한다. 이후 보스/적 디버깅 시 층 이동으로 인한 좌표 드리프트 문제가 이 씬에서는 구조적으로 발생하지 않는다.
</success_criteria>

<output>
No SUMMARY.md needed for quick tasks. State update: add row to STATE.md Quick Tasks Completed table after execution.
</output>
