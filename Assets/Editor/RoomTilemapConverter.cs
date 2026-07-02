using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class RoomTilemapConverter
{
    static readonly string[] RoomNames = {
        "Room_Combat", "Room_Hunt", "Room_Ladder", "Room_LadderDanger",
        "Room_Gap", "Room_Fall", "Room_Sniper", "Room_Stair",
        "Room_Crossroad", "Room_Chase", "Room_Dodge", "Room_Chain",
        "Room_Recovery", "Room_Mixed"
    };

    [MenuItem("Fast/Phase8/Convert Rooms to Tilemap")]
    public static void ConvertAll()
    {
        var (solid, platform) = EnsureTileAssets();
        foreach (var name in RoomNames)
            ConvertRoom(name, solid, platform);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RoomTilemapConverter] All rooms converted.");
    }

    static void ConvertRoom(string roomName, Tile solidTile, Tile platformTile)
    {
        string path = $"Assets/Prefabs/Rooms/{roomName}/{roomName}.prefab";
        if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(path)))
        {
            Debug.LogWarning($"[RoomTilemapConverter] Prefab not found — skip: {path}");
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(path);
        if (root == null)
        {
            Debug.LogError($"[RoomTilemapConverter] Failed to load prefab contents: {path}");
            return;
        }

        // 기존 Grid가 있으면 제거 후 재변환 (타일 에셋 교체 시 GUID 불일치 방지)
        var existingGrid = root.GetComponentInChildren<Grid>();
        if (existingGrid != null)
            Object.DestroyImmediate(existingGrid.gameObject);

        var (tilemapSolid, tilemapPlatform) = CreateTilemapHierarchy(root);

        // 모든 지오메트리 오브젝트에 타일 배치
        var allGeom = new List<GameObject>();
        CollectAllGeometry(root.transform, allGeom);
        foreach (var go in allGeom)
        {
            bool isPlatform = go.GetComponent<PlatformEffector2D>() != null;
            PlaceTiles(isPlatform ? tilemapPlatform : tilemapSolid, go.transform,
                       isPlatform ? platformTile : solidTile);
        }

        // 최상위 레이어9 오브젝트만 삭제 (자식은 자동 삭제됨)
        var topLevel = new List<GameObject>();
        CollectTopLevelGeometry(root.transform, topLevel);
        foreach (var go in topLevel)
            Object.DestroyImmediate(go);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log($"[RoomTilemapConverter] {roomName} converted.");
    }

    // 모든 레벨의 Layer9 + BoxCollider2D(비트리거) 오브젝트 수집 (타일 배치용)
    static void CollectAllGeometry(Transform t, List<GameObject> result)
    {
        foreach (Transform child in t)
        {
            var bc = child.GetComponent<BoxCollider2D>();
            if (child.gameObject.layer == 9 && bc != null && !bc.isTrigger)
                result.Add(child.gameObject);
            CollectAllGeometry(child, result);
        }
    }

    // 부모가 Layer9가 아닌 최상위 지오메트리만 수집 (삭제용)
    static void CollectTopLevelGeometry(Transform t, List<GameObject> result)
    {
        foreach (Transform child in t)
        {
            var bc = child.GetComponent<BoxCollider2D>();
            if (child.gameObject.layer == 9 && bc != null && !bc.isTrigger)
                result.Add(child.gameObject);
            else
                CollectTopLevelGeometry(child, result);
        }
    }

    static void PlaceTiles(Tilemap tilemap, Transform t, Tile tile)
    {
        // lossyScale 사용: Platforms/ 컨테이너 스케일 오프셋 흡수
        Vector3 half = new Vector3(t.lossyScale.x / 2f, t.lossyScale.y / 2f, 0f);
        Vector3Int origin = tilemap.WorldToCell(t.position - half);
        int w = Mathf.RoundToInt(t.lossyScale.x);
        int h = Mathf.RoundToInt(t.lossyScale.y);
        for (int dx = 0; dx < w; dx++)
            for (int dy = 0; dy < h; dy++)
                tilemap.SetTile(origin + new Vector3Int(dx, dy, 0), tile);
    }

    static (Tilemap solid, Tilemap platform) CreateTilemapHierarchy(GameObject root)
    {
        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(root.transform, false);
        gridGO.transform.localPosition = Vector3.zero;
        var grid = gridGO.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        var solidGO = new GameObject("Tilemap_Solid");
        solidGO.transform.SetParent(gridGO.transform, false);
        var tilemapSolid = solidGO.AddComponent<Tilemap>();
        solidGO.AddComponent<TilemapRenderer>();
        solidGO.AddComponent<TilemapCollider2D>();

        var platGO = new GameObject("Tilemap_Platform");
        platGO.transform.SetParent(gridGO.transform, false);
        var tilemapPlatform = platGO.AddComponent<Tilemap>();
        platGO.AddComponent<TilemapRenderer>();
        var col = platGO.AddComponent<TilemapCollider2D>();
        col.usedByEffector = true;
        var eff = platGO.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 180f;

        return (tilemapSolid, tilemapPlatform);
    }

    static (Tile solid, Tile platform) EnsureTileAssets()
    {
        const string dir = "Assets/Tiles";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        var squareSprite = EnsureSquareSprite(dir);

        var solidPath = dir + "/Tile_Solid.asset";
        var solid = AssetDatabase.LoadAssetAtPath<Tile>(solidPath);
        if (solid == null)
        {
            solid = ScriptableObject.CreateInstance<Tile>();
            solid.color = Color.white;
            AssetDatabase.CreateAsset(solid, solidPath);
        }
        solid.sprite = squareSprite;
        EditorUtility.SetDirty(solid);

        var platPath = dir + "/Tile_Platform.asset";
        var plat = AssetDatabase.LoadAssetAtPath<Tile>(platPath);
        if (plat == null)
        {
            plat = ScriptableObject.CreateInstance<Tile>();
            plat.color = Color.white;
            AssetDatabase.CreateAsset(plat, platPath);
        }
        plat.sprite = squareSprite;
        EditorUtility.SetDirty(plat);

        AssetDatabase.SaveAssets();
        return (solid, plat);
    }

    // PPU=32인 32×32 흰색 텍스처 스프라이트 — 그리드 셀(1×1 유닛)을 정확히 채움
    static Sprite EnsureSquareSprite(string dir)
    {
        const string texPath = "Assets/Tiles/TileSquare.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        if (sprite != null) return sprite;

        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        var pixels = new Color32[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(pixels);
        tex.Apply();
        System.IO.File.WriteAllBytes(
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath), texPath),
            tex.EncodeToPNG());
        AssetDatabase.ImportAsset(texPath);

        var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = 32;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
    }
}
