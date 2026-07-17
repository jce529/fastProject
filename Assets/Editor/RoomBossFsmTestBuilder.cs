using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Menu: Fast/Phase15/Build Room_BossFsmTest
/// D-11 (RE-RESOLVED 2026-07-15): Room_Debug.prefab은 Phase 16 레거시 정리에서 삭제될 예정이므로,
/// 보스 FSM 격리 테스트 전용 목적지 룸을 Room_Debug와 완전히 독립된 신규 프리팹으로 생성한다.
/// RoomEntry 마커 하나 + 평평한 바닥(Tilemap)만 포함 — 기존 적 스폰 마커/기믹은 전혀 없음(D-11 명시).
/// Assets/Editor/RoomCreator.cs의 NewRoom/PRow/AddEntry/EnsureSolidTile 컨벤션을 그대로 따른다
/// (RoomCreator의 헬퍼는 private static이라 직접 호출 불가 — 동일 패턴을 이 파일 안에 인라인으로 재작성).
/// D-13 (2026-07-17): Door/ENT/EXIT RoomConnector + ExitSpawnPoint + nested BossEnemy 인스턴스 추가 —
/// WorldGenerator _roomPrefabs 풀 스왑(BossFsmTestPoolSwapTool.cs)을 통해 Play만 눌러도 보스전이
/// 시작되도록 지원한다. ExitSpawnPoint는 gsd-plan-checker 이슈 2 수정(Vector3.zero 폴백 회피)이다.
/// </summary>
public static class RoomBossFsmTestBuilder
{
    private const string RoomName      = "Room_BossFsmTest";
    private const string PrefabPath    = "Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab";
    private const string SolidTilePath = "Assets/Tiles/Tile_Solid.asset"; // RoomCreator.cs가 이미 생성해둔 공유 자산 — 재생성 금지
    private const string BossPrefabPath = "Assets/Prefabs/Enemies/BossEnemy.prefab"; // D-13: 이 룸 자체에 nested prefab으로 미리 심어둠

    [MenuItem("Fast/Phase15/Build Room_BossFsmTest")]
    public static void Build()
    {
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(SolidTilePath);
        if (tile == null)
        {
            Debug.LogError($"[RoomBossFsmTestBuilder] {SolidTilePath} not found — Fast/RoomCreator/Build All Complex Rooms를 먼저 실행해 공유 타일 자산을 생성하세요.");
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

        for (int x = -14; x <= 14; x++) // 평평한 바닥 29타일 (D-11: 바닥만, 벽/장애물 없음)
            tilemap.SetTile(new Vector3Int(x, 0, 0), tile);

        var entryGO = new GameObject("RoomEntry");
        entryGO.transform.SetParent(root.transform);
        entryGO.transform.localPosition = new Vector3(0f, 1f, 0f);
        entryGO.AddComponent<RoomEntry>(); // 진입 지점 마커
        // gsd-plan-checker 이슈 2 (revision, 2026-07-17): WorldGenerator.Start()는 시작 룸에 ExitSpawnPoint가
        // 하나도 없으면 플레이어를 Vector3.zero(바닥 높이, RoomEntry(0,1,0)보다 낮음)로 텔레포트한다.
        // Room_BossFsmTest는 이 폴백 경로를 타지 않도록 RoomEntry와 동일 GameObject/위치에 ExitSpawnPoint를
        // 함께 심는다 (필드 없는 순수 마커라 컴포넌트만 추가하면 충분 — 포탈 스폰 후보로도 쓰이지만
        // _exitSpawnChance가 BossFsmTestPoolSwapTool.cs에 의해 0으로 강제되는 한 포탈은 생기지 않는다,
        // iteration 2 재확인 — 이 룸 자체는 그대로 두고 Task 2가 스왑 시점에 확률을 0으로 막는다).
        entryGO.AddComponent<ExitSpawnPoint>();

        // D-13 (2026-07-17): Door/ENT(Left)+Door/EXIT(Right) RoomConnector — 다른 13개 룸과 동일 컨벤션
        // (Assets/Editor/RoomMarkerTool.cs 참고). WorldGenerator.FindConnector()/AlignByEntry()/AlignByExit()가
        // 이 마커로 체인 정렬한다. 바닥 span(x:-14~14) 양 끝, RoomEntry와 동일 y=1.
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

        // D-13: WorldGenerator._roomPrefabs가 이 룸 하나로 스왑됐을 때 Play만 눌러도 보스가 실제로 등장하도록,
        // BossEnemy.prefab을 nested prefab instance로 미리 심어둔다 — 기존 DebugRoomTeleporter._bossPrefab의
        // "텔레포트 시점 즉시 Instantiate" 동작을 prefab-build 시점으로 이전한 것. WorldGenerator/EnemySpawner의
        // Melee/Ranged 전용 EnemyType 계약은 전혀 건드리지 않는다(D-13 취지, 정밀한 변경).
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        if (bossPrefab == null)
        {
            Debug.LogError($"[RoomBossFsmTestBuilder] {BossPrefabPath} not found — run 'Fast/Phase15/Build BossEnemy Prefab' first.");
            Object.DestroyImmediate(root);
            return;
        }
        var bossInstance = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab, root.transform);
        bossInstance.transform.localPosition = new Vector3(6f, 1f, 0f); // RoomEntry(0,1,0) 기준 우측 6유닛 — 기존 DebugRoomTeleporter의 Vector3.right*6f 오프셋과 동일 컨벤션

        string dir = "Assets/Prefabs/Rooms/Room_BossFsmTest";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Prefabs/Rooms", "Room_BossFsmTest");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath); // 멱등적 재실행 — 기존 자산 덮어쓰기

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RoomBossFsmTestBuilder] Created {PrefabPath} — RoomEntry(0,1,0) + ExitSpawnPoint(0,1,0) + flat floor(x:-14~14, y:0) + Door/ENT(Left)/EXIT(Right) RoomConnectors + nested BossEnemy instance(6,1,0).");
    }
}
