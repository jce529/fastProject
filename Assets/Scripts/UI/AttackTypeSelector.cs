using UnityEngine;

public enum AttackType { Linear, Fan }

// Zone-based attack type selector — always visible on screen.
// Type is set externally by AttackTypeZone triggers in the world.
// Not affected by timeScale.
//
// Inspector setup:
//   - Optionally assign linearHighlight / fanHighlight Image for visual feedback
//   - Place AttackTypeZone components on world colliders to drive type changes
public class AttackTypeSelector : MonoBehaviour
{
    public static AttackType Selected { get; private set; } = AttackType.Linear;

    public static bool IsSelecting => false;

    [SerializeField] private UnityEngine.UI.Image linearHighlight;
    [SerializeField] private UnityEngine.UI.Image fanHighlight;

    private static AttackTypeSelector _instance;

    private void Awake()
    {
        _instance = this;
    }

    private void Start() => RefreshHighlights();

    /// <summary>
    /// Called by AttackTypeZone when player enters a zone.
    /// Updates Selected and refreshes UI highlights.
    /// </summary>
    public static void SetType(AttackType type)
    {
        if (_instance == null) return;
        if (Selected == type) return;
        Selected = type;
        _instance.RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        if (linearHighlight != null)
            linearHighlight.color = Selected == AttackType.Linear ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        if (fanHighlight != null)
            fanHighlight.color = Selected == AttackType.Fan ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }
}
