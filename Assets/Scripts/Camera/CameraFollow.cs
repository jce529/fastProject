using UnityEngine;

/// <summary>
/// LateUpdate 기반 카메라 추적 -- no Cinemachine(25-RESEARCH.md 결론, 자체 SmoothDamp 합성 채택).
/// SmoothDamp 부드러운 추적 + 에임 리드 오프셋(D-01~D-04) + 텐션 캐치업(D-09~D-10) + 다이나믹 줌(D-05~D-08, Task 1)을 포함한다.
/// Attach to Main Camera. Assign target = Player Transform in Inspector.
/// CameraBound Bounds 내부로 클램프하며 플레이어를 추적한다.
/// FloorSpawner가 룸 스폰/전환 시 SnapToRoom()을 호출한다.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
    [SerializeField] private float roomOrthoSize = 7f; // 룸 뷰 orthographicSize — Inspector에서 조정 가능 (기본 7f: 16:9 기준 너비 22 units 수용)

    [Header("SmoothDamp Tracking (Phase 25)")]
    [SerializeField] private float positionSmoothTime = 0.12f; // 메인 카메라 추적 SmoothDamp

    [Header("Aim Lead Offset (D-01~D-04)")]
    [SerializeField, Range(0.15f, 0.25f)] private float leadIntensity = 0.20f; // D-02: 화면 절반 너비 대비 리드 오프셋 비율 (15~25%)
    [SerializeField] private float leadSmoothTime = 0f; // D-03: 리드 오프셋 전용 SmoothDamp, 기본값 0 = 즉시 반응

    [Header("Tension Catch-up (D-09~D-10)")]
    [SerializeField] private float tensionExponent = 3f;        // D-10: 지수형 가속 곡선
    [SerializeField] private float minCatchUpSmoothTime = 0.03f; // 화면 경계 최대 이탈 시 가장 빠른(작은) SmoothTime

    [Header("Dynamic Zoom (D-05~D-08)")]
    [SerializeField] private float maxZoomOrthoSize = 9f;       // D-06: roomOrthoSize(7) 대비 최대 확장 한계 — 룸 뷰 규격을 크게 벗어나지 않는 범위
    [SerializeField] private float zoomOutSmoothTime = 0.1f;    // D-07: 줌아웃(넓어짐) 빠르게
    [SerializeField] private float zoomInSmoothTime = 0.5f;     // D-07: 줌인(원상 복귀) 느리게
    [SerializeField] private float zoomDistanceReference = 8f;  // D-05: 이 거리(units) 이상이면 거리 팩터가 1.0으로 포화
    [SerializeField] private float zoomSpeedReference = 40f;    // D-05: 이 속도(units/sec) 이상이면 속도 팩터가 1.0으로 포화 — 대시거리(최대 10)/dashDuration(0.15s) 기준 최대 약 66u/s

    private bool _hasBounds;
    private Bounds _activeBounds;
    private Camera _camera;

    private float _shakeTimeRemaining;
    private float _shakeDurationTotal;
    private float _shakeAmplitude;

    private float _zoomTargetSize;
    private float _zoomVelocity;

    private Vector2 _smoothedLead;
    private Vector2 _leadVelocity;
    private bool _leadSuppressed;
    private Vector3 _positionVelocity;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _zoomTargetSize = roomOrthoSize;
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

    /// <summary>D-05: 대시 거리+속도 결합으로 줌아웃 목표치를 계산해 요청한다.
    /// OverclockModule.Resolve()가 대시 이동(MovePosition) 완료 직후 호출한다 (25-02).
    /// Rigidbody2D.linearVelocity는 대시 중 0으로 고정되므로 사용하지 않는다 (Pitfall 1) — distance/DashDuration으로 미리 계산된 speed를 그대로 받는다.</summary>
    public void RequestDynamicZoom(float distance, float speed)
    {
        float distanceFactor = Mathf.Clamp01(distance / zoomDistanceReference);
        float speedFactor = Mathf.Clamp01(speed / zoomSpeedReference);
        float t = (distanceFactor + speedFactor) * 0.5f;
        _zoomTargetSize = Mathf.Lerp(roomOrthoSize, maxZoomOrthoSize, t);
    }

    /// <summary>D-08: 히트프리즈(OverclockModule.HitFreeze) 종료 직후 호출 — 줌 목표치를 룸 기본값으로 되돌려 줌인을 시작시킨다.</summary>
    public void ReleaseDynamicZoom()
    {
        _zoomTargetSize = roomOrthoSize;
    }

    /// <summary>D-04: 대시 시작/종료 시 OverclockModule.Resolve()가 호출해 리드 오프셋을 켜고 끈다 (25-02에서 배선).</summary>
    public void SetAimLeadSuppressed(bool suppressed)
    {
        _leadSuppressed = suppressed;
    }

    /// <summary>D-01/D-02: 마우스 방향 계산 — OverclockModule.GetMouseWorldDirection()(§134-141)과 동일 패턴,
    /// CombatContext 의존 없이 자체 Mouse.current + 캐시된 _camera만 사용 (항상 활성화된 리드 오프셋을 위해 전투 상태와 무관하게 동작).</summary>
    private Vector2 GetMouseWorldDirection()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null || _camera == null || target == null) return Vector2.zero;
        Vector2 mouseScreen = mouse.position.ReadValue();
        Vector3 mouseWorld = _camera.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, Mathf.Abs(_camera.transform.position.z)));
        Vector2 dir = (Vector2)mouseWorld - (Vector2)target.position;
        return dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.zero;
    }

    /// <summary>D-02: 리드 오프셋 최대 크기 — 화면 절반 너비(orthoSize*aspect)의 leadIntensity(15~25%) 비율.</summary>
    private float ComputeLeadMagnitude()
    {
        if (_camera == null) return 0f;
        return _camera.orthographicSize * _camera.aspect * leadIntensity;
    }

    /// <summary>D-09/D-10: target이 화면 좌우 바깥쪽 25% 구간(viewport x &lt; 0.25 또는 &gt; 0.75)에 진입할 때만
    /// 개입하는 지수형 캐치업 — 개입폭(overshoot)에 비례해 positionSmoothTime을 minCatchUpSmoothTime까지 줄인다.</summary>
    private float ComputeTensionAdjustedSmoothTime()
    {
        if (_camera == null || target == null) return positionSmoothTime;
        Vector3 vp = _camera.WorldToViewportPoint(target.position);
        float overshoot = 0f;
        if (vp.x < 0.25f) overshoot = (0.25f - vp.x) / 0.25f;
        else if (vp.x > 0.75f) overshoot = (vp.x - 0.75f) / 0.25f;
        overshoot = Mathf.Clamp01(overshoot);
        if (overshoot <= 0f) return positionSmoothTime;

        float catchUpStrength = Mathf.Pow(overshoot, tensionExponent);
        return Mathf.Lerp(positionSmoothTime, minCatchUpSmoothTime, catchUpStrength);
    }

    /// <summary>
    /// 카메라를 룸 중심(worldCenter)으로 즉시 스냅한다. 이후 플레이어 자유 추적.
    /// FloorSpawner가 CameraBound 없는 룸 스폰/전환 시 호출한다.
    /// </summary>
    public void SnapToRoom(Vector3 worldCenter)
    {
        _hasBounds = false;
        transform.position = new Vector3(worldCenter.x, worldCenter.y, offset.z);
        _zoomTargetSize = roomOrthoSize; // 룸 전환 시 줌 목표 리셋 — 룸-to-룸 텔레포트 중 줌 크리프 방지
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
        _zoomTargetSize = roomOrthoSize; // 룸 전환 시 줌 목표 리셋 — 룸-to-룸 텔레포트 중 줌 크리프 방지
        if (_camera != null) _camera.orthographicSize = roomOrthoSize;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // D-05~D-08: 줌 SmoothDamp — bounds clamp가 orthographicSize를 읽으므로 반드시 먼저 실행 (Pitfall 2)
        if (_camera != null)
        {
            float zoomSmoothTime = (_zoomTargetSize > _camera.orthographicSize) ? zoomOutSmoothTime : zoomInSmoothTime;
            _camera.orthographicSize = Mathf.SmoothDamp(_camera.orthographicSize, _zoomTargetSize, ref _zoomVelocity, zoomSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        Vector2 leadTarget = _leadSuppressed ? Vector2.zero : GetMouseWorldDirection() * ComputeLeadMagnitude();
        _smoothedLead = Vector2.SmoothDamp(_smoothedLead, leadTarget, ref _leadVelocity, leadSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        Vector3 desired = target.position + offset + (Vector3)_smoothedLead;
        float effectiveSmoothTime = ComputeTensionAdjustedSmoothTime();
        transform.position = Vector3.SmoothDamp(transform.position, desired, ref _positionVelocity, effectiveSmoothTime, Mathf.Infinity, Time.unscaledDeltaTime);

        if (_hasBounds && _camera != null)
        {
            float halfH = _camera.orthographicSize;
            float halfW = halfH * _camera.aspect;

            float x = _activeBounds.size.x <= halfW * 2f
                ? _activeBounds.center.x
                : Mathf.Clamp(transform.position.x, _activeBounds.min.x + halfW, _activeBounds.max.x - halfW);

            float y = _activeBounds.size.y <= halfH * 2f
                ? _activeBounds.center.y
                : Mathf.Clamp(transform.position.y, _activeBounds.min.y + halfH, _activeBounds.max.y - halfH);

            transform.position = new Vector3(x, y, offset.z);
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
