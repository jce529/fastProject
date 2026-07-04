using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Fast/RoomCreator
/// 복합룸설계.md 기반 복합 Room 프리팹 6종을 생성한다.
/// END_Left·END_Right(RoomConnector), ENTRY_Bottom(RoomEntry), 적 스폰 마커 포함.
/// </summary>
public static class RoomCreator
{
    [MenuItem("Fast/RoomCreator/Build All Complex Rooms")]
    public static void BuildAll()
    {
        var tile = EnsureSolidTile();
        Build_VerticalGauntlet(tile);
        Build_RiskCrossing(tile);
        Build_EdgeRun(tile);
        Build_LastStand(tile);
        Build_GaugeOutpost(tile);
        Build_AllInOne(tile);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RoomCreator] 복합 Room 6종 생성 완료.");
    }

    [MenuItem("Fast/RoomCreator/Room_Vertical_Gauntlet")]
    static void Menu_VG() => Run(Build_VerticalGauntlet);

    [MenuItem("Fast/RoomCreator/Room_RiskCrossing")]
    static void Menu_RC() => Run(Build_RiskCrossing);

    [MenuItem("Fast/RoomCreator/Room_EdgeRun")]
    static void Menu_ER() => Run(Build_EdgeRun);

    [MenuItem("Fast/RoomCreator/Room_LastStand")]
    static void Menu_LS() => Run(Build_LastStand);

    [MenuItem("Fast/RoomCreator/Room_GaugeOutpost")]
    static void Menu_GO() => Run(Build_GaugeOutpost);

    [MenuItem("Fast/RoomCreator/Room_AllInOne")]
    static void Menu_AIO() => Run(Build_AllInOne);

    static void Run(System.Action<Tile> builder)
    {
        builder(EnsureSolidTile());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    // ── Room Builders ─────────────────────────────────────────────────────────

    static void Build_VerticalGauntlet(Tile tile)
    {
        // Combat(하단 평지) + LadderDanger(중앙 수직 통로) + Chain(우상단 연속처치)
        // END_Left: 하단 좌측 / END_Right: 체인 정상 우측 (수직 이동 강제)
        const string N = "Room_Vertical_Gauntlet";
        var (root, tm) = NewRoom(N);
        var spawns = AddChild(root, "EnemySpawns");

        PRow(tm, tile, -16, 15, 0);        // 전체 바닥

        PCol(tm, tile, -2, 1, 12);         // 사다리 통로 왼쪽 벽
        PCol(tm, tile,  2, 1, 12);         // 사다리 통로 오른쪽 벽

        PRow(tm, tile, -13, -6, 6);        // 공중 플랫폼 (원거리 적)

        PRow(tm, tile,  3,  8, 3);         // 체인 1단
        PRow(tm, tile,  6, 11, 6);         // 체인 2단
        PRow(tm, tile,  9, 14, 9);         // 체인 3단

        AddLeft(root,  "END_Left",  new Vector3(-16, 1, 0));
        AddRight(root, "END_Right", new Vector3( 15, 10, 0));
        // 사다리 통로 내부 (벽 x=-2, x=2 사이 — 플레이어가 오를 수 있는 트리거 영역)
        AddLadderTilemap(root, tm, tile, 0, 1, 12);

        SpawnMarker(spawns, "EnemySpawn_Melee_0",  new Vector3(-10, 1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_1",  new Vector3( -6, 1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_2",  new Vector3(  0, 1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_3",  new Vector3(  3, 1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Ranged_0", new Vector3(-12, 7, 0), EnemySpawner.EnemyType.Ranged);
        SpawnMarker(spawns, "EnemySpawn_Ranged_1", new Vector3( -8, 7, 0), EnemySpawner.EnemyType.Ranged);
        SpawnMarker(spawns, "EnemySpawn_Chain_0",  new Vector3(  5, 4, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Chain_1",  new Vector3(  8, 7, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Chain_2",  new Vector3( 11,10, 0), EnemySpawner.EnemyType.Melee);

        Save(root, N);
    }

    static void Build_RiskCrossing(Tile tile)
    {
        // Crossroad + Sniper(상단 경로 A) + Hunt(하단 경로 B) — Entry-eligible
        // ENTRY_Bottom 중앙 스폰 → 상단(저격 압박) / 하단(다층 헌트) 양방향 선택
        const string N = "Room_RiskCrossing";
        var (root, tm) = NewRoom(N);
        var spawns = AddChild(root, "EnemySpawns");

        PRow(tm, tile, -16, -4, 0);        // 좌측 메인 바닥
        PRow(tm, tile,  -2,  2, 0);        // 중앙 스폰 발판
        PRow(tm, tile,   4, 16, 0);        // 우측 메인 바닥

        PRow(tm, tile, -14, 14, 7);        // 상단 경로 A (저격수)

        PRow(tm, tile, -13, -5, -3);       // 하단 경로 B 좌측 (헌트)
        PRow(tm, tile,   5, 13, -3);       // 하단 경로 B 우측 (헌트)

        KillZone(root, "KillZone", new Vector3(0, -5.5f, 0), 32, 2);

        AddLeft(root,  "END_Left",     new Vector3(-16, 1, 0));
        AddRight(root, "END_Right",    new Vector3( 16, 1, 0));
        AddEntry(root, "ENTRY_Bottom", new Vector3(  0, 1, 0));

        SpawnMarker(spawns, "EnemySpawn_Ranged_0", new Vector3( -8,  8, 0), EnemySpawner.EnemyType.Ranged);
        SpawnMarker(spawns, "EnemySpawn_Ranged_1", new Vector3(  0,  8, 0), EnemySpawner.EnemyType.Ranged);
        SpawnMarker(spawns, "EnemySpawn_Ranged_2", new Vector3(  8,  8, 0), EnemySpawner.EnemyType.Ranged);
        SpawnMarker(spawns, "EnemySpawn_Melee_0",  new Vector3(-11, -2, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_1",  new Vector3( -7, -2, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_2",  new Vector3(  6, -2, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_3",  new Vector3( 10, -2, 0), EnemySpawner.EnemyType.Melee);

        Save(root, N);
    }

    static void Build_EdgeRun(Tile tile)
    {
        // Gap + Fall + Stair + Combat — 스피드런 단방향
        // 계단 가속 → 갭 점프 → 전투(속도 유지 필요) → END_Right
        const string N = "Room_EdgeRun";
        var (root, tm) = NewRoom(N);
        var spawns = AddChild(root, "EnemySpawns");

        PRow(tm, tile, -16, -12, 0);       // 계단 0단 (시작)
        PRow(tm, tile, -10,  -6, 2);       // 계단 1단
        PRow(tm, tile,  -4,   0, 4);       // 계단 2단
        PRow(tm, tile,   2,   6, 6);       // 계단 3단 (정상)

        PRow(tm, tile,  8, 10, 5);         // 갭 플랫폼 A
        PRow(tm, tile, 12, 16, 4);         // 갭 플랫폼 B + 전투 구역 (연결)

        KillZone(root, "KillZone", new Vector3(0, -1.5f, 0), 28, 2);

        AddLeft(root,  "END_Left",  new Vector3(-16, 1, 0));
        AddRight(root, "END_Right", new Vector3( 16, 5, 0));

        SpawnMarker(spawns, "EnemySpawn_Melee_0", new Vector3(13, 5, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_1", new Vector3(14, 5, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_2", new Vector3(15, 5, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_3", new Vector3(16, 5, 0), EnemySpawner.EnemyType.Melee);

        Save(root, N);
    }

    static void Build_LastStand(Tile tile)
    {
        // Dodge + Chase + Combat — 밀폐 터널
        // 좌: 추격 보스(처치불가), 중: 근접 4마리, 우: 파동 공격 보스
        const string N = "Room_LastStand";
        var (root, tm) = NewRoom(N);
        var spawns = AddChild(root, "EnemySpawns");

        PRow(tm, tile, -18, 18, 0);        // 바닥
        PRow(tm, tile, -18, 18, 5);        // 천장 (밀폐 압박)

        PRow(tm, tile, 13, 18, 2);         // 우측 보스 플랫폼 (약간 높음)

        AddLeft(root,  "END_Left",  new Vector3(-18, 1, 0));
        AddRight(root, "END_Right", new Vector3( 18, 1, 0));

        SpawnMarker(spawns, "BossChaseSpawn",     new Vector3(-16,  1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_0", new Vector3( -6,  1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_1", new Vector3( -2,  1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_2", new Vector3(  2,  1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_3", new Vector3(  6,  1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "BossWaveSpawn",      new Vector3( 15,  3, 0), EnemySpawner.EnemyType.Ranged);

        Save(root, N);
    }

    static void Build_GaugeOutpost(Tile tile)
    {
        // Recovery + Combat(저강도) — Entry-eligible, 층 시작 1순위 후보
        // 안전지대 + 근접 3마리 + 이동 발판 (게이지 관리 검증)
        const string N = "Room_GaugeOutpost";
        var (root, tm) = NewRoom(N);
        var spawns = AddChild(root, "EnemySpawns");

        PRow(tm, tile, -15, 15, 0);        // 메인 바닥

        PRow(tm, tile, -14, -8, 5);        // 상단 좌측 플랫폼
        PRow(tm, tile,   8, 14, 3);        // 상단 우측 플랫폼

        MovingPlatMarker(root, "[MovingPlatform]", new Vector3(-4, 3, 0));
        MovingPlatMarker(root, "[MovingPlatform]", new Vector3( 4, 3, 0));

        AddLeft(root,  "END_Left",     new Vector3(-15, 1, 0));
        AddRight(root, "END_Right",    new Vector3( 15, 1, 0));
        AddEntry(root, "ENTRY_Bottom", new Vector3(  0, 1, 0));

        SpawnMarker(spawns, "EnemySpawn_Melee_0", new Vector3(-3, 1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_1", new Vector3( 0, 1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_2", new Vector3( 3, 1, 0), EnemySpawner.EnemyType.Melee);

        Save(root, N);
    }

    static void Build_AllInOne(Tile tile)
    {
        // Mixed 확장판 — 전 메카닉 통합, 최고난이도, ENTRY_Bottom 없음
        // 경로A(상단 회피) / 경로B(하단 갭 점프) / 사다리 위험 / 체인
        const string N = "Room_AllInOne";
        var (root, tm) = NewRoom(N);
        var spawns = AddChild(root, "EnemySpawns");

        PRow(tm, tile, -20, -10, 0);       // 진입 바닥 (좌)
        PRow(tm, tile,  10,  20, 0);       // 탈출 바닥 (우)

        PRow(tm, tile, -8, 8, 7);          // 경로 A — 상단 전투 (거대몹)

        PRow(tm, tile, -8, -4, 0);         // 경로 B — 갭 플랫폼 1
        PRow(tm, tile,  0,  4, 0);         // 경로 B — 갭 플랫폼 2
        PRow(tm, tile,  5, 10, 0);         // 경로 B — 갭 플랫폼 3

        PCol(tm, tile, -10, 1, 12);        // 사다리 위험 통로 외벽
        PRow(tm, tile, -14, -10, 8);       // 원거리 적 고지대 플랫폼

        PRow(tm, tile, 12, 16,  4);        // 체인 1단
        PRow(tm, tile, 14, 18,  7);        // 체인 2단
        PRow(tm, tile, 16, 20, 10);        // 체인 3단

        KillZone(root, "KillZone", new Vector3(1, -2f, 0), 20, 2);

        AddLeft(root,  "END_Left",  new Vector3(-20,  1, 0));
        AddRight(root, "END_Right", new Vector3( 20, 11, 0));
        // 사다리 통로 내부 (외벽 x=-10 안쪽, 플레이어가 오를 수 있는 트리거 영역)
        AddLadderTilemap(root, tm, tile, -9, 1, 12);

        SpawnMarker(spawns, "EnemySpawn_LargeBoss", new Vector3(  0,  8, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Ranged",    new Vector3(-12,  9, 0), EnemySpawner.EnemyType.Ranged);
        SpawnMarker(spawns, "EnemySpawn_Chain_0",   new Vector3( 13,  5, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Chain_1",   new Vector3( 15,  8, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Chain_2",   new Vector3( 17, 11, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_0",   new Vector3( -6,  1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_1",   new Vector3(  2,  1, 0), EnemySpawner.EnemyType.Melee);
        SpawnMarker(spawns, "EnemySpawn_Melee_2",   new Vector3(  7,  1, 0), EnemySpawner.EnemyType.Melee);

        Save(root, N);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static (GameObject root, Tilemap tilemap) NewRoom(string roomName)
    {
        var root   = new GameObject(roomName);
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

    static GameObject AddChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    static GameObject Marker(GameObject parent, string name, Vector3 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        go.transform.localPosition = localPos;
        return go;
    }

    static void SpawnMarker(GameObject parent, string name, Vector3 localPos,
                            EnemySpawner.EnemyType type)
    {
        var go = Marker(parent, name, localPos);
        var spawner = go.AddComponent<EnemySpawner>();
        var so = new SerializedObject(spawner);
        so.FindProperty("_type").enumValueIndex = (int)type;
        so.ApplyModifiedProperties();
    }

    static void AddLeft(GameObject root, string name, Vector3 localPos)
    {
        var go = Marker(root, name, localPos);
        go.AddComponent<RoomConnector>().direction = RoomConnector.Direction.Left;
    }

    static void AddRight(GameObject root, string name, Vector3 localPos)
    {
        var go = Marker(root, name, localPos);
        go.AddComponent<RoomConnector>().direction = RoomConnector.Direction.Right;
    }

    static void AddEntry(GameObject root, string name, Vector3 localPos)
    {
        var go = Marker(root, name, localPos);
        go.AddComponent<RoomEntry>();
    }

    /// <summary>
    /// Grid 하위에 Tilemap_Ladder(단일 x열)를 추가한다.
    /// Tag="Ladder" + TilemapCollider2D(isTrigger=true) → LadderController가 감지.
    /// 하단 (y0-1): solidTm에 solidTile 추가 — 하단 바닥으로 막음.
    /// 상단 (y1): Tilemap_LadderTop 생성 + PlatformEffector2D — 위에서 서고 아래서 통과.
    /// </summary>
    static void AddLadderTilemap(GameObject root, Tilemap solidTm, Tile solidTile, int x, int y0, int y1)
    {
        var ladderTile = EnsureLadderTile();
        var grid = root.GetComponentInChildren<Grid>();

        // Ladder trigger tilemap (단일 x열)
        var go = new GameObject("Tilemap_Ladder");
        go.transform.SetParent(grid.transform, false);
        go.tag = "Ladder";
        var ladderTm = go.AddComponent<Tilemap>();
        go.AddComponent<TilemapRenderer>();
        var col = go.AddComponent<TilemapCollider2D>();
        col.isTrigger = true;

        for (int y = y0; y <= y1; y++)
            ladderTm.SetTile(new Vector3Int(x, y, 0), ladderTile);

        // 하단 바닥 — solidTm에 y0-1 위치 타일 추가 (내려갈 때 막음)
        solidTm.SetTile(new Vector3Int(x, y0 - 1, 0), solidTile);

        // 상단 OneWay 플랫폼 — 위에서 서고 아래서 통과
        var topGO = new GameObject("Tilemap_LadderTop");
        topGO.transform.SetParent(grid.transform, false);
        topGO.layer = 9; // Platform
        var topTm = topGO.AddComponent<Tilemap>();
        topGO.AddComponent<TilemapRenderer>();
        var topCol = topGO.AddComponent<TilemapCollider2D>();
        topCol.usedByEffector = true;
        var eff = topGO.AddComponent<PlatformEffector2D>();
        eff.useOneWay = true;
        eff.surfaceArc = 170f;

        topTm.SetTile(new Vector3Int(x, y1, 0), solidTile);
    }

    static void KillZone(GameObject root, string name, Vector3 localPos, float w, float h)
    {
        var go  = Marker(root, name, localPos);
        var col = go.AddComponent<BoxCollider2D>();
        col.size      = new Vector2(w, h);
        col.isTrigger = true;
        go.AddComponent<FallZoneTrigger>();
    }

    static void MovingPlatMarker(GameObject root, string name, Vector3 localPos)
    {
        var go = Marker(root, name, localPos);
        go.layer = 9;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite   = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.size     = new Vector2(4f, 1f);
        sr.color    = new Color(0.4f, 0.6f, 1f, 1f);
        go.AddComponent<BoxCollider2D>().size = new Vector2(4f, 1f);
    }

    static void PRow(Tilemap tm, Tile tile, int x0, int x1, int y)
    {
        for (int x = x0; x <= x1; x++)
            tm.SetTile(new Vector3Int(x, y, 0), tile);
    }

    static void PCol(Tilemap tm, Tile tile, int x, int y0, int y1)
    {
        for (int y = y0; y <= y1; y++)
            tm.SetTile(new Vector3Int(x, y, 0), tile);
    }

    static Tile EnsureSolidTile()
    {
        const string path = "Assets/Tiles/Tile_Solid.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;

        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        tile = ScriptableObject.CreateInstance<Tile>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/TileSquare.png");
        if (sprite != null)
            tile.sprite = sprite;
        tile.color = Color.white;
        AssetDatabase.CreateAsset(tile, path);
        AssetDatabase.SaveAssets();

        if (sprite == null)
            Debug.LogWarning("[RoomCreator] TileSquare.png 없음 — " +
                             "Fast/Phase8/Convert Rooms to Tilemap 먼저 실행하면 스프라이트가 할당됩니다.");
        return tile;
    }

    static Tile EnsureLadderTile()
    {
        const string path = "Assets/Tiles/Tile_Ladder.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile != null) return tile;

        if (!AssetDatabase.IsValidFolder("Assets/Tiles"))
            AssetDatabase.CreateFolder("Assets", "Tiles");

        tile = ScriptableObject.CreateInstance<Tile>();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiles/TileSquare.png");
        if (sprite != null)
            tile.sprite = sprite;
        tile.color = new Color(0.76f, 0.60f, 0.42f); // 갈색
        AssetDatabase.CreateAsset(tile, path);
        AssetDatabase.SaveAssets();
        return tile;
    }

    static void Save(GameObject root, string roomName)
    {
        string dir  = $"Assets/Prefabs/Rooms/{roomName}";
        string path = $"{dir}/{roomName}.prefab";

        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Prefabs/Rooms", roomName);

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            AssetDatabase.DeleteAsset(path);

        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[RoomCreator] {roomName} → {path}");
    }
}
