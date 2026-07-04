using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Fast/Phase8/Build Corridors
/// Corridor 3종(Flat/Up/Down) 프리팹을 Assets/Prefabs/Corridors/ 아래에 생성한다.
/// 각 Corridor는 Grid→Tilemap_Solid 계층(TilemapCollider2D), EnemySpawnPoint 태그 자식, 양 끝 RoomConnector 마커를 포함한다.
/// Phase 9 WorldGenerator가 이 프리팹을 랜덤 선택하여 Room 사이에 배치한다.
/// </summary>
public static class CorridorBuilder
{
    [MenuItem("Fast/Phase8/Build Corridors")]
    public static void Run()
    {
        var tile = EnsureSolidTile();
        BuildCorridor_Flat(tile);
        BuildCorridor_Up(tile);
        BuildCorridor_Down(tile);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CorridorBuilder] All 3 corridors built.");
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Corridor builders
    // ──────────────────────────────────────────────────────────────────────────

    private static void BuildCorridor_Flat(Tile tile)
    {
        const string corridorName = "Corridor_Flat";
        var (root, tm) = NewCorridor(corridorName);

        PRow(tm, tile, -6, 5, 0);   // 12-tile floor, surface y=1

        var ent  = CreateMarker(root, "ENT",  new Vector3(-6f, 1f, 0f));
        var exit = CreateMarker(root, "EXIT", new Vector3( 6f, 1f, 0f));
        AddConnector(ent,  RoomConnector.Direction.Left);
        AddConnector(exit, RoomConnector.Direction.Right);

        CreateSpawnPoint(root, "EnemySpawn_0", new Vector3(0f, 1f, 0f));

        SavePrefab(root, corridorName);
    }

    private static void BuildCorridor_Up(Tile tile)
    {
        const string corridorName = "Corridor_Up";
        var (root, tm) = NewCorridor(corridorName);

        PRow(tm, tile, -6, -2, 0);  // Step A: surface y=1
        PRow(tm, tile, -1,  3, 2);  // Step B: surface y=3
        PRow(tm, tile,  4,  7, 4);  // Step C: surface y=5

        var ent  = CreateMarker(root, "ENT",  new Vector3(-6f, 1f, 0f));
        var exit = CreateMarker(root, "EXIT", new Vector3( 8f, 5f, 0f));
        AddConnector(ent,  RoomConnector.Direction.Left);
        AddConnector(exit, RoomConnector.Direction.Right);

        CreateSpawnPoint(root, "EnemySpawn_0", new Vector3(5f, 5f, 0f));

        SavePrefab(root, corridorName);
    }

    private static void BuildCorridor_Down(Tile tile)
    {
        const string corridorName = "Corridor_Down";
        var (root, tm) = NewCorridor(corridorName);

        PRow(tm, tile, -7, -3, 4);  // Step A: surface y=5
        PRow(tm, tile, -2,  2, 2);  // Step B: surface y=3
        PRow(tm, tile,  3,  6, 0);  // Step C: surface y=1

        var ent  = CreateMarker(root, "ENT",  new Vector3(-7f, 5f, 0f));
        var exit = CreateMarker(root, "EXIT", new Vector3( 7f, 1f, 0f));
        AddConnector(ent,  RoomConnector.Direction.Left);
        AddConnector(exit, RoomConnector.Direction.Right);

        CreateSpawnPoint(root, "EnemySpawn_0", new Vector3(-5f, 5f, 0f));

        SavePrefab(root, corridorName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Tilemap helpers (RoomCreator 패턴 동일)
    // ──────────────────────────────────────────────────────────────────────────

    private static (GameObject root, Tilemap tilemap) NewCorridor(string corridorName)
    {
        var root   = new GameObject(corridorName);
        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(root.transform, false);
        gridGO.AddComponent<Grid>().cellSize = Vector3.one;

        var solidGO = new GameObject("Tilemap_Solid");
        solidGO.transform.SetParent(gridGO.transform, false);
        solidGO.layer = 9; // Platform
        var tilemap = solidGO.AddComponent<Tilemap>();
        solidGO.AddComponent<TilemapRenderer>();
        solidGO.AddComponent<TilemapCollider2D>();

        return (root, tilemap);
    }

    private static void PRow(Tilemap tm, Tile tile, int x0, int x1, int y)
    {
        for (int x = x0; x <= x1; x++)
            tm.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private static Tile EnsureSolidTile()
    {
        const string path = "Assets/Tiles/Tile_Solid.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;

        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        tile = ScriptableObject.CreateInstance<Tile>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/TileSquare.png");
        if (sprite != null) tile.sprite = sprite;
        tile.color = Color.white;
        AssetDatabase.CreateAsset(tile, path);
        AssetDatabase.SaveAssets();

        if (sprite == null)
            Debug.LogWarning("[CorridorBuilder] TileSquare.png 없음 — Assets/Tiles/Tile_Solid.asset 수동 확인 필요");
        return tile;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Marker / connector helpers (unchanged)
    // ──────────────────────────────────────────────────────────────────────────

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
