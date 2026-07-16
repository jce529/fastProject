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
/// </summary>
public static class RoomBossFsmTestBuilder
{
    private const string RoomName      = "Room_BossFsmTest";
    private const string PrefabPath    = "Assets/Prefabs/Rooms/Room_BossFsmTest/Room_BossFsmTest.prefab";
    private const string SolidTilePath = "Assets/Tiles/Tile_Solid.asset"; // RoomCreator.cs가 이미 생성해둔 공유 자산 — 재생성 금지

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
        entryGO.AddComponent<RoomEntry>(); // D-11: 유일한 마커 — EnemySpawner/RoomConnector/KillZone 등 일절 없음

        string dir = "Assets/Prefabs/Rooms/Room_BossFsmTest";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Prefabs/Rooms", "Room_BossFsmTest");

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath); // 멱등적 재실행 — 기존 자산 덮어쓰기

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[RoomBossFsmTestBuilder] Created {PrefabPath} — RoomEntry(0,1,0) + flat floor(x:-14~14, y:0) only.");
    }
}
