using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase15/Build BossEnemy Prefab, Fast/Phase15/Wire Boss Into Room Debug
/// D-10: 신규 아트 없이 MeleeEnemy.prefab 구조(Rigidbody2D/CapsuleCollider2D/SpriteRenderer/Animator/
/// ExclamationIcon/MeleeHitbox)를 복제하되 MeleeEnemy 컴포넌트를 BossEnemy로 교체하고, 크기 1.6배 확대 +
/// 진한 붉은색 색조로 "보스처럼 보이게" 변형한다. 기존 MeleeEnemy.prefab은 건드리지 않는다(정밀한 변경).
/// D-11 (RE-RESOLVED 2026-07-15, checker fix): 생성된 프리팹을 Room_Debug.prefab이 아니라
/// 영속 씬(Assets/Scenes/SampleScene.unity)에 독립적으로 배치한 "BossFsmTest_Teleporter" GameObject에
/// 배선한다 — Room_Debug.prefab은 이 플랜에서 전혀 열리거나 수정되지 않는다(Phase 16 삭제 대상이므로
/// 그 생명주기와 완전히 무관해야 함). targetRoomPrefab은 독립 프리팹 Room_BossFsmTest.prefab을 가리킨다.
/// </summary>
public static class BossEnemyPrefabBuilder
{
    private const string SourcePrefabPath = "Assets/Prefabs/Enemies/MeleeEnemy.prefab";
    private const string TargetPrefabPath = "Assets/Prefabs/Enemies/BossEnemy.prefab";
    private const string BossFsmTestRoomPath       = "Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab";
    private const string BossFsmTestTeleporterName = "BossFsmTest_Teleporter";
    private const string TargetSceneName           = "SampleScene";
    private static readonly Vector3 BossScale = new Vector3(1.6f, 1.6f, 1f);   // D-10: 크기 확대
    private static readonly Color   BossTint  = new Color(0.7f, 0.15f, 0.2f);  // D-10: 진한 붉은색 색조

    [MenuItem("Fast/Phase15/Build BossEnemy Prefab")]
    public static void BuildBossEnemyPrefab()
    {
        var source = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        if (source == null)
        {
            Debug.LogError($"[BossEnemyPrefabBuilder] Source prefab not found: {SourcePrefabPath}");
            return;
        }

        var clone = Object.Instantiate(source);
        clone.name = "BossEnemy";
        PrefabUtility.UnloadPrefabContents(source);

        var oldMelee = clone.GetComponent<MeleeEnemy>();
        if (oldMelee != null) Object.DestroyImmediate(oldMelee);

        var boss = clone.AddComponent<FioraBoss>();

        var exclamation = clone.transform.Find("ExclamationIcon")?.GetComponent<SpriteRenderer>();
        var hitbox      = clone.transform.Find("MeleeHitbox")?.GetComponent<Collider2D>();
        if (exclamation == null || hitbox == null)
        {
            Debug.LogError("[BossEnemyPrefabBuilder] ExclamationIcon/MeleeHitbox child not found on cloned MeleeEnemy structure.");
            Object.DestroyImmediate(clone);
            return;
        }

        var so = new SerializedObject(boss);
        so.FindProperty("_exclamationIcon").objectReferenceValue = exclamation;
        so.FindProperty("_meleeHitbox").objectReferenceValue     = hitbox;
        so.ApplyModifiedProperties();

        clone.transform.localScale = BossScale; // D-10: 크기 확대
        var sr = clone.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = BossTint;    // D-10: 색조 변형 — BossEnemy.Awake()가 _baseColor로 캡처

        PrefabUtility.SaveAsPrefabAsset(clone, TargetPrefabPath);
        Object.DestroyImmediate(clone);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BossEnemyPrefabBuilder] Created {TargetPrefabPath} — scale {BossScale}, tint {BossTint}.");
    }

    [MenuItem("Fast/Phase15/Wire Boss Into BossFsmTest Room")]
    public static void WireBossIntoBossFsmTestRoom()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (activeScene.name != TargetSceneName)
        {
            Debug.LogError($"[BossEnemyPrefabBuilder] '{TargetSceneName}'이 열려있지 않습니다 (현재: '{activeScene.name}'). Assets/Scenes/SampleScene.unity를 열고 다시 실행하세요.");
            return;
        }

        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
        if (bossPrefab == null)
        {
            Debug.LogError($"[BossEnemyPrefabBuilder] {TargetPrefabPath} not found — run 'Build BossEnemy Prefab' first.");
            return;
        }

        var testRoomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossFsmTestRoomPath);
        if (testRoomPrefab == null)
        {
            Debug.LogError($"[BossEnemyPrefabBuilder] {BossFsmTestRoomPath} not found — run 'Fast/Phase15/Build Room_BossFsmTest' first.");
            return;
        }

        // D-11 RE-RESOLVED (checker fix): Room_Debug.prefab은 절대 열지 않는다 —
        // 영속 씬에 독립 오브젝트로 이름 기준 find-or-create 한다.
        var teleporterGO = GameObject.Find(BossFsmTestTeleporterName);
        if (teleporterGO == null)
        {
            teleporterGO = new GameObject(BossFsmTestTeleporterName);

            var col = teleporterGO.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 1f); // Room_Debug 레거시 teleporter 트리거와 동일 규격

            var player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            Vector3 basePos = player != null ? player.transform.position : Vector3.zero;
            teleporterGO.transform.position = basePos + new Vector3(6f, 3f, 0f); // 플레이어 스폰 근처 — 기존 지오메트리와 겹치지 않는 임시 위치, 부적절하면 Task 3에서 Inspector로 수동 조정

            teleporterGO.AddComponent<DebugRoomTeleporter>();
            // 스프라이트 미부착 — DebugRoomTeleporter.OnDrawGizmos()의 시안색 Gizmo만으로 시각 확인 충분 (ExitPortalBuilder와 동일 컨벤션)
        }

        var teleporter = teleporterGO.GetComponent<DebugRoomTeleporter>();
        var so = new SerializedObject(teleporter);
        so.FindProperty("targetRoomPrefab").objectReferenceValue = testRoomPrefab;
        so.FindProperty("_bossPrefab").objectReferenceValue      = bossPrefab;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(teleporterGO);
        EditorSceneManager.MarkSceneDirty(teleporterGO.scene);
        EditorSceneManager.SaveScene(teleporterGO.scene);

        Debug.Log($"[BossEnemyPrefabBuilder] Wired '{BossFsmTestTeleporterName}' in {TargetSceneName} → targetRoomPrefab={BossFsmTestRoomPath}, _bossPrefab={TargetPrefabPath}. Room_Debug.prefab was not opened or modified.");
    }
}
