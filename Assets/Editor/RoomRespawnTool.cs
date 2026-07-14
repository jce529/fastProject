using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase999.2/Add Room Respawn Gates
/// D-01/D-02/D-04/D-06(999.2): Complex_Room 6종 프리팹 루트에 RoomClearCondition+RoomRespawnGate를
/// 멱등적으로 부착한다. Corridor/보스 룸은 리스폰 대상이 아니므로(D-04/D-06) 이 배열에 포함하지
/// 않는다. RoomMarkerTool.cs/CorridorEnemySpawnerTool.cs와 동일한
/// LoadPrefabContents/SaveAsPrefabAsset 패턴.
/// </summary>
public static class RoomRespawnTool
{
    private static readonly string[] RoomNames =
    {
        "Room_AllInOne",
        "Room_EdgeRun",
        "Room_GaugeOutpost",
        "Room_LastStand",
        "Room_RiskCrossing",
        "Room_Vertical_Gauntlet",
    };

    [MenuItem("Fast/Phase999.2/Add Room Respawn Gates")]
    public static void AddRoomRespawnGates()
    {
        foreach (var roomName in RoomNames)
        {
            string path = $"Assets/Prefabs/Rooms/Complex_Room/{roomName}/{roomName}.prefab";

            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[RoomRespawnTool] Prefab not found: {path}");
                continue;
            }

            if (root.GetComponent<RoomClearCondition>() == null)
                root.AddComponent<RoomClearCondition>();

            if (root.GetComponent<RoomRespawnGate>() == null)
                root.AddComponent<RoomRespawnGate>();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log($"[RoomRespawnTool] RoomClearCondition+RoomRespawnGate applied to {roomName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RoomRespawnTool] Done. 6 Complex_Room prefabs updated with respawn gates.");
    }
}
