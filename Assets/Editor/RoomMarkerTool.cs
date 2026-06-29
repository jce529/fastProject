using UnityEditor;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase8/Add Room Connectors
/// 4개 Room 프리팹의 ENT/EXIT 자식 오브젝트에 RoomConnector를 멱등적으로 부착한다.
/// </summary>
public static class RoomMarkerTool
{
    private static readonly string[] RoomNames =
    {
        "Room_Combat",
        "Room_Fall",
        "Room_Gap",
        "Room_Stair",
    };

    [MenuItem("Fast/Phase8/Add Room Connectors")]
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

            AddConnector(root, "ENT",  RoomConnector.Direction.Left);
            AddConnector(root, "EXIT", RoomConnector.Direction.Right);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);

            Debug.Log($"[RoomMarkerTool] RoomConnector applied to {roomName}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[RoomMarkerTool] Done. RoomConnectors added to all target rooms.");
    }

    private static void AddConnector(GameObject root, string childName, RoomConnector.Direction dir)
    {
        var child = root.transform.Find(childName);
        if (child == null)
        {
            Debug.LogWarning($"[RoomMarkerTool] Child '{childName}' not found in '{root.name}'");
            return;
        }

        if (child.GetComponent<RoomConnector>() != null)
            return; // Already attached — idempotent

        var rc = child.gameObject.AddComponent<RoomConnector>();
        rc.direction = dir;
    }
}
