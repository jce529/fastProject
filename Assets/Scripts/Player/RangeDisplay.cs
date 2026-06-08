using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ATCK-02 (range display): Renders the attack range during slow-motion.
///
/// Linear mode (D-01): Two LineRenderer beams, one left, one right of the player.
/// Fan mode (D-02): One LineRenderer wireframe arc facing the player's current direction.
/// Default color: Yellow (D-03). Switches to Red on the nearest detected enemy (D-04).
///
/// Show() / Hide() are called by CombatController.EnterSlowMotion / ExitSlowMotion.
/// Does not own enemy detection — CombatController calls ClearHighlight/color directly.
/// </summary>
public class RangeDisplay : MonoBehaviour
{
    // -- Linear beam settings (D-01, D-05) -----------------------------------------
    [SerializeField] private float lineWidth = 0.12f;  // beam thickness

    // -- Fan arc settings (D-02, D-05) -----------------------------------------------
    [SerializeField] private int   arcSegments     = 24;   // 24 points — smooth at 1080p

    // -- Colors (D-03, D-04) ----------------------------------------------------------
    private static readonly Color ColorDefault  = Color.yellow;
    private static readonly Color ColorTargeted = Color.red;

    // -- Circle display settings -------------------------------------------------------
    [SerializeField] private int circleSegments = 36;  // 10-degree steps — smooth enough

    // -- LineRenderer references (assign in Inspector after creating child objects) ---
    [SerializeField] private LineRenderer _leftBeam;   // Linear mode — left ray
    [SerializeField] private LineRenderer _rightBeam;  // Linear mode — right ray
    [SerializeField] private LineRenderer _arcLine;    // Fan mode — wireframe arc
    [SerializeField] private LineRenderer _rangeCircle; // Max range circle — both modes

    // -- CombatController reference (single source of truth for range values) ----------
    private CombatController _combat;

    private SpriteRenderer _playerSprite;
    private bool _isShown;

    private void Awake()
    {
        _combat = GetComponentInParent<CombatController>();

        // Player sprite is on the parent (Player GameObject)
        _playerSprite = GetComponentInParent<SpriteRenderer>();

        // Start hidden — no display until slow-motion is active
        SetAllRenderers(false);
    }

    // -- Public API (called by CombatController) ------------------------------------

    /// <summary>Activate the range display for the current attack type.</summary>
    public void Show()
    {
        _isShown = true;
        bool isLinear = AttackTypeSelector.Selected == AttackType.Linear;

        if (_leftBeam    != null) _leftBeam.enabled    = isLinear;
        if (_rightBeam   != null) _rightBeam.enabled   = false;   // single-beam mode — right beam unused
        if (_arcLine     != null) _arcLine.enabled     = !isLinear;
        if (_rangeCircle != null) _rangeCircle.enabled = true;
    }

    /// <summary>Deactivate the range display and reset all line colors to default.</summary>
    public void Hide()
    {
        _isShown = false;
        SetAllRenderers(false);
        ResetColors();
    }

    // -- Update (only runs while shown — early-out otherwise) -----------------------

    private void Update()
    {
        if (!_isShown) return;

        if (AttackTypeSelector.Selected == AttackType.Linear)
            UpdateLinearDisplay();
        else
            UpdateFanDisplay();

        UpdateCircleDisplay();
    }

    // -- Linear display (D-01) -------------------------------------------------------

    private void UpdateLinearDisplay()
    {
        if (_leftBeam == null) return;

        Vector2 origin = transform.position;

        // Mouse direction in world space (new Input System)
        Vector2 mouseScreen = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Camera.main.WorldToScreenPoint(origin);
        Vector3 mouseScreen3 = new Vector3(mouseScreen.x, mouseScreen.y,
            Mathf.Abs(Camera.main.transform.position.z));
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen3);
        Vector2 dir = (mouseWorld - origin);
        if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;
        dir.Normalize();

        _leftBeam.positionCount = 2;
        _leftBeam.SetPosition(0, origin);
        _leftBeam.SetPosition(1, origin + dir * _combat.SearchRadius);
        _leftBeam.startWidth = _leftBeam.endWidth = lineWidth;
        _leftBeam.startColor = _leftBeam.endColor = ColorDefault;
    }

    // -- Fan display (D-02) ----------------------------------------------------------

    private void UpdateFanDisplay()
    {
        if (_arcLine == null) return;

        Vector2 origin = transform.position;

        Vector2 mouseScreen = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : (Vector2)Camera.main.WorldToScreenPoint(origin);
        Vector3 mouseScreen3 = new Vector3(mouseScreen.x, mouseScreen.y, Mathf.Abs(Camera.main.transform.position.z));
        Vector2 mouseDir = (Vector2)Camera.main.ScreenToWorldPoint(mouseScreen3) - origin;
        if (mouseDir.sqrMagnitude < 0.001f) mouseDir = Vector2.right;
        mouseDir.Normalize();
        float baseAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

        _arcLine.positionCount = arcSegments + 3;
        _arcLine.SetPosition(0, origin);                          // center start
        for (int i = 0; i <= arcSegments; i++)
        {
            float t     = (float)i / arcSegments;
            float angle = (baseAngle - _combat.FanHalfAngleDeg + t * _combat.FanHalfAngleDeg * 2f) * Mathf.Deg2Rad;
            Vector2 pt  = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _combat.FanRadius;
            _arcLine.SetPosition(i + 1, pt);                      // arc points at 1..arcSegments+1
        }
        _arcLine.SetPosition(arcSegments + 2, origin);            // center end — closes sector
        _arcLine.startWidth = _arcLine.endWidth = lineWidth;
        _arcLine.startColor = _arcLine.endColor = ColorDefault;
    }

    // -- Circle display ---------------------------------------------------------------

    private void UpdateCircleDisplay()
    {
        if (_rangeCircle == null) return;

        float radius = AttackTypeSelector.Selected == AttackType.Linear ? _combat.SearchRadius : _combat.FanRadius;
        Vector2 origin = transform.position;

        _rangeCircle.loop = true;
        _rangeCircle.positionCount = circleSegments;
        for (int i = 0; i < circleSegments; i++)
        {
            float angle = (float)i / circleSegments * Mathf.PI * 2f;
            _rangeCircle.SetPosition(i, origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
        }
        _rangeCircle.startWidth = _rangeCircle.endWidth = lineWidth;
        _rangeCircle.startColor = _rangeCircle.endColor = ColorDefault;
    }

    // -- Enemy highlight (D-04) — called by CombatController when nearest changes ---

    /// <summary>
    /// Highlight the targeted enemy's sprite red.
    /// Called by CombatController.FindNearestEnemyInRange when a new nearest is found.
    /// </summary>
    public void HighlightEnemy(SpriteRenderer enemySprite)
    {
        if (enemySprite != null)
            enemySprite.color = ColorTargeted;
    }

    // -- Helpers --------------------------------------------------------------------

    private void SetAllRenderers(bool enabled)
    {
        if (_leftBeam    != null) _leftBeam.enabled    = enabled;
        if (_rightBeam   != null) _rightBeam.enabled   = enabled;
        if (_arcLine     != null) _arcLine.enabled     = enabled;
        if (_rangeCircle != null) _rangeCircle.enabled = enabled;
    }

    private void ResetColors()
    {
        if (_leftBeam    != null) { _leftBeam.startColor    = _leftBeam.endColor    = ColorDefault; }
        if (_rightBeam   != null) { _rightBeam.startColor   = _rightBeam.endColor   = ColorDefault; }
        if (_arcLine     != null) { _arcLine.startColor     = _arcLine.endColor     = ColorDefault; }
        if (_rangeCircle != null) { _rangeCircle.startColor = _rangeCircle.endColor = ColorDefault; }
    }
}
