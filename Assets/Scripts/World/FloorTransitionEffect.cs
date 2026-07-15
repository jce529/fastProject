using System.Collections;
using UnityEngine;

/// <summary>
/// D-01/D-03 -- 포탈 입장/퇴장 SpriteMask 애니메이션 컴포넌트. Player GameObject에
/// 부착된다. WorldGenerator(Plan 12-02)가 PlayEntry()/PlayExit() 코루틴을 호출해
/// FloorTransitionSequence의 ENTRY/EXIT 구간을 재생한다. 모든 타이밍은
/// Time.unscaledDeltaTime 기반이라 슬로우모션/HitFreeze(timeScale=0) 중에도 정상 진행된다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FloorTransitionEffect : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private Animator _animator;

    private static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");
    private static readonly int EffectAlphaId  = Shader.PropertyToID("_EffectAlpha");

    [Header("Entry Vortex (Phase 999.3 D-01~D-03)")]
    [SerializeField] private Material _vortexMaterial;          // PortalVortex.mat — WorldGenerator가 PlayEntry() 호출 시 전달 (999.3-01 산출물)
    [SerializeField] private float _entryVortexDuration = 0.4f; // D-08: 기존 E1-E4 총합(~0.4s)과 동일 기준 유지
    [SerializeField] private float _vortexWorldRadius = 4f;     // 소용돌이가 덮는 월드 반경 — 플레이어+주변 타일 포함

    [SerializeField] private float _exitPortalGrowDuration = 0.4f;
    [SerializeField] private float _exitMaskDuration = 0.5f;    // NOTE: Plan 999.3-02 Task 2가 이 필드를 leap 필드로 교체 예정 — 이 태스크에서는 값만 유지
    [SerializeField] private float _portalFadeDuration = 0.3f;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();       // D-05: 흡입/도약 이동에 사용 — Player 루트에 이미 존재(CombatController RequireComponent)
        _animator = GetComponent<Animator>();    // D-04: IsDashing 재사용에 사용
    }

    /// <summary>D-01/D-02/D-03: 포탈 진입 시 URP 셰이더 기반 소용돌이 흡입 이펙트를 재생하고, 플레이어
    /// Transform 자체도 포탈 중심으로 실제 이동한다(기존 SpriteMask 페이드를 완전히 대체 — 겹쳐 쓰지 않음).
    /// vortexMaterial은 WorldGenerator가 호출 시 전달한다(null이면 이동만 재생, 셰이더 오버레이는 생략).</summary>
    public IEnumerator PlayEntry(Transform portal, Material vortexMaterial)
    {
        AudioManager.PlaySfx(Sfx.PortalEnter); // SFX-02/D-07: 그대로 재사용 — 새 이펙트 시작과 동시

        Vector3 startPos = transform.position;

        GameObject vortexGO = null;
        Material vortexMat = null;
        if (vortexMaterial != null)
        {
            vortexGO = new GameObject("EntryVortex");
            vortexGO.transform.position = portal.position;
            vortexGO.transform.localScale = new Vector3(_vortexWorldRadius * 2f, _vortexWorldRadius * 2f, 1f);
            var vortexSr = vortexGO.AddComponent<SpriteRenderer>();
            vortexSr.sprite = RuntimeMaskSprite.CreateMaskSprite(); // 기존 12-01/14-01 공용 4x4 흰 스프라이트 재사용 — UV 0..1 매핑용
            vortexSr.sortingLayerName = "PortalVFX"; // 999.3-01에서 신설한 레이어 — Default보다 항상 위에 렌더되어야 그랩 텍스처를 왜곡해 덮어씌울 수 있다
            vortexMat = new Material(vortexMaterial); // Pitfall 3: 인스턴스 카피 — sharedMaterial 절대 금지(플레이어+전 Room Tilemap이 공유하는 기본 머티리얼과 무관하게 항상 신규 인스턴스)
            vortexSr.material = vortexMat;
        }

        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        float elapsed = 0f;
        while (elapsed < _entryVortexDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 슬로우모션/HitFreeze 면역
            float t = Mathf.Clamp01(elapsed / _entryVortexDuration);

            if (vortexMat != null)
            {
                // Pitfall 1: Shader Graph/_Time 대신 반드시 수동으로 Time.unscaledTime을 매 프레임 주입
                vortexMat.SetFloat(UnscaledTimeId, Time.unscaledTime);
                vortexMat.SetFloat(EffectAlphaId, Mathf.Sin(t * Mathf.PI)); // 0→1→0 — 등장과 동시에 옅어지며 사라짐
            }

            // 핵심 불만 해소(999.3-CONTEXT.md <domain>): 플레이어 Transform이 실제로 포탈 중심을 향해
            // 이동한다 — 기존엔 SpriteMask만 움직이고 Transform은 고정이었다.
            Vector3 pos = Vector3.Lerp(startPos, portal.position, t * t); // ease-in
            if (_rb != null) _rb.MovePosition(pos);
            else transform.position = pos;

            yield return null;
        }

        if (_rb != null) _rb.MovePosition(portal.position);
        else transform.position = portal.position;

        _sr.enabled = false; // 기존 E4와 동일 목적 — 완전 비가시 전환(새 층 텔레포트 전까지)
        if (vortexGO != null) Destroy(vortexGO);
    }

    /// <summary>X1-X4: 새 층 진입 시 포탈이 성장하고, 플레이어가 마스크 수축에 의해 포탈에서 걸어나오듯 나타난다.</summary>
    public IEnumerator PlayExit(Vector3 spawnWorldPos, GameObject portalEffectPrefab)
    {
        AudioManager.PlaySfx(Sfx.PortalExit); // SFX-02/D-06: 퇴장 = 하강 마무리음 — X1 포탈 성장(0.4s)과 동시 시작
        // X1: 퇴장 포탈 이펙트 성장.
        GameObject portalEffect = null;
        if (portalEffectPrefab != null)
        {
            portalEffect = Instantiate(portalEffectPrefab, spawnWorldPos, Quaternion.identity);
            portalEffect.transform.localScale = Vector3.zero;
            yield return ScaleTransform(portalEffect.transform, Vector3.zero, Vector3.one, _exitPortalGrowDuration);
        }

        float portalX = spawnWorldPos.x;
        float dir = transform.position.x > portalX ? 1f : -1f;
        float startWidth = Mathf.Abs(transform.position.x - portalX) + _sr.bounds.extents.x + 2f;

        var maskGO = new GameObject("ExitMask");
        var mask = maskGO.AddComponent<SpriteMask>();
        mask.sprite = RuntimeMaskSprite.CreateMaskSprite();
        _sr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;
        _sr.enabled = true;

        // X2: 마스크가 startWidth -> 0으로 수축하며 플레이어가 포탈에서 걸어나오는 효과를 낸다.
        float elapsed = 0f;
        while (elapsed < _exitMaskDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _exitMaskDuration);
            float width = Mathf.Lerp(startWidth, 0f, t);
            Vector3 pos = transform.position;
            pos.x = portalX + (width * 0.5f * dir);
            maskGO.transform.position = pos;
            maskGO.transform.localScale = new Vector3(Mathf.Max(width, 0.001f), 20f, 1f);
            yield return null;
        }

        // X3: 마스크 정리.
        Destroy(maskGO);
        _sr.maskInteraction = SpriteMaskInteraction.None;

        // X4: 퇴장 포탈 이펙트 페이드 아웃 후 정리.
        if (portalEffect != null)
        {
            yield return ScaleTransform(portalEffect.transform, Vector3.one, Vector3.zero, _portalFadeDuration);
            Destroy(portalEffect);
        }
    }

    /// <summary>공용 스케일 보간 헬퍼. Time.unscaledDeltaTime 사용 -- 슬로우모션/HitFreeze 면역.</summary>
    private IEnumerator ScaleTransform(Transform t, Vector3 from, Vector3 to, float duration)
    {
        if (t == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (t == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            float lerp = Mathf.Clamp01(elapsed / duration);
            t.localScale = Vector3.Lerp(from, to, lerp);
            yield return null;
        }

        if (t != null) t.localScale = to;
    }
}
