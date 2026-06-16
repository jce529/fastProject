using UnityEngine;

/// <summary>
/// ATCK-05: 크로노 게이지(Chrono Gauge) — Attack을 누르는 동안 소모되고, 떼면 자동 충전.
/// HUD (Phase 4)가 직접 읽는 [0, 1] float 값으로 노출.
///
/// 모든 타이밍은 Time.unscaledDeltaTime 기준 — 슬로우 모션 중에도 실제 시간 속도로 소모.
/// </summary>
public class ChronoGaugeController : MonoBehaviour
{
    [SerializeField] private float drainPerSecond = 0.25f; // 4 seconds to empty
    [SerializeField] private float regenPerSecond = 0.15f; // ~6.7 seconds to full
    [SerializeField] private float killBonus      = 0.20f; // +20% on kill

    /// <summary>Current gauge value in [0, 1]. Read by HUD (Phase 4) directly.</summary>
    public float Value { get; private set; } = 1f;

    /// <summary>True when the gauge has completely emptied.</summary>
    public bool IsEmpty => Value <= 0f;

    private bool _isDraining;

    /// <summary>Called by CombatController every Update. True while Attack is held.</summary>
    public void SetDraining(bool drain) => _isDraining = drain;

    /// <summary>Called by CombatController after a successful kill. Adds kill bonus to gauge.</summary>
    public void AddKillBonus() => Value = Mathf.Min(1f, Value + killBonus);

    private void Update()
    {
        if (_isDraining)
            Value = Mathf.Max(0f, Value - drainPerSecond * Time.unscaledDeltaTime);
        else
            Value = Mathf.Min(1f, Value + regenPerSecond * Time.unscaledDeltaTime);
    }
}
