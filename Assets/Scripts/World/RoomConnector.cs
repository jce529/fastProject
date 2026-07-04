using UnityEngine;

/// <summary>
/// Room 프리팹의 ENT/EXIT 자식 오브젝트에 부착하는 방향 연결 마커.
/// Phase 9 WorldGenerator가 GetComponentsInChildren&lt;RoomConnector&gt;()로 탐색한다.
/// </summary>
public class RoomConnector : MonoBehaviour
{
    public enum Direction { Left, Right }

    [SerializeField] public Direction direction;

    /// <summary>
    /// Phase 9 WorldGenerator가 채운다. Phase 8에서는 null 허용.
    /// </summary>
    [SerializeField] public GameObject connectedObject;

    private void OnDrawGizmos()
    {
        Gizmos.color = direction == Direction.Left ? Color.blue : Color.green;
        Gizmos.DrawSphere(transform.position, 0.4f);
    }
}
