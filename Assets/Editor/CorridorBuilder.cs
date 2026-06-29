using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase8/Build Corridors
/// Corridor 3종(Flat/Up/Down) 프리팹을 Assets/Prefabs/Corridors/ 아래에 생성한다.
/// 각 Corridor는 계단 플랫폼, EnemySpawnPoint 태그 자식, 양 끝 RoomConnector 마커를 포함한다.
/// Phase 9 WorldGenerator가 이 프리팹을 랜덤 선택하여 Room 사이에 배치한다.
/// </summary>
public static class CorridorBuilder
{
    private const int PLATFORM_LAYER = 9; // TagManager: Platform

    [MenuItem("Fast/Phase8/Build Corridors")]
    public static void Run()
    {
        BuildCorridor_Flat();
        BuildCorridor_Up();
        BuildCorridor_Down();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CorridorBuilder] All 3 corridors built.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Corridor builders
    // ──────────────────────────────────────────────────────────────────────────

    private static void BuildCorridor_Flat()
    {
        const string corridorName = "Corridor_Flat";
        var root = new GameObject(corridorName);

        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);

        CreatePlatform(geo, "Floor", new Vector3(0f, 0f, 0f), 12f, 1f);

        var ent  = CreateMarker(root, "ENT",  new Vector3(-6f, 0f, 0f));
        var exit = CreateMarker(root, "EXIT", new Vector3( 6f, 0f, 0f));
        AddConnector(ent,  RoomConnector.Direction.Left);
        AddConnector(exit, RoomConnector.Direction.Right);

        CreateSpawnPoint(root, "EnemySpawn_0", new Vector3(0f, 1f, 0f));

        SavePrefab(root, corridorName);
    }

    private static void BuildCorridor_Up()
    {
        const string corridorName = "Corridor_Up";
        var root = new GameObject(corridorName);

        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);

        CreatePlatform(geo, "Step_A", new Vector3(-4f, 0f, 0f), 5f, 1f);
        CreatePlatform(geo, "Step_B", new Vector3( 1f, 2f, 0f), 5f, 1f);
        CreatePlatform(geo, "Step_C", new Vector3( 5f, 4f, 0f), 5f, 1f);

        var ent  = CreateMarker(root, "ENT",  new Vector3(-6f, 0f, 0f));
        var exit = CreateMarker(root, "EXIT", new Vector3( 7f, 4f, 0f));
        AddConnector(ent,  RoomConnector.Direction.Left);
        AddConnector(exit, RoomConnector.Direction.Right);

        CreateSpawnPoint(root, "EnemySpawn_0", new Vector3(5f, 5f, 0f));

        SavePrefab(root, corridorName);
    }

    private static void BuildCorridor_Down()
    {
        const string corridorName = "Corridor_Down";
        var root = new GameObject(corridorName);

        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);

        CreatePlatform(geo, "Step_A", new Vector3(-5f, 4f, 0f), 5f, 1f);
        CreatePlatform(geo, "Step_B", new Vector3(-1f, 2f, 0f), 5f, 1f);
        CreatePlatform(geo, "Step_C", new Vector3( 4f, 0f, 0f), 5f, 1f);

        var ent  = CreateMarker(root, "ENT",  new Vector3(-7f, 4f, 0f));
        var exit = CreateMarker(root, "EXIT", new Vector3( 6f, 0f, 0f));
        AddConnector(ent,  RoomConnector.Direction.Left);
        AddConnector(exit, RoomConnector.Direction.Right);

        CreateSpawnPoint(root, "EnemySpawn_0", new Vector3(-5f, 5f, 0f));

        SavePrefab(root, corridorName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers (RoomPrefabBuilder 패턴 재사용)
    // ──────────────────────────────────────────────────────────────────────────

    private static GameObject CreatePlatform(GameObject parent, string name, Vector3 pos, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;
        go.layer = PLATFORM_LAYER;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);

        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = uiSprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(width, height);

        return go;
    }

    private static GameObject CreateMarker(GameObject parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        return go;
    }

    private static void AddConnector(GameObject markerGO, RoomConnector.Direction dir)
    {
        var rc = markerGO.AddComponent<RoomConnector>();
        rc.direction = dir;
    }

    private static void CreateSpawnPoint(GameObject parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        try
        {
            go.tag = "EnemySpawnPoint";
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[CorridorBuilder] Tag 'EnemySpawnPoint' not defined in TagManager — {e.Message}");
        }
    }

    private static void SavePrefab(GameObject root, string corridorName)
    {
        string fullDir = Path.Combine(Application.dataPath, "Prefabs", "Corridors", corridorName);
        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);

        string assetPath = $"Assets/Prefabs/Corridors/{corridorName}/{corridorName}.prefab";

        // Idempotent: remove old prefab before overwriting
        if (AssetDatabase.LoadAssetAtPath<GameObject>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);

        AssetDatabase.Refresh(); // ensure Unity sees the new directory

        PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[CorridorBuilder] {corridorName} built at {assetPath}");
    }
}
