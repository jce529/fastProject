using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase9/Add Room Connectors
/// 전체 14개 Room 프리팹의 Door/ENT 자식에 RoomConnector(Left)를,
/// Door/EXIT 자식에 RoomConnector(Right)를 멱등적으로 부착한다.
/// Phase 9 WorldGenerator.FindConnector()가 체인 정렬에 사용한다.
/// </summary>
public static class RoomMarkerTool
{
    private static readonly string[] RoomNames =
    {
        "Room_Combat",
        "Room_Hunt",
        "Room_Ladder",
        "Room_LadderDanger",
        "Room_Gap",
        "Room_Fall",
        "Room_Sniper",
        "Room_Stair",
        "Room_Crossroad",
        "Room_Chase",
        "Room_Dodge",
        "Room_Chain",
        "Room_Recovery",
        "Room_Mixed",
    };

    [MenuItem("Fast/Phase9/Add Room Connectors")]
    public static void AddRoomConnectors()
    {
        foreach (var roomName in RoomNames)
        {
            string path = $"Assets/Prefabs/Rooms/{roomName}/{roomName}.prefab";

            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
            {
                Debug.LogError($"[RoomMarkerTool] Prefab not found: {path}");
                continue;
            }

            AddConnector(root, "Door/ENT",  RoomConnector.Direction.Left);
            AddConnector(root, "Door/EXIT", RoomConnector.Direction.Right);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log($"[RoomMarkerTool] ENT(Left)+EXIT(Right) applied to {roomName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RoomMarkerTool] Done. 14 rooms updated with bidirectional RoomConnectors.");
    }

    private static void AddConnector(GameObject root, string childPath, RoomConnector.Direction dir)
    {
        var child = root.transform.Find(childPath); // "/" 경로 지원 (예: "Door/ENT")
        if (child == null)
        {
            Debug.LogWarning($"[RoomMarkerTool] '{childPath}' not found in '{root.name}'");
            return;
        }

        if (child.GetComponent<RoomConnector>() != null)
            return; // Already attached — idempotent

        var rc = child.gameObject.AddComponent<RoomConnector>();
        rc.direction = dir;
    }
}
