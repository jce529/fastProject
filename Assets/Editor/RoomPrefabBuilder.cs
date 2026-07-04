using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Build Room Prefabs
/// Creates 14 Room prefabs under Assets/Prefabs/Rooms/[RoomName]/[RoomName].prefab
/// based on 각 룸 상세 최종.md specifications.
/// </summary>
public static class RoomPrefabBuilder
{
    private const int PLATFORM_LAYER = 9; // TagManager: Platform

    [MenuItem("Fast/Build Room Prefabs")]
    public static void Run()
    {
        BuildRoom_Combat();
        BuildRoom_Hunt();
        BuildRoom_Ladder();
        BuildRoom_LadderDanger();
        BuildRoom_Gap();
        BuildRoom_Fall();
        BuildRoom_Sniper();
        BuildRoom_Stair();
        BuildRoom_Crossroad();
        BuildRoom_Chase();
        BuildRoom_Dodge();
        BuildRoom_Chain();
        BuildRoom_Recovery();
        BuildRoom_Mixed();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Room builders
    // ──────────────────────────────────────────────────────────────────────────

    private static void BuildRoom_Combat()
    {
        const string roomName = "Room_Combat";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        CreatePlatform(geo, "Floor",      new Vector3(0f,     0f, 0f), 30f, 1f);
        CreatePlatform(geo, "WallLeft",   new Vector3(-14.5f, 4f, 0f), 1f,  8f);
        CreatePlatform(geo, "WallRight",  new Vector3(14.5f,  4f, 0f), 1f,  8f);

        CreateMarker(spawns, "EnemySpawn_0", new Vector3(-8f, 1f, 0f));
        CreateMarker(spawns, "EnemySpawn_1", new Vector3( 0f, 1f, 0f));
        CreateMarker(spawns, "EnemySpawn_2", new Vector3( 8f, 1f, 0f));

        CreateMarker(root, "ENT",  new Vector3(-14f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 14f, 0f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Hunt()
    {
        const string roomName = "Room_Hunt";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        CreatePlatform(geo, "Floor",           new Vector3( 0f, 0f, 0f), 24f, 1f);
        CreatePlatform(geo, "PlatformLowLeft", new Vector3(-9f, 2f, 0f),  6f, 1f);
        CreatePlatform(geo, "PlatformMid",     new Vector3(-1f, 4f, 0f),  8f, 1f);
        CreatePlatform(geo, "PlatformHigh",    new Vector3( 6f, 6f, 0f),  6f, 1f);

        CreateMarker(spawns, "EnemySpawn_0", new Vector3(-9f, 3f, 0f));
        CreateMarker(spawns, "EnemySpawn_1", new Vector3(-1f, 5f, 0f));
        CreateMarker(spawns, "EnemySpawn_2", new Vector3( 6f, 7f, 0f));

        CreateMarker(root, "ENT",  new Vector3(-11f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 11f, 7f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Ladder()
    {
        const string roomName = "Room_Ladder";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);

        CreatePlatform(geo, "Floor",     new Vector3(  0f,    0f, 0f),  6f,  1f);
        CreatePlatform(geo, "Ceiling",   new Vector3(  0f, 19.5f, 0f),  6f,  1f);
        CreatePlatform(geo, "WallLeft",  new Vector3(-2.5f,  10f, 0f),  1f, 20f);
        CreatePlatform(geo, "WallRight", new Vector3( 2.5f,  10f, 0f),  1f, 20f);

        CreateMarker(root, "[Ladder]", new Vector3(0f, 10f, 0f));
        CreateMarker(root, "ENT",      new Vector3(0f,  0f, 0f));
        CreateMarker(root, "EXIT",     new Vector3(0f, 19f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_LadderDanger()
    {
        const string roomName = "Room_LadderDanger";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        CreatePlatform(geo, "Floor",     new Vector3(  0f,    0f, 0f),  6f,  1f);
        CreatePlatform(geo, "Ceiling",   new Vector3(  0f, 19.5f, 0f),  6f,  1f);
        CreatePlatform(geo, "WallLeft",  new Vector3(-2.5f,  10f, 0f),  1f, 20f);
        CreatePlatform(geo, "WallRight", new Vector3( 2.5f,  10f, 0f),  1f, 20f);

        // Danger platform (ranged enemy) — colored hint
        var dangerPlat = CreatePlatform(geo, "PlatformRanged", new Vector3(-5f, 12f, 0f), 4f, 1f);
        var sr = dangerPlat.GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = new Color(1f, 0.6f, 0.6f, 1f);

        CreatePlatform(geo, "PlatformMelee", new Vector3(4f, 8f, 0f), 4f, 1f);

        CreateMarker(root, "[Ladder]", new Vector3(0f, 10f, 0f));

        CreateMarker(spawns, "EnemySpawn_0", new Vector3(-5f, 13f, 0f));
        CreateMarker(spawns, "EnemySpawn_1", new Vector3( 4f,  9f, 0f));

        CreateMarker(root, "ENT",  new Vector3(0f,  0f, 0f));
        CreateMarker(root, "EXIT", new Vector3(0f, 19f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Gap()
    {
        const string roomName = "Room_Gap";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);

        CreatePlatform(geo, "Platform_A", new Vector3(-12f, 0f, 0f), 5f, 1f);
        CreatePlatform(geo, "Platform_B", new Vector3( -4f, 0f, 0f), 5f, 1f);
        CreatePlatform(geo, "Platform_C", new Vector3(  4f, 0f, 0f), 5f, 1f);
        CreatePlatform(geo, "Platform_D", new Vector3( 12f, 0f, 0f), 5f, 1f);

        CreateKillZone(root, "KillZone", new Vector3(0f, -3f, 0f), 28f, 2f);

        CreateMarker(root, "ENT",  new Vector3(-14f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 14f, 1f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Fall()
    {
        const string roomName = "Room_Fall";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);

        CreatePlatform(geo, "Platform_A", new Vector3(-9f, 0f, 0f), 4f, 1f);
        CreatePlatform(geo, "Platform_B", new Vector3( 0f, 2f, 0f), 3f, 1f);
        CreatePlatform(geo, "Platform_C", new Vector3( 9f, 1f, 0f), 4f, 1f);

        CreateKillZone(root, "KillZone", new Vector3(0f, -3f, 0f), 24f, 2f);

        CreateMarker(root, "ENT",  new Vector3(-11f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 11f, 2f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Sniper()
    {
        const string roomName = "Room_Sniper";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        CreatePlatform(geo, "FloorGround",   new Vector3(  0f,   0f, 0f), 22f, 1f);
        CreatePlatform(geo, "Cover",         new Vector3(-10f, 1.5f, 0f),  3f, 2f);
        CreatePlatform(geo, "HighGround",    new Vector3( 11f,   4f, 0f),  8f, 1f);

        CreateMarker(spawns, "EnemySpawn_0", new Vector3(11f, 5f, 0f));
        CreateMarker(spawns, "EnemySpawn_1", new Vector3(-4f, 1f, 0f));
        CreateMarker(spawns, "EnemySpawn_2", new Vector3( 3f, 1f, 0f));

        CreateMarker(root, "ENT",  new Vector3(-14f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 14f, 5f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Stair()
    {
        const string roomName = "Room_Stair";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);

        CreatePlatform(geo, "Step_0", new Vector3(-10f, 0f, 0f), 5f, 1f);
        CreatePlatform(geo, "Step_1", new Vector3( -5f, 2f, 0f), 5f, 1f);
        CreatePlatform(geo, "Step_2", new Vector3(  0f, 4f, 0f), 5f, 1f);
        CreatePlatform(geo, "Step_3", new Vector3(  5f, 6f, 0f), 5f, 1f);
        CreatePlatform(geo, "Step_4", new Vector3( 10f, 8f, 0f), 5f, 1f);

        CreateMarker(root, "ENT",  new Vector3(-12f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 12f, 9f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Crossroad()
    {
        const string roomName = "Room_Crossroad";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        CreatePlatform(geo, "FloorEntry",    new Vector3(-13f, 0f, 0f),  6f, 1f);
        CreatePlatform(geo, "UpperPath",     new Vector3(  0f, 5f, 0f), 18f, 1f);
        CreatePlatform(geo, "LowerPlat_A",   new Vector3( -5f, 0f, 0f),  4f, 1f);
        CreatePlatform(geo, "LowerPlat_B",   new Vector3(  3f, 0f, 0f),  4f, 1f);
        CreatePlatform(geo, "LowerPlat_C",   new Vector3( 10f, 0f, 0f),  4f, 1f);
        CreatePlatform(geo, "FloorExit",     new Vector3( 15f, 0f, 0f),  4f, 1f);

        CreateMarker(spawns, "EnemySpawn_0", new Vector3(-6f, 6f, 0f));
        CreateMarker(spawns, "EnemySpawn_1", new Vector3( 0f, 6f, 0f));
        CreateMarker(spawns, "EnemySpawn_2", new Vector3( 6f, 6f, 0f));

        CreateKillZone(root, "KillZone", new Vector3(2f, -3f, 0f), 20f, 2f);

        CreateMarker(root, "ENT",  new Vector3(-15f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 15f, 1f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Chase()
    {
        const string roomName = "Room_Chase";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        CreatePlatform(geo, "Floor",   new Vector3(0f,   0f,  0f), 36f, 1f);
        CreatePlatform(geo, "Ceiling", new Vector3(0f, 5.5f,  0f), 36f, 1f);

        CreateMarker(spawns, "BossChaseSpawn", new Vector3(-16f, 1f, 0f));

        CreateMarker(root, "ENT",  new Vector3(-17f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 17f, 0f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Dodge()
    {
        const string roomName = "Room_Dodge";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        CreatePlatform(geo, "Floor",     new Vector3(  0f,   0f, 0f), 20f, 1f);
        CreatePlatform(geo, "Ceiling",   new Vector3(  0f, 5.5f, 0f), 20f, 1f);
        CreatePlatform(geo, "WallLeft",  new Vector3(-9.5f,  3f, 0f),  1f, 6f);
        CreatePlatform(geo, "WallRight", new Vector3( 9.5f,  3f, 0f),  1f, 6f);

        CreateMarker(spawns, "BossDodgeSpawn", new Vector3(8f, 1f, 0f));

        CreateMarker(root, "ENT",  new Vector3(-8f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 8f, 0f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Chain()
    {
        const string roomName = "Room_Chain";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        CreatePlatform(geo, "Step_0", new Vector3(-9f, 0f, 0f), 6f, 1f);
        CreatePlatform(geo, "Step_1", new Vector3(-3f, 3f, 0f), 6f, 1f);
        CreatePlatform(geo, "Step_2", new Vector3( 3f, 6f, 0f), 6f, 1f);
        CreatePlatform(geo, "Step_3", new Vector3( 9f, 9f, 0f), 6f, 1f);

        CreateMarker(spawns, "EnemySpawn_0", new Vector3(-9f,  1f, 0f));
        CreateMarker(spawns, "EnemySpawn_1", new Vector3(-3f,  4f, 0f));
        CreateMarker(spawns, "EnemySpawn_2", new Vector3( 3f,  7f, 0f));
        CreateMarker(spawns, "EnemySpawn_3", new Vector3( 9f, 10f, 0f));

        CreateMarker(root, "ENT",  new Vector3(-11f,  0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 11f, 10f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Recovery()
    {
        const string roomName = "Room_Recovery";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        CreatePlatform(geo, "Floor",          new Vector3(  0f, 0f, 0f), 36f, 1f);
        CreatePlatform(geo, "PlatformUpperL", new Vector3(-14f, 5f, 0f),  8f, 1f);
        CreatePlatform(geo, "PlatformUpperR", new Vector3( 14f, 2f, 0f),  8f, 1f);

        // Moving platform markers (blue tint, with collider + layer)
        CreateMovingPlatformMarker(root, "[MovingPlatform]", new Vector3(-4f, 3f, 0f));
        CreateMovingPlatformMarker(root, "[MovingPlatform]", new Vector3( 6f, 3f, 0f));

        CreateMarker(spawns, "EnemySpawn_0", new Vector3(-14f, 6f, 0f));
        CreateMarker(spawns, "EnemySpawn_1", new Vector3( 14f, 3f, 0f));

        CreateMarker(root, "ENT",  new Vector3(-17f, 0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 17f, 0f, 0f));

        SavePrefab(root, roomName);
    }

    private static void BuildRoom_Mixed()
    {
        const string roomName = "Room_Mixed";
        var root = new GameObject(roomName);
        var geo = new GameObject("Geometry");
        geo.transform.SetParent(root.transform);
        var spawns = new GameObject("EnemySpawns");
        spawns.transform.SetParent(root.transform);

        // Ground platforms
        CreatePlatform(geo, "FloorEntry",   new Vector3(-17f,  0f, 0f),  6f, 1f);
        CreatePlatform(geo, "FloorExit",    new Vector3( 17f,  0f, 0f),  6f, 1f);

        // Path A — upper combat
        CreatePlatform(geo, "UpperPath",    new Vector3(  0f,  6f, 0f), 16f, 1f);

        // Path B — lower gap platforms
        CreatePlatform(geo, "LowerPlat_A", new Vector3( -5f,  0f, 0f),  5f, 1f);
        CreatePlatform(geo, "LowerPlat_B", new Vector3(  4f,  0f, 0f),  5f, 1f);
        CreatePlatform(geo, "LowerPlat_C", new Vector3( 12f,  0f, 0f),  5f, 1f);

        // Chain staircase section
        CreatePlatform(geo, "Chain_Step_0", new Vector3(16f,  5f, 0f), 4f, 1f);
        CreatePlatform(geo, "Chain_Step_1", new Vector3(18f,  8f, 0f), 4f, 1f);
        CreatePlatform(geo, "Chain_Step_2", new Vector3(20f, 11f, 0f), 4f, 1f);

        // Kill zone
        CreateKillZone(root, "KillZone", new Vector3(4f, -3f, 0f), 28f, 2f);

        // Enemies
        CreateMarker(spawns, "MixedBossSpawn", new Vector3( 0f,  7f, 0f));
        CreateMarker(spawns, "EnemySpawn_1",   new Vector3(16f,  6f, 0f));
        CreateMarker(spawns, "EnemySpawn_2",   new Vector3(18f,  9f, 0f));
        CreateMarker(spawns, "EnemySpawn_3",   new Vector3(20f, 12f, 0f));

        CreateMarker(root, "ENT",  new Vector3(-19f,  0f, 0f));
        CreateMarker(root, "EXIT", new Vector3( 19f, 12f, 0f));

        SavePrefab(root, roomName);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────────────────

    private static GameObject CreateMarker(GameObject parent, string name, Vector3 localPosition)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPosition;
        return go;
    }

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

    private static void CreateKillZone(GameObject parent, string name, Vector3 pos, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, height);
        col.isTrigger = true;

        go.AddComponent<FallZoneTrigger>();
    }

    private static void CreateMovingPlatformMarker(GameObject parent, string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = pos;
        go.layer = PLATFORM_LAYER;

        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = uiSprite;
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size = new Vector2(4f, 1f);
        sr.color = new Color(0.4f, 0.6f, 1f, 1f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(4f, 1f);
    }

    private static void SavePrefab(GameObject root, string roomName)
    {
        string path = $"Assets/Prefabs/Rooms/{roomName}/{roomName}.prefab";

        // Delete existing prefab for idempotent re-runs
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);

        Debug.Log($"[RoomPrefabBuilder] {roomName} built at {path}");
    }
}
