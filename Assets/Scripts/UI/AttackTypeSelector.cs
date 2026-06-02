using UnityEngine;

/// <summary>
/// ATCK-01: Shows a Canvas overlay at game start for attack type selection.
/// Player cannot act until a type is chosen. Selection is stored in a static field
/// for the session — no DontDestroyOnLoad or PlayerPrefs needed for a single-scene prototype.
///
/// [MEDIUM — Gemini] IsSelecting static flag: CombatController reads this in Update()
/// to prevent EnterSlowMotion() from firing while the overlay is active. This avoids
/// the timeScale race condition where combat input is processed before the player selects.
/// </summary>
public enum AttackType { Linear, Fan }

public class AttackTypeSelector : MonoBehaviour
{
    /// <summary>Session-wide attack type. Read by CombatController and RangeDisplay.</summary>
    public static AttackType Selected { get; private set; } = AttackType.Linear;

    /// <summary>
    /// [MEDIUM — Gemini] True while the selection overlay is active.
    /// CombatController checks this before entering slow-motion to prevent
    /// combat input from firing while the selection screen is visible.
    /// Reset to false when a selection is made.
    /// </summary>
    public static bool IsSelecting { get; private set; } = true;

    [SerializeField] private GameObject overlayRoot; // Assign the Canvas panel in Inspector

    private void Start()
    {
        // Guard: set IsSelecting before pausing so CombatController cannot fire.
        IsSelecting = true;
        // Pause the game while the overlay is active.
        // fixedDeltaTime must always be paired with timeScale (ROADMAP Stack Constraint).
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        overlayRoot.SetActive(true);
    }

    /// <summary>Called by the "Linear" Button's OnClick() in the Inspector.</summary>
    public void SelectLinear()
    {
        Selected = AttackType.Linear;
        IsSelecting = false;
        overlayRoot.SetActive(false);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    /// <summary>Called by the "Fan" Button's OnClick() in the Inspector.</summary>
    public void SelectFan()
    {
        Selected = AttackType.Fan;
        IsSelecting = false;
        overlayRoot.SetActive(false);
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
    }
}
