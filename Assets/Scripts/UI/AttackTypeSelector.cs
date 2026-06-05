using UnityEngine;

public enum AttackType { Linear, Fan }

// Zone-based attack type selector — always visible on screen.
// Polls input position every frame; if inside zoneRect, left half = Linear, right half = Fan.
// Works on both PC (mouse) and mobile (first touch). Not affected by timeScale.
//
// Inspector setup:
//   - Assign zoneRect to the zone panel's RectTransform (Screen Space - Overlay canvas)
//   - Optionally assign linearHighlight / fanHighlight Image for visual feedback
public class AttackTypeSelector : MonoBehaviour
{
    public static AttackType Selected { get; private set; } = AttackType.Linear;

    // Zone is always present — never blocks gameplay or pauses timeScale
    public static bool IsSelecting => false;

    [SerializeField] private RectTransform zoneRect;
    [SerializeField] private UnityEngine.UI.Image linearHighlight;
    [SerializeField] private UnityEngine.UI.Image fanHighlight;

    private void Start() => RefreshHighlights();

    private void Update()
    {
        if (zoneRect == null) return;

        Vector2 pos = Input.touchCount > 0
            ? Input.GetTouch(0).position
            : (Vector2)Input.mousePosition;

        if (!RectTransformUtility.RectangleContainsScreenPoint(zoneRect, pos, null)) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(zoneRect, pos, null, out Vector2 local);
        Select(local.x >= 0f ? AttackType.Fan : AttackType.Linear);
    }

    private void Select(AttackType type)
    {
        if (Selected == type) return;
        Selected = type;
        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        if (linearHighlight != null)
            linearHighlight.color = Selected == AttackType.Linear ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        if (fanHighlight != null)
            fanHighlight.color = Selected == AttackType.Fan ? Color.white : new Color(1f, 1f, 1f, 0.35f);
    }
}
