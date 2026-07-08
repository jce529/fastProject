using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase12/Build Portal Effect Prefab -- D-02: 신규 아트 없이 기존 ExitPortal
/// 스프라이트(Portal_100x100px1.png)를 재활용해 퇴장 연출용 PortalEffect 프리팹을 생성한다.
/// Collider 없음 -- 비주얼 전용(10-TRANSITION-DESIGN.md).
/// </summary>
public static class PortalEffectBuilder
{
    private const string AssetPath = "Assets/Prefabs/World/PortalEffect/PortalEffect.prefab";
    private const string SpritePath = "Assets/Prefabs/Portal/Portal_100x100px1.png";

    [MenuItem("Fast/Phase12/Build Portal Effect Prefab")]
    public static void Run()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null)
        {
            Debug.LogError($"[PortalEffectBuilder] Sprite not found at {SpritePath}");
            return;
        }

        var root = new GameObject("PortalEffect");
        var sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        // D-01: 비주얼 전용 프리팹 -- Collider2D를 의도적으로 추가하지 않는다.

        SavePrefab(root);
    }

    private static void SavePrefab(GameObject root)
    {
        string fullDir = Path.Combine(Application.dataPath, "Prefabs", "World", "PortalEffect");
        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath) != null)
            AssetDatabase.DeleteAsset(AssetPath);

        AssetDatabase.Refresh();

        PrefabUtility.SaveAsPrefabAsset(root, AssetPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PortalEffectBuilder] PortalEffect built at {AssetPath}");
    }
}
