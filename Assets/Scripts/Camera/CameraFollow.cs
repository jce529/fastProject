using UnityEngine;

/// <summary>
/// Direct LateUpdate camera follow -- no Cinemachine, no lead-ahead (per D-11, D-12, D-13).
/// Attach to Main Camera. Assign target = Player Transform in Inspector.
/// SnapToRoom() 모드: _roomMode=true이면 플레이어 추적을 중단하고 룸 중심에 고정된다.
/// FloorSpawner가 룸 스폰/전환 시 호출한다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float roomOrthoSize = 7f; // 룸 뷰 orthographicSize — Inspector에서 조정 가능 (기본 7f: 16:9 기준 너비 22 units 수용)

    private bool _roomMode;
    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    /// <summary>
    /// 카메라를 룸 중심(worldCenter)으로 고정한다. LateUpdate 플레이어 추적을 중단한다.
    /// FloorSpawner가 룸 스폰/전환 시 호출한다.
    /// </summary>
    public void SnapToRoom(Vector3 worldCenter)
    {
        _roomMode = true;
        transform.position = new Vector3(worldCenter.x, worldCenter.y, offset.z);
        if (_camera != null) _camera.orthographicSize = roomOrthoSize;
    }

    /// <summary>
    /// 카메라를 worldBounds에 맞게 고정한다. orthographicSize를 Bounds에서 자동 계산한다.
    /// CameraBound.GetWorldBounds() 결과를 전달받는다.
    /// </summary>
    public void SnapToRoom(Bounds worldBounds)
    {
        _roomMode = true;
        transform.position = new Vector3(worldBounds.center.x, worldBounds.center.y, offset.z);
        float aspect = (_camera != null) ? _camera.aspect : (16f / 9f);
        float orthoH = worldBounds.size.y / 2f;
        float orthoW = worldBounds.size.x / (2f * aspect);
        if (_camera != null) _camera.orthographicSize = Mathf.Max(orthoH, orthoW);
    }

    private void LateUpdate()
    {
        if (_roomMode) return;          // 룸 고정 중 — 플레이어 추적 건너뜀
        if (target == null) return;
        transform.position = target.position + offset;
    }
}
