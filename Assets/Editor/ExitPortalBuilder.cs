using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase10/Build Exit Portal Prefab
/// ExitPortal 프리팹을 Assets/Prefabs/World/ExitPortal/ 아래에 생성한다.
/// BoxCollider2D(isTrigger) + ExitPortal 컴포넌트만 포함한다.
/// 시각 이펙트/스프라이트는 REQUIREMENTS.md Out of Scope에 따라 의도적으로 제외 --
/// 확인은 ExitPortal.OnDrawGizmos()의 Gizmo만으로 충분하다.
/// </summary>
public static class ExitPortalBuilder
{
    private const string AssetPath = "Assets/Prefabs/World/ExitPortal/ExitPortal.prefab";

    [MenuItem("Fast/Phase10/Build Exit Portal Prefab")]
    public static void Run()
    {
        var root = new GameObject("ExitPortal");

        var col = root.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.5f, 2.5f);
        col.offset = new Vector2(0f, 1.25f);

        root.AddComponent<ExitPortal>();

        SavePrefab(root);
    }

    private static void SavePrefab(GameObject root)
    {
        string fullDir = Path.Combine(Application.dataPath, "Prefabs", "World", "ExitPortal");
        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath) != null)
            AssetDatabase.DeleteAsset(AssetPath);

        AssetDatabase.Refresh();

        PrefabUtility.SaveAsPrefabAsset(root, AssetPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ExitPortalBuilder] ExitPortal built at {AssetPath}");
    }
}
