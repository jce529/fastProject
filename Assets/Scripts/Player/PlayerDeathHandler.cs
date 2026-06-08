using UnityEngine;

/// <summary>
/// Phase 3 death handler. Subscribes to PlayerController.OnPlayerDeath.
/// D-14: On death — disable player GameObject + Debug.Log. No UI (Phase 4 owns that).
/// D-15: Phase 4's UIManager subscribes alongside this — no modification needed here.
///
/// Attach to: Player GameObject.
/// </summary>
public class PlayerDeathHandler : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += HandleDeath;
    }

    private void OnDisable()
    {
        // Always unsubscribe — static event persists across Play Mode restarts.
        // Prevents stale double-fire if domain reload is disabled in Editor settings.
        PlayerController.OnPlayerDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        Debug.Log("Player died");
        gameObject.SetActive(false);
    }
}
