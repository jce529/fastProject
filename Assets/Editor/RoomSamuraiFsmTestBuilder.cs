using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Fast/Phase19/Build Room_SamuraiFsmTest
/// D-14(Phase 18/18.1 선례대로 DebugScene 확장): SAMURAI 보스 FSM 격리 테스트 전용 룸.
/// RoomBossFsmTestBuilder.cs(Plan 15-05, D-13)와 동일한 컨벤션 — 평평한 바닥 + RoomEntry/ExitSpawnPoint +
/// Door/ENT(Left)+EXIT(Right) RoomConnector + CameraBound + nested boss instance. SamuraiBoss.prefab을
/// 미리 심어둔다(WorldGenerator 풀 스왑 없이도 이 룸 프리팹 자체로 완결). 다른 룸 프리팹은 전혀 건드리지 않는다.
/// </summary>
public static class RoomSamuraiFsmTestBuilder
{
    private const string RoomName      = "Room_SamuraiFsmTest";
    private const string PrefabPath    = "Assets/Prefabs/Rooms/Room_SamuraiFsmTest/Room_SamuraiFsmTest.prefab";
    private const string SolidTilePath = "Assets/Tiles/Tile_Solid.asset"; // RoomCreator.cs가 이미 생성해둔 공유 자산 — 재생성 금지
    private const string BossPrefabPath = "Assets/Prefabs/Enemies/SamuraiBoss.prefab"; // 이 룸 자체에 nested prefab으로 미리 심어둠

    [MenuItem("Fast/Phase19/Build Room_SamuraiFsmTest")]
    public static void Build()
    {
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(SolidTilePath);
        if (tile == null)
        {
            Debug.LogError($"[RoomSamuraiFsmTestBuilder] {SolidTilePath} not found — Fast/RoomCreator/Build All Complex Rooms를 먼저 실행해 공유 타일 자산을 생성하세요.");
            return;
        }

        var root = new GameObject(RoomName);

        var gridGO = new GameObject("Grid");
        gridGO.transform.SetParent(root.transform, false);
        gridGO.AddComponent<Grid>().cellSize = Vector3.one;

        var solidGO = new GameObject("Tilemap_Solid");
        solidGO.transform.SetParent(gridGO.transform, false);
        solidGO.layer = 9; // Platform
        var tilemap = solidGO.AddComponent<Tilemap>();
        solidGO.AddComponent<TilemapRenderer>();
        solidGO.AddComponent<TilemapCollider2D>();

        for (int x = -14; x <= 14; x++) // 평평한 바닥 29타일
            tilemap.SetTile(new Vector3Int(x, 0, 0), tile);

        var entryGO = new GameObject("RoomEntry");
        entryGO.transform.SetParent(root.transform);
        entryGO.transform.localPosition = new Vector3(0f, 1f, 0f);
        entryGO.AddComponent<RoomEntry>(); // 진입 지점 마커
        entryGO.AddComponent<ExitSpawnPoint>(); // WorldGenerator.Start() Vector3.zero 폴백 회피 (RoomBossFsmTestBuilder.cs와 동일 이유)

        // Door/ENT(Left)+Door/EXIT(Right) RoomConnector — 다른 룸과 동일 컨벤션. 바닥 span(x:-14~14) 양 끝, RoomEntry와 동일 y=1.
        var doorGO = new GameObject("Door");
        doorGO.transform.SetParent(root.transform, false);

        var entConnGO = new GameObject("ENT");
        entConnGO.transform.SetParent(doorGO.transform, false);
        entConnGO.transform.localPosition = new Vector3(-14f, 1f, 0f);
        entConnGO.AddComponent<RoomConnector>().direction = RoomConnector.Direction.Left;

        var exitConnGO = new GameObject("EXIT");
        exitConnGO.transform.SetParent(doorGO.transform, false);
        exitConnGO.transform.localPosition = new Vector3(14f, 1f, 0f);
        exitConnGO.AddComponent<RoomConnector>().direction = RoomConnector.Direction.Right;

        // 룸 전체를 아우르는 CameraBound. DebugSceneCameraBinder/DebugRoomTeleporter가
        // CameraFollow.SnapToRoom(Bounds)로 재사용한다.
        var camBoundGO = new GameObject("CameraBound");
        camBoundGO.transform.SetParent(root.transform, false);
        camBoundGO.transform.localPosition = new Vector3(0f, 10f, 0f);
        camBoundGO.AddComponent<CameraBound>();
        var camBoundSO = new SerializedObject(camBoundGO.GetComponent<CameraBound>());
        camBoundSO.FindProperty("_size").vector2Value = new Vector2(30f, 40f);
        camBoundSO.ApplyModifiedProperties();

        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        if (bossPrefab == null)
        {
            Debug.LogError($"[RoomSamuraiFsmTestBuilder] {BossPrefabPath} not found — run 'Fast/Phase19/Build SamuraiBoss Prefab' first.");
            Object.DestroyImmediate(root);
            return;
        }
        var bossInstance = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, root.transform);
        bossInstance.transform.localPosition = new Vector3(6f, 1f, 0f); // RoomEntry(0,1,0) 기준 우측 6유닛 — RoomBossFsmTestBuilder.cs와 동일 컨벤션

        string dir = "Assets/Prefabs/Rooms/Room_SamuraiFsmTest";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Prefabs/Rooms", "Room_SamuraiFsmTest");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath); // 멱등적 재실행 — 기존 자산 덮어쓰기

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RoomSamuraiFsmTestBuilder] Created {PrefabPath} — RoomEntry(0,1,0) + ExitSpawnPoint(0,1,0) + flat floor(x:-14~14, y:0) + Door/ENT(Left)/EXIT(Right) RoomConnectors + nested SamuraiBoss instance(6,1,0).");
    }
}
