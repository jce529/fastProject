using System.Collections;
using UnityEngine;

/// <summary>
/// D-09: 적 사망 연출 -- 기존 Die 애니메이션 재생 -> 파티클 재생 + SpriteMask로 아래에서 위로
/// 가리며 사라짐 -> Destroy. SpriteMask 생성 패턴은 FloorTransitionEffect(D-01)와 동일한
/// RuntimeMaskSprite.CreateMaskSprite()를 재사용한다. MeleeEnemy/RangedEnemy가 OnDashHit()에서
/// AddComponent 후 StartCoroutine(PlayDeathSequence(...))로 호출한다.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class EnemyDeathEffect : MonoBehaviour
{
    [SerializeField] private float _maskRiseDuration = 0.6f;
    [SerializeField] private Color _particleColor = new Color(1f, 0.3f, 0.1f);

    private SpriteRenderer _sr;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
    }

    public IEnumerator PlayDeathSequence(Animator animator)
    {
        // 1. Die 애니메이션 재생 완료 대기
        if (animator != null)
        {
            yield return null; // isDead bool 전환이 이 프레임에 반영되도록 한 프레임 대기
            float dieLength = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSecondsRealtime(dieLength);
        }

        // 2. 파티클 재생
        SpawnDeathParticles();

        // 3. SpriteMask 아래->위 페이드
        var maskGO = new GameObject("DeathMask");
        var mask = maskGO.AddComponent<SpriteMask>();
        mask.sprite = RuntimeMaskSprite.CreateMaskSprite();
        _sr.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask;

        float height = _sr.bounds.size.y;
        maskGO.transform.position = transform.position + Vector3.down * _sr.bounds.extents.y;

        float elapsed = 0f;
        while (elapsed < _maskRiseDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _maskRiseDuration);
            float maskHeight = Mathf.Lerp(0f, height, t);
            maskGO.transform.position = transform.position + Vector3.down * (_sr.bounds.extents.y - maskHeight * 0.5f);
            maskGO.transform.localScale = new Vector3(20f, Mathf.Max(maskHeight, 0.001f), 1f);
            yield return null;
        }

        // 4. 정리
        Destroy(maskGO);
        Destroy(gameObject);
    }

    private void SpawnDeathParticles()
    {
        var psGO = new GameObject("DeathParticles");
        psGO.transform.position = transform.position;
        var ps = psGO.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startColor = _particleColor;
        main.startLifetime = 0.5f;
        main.startSpeed = 3f;
        main.startSize = 0.15f;
        main.loop = false;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        ps.Play();
    }
}
