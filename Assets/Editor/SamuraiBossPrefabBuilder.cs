using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase19/Build SamuraiBoss Prefab
/// D-14(Phase 18/18.1 선례대로 DebugScene 확장): 신규 아트 없이 MeleeEnemy.prefab 구조를 복제하되
/// MeleeEnemy 컴포넌트를 SamuraiBoss로 교체하고, 크기 1.5배 확대 + 청회색 색조로 변형한다.
/// BossEnemyPrefabBuilder.cs(Plan 15-03, D-10)의 클론/컴포넌트교체/compound-collider 패턴을 그대로 미러링한다.
/// 기존 MeleeEnemy.prefab/BossEnemy.prefab은 전혀 건드리지 않는다(정밀한 변경).
/// </summary>
public static class SamuraiBossPrefabBuilder
{
    private const string SourcePrefabPath = "Assets/Prefabs/Enemies/MeleeEnemy.prefab";
    private const string TargetPrefabPath = "Assets/Prefabs/Enemies/SamuraiBoss.prefab";
    private static readonly Vector3 BossScale = new Vector3(1.5f, 1.5f, 1f);    // Fiora(1.6배)와 살짝 다르게 — 시각적 구분
    private static readonly Color   BossTint  = new Color(0.25f, 0.35f, 0.6f);  // 청회색 — Fiora의 진한 붉은색과 구분

    [MenuItem("Fast/Phase19/Build SamuraiBoss Prefab")]
    public static void BuildSamuraiBossPrefab()
    {
        var source = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        if (source == null)
        {
            Debug.LogError($"[SamuraiBossPrefabBuilder] Source prefab not found: {SourcePrefabPath}");
            return;
        }

        var clone = Object.Instantiate(source);
        clone.name = "SamuraiBoss";
        PrefabUtility.UnloadPrefabContents(source);

        var oldMelee = clone.GetComponent<MeleeEnemy>();
        if (oldMelee != null) Object.DestroyImmediate(oldMelee);

        var boss = clone.AddComponent<SamuraiBoss>();

        var exclamation   = clone.transform.Find("ExclamationIcon")?.GetComponent<SpriteRenderer>();
        var meleeHitboxGO = clone.transform.Find("MeleeHitbox");
        if (exclamation == null || meleeHitboxGO == null)
        {
            Debug.LogError("[SamuraiBossPrefabBuilder] ExclamationIcon/MeleeHitbox child not found on cloned MeleeEnemy structure.");
            Object.DestroyImmediate(clone);
            return;
        }

        // BossEnemyPrefabBuilder.cs와 동일 패턴(18.1-01 교훈) — 자식 MeleeHitbox 오브젝트를 제거하고
        // 루트 바디 콜라이더와 동일 모양의 트리거 콜라이더를 루트에 compound로 추가한다.
        // 자식 오프셋이 루트 스케일과 어긋나 재발하던 정렬 버그를 구조적으로 제거하기 위함.
        Object.DestroyImmediate(meleeHitboxGO.gameObject);

        var bodyCapsule = clone.GetComponent<CapsuleCollider2D>();
        var hitboxCapsule = clone.AddComponent<CapsuleCollider2D>();
        hitboxCapsule.isTrigger = true;
        hitboxCapsule.direction = bodyCapsule.direction;
        hitboxCapsule.offset    = bodyCapsule.offset;
        hitboxCapsule.size      = bodyCapsule.size * 1.08f; // 살짝 크게 — 경계 정밀도 여유

        var so = new SerializedObject(boss);
        so.FindProperty("_exclamationIcon").objectReferenceValue = exclamation;
        so.FindProperty("_meleeHitbox").objectReferenceValue     = hitboxCapsule;
        so.ApplyModifiedProperties();

        clone.transform.localScale = BossScale;
        var sr = clone.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = BossTint; // SamuraiBoss.Awake()가 _baseColor로 캡처

        PrefabUtility.SaveAsPrefabAsset(clone, TargetPrefabPath);
        Object.DestroyImmediate(clone);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[SamuraiBossPrefabBuilder] Created {TargetPrefabPath} — scale {BossScale}, tint {BossTint}.");
    }
}
