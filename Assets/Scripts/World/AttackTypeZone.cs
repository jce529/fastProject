using UnityEngine;

/// <summary>
/// World-space trigger zone that sets the active attack type when the player enters.
/// Place on a GameObject with a Collider2D (IsTrigger = true).
/// Set zoneType in Inspector to Linear or Fan.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AttackTypeZone : MonoBehaviour
{
    [SerializeField] private AttackType zoneType;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        AttackTypeSelector.SetType(zoneType);
    }
}
