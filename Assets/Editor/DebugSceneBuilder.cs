using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu: Fast/Debug/Build DebugScene
/// 층 체인 생성(월드 절차 생성) 로직과 완전히 분리된 디버그 전용 씬을 절차적으로 생성한다.
/// Room_BossFsmTest.prefab(바닥+RoomEntry+nested BossEnemy(FioraBoss) 인스턴스, RoomBossFsmTestBuilder.cs
/// D-13 산출물)을 원점에 배치한다 — 보스는 룸 프리팹에 이미 심어져 있으므로 별도로 Instantiate하지 않는다.
/// SampleScene.unity의 Player GameObject를 additive로 열어 읽기 전용 복제한 뒤 RoomEntry 위치에 놓는다.
/// SampleScene.unity는 변경 없이 CloseScene되므로 절대 수정되지 않는다.
/// 우측 하단 버튼(ReturnToMainSceneButton)으로 SampleScene 복귀 가능.
/// </summary>
public static class DebugSceneBuilder
{
    private const string DebugScenePath  = "Assets/Scenes/DebugScene.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RoomPrefabPath  = "Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab";
    private const string SamuraiRoomPrefabPath = "Assets/Prefabs/Rooms/Room_SamuraiFsmTest/Room_SamuraiFsmTest.prefab"; // Plan 19-05

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

        // 4. CameraFollow + CameraBound 재사용 — 룸 전체를 아우르는 바운드 안에서만 카메라가 플레이어를
        //    추적한다(Phase 18.1 deviation, 사용자 플레이테스트 피드백). WorldGenerator/DebugRoomTeleporter의
        //    기존 SnapToRoom 배선 패턴과 동일 컨벤션 — DebugSceneCameraBinder가 Start() 1회 연결한다.
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        camGO.transform.position = entryPos + new Vector3(4f, 3f, -10f);
        camGO.AddComponent<AudioListener>();

        var camFollow = camGO.AddComponent<CameraFollow>();
        var camFollowSO = new SerializedObject(camFollow);
        camFollowSO.FindProperty("target").objectReferenceValue = playerCopy.transform;
        camFollowSO.ApplyModifiedProperties();

        var camBinder = camGO.AddComponent<DebugSceneCameraBinder>();
        var camBinderSO = new SerializedObject(camBinder);
        camBinderSO.FindProperty("_cameraFollow").objectReferenceValue   = camFollow;
        camBinderSO.FindProperty("_roomRoot").objectReferenceValue       = roomInstance.transform;
        camBinderSO.ApplyModifiedProperties();

        // 4.5. Room_SamuraiFsmTest 텔레포터 pad + DebugCombatModuleSwitcher (Plan 19-05, D-14/D-18)
        //      SAMURAI-01~05를 DebugScene에서 실제로 플레이 검증할 수 있는 환경 확장 — 기존
        //      Room_BossFsmTest 배치/카메라 로직은 위에서 이미 끝났고, 이 블록은 순수 추가다.
        var samuraiRoomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SamuraiRoomPrefabPath);
        if (samuraiRoomPrefab == null)
        {
            Debug.LogError($"[DebugSceneBuilder] {SamuraiRoomPrefabPath} not found — run 'Fast/Phase19/Build Room_SamuraiFsmTest' first.");
            return;
        }

        var samuraiTeleporterGO = new GameObject("ToSamuraiRoom_Teleporter");
        var samuraiTeleporterCol = samuraiTeleporterGO.AddComponent<BoxCollider2D>();
        samuraiTeleporterCol.isTrigger = true;
        samuraiTeleporterCol.size = new Vector2(1f, 1f);
        samuraiTeleporterGO.transform.position = entryPos + new Vector3(10f, 0f, 0f); // ENT/EXIT 28유닛 스팬 안쪽, 플레이어 스폰에서 도보 접근 가능

        var samuraiTeleporter = samuraiTeleporterGO.AddComponent<DebugRoomTeleporter>();
        var samuraiTeleporterSO = new SerializedObject(samuraiTeleporter);
        samuraiTeleporterSO.FindProperty("targetRoomPrefab").objectReferenceValue = samuraiRoomPrefab; // _bossPrefab은 비워둠 — SamuraiBoss는 룸 프리팹에 이미 nested됨
        samuraiTeleporterSO.ApplyModifiedProperties();

        var moduleSwitcher = playerCopy.AddComponent<DebugCombatModuleSwitcher>();
        var moduleSwitcherSO = new SerializedObject(moduleSwitcher);
        moduleSwitcherSO.FindProperty("_combatController").objectReferenceValue = playerCopy.GetComponent<CombatController>();
        moduleSwitcherSO.ApplyModifiedProperties();

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
        Debug.Log($"[DebugSceneBuilder] Created {DebugScenePath} — Room_BossFsmTest + FioraBoss(BossEnemy.prefab) + cloned Player(from SampleScene, read-only) + static camera + bottom-right return button + Room_SamuraiFsmTest 텔레포터 pad + DebugCombatModuleSwitcher(숫자키 1/2/3). Build Settings updated (idempotent). SampleScene.unity was opened additively and closed without saving — untouched.");
    }
}
