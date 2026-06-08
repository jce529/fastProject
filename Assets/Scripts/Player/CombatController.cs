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
    [SerializeField] private float dashDuration       = 0.15f;  // 대시 이동 시간 (초)
    [SerializeField] private float hitFreezeDuration  = 0.075f; // 75ms — midpoint of 50-100ms FEEL-01 range
    [SerializeField] private float postKillLockout    = 0.2f;   // real seconds after kill before control returns
    [SerializeField] private float whiffLockout       = 0.5f;   // real seconds — must be > postKillLockout (ATCK-04)
    [SerializeField] private float searchRadius       = 10f;    // OverlapCircle radius — covers linear beam length
    [SerializeField] private float fanRadius          = 4f;     // [NEW] radius for fan detection (matches RangeDisplay)
    [SerializeField] private float fanHalfAngleDeg    = 35f;    // half of 70-degree fan arc
    [SerializeField] private float linearHalfAngleDeg = 30f;    // [UPDATED] 15 -> 30 for more forgiving aim
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
    private TrailRenderer        _trailRenderer;
    private Camera               _mainCamera;
    private Animator             _animator;

    // RangeDisplay reference — found via GetComponentInChildren in Start
    private RangeDisplay _rangeDisplay;

    // -- State ----------------------------------------------------------------------
    private bool  _isBusy;    // Prevents re-entrance during dash/whiff/lockout coroutines
    private bool  _isSlowMo;
    private float _slowMoStartTime; // [MEDIUM — Gemini] unscaled timestamp when slow-mo began
    private bool  _slowMoCancelledByRoll; // true: 이번 슬로우모션이 Roll로 취소됨
    private float _attackCooldown;        // 처치 후 공격 재사용 대기 (unscaledDeltaTime)

    // -- Range display accessors (RangeDisplay reads these — single source of truth) ----
    public float FanRadius       => fanRadius;
    public float FanHalfAngleDeg => fanHalfAngleDeg;
    public float SearchRadius    => searchRadius;

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
        _mainCamera           = Camera.main;
        _animator             = GetComponent<Animator>();

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

        // 슬로우모션 유지 중 — 가장 가까운 적 하이라이트 갱신 (D-04)
        if (_isSlowMo && !_isBusy)
            UpdateHighlight(FindNearestEnemyInRange());

        // Gauge-empty auto-exit: slow-mo ends but player can still release to dash
        if (_isSlowMo && _gauge.IsEmpty)
            ExitSlowMotion();

        // Exit slow-motion if no longer holding (e.g. quick tap without release event)
        if (_isSlowMo && !input.IsAttackDown && !input.AttackReleased)
            ExitSlowMotion();

        // Release event: start dash or whiff
        if (input.AttackReleased)
        {
            Debug.Log("[Combat] Attack released. Checking dash condition.");
            // ExitSlowMotion이 _lastHighlighted를 지우기 전에 캐시
            DummyEnemy cachedTarget = _lastHighlighted;
            if (_isSlowMo)
                ExitSlowMotion();
            // Roll로 슬로우모션이 취소된 경우 대시/whiff 발동 안 함
            if (_slowMoCancelledByRoll)
            {
                Debug.Log("[Combat] Attack released but slowMo was cancelled by roll. Ignored.");
                _slowMoCancelledByRoll = false;
                return;
            }
            StartCoroutine(DashOrWhiff(cachedTarget));
        }
    }

    // -- Slow-motion ---------------------------------------------------------------

    /// <summary>
    /// Enter slow-motion. ALWAYS sets both timeScale AND fixedDeltaTime (ROADMAP Stack Constraint).
    /// Records _slowMoStartTime for the safety timeout check.
    /// </summary>
    private void EnterSlowMotion()
    {
        if (_isSlowMo) return; // Prevent double entry
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
        if (!_isSlowMo) return; // Safely handle multiple calls
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f; // restore standard fixed step
        _isSlowMo           = false;
        
        if (_rangeDisplay != null)
            _rangeDisplay.Hide();

        // Clear enemy highlight on exit
        if (_lastHighlighted != null)
        {
            _lastHighlighted.ClearHighlight();
            _lastHighlighted = null;
        }
    }

    // -- Combat coroutine chain ----------------------------------------------------

    private IEnumerator DashOrWhiff(DummyEnemy cachedTarget = null)
    {
        Debug.Log("[Combat] Coroutine: DashOrWhiff started.");
        _isBusy = true;
        _animator?.SetBool("IsAttacking", true);

        // 하이라이트된 적을 우선 사용, 없으면 재탐색
        var target = (cachedTarget != null && cachedTarget.IsAlive)
            ? cachedTarget
            : FindNearestEnemyInRange();
        if (target != null)
        {
            Debug.Log($"[Combat] DashOrWhiff: Target '{target.name}' confirmed. Jumping to ExecuteDash.");
            yield return ExecuteDash(target);
            Debug.Log("[Combat] DashOrWhiff: ExecuteDash yield returned.");
        }
        else
        {
            Debug.Log("[Combat] DashOrWhiff: No target. Jumping to ExecuteWhiff.");
            yield return ExecuteWhiff();
        }

        _animator?.SetBool("IsAttacking", false);
        Debug.Log("[Combat] DashOrWhiff: Setting _isBusy = false and exiting.");
        _isBusy = false;
    }

    private IEnumerator ExecuteDash(DummyEnemy target)
    {
        Debug.Log("[Combat] ExecuteDash: ENTRANCE - Coroutine effectively started.");
        
        if (target == null) {
            Debug.LogError("[Combat] ExecuteDash: TARGET IS NULL at start! Aborting.");
            yield break;
        }

        // 1. ExitSlowMotion BEFORE first yield — ensures MovePosition runs at timeScale=1
        Debug.Log("[Combat] ExecuteDash: Calling ExitSlowMotion.");
        ExitSlowMotion();
        Debug.Log("[Combat] ExecuteDash: ExitSlowMotion finished.");

        Vector2 startPos    = _rb.position;
        Vector2 destination = (Vector2)target.transform.position;
        Vector2 dirToTarget = (destination - startPos).normalized;

        Debug.Log($"[Combat] ExecuteDash: StartPos={startPos}, Dest={destination}, Dir={dirToTarget}");

        Debug.Log("[Combat] ExecuteDash: Path clear. Preparing movement.");

        // 3. 대상 방향으로 스프라이트 전환
        _spriteRenderer.flipX = destination.x < startPos.x;

        // 4. Setup visual, animation, invincibility
        _animator?.SetBool("IsDashing", true);
        _invincibilityHandler.StartInvincibility(dashDuration + 0.05f);
        if (_trailRenderer != null) _trailRenderer.emitting = true;
        _rb.linearVelocity = Vector2.zero;

        // 5. 대시 이동 (smoothstep 보간으로 가속-감속 느낌)
        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            float t = elapsed / dashDuration;
            float smooth = t * t * (3f - 2f * t); // smoothstep
            _rb.MovePosition(Vector2.Lerp(startPos, destination, smooth));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        _rb.MovePosition(destination);

        Debug.Log("[Combat] ExecuteDash: Arrived at target");

        // 6. Cleanup visual and animation
        _animator?.SetBool("IsDashing", false);
        if (_trailRenderer != null) _trailRenderer.emitting = false;

        // 6. Kill and effects
        target.OnDashHit();
        yield return StartCoroutine(HitFreeze(hitFreezeDuration));

        _attackCooldown = postKillLockout;
        _gauge.AddKillBonus();
        Debug.Log("[Combat] ExecuteDash: Sequence Complete");
    }

    private IEnumerator ExecuteWhiff()
    {
        Debug.Log("[Combat] Executing Whiff (Penalty)");
        _animator?.SetTrigger("Whiff");

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
        Vector2 origin = (Vector2)transform.position;
        Vector2 attackDir = Vector2.right;
        float currentMaxDist = searchRadius;

        // [D-01/D-02 Integration] Calculate direction and specific radius once per search
        if (AttackTypeSelector.Selected == AttackType.Linear)
        {
            // Linear attack uses mouse-driven direction (matches RangeDisplay)
            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
            Vector2 mousePos = mouse != null ? mouse.position.ReadValue() : (Vector2)_mainCamera.WorldToScreenPoint(origin);
            Vector3 mouseWorld = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Mathf.Abs(_mainCamera.transform.position.z)));
            attackDir = ((Vector2)mouseWorld - origin).normalized;
            currentMaxDist = searchRadius; // matches linearLength=10
        }
        else
        {
            // Fan attack uses mouse-driven direction (matches RangeDisplay)
            UnityEngine.InputSystem.Mouse mouse = UnityEngine.InputSystem.Mouse.current;
            Vector2 mousePos2 = mouse != null ? mouse.position.ReadValue() : (Vector2)_mainCamera.WorldToScreenPoint(origin);
            Vector3 mouseWorld2 = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos2.x, mousePos2.y, Mathf.Abs(_mainCamera.transform.position.z)));
            Vector2 dir = (Vector2)mouseWorld2 - origin;
            attackDir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.right;
            currentMaxDist = fanRadius;
        }

        // Pre-allocated buffer — no GC (ROADMAP Stack Constraint)
        int count = Physics2D.OverlapCircleNonAlloc(origin, searchRadius, _hitBuffer, _enemyLayerMask);

        DummyEnemy nearest   = null;
        float      bestSqDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var dummy = _hitBuffer[i].GetComponent<DummyEnemy>();
            // Skip dead enemies — physics broadphase may lag behind collider.enabled=false (Pitfall 6)
            if (dummy == null || !dummy.IsAlive) continue;

            Vector2 targetPos = (Vector2)_hitBuffer[i].transform.position;
            Vector2 toTarget = targetPos - origin;
            float dist = toTarget.magnitude;

            // Shape and distance filter: checks specific arc/beam and radius
            if (!IsInAttackShape(toTarget / dist, dist, attackDir, currentMaxDist)) continue;

            // SqrMagnitude avoids sqrt — sufficient for closest-enemy comparison
            float sqDist = dist * dist;
            if (sqDist < bestSqDist)
            {
                bestSqDist = sqDist;
                nearest    = dummy;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Update enemy highlight (D-04): red on nearest, clear previous.
    /// Called only from Update() while _isSlowMo is true — ensures highlight
    /// persists until ExitSlowMotion() clears it, preventing one-frame flash.
    /// </summary>
    private void UpdateHighlight(DummyEnemy nearest)
    {
        if (nearest == _lastHighlighted) return;
        if (_lastHighlighted != null) _lastHighlighted.ClearHighlight();
        if (nearest != null)
        {
            var sr = nearest.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.red;
        }
        _lastHighlighted = nearest;
    }

    /// <summary>
    /// Filter enemies by attack shape and distance.
    /// Linear mode: checks narrow beam towards aim.
    /// Fan mode: checks forward-facing arc and fan radius.
    /// </summary>
    private bool IsInAttackShape(Vector2 normalizedToTarget, float distance, Vector2 attackDir, float maxDistance)
    {
        if (distance > maxDistance) return false;

        float dot = Vector2.Dot(attackDir, normalizedToTarget);
        float thresholdAngle = (AttackTypeSelector.Selected == AttackType.Linear) ? linearHalfAngleDeg : fanHalfAngleDeg;
        float cosHalf = Mathf.Cos(thresholdAngle * Mathf.Deg2Rad);
        
        return dot >= cosHalf;
    }
}
