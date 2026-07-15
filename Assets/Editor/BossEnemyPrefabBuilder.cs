using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase15/Build BossEnemy Prefab, Fast/Phase15/Wire Boss Into Room Debug
/// D-10: 신규 아트 없이 MeleeEnemy.prefab 구조(Rigidbody2D/CapsuleCollider2D/SpriteRenderer/Animator/
/// ExclamationIcon/MeleeHitbox)를 복제하되 MeleeEnemy 컴포넌트를 BossEnemy로 교체하고, 크기 1.6배 확대 +
/// 진한 붉은색 색조로 "보스처럼 보이게" 변형한다. 기존 MeleeEnemy.prefab은 건드리지 않는다(정밀한 변경).
/// D-11: 생성된 프리팹을 Room_Debug.prefab의 모든 DebugRoomTeleporter 인스턴스 _bossPrefab 필드에 배선한다.
/// </summary>
public static class BossEnemyPrefabBuilder
{
    private const string SourcePrefabPath = "Assets/Prefabs/Enemies/MeleeEnemy.prefab";
    private const string TargetPrefabPath = "Assets/Prefabs/Enemies/BossEnemy.prefab";
    private const string RoomDebugPath    = "Assets/Prefabs/Rooms/Room_Debug/Room_Debug.prefab";
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

        var boss = clone.AddComponent<BossEnemy>();

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

    [MenuItem("Fast/Phase15/Wire Boss Into Room Debug")]
    public static void WireBossIntoRoomDebug()
    {
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TargetPrefabPath);
        if (bossPrefab == null)
        {
            Debug.LogError($"[BossEnemyPrefabBuilder] {TargetPrefabPath} not found — run 'Build BossEnemy Prefab' first.");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(RoomDebugPath);
        if (root == null)
        {
            Debug.LogError($"[BossEnemyPrefabBuilder] Prefab not found: {RoomDebugPath}");
            return;
        }

        int wired = 0;
        foreach (var teleporter in root.GetComponentsInChildren<DebugRoomTeleporter>(true))
        {
            var so = new SerializedObject(teleporter);
            var prop = so.FindProperty("_bossPrefab");
            if (prop.objectReferenceValue == bossPrefab) continue; // 멱등성 — 이미 배선됨
            prop.objectReferenceValue = bossPrefab;
            so.ApplyModifiedProperties();
            wired++;
        }

        PrefabUtility.SaveAsPrefabAsset(root, RoomDebugPath);
        PrefabUtility.UnloadPrefabContents(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[BossEnemyPrefabBuilder] Wired _bossPrefab into {wired} DebugRoomTeleporter instance(s) in Room_Debug.prefab.");
    }
}
