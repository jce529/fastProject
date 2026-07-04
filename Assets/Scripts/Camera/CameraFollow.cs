using UnityEngine;

/// <summary>
/// Direct LateUpdate camera follow -- no Cinemachine, no lead-ahead (per D-11, D-12, D-13).
/// Attach to Main Camera. Assign target = Player Transform in Inspector.
/// CameraBound Bounds 내부로 클램프하며 플레이어를 추적한다.
/// FloorSpawner가 룸 스폰/전환 시 SnapToRoom()을 호출한다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float roomOrthoSize = 7f; // 룸 뷰 orthographicSize — Inspector에서 조정 가능 (기본 7f: 16:9 기준 너비 22 units 수용)

    private bool _hasBounds;
    private Bounds _activeBounds;
    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    /// <summary>
    /// 카메라를 룸 중심(worldCenter)으로 즉시 스냅한다. 이후 플레이어 자유 추적.
    /// FloorSpawner가 CameraBound 없는 룸 스폰/전환 시 호출한다.
    /// </summary>
    public void SnapToRoom(Vector3 worldCenter)
    {
        _hasBounds = false;
        transform.position = new Vector3(worldCenter.x, worldCenter.y, offset.z);
        if (_camera != null) _camera.orthographicSize = roomOrthoSize;
    }

    /// <summary>
    /// 카메라 이동 가능 범위를 worldBounds로 설정한다. orthographicSize는 roomOrthoSize로 고정.
    /// LateUpdate에서 플레이어를 Bounds 내부로 클램프하며 추적한다.
    /// CameraBound.GetWorldBounds() 결과를 전달받는다.
    /// </summary>
    public void SnapToRoom(Bounds worldBounds)
    {
        _hasBounds = true;
        _activeBounds = worldBounds;
        if (_camera != null) _camera.orthographicSize = roomOrthoSize;
    }

    private void LateUpdate()
    {
        if (target == null) return;
        Vector3 desired = target.position + offset;

        if (_hasBounds && _camera != null)
        {
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;

            float x = _activeBounds.size.x <= halfW * 2f
                ? _activeBounds.center.x
                : Mathf.Clamp(desired.x, _activeBounds.min.x + halfW, _activeBounds.max.x - halfW);

            float y = _activeBounds.size.y <= halfH * 2f
                ? _activeBounds.center.y
                : Mathf.Clamp(desired.y, _activeBounds.min.y + halfH, _activeBounds.max.y - halfH);

            transform.position = new Vector3(x, y, offset.z);
        }
        else
        {
            transform.position = desired;
        }
    }
}
