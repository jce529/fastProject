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

    public System.Action<float> SetAttackCooldown;
}
