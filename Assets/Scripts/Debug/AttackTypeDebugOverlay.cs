// ============================================================
// DEBUG ONLY — Remove before release build
// AttackTypeSelector.Selected 변경을 화면에 표시한다.
// ============================================================

using UnityEngine;

/// <summary>
/// Renders the current AttackType (Linear/Fan) in the top-left corner using OnGUI.
/// No Canvas or UI dependency required.
/// To remove: disable or destroy this component.
/// </summary>
public class AttackTypeDebugOverlay : MonoBehaviour
{
    private AttackType _lastType;

    private void Update()
    {
        AttackType current = AttackTypeSelector.Selected;
        if (current != _lastType)
        {
            Debug.Log($"[AttackTypeDebug] Type changed → {current}");
            _lastType = current;
        }
    }

    private void OnGUI()
    {
        AttackType current = AttackTypeSelector.Selected;

        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 22;
        style.normal.textColor = current == AttackType.Linear ? Color.cyan : Color.yellow;

        GUI.Label(new Rect(10, 10, 400, 40), $"[DEBUG] Attack Type: {current}", style);
    }
}
