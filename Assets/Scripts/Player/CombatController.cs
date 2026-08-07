using System.Collections;
using UnityEngine;

/// <summary>
/// ATCK-02, ATCK-03, ATCK-04, FEEL-01: Core combat state machine.
///
/// Owns the slow-motion lifecycle (enter on Attack held, exit on release or gauge empty)
/// and the _isBusy lockout to prevent re-entrance. Targeting and dash/whiff resolution are
/// delegated to _activeModule (IPlayerCombatModule — Phase 18 INFRA-01), which currently
/// hosts OverclockModule, the F.I.O.R.A combat logic.
///
/// Does NOT own the gauge — ChronoGaugeController handles drain/regen.
/// Does NOT own range display — RangeDisplay (Plan 02-03) handles visual feedback.
///
/// Review fixes applied:
///   [HIGH]   Obstacle linecast: the active module linecast-checks the path before dashing;
///            blocked path converts the attack to a whiff.
///   [MEDIUM] Slow-mo timeout: maxSlowMoDuration safety timer prevents infinite slow-mo
///            if Input System drops the release event.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(InvincibilityHandler))]
[RequireComponent(typeof(ChronoGaugeController))]
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
    [SerializeField] private GameObject _hitSparkPrefab;
    [SerializeField] private float _cameraShakeDuration = 0.15f;
    [SerializeField] private float _cameraShakeAmplitude = 0.2f;
    [SerializeField] private float swingRadius = 3f;         // D-01: 기본/사무라이 전투형 모듈 스윙 반경 (Overclock의 searchRadius/fanRadius와 독립)
    [SerializeField] private float swingHalfAngleDeg = 50f;  // D-01: 스윙 부채꼴 절반각
    [SerializeField] private float tapLockout = 0.12f;       // D-03: 탭 공격 사이 짧은 고정 락아웃

    // -- Component references -------------------------------------------------------
    [SerializeField] private PlayerController _player;
    private Rigidbody2D          _rb;
    private InvincibilityHandler _invincibilityHandler;
    private ChronoGaugeController _gauge;
    private SpriteRenderer       _spriteRenderer;
    private TrailRenderer        _trailRenderer;
    private Camera               _mainCamera;
    private Animator             _animator;
    private CameraFollow         _cameraFollow;

    // RangeDisplay reference — found via GetComponentInChildren in Start
    private RangeDisplay _rangeDisplay;

    // -- State ----------------------------------------------------------------------
    private bool  _isBusy;    // Prevents re-entrance during dash/whiff/lockout coroutines
    private bool  _isSlowMo;
    private float _slowMoStartTime; // [MEDIUM — Gemini] unscaled timestamp when slow-mo began
    private bool  _isAttackPending; // true: attack 버튼이 눌린 채 대시 대기 중
    private float _attackCooldown;        // 처치 후 공격 재사용 대기 (unscaledDeltaTime)
    // -- Range display accessors (RangeDisplay reads these — single source of truth) ----
    public float FanRadius       => fanRadius;
    public float FanHalfAngleDeg => fanHalfAngleDeg;
    public float SearchRadius    => searchRadius;

    // -- Enemy detection buffer (pre-allocated — no GC per frame) ------------------
    private readonly Collider2D[] _hitBuffer = new Collider2D[16];
    private int _enemyLayerMask;
    private ContactFilter2D _enemyFilter;
    /// <summary>[HIGH — Gemini] Layer mask for environment obstacles (Default layer = platforms/walls).</summary>
    private int _obstacleMask;

    // -- Enemy highlight tracking ---------------------------------------------------
    private IEnemy _lastHighlighted;

    // -- Combat module (Phase 18 INFRA-01) ------------------------------------------
    private IPlayerCombatModule _activeModule;
    private CombatContext _ctx;

    private void Awake()
    {
        _rb                   = GetComponent<Rigidbody2D>();
        _invincibilityHandler = GetComponent<InvincibilityHandler>();
        _gauge                = GetComponent<ChronoGaugeController>();
        _spriteRenderer       = GetComponent<SpriteRenderer>();
        _trailRenderer        = GetComponentInChildren<TrailRenderer>();
        if (_trailRenderer != null) ConfigureTrailVisuals(_trailRenderer);
        _mainCamera           = Camera.main;
        _cameraFollow         = _mainCamera != null ? _mainCamera.GetComponent<CameraFollow>() : null;
        _animator             = GetComponent<Animator>();

        // Cache layer masks once in Awake — avoid NameToLayer in Update (ROADMAP constraint)
        _enemyLayerMask = LayerMask.GetMask("Enemy");
        _enemyFilter.SetLayerMask(_enemyLayerMask);
        _enemyFilter.useTriggers = true;
        // [HIGH — Gemini] Default/Ground/Platform layers contain walls and floors for the obstacle linecast.
        // Enemy layer is deliberately excluded — an enemy standing behind another enemy must not block targeting.
        _obstacleMask   = LayerMask.GetMask("Default", "Ground", "Platform");

        _activeModule = BuildModule(CombatModuleSelector.SelectedModuleId);
        _ctx = new CombatContext
        {
            Rb                   = _rb,
            SpriteRenderer       = _spriteRenderer,
            Animator             = _animator,
            TrailRenderer        = _trailRenderer,
            Invincibility        = _invincibilityHandler,
            CameraFollow         = _cameraFollow,
            Gauge                = _gauge,
            MainCamera           = _mainCamera,
            HitSparkPrefab       = _hitSparkPrefab,
            DashDuration         = dashDuration,
            HitFreezeDuration    = hitFreezeDuration,
            PostKillLockout      = postKillLockout,
            WhiffLockout         = whiffLockout,
            CameraShakeDuration  = _cameraShakeDuration,
            CameraShakeAmplitude = _cameraShakeAmplitude,
            SearchRadius         = searchRadius,
            FanRadius            = fanRadius,
            FanHalfAngleDeg      = fanHalfAngleDeg,
            LinearHalfAngleDeg   = linearHalfAngleDeg,
            EnemyFilter          = _enemyFilter,
            HitBuffer            = _hitBuffer,
            ObstacleMask         = _obstacleMask,
            SwingRadius          = swingRadius,
            SwingHalfAngleDeg    = swingHalfAngleDeg,
            TapLockout           = tapLockout,
            SetAttackCooldown    = sec => _attackCooldown = sec,
        };
    }

    private IPlayerCombatModule BuildModule(CombatModuleId id)
    {
        switch (id)
        {
            case CombatModuleId.Overclock: return new OverclockModule();
            case CombatModuleId.Samurai:   return new SamuraiParryModule();
            case CombatModuleId.Basic:
            default:                       return new BasicCombatModule();
        }
    }

    /// <summary>D-18 대체 — 실제 로비 UI 없이 DebugScene에서 즉시 모듈을 바꿔 테스트하기 위한
    /// 디버그 전용 훅. DebugCombatModuleSwitcher가 호출한다. 정식 플로우(Awake 시점 결정)는
    /// 변경하지 않는다.</summary>
    public void DebugSetActiveModule(CombatModuleId id) => _activeModule = BuildModule(id);

    private void Start()
    {
        // RangeDisplay is on a child GameObject — find it after all Awake() calls
        _rangeDisplay = GetComponentInChildren<RangeDisplay>();
    }

    private void Update()
    {
        if (_player != null && _player.InputLocked) return;
        // _isBusy lockout: all attack state transitions blocked during dash/whiff/lockout
        if (_isBusy) return;

        // SAMURAI-02: 실시간 모듈은 Overclock의 hold-slowmo→release-resolve 상태머신을 완전히 우회한다.
        // OverclockModule은 IRealtimeCombatModule을 구현하지 않으므로 이 분기는 항상 false — Overclock
        // 경로는 아래 기존 로직으로 그대로 흘러간다(19-RESEARCH.md §1, zero behavior change).
        if (_activeModule is IRealtimeCombatModule realtimeModule)
        {
            realtimeModule.Tick(_ctx);
            return;
        }

        var input = InputManager.Instance;

        // 공격 쿨다운 카운트다운 (처치 후 공격만 제한, 이동은 자유)
        if (_attackCooldown > 0f)
            _attackCooldown -= Time.unscaledDeltaTime;

        // Roll 입력이 있으면 슬로우모션 취소 (대시는 발동하지 않음)
        if (_isAttackPending && input.RollPressed)
        {
            ExitSlowMotion();
            ExitAttackPending();
            return;
        }

        // Gauge drains every frame Attack is held (uses unscaledDeltaTime internally)
        bool drainCond = input.IsAttackDown && _isAttackPending && _attackCooldown <= 0f;
        _gauge.SetDraining(drainCond);

        // Enter slow-motion on the frame Attack button is first pressed
        if (input.AttackHeld && !_isSlowMo && !_isAttackPending && _attackCooldown <= 0f)
            EnterSlowMotion();

        // [MEDIUM — Gemini] Safety timeout: force-exit slow-mo if it has lasted longer
        // than maxSlowMoDuration real seconds, regardless of input state.
        // Prevents stuck state when Input System drops AttackReleased event.
        if (_isSlowMo && Time.unscaledTime > _slowMoStartTime + maxSlowMoDuration)
            ExitSlowMotion();

        // 공격 대기 중 — 가장 가까운 적 하이라이트 갱신 (D-04)
        if (_isAttackPending && !_isBusy)
            UpdateHighlight(_activeModule.FindTarget((Vector2)transform.position, _ctx));

        // Gauge-empty auto-exit: slow-mo ends but player can still release to dash
        if (_isSlowMo && _gauge.IsEmpty)
            ExitSlowMotion();

        // Exit slow-motion if no longer holding (e.g. quick tap without release event)
        if (_isAttackPending && !input.IsAttackDown && !input.AttackReleased)
        {
            ExitSlowMotion();
            ExitAttackPending();
        }

        // Release event: start dash or whiff
        if (input.AttackReleased && _isAttackPending)
        {
            Debug.Log("[Combat] Attack released. Checking dash condition.");
            IEnemy cachedTarget = _lastHighlighted;
            if (_isSlowMo)
                ExitSlowMotion();
            ExitAttackPending();
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
        _isAttackPending    = true;
        Time.timeScale      = slowTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale; // always paired
        _isSlowMo           = true;
        _slowMoStartTime    = Time.unscaledTime; // [MEDIUM — Gemini] record entry time
        _rangeDisplay?.Show();
    }

    /// <summary>
    /// FloorSpawner가 층 전환 시작 전에 호출한다.
    /// slow-motion + attack-pending 상태를 강제 종료하고 timeScale을 1로 복구.
    /// LockInput() 이전에 호출해야 CombatController.Update() 차단 전에 정리됨.
    /// </summary>
    public void ForceExitCombatState()
    {
        ExitSlowMotion();
        ExitAttackPending();
    }

    /// <summary>
    /// Exit slow-motion. ALWAYS restores both timeScale AND fixedDeltaTime.
    /// Called BEFORE dash yields so MovePosition moves at full speed.
    /// Does NOT hide range display — ExitAttackPending() owns that.
    /// </summary>
    private void ExitSlowMotion()
    {
        if (!_isSlowMo) return; // Safely handle multiple calls
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f; // restore standard fixed step
        _isSlowMo           = false;
    }

    /// <summary>
    /// End the attack waiting state: hide range display and clear enemy highlight.
    /// Separated from ExitSlowMotion so gauge-empty only exits slow-mo while
    /// keeping range display and dash-on-release alive.
    /// </summary>
    private void ExitAttackPending()
    {
        if (!_isAttackPending) return;
        _isAttackPending = false;
        _rangeDisplay?.Hide();
        if (_lastHighlighted != null)
        {
            _lastHighlighted.ClearHighlight();
            _lastHighlighted = null;
        }
    }

    // -- Combat coroutine chain ----------------------------------------------------

    private IEnumerator DashOrWhiff(IEnemy cachedTarget = null)
    {
        _isBusy = true;
        _animator?.SetBool("IsAttacking", true);

        // 하이라이트된 적을 우선 사용, 없으면 재탐색
        var target = (cachedTarget != null && cachedTarget.IsAlive)
            ? cachedTarget
            : _activeModule.FindTarget((Vector2)transform.position, _ctx);
        if (target != null)
        {
            ExitSlowMotion();
            yield return _activeModule.Resolve(target, _ctx);
        }
        else
        {
            yield return _activeModule.Whiff(_ctx);
        }

        _animator?.SetBool("IsAttacking", false);
        _isBusy = false;
    }

    // -- Hit impact (D-07, D-10) ----------------------------------------------------

    /// <summary>D-10: TrailRenderer 시각(그라데이션 색상, 폭 커브) 강화 설정.</summary>
    private void ConfigureTrailVisuals(TrailRenderer trail)
    {
        trail.time = 0.25f;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 0.4f), new Keyframe(1f, 0f));
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(new Color(0.6f, 0.9f, 1f), 0f), new GradientColorKey(new Color(0.1f, 0.4f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = grad;
    }

    // -- Enemy detection -----------------------------------------------------------

    /// <summary>
    /// Update enemy highlight (D-04): red on nearest, clear previous.
    /// Called while _isAttackPending — persists through slow-mo exit on gauge empty.
    /// </summary>
    private void UpdateHighlight(IEnemy nearest)
    {
        if (nearest == _lastHighlighted) return;
        if (_lastHighlighted != null) _lastHighlighted.ClearHighlight();
        if (nearest != null)
        {
            var sr = (nearest as MonoBehaviour)?.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.red;
        }
        _lastHighlighted = nearest;
    }
}
