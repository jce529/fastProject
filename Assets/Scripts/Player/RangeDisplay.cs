using UnityEngine;

/// <summary>
/// Stub — full implementation in Plan 02-03 (RangeDisplay visual feedback).
/// CombatController calls Show()/Hide() via null-conditional; this stub satisfies
/// the type reference so the project compiles before 02-03 is executed.
/// </summary>
public class RangeDisplay : MonoBehaviour
{
    /// <summary>Called by CombatController.EnterSlowMotion() to reveal the attack range indicator.</summary>
    public void Show() { }

    /// <summary>Called by CombatController.ExitSlowMotion() to hide the attack range indicator.</summary>
    public void Hide() { }
}
