/// <summary>
/// Contract for all enemy types in Phase 3+.
/// CombatController uses only these three members — no Unity-specific base type.
/// All implementors are MonoBehaviours; cast via (target as MonoBehaviour) when transform access is needed.
/// D-01: IsAlive, OnDashHit(), ClearHighlight() are the sole interface members.
/// </summary>
public interface IEnemy
{
    /// <summary>False during death/respawn window. CombatController skips dead enemies.</summary>
    bool IsAlive { get; }

    /// <summary>Called by CombatController.ExecuteDash() after the player arrives. Triggers death logic.</summary>
    void OnDashHit();

    /// <summary>Resets highlight color to white. Called when this enemy is no longer targeted.</summary>
    void ClearHighlight();
}
