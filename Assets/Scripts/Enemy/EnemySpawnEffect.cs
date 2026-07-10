using System.Collections;
using UnityEngine;

/// <summary>
/// SPWN-01/SPWN-02/D-06/D-07/D-08/D-09: 적 스폰 시 포탈 성장 + 실제 걸어나오는 이동(마스크 수축과
/// 동시 진행) + 포탈 축소 연출을 재생하고, 완료 즉시 ISpawnGatable 게이트를 해제한다.
/// EnemyType(Melee/Ranged, 추후 Boss)에 종속되지 않는다 — GetComponent&lt;ISpawnGatable&gt;()로만
/// 상호작용하며 캐스팅하지 않는다 (Phase 16 BossEnemy 재사용 전제, SPWN Success Criterion 5).
/// EnemySpawner.Activate()에서만 AddComponent+StartCoroutine으로 호출된다 (Awake/OnEnable 트리거 금지 — STATE.md 제약).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpawnEffect : MonoBehaviour
{
    [SerializeField] private float _portalGrowDuration    = 0.4f; // D-06: FloorTransitionEffect X1과 동일 기준
    [SerializeField] private float _walkMaskDuration       = 0.5f; // D-06/D-07: 걸어나오기+마스크 수축 동시 재생 구간
    [SerializeField] private float _portalFadeDuration    = 0.3f; // D-06: FloorTransitionEffect X4와 동일 기준
    [SerializeField] private float _walkOutDistance        = 1f;   // D-07: 포탈 중심에서 걸어나오는 거리(Inspector 조정 가능)
    [SerializeField] private float _portalScaleMultiplier = 1.2f; // D-08: 적 스프라이트 크기 대비 포탈 배율

    private SpriteRenderer _sr;
    private Rigidbody2D    _rb;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>portalEffectPrefab이 null이면 포탈 비주얼 없이 걸어나오기+게이팅만 재생한다
    /// (DebugRoomTeleporter 등 포탈 프리팹을 넘기지 않는 호출자 호환).</summary>
    public IEnumerator PlaySpawnSequence(GameObject portalEffectPrefab, ISpawnGatable gate)
    {
        Vector3 restPos = transform.position; // EnemySpawner 마커 위치 — 최종 정지 위치(변경 없음)

        // D-08/Pitfall 3: Vector3.one 하드코딩 금지 — 스프라이트 실제 크기 기준으로 포탈 크기 계산
        float spriteSize = Mathf.Max(_sr.bounds.size.x, _sr.bounds.size.y);
        Vector3 targetPortalScale = Vector3.one * spriteSize * _portalScaleMultiplier;

        // 1. 포탈 성장 (D-06 — FloorTransitionEffect.PlayExit() X1과 동일 구조)
        GameObject portal = null;
        if (portalEffectPrefab != null)
        {
            portal = Instantiate(portalEffectPrefab, restPos, Quaternion.identity);
            portal.transform.localScale = Vector3.zero;
        }
        AudioManager.PlaySfx(Sfx.PortalEnter); // D-09: 일반 적 스폰음 재사용 — 신규 클립 임포트 없음
        if (portal != null)
            yield return ScaleTransform(portal.transform, Vector3.zero, targetPortalScale, _portalGrowDuration);

        // 2. D-07: 포탈 중심에서 실제로 걸어나오는 이동 — Rigidbody2D를 잠시 Kinematic으로 전환해
        // 중력/충돌 간섭 없이 startPos -> restPos로 이동시킨 뒤 원래 BodyType을 복원한다.
        RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;
        Vector3 startPos = restPos + Vector3.left * _walkOutDistance;
        if (_rb != null)
        {
            originalBodyType = _rb.bodyType;
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.linearVelocity = Vector2.zero;
            _rb.position = startPos;
        }
        else
        {
            transform.position = startPos;
        }

        // SpriteMask 수축(FloorTransitionEffect와 동일한 VisibleOutsideMask 와이프 패턴)과 물리 이동을
        // 같은 시간축으로 동시 진행한다 — 이것이 D-07이 요구하는 "실제로 걸어나온다"는 느낌의 핵심.
        var maskGO = new GameObject("SpawnMask");
        var mask = maskGO.AddComponent<SpriteMask>();
        mask.sprite = RuntimeMaskSprite.CreateMaskSprite();
        _sr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

        float portalX = restPos.x;
        float dir = Mathf.Sign(startPos.x - portalX); // startPos가 restPos 왼쪽이므로 -1
        float startWidth = Mathf.Abs(startPos.x - portalX) + _sr.bounds.extents.x + 0.3f;

        float elapsed = 0f;
        while (elapsed < _walkMaskDuration)
        {
            elapsed += Time.unscaledDeltaTime; // 슬로우모션/HitFreeze 면역
            float t = Mathf.Clamp01(elapsed / _walkMaskDuration);

            Vector3 currentPos = Vector3.Lerp(startPos, restPos, t);
            if (_rb != null) _rb.MovePosition(currentPos);
            else transform.position = currentPos;

            float width = Mathf.Lerp(startWidth, 0f, t);
            Vector3 maskPos = currentPos;
            maskPos.x = portalX + (width * 0.5f * dir);
            maskGO.transform.position = maskPos;
            maskGO.transform.localScale = new Vector3(Mathf.Max(width, 0.001f), 20f, 1f);

            yield return null;
        }

        if (_rb != null)
        {
            _rb.position = restPos;
            _rb.bodyType = originalBodyType;
        }
        else
        {
            transform.position = restPos;
        }

        Destroy(maskGO);
        _sr.maskInteraction = SpriteMaskInteraction.None;

        // 3. 포탈 축소 후 정리 (D-06 — FloorTransitionEffect.PlayExit() X4와 동일 구조)
        if (portal != null)
        {
            yield return ScaleTransform(portal.transform, targetPortalScale, Vector3.zero, _portalFadeDuration);
            Destroy(portal);
        }

        // 4. SPWN-02: 연출 완료 즉시 게이트 해제 — 정상 FSM/타겟팅 재개
        gate?.SetSpawnGate(false);
    }

    /// <summary>공용 스케일 보간 헬퍼 — FloorTransitionEffect.ScaleTransform()과 동일 패턴(독립 사본,
    /// EnemyDeathEffect처럼 이펙트 컴포넌트가 자기 완결적이도록 공용 유틸로 추출하지 않음).</summary>
    private IEnumerator ScaleTransform(Transform t, Vector3 from, Vector3 to, float duration)
    {
        if (t == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (t == null) yield break;
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (t != null) t.localScale = to;
    }
}
