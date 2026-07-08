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

    [SerializeField] private float _entryMaskDuration = 0.4f;
    [SerializeField] private float _portalShrinkDuration = 0.3f;
    [SerializeField] private float _exitPortalGrowDuration = 0.4f;
    [SerializeField] private float _exitMaskDuration = 0.5f;
    [SerializeField] private float _portalFadeDuration = 0.3f;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>E1-E4: 포탈 진입 시 플레이어가 포탈 경계선 너머로 점진적으로 사라지고, 포탈이 수축한다.</summary>
    public IEnumerator PlayEntry(Transform portal)
    {
        float portalX = portal.position.x;
        float dir = transform.position.x > portalX ? 1f : -1f;
        float targetWidth = Mathf.Abs(transform.position.x - portalX) + _sr.bounds.extents.x;

        var maskGO = new GameObject("EntryMask");
        var mask = maskGO.AddComponent<SpriteMask>();
        mask.sprite = RuntimeMaskSprite.CreateMaskSprite();
        _sr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

        // E2: 마스크가 0 -> targetWidth로 성장하며 플레이어를 포탈 경계선 너머로 가린다.
        float elapsed = 0f;
        while (elapsed < _entryMaskDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _entryMaskDuration);
            float width = Mathf.Lerp(0f, targetWidth, t);
            Vector3 pos = transform.position;
            pos.x = portalX + (width * 0.5f * dir);
            maskGO.transform.position = pos;
            maskGO.transform.localScale = new Vector3(Mathf.Max(width, 0.001f), 20f, 1f);
            yield return null;
        }

        // E3: 포탈 자체가 수축한다.
        yield return ScaleTransform(portal, Vector3.one, Vector3.zero, _portalShrinkDuration);

        // E4: 플레이어 완전 비가시 상태로 전환.
        _sr.enabled = false;
        Destroy(maskGO);
    }

    /// <summary>X1-X4: 새 층 진입 시 포탈이 성장하고, 플레이어가 마스크 수축에 의해 포탈에서 걸어나오듯 나타난다.</summary>
    public IEnumerator PlayExit(Vector3 spawnWorldPos, GameObject portalEffectPrefab)
    {
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
