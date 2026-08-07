using UnityEngine;

public class CombatContext
{
    public Rigidbody2D Rb;
    public SpriteRenderer SpriteRenderer;
    public Animator Animator;
    public TrailRenderer TrailRenderer;
    public InvincibilityHandler Invincibility;
    public CameraFollow CameraFollow;
    public ChronoGaugeController Gauge;
    public Camera MainCamera;

    public GameObject HitSparkPrefab;
    public float DashDuration;
    public float HitFreezeDuration;
    public float PostKillLockout;
    public float WhiffLockout;
    public float CameraShakeDuration;
    public float CameraShakeAmplitude;
    public float SearchRadius;
    public float FanRadius;
    public float FanHalfAngleDeg;
    public float LinearHalfAngleDeg;

    public ContactFilter2D EnemyFilter;
    public Collider2D[] HitBuffer;
    public int ObstacleMask;
    public float SwingRadius;       // D-01: 기본/사무라이 전투형 모듈 스윙 반경 (Overclock의 SearchRadius/FanRadius와 독립 — Open Question 2)
    public float SwingHalfAngleDeg; // D-01: 스윙 부채꼴 절반각
    public float TapLockout;        // D-03: 탭 공격 사이 짧은 고정 락아웃

    public System.Action<float> SetAttackCooldown;
}
