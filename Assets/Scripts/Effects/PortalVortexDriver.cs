using UnityEngine;

/// <summary>
/// PortalVortex.shader의 _UnscaledTime을 매 프레임 갱신한다 -- 슬로우모션 면역을 위해
/// Shader Graph Time 노드 대신 Time.unscaledTime을 수동 주입하는 프로젝트 컨벤션.
/// 999.3-01 Task 3 스파이크 검증용 임시 스크립트. Renderer.material 대신
/// MaterialPropertyBlock을 사용해 공유 머티리얼 인스턴스화 없이 값만 밀어넣는다.
/// </summary>
public class PortalVortexDriver : MonoBehaviour
{
    private static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");

    // 활성화 후 이 시간(초)까지만 각도가 감기고, 이후로는 고정된다 -- 수동 육안 검증이
    // 몇 초 늦게 확인해도 항상 같은 모양으로 보이게 하고, 실제 사용처(999.3-02의
    // ~0.3-0.4초 짧은 버스트)가 항상 이 창 안에서 끝나도록 보장하기 위함.
    private const float MaxSwirlTime = 0.6f;

    private Renderer _renderer;
    private MaterialPropertyBlock _block;
    private float _localElapsed;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
    }

    // 활성화될 때마다 0부터 다시 시작 -- Time.unscaledTime(절대 세션 시간)을 그대로 먹이면
    // 켜져 있는 시간이 길어질수록 각도가 무한히 감겨 다중 나선(paisley) 패턴으로 보이는 문제 방지.
    private void OnEnable()
    {
        _localElapsed = 0f;
    }

    private void Update()
    {
        _localElapsed += Time.unscaledDeltaTime;
        float shaderTime = Mathf.Min(_localElapsed, MaxSwirlTime);
        _renderer.GetPropertyBlock(_block);
        _block.SetFloat(UnscaledTimeId, shaderTime);
        _renderer.SetPropertyBlock(_block);
    }
}
