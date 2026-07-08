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

    private float _shakeTimeRemaining;
    private float _shakeDurationTotal;
    private float _shakeAmplitude;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    /// <summary>
    /// 처치 순간 카메라를 짧게 흔든다 (D-08). Time.unscaledDeltaTime 기반으로 감쇠하므로
    /// HitFreeze(Time.timeScale=0) 중에도 정상 동작한다.
    /// </summary>
    public void Shake(float duration, float amplitude)
    {
        _shakeDurationTotal = duration;
        _shakeTimeRemaining = duration;
        _shakeAmplitude = amplitude;
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

        if (_shakeTimeRemaining > 0f)
        {
            _shakeTimeRemaining -= Time.unscaledDeltaTime;
            float damper = Mathf.Clamp01(_shakeTimeRemaining / _shakeDurationTotal);
            Vector2 shakeOffset = Random.insideUnitCircle * _shakeAmplitude * damper;
            transform.position += new Vector3(shakeOffset.x, shakeOffset.y, 0f);
        }
    }
}
