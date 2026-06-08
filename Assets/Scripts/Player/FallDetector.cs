using UnityEngine;

/// <summary>
/// D-17: Fall = instant death. Teleport recovery removed.
/// FallZoneTrigger (child trigger collider) calls OnFall() when the player enters the fall zone.
/// OnPlayerDeath fires → PlayerDeathHandler disables the player GameObject.
/// Phase 1 last-safe-position tracking removed — no recovery in Phase 3+.
/// </summary>
public class FallDetector : MonoBehaviour
{
    /// <summary>
    /// Called by FallZoneTrigger when the player enters a fall zone.
    /// Fires the death event. PlayerDeathHandler handles the response.
    /// </summary>
    public void OnFall()
    {
        PlayerController.TriggerDeath();
    }
}
