using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase14/Add Corridor Enemy Spawners
/// D-03: Corridor 3종(Corridor_Flat/Up/Down)의 기존 "EnemySpawn_0" 마커 GameObject에
/// EnemySpawner(Melee) 컴포넌트를 멱등적으로 부착한다. Room 프리팹과 달리 Corridor는
/// 이 Phase 이전까지 EnemySpawner 마커가 전무했다 (14-RESEARCH.md Open Question 1 — 이번
/// Phase 범위로 확정). RoomMarkerTool.cs와 동일한 LoadPrefabContents/SaveAsPrefabAsset 패턴.
/// </summary>
public static class CorridorEnemySpawnerTool
{
    private static readonly string[] CorridorNames =
    {
        "Corridor_Flat",
        "Corridor_Up",
        "Corridor_Down",
    };

    [MenuItem("Fast/Phase14/Add Corridor Enemy Spawners")]
    public static void AddCorridorEnemySpawners()
    {
        foreach (var corridorName in CorridorNames)
        {
            string path = $"Assets/Prefabs/Corridors/{corridorName}/{corridorName}.prefab";

            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[CorridorEnemySpawnerTool] Prefab not found: {path}");
                continue;
            }

            var marker = root.transform.Find("EnemySpawn_0");
            if (marker == null)
            {
                Debug.LogWarning($"[CorridorEnemySpawnerTool] 'EnemySpawn_0' not found in '{corridorName}'");
                PrefabUtility.UnloadPrefabContents(root);
                continue;
            }

            if (marker.GetComponent<EnemySpawner>() == null)
            {
                marker.gameObject.AddComponent<EnemySpawner>();
                // _type 기본값 EnemyType.Melee 그대로 사용 — Corridor는 마커 1개 = 근접 1마리 기준 (D-04)
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log($"[CorridorEnemySpawnerTool] EnemySpawner(Melee) applied to {corridorName}/EnemySpawn_0");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CorridorEnemySpawnerTool] Done. 3 corridors updated with EnemySpawner markers.");
    }
}
