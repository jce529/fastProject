using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu: Fast/Debug/Build DebugScene
/// 층 체인 생성(월드 절차 생성) 로직과 완전히 분리된 디버그 전용 씬을 절차적으로 생성한다.
/// Room_BossFsmTest.prefab(바닥+RoomEntry, RoomBossFsmTestBuilder.cs 산출물)을 원점에 배치하고,
/// BossEnemy.prefab(FioraBoss)을 DebugRoomTeleporter.TeleportToRoom()과 동일하게 RoomEntry 기준
/// 우측 6유닛 위치에 별도로 Instantiate한다(룸 프리팹 자체에는 보스가 nested되어 있지 않음).
/// SampleScene.unity의 Player GameObject를 additive로 열어 읽기 전용 복제한 뒤 RoomEntry 위치에 놓는다.
/// SampleScene.unity는 변경 없이 CloseScene되므로 절대 수정되지 않는다.
/// 우측 하단 버튼(ReturnToMainSceneButton)으로 SampleScene 복귀 가능.
/// </summary>
public static class DebugSceneBuilder
{
    private const string DebugScenePath  = "Assets/Scenes/DebugScene.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string RoomPrefabPath  = "Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab";
    private const string BossPrefabPath  = "Assets/Prefabs/Enemies/BossEnemy.prefab";

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

        // 3b. FioraBoss(BossEnemy.prefab) — DebugRoomTeleporter.TeleportToRoom()의 D-11 스폰 규칙(진입
        //     지점 우측 6유닛)을 그대로 따른다. Room_BossFsmTest.prefab 자체에는 보스가 없다.
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        if (bossPrefab != null)
        {
            Vector3 bossSpawnPos = entryPos + Vector3.right * 6f;
            Object.Instantiate(bossPrefab, bossSpawnPos, Quaternion.identity, roomInstance.transform);
        }
        else
        {
            Debug.LogWarning($"[DebugSceneBuilder] {BossPrefabPath} not found — DebugScene will have no boss to debug.");
        }

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
        Debug.Log($"[DebugSceneBuilder] Created {DebugScenePath} — Room_BossFsmTest + FioraBoss(BossEnemy.prefab) + cloned Player(from SampleScene, read-only) + static camera + bottom-right return button. Build Settings updated (idempotent). SampleScene.unity was opened additively and closed without saving — untouched.");
    }
}
