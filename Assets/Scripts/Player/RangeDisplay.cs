using UnityEngine;

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
    [SerializeField] private float linearLength = 10f;  // Claude's discretion: 10 units covers typical room width

    // -- Fan arc settings (D-02, D-05) -----------------------------------------------
    [SerializeField] private float fanRadius       = 7f;   // Claude's discretion: 7 units
    [SerializeField] private float fanHalfAngleDeg = 55f;  // half of 110-degree arc (matches CombatController)
    [SerializeField] private int   arcSegments     = 24;   // 24 points — smooth at 1080p

    // -- Colors (D-03, D-04) ----------------------------------------------------------
    private static readonly Color ColorDefault  = Color.yellow;
    private static readonly Color ColorTargeted = Color.red;

    // -- LineRenderer references (assign in Inspector after creating child objects) ---
    [SerializeField] private LineRenderer _leftBeam;   // Linear mode — left ray
    [SerializeField] private LineRenderer _rightBeam;  // Linear mode — right ray
    [SerializeField] private LineRenderer _arcLine;    // Fan mode — wireframe arc

    private SpriteRenderer _playerSprite;
    private bool _isShown;

    private void Awake()
    {
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

        if (_leftBeam  != null) _leftBeam.enabled  = isLinear;
        if (_rightBeam != null) _rightBeam.enabled  = isLinear;
        if (_arcLine   != null) _arcLine.enabled    = !isLinear;
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
    }

    // -- Linear display (D-01) -------------------------------------------------------

    private void UpdateLinearDisplay()
    {
        Vector2 origin = transform.position;

        // Linear fires both directions regardless of facing (D-01 spec)
        if (_leftBeam != null)
        {
            _leftBeam.positionCount = 2;
            _leftBeam.SetPosition(0, origin);
            _leftBeam.SetPosition(1, origin + Vector2.left  * linearLength);
            _leftBeam.startColor = _leftBeam.endColor = ColorDefault;
        }

        if (_rightBeam != null)
        {
            _rightBeam.positionCount = 2;
            _rightBeam.SetPosition(0, origin);
            _rightBeam.SetPosition(1, origin + Vector2.right * linearLength);
            _rightBeam.startColor = _rightBeam.endColor = ColorDefault;
        }
    }

    // -- Fan display (D-02) ----------------------------------------------------------

    private void UpdateFanDisplay()
    {
        if (_arcLine == null) return;

        Vector2 facing    = (_playerSprite != null && _playerSprite.flipX) ? Vector2.left : Vector2.right;
        float   baseAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

        _arcLine.positionCount = arcSegments + 1;
        for (int i = 0; i <= arcSegments; i++)
        {
            float t     = (float)i / arcSegments;
            float angle = (baseAngle - fanHalfAngleDeg + t * fanHalfAngleDeg * 2f) * Mathf.Deg2Rad;
            Vector2 pt  = (Vector2)transform.position
                        + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * fanRadius;
            _arcLine.SetPosition(i, pt);
        }

        _arcLine.startColor = _arcLine.endColor = ColorDefault;
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
        if (_leftBeam  != null) _leftBeam.enabled  = enabled;
        if (_rightBeam != null) _rightBeam.enabled  = enabled;
        if (_arcLine   != null) _arcLine.enabled    = enabled;
    }

    private void ResetColors()
    {
        if (_leftBeam  != null) { _leftBeam.startColor  = _leftBeam.endColor  = ColorDefault; }
        if (_rightBeam != null) { _rightBeam.startColor = _rightBeam.endColor = ColorDefault; }
        if (_arcLine   != null) { _arcLine.startColor   = _arcLine.endColor   = ColorDefault; }
    }
}
