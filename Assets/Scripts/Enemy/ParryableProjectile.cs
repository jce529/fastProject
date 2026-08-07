using UnityEngine;

/// <summary>
/// D-03/D-06/Pitfall 2: SAMURAI의 패링 전용 타이밍 공격을 물리 트리거로 전달하는 투사체.
/// ProjectileController.cs와 동일한 Rigidbody2D(Dynamic, Gravity=0, Continuous+Interpolate)
/// 패턴을 따르되 IParryable을 구현해 SamuraiParryModule(19-03)이 스윙 범위 안에서 검출한다.
/// 죽음 전달은 오직 OnTriggerEnter2D뿐이다(Pitfall 2) — RollController의 PlayerInvincible
/// 레이어 교체가 이 트리거를 자동으로 무시하므로 구르기 회피(D-05)가 별도 코드 없이 성립한다.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ParryableProjectile : MonoBehaviour, IParryable
{
    private Rigidbody2D _rb;
    private Vector2 _startPosition;
    private float _maxDistance;
    private System.Action<Vector2> _onParried;
    private bool _consumed;

    public Vector2 Position => transform.position;

    private void Awake() => _rb = GetComponent<Rigidbody2D>();

    /// <summary>SamuraiBoss.ParryTimingSegment()가 인스턴스 생성 직후(첫 FixedUpdate 이전) 호출한다.</summary>
    public void Init(Vector2 direction, float speed, float maxDistance, System.Action<Vector2> onParried)
    {
        _startPosition = _rb.position;
        _maxDistance = maxDistance;
        _onParried = onParried;
        _rb.linearVelocity = direction.normalized * speed;
    }

    private void FixedUpdate()
    {
        if ((_rb.position - _startPosition).sqrMagnitude >= _maxDistance * _maxDistance)
            Destroy(gameObject);
    }

    /// <summary>D-06: 패링 성공 — 순수 방어, 보스에게 데미지 없음. 투사체 소멸 + 그로기 게이지
    /// 콜백만 수행한다.</summary>
    public void OnParried(Vector2 reflectDirection)
    {
        if (_consumed) return;
        _consumed = true;
        _onParried?.Invoke(reflectDirection);
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_consumed) return;
        if (other.CompareTag("Player"))
        {
            _consumed = true;
            // Pitfall 2: 오직 이 트리거 경유만 — PlayerInvincible 레이어(구르기 i-frame)가
            // Physics2D 충돌 매트릭스에서 자동으로 이 트리거와의 충돌을 배제해 D-05를 보장한다.
            PlayerController.TriggerDeath();
            Destroy(gameObject);
        }
    }
}
