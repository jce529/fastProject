# Phase 3: Enemy System - Research

**Researched:** 2026-06-08
**Domain:** Unity 2D FSM Enemy AI, Projectile Physics, IEnemy Interface Integration
**Confidence:** HIGH — all findings derived from direct code inspection of the existing codebase

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

- **D-01:** `IEnemy` 인터페이스 — `IsAlive`, `OnDashHit()`, `ClearHighlight()` 세 멤버. `CombatController`의 `DummyEnemy` 직접 참조를 `IEnemy`로 교체.
- **D-02:** `DummyEnemy`도 `IEnemy` 구현 (하위 호환 유지).
- **D-03:** FSM 4상태: `Idle(Patrol)` → `Chase` → `Telegraph` → `Attack`.
- **D-04:** Idle: 좌우 순찰 왕복. 탐지 반경 내 플레이어 진입 시 Chase.
- **D-05:** Telegraph 시각: 적 머리 위 `!` 아이콘 (자식 SpriteRenderer).
- **D-06:** Telegraph 지속 시간: **0.8초 실시간** (unscaledDeltaTime).
- **D-07:** 근접 공격: 플레이어 위치로 짧은 돌진 또는 히트박스 활성화 — Claude 재량, 단순하게.
- **D-08:** 근접 공격 히트박스: Trigger Collider2D, 공격 시작 프레임에 잠깐 활성화.
- **D-09:** 조준선: `LineRenderer` 빨간 실선. Telegraph 시작 시 알파 0→1 (0.8초), 발사 직전 최대.
- **D-10:** 원거리 이동: `moveSpeed = 0f` 직렬화 필드. 제자리 고정으로 시작.
- **D-11:** 투사체: Rigidbody2D 직선 등속. Trigger Collider2D. 별도 `ProjectileController`.
- **D-12:** 투사체: 일정 거리 이상 이동 또는 Platform 충돌 시 Destroy.
- **D-13:** `PlayerController`에 `static event Action OnPlayerDeath` 선언.
- **D-14:** Phase 3 임시: 피격/낙사 시 `OnPlayerDeath` → `SetActive(false)` + `Debug.Log`. 재시작은 에디터 Play Mode 재시작.
- **D-15:** Phase 4에서 UIManager가 `OnPlayerDeath` 구독 — Phase 3 코드 수정 없음.
- **D-16:** 피격 조건: 플레이어가 `PlayerInvincible` 레이어일 때 적 Trigger 무시.
- **D-17:** 낙사 = 즉사. `FallDetector.cs` 수정 — 텔레포트 복귀 로직 제거, `OnPlayerDeath` 발동으로 교체. 마지막 발판 저장 로직도 제거.
- All timers: `Time.unscaledDeltaTime` (슬로우모션 안전).
- Detection: `Physics2D.OverlapCircleNonAlloc()` — Update 내 LINQ 금지.
- FEEL-01 히트프리즈: 모든 킬에 동일하게 발동.
- Invincibility: 레이어 스왑 PlayerHurtbox/PlayerInvincible.

### Claude's Discretion

- 탐지 반경 수치 (권장: 8~12 units)
- 근접 적의 Chase 이동 속도 (권장: 3~5 units/s)
- 원거리 적의 탐지/공격 사거리 (권장: 10~15 units)
- 투사체 속도 (권장: 8~12 units/s)
- `!` 아이콘 구현 방법 → 자식 SpriteRenderer 선택 (D-05에서 결정됨)

### Deferred Ideas (OUT OF SCOPE)

- 원거리 적 이동 (Chase 속도 > 0) — v2 또는 플레이테스트 후
- 복잡한 순찰 경로 (웨이포인트 기반) — v2
- 적 사망 이펙트 (파티클) — Phase 4 polish 또는 v2
</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| ENMY-01 | 근접형 적이 플레이어를 감지하면 접근하고, 공격 전 예고 모션 후 근접 공격한다 (원샷원킬 양방향) | FSM (D-03~D-08), PlayerDeath event (D-13~D-17), IEnemy (D-01~D-02), FEEL-01 integration |
| ENMY-02 | 원거리형 적이 플레이어를 감지하면 조준선을 표시 후 투사체를 발사한다 (원샷원킬 양방향) | FSM (D-03~D-04, D-09~D-12), LineRenderer pattern reuse, ProjectileController, EnemyProjectile layer |
</phase_requirements>

---

## Summary

Phase 3 is an integration phase. The core game systems (combat, invincibility, layer swap, hit-freeze) are fully implemented and tested. The work here is (1) extracting the `IEnemy` interface that CombatController already depends on implicitly, (2) implementing two FSM-based enemy types against that interface, and (3) wiring `OnPlayerDeath` so enemies can kill the player.

The biggest integration risk is `CombatController`'s hard-coded `DummyEnemy` type: four references must change to `IEnemy` simultaneously (`_lastHighlighted` field, `FindNearestEnemyInRange` return type, `UpdateHighlight` parameter, `DashOrWhiff` cached target). A half-done refactor will compile but produce null references at runtime.

The layer matrix needs one addition: `EnemyProjectile` (layer 11). Everything else — `PlayerHurtbox` (7), `PlayerInvincible` (8), `Platform` (9), `Enemy` (10) — is already registered in `TagManager.asset`.

**Primary recommendation:** Decompose into 4 plans: (1) IEnemy interface + DummyEnemy + CombatController refactor, (2) PlayerDeath event + FallDetector rewrite, (3) MeleeEnemy FSM, (4) RangedEnemy FSM + ProjectileController. Each plan is independently compilable and testable.

---

## 1. Integration Analysis

### 1.1 CombatController — Exact Changes Required

Source file: `Assets/Scripts/Player/CombatController.cs`

**Four changes, all in one file:**

| Line(s) | Current | Required Change |
|---------|---------|----------------|
| 75 | `private DummyEnemy _lastHighlighted;` | `private IEnemy _lastHighlighted;` |
| 204 | `private IEnumerator DashOrWhiff(DummyEnemy cachedTarget = null)` | `private IEnumerator DashOrWhiff(IEnemy cachedTarget = null)` |
| 231 | `private IEnumerator ExecuteDash(DummyEnemy target)` | `private IEnumerator ExecuteDash(IEnemy target)` |
| 312 | `FindNearestEnemyInRange()` return type + body | Return `IEnemy`, replace `GetComponent<DummyEnemy>()` with `GetComponent<IEnemy>()` |
| 375 | `private void UpdateHighlight(DummyEnemy nearest)` | `private void UpdateHighlight(IEnemy nearest)` |
| 377-384 | Inside UpdateHighlight — `sr.color = Color.red` via SpriteRenderer | Replace direct SpriteRenderer color set with `nearest.Highlight()` OR keep the SpriteRenderer lookup (IEnemy.ClearHighlight already defined — Highlight can be added or handled via GetComponent on the MonoBehaviour) |

**Critical detail on UpdateHighlight:** The current code does `nearest.GetComponent<SpriteRenderer>().color = Color.red` inside `CombatController`. Since `IEnemy` only defines `ClearHighlight()`, there are two options:
- Option A: Add `void Highlight()` to IEnemy (modifies interface, cleaner)
- Option B: Keep the `GetComponent<SpriteRenderer>()` call in CombatController, cast `nearest` to `MonoBehaviour` first (no interface change needed)

**Recommendation: Option B** — fewer changes, prototype scope. Cast `(nearest as MonoBehaviour)?.GetComponent<SpriteRenderer>()`. IEnemy stays at 3 members.

**Also in ExecuteDash:** `target.transform.position` — this requires casting `target` to `MonoBehaviour` or `Component` since `IEnemy` does not extend `UnityEngine.Object`. The existing `DummyEnemy target` had this implicitly. Solution: add `Transform Transform { get; }` to IEnemy, OR cast to MonoBehaviour. Again, cast is simpler for prototype.

**Recommended IEnemy definition:**
```csharp
public interface IEnemy
{
    bool IsAlive { get; }
    void OnDashHit();
    void ClearHighlight();
}
```
`CombatController.ExecuteDash` accesses `target.transform.position` — cast internally: `((MonoBehaviour)target).transform.position`.

### 1.2 FallDetector — Exact Changes Required (D-17)

Source file: `Assets/Scripts/Player/FallDetector.cs`

Current `OnFall()`:
- Teleports player to `_lastSafePosition`
- Zeroes velocity
- Calls `_invincibility.StartInvincibility(1.0f)`

New behavior per D-17:
- Remove `_lastSafePosition` tracking in `FixedUpdate` (field and update logic deleted)
- Remove `_invincibility` field and `[RequireComponent(typeof(InvincibilityHandler))]`
- `OnFall()` becomes: `PlayerController.OnPlayerDeath?.Invoke()`
- `FallZoneTrigger.cs` calls `fallDetector.OnFall()` — no change needed there

**Remaining fields after refactor:** only `_controller` (PlayerController reference, used for IsGrounded — now unused too). After D-17, `FallDetector` only needs to respond to the trigger. It can be simplified to just call `PlayerController.OnPlayerDeath?.Invoke()` directly, removing the need for the PlayerController reference entirely. The `[RequireComponent(typeof(PlayerController))]` attribute must also be removed.

**Simplest valid FallDetector after D-17:**
```csharp
public class FallDetector : MonoBehaviour
{
    public void OnFall()
    {
        PlayerController.OnPlayerDeath?.Invoke();
    }
}
```

### 1.3 PlayerController — OnPlayerDeath Addition (D-13)

Source file: `Assets/Scripts/Player/PlayerController.cs`

Add one `static` field at class level:
```csharp
using System;
// ...
public static event Action OnPlayerDeath;
```

No other changes to PlayerController needed.

**Subscriber for Phase 3 (D-14):** A separate `PlayerDeathHandler` MonoBehaviour on the Player GameObject:
```csharp
public class PlayerDeathHandler : MonoBehaviour
{
    private void OnEnable()  => PlayerController.OnPlayerDeath += HandleDeath;
    private void OnDisable() => PlayerController.OnPlayerDeath -= HandleDeath;

    private void HandleDeath()
    {
        Debug.Log("Player died");
        gameObject.SetActive(false);
    }
}
```

**Why a separate component:** Keeps PlayerController clean, avoids touching the movement script, and Phase 4's UIManager subscribes alongside it without any modification to Phase 3 code (D-15).

**Static event stale subscription risk:** Because `OnPlayerDeath` is `static`, if the scene is reloaded without proper unsubscription, old handlers persist. The `OnDisable` unsubscription above handles this correctly for Play Mode restart.

---

## 2. FSM Implementation Pattern

### Enum + Switch (Standard Unity Pattern)

```csharp
// Confidence: HIGH — established pattern from CONTEXT.md code_context, standard Unity FSM
public enum EnemyState { Idle, Chase, Telegraph, Attack }

private EnemyState _state = EnemyState.Idle;
private Coroutine  _attackCoroutine;

private void Update()
{
    switch (_state)
    {
        case EnemyState.Idle:      UpdateIdle();      break;
        case EnemyState.Chase:     UpdateChase();     break;
        case EnemyState.Telegraph: /* coroutine owns this state */ break;
        case EnemyState.Attack:    /* coroutine owns this state */ break;
    }
}
```

**State transition rules:**
- `Idle → Chase`: player enters detection radius (OverlapCircleNonAlloc check in UpdateIdle)
- `Chase → Telegraph`: player within attack range AND line-of-sight (optional for prototype)
- `Telegraph → Attack`: coroutine completes 0.8s wait (WaitForSecondsRealtime)
- `Attack → Idle` or `Attack → Chase`: coroutine completes, check if player still in range
- Any state → Idle: `OnPlayerDeath` fired (enemy stops pursuing dead player)

### Detection Query (No LINQ)

```csharp
// Pre-allocated buffer per enemy — same pattern as CombatController._hitBuffer
private readonly Collider2D[] _detectionBuffer = new Collider2D[4];
private int _playerLayerMask;

private void Awake()
{
    // Cache once — avoid NameToLayer in Update (ROADMAP constraint)
    _playerLayerMask = LayerMask.GetMask("PlayerHurtbox");
}

private bool IsPlayerInRange(float radius)
{
    int count = Physics2D.OverlapCircle(
        (Vector2)transform.position, radius,
        new ContactFilter2D { layerMask = _playerLayerMask, useTriggers = false, useLayerMask = true },
        _detectionBuffer);
    return count > 0;
}
```

**Why PlayerHurtbox mask:** Only detects non-invincible player. When player is in `PlayerInvincible` layer (rolling, post-dash), the enemy ignores them for detection (D-16). This is correct behavior — don't chase an invincible player.

**Note:** `Physics2D.OverlapCircle` with `ContactFilter2D` is the non-alloc variant. The method signature is `int OverlapCircle(Vector2 point, float radius, ContactFilter2D contactFilter, Collider2D[] results)`.

### Patrol (Idle State)

Simple left/right bounce:
```csharp
[SerializeField] private float patrolSpeed    = 2f;
[SerializeField] private float patrolHalfRange = 3f;

private Vector3 _spawnPosition;
private float   _patrolDir = 1f;

private void UpdateIdle()
{
    // Move in current direction
    float newX = transform.position.x + _patrolDir * patrolSpeed * Time.deltaTime;

    // Bounce at patrol boundary
    if (Mathf.Abs(newX - _spawnPosition.x) >= patrolHalfRange)
        _patrolDir *= -1f;

    transform.position = new Vector3(newX, transform.position.y, transform.position.z);

    // Detection check
    if (IsPlayerInRange(detectionRadius))
        TransitionTo(EnemyState.Chase);
}
```

**Note:** Enemy movement uses `transform.position` directly, not Rigidbody2D.MovePosition. Reason: enemies are Kinematic (confirmed by quick task 260605-r61 which set DummyEnemy's Rigidbody2D to Kinematic). Kinematic bodies should use `MovePosition` for physics interactions, but for simple patrol a direct transform move is acceptable for a prototype. If enemies need to interact with physics (e.g. not fall off platforms), `Rigidbody2D.MovePosition` is required. **Recommendation: use Rigidbody2D.MovePosition for consistency with the constraint.**

---

## 3. Projectile System

### ProjectileController Setup

```csharp
// Rigidbody2D settings (set in Awake or via Inspector):
//   Body Type: Dynamic
//   Collision Detection: Continuous  (fast projectile — tunneling risk)
//   Interpolate: Interpolate
//   Gravity Scale: 0                 (straight horizontal flight)
//   Constraints: FreezeRotation Z

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ProjectileController : MonoBehaviour
{
    [SerializeField] private float speed       = 10f;
    [SerializeField] private float maxDistance = 20f;

    private Rigidbody2D _rb;
    private Vector2     _startPosition;
    private Vector2     _direction;
    private int         _playerLayerMask;

    public void Init(Vector2 direction)
    {
        _direction     = direction.normalized;
        _startPosition = transform.position;
        _rb.linearVelocity = _direction * speed;
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _playerLayerMask = LayerMask.GetMask("PlayerHurtbox");
    }

    private void FixedUpdate()
    {
        // Distance-based lifetime — no Update timer (physics already ticking)
        if (Vector2.Distance(transform.position, _startPosition) >= maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Hit Platform — destroy on wall/floor contact
        if (other.gameObject.layer == LayerMask.NameToLayer("Platform"))
        {
            Destroy(gameObject);
            return;
        }

        // Hit Player (only PlayerHurtbox layer registers — D-16)
        if (other.CompareTag("Player"))
        {
            PlayerController.OnPlayerDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}
```

**Layer assignment for projectile GameObject:** Set to `EnemyProjectile` layer (layer 11, to be added).

**Collider2D:** Use `CircleCollider2D` (radius ~0.1f) or `BoxCollider2D`. Must be `isTrigger = true`.

**Lifetime approach:** `maxDistance` check in `FixedUpdate` is simple and reliable. Alternative: `Destroy(gameObject, lifeTimeSeconds)` — but distance-based is more predictable in a platformer where screen width matters more than time.

### Layer Collision Matrix for EnemyProjectile

The projectile must:
- Collide with (trigger): `PlayerHurtbox` — kills player
- Collide with (trigger): `Platform` — destroys projectile
- NOT collide with: `Enemy`, `PlayerInvincible`, `EnemyProjectile`, `Default`

This requires configuring the 2D Physics collision matrix in `ProjectSettings/Physics2DSettings.asset`. Currently `m_LayerCollisionMatrix` is all `f` (everything collides with everything). The planner must include a step to configure this via `Edit > Project Settings > Physics 2D`.

---

## 4. Telegraph Mechanics

### `!` Icon via Child SpriteRenderer (D-05 decision confirmed)

The CONTEXT.md specifics section confirms: "WorldSpace Canvas보다 자식 SpriteRenderer에 `!` 텍스처가 더 단순함 — placeholder 환경에서 Canvas 오버헤드 불필요."

**Implementation:**
```csharp
[SerializeField] private SpriteRenderer _exclamationIcon; // assign child SR in Inspector
// icon is a child GameObject positioned above the enemy's head

private IEnumerator TelegraphCoroutine()
{
    _state = EnemyState.Telegraph;
    _exclamationIcon.enabled = true;

    // 0.8 real seconds — unscaledDeltaTime (D-06)
    yield return new WaitForSecondsRealtime(0.8f);

    _exclamationIcon.enabled = false;
    TransitionToAttack();
}
```

**Sprite:** Use Unity's built-in font texture or any `!` sprite. For placeholder, a white square with text is sufficient. The SpriteRenderer can display a `Sprite` asset created from a simple white circle or the default Unity sprite with a "!" text rendered via `TextMesh` on the same child. For pure simplicity, attach a `TextMeshPro` component to the child instead of SpriteRenderer — but that adds a dependency. A `!` sprite is simpler.

**Recommendation:** Create a white `!` sprite in the editor using `Create > Sprites > Square`, then write "!" on it, OR use a GUIStyle in `OnDrawGizmos` for editor visibility only. For runtime: a `SpriteRenderer` with a `!` texture asset.

**Alternative:** Create a 32x32 pixel white texture with "!" drawn, import as Sprite. Zero overhead. **This is the correct approach for prototype scope.**

### RangedEnemy Aim LineRenderer (D-09)

Reuse pattern from `RangeDisplay.cs`:
```csharp
private LineRenderer _aimLine;
private float        _telegraphElapsed;
private const float  TelegraphDuration = 0.8f;

private IEnumerator RangedTelegraphCoroutine(Vector2 direction)
{
    _state = EnemyState.Telegraph;
    _aimLine.enabled = true;

    float elapsed = 0f;
    while (elapsed < TelegraphDuration)
    {
        elapsed += Time.unscaledDeltaTime;
        float alpha = elapsed / TelegraphDuration; // 0→1
        Color c = Color.red;
        c.a = alpha;
        _aimLine.startColor = _aimLine.endColor = c;

        // Update line endpoint (player may have moved)
        Vector2 origin = transform.position;
        _aimLine.SetPosition(0, origin);
        _aimLine.SetPosition(1, origin + direction * aimLineLength);
        yield return null;
    }

    _aimLine.enabled = false;
    FireProjectile(direction);
    _state = EnemyState.Idle; // or Chase
}
```

**LineRenderer setup:** `positionCount = 2`, `useWorldSpace = true`, `startWidth = endWidth = 0.05f`, `startColor = endColor = Color.red` (alpha starts at 0). No material override needed — use default Sprites/Default material.

**Direction lock vs. tracking:** The CONTEXT.md does not specify. Recommendation: lock aim direction when Telegraph starts (capture player position at that frame). This makes the telegraph readable and allows the player to move out of the way after the aim is shown — which is the correct game-feel decision.

---

## 5. Layer Matrix

### Current State (verified from TagManager.asset)

| Layer Index | Name | Status |
|-------------|------|--------|
| 0 | Default | Existing |
| 7 | PlayerHurtbox | Existing |
| 8 | PlayerInvincible | Existing |
| 9 | Platform | Existing |
| 10 | Enemy | Existing |
| 11 | (empty) | **Available — add EnemyProjectile here** |

### Required Addition

**Add `EnemyProjectile` at layer index 11.** This is the only new layer needed.

**In TagManager.asset**, the currently empty slot at index 11 (shown as `-` in the file) receives this name.

### Physics2D Collision Matrix Configuration Required

The current matrix is all-enabled (all `f` hex = every layer pair collides). This means enemy projectiles will currently trigger on everything including other enemies. The planner must include a task to disable unwanted pairs via the Unity Editor Physics 2D settings.

**Required collisions for EnemyProjectile (layer 11):**
- Enable: EnemyProjectile ↔ PlayerHurtbox (trigger = kill player)
- Enable: EnemyProjectile ↔ Platform (trigger = destroy projectile)
- Disable: EnemyProjectile ↔ Enemy
- Disable: EnemyProjectile ↔ PlayerInvincible
- Disable: EnemyProjectile ↔ EnemyProjectile
- Disable: EnemyProjectile ↔ Default (if Default doesn't contain platforms — but Platform is separate layer, so Default is fine to disable)

**Also verify Enemy layer doesn't collide with PlayerInvincible:**
- Disable: Enemy ↔ PlayerInvincible (melee hitbox must not fire when player is rolling)
- This is the D-16 implementation.

**Note:** `InvincibilityHandler` already performs the layer swap (`PlayerHurtbox` ↔ `PlayerInvincible`). The layer collision matrix must match: `Enemy` and `EnemyProjectile` must NOT collide with `PlayerInvincible`. This is currently not configured (everything collides). **This is a blocking task that must appear in Wave 0 / Plan 1.**

### Hardcoded Layer Constants Pattern

Following the established pattern in `InvincibilityHandler.cs`:
```csharp
// In MeleeEnemy.cs, RangedEnemy.cs, ProjectileController.cs
private const int LayerPlayerHurtbox    = 7;
private const int LayerPlayerInvincible = 8;
private const int LayerPlatform         = 9;
private const int LayerEnemy            = 10;
private const int LayerEnemyProjectile  = 11;
```

---

## 6. Plan Decomposition

### Recommended: 4 Plans

**Why 4:** Each plan is independently compilable. The IEnemy refactor in Plan 1 unblocks Plans 3 and 4 in parallel. Plan 2 is independent of enemy types. Plans 3 and 4 each address one requirement (ENMY-01, ENMY-02).

---

### Plan 03-01: IEnemy Interface + DummyEnemy + CombatController Refactor

**Scope:** Define the IEnemy contract, make DummyEnemy implement it, update CombatController's 4 type references.

**Files:**
- `Assets/Scripts/Enemy/IEnemy.cs` — new (interface definition)
- `Assets/Scripts/Enemy/DummyEnemy.cs` — add `: IEnemy`
- `Assets/Scripts/Player/CombatController.cs` — change `DummyEnemy` → `IEnemy` in 5 places

**Tasks:**
1. Create `IEnemy.cs` with `IsAlive`, `OnDashHit()`, `ClearHighlight()` members
2. Add `: IEnemy` to `DummyEnemy` class declaration (already satisfies all 3 members — zero body changes)
3. In `CombatController`: change `_lastHighlighted` field type, `DashOrWhiff` parameter type, `ExecuteDash` parameter type, `FindNearestEnemyInRange` return type + `GetComponent<DummyEnemy>()` → `GetComponent<IEnemy>()`, `UpdateHighlight` parameter type

**Verification:** Play Mode — DummyEnemy is still targetable and killable. Hit-freeze fires. Highlight works.

**Risk:** If `UpdateHighlight` calls `nearest.GetComponent<SpriteRenderer>()` via MonoBehaviour cast, it fails silently if `nearest` is not a MonoBehaviour. Since all IEnemy implementors in this project ARE MonoBehaviours, the cast is safe. Document the assumption.

---

### Plan 03-02: PlayerDeath Event + FallDetector Rewrite (D-13, D-14, D-17)

**Scope:** Add `OnPlayerDeath` static event to PlayerController, create `PlayerDeathHandler`, rewrite FallDetector to fire death instead of teleport. Also: add EnemyProjectile layer to TagManager, configure Physics2D collision matrix.

**Files:**
- `Assets/Scripts/Player/PlayerController.cs` — add `static event Action OnPlayerDeath`
- `Assets/Scripts/Player/PlayerDeathHandler.cs` — new (subscribes, calls SetActive(false))
- `Assets/Scripts/Player/FallDetector.cs` — remove teleport logic, fire OnPlayerDeath
- `ProjectSettings/TagManager.asset` — add `EnemyProjectile` at layer 11
- Physics2D collision matrix — configure via Editor (Enemy ↔ PlayerInvincible: off; EnemyProjectile ↔ PlayerInvincible: off)

**Tasks:**
1. Add `public static event Action OnPlayerDeath;` to PlayerController
2. Create PlayerDeathHandler (OnEnable/OnDisable subscribe pattern, SetActive(false) + Debug.Log)
3. Rewrite FallDetector.OnFall() to invoke OnPlayerDeath; remove _lastSafePosition, _invincibility
4. Add layer + configure matrix in Editor

**Verification:** Fall into FallZone → console shows "Player died", player GameObject disabled. Play Mode restart to reset.

**Dependency:** This plan is independent of Plan 03-01. Can be executed in parallel by a separate agent, or after Plan 1.

---

### Plan 03-03: MeleeEnemy FSM (ENMY-01)

**Depends on:** Plan 03-01 (IEnemy interface), Plan 03-02 (PlayerDeath event + layer matrix)

**Scope:** Create MeleeEnemy with 4-state FSM, patrol, detection, telegraph (`!` icon), hitbox attack.

**Files:**
- `Assets/Scripts/Enemy/MeleeEnemy.cs` — new
- `Assets/Prefabs/MeleeEnemy.prefab` — new (or configure in scene directly)
- SpriteRenderer `!` icon child GameObject (in scene/prefab)

**Tasks:**
1. Create MeleeEnemy.cs implementing IEnemy
2. Implement FSM: Idle (patrol), Chase (move toward player), Telegraph (0.8s coroutine + `!` show), Attack (trigger hitbox Collider2D active 0.1s)
3. OnTriggerEnter2D on hitbox: if `other.CompareTag("Player")` → `PlayerController.OnPlayerDeath?.Invoke()`
4. OnDashHit(): set IsAlive=false, disable self
5. Subscribe to PlayerController.OnPlayerDeath in OnEnable — return to Idle on death
6. Place in SampleScene, test

**Verification:** ENMY-01 success criteria — player can read the `!` and roll through the attack window.

---

### Plan 03-04: RangedEnemy FSM + ProjectileController (ENMY-02)

**Depends on:** Plan 03-01 (IEnemy interface), Plan 03-02 (PlayerDeath event + layer matrix)

**Scope:** Create RangedEnemy with LineRenderer aim telegraph and projectile firing, plus ProjectileController.

**Files:**
- `Assets/Scripts/Enemy/RangedEnemy.cs` — new
- `Assets/Scripts/Enemy/ProjectileController.cs` — new
- `Assets/Prefabs/RangedEnemy.prefab` — new (or scene config)
- `Assets/Prefabs/Projectile.prefab` — new

**Tasks:**
1. Create ProjectileController: Init(direction), FixedUpdate distance check, OnTriggerEnter2D (player kill + platform destroy)
2. Create RangedEnemy implementing IEnemy: Idle (patrol), Chase (detect player — but moveSpeed=0f so stays put), Telegraph (LineRenderer 0→1 alpha over 0.8s WaitForSecondsRealtime), Attack (Instantiate projectile, fire)
3. Create Projectile prefab: Rigidbody2D (Dynamic, Continuous, Gravity=0), CircleCollider2D (isTrigger), ProjectileController, layer=EnemyProjectile
4. Place RangedEnemy in SampleScene
5. Verify aim line appears before projectile, projectile travels straight, player dies on contact

**Verification:** ENMY-02 success criteria — aim line visible before projectile launches, player can dodge.

---

## 7. Validation Architecture

`nyquist_validation: true` is set in `.planning/config.json`. However, the quick task `260604-vst` explicitly removed the test runner in favor of editor playtesting. The Roadmap note "02-04-PLAN.md — Test infrastructure" remains uncompleted (Phase 2 plan 4 is not checked off). Given this context, validation for Phase 3 is **editor Play Mode verification** rather than automated NUnit tests.

### Test Framework Status

| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.6.0 (NUnit) — installed but deliberately bypassed (260604-vst) |
| Config file | None active for Phase 3 |
| Quick run command | N/A — editor Play Mode |
| Full suite command | N/A |

### Phase Requirements → Verification Map

| Req ID | Behavior | Test Type | Verification Method |
|--------|----------|-----------|---------------------|
| ENMY-01 | Melee enemy detects, chases, telegraphs `!`, attacks — player can roll through | Manual Play Mode | Enter scene, wait for `!`, roll during telegraph window — no damage |
| ENMY-01 | Player dash-kill on melee enemy fires FEEL-01 hit-freeze | Manual Play Mode | Dash into melee enemy — 75ms freeze visible |
| ENMY-01 | Melee hit kills player (one-shot) | Manual Play Mode | Stand still during melee attack — player GameObject disables, "Player died" in console |
| ENMY-02 | Ranged enemy shows aim line before projectile | Manual Play Mode | Aim line fades in over 0.8s before projectile spawns |
| ENMY-02 | Projectile travels straight, destroys on Platform contact | Manual Play Mode | Observe projectile path, watch destruction on wall |
| ENMY-02 | Projectile hit kills player | Manual Play Mode | Stand in projectile path — player disabled |
| ENMY-02 | Player dash-kill on ranged enemy fires FEEL-01 | Manual Play Mode | Dash into ranged enemy |
| Both | Player in roll (invincible) is not hit by melee or projectile | Manual Play Mode | Roll through attack window — no death |

### Wave 0 Gaps

No automated test files are required for this phase. The planner should note:
- [ ] Physics2D collision matrix must be configured before enemy scripts are testable
- [ ] EnemyProjectile layer (11) must be added to TagManager before ProjectileController compiles cleanly
- [ ] `!` sprite asset must exist before MeleeEnemy prefab can be created

---

## 8. Risk Flags

### Risk 1: CombatController Type Mismatch (HIGH)
**What:** `ExecuteDash(IEnemy target)` calls `target.transform.position`. `IEnemy` doesn't extend `UnityEngine.Object`, so `.transform` doesn't exist on the interface.
**Mitigation:** Cast to `MonoBehaviour` inside ExecuteDash: `((MonoBehaviour)target).transform.position`. Document this assumption. All IEnemy implementors in this project are MonoBehaviours. If the cast fails, it will throw a visible NullReferenceException in the editor — easy to catch.

### Risk 2: Static Event Stale Subscription (MEDIUM)
**What:** `PlayerController.OnPlayerDeath` is `static`. If Play Mode is stopped and restarted without a domain reload, old subscriptions persist and fire twice.
**Mitigation:** `PlayerDeathHandler` uses `OnEnable`/`OnDisable` subscription pattern (already shown above). Unity 6 preserves domain reloads by default — this is only a risk if "Domain Reload on Play" is disabled in Editor settings. Verify it's enabled (default in this project, no indication it was changed).

### Risk 3: Enemy Hitbox Fires While Player is Invincible (MEDIUM)
**What:** The Physics2D collision matrix is currently all-enabled. `Enemy` layer hits `PlayerInvincible` layer until the matrix is configured. This means rolling through a melee attack still kills the player until the matrix is fixed.
**Mitigation:** Plan 03-02 MUST configure the collision matrix before MeleeEnemy is testable. This is a blocking dependency.

### Risk 4: Projectile Layer Not Assigned (HIGH)
**What:** `ProjectileController` checks `other.gameObject.layer == LayerMask.NameToLayer("Platform")`. If `EnemyProjectile` layer 11 isn't added to TagManager yet, `LayerMask.GetMask("EnemyProjectile")` returns 0 (no layer) and projectiles won't be filtered correctly.
**Mitigation:** Layer addition is in Plan 03-02 (infrastructure plan). Plans 03-03 and 03-04 depend on 03-02 being complete. The hardcoded constant `private const int LayerEnemyProjectile = 11` avoids the string lookup risk at runtime.

### Risk 5: DummyEnemy Respawn vs. IEnemy.IsAlive Inconsistency (LOW)
**What:** DummyEnemy currently respawns after 2 seconds. New enemies (MeleeEnemy, RangedEnemy) call `SetActive(false)` on dash hit and do not respawn. If both types are in the scene simultaneously, the highlight system may try to target a dead DummyEnemy that is mid-respawn (IsAlive=false, but collider will re-enable after 1 frame).
**Mitigation:** `FindNearestEnemyInRange()` already checks `!enemy.IsAlive` and skips it. No new code needed. DummyEnemy can stay in scene safely.

### Risk 6: RangedEnemy Chase with moveSpeed=0f (LOW)
**What:** With `moveSpeed = 0f`, the Chase state does nothing useful — enemy detects player but doesn't move. If Chase→Telegraph transition is distance-based ("within attack range"), and the enemy starts outside attack range, it will never transition.
**Mitigation:** For RangedEnemy, make the detection radius equal the attack/telegraph trigger distance. When player enters detection range, immediately transition to Telegraph (skip Chase or make Chase = immediate telegraph). OR: Chase state for RangedEnemy simply waits until player is in aim range, then telegraphs. Since moveSpeed=0, the Chase state can still check aim range and transition. Document this clearly in the plan.

### Risk 7: Time.timeScale=0 During Telegraph (LOW)
**What:** If slow-motion is active (timeScale ≈ 0.2f) when Telegraph begins, a `WaitForSecondsRealtime(0.8f)` correctly waits 0.8 real seconds. But if timeScale=0 (hit-freeze during another kill), the Coroutine pauses because `yield return null` inside the alpha-lerp loop IS affected by timeScale=0.
**Mitigation:** Use `WaitForSecondsRealtime` for the main 0.8s delay (already decided). For the LineRenderer alpha lerp loop that uses `yield return null`, replace `Time.unscaledDeltaTime` accumulation instead of `Time.deltaTime`. The alpha lerp uses `Time.unscaledDeltaTime` — the `yield return null` inside the while loop will still execute every frame even during timeScale=0 in Unity 6. Confirm: `yield return null` waits one frame regardless of timeScale. `Time.unscaledDeltaTime` gives the correct real frame time. This is safe.

---

## Architecture Patterns

### Recommended Project Structure (Enemy scripts)

```
Assets/Scripts/Enemy/
├── IEnemy.cs              # Interface — IsAlive, OnDashHit(), ClearHighlight()
├── DummyEnemy.cs          # Existing — add : IEnemy
├── MeleeEnemy.cs          # New — FSM, patrol, telegraph, hitbox attack
├── RangedEnemy.cs         # New — FSM, aim line, projectile fire
└── ProjectileController.cs # New — Rigidbody2D straight, lifetime, player kill
```

### Anti-Patterns to Avoid

- **FindObjectsOfType in Update:** Never. Use OverlapCircleNonAlloc with pre-allocated buffer.
- **LINQ in Update (`.Where`, `.OrderBy`, `.First`):** Never. Use manual loop.
- **Physics2D.IgnoreLayerCollision:** Never. Use layer swap (established project constraint).
- **WaitForSeconds for telegraph/attack timers:** Never. Use WaitForSecondsRealtime — the telegraph MUST be exactly 0.8 real seconds regardless of slow-motion state.
- **Coroutine without IsAlive guard:** If enemy is dash-killed during Telegraph coroutine, the coroutine must check `IsAlive` before transitioning to Attack. Add `if (!IsAlive) yield break;` after the WaitForSecondsRealtime.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Telegraph timing | Custom timer class | `WaitForSecondsRealtime` coroutine | Already established — timeScale-immune, cleaner than deltaTime accumulation |
| Aim line rendering | Custom mesh | `LineRenderer` with `positionCount=2` | Established by RangeDisplay — same component, same API |
| Invincibility during roll | Custom flag | `InvincibilityHandler.IsInvincible` | Already implemented, already tested |
| Enemy targeting | Manual distance loop with allocation | `Physics2D.OverlapCircle` + pre-allocated buffer | Established by CombatController._hitBuffer pattern |
| Player death notification | Polling/FindObjectOfType | `static event Action OnPlayerDeath` | Pub-sub — zero coupling, Phase 4 subscribes without modifying Phase 3 code |

---

## Code Examples

### IEnemy Interface
```csharp
// Assets/Scripts/Enemy/IEnemy.cs
public interface IEnemy
{
    bool IsAlive { get; }
    void OnDashHit();
    void ClearHighlight();
}
```

### DummyEnemy — Add IEnemy (zero body changes)
```csharp
// One line change
public class DummyEnemy : MonoBehaviour, IEnemy
// All three interface members already exist as public methods/properties
```

### CombatController — _lastHighlighted field change
```csharp
// Before:
private DummyEnemy _lastHighlighted;
// After:
private IEnemy _lastHighlighted;
```

### CombatController — UpdateHighlight with MonoBehaviour cast
```csharp
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
```

### PlayerController — OnPlayerDeath event
```csharp
using System;
// Add to PlayerController class body:
public static event Action OnPlayerDeath;
```

### FallDetector — After D-17 rewrite
```csharp
public class FallDetector : MonoBehaviour
{
    // D-17: Fall = death. No teleport recovery. No last-safe-position tracking.
    public void OnFall()
    {
        PlayerController.OnPlayerDeath?.Invoke();
    }
}
```

### MeleeEnemy — Core FSM skeleton
```csharp
public class MeleeEnemy : MonoBehaviour, IEnemy
{
    [SerializeField] private float detectionRadius  = 10f;
    [SerializeField] private float attackRange      = 1.5f;
    [SerializeField] private float chaseSpeed       = 4f;
    [SerializeField] private SpriteRenderer _exclamationIcon;
    [SerializeField] private Collider2D _meleeHitbox;

    private EnemyState         _state = EnemyState.Idle;
    private readonly Collider2D[] _detectionBuffer = new Collider2D[4];
    private int                _playerMask;
    private Transform          _playerTransform;
    private Coroutine          _attackCoroutine;

    public bool IsAlive { get; private set; } = true;

    private void Awake()
    {
        _playerMask = LayerMask.GetMask("PlayerHurtbox");
        _meleeHitbox.enabled = false;
        _exclamationIcon.enabled = false;
    }

    private void OnEnable()  => PlayerController.OnPlayerDeath += OnPlayerDied;
    private void OnDisable() => PlayerController.OnPlayerDeath -= OnPlayerDied;

    private void OnPlayerDied()
    {
        if (_attackCoroutine != null) StopCoroutine(_attackCoroutine);
        _state = EnemyState.Idle;
    }

    public void OnDashHit()
    {
        if (!IsAlive) return;
        IsAlive = false;
        gameObject.SetActive(false);
    }

    public void ClearHighlight()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = Color.white;
    }
    // ... FSM Update, patrol, chase, telegraph coroutine
}
```

### ProjectileController — Core
```csharp
public class ProjectileController : MonoBehaviour
{
    [SerializeField] private float speed       = 10f;
    [SerializeField] private float maxDistance = 20f;

    private Rigidbody2D _rb;
    private Vector2     _startPosition;
    private const int   LayerPlatform = 9;

    private void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void Init(Vector2 direction)
    {
        _startPosition = _rb.position;
        _rb.linearVelocity = direction.normalized * speed;
    }

    private void FixedUpdate()
    {
        if (Vector2.SqrMagnitude(_rb.position - _startPosition) >= maxDistance * maxDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerPlatform)
        { Destroy(gameObject); return; }

        if (other.CompareTag("Player"))
        {
            PlayerController.OnPlayerDeath?.Invoke();
            Destroy(gameObject);
        }
    }
}
```

---

## Environment Availability

Step 2.6: SKIPPED — Phase 3 is purely code/scene changes within an existing Unity project. No external CLI tools, services, or runtimes beyond the already-confirmed Unity 6000.3.11f1 installation.

---

## Sources

### Primary (HIGH confidence)
- Direct code inspection: `Assets/Scripts/Player/CombatController.cs` — exact field types, method signatures, DummyEnemy references
- Direct code inspection: `Assets/Scripts/Enemy/DummyEnemy.cs` — existing IEnemy member pattern
- Direct code inspection: `Assets/Scripts/Player/InvincibilityHandler.cs` — layer constants, layer swap pattern
- Direct code inspection: `Assets/Scripts/Player/FallDetector.cs` — current OnFall logic to be replaced
- Direct code inspection: `Assets/Scripts/Player/PlayerController.cs` — insertion point for OnPlayerDeath
- Direct code inspection: `Assets/Scripts/Player/RangeDisplay.cs` — LineRenderer pattern for RangedEnemy aim line
- `ProjectSettings/TagManager.asset` — confirmed layer indices 7-10, slot 11 available
- `ProjectSettings/Physics2DSettings.asset` — confirmed all-enabled collision matrix (all `f`)
- `.planning/phases/03-enemy-system/03-CONTEXT.md` — all locked decisions

### Secondary (MEDIUM confidence)
- `.planning/STATE.md` — established constraints (OverlapCircleNonAlloc, WaitForSecondsRealtime, layer swap pattern)
- `.planning/ROADMAP.md` — success criteria SC 1-4 for Phase 3

---

## Metadata

**Confidence breakdown:**
- Integration analysis: HIGH — derived from direct source inspection
- FSM pattern: HIGH — established in CONTEXT.md, standard Unity pattern
- Projectile system: HIGH — standard Rigidbody2D usage, no novel APIs
- Layer matrix: HIGH — direct inspection of TagManager.asset and Physics2DSettings.asset
- Telegraph timing: HIGH — WaitForSecondsRealtime already used throughout codebase

**Research date:** 2026-06-08
**Valid until:** Stable — changes only if CombatController is modified before planning begins

---

## RESEARCH COMPLETE
