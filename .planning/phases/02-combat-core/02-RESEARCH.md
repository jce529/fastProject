# Phase 2: Combat Core - Research

**Researched:** 2026-06-02
**Domain:** Unity 6 combat system — slow-motion, dash-kill, range detection, hit-freeze, roll, gauge
**Confidence:** HIGH (all core findings verified against existing codebase and established Unity patterns)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** 직선형 범위 — LineRenderer로 플레이어 양쪽 방향으로 레이저 빔 2줄 표시
- **D-02:** 부채꼴형 범위 — LineRenderer 와이어프레임으로 부채꼴 윤곽선만 표시 (성능 우선)
- **D-03:** 기본 범위 색상: 노란색 (Yellow)
- **D-04:** 범위 내 적 감지 시: 가장 가까운 적의 아웃라인/스프라이트를 빨간색으로 강조
- **D-05:** 범위 수치 (직선 길이, 부채꼴 각도/반지름) — Claude 초기값 결정 후 플레이테스트 조정
- **D-06:** 돌진 중 Trail Renderer로 잔상 표시 — 속도감 시각화
- **D-07:** 카메라는 LateUpdate 추적 유지 — 돌진 시 별도 카메라 반응 없음 (Phase 1 D-11 일관성)
- **D-08:** 시각: 회색 실루엣 캡슐 또는 사각형 placeholder 스프라이트 — Phase 1 플랫폼과 동일한 스타일
- **D-09:** 수량: 씬에 3~5개 고정 배치 — 직선형/부채꼴형 범위 패턴 테스트에 충분한 수
- **D-10:** 처치 후 ~2초 뒤 제자리 부활 — 씬 재시작 없이 연속 테스트 가능
- **D-11:** 구르기 무적: InvincibilityHandler 레이어 스왑 패턴 재사용 (PlayerHurtbox ↔ PlayerInvincible)
- **D-12:** 구르기 쿨타임 타이머: `Time.unscaledDeltaTime` 필수 (슬로우모션 timeScale 영향 없어야 함)
- **D-13:** 돌진 구현: `Rigidbody2D.MovePosition()` 2-3프레임, 속도 스파이크 금지

### Claude's Discretion
- 직선형 레이저 빔 길이 초기값 (권장: 8~12 units)
- 부채꼴형 각도/반지름 초기값 (권장: 90~120도, 반지름 6~8 units)
- Trail Renderer 길이 및 페이드아웃 시간
- 더미 부활 이펙트 유무
- 슬로우모션 timeScale 값 (ROADMAP 권장: 0.15~0.25x, STATE.md 참조)
- 게이지 드레인/회복 속도 초기값

### Deferred Ideas (OUT OF SCOPE)
None — discussion stayed within phase scope.
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| MOVE-03 | 별도 버튼으로 구르기가 발동된다 — 구르기 중 무적 판정, 쿨타임 있음, 슬로우 모션 중에도 사용 가능 | RollController coroutine pattern, InvincibilityHandler reuse, unscaledDeltaTime cooldown |
| ATCK-01 | 게임 시작 전 직선형 / 부채꼴형 공격 타입을 선택할 수 있다 (단순 버튼 2개) | Canvas overlay UI pattern, static selection storage |
| ATCK-02 | 공격 버튼을 누르고 있으면 슬로우 모션이 발동되고 공격 범위가 표시된다 (시간정지 게이지 소모) | timeScale + fixedDeltaTime pairing, LineRenderer range display |
| ATCK-03 | 공격 버튼을 떼면 공격 범위 내 가장 가까운 적에게 돌진하여 원샷 처치한다 (돌진 중 무적, 처치 후 짧은 딜레이) | MovePosition dash coroutine, OverlapCircleNonAlloc nearest-enemy query |
| ATCK-04 | 공격 범위 내 적이 없으면 헛베기 애니메이션 재생 후 더 긴 페널티 딜레이가 발생한다 | Whiff branch in dash release handler |
| ATCK-05 | 시간정지 게이지는 시간이 지나면 자동 회복되고, 적 처치 시에도 일부 회복된다 | GaugeController with unscaledDeltaTime drain/regen |
| FEEL-01 | 적 처치 시 히트프리즈 발생 (50-100ms `Time.timeScale = 0`) — 킬의 타격감 핵심 | WaitForSecondsRealtime hit-freeze sequence |
</phase_requirements>

---

## Summary

Phase 2 adds the complete combat loop on top of the Phase 1 movement foundation. The existing codebase already provides all three integration points: `InputManager.IsAttackDown` / `AttackReleased` / `RollPressed` for event polling; `PlayerController.ApplyMovement()` with `1f / Time.timeScale` compensation already baked in so slow-motion requires zero rewrites to movement; and `InvincibilityHandler.StartInvincibility()` for the roll's i-frame window.

The core implementation challenge is the state machine inside `CombatController`: one component that owns the slow-motion lifecycle (enter slow on `AttackHeld`, exit slow on `AttackReleased`), launches the dash coroutine or whiff branch, sequences the hit-freeze, and signals the gauge. Everything except the gauge value update runs in a single coroutine chain so ordering is explicit and reentrancy bugs are impossible. The gauge (`GaugeController`) is a separate component because it has its own Update loop (drain/regen) and the HUD (Phase 4) will read from it independently — coupling it into `CombatController` would make Phase 4 harder.

**Primary recommendation:** `CombatController` (on the Player GameObject, alongside `PlayerController`) owns slow-mo and dash. `GaugeController` (same GameObject) owns drain/regen and exposes a `[0,1]` float. `RangeDisplay` (child GameObject with LineRenderer) is toggled by `CombatController`. `DummyEnemy` is a standalone prefab in `Assets/Scripts/Enemy/`.

---

## Standard Stack

### Core (all already in project — no new packages required)

| Component | Version | Purpose | Why |
|-----------|---------|---------|-----|
| `Rigidbody2D.MovePosition()` | Unity 6.0 | Dash translation | Prevents tunneling through thin colliders; physics-engine moves the body |
| `LineRenderer` | Unity 6.0 | Range visualization | Built-in, zero-cost on mobile when `positionCount` is low |
| `TrailRenderer` | Unity 6.0 | Dash trail | Built-in; enable/disable per dash window |
| `Physics2D.OverlapCircleNonAlloc()` | Unity 6.0 | Enemy detection | Zero-alloc per frame; pre-allocated result buffer |
| `Time.timeScale` + `Time.fixedDeltaTime` | Unity 6.0 | Slow-motion | Must always be set together |
| `WaitForSecondsRealtime` | Unity 6.0 | Hit-freeze + i-frame timing | Immune to `timeScale = 0` |
| `InvincibilityHandler` (existing) | Phase 1 | Roll invincibility | Layer-swap pattern already tested |
| `Canvas` (Screen Space - Overlay) | uGUI 2.0.0 | Attack type selection UI | Simplest approach; no scene transition needed |

### No New Packages Required

All required functionality is covered by Unity built-ins and the existing package set. Do NOT add new UPM packages for Phase 2.

---

## Architecture Patterns

### Recommended New Script Layout

```
Assets/Scripts/
├── Player/
│   ├── CombatController.cs      # NEW — slow-mo, dash, whiff, hit-freeze, state lock
│   ├── GaugeController.cs       # NEW — gauge drain/regen; exposes float property for HUD
│   ├── RollController.cs        # NEW — roll coroutine + cooldown; calls InvincibilityHandler
│   └── RangeDisplay.cs          # NEW — LineRenderer rendering for both attack shapes
├── Enemy/
│   └── DummyEnemy.cs            # NEW — stationary target; respawns after 2s real-time
└── UI/
    └── AttackTypeSelector.cs    # NEW — Canvas overlay at game start; stores selection statically
```

### Pattern 1: CombatController State Machine

`CombatController` is not a full FSM class — it uses a `bool _isBusy` lockout flag and a coroutine reference. This is the minimum machinery for a prototype.

**State:** `Normal | SlowMotionAiming | Dashing | PostKillLockout | Whiffing`

Implemented as a single coroutine chain, not an enum state machine:

```csharp
// Called from Update() — one entry point
private void Update()
{
    if (_isBusy) return;

    if (InputManager.Instance.AttackHeld && !_isSlowMo)
        EnterSlowMotion();

    if (InputManager.Instance.IsAttackDown)
        _gauge.Drain(Time.unscaledDeltaTime);
    else if (_isSlowMo)
        ExitSlowMotion();

    if (InputManager.Instance.AttackReleased && _isSlowMo)
    {
        ExitSlowMotion();
        StartCoroutine(DashOrWhiff());
    }
}

private IEnumerator DashOrWhiff()
{
    _isBusy = true;
    var target = FindNearestEnemyInRange();
    if (target != null)
        yield return StartCoroutine(ExecuteDash(target));
    else
        yield return StartCoroutine(ExecuteWhiff());
    _isBusy = false;
}
```

**Key insight:** `_isBusy = true` is set before any `yield` and cleared only after all post-kill/whiff yields complete. This prevents double-activation if the player taps attack rapidly.

### Pattern 2: Slow-Motion Pairing (LOCKED — ROADMAP Stack Constraint)

Always set both together. Never set one without the other:

```csharp
private void EnterSlowMotion()
{
    Time.timeScale = _slowTimeScale;               // e.g. 0.2f
    Time.fixedDeltaTime = 0.02f * Time.timeScale; // always paired
    _isSlowMo = true;
    _rangeDisplay.Show();
}

private void ExitSlowMotion()
{
    Time.timeScale = 1f;
    Time.fixedDeltaTime = 0.02f;                   // restore
    _isSlowMo = false;
    _rangeDisplay.Hide();
}
```

`PlayerController.ApplyMovement()` already multiplies speed by `1f / Time.timeScale` — no changes needed there.

**Where to call:** `EnterSlowMotion()` is called from `CombatController.Update()` on `AttackHeld`. `ExitSlowMotion()` is called before the dash coroutine yields. This ensures the physics engine runs at normal speed during the dash frames, preventing MovePosition from moving only 20% of the intended distance.

### Pattern 3: Dash Coroutine (MovePosition over 2-3 frames)

```csharp
private IEnumerator ExecuteDash(DummyEnemy target)
{
    // 1. Restore time BEFORE dash so MovePosition moves at full speed.
    ExitSlowMotion();

    // 2. Grant invincibility for dash duration.
    _invincibilityHandler.StartInvincibility(0.2f);

    // Enable trail.
    _trailRenderer.emitting = true;

    Vector2 destination = target.transform.position;
    float   dashDuration = 3f * Time.fixedDeltaTime; // 3 FixedUpdate frames

    float elapsed = 0f;
    while (elapsed < dashDuration)
    {
        float t = elapsed / dashDuration;
        _rb.MovePosition(Vector2.Lerp(_startPos, destination, t));
        elapsed += Time.fixedDeltaTime;
        yield return new WaitForFixedUpdate();
    }
    _rb.MovePosition(destination); // snap to exact position

    _trailRenderer.emitting = false;

    // 3. Kill.
    target.OnDashHit();

    // 4. Hit-freeze.
    yield return StartCoroutine(HitFreeze(0.075f)); // 75ms — midpoint of 50-100ms range

    // 5. Post-kill lockout (~0.2s real-time).
    yield return new WaitForSecondsRealtime(0.2f);

    // 6. Partial gauge recovery on kill.
    _gauge.AddKillBonus();
}
```

**Anti-tunneling note:** `Continuous` collision detection is already set on the Rigidbody2D (enforced in `PlayerController.Awake()`). MovePosition still goes through the physics engine, so tunneling through thin colliders is prevented. For dummy enemies (which are stationary and sized ~1 unit), 3 frames at normal speed covers ~0.25 units per frame — no tunneling risk at typical room scales.

### Pattern 4: Hit-Freeze Sequence

```csharp
private IEnumerator HitFreeze(float realSeconds)
{
    Time.timeScale = 0f;
    Time.fixedDeltaTime = 0f; // fixedDeltaTime must also be zeroed
    yield return new WaitForSecondsRealtime(realSeconds);
    Time.timeScale = 1f;
    Time.fixedDeltaTime = 0.02f;
}
```

**Why `WaitForSecondsRealtime` is mandatory:** When `timeScale = 0`, `WaitForSeconds(0.075f)` waits for `0.075f / timeScale = infinity`. `WaitForSecondsRealtime` uses the wall clock (`Time.unscaledTime`) which is immune to `timeScale`. This is the same reason `InvincibilityHandler` already uses `WaitForSecondsRealtime`.

**Ordering:** Hit-freeze fires AFTER `target.OnDashHit()` (enemy death) and BEFORE the post-kill lockout yield. This creates: dash arrives → enemy dies → world freezes for 75ms → world unfreezes → player has 200ms of lockout before regaining control. The freeze is the punctuation mark; the lockout is the breath.

### Pattern 5: Enemy Detection — OverlapCircleNonAlloc (LOCKED — ROADMAP Stack Constraint)

```csharp
// Field — pre-allocated once, reused every frame. No GC.
private readonly Collider2D[] _hitBuffer = new Collider2D[16];
private int _enemyLayer;

private void Awake()
{
    _enemyLayer = LayerMask.GetMask("Enemy"); // one-time name lookup is fine in Awake
}

private DummyEnemy FindNearestEnemyInRange()
{
    int count = Physics2D.OverlapCircleNonAlloc(
        transform.position, _rangeRadius, _hitBuffer, _enemyLayer);

    DummyEnemy nearest = null;
    float bestDist = float.MaxValue;

    for (int i = 0; i < count; i++)
    {
        // Additional shape filter for fan mode.
        if (!IsInAttackShape(_hitBuffer[i].transform.position)) continue;

        float d = Vector2.SqrMagnitude(
            (Vector2)_hitBuffer[i].transform.position - (Vector2)transform.position);
        if (d < bestDist)
        {
            bestDist = d;
            nearest = _hitBuffer[i].GetComponent<DummyEnemy>();
        }
    }
    return nearest;
}
```

**No LINQ in Update.** Use `SqrMagnitude` for distance comparison (avoids sqrt). Buffer size 16 is sufficient for 3-5 dummies in the test room.

**Fan mode shape filter:**
```csharp
private bool IsInAttackShape(Vector2 targetPos)
{
    if (_attackType == AttackType.Linear) return true; // radius already filters

    // Fan: check angle from facing direction
    Vector2 toTarget = (targetPos - (Vector2)transform.position).normalized;
    Vector2 facing   = _spriteRenderer.flipX ? Vector2.left : Vector2.right;
    float   dot      = Vector2.Dot(facing, toTarget);
    float   halfAngle = _fanHalfAngleDeg * Mathf.Deg2Rad;
    return dot >= Mathf.Cos(halfAngle);
}
```

### Pattern 6: Range Display — LineRenderer

**Linear mode (2 lines, 2 points each):**

```csharp
// _leftLine and _rightLine are two LineRenderer components on child GameObjects.
private void UpdateLinearDisplay()
{
    bool facingRight = !_spriteRenderer.flipX;
    Vector2 origin = transform.position;

    if (facingRight)
    {
        _rightLine.SetPosition(0, origin);
        _rightLine.SetPosition(1, origin + Vector2.right * _linearLength);
        _leftLine.SetPosition(0, origin);
        _leftLine.SetPosition(1, origin + Vector2.left * _linearLength);
    }
    else // mirror
    {
        // same — linear fires both directions regardless of facing
        _rightLine.SetPosition(0, origin);
        _rightLine.SetPosition(1, origin + Vector2.right * _linearLength);
        _leftLine.SetPosition(0, origin);
        _leftLine.SetPosition(1, origin + Vector2.left * _linearLength);
    }
}
```

**Fan mode (wireframe arc):**

Use a single LineRenderer with `positionCount = arcSegments + 1`. 24 segments gives a smooth arc visible on a 1080p screen. On mobile at 720p, 16 segments is sufficient.

```csharp
// Recommended: 24 points for editor preview, 16 for mobile build
private void UpdateFanDisplay()
{
    Vector2 facing   = _spriteRenderer.flipX ? Vector2.left : Vector2.right;
    float   baseAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

    _arcLine.positionCount = _arcSegments + 1;
    for (int i = 0; i <= _arcSegments; i++)
    {
        float t     = (float)i / _arcSegments;
        float angle = (baseAngle - _fanHalfAngleDeg + t * _fanHalfAngleDeg * 2f) * Mathf.Deg2Rad;
        Vector2 pt  = (Vector2)transform.position
                    + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _fanRadius;
        _arcLine.SetPosition(i, pt);
    }
}
```

**Performance:** 16-24 `SetPosition` calls per frame during slow-motion only. During normal play the LineRenderers are disabled. Cost is negligible.

**Enemy highlight (D-04):** On the `DummyEnemy`, expose a `SpriteRenderer`. When `CombatController.FindNearestEnemyInRange()` returns a target, set its `spriteRenderer.color = Color.red`. Clear to white when slow-mo exits or target leaves range. Track the previously highlighted enemy to clear it when the nearest changes.

### Pattern 7: Gauge Controller

`GaugeController` is a separate MonoBehaviour component on the Player GameObject. It is NOT part of `CombatController`.

```csharp
public class GaugeController : MonoBehaviour
{
    [SerializeField] private float drainPerSecond  = 0.25f; // 4 seconds to empty
    [SerializeField] private float regenPerSecond  = 0.15f; // ~6.7 seconds to full
    [SerializeField] private float killBonus       = 0.20f; // +20% on kill

    public float Value { get; private set; } = 1f; // [0, 1]
    public bool  IsEmpty => Value <= 0f;

    private bool _isDraining;

    public void SetDraining(bool drain) => _isDraining = drain;

    public void AddKillBonus() => Value = Mathf.Min(1f, Value + killBonus);

    private void Update()
    {
        if (_isDraining)
            Value = Mathf.Max(0f, Value - drainPerSecond * Time.unscaledDeltaTime);
        else
            Value = Mathf.Min(1f, Value + regenPerSecond * Time.unscaledDeltaTime);
    }
}
```

`CombatController` calls `_gauge.SetDraining(InputManager.Instance.IsAttackDown)` each Update. When the gauge empties, `CombatController` calls `ExitSlowMotion()` — the player can still release Attack to dash (gauge empty does not block the dash, only exits slow-mo automatically).

**HUD exposure (Phase 4):** Phase 4 reads `GaugeController.Value` directly — no events needed for a prototype. This is why separation from `CombatController` matters.

### Pattern 8: Roll Controller

`RollController` is a separate MonoBehaviour on the Player GameObject.

```csharp
public class RollController : MonoBehaviour
{
    [SerializeField] private float rollSpeed        = 12f;   // units/s during roll
    [SerializeField] private float rollDuration     = 0.3f;  // real seconds
    [SerializeField] private float rollCooldown     = 0.8f;  // real seconds
    [SerializeField] private float iFrameDuration   = 0.4f;  // real seconds (longer than roll)

    private InvincibilityHandler _invincibility;
    private Rigidbody2D          _rb;
    private float                _cooldownRemaining;
    private bool                 _isRolling;

    private void Awake()
    {
        _invincibility = GetComponent<InvincibilityHandler>();
        _rb            = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Cooldown ticks in REAL time — not affected by slow-motion.
        if (_cooldownRemaining > 0f)
            _cooldownRemaining -= Time.unscaledDeltaTime;

        if (InputManager.Instance.RollPressed && !_isRolling && _cooldownRemaining <= 0f)
            StartCoroutine(RollCoroutine());
    }

    private IEnumerator RollCoroutine()
    {
        _isRolling = true;
        _cooldownRemaining = rollCooldown;

        // Determine roll direction from current facing (spriteRenderer.flipX).
        var sr = GetComponent<SpriteRenderer>();
        float dir = sr.flipX ? -1f : 1f;

        // Trigger animation BEFORE velocity — animator needs a frame to respond.
        GetComponent<Animator>().SetTrigger("Roll");

        // Grant i-frames immediately.
        _invincibility.StartInvincibility(iFrameDuration);

        // Apply velocity kick over real-time duration.
        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            // Compensate for timeScale so roll speed is consistent in slow-mo.
            float compensated = rollSpeed * (1f / Time.timeScale);
            _rb.linearVelocity = new Vector2(dir * compensated, _rb.linearVelocity.y);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        _isRolling = false;
    }
}
```

**Key choices:**
- Animation trigger fires BEFORE velocity so the sprite is already in roll pose when movement starts.
- Roll velocity uses `1f / Time.timeScale` compensation (same pattern as `PlayerController`) so the player visually moves at the same speed during slow-mo.
- Cooldown drains with `Time.unscaledDeltaTime` — immune to `timeScale`.
- `iFrameDuration > rollDuration` is intentional: i-frames persist slightly after the velocity ends so the player is still protected during recovery frames.

### Pattern 9: Attack Type Selection — Canvas Overlay

**Decision: Canvas overlay on SampleScene, NOT a separate scene.**

Rationale: A separate scene requires `SceneManager.LoadScene()` and a way to pass the selection across the load (PlayerPrefs or a DontDestroyOnLoad singleton). For a prototype test room, a Canvas overlay that disables itself is simpler and carries zero cross-scene complexity.

**Storage:** A `static` field on `AttackTypeSelector` — not a singleton component, just a static value. It persists for the session (until Play mode exits) without any DontDestroyOnLoad setup.

```csharp
public enum AttackType { Linear, Fan }

public class AttackTypeSelector : MonoBehaviour
{
    public static AttackType Selected { get; private set; } = AttackType.Linear;

    [SerializeField] private GameObject overlayRoot; // the Canvas or panel

    public void SelectLinear()
    {
        Selected = AttackType.Linear;
        overlayRoot.SetActive(false);
        Time.timeScale = 1f; // in case scene started paused
    }

    public void SelectFan()
    {
        Selected = AttackType.Fan;
        overlayRoot.SetActive(false);
        Time.timeScale = 1f;
    }

    private void Start()
    {
        // Pause the game while the overlay is showing.
        // CombatController checks AttackTypeSelector.Selected before acting.
        overlayRoot.SetActive(true);
    }
}
```

**Wire-up:** Two UI Buttons (Linear / Fan) each call the respective method via `OnClick()`. No code needed beyond this. `CombatController` and `RangeDisplay` read `AttackTypeSelector.Selected` at runtime.

### Pattern 10: Dummy Enemy

```csharp
// Assets/Scripts/Enemy/DummyEnemy.cs
public class DummyEnemy : MonoBehaviour
{
    [SerializeField] private float respawnDelay = 2f; // real seconds (D-10)

    private SpriteRenderer _spriteRenderer;
    private Collider2D     _collider;
    private Vector3        _spawnPosition;

    public bool IsAlive { get; private set; } = true;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider       = GetComponent<Collider2D>();
        _spawnPosition  = transform.position;
    }

    /// <summary>Called by CombatController after dash arrives.</summary>
    public void OnDashHit()
    {
        if (!IsAlive) return;
        StartCoroutine(DeathAndRespawn());
    }

    private IEnumerator DeathAndRespawn()
    {
        IsAlive = false;
        _spriteRenderer.enabled = false;
        _collider.enabled = false;

        yield return new WaitForSecondsRealtime(respawnDelay);

        transform.position = _spawnPosition;
        _spriteRenderer.enabled = true;
        _collider.enabled = false;   // re-enable one frame later to avoid physics re-overlap
        yield return null;
        _collider.enabled = true;

        // Reset highlight color in case it was red.
        _spriteRenderer.color = Color.white;
        IsAlive = true;
    }
}
```

**`OnDashHit()` is called from the dash coroutine** in `CombatController`, not from a physics trigger. This keeps the kill sequence explicit and sequenced: arrive at position → call `OnDashHit()` → then hit-freeze. A trigger callback would fire asynchronously and make sequencing unreliable.

**Collider:** Use `CapsuleCollider2D` on a child GameObject tagged "Enemy" and on the "Enemy" layer (must be added to the layer matrix). The Rigidbody2D on the dummy should be `Static` type — it never moves, so dynamic physics is unnecessary overhead.

### Anti-Patterns to Avoid

- **Setting `Time.timeScale` without `Time.fixedDeltaTime`:** Physics will run at wrong speed. Always set both.
- **Using `WaitForSeconds` during timeScale=0 hit-freeze:** Coroutine never resumes. Use `WaitForSecondsRealtime`.
- **LINQ in Update for enemy queries:** Generates GC alloc per frame. Use `OverlapCircleNonAlloc` with a pre-allocated buffer.
- **Velocity spike for dash:** `rb.linearVelocity = hugeVector` fires the body as a projectile; it clips through things. Use `MovePosition()`.
- **Separate scene for attack type selection:** Adds cross-scene data passing complexity. Canvas overlay is sufficient for a prototype.
- **Separate `DontDestroyOnLoad` singleton for attack type:** Static field on a UI class is enough for a single-scene prototype.
- **Rolling cooldown with `Time.deltaTime`:** Cooldown would extend 5x during 0.2x slow-motion. Always `Time.unscaledDeltaTime`.
- **IgnoreLayerCollision for roll i-frames:** Projectile colliders globally affected. Use layer swap (already established in Phase 1).
- **Calling `ExitSlowMotion()` after `ExecuteDash()` starts:** Dash MovePosition distance would be scaled by slow timeScale. Must exit slow-mo BEFORE dash movement begins.
- **`Physics2D.OverlapCircle` (alloc version) in Update:** GC pressure. Always `NonAlloc`.
- **`LayerMask.NameToLayer()` in Update:** String lookup every frame. Cache in Awake.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Dash distance movement | Custom velocity formula | `Rigidbody2D.MovePosition()` | MovePosition is CCD-aware; velocity spikes clip through geometry |
| Real-time delay during timeScale=0 | Custom unscaled timer loop | `WaitForSecondsRealtime` | Already battle-tested in InvincibilityHandler (Phase 1) |
| Enemy proximity sorting | Manual bubble sort or LINQ | Pre-allocated buffer + manual min-scan | Zero GC; LINQ allocates an enumerator on heap |
| Fan arc geometry | Mesh polygon | LineRenderer wireframe | No mesh asset needed; sufficient visual clarity for prototype |
| Session data persistence | PlayerPrefs / SceneManager | `static` field on selector class | Zero infrastructure; resets cleanly when Play mode exits |

---

## Runtime State Inventory

Step 2.5: SKIPPED — Phase 2 is not a rename/refactor/migration phase. New files are created; no existing runtime state is renamed.

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity 6000.3.11f1 | All | Yes (confirmed from CLAUDE.md) | 6000.3.11f1 | — |
| com.unity.inputsystem | InputManager | Yes (1.19.0 in manifest) | 1.19.0 | — |
| com.unity.modules.physics2d | OverlapCircleNonAlloc | Yes (1.0.0) | 1.0.0 | — |
| com.unity.modules.animation | Animator | Yes (1.0.0) | 1.0.0 | — |
| com.unity.ugui | Canvas overlay | Yes (2.0.0) | 2.0.0 | — |
| com.unity.modules.particlesystem | TrailRenderer | Yes (1.0.0) | 1.0.0 | — |
| com.unity.test-framework | Play Mode tests | Yes (1.6.0) | 1.6.0 | Manual validation |

**Missing dependencies with no fallback:** None.

**Missing dependencies with fallback:** None.

**Note:** No new packages are required for Phase 2. All functionality is covered by existing project dependencies.

---

## Common Pitfalls

### Pitfall 1: MovePosition Scaled by Slow timeScale

**What goes wrong:** Dash coroutine starts while `timeScale = 0.2f`. Each `WaitForFixedUpdate()` step uses `Time.fixedDeltaTime = 0.004f` instead of `0.02f`. The player moves 5x slower than intended and may appear frozen during the dash.

**Why it happens:** Forgetting to call `ExitSlowMotion()` before the first `yield return new WaitForFixedUpdate()`.

**How to avoid:** `ExitSlowMotion()` is always the FIRST line of `ExecuteDash()`, before any yield.

**Warning signs:** Dash visually crawls. `Time.fixedDeltaTime` in Debug.Log is 0.004 during dash.

### Pitfall 2: Hit-Freeze Coroutine Never Resumes

**What goes wrong:** `yield return new WaitForSeconds(0.075f)` inside `HitFreeze()` after `Time.timeScale = 0`. The coroutine is suspended indefinitely. Game appears frozen permanently.

**Why it happens:** `WaitForSeconds` waits scaled time; `0.075f / 0 = infinity`.

**How to avoid:** Always use `WaitForSecondsRealtime` inside `HitFreeze()`.

**Warning signs:** Game freezes on first kill and never unfreezes.

### Pitfall 3: Roll Cooldown Extends in Slow-Motion

**What goes wrong:** Player holds attack (entering 0.2x slow-mo), tries to roll. Cooldown `_cooldownRemaining` drains at `0.2x * Time.deltaTime` (scaled). An 0.8s real-time cooldown takes 4 real seconds to expire during slow-mo.

**Why it happens:** Using `Time.deltaTime` instead of `Time.unscaledDeltaTime` for cooldown decrement.

**How to avoid:** `_cooldownRemaining -= Time.unscaledDeltaTime` in RollController.Update.

**Warning signs:** Roll button becomes unresponsive for long periods during slow-motion.

### Pitfall 4: Double Dash Activation

**What goes wrong:** Player rapidly releases and re-presses Attack. `AttackReleased` fires, dash coroutine starts. Before it completes, `AttackReleased` fires again. Second coroutine starts, targeting a different (or same dead) enemy. Two dash coroutines running simultaneously.

**Why it happens:** No `_isBusy` lockout check at the entry point.

**How to avoid:** Check `if (_isBusy) return;` before any attack state transition. Set `_isBusy = true` as the FIRST thing in `DashOrWhiff()`, before the first yield.

**Warning signs:** Player dashes twice in quick succession; gauge depletes unexpectedly; enemy dies twice.

### Pitfall 5: fixedDeltaTime Not Restored After Hit-Freeze

**What goes wrong:** `HitFreeze()` sets `Time.fixedDeltaTime = 0f` alongside `timeScale = 0f`. After the freeze, `Time.timeScale = 1f` is restored but `Time.fixedDeltaTime` remains 0. Physics FixedUpdate never fires. Player falls through the floor. `PlayerController.ApplyMovement()` never runs.

**Why it happens:** Restoring only `timeScale` and forgetting `fixedDeltaTime`.

**How to avoid:** In `HitFreeze()`, always restore both: `Time.timeScale = 1f; Time.fixedDeltaTime = 0.02f;`

**Warning signs:** Player can't move after the first kill. Console errors about Rigidbody being in invalid state (less common but possible).

### Pitfall 6: Targeting a Dead Enemy

**What goes wrong:** Dummy is in the death coroutine (invisible, collider disabled). The range query finds its `Collider2D` in the buffer because Unity's physics engine may not immediately remove a disabled collider from the broadphase within the same frame.

**Why it happens:** `_collider.enabled = false` and `OverlapCircleNonAlloc` may overlap frames.

**How to avoid:** Check `DummyEnemy.IsAlive` on each candidate after `OverlapCircleNonAlloc`. Skip dead enemies in the min-scan loop.

**Warning signs:** Dash fires toward an invisible enemy. Player arrives at an empty location.

### Pitfall 7: Canvas Overlay Blocks Input During Gameplay

**What goes wrong:** `AttackTypeSelector` overlay remains active after a button is pressed (bug in deactivation logic). The Canvas raycaster intercepts all click/tap events. `InputManager` never receives Attack action events. Slow-motion never triggers.

**Why it happens:** Forgetting to call `overlayRoot.SetActive(false)` or accidentally activating it again.

**How to avoid:** After button press, disable the entire root GameObject (not just the Image). Verify in the Hierarchy window that the canvas is gone before testing combat.

**Warning signs:** Attack button does nothing in gameplay. No slow-motion. Canvas is still visible in Scene view.

---

## Code Examples

### Slow-Mo Entry/Exit (verified pattern)

```csharp
// Source: established project constraint (ROADMAP.md Stack Constraints + Phase 1 decisions)
private const float SlowScale = 0.2f; // Claude's discretion; playtest to tune (0.15-0.25 range)

private void EnterSlowMotion()
{
    Time.timeScale       = SlowScale;
    Time.fixedDeltaTime  = 0.02f * Time.timeScale;
    _rangeDisplay.Show();
}

private void ExitSlowMotion()
{
    Time.timeScale       = 1f;
    Time.fixedDeltaTime  = 0.02f;
    _rangeDisplay.Hide();
    _isSlowMo = false;
}
```

### Hit-Freeze (verified pattern)

```csharp
// Source: established project constraint — WaitForSecondsRealtime (STATE.md Key Decisions Locked)
private IEnumerator HitFreeze(float realSeconds)
{
    Time.timeScale      = 0f;
    Time.fixedDeltaTime = 0f;
    yield return new WaitForSecondsRealtime(realSeconds);
    Time.timeScale      = 1f;
    Time.fixedDeltaTime = 0.02f;
}
```

### OverlapCircleNonAlloc — Pre-allocated buffer (verified pattern)

```csharp
// Source: Unity Physics2D documentation — NonAlloc variant, pre-allocated array
private readonly Collider2D[] _hitBuffer = new Collider2D[16];

private DummyEnemy FindNearestEnemyInRange()
{
    int count = Physics2D.OverlapCircleNonAlloc(
        transform.position, _searchRadius, _hitBuffer, _enemyLayerMask);

    DummyEnemy nearest  = null;
    float      bestSqDist = float.MaxValue;

    for (int i = 0; i < count; i++)
    {
        var dummy = _hitBuffer[i].GetComponent<DummyEnemy>();
        if (dummy == null || !dummy.IsAlive) continue;
        if (!IsInAttackShape(_hitBuffer[i].transform.position)) continue;

        float sqDist = ((Vector2)_hitBuffer[i].transform.position
                       - (Vector2)transform.position).sqrMagnitude;
        if (sqDist < bestSqDist)
        {
            bestSqDist = sqDist;
            nearest    = dummy;
        }
    }
    return nearest;
}
```

### InvincibilityHandler.StartInvincibility — existing API

```csharp
// Source: Assets/Scripts/Player/InvincibilityHandler.cs (existing, Phase 1)
// Signature: public void StartInvincibility(float duration)
// - duration is in real seconds (unscaled)
// - safe to call while already invincible (restarts timer)
// Usage in RollController:
_invincibilityHandler.StartInvincibility(iFrameDuration); // e.g. 0.4f
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| `IgnoreLayerCollision` for i-frames | Layer swap (PlayerHurtbox/PlayerInvincible) | Phase 1 (established) | Avoids global collision matrix side-effects |
| `FindObjectsOfType` for enemy queries | `OverlapCircleNonAlloc` pre-allocated | Phase 1 constraint | Zero GC alloc per frame |
| Velocity spike for dashes | `MovePosition()` over 2-3 frames | Phase 1 constraint | CCD-safe; no tunneling |

---

## Open Questions

1. **Gauge drain rate balance**
   - What we know: Initial values (drainPerSecond=0.25, regenPerSecond=0.15) are Claude's starting guess based on 4s empty / 6.7s regen. These are within Claude's discretion per D-05.
   - What's unclear: Whether 4 seconds of slow-mo per fill feels too short or too long on mobile. Cannot know without playtesting.
   - Recommendation: Expose all gauge values as `[SerializeField]` fields so the playtester can tune in the Inspector without recompiling.

2. **Slow-motion timeScale optimal value**
   - What we know: STATE.md documents 0.15-0.25x as the research-suggested range. Claude's discretion per CONTEXT.md.
   - What's unclear: 0.2x may feel too slow on mobile (hard to aim if enemies barely move). 0.3x may feel insufficiently dramatic.
   - Recommendation: Start at 0.2f. Make it a `[SerializeField]`. Tune during Phase 2 playtest.

3. **Arc segment count for fan LineRenderer**
   - What we know: 24 segments renders smoothly at 1080p. 16 is sufficient at 720p.
   - What's unclear: Android target device resolution. CLAUDE.md specifies 1920x1080 default.
   - Recommendation: Use 24 segments. Expose as `[SerializeField]`. Down to 16 if profiling shows LineRenderer is in the top 5% of CPU cost (unlikely at 24 points).

---

## Validation Architecture

> `workflow.nyquist_validation = true` in `.planning/config.json` — this section is required.

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.6.0 (NUnit, bundled) |
| Config file | None yet — Wave 0 must create `Assets/Tests/` assembly |
| Quick run command | Unity Editor: Window > General > Test Runner > Play Mode > Run All |
| Full suite command | Same — all Play Mode tests in `Assets/Tests/PlayMode/` |

**Important:** Unity Play Mode tests run in a temporary scene inside the Editor. They can test time manipulation, physics, and coroutines. Edit Mode tests cannot test coroutines or physics. All Phase 2 requirements require Play Mode tests or manual validation.

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated? | Notes |
|--------|----------|-----------|------------|-------|
| MOVE-03 | Roll activates, grants i-frames, has cooldown, works in slow-mo | Play Mode | Partially | Can test cooldown timer (unscaled) and i-frame layer swap; slow-mo interaction needs manual confirm |
| ATCK-01 | Attack type selection UI shows at start; button click sets selection | Play Mode (Edit OK for static) | Yes | Test `AttackTypeSelector.Selected` after calling `SelectLinear()` / `SelectFan()` |
| ATCK-02 | `IsAttackDown` → `Time.timeScale` drops to SlowScale; LineRenderer enabled | Play Mode | Yes | Check `Time.timeScale < 1f` and `lineRenderer.enabled` after simulating held attack |
| ATCK-03 | `AttackReleased` with enemy in range → player reaches enemy position | Play Mode | Yes | Assert `player.position == dummy.position` (±0.1 units) after coroutine finishes |
| ATCK-04 | `AttackReleased` with no enemy in range → whiff anim + longer lockout than kill | Manual | No | Lockout duration comparison requires real-time measurement; manual stopwatch |
| ATCK-05 | Gauge drains while held, regens when released, +bonus on kill | Play Mode | Yes | Tick `GaugeController.Update()` manually or use `yield return null` loops |
| FEEL-01 | Kill → `Time.timeScale = 0` for 50-100ms → resumes | Play Mode | Yes | Check `Time.timeScale == 0f` immediately after kill event; check it is `1f` after `WaitForSecondsRealtime(0.1f)` |

### Detailed Test Specifications

**ATCK-01 (Edit Mode OK):**
```csharp
[Test]
public void SelectLinear_SetsSelectedToLinear()
{
    var go = new GameObject();
    var selector = go.AddComponent<AttackTypeSelector>();
    selector.SelectLinear();
    Assert.AreEqual(AttackType.Linear, AttackTypeSelector.Selected);
    Object.DestroyImmediate(go);
}
```

**ATCK-02 (Play Mode):**
```csharp
[UnityTest]
public IEnumerator AttackHeld_EntersSlowMotion()
{
    // Set up minimal scene with CombatController
    // Simulate InputManager.IsAttackDown = true (requires test double or setter)
    yield return null;
    Assert.Less(Time.timeScale, 1f);
    Assert.IsTrue(rangeDisplay.IsShown);
}
```

**FEEL-01 (Play Mode):**
```csharp
[UnityTest]
public IEnumerator DashKill_TriggersHitFreeze()
{
    // Position dummy in range, trigger AttackReleased
    yield return null; // let DashOrWhiff start
    // After kill signal, timeScale should be 0
    Assert.AreEqual(0f, Time.timeScale);
    yield return new WaitForSecondsRealtime(0.15f);
    Assert.AreEqual(1f, Time.timeScale);
}
```

**ATCK-04 — Manual validation step (cannot automate lockout duration comparison reliably in Play Mode tests without a real-time stopwatch fixture):**
1. Enter Play mode.
2. Position away from any dummy.
3. Hold Attack (slow-mo starts), release immediately (whiff).
4. Count frames/seconds until player regains control.
5. Repeat with a dummy in range.
6. Verify whiff lockout is visibly longer (target: kill=0.2s, whiff=0.5s).

### Sampling Rate

- **Per task commit:** Run Play Mode tests for the tasks in that wave (Target: all pass, 0 failures)
- **Per wave merge:** Run full Play Mode suite (all Phase 2 tests)
- **Phase gate:** Full suite green before `/gsd:verify-work` — ATCK-04 manual step documented and signed off

### Wave 0 Gaps

- [ ] `Assets/Tests/PlayMode/` directory — create with `.asmdef` referencing `Unity.TestFramework.Tests`
- [ ] `Assets/Tests/PlayMode/CombatTests.cs` — covers ATCK-01, ATCK-02, ATCK-03, ATCK-05, FEEL-01
- [ ] `Assets/Tests/PlayMode/RollTests.cs` — covers MOVE-03
- [ ] `Assets/Tests/PlayMode.asmdef` — assembly definition enabling Play Mode tests

---

## Project Constraints (from CLAUDE.md)

| Directive | Source | Constraint |
|-----------|--------|------------|
| Unity 6 LTS + C# — already configured | CLAUDE.md §Tech Stack | No engine version change; no new packages unless essential |
| Android platform, ARM64, minSdk 25 | CLAUDE.md §Platform | No features that require APIs above Android API 25; consider mobile perf budget |
| Core mechanic validation ONLY | CLAUDE.md §Scope | No polish, no extra features beyond Phase 2 requirements |
| Phase isolation | .claude/CLAUDE.md §Phase 격리 | No Phase 3 code (enemy AI, FSM) written during Phase 2 |
| No overengineering | .claude/CLAUDE.md §단순성 우선 | Minimum code to make each requirement pass; no abstractions for hypothetical Phase 5 |
| Surgical changes | .claude/CLAUDE.md §정밀한 변경 | `PlayerController.cs` changes must be near-zero (timeScale compensation already done) |
| `Time.timeScale` → always pair `Time.fixedDeltaTime` | ROADMAP.md Stack Constraints | Enforced in every CombatController method that touches timeScale |
| `Time.unscaledDeltaTime` for ALL i-frame/cooldown timers | ROADMAP.md Stack Constraints | Roll cooldown, hit-freeze duration, dummy respawn — all unscaled |
| `Physics2D.OverlapCircleNonAlloc()` — no LINQ in Update | ROADMAP.md Stack Constraints | Pre-allocated `Collider2D[16]` buffer; no `Where()`, `OrderBy()`, `Select()` in hot path |
| `Rigidbody2D.MovePosition()` for dash | ROADMAP.md Stack Constraints | Never `rb.linearVelocity = dashVector` spike |
| Invincibility: layer swap only | ROADMAP.md Stack Constraints | `InvincibilityHandler` already implements this — reuse directly |
| Animator Transition Duration = 0 | ROADMAP.md Stack Constraints | Apply to all new action states: Roll, Attack, Whiff |
| Visual Scripting (Bolt) FORBIDDEN | CLAUDE.md §Out of Scope | All Phase 2 code must be C# MonoBehaviours |

---

## Sources

### Primary (HIGH confidence)
- Existing codebase: `Assets/Scripts/Player/PlayerController.cs`, `InvincibilityHandler.cs`, `InputManager.cs` — direct code read, patterns confirmed
- `.planning/ROADMAP.md` §Stack Constraints — all mandatory implementation rules
- `.planning/STATE.md` §Key Decisions Locked — WaitForSecondsRealtime, MovePosition, layer swap decisions
- `.planning/REQUIREMENTS.md` — exact requirement text for MOVE-03, ATCK-01 through ATCK-05, FEEL-01
- `.planning/phases/02-combat-core/02-CONTEXT.md` — all locked decisions D-01 through D-13

### Secondary (MEDIUM confidence)
- Unity documentation pattern: `Time.timeScale` + `Time.fixedDeltaTime` pairing — well-documented behavior, verified against existing codebase compensation pattern
- `Physics2D.OverlapCircleNonAlloc` zero-alloc pattern — standard Unity performance practice, verified against API signatures in Unity 6
- `WaitForSecondsRealtime` behavior under `timeScale=0` — confirmed by existing `InvincibilityHandler` implementation in project

### Tertiary (LOW confidence)
- Optimal initial values for gauge drain/regen rates — estimated from design intuition; must be validated during playtest

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all libraries already in project; no new packages
- Architecture patterns: HIGH — verified against existing code integration points
- Don't-hand-roll items: HIGH — verified Unity API capabilities
- Common pitfalls: HIGH — each pitfall derived from a specific, identifiable root cause in the code patterns
- Gauge/timing initial values: LOW — design parameters require playtest validation

**Research date:** 2026-06-02
**Valid until:** 2026-07-02 (Unity 6 LTS; stable API surface)
