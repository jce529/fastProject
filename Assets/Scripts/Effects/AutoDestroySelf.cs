using System.Collections;
using UnityEngine;

/// <summary>
/// 임의 이펙트를 실시간(초) 뒤 자동 Destroy, WaitForSecondsRealtime 기반으로
/// HitFreeze(timeScale=0) 중에도 정상 종료.
/// Animator가 있으면 현재 클립 길이만큼, 없으면 _fallbackSeconds 만큼 대기한다.
/// </summary>
public class AutoDestroySelf : MonoBehaviour
{
    [SerializeField] private float _fallbackSeconds = 0.3f;

    private IEnumerator Start()
    {
        float seconds = _fallbackSeconds;
        var animator = GetComponent<Animator>();
        if (animator != null)
        {
            yield return null;
            seconds = animator.GetCurrentAnimatorStateInfo(0).length;
        }

        yield return new WaitForSecondsRealtime(seconds);
        Destroy(gameObject);
    }
}
