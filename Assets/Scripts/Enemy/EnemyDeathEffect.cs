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
    [SerializeField] private int _particleBurstCount = 12; // 기본값 12 — MeleeEnemy/RangedEnemy 기존 동작과 동일 유지

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
            // Animator는 Time.timeScale의 영향을 받는 Scaled Time으로 재생되지만 WaitForSecondsRealtime은
            // timeScale과 무관한 실시간이라 HitFreeze(timeScale=0) 등과 겹치면 어긋난다 -- 고정 시간 대기 대신
            // Die 상태의 실제 재생 완료(normalizedTime >= 1) 여부를 매 프레임 직접 확인한다.
            while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Die")
                   || animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f)
            {
                yield return null;
            }

            // Die.anim은 loop 클립이라 완주 후 그대로 두면 처음부터 다시 재생된다(캐릭터가 일어나는 것처럼 보임).
            // 파티클/마스크 페이드가 진행되는 동안 마지막 포즈에 고정되도록 Animator 재생을 멈춘다.
            animator.speed = 0f;
        }

        // 2. 파티클 재생
        AudioManager.PlaySfx(Sfx.EnemyDeathGlitch); // SFX-04/D-05: 시뮬레이션 NPC 붕괴 노이즈 — Die 애니메이션 완주 후라 슬래시와 자연 시간차
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

    /// <summary>
    /// D-08: 이 컴포넌트는 AddComponent()로 매번 새로 붙기 때문에 Inspector 프리팹 값이 존재하지 않는다
    /// (EnemyDeathEffect.cs 상단 doc-comment 참조). BossEnemy.Die()가 AddComponent 직후,
    /// StartCoroutine(PlayDeathSequence(...)) 호출 전에 호출해 보스 전용 강도로 재정의한다.
    /// MeleeEnemy/RangedEnemy는 이 메서드를 호출하지 않으므로 기존 기본값 그대로 동작한다.
    /// </summary>
    public void ConfigureIntensity(float maskRiseDuration, Color particleColor, int particleBurstCount)
    {
        _maskRiseDuration = maskRiseDuration;
        _particleColor = particleColor;
        _particleBurstCount = particleBurstCount;
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
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)_particleBurstCount) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.3f;

        ps.Play();
    }
}
