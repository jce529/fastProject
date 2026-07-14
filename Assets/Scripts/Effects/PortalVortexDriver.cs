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

    private Renderer _renderer;
    private MaterialPropertyBlock _block;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
    }

    private void Update()
    {
        _renderer.GetPropertyBlock(_block);
        _block.SetFloat(UnscaledTimeId, Time.unscaledTime);
        _renderer.SetPropertyBlock(_block);
    }
}
