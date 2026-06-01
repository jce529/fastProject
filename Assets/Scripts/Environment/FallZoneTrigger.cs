using UnityEngine;

/// <summary>
/// Attach to FallZone_Left and FallZone_Right trigger colliders.
/// When the player enters the trigger zone, calls FallDetector.OnFall().
///
/// Uses the "Player" tag to identify the player — avoids FindObjectsOfType.
/// </summary>
public class FallZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var fallDetector = other.GetComponent<FallDetector>();
        if (fallDetector == null) return;

        fallDetector.OnFall();
    }
}
