using System.Collections;
using UnityEngine;

/// <summary>
/// Phase 999.3 D-01~D-08 -- 포탈 입장(소용돌이 흡입 + 실제 Transform 이동)/퇴장(IsDashing 재사용
/// 수직 도약) 연출 컴포넌트. Player GameObject에 부착된다. WorldGenerator(Plan 999.3-02)가
/// PlayEntry()/PlayExit() 코루틴을 호출해 FloorTransitionSequence의 ENTRY/EXIT 구간을 재생한다.
/// 모든 타이밍은 Time.unscaledDeltaTime 기반이라 슬로우모션/HitFreeze(timeScale=0) 중에도 정상 진행된다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class FloorTransitionEffect : MonoBehaviour
{
    private SpriteRenderer _sr;
    private Rigidbody2D _rb;
    private Animator _animator;

    private static readonly int UnscaledTimeId = Shader.PropertyToID("_UnscaledTime");
    private static readonly int EffectAlphaId  = Shader.PropertyToID("_EffectAlpha");

    // round 6에서 t(0→1)를 그대로 EntrySwirlPhase까지 감아올리는 애니메이션 방식은 확정됐지만
    // (999.3-01의 PortalVortexDriver.MaxSwirlTime=0.6f를 시작값으로 재사용한 결과) 회전감이 여전히
    // 약하다는 피드백(round 7)에 따라 진입 연출(_entryVortexDuration=0.4s) 전용으로 별도 튜닝한
    // 값이다 -- 더 이상 PortalVortexDriver.MaxSwirlTime과 같을 필요는 없다(_SwirlStrength=6 기준
    // angle_max가 3.6rad에서 6.0rad로 상승). 999.3-01에서 문제였던 "과도하게 감긴" 느낌(다회전)까지는
    // 가지 않는 선에서 강도만 올렸다 -- 부족/과함이 재확인되면 이 상수만 다시 조정한다.
    private const float EntrySwirlPhase = 1.0f;

    [Header("Entry Vortex (Phase 999.3 D-01~D-03)")]
    [SerializeField] private Material _vortexMaterial;          // PortalVortex.mat — WorldGenerator가 PlayEntry() 호출 시 전달 (999.3-01 산출물)
    [SerializeField] private float _entryVortexDuration = 0.4f; // D-08: 기존 E1-E4 총합(~0.4s)과 동일 기준 유지
    [SerializeField] private float _vortexWorldRadius = 2f;     // 소용돌이가 덮는 월드 반경 — 플레이어+주변 타일 포함

    [Header("Exit Portal Backdrop — X1/X4 존치 (Open Question 1 기본값: KEEP)")]
    [SerializeField] private float _exitPortalGrowDuration = 0.4f;
    [SerializeField] private float _portalFadeDuration = 0.3f;

    [Header("Exit Leap (Phase 999.3 D-04~D-06)")]
    [SerializeField] private float _exitLeapDuration = 0.45f; // D-08: 기존 X1-X4 총합(~0.4~0.5s)과 동일 기준 유지
    [SerializeField] private float _exitLeapHeight = 2.5f;    // 포탈에서 머리 위로 튀어오르는 높이(월드 유닛)

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
                // Pitfall 1: Shader Graph/_Time 대신 반드시 수동으로 시간을 매 프레임 주입.
                // 999.3-01 Deviation #6/#7과 동일 클래스 버그 회피: 절대 Time.unscaledTime(세션 절대
                // 시간)을 먹이지 않는다. round 4에서는 이 코루틴의 로컬 elapsed를 그대로 먹여
                // _entryVortexDuration(0.4s)까지만 감기다 보니 각도가 얕아 옅게 보였고(round 4 피드백),
                // round 5는 EntrySwirlPhase 고정값으로 홀드해 각도는 확정됐지만 매 프레임 값이 바뀌지
                // 않아 회전감/흡입감이 완전히 사라졌다(round 5 피드백). t(0→1, 이미 알파 페이드/포지션
                // 보간에 쓰이는 진행률)를 그대로 재사용해 EntrySwirlPhase까지 부드럽게 감아올리면,
                // 애니메이션(회전감)과 t=1 시점의 확정 각도(강도)를 동시에 만족한다.
                vortexMat.SetFloat(UnscaledTimeId, t * EntrySwirlPhase);
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

    /// <summary>D-04/D-05/D-06: 새 층 진입 시 기존 IsDashing 애니메이터 대시 모션을 재사용해 포탈에서
    /// 머리 위로 짧게 도약(수직 arc)한 뒤 원래 spawnWorldPos에 정확히 착지한다. 기존 SpriteMask
    /// 수축(X2/X3)은 완전히 대체됨 — X1(포탈 성장)/X4(포탈 페이드)는 Open Question 1 기본값(KEEP)에
    /// 따라 배경 요소로 존치한다.</summary>
    public IEnumerator PlayExit(Vector3 spawnWorldPos, GameObject portalEffectPrefab)
    {
        AudioManager.PlaySfx(Sfx.PortalExit); // SFX-02/D-07: 그대로 재사용

        // X1(존치): 퇴장 포탈 이펙트 성장 — 도약과 동시 진행(병렬, StartCoroutine — 대기하지 않음)
        GameObject portalEffect = null;
        if (portalEffectPrefab != null)
        {
            portalEffect = Instantiate(portalEffectPrefab, spawnWorldPos, Quaternion.identity);
            portalEffect.transform.localScale = Vector3.zero;
            StartCoroutine(ScaleTransform(portalEffect.transform, Vector3.zero, Vector3.one, _exitPortalGrowDuration));
        }

        // D-04/D-05/D-06: X2/X3 마스크 로직을 수직 도약으로 완전히 대체
        _sr.enabled = true;
        if (_animator == null) _animator = GetComponent<Animator>(); // Awake에서 이미 캐시되지만 방어적으로 재확인
        _animator?.SetBool("IsDashing", true);
        if (_rb != null) _rb.linearVelocity = Vector2.zero;

        // Pitfall 5: spawnWorldPos는 이미 WorldGenerator.FloorTransitionSequence Step 2에서 하드
        // 텔레포트된 현재 위치와 동일하다 — 수평 이동 없이 순수 수직 오프셋만 적용한다.
        float elapsed = 0f;
        while (elapsed < _exitLeapDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 슬로우모션/HitFreeze 면역
            float t = Mathf.Clamp01(elapsed / _exitLeapDuration);
            float height = _exitLeapHeight * Mathf.Sin(t * Mathf.PI); // 0 -> 정점 -> 0
            Vector3 pos = spawnWorldPos + Vector3.up * height;
            if (_rb != null) _rb.MovePosition(pos);
            else transform.position = pos;
            yield return null;
        }

        if (_rb != null) _rb.MovePosition(spawnWorldPos);
        else transform.position = spawnWorldPos;
        _animator?.SetBool("IsDashing", false);

        // X4(존치): 퇴장 포탈 이펙트 페이드 아웃 후 정리.
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
