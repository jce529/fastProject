using UnityEngine;

/// <summary>
/// Room 프리팹 자식 오브젝트에 부착해 카메라 뷰 영역을 정의한다.
/// CameraFollow.SnapToRoom(Bounds) 가 이 값을 읽어 orthographicSize를 자동 계산한다.
/// BoxCollider2D 불필요 — 물리 개입 없음.
/// </summary>
public class CameraBound : MonoBehaviour
{
    [SerializeField] private Vector2 _size = new Vector2(20f, 12f);

    /// <summary>
    /// 월드 좌표 기준 카메라 뷰 Bounds를 반환한다.
    /// center = transform.position, size = _size.
    /// </summary>
    public Bounds GetWorldBounds()
    {
        return new Bounds(transform.position, new Vector3(_size.x, _size.y, 0f));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(_size.x, _size.y, 0f));
    }
}
