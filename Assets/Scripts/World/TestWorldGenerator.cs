using UnityEngine;

/// <summary>
/// 08-03 플레이테스트 전용 간이 WorldGenerator.
/// Inspector에서 Room/Corridor 프리팹 배열을 지정하면 Start()에서
/// _roomPrefabs[0] → _corridorPrefabs[0] → _roomPrefabs[last] 수평 체인을 자동 배치한다.
///
/// IMPORTANT: 이 컴포넌트를 씬에 배치할 때는 FloorSpawner 컴포넌트(또는 해당 GameObject)를
/// Inspector에서 비활성화(disable)해야 한다. 두 시스템이 동시에 동작하면 충돌한다.
/// </summary>
public class TestWorldGenerator : MonoBehaviour
{
    [SerializeField] private GameObject[] _roomPrefabs;
    [SerializeField] private GameObject[] _corridorPrefabs;

    private void Start()
    {
        if (_roomPrefabs == null || _roomPrefabs.Length == 0)
        {
            Debug.LogError("TestWorldGenerator: _roomPrefabs is empty");
            return;
        }
        if (_corridorPrefabs == null || _corridorPrefabs.Length == 0)
        {
            Debug.LogError("TestWorldGenerator: _corridorPrefabs is empty");
            return;
        }

        // Room1 스폰 (원점)
        GameObject room1 = Instantiate(_roomPrefabs[0], Vector3.zero, Quaternion.identity);
        RoomConnector room1Exit = FindConnector(room1, RoomConnector.Direction.Right);
        if (room1Exit == null)
            Debug.LogWarning("Room1 has no Right RoomConnector");
        Vector3 prevExitPos = room1Exit != null ? room1Exit.transform.position : Vector3.zero;

        // Corridor 스폰 및 정렬
        GameObject corridor = Instantiate(_corridorPrefabs[0], Vector3.zero, Quaternion.identity);
        AlignByEntry(corridor, prevExitPos);
        RoomConnector corridorExit = FindConnector(corridor, RoomConnector.Direction.Right);
        if (corridorExit != null)
            prevExitPos = corridorExit.transform.position;

        // Room2 스폰 및 정렬
        GameObject room2Prefab = _roomPrefabs.Length > 1
            ? _roomPrefabs[_roomPrefabs.Length - 1]
            : _roomPrefabs[0];
        GameObject room2 = Instantiate(room2Prefab, Vector3.zero, Quaternion.identity);
        AlignByEntry(room2, prevExitPos);
    }

    private RoomConnector FindConnector(GameObject go, RoomConnector.Direction direction)
    {
        foreach (RoomConnector rc in go.GetComponentsInChildren<RoomConnector>(true))
        {
            if (rc.direction == direction) return rc;
        }
        return null;
    }

    private void AlignByEntry(GameObject go, Vector3 targetWorldPos)
    {
        RoomConnector entry = FindConnector(go, RoomConnector.Direction.Left);
        if (entry == null)
        {
            Debug.LogWarning($"TestWorldGenerator: {go.name} has no Left RoomConnector — placed at {targetWorldPos}");
            go.transform.position = targetWorldPos;
            return;
        }
        // entry.transform.position은 원점 기준(Instantiate 직후) = 루트에서의 로컬 오프셋
        go.transform.position = targetWorldPos - entry.transform.position;
    }
}
