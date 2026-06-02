using UnityEngine;

/// <summary>
/// ATCK-05: Time-stop gauge that drains while Attack is held and regens when released.
/// Exposed as a [0, 1] float for HUD (Phase 4) to read directly.
///
/// All timing uses Time.unscaledDeltaTime — drain rate is independent of timeScale.
/// This means the gauge drains at the same wall-clock speed during slow-motion as in normal time.
/// </summary>
public class GaugeController : MonoBehaviour
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
