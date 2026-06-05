using System.Collections;
using UnityEngine;

/// <summary>
/// ATCK-02, ATCK-03, ATCK-04, FEEL-01: Core combat state machine.
///
/// Owns the slow-motion lifecycle (enter on Attack held, exit on release or gauge empty),
/// dash to nearest enemy (MovePosition over 3 FixedUpdate frames), whiff branch,
/// hit-freeze sequence, and _isBusy lockout to prevent re-entrance.
///
/// Does NOT own the gauge — GaugeController handles drain/regen.
/// Does NOT own range display — RangeDisplay (Plan 02-03) handles visual feedback.
///
/// Review fixes applied:
///   [HIGH]   Obstacle linecast: ExecuteDash linecast-checks the path before dashing;
///            blocked path converts the attack to a whiff.
///   [MEDIUM] Slow-mo timeout: maxSlowMoDuration safety timer prevents infinite slow-mo
///            if Input System drops the release event.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InvincibilityHandler))]
[RequireComponent(typeof(GaugeController))]
public class CombatController : MonoBehaviour
{
    // -- Tunable values (expose for playtest tuning) --------------------------------
    [SerializeField] private float slowTimeScale      = 0.2f;   // Claude's discretion: 0.15-0.25x range
    [SerializeField] private float dashDurationFrames = 3f;     // MovePosition spread over N FixedUpdate frames
    [SerializeField] private float hitFreezeDuration  = 0.075f; // 75ms — midpoint of 50-100ms FEEL-01 range
    [SerializeField] private float postKillLockout    = 0.2f;   // real seconds after kill before control returns
    [SerializeField] private float whiffLockout       = 0.5f;   // real seconds — must be > postKillLockout (ATCK-04)
    [SerializeField] private float searchRadius       = 10f;    // OverlapCircle radius — covers linear beam length
    [SerializeField] private float fanHalfAngleDeg    = 55f;    // half of 110-degree fan arc
    /// <summary>
    /// [MEDIUM — Gemini] Safety timeout: if slow-mo stays active this many real seconds
    /// without a release event (e.g. Input System dropped the event), force-exit slow-motion.
    /// 5 seconds is long enough to not interfere with legitimate holds, but short enough
    /// to avoid a stuck state during playtesting.
    /// </summary>
    [SerializeField] private float maxSlowMoDuration  = 5f;

    // -- Component references -------------------------------------------------------
    private Rigidbody2D          _rb;
    private InvincibilityHandler _invincibilityHandler;
    private GaugeController      _gauge;
    private SpriteRenderer       _spriteRenderer;
    private TrailRenderer        _trailRenderer; // assigned from child in Awake

    // RangeDisplay reference — found via GetComponentInChildren in Start
    private RangeDisplay _rangeDisplay;

    // -- State ----------------------------------------------------------------------
    private bool  _isBusy;    // Prevents re-entrance during dash/whiff/lockout coroutines
    private bool  _isSlowMo;
    private float _slowMoStartTime; // [MEDIUM — Gemini] unscaled timestamp when slow-mo began
    private bool  _slowMoCancelledByRoll; // true: 이번 슬로우모션이 Roll로 취소됨
    private float _attackCooldown;        // 처치 후 공격 재사용 대기 (unscaledDeltaTime)

    // -- Enemy detection buffer (pre-allocated — no GC per frame) ------------------
    private readonly Collider2D[] _hitBuffer = new Collider2D[16];
    private int _enemyLayerMask;
    /// <summary>[HIGH — Gemini] Layer mask for environment obstacles (Default layer = platforms/walls).</summary>
    private int _obstacleMask;

    // -- Enemy highlight tracking ---------------------------------------------------
    private DummyEnemy _lastHighlighted;

    private void Awake()
    {
        _rb                   = GetComponent<Rigidbody2D>();
        _invincibilityHandler = GetComponent<InvincibilityHandler>();
        _gauge                = GetComponent<GaugeController>();
        _spriteRenderer       = GetComponent<SpriteRenderer>();
        _trailRenderer        = GetComponentInChildren<TrailRenderer>();

        // Cache layer masks once in Awake — avoid NameToLayer in Update (ROADMAP constraint)
        _enemyLayerMask = LayerMask.GetMask("Enemy");
        // [HIGH — Gemini] Default layer contains platforms and walls for the obstacle linecast.
        _obstacleMask   = LayerMask.GetMask("Default");
    }

    private void Start()
    {
        // RangeDisplay is on a child GameObject — find it after all Awake() calls
        _rangeDisplay = GetComponentInChildren<RangeDisplay>();
    }

    private void Update()
    {
        // _isBusy lockout: all attack state transitions blocked during dash/whiff/lockout
        if (_isBusy) return;

        var input = InputManager.Instance;

        // 공격 쿨다운 카운트다운 (처치 후 공격만 제한, 이동은 자유)
        if (_attackCooldown > 0f)
            _attackCooldown -= Time.unscaledDeltaTime;

        // Roll 입력이 있으면 슬로우모션 취소 (대시는 발동하지 않음)
        if (_isSlowMo && input.RollPressed)
        {
            ExitSlowMotion();
            _slowMoCancelledByRoll = true;
            return;
        }

        // Gauge drains every frame Attack is held (uses unscaledDeltaTime internally)
        _gauge.SetDraining(input.IsAttackDown && _attackCooldown <= 0f);

        // Enter slow-motion on the frame Attack button is first pressed
        if (input.AttackHeld && !_isSlowMo && _attackCooldown <= 0f)
            EnterSlowMotion();

        // [MEDIUM — Gemini] Safety timeout: force-exit slow-mo if it has lasted longer
        // than maxSlowMoDuration real seconds, regardless of input state.
        // Prevents stuck state when Input System drops AttackReleased event.
        if (_isSlowMo && Time.unscaledTime > _slowMoStartTime + maxSlowMoDuration)
            ExitSlowMotion();

        // Gauge-empty auto-exit: slow-mo ends but player can still release to dash
        if (_isSlowMo && _gauge.IsEmpty)
            ExitSlowMotion();

        // Exit slow-motion if no longer holding (e.g. quick tap without release event)
        if (_isSlowMo && !input.IsAttackDown && !input.AttackReleased)
            ExitSlowMotion();

        // Release event: start dash or whiff
        if (input.AttackReleased)
        {
            if (_isSlowMo)
                ExitSlowMotion();
            // Roll로 슬로우모션이 취소된 경우 대시/whiff 발동 안 함
            if (_slowMoCancelledByRoll)
            {
                _slowMoCancelledByRoll = false;
                return;
            }
            StartCoroutine(DashOrWhiff());
        }
    }

    // -- Slow-motion ---------------------------------------------------------------

    /// <summary>
    /// Enter slow-motion. ALWAYS sets both timeScale AND fixedDeltaTime (ROADMAP Stack Constraint).
    /// Records _slowMoStartTime for the safety timeout check.
    /// </summary>
    private void EnterSlowMotion()
    {
        Time.timeScale      = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // always paired
        _isSlowMo           = true;
        _slowMoStartTime    = Time.unscaledTime; // [MEDIUM — Gemini] record entry time
        _rangeDisplay?.Show();
    }

    /// <summary>
    /// Exit slow-motion. ALWAYS restores both timeScale AND fixedDeltaTime.
    /// Called BEFORE dash yields so MovePosition moves at full speed.
    /// </summary>
    private void ExitSlowMotion()
    {
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f; // restore standard fixed step
        _isSlowMo           = false;
        _rangeDisplay?.Hide();

        // Clear enemy highlight on exit
        if (_lastHighlighted != null)
        {
            _lastHighlighted.ClearHighlight();
            _lastHighlighted = null;
        }
    }

    // -- Combat coroutine chain ----------------------------------------------------

    private IEnumerator DashOrWhiff()
    {
        // Set _isBusy BEFORE any yield — prevents double activation (Pitfall 4)
        _isBusy = true;

        var target = FindNearestEnemyInRange();
        if (target != null)
            yield return StartCoroutine(ExecuteDash(target));
        else
            yield return StartCoroutine(ExecuteWhiff());

        _isBusy = false;
    }

    private IEnumerator ExecuteDash(DummyEnemy target)
    {
        // 1. ExitSlowMotion BEFORE first yield — ensures MovePosition runs at timeScale=1
        //    (Pitfall 1: if still in slow-mo, MovePosition moves 5x slower than intended)
        ExitSlowMotion();

        // 2. [HIGH — Gemini] Obstacle check: linecast from player to target.
        //    If a Default-layer collider (platform/wall) blocks the path, the attack whiffs
        //    instead of producing a broken partial dash where the player stops mid-air.
        //    This is a prototype — a single linecast against the Default layer is sufficient.
        Vector2 startPos    = _rb.position;
        Vector2 destination = (Vector2)target.transform.position;
        RaycastHit2D obstacleHit = Physics2D.Linecast(startPos, destination, _obstacleMask);
        if (obstacleHit.collider != null)
        {
            // Path blocked — whiff instead. Do NOT start a partial dash.
            yield return StartCoroutine(ExecuteWhiff());
            yield break;
        }

        // 3. Grant i-frames for the dash window
        _invincibilityHandler.StartInvincibility(0.2f);

        // 4. Enable trail visual (D-06)
        if (_trailRenderer != null) _trailRenderer.emitting = true;

        float dashDuration = dashDurationFrames * Time.fixedDeltaTime; // 3 * 0.02 = 0.06s

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            float t = elapsed / dashDuration;
            _rb.MovePosition(Vector2.Lerp(startPos, destination, t));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        // Snap to exact destination to avoid sub-pixel drift
        _rb.MovePosition(destination);

        // 5. Disable trail
        if (_trailRenderer != null) _trailRenderer.emitting = false;

        // 6. Kill the enemy BEFORE hit-freeze (freeze is the punctuation of the kill)
        target.OnDashHit();

        // 7. Hit-freeze: timeScale=0 for hitFreezeDuration real seconds (FEEL-01)
        yield return StartCoroutine(HitFreeze(hitFreezeDuration));

        // 8. Post-kill cooldown: 공격만 제한, 이동은 자유 (WaitForSecondsRealtime 제거)
        _attackCooldown = postKillLockout;

        // 9. Partial gauge recovery on kill (ATCK-05)
        _gauge.AddKillBonus();
    }

    private IEnumerator ExecuteWhiff()
    {
        // Trigger whiff animation (Animator must have "Whiff" trigger — set up in scene)
        var animator = GetComponent<Animator>();
        if (animator != null) animator.SetTrigger("Whiff");

        // Longer lockout than kill — ATCK-04: whiff penalty must be clearly longer
        yield return new WaitForSecondsRealtime(whiffLockout);
    }

    private IEnumerator HitFreeze(float realSeconds)
    {
        // FEEL-01: world freeze. Both timeScale AND fixedDeltaTime must be zeroed.
        Time.timeScale      = 0f;
        Time.fixedDeltaTime = 0f;
        // WaitForSecondsRealtime is mandatory — WaitForSeconds never resumes when timeScale=0 (Pitfall 2)
        yield return new WaitForSecondsRealtime(realSeconds);
        // Restore both — forgetting fixedDeltaTime causes physics to stop permanently (Pitfall 5)
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
    }

    // -- Enemy detection -----------------------------------------------------------

    private DummyEnemy FindNearestEnemyInRange()
    {
        // Pre-allocated buffer — no GC (ROADMAP Stack Constraint)
        int count = Physics2D.OverlapCircleNonAlloc(
            transform.position, searchRadius, _hitBuffer, _enemyLayerMask);

        DummyEnemy nearest   = null;
        float      bestSqDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var dummy = _hitBuffer[i].GetComponent<DummyEnemy>();
            // Skip dead enemies — physics broadphase may lag behind collider.enabled=false (Pitfall 6)
            if (dummy == null || !dummy.IsAlive) continue;
            // Shape filter: linear accepts all in radius; fan checks angle
            if (!IsInAttackShape((Vector2)_hitBuffer[i].transform.position)) continue;

            // SqrMagnitude avoids sqrt — sufficient for closest-enemy comparison
            float sqDist = ((Vector2)_hitBuffer[i].transform.position
                           - (Vector2)transform.position).sqrMagnitude;
            if (sqDist < bestSqDist)
            {
                bestSqDist = sqDist;
                nearest    = dummy;
            }
        }

        // Update enemy highlight (D-04): red on nearest, clear previous
        if (nearest != _lastHighlighted)
        {
            if (_lastHighlighted != null) _lastHighlighted.ClearHighlight();
            if (nearest != null)
            {
                var sr = nearest.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.red;
            }
            _lastHighlighted = nearest;
        }

        return nearest;
    }

    /// <summary>
    /// Fan shape angle filter. Linear mode: always true (radius handles range).
    /// Fan mode: checks that the target is within the forward-facing arc.
    /// </summary>
    private bool IsInAttackShape(Vector2 targetPos)
    {
        if (AttackTypeSelector.Selected == AttackType.Linear) return true;

        Vector2 toTarget = (targetPos - (Vector2)transform.position).normalized;
        Vector2 facing   = _spriteRenderer.flipX ? Vector2.left : Vector2.right;
        float   dot      = Vector2.Dot(facing, toTarget);
        float   cosHalf  = Mathf.Cos(fanHalfAngleDeg * Mathf.Deg2Rad);
        return dot >= cosHalf;
    }
}
