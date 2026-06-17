# Phase 5: 절차적 맵 생성 — 무한 스테이지 - Research

**Researched:** 2026-06-17
**Domain:** Unity 6 chunk-based procedural level generation, trigger-driven floor transitions, enemy spawning
**Confidence:** HIGH (all findings from direct codebase inspection + established project patterns)

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions
- **D-01:** 출구 트리거(Trigger Collider2D)를 각 Room 프리팹의 위쪽에 자식 오브젝트로 배치. 플레이어가 밟으면 층 전환 시퀀스 시작.
- **D-02:** 적 처치 여부와 무관하게 언제든 출구를 밟으면 전환.
- **D-03:** 14개 Room 폴더 중 4~5개를 Unity Editor에서 수동으로 콘텐츠 채움. 나머지는 빈 폴더로 유지.
- **D-04:** 각 Room 프리팹에 포함할 요소: 플랫폼(Tilemap 또는 Sprite), 출구 트리거(자식 오브젝트), 적 스폰 포인트(빈 GameObject 태그).
- **D-05:** 1층은 항상 고정된 Room(Room_Combat 또는 전용 단순 Room). 2층부터 가중치 랜덤 선택.
- **D-06:** 스폰 포인트 기반 런타임 스폰 — Room 프리팹에 빈 `EnemySpawnPoint` 오브젝트만 배치, `FloorSpawner`가 층 번호를 읽어 MeleeEnemy/RangedEnemy를 동적 Instantiate.
- **D-07:** 난이도 스케일링 — 1~5층: 근접 위주 (MeleeEnemy 2~3마리, RangedEnemy 0~1마리) / 6~10층: 혼합 (MeleeEnemy 2, RangedEnemy 1~2) / 11층+: 원거리 비율 확대 (MeleeEnemy 2, RangedEnemy 2~3).
- **D-08:** 6단계 전환 시퀀스: (1) 입력 잠금 → (2) 순간이동 → (3) 카메라 Y스냅 → (4) 적 SetActive(true) → (5) 적 FSM 인식 활성화 → (6) 입력 재개.
- **D-09:** 전환 완료 직후 이전 층 Destroy (현재 층 + 미리 스폰된 다음 층만 씬에 유지).
- **D-10:** DeathScreenController.RestartGame()의 SceneManager.LoadScene(0) 유지 — 추가 코드 불필요.

### Claude's Discretion
- 각 층의 Room 높이 통일 여부 및 수치 (권장: 모든 Room 동일 높이 — Y 오프셋 계산 단순화)
- 스폰 Y 오프셋 계산 방식 (Room 높이 × 층 번호)
- 가중치 랜덤 선택 알고리즘 (단순 Random.Range 배열 인덱스로 충분)
- 다음 층 사전 스폰 타이밍 (플레이어가 현재 층 출구 진입 시)
- 적 수 구체적 수치 (D-07 예시 범위 내)

### Deferred Ideas (OUT OF SCOPE)
- 모바일 온스크린 컨트롤 (MOBI-01, MOBI-02) — v2 Requirements
- 층 난이도 커브 세밀 조정 — 플레이테스트 후
- Room 레이아웃 14개 완전 채우기 — v2 콘텐츠 확장
- 복잡한 순찰 경로/웨이포인트 적 AI — v2 범위
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| FLOOR-01 | 프리셋 기반 층 생성 (3~5개 프리셋 — 플랫폼/사다리/계단/낙사/혼합) | FloorSpawner: Room[] prefab array + weighted Random.Range selection; 1st floor fixed, 2nd+ random |
| FLOOR-02 | 위쪽 출구 도달 시 층 전환 시퀀스 발동 (6단계: 입력잠금→이동→카메라→가림막→인식→재개) | RoomExit Trigger → FloorSpawner.AdvanceFloor() Coroutine; PlayerController _inputLocked flag |
| FLOOR-03 | 층 전환 중 적 비활성화 — 카메라 전환 완료 후에만 플레이어 인식 시작 | Enemy spawned with SetActive(false); activated in step 4 of sequence |
| FLOOR-04 | 이전 층 제거/비활성화 (모바일 성능 — 현재+다음 층만 유지) | Destroy(previousRoomGO) after player teleport; only _currentRoom and _nextRoom live in scene |
</phase_requirements>

---

## Summary

Phase 5 implements chunk-based infinite floor generation within the existing Unity 6 / URP 2D prototype. The approach is intentionally minimal: a `FloorSpawner` MonoBehaviour manages three references (`_floor1RoomPrefab`, `_roomPool` weighted list, and `_currentRoom`), triggers a 6-step coroutine transition on exit collision, and destroys the previous room immediately after the player teleports. No procedural geometry — all "procedural" variety comes from selecting among 4~5 hand-authored Room prefabs by index.

The codebase already has every building block: `FloorManager.CurrentFloor` (static int, wired to HUD and death screen), `CameraFollow` (LateUpdate direct follow, snaps instantly on position change), `PlayerController` (has no input lock yet — **this is the one gap**), and both enemy FSMs (`MeleeEnemy`/`RangedEnemy`) which already support `SetActive(false/true)` via `OnEnable`/`OnDisable` subscriptions to `PlayerController.OnPlayerDeath`.

**Primary recommendation:** Implement `FloorSpawner` as a new MonoBehaviour scene singleton. Add `_inputLocked` bool to `PlayerController`. Keep transition sequence as a single Coroutine using `WaitForSecondsRealtime` for all timing. Room height is fixed at 18 Unity units — makes Y offset `CurrentFloor * 18f` trivially correct.

---

## Standard Stack

### Core
| Component | Source | Purpose | Why This Approach |
|-----------|--------|---------|-------------------|
| `FloorSpawner` (new MonoBehaviour) | New file | Owns spawn loop, exit detection dispatch, transition Coroutine | Keeps FloorManager as pure data class (established pattern from 04-01) |
| `FloorManager` (existing static class) | `Assets/Scripts/World/FloorManager.cs` | `CurrentFloor` int — HUD reads it every Update | Already wired to HUDController and DeathScreenController; no change needed |
| `RoomExit` (new MonoBehaviour) | New file, child of Room prefab | Detects player in `OnTriggerEnter2D`, calls `FloorSpawner.AdvanceFloor()` | One-way dependency: Room tells Spawner; Spawner doesn't poll |
| `Collider2D` (Trigger, Is Trigger = true) | Unity built-in | Exit zone at top of each Room prefab | Standard Unity trigger pattern; zero overhead |
| `Random.Range(0, pool.Length)` | UnityEngine | Weighted room selection | Sufficient for 4~5 item pool; no third-party needed |

### Supporting
| Component | Version/Source | Purpose | When to Use |
|-----------|---------------|---------|-------------|
| `WaitForSecondsRealtime` | Unity built-in | All transition step delays | Mandatory — timeScale may be 0 during slow-mo; `WaitForSeconds` would hang |
| `Coroutine` on FloorSpawner | Unity built-in | Sequence the 6-step transition | Already established pattern (MeleeEnemy, RangedEnemy telegraph coroutines) |
| `PlayerController._inputLocked` bool | Extend existing | Block movement/jump/attack during transition | Simplest gate — read in FixedUpdate before ApplyMovement |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Fixed room height | Variable-height rooms | Variable adds per-room height metadata, Y offset complexity; no prototype benefit |
| `Random.Range` index | `WeightedRandom<T>` struct | Overkill for 4~5 items; simple index array achieves weighted selection via repeated entries |
| Scene-level Coroutine | UniTask / async-await | No UniTask in project; Coroutine is project standard |
| `Destroy(previousRoom)` immediately | Object pool | Pool adds complexity with no mobile benefit at 2-room budget |

**Installation:** No new packages required. All implementation is new C# scripts using existing Unity built-ins.

---

## Architecture Patterns

### Recommended Project Structure
```
Assets/
├── Scripts/
│   ├── World/
│   │   ├── FloorManager.cs          (existing — static data, no change)
│   │   ├── FloorSpawner.cs          (NEW — MonoBehaviour, scene singleton)
│   │   └── RoomExit.cs              (NEW — child of Room prefab, reports to FloorSpawner)
│   └── Player/
│       └── PlayerController.cs      (MODIFY — add _inputLocked flag + LockInput/UnlockInput)
└── Prefabs/
    └── Rooms/
        ├── Room_Combat/             (existing folder — add prefab here for Floor 1 fixed)
        ├── Room_Chase/              (add prefab — pool candidate)
        ├── Room_Dodge/              (add prefab — pool candidate)
        ├── Room_Gap/                (add prefab — pool candidate)
        ├── Room_Mixed/              (add prefab — pool candidate)
        └── [10 remaining folders]  (stay empty — v2 expansion)
```

### Pattern 1: FloorSpawner as Scene Singleton (MonoBehaviour)

**What:** Single MonoBehaviour attached to a `FloorSpawner` empty GameObject in SampleScene. Exposes `AdvanceFloor()` which runs the 6-step coroutine.

**When to use:** When you need scene lifecycle (Awake spawns floor 1, OnDestroy cleanup) but also need a reference-able instance for RoomExit to call.

**Example:**
```csharp
// FloorSpawner.cs
public class FloorSpawner : MonoBehaviour
{
    [SerializeField] private GameObject   _floor1RoomPrefab;   // D-05: fixed floor 1
    [SerializeField] private GameObject[] _roomPool;           // D-03: 4~5 Room prefabs (weighted by repetition)
    [SerializeField] private Transform    _playerTransform;
    [SerializeField] private float        _roomHeight = 18f;   // Claude's discretion: fixed height

    [SerializeField] private MeleeEnemy   _meleeEnemyPrefab;
    [SerializeField] private RangedEnemy  _rangedEnemyPrefab;

    private GameObject _currentRoom;
    private bool       _transitioning;

    public static FloorSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        SpawnRoom(_floor1RoomPrefab, 1); // D-05: floor 1 always fixed
    }

    // Called by RoomExit.OnTriggerEnter2D
    public void AdvanceFloor()
    {
        if (_transitioning) return; // guard double-trigger
        StartCoroutine(FloorTransitionSequence());
    }
}
```

### Pattern 2: Room Prefab Structure

**What:** Each Room prefab has three types of child objects — platform geometry, one `RoomExit` child with Trigger Collider2D, and zero or more `EnemySpawnPoint` empty GameObjects.

**When to use:** Always — this is the D-04 locked decision.

**Example Room Prefab Hierarchy:**
```
Room_Combat (GameObject — root, positioned at Y = floor * roomHeight)
├── Platforms (child)
│   ├── Ground (SpriteRenderer + BoxCollider2D, Platform layer)
│   └── Platform_A (SpriteRenderer + BoxCollider2D)
├── RoomExit (child — RoomExit script + BoxCollider2D Is Trigger = true)
│   └── [positioned at top of room, full room width]
├── EnemySpawnPoint_0 (empty GameObject, tag = "EnemySpawnPoint")
├── EnemySpawnPoint_1 (empty GameObject, tag = "EnemySpawnPoint")
└── FallZone (child — FallZoneTrigger + BoxCollider2D Is Trigger = true, at room bottom)
```

### Pattern 3: RoomExit Trigger (One-Shot)

**What:** Thin script on the exit child object. Detects player tag, calls `FloorSpawner.AdvanceFloor()` once. The `FloorSpawner._transitioning` guard prevents double-fire.

```csharp
// RoomExit.cs
public class RoomExit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        FloorSpawner.Instance?.AdvanceFloor();
    }
}
```

### Pattern 4: 6-Step Transition Coroutine

**What:** Runs entirely in `WaitForSecondsRealtime` to survive `Time.timeScale = 0` (hit-freeze). The sequence is:

```csharp
private IEnumerator FloorTransitionSequence()
{
    _transitioning = true;

    // Step 1: Lock input
    PlayerController.Instance.LockInput();

    // Step 2: Teleport player to new room spawn point
    FloorManager.CurrentFloor++;
    GameObject nextRoom = SpawnRoom(SelectRoom(), FloorManager.CurrentFloor);
    // Enemies spawned SetActive(false) inside SpawnRoom

    Transform spawnPoint = nextRoom.transform.Find("PlayerSpawn");
    _playerTransform.position = spawnPoint != null
        ? spawnPoint.position
        : new Vector3(_playerTransform.position.x, FloorManager.CurrentFloor * _roomHeight + 1f, 0f);

    // Step 3: Camera Y-snap — CameraFollow.LateUpdate handles this automatically
    // because CameraFollow tracks _playerTransform.position each LateUpdate.
    // No explicit camera code needed (established CameraFollow pattern).
    yield return null; // one frame for CameraFollow to catch up

    // Step 4: Activate enemies
    ActivateEnemies(nextRoom);

    // Step 5: Short delay so FSMs have one Update cycle before player can move
    yield return new WaitForSecondsRealtime(0.05f);

    // Step 6: Unlock input
    PlayerController.Instance.UnlockInput();

    // Step 9: Destroy previous room (D-09)
    if (_currentRoom != null) Destroy(_currentRoom);
    _currentRoom = nextRoom;

    _transitioning = false;
}
```

### Pattern 5: PlayerController Input Lock

**What:** Add `_inputLocked` bool field and `LockInput()`/`UnlockInput()` methods. Check in `FixedUpdate` before `ApplyMovement`. Jump callback also checks the flag.

**Critical:** `PlayerController` currently has no `Instance` static reference. FloorSpawner needs a way to call `LockInput()`. Options:
- Add `public static PlayerController Instance` (simplest, consistent with `FloorSpawner.Instance` pattern)
- Pass via Inspector `[SerializeField]` on FloorSpawner (no static needed)

**Recommendation:** Use `[SerializeField] private PlayerController _player` on FloorSpawner. Avoids adding another static singleton. PlayerController already uses static event (`OnPlayerDeath`) not instance reference.

```csharp
// PlayerController additions (surgical — existing code untouched above/below)
private bool _inputLocked;

public void LockInput()
{
    _inputLocked = true;
    _rb.linearVelocity = Vector2.zero;  // stop immediately
}

public void UnlockInput() => _inputLocked = false;

// In FixedUpdate, wrap ApplyMovement:
private void FixedUpdate()
{
    CheckGround();
    if (!_inputLocked) ApplyMovement();
}

// In OnJumpPerformed:
private void OnJumpPerformed(InputAction.CallbackContext ctx)
{
    if (_inputLocked || !_isGrounded) return;
    // ... existing jump logic
}
```

### Pattern 6: Enemy Spawn with Deferred Activation (FLOOR-03)

**What:** `FloorSpawner.SpawnRoom()` Instantiates enemies at spawn points with `SetActive(false)` immediately after Instantiate. `ActivateEnemies()` calls `SetActive(true)` in step 4 of the sequence.

**Why this works:** `MeleeEnemy` and `RangedEnemy` subscribe to `PlayerController.OnPlayerDeath` in `OnEnable`. When `SetActive(false)` is called, `OnDisable` fires and unsubscribes. When `SetActive(true)` fires, `OnEnable` resubscribes. The FSM `_state` resets to `EnemyState.Idle` when `OnPlayerDied()` is called. No enemy-side code changes needed.

**Pitfall:** After `SetActive(false)`, Coroutines on the enemy are stopped by Unity automatically. When re-enabled, coroutines do NOT auto-restart — but since FSM re-enters Idle via `OnEnable` → resubscription → no coroutine running, this is safe.

```csharp
private GameObject SpawnRoom(GameObject prefab, int floor)
{
    Vector3 pos = new Vector3(0f, (floor - 1) * _roomHeight, 0f);
    GameObject room = Instantiate(prefab, pos, Quaternion.identity);

    // Spawn enemies at tagged spawn points — inactive until step 4 (FLOOR-03)
    foreach (Transform sp in room.GetComponentsInChildren<Transform>())
    {
        if (!sp.CompareTag("EnemySpawnPoint")) continue;
        SpawnEnemyAtPoint(sp, floor);
    }
    return room;
}

private void SpawnEnemyAtPoint(Transform spawnPoint, int floor)
{
    // D-07: difficulty table
    (int melee, int ranged) = GetEnemyCount(floor);
    // ... instantiate based on spawnPoint index vs melee/ranged split
    GameObject enemyGO = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
    enemyGO.SetActive(false); // FLOOR-03: deferred activation
}

private void ActivateEnemies(GameObject room)
{
    foreach (var enemy in room.GetComponentsInChildren<IEnemy>(includeInactive: true))
    {
        (enemy as MonoBehaviour)?.gameObject.SetActive(true);
    }
}
```

**Note:** `GetComponentsInChildren<IEnemy>(includeInactive: true)` — the `includeInactive: true` parameter is required to find components on inactive GameObjects. Without it, the call returns zero results.

### Pattern 7: Difficulty Scaling Table (D-07)

**What:** Pure data lookup — no runtime computation.

```csharp
private (int melee, int ranged) GetEnemyCount(int floor)
{
    if (floor <= 5)  return (UnityEngine.Random.Range(2, 4), UnityEngine.Random.Range(0, 2));
    if (floor <= 10) return (2, UnityEngine.Random.Range(1, 3));
    return (2, UnityEngine.Random.Range(2, 4));
}
```

### Anti-Patterns to Avoid

- **Polling player Y position in Update:** Do not detect floor advancement by checking `transform.position.y` each frame. Use `OnTriggerEnter2D` on the exit collider — event-driven, zero per-frame cost.
- **FindObjectsOfType in transition:** Never call `FindObjectsOfType<MeleeEnemy>()` during transition. Use `room.GetComponentsInChildren<IEnemy>(true)` — scoped to the room instance.
- **WaitForSeconds inside transition Coroutine:** All waits MUST be `WaitForSecondsRealtime`. The hit-freeze sequence (`CombatController.HitFreeze`) sets `Time.timeScale = 0f`. If the player somehow triggers exit during a kill, `WaitForSeconds` would hang forever.
- **Spawning room at exactly floor * height without Y offset:** Player teleport target must be `roomBaseY + 1f` (above platform surface), not the room root Y which is at the floor.
- **Coroutine started on a disabled MonoBehaviour:** `StartCoroutine` on an inactive MonoBehaviour throws `InvalidOperationException`. All coroutines run on `FloorSpawner` (always active), not on Room or Enemy objects.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Timer immune to timeScale | Custom unscaledTime accumulator | `WaitForSecondsRealtime` | Already established in project (InvincibilityHandler, MeleeEnemy, RangedEnemy) |
| Enemy detection in room | Custom spatial query | `room.GetComponentsInChildren<IEnemy>(true)` | Scoped to room instance; no scene-wide search |
| Camera animation | Lerp coroutine in CameraFollow | Instant snap via existing `CameraFollow.LateUpdate` | CameraFollow already follows `target.position` exactly each LateUpdate — teleporting the player IS the camera snap |
| Weighted random selection | WeightedRandom<T> class | Duplicate entries in `_roomPool` array | 4~5 rooms; array index Random.Range is sufficient |
| Object pooling for rooms | Pool<GameObject> | `Instantiate` + `Destroy` | Only 2 rooms live at once; no GC pressure from room lifecycle |

**Key insight:** The camera Y-snap (D-08 step 3) is free — `CameraFollow.LateUpdate` calls `transform.position = target.position + offset` every frame. Teleporting the player (`Transform.position` set in step 2) means the camera snaps on the next `LateUpdate` call within the same frame or next frame. No additional camera code is required.

---

## Common Pitfalls

### Pitfall 1: Double-Trigger on RoomExit
**What goes wrong:** Player passes through exit collider on both entry and exit edges, firing `AdvanceFloor()` twice.
**Why it happens:** `OnTriggerEnter2D` fires once on overlap begin, but if the transition Coroutine yields before the player exits the trigger zone, a second `OnTriggerEnter2D` can fire if physics re-detects.
**How to avoid:** `FloorSpawner._transitioning` flag checked at top of `AdvanceFloor()`. `if (_transitioning) return;` is the guard.
**Warning signs:** Floor number increments by 2, or transition plays twice in rapid succession.

### Pitfall 2: `GetComponentsInChildren<IEnemy>` Returns Empty on Inactive Objects
**What goes wrong:** `ActivateEnemies()` finds zero enemies even though they were spawned.
**Why it happens:** Default `GetComponentsInChildren<T>()` skips inactive GameObjects. Enemies were spawned with `SetActive(false)`.
**How to avoid:** Always pass `includeInactive: true` — `room.GetComponentsInChildren<IEnemy>(true)`.
**Warning signs:** Enemies never appear after floor transition; room is empty of hostiles.

### Pitfall 3: `WaitForSeconds` Hanging During `Time.timeScale = 0`
**What goes wrong:** Floor transition coroutine freezes indefinitely mid-sequence.
**Why it happens:** `CombatController.HitFreeze` sets `Time.timeScale = 0f`. If exit is triggered at the same moment, any `WaitForSeconds(x)` in `FloorTransitionSequence` will never complete.
**How to avoid:** Use `WaitForSecondsRealtime` for ALL delays in `FloorTransitionSequence`. Already established rule: `Time.unscaledDeltaTime` / `WaitForSecondsRealtime` for all coroutines.
**Warning signs:** Screen freezes mid-transition after a kill near the exit.

### Pitfall 4: Enemy OnEnable Fires Before Room Is Positioned
**What goes wrong:** Enemies subscribe to `PlayerController.OnPlayerDeath` before their parent room is at the correct Y position, then their `_spawnPosition` (cached in `Awake`) is wrong.
**Why it happens:** `Instantiate` calls `Awake` synchronously. If position is set after Instantiate, `_spawnPosition = transform.position` in Awake captures the wrong position.
**How to avoid:** Pass the correct position to `Instantiate(prefab, position, rotation)` — the spawn point world position — so `Awake` captures the correct value. Do NOT set `transform.position` after Instantiate.
**Warning signs:** MeleeEnemy patrols around world origin (0,0) instead of its spawn point.

### Pitfall 5: SceneManager.LoadScene Destroys FloorSpawner Before Coroutine Ends
**What goes wrong:** If `RestartGame()` is called mid-transition, the Coroutine on FloorSpawner is interrupted.
**Why it happens:** `SceneManager.LoadScene(0)` destroys all scene objects including FloorSpawner. Any running Coroutine is killed.
**How to avoid:** This is acceptable behavior — scene reload is a full reset. `FloorManager.CurrentFloor = 1` is set by DeathScreenController before LoadScene (D-10). No guard needed.
**Warning signs:** None (this is intentional behavior per D-10).

### Pitfall 6: CameraFollow Y-Snap Shows Wrong Room for One Frame
**What goes wrong:** For one frame after player teleport, camera is at the old Y position, showing empty space or the destroyed previous room.
**Why it happens:** `LateUpdate` runs after all `Update` and coroutine resumes. If teleport happens mid-coroutine and camera catches up on the SAME frame's LateUpdate, it snaps correctly. But if the coroutine yields a frame, the camera snaps on the NEXT frame.
**How to avoid:** Step 2 (teleport) should NOT yield before completing the position set. The `yield return null` in step 3 is acceptable — one frame of black/snap is imperceptible at 60fps.
**Warning signs:** Brief camera stutter showing empty space during transition.

### Pitfall 7: PlayerController Has No Instance Reference
**What goes wrong:** `FloorSpawner` cannot call `LockInput()` / `UnlockInput()` because `PlayerController` has no static instance or accessible reference.
**Why it happens:** `PlayerController` currently only exposes `IsGrounded`, `MoveSpeed` properties and static event. No instance singleton.
**How to avoid:** Add `[SerializeField] private PlayerController _player` to `FloorSpawner` and wire in Inspector. Do NOT add a static singleton to PlayerController — it already uses the static event pattern for death; mixing patterns adds confusion.
**Warning signs:** NullReferenceException on `FloorSpawner._player.LockInput()` — means Inspector wire not set.

---

## Code Examples

### FloorSpawner Core Skeleton (verified against project patterns)
```csharp
// Assets/Scripts/World/FloorSpawner.cs
// Source: inferred from MeleeEnemy.cs coroutine pattern + CombatController timing
using System.Collections;
using UnityEngine;

public class FloorSpawner : MonoBehaviour
{
    [SerializeField] private GameObject     _floor1RoomPrefab;
    [SerializeField] private GameObject[]   _roomPool;          // 4~5 prefabs; repeat entries = weight
    [SerializeField] private Transform      _playerTransform;
    [SerializeField] private PlayerController _player;          // Pitfall 7: via Inspector, not static
    [SerializeField] private GameObject     _meleeEnemyPrefab;
    [SerializeField] private GameObject     _rangedEnemyPrefab;
    [SerializeField] private float          _roomHeight = 18f;  // Claude's discretion

    private GameObject _currentRoom;
    private bool       _transitioning;

    public static FloorSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        _currentRoom = SpawnRoom(_floor1RoomPrefab, 1);
    }

    public void AdvanceFloor()
    {
        if (_transitioning) return;  // Pitfall 1 guard
        StartCoroutine(FloorTransitionSequence());
    }

    private IEnumerator FloorTransitionSequence()
    {
        _transitioning = true;

        // Step 1: Lock input (D-08 step 1)
        _player.LockInput();

        // Increment floor, spawn next room (enemies SetActive=false — FLOOR-03)
        FloorManager.CurrentFloor++;
        GameObject nextRoom = SpawnRoom(SelectNextRoom(), FloorManager.CurrentFloor);

        // Step 2: Teleport player to new room spawn anchor
        // Pitfall 4: position passed to Instantiate, so enemy Awake already ran with correct pos
        Vector3 newPos = new Vector3(
            _playerTransform.position.x,
            (FloorManager.CurrentFloor - 1) * _roomHeight + 2f,  // +2 = above floor surface
            0f);
        _playerTransform.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        _playerTransform.position = newPos;

        // Step 3: Camera snaps via CameraFollow.LateUpdate (no explicit call needed)
        yield return null;  // one frame — LateUpdate runs, camera snaps

        // Step 4: Activate enemies (FLOOR-03)
        // Pitfall 2: includeInactive: true required
        foreach (var enemy in nextRoom.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (enemy is IEnemy) enemy.gameObject.SetActive(true);
        }

        // Step 5: One physics frame so FSMs enter Update with correct player ref
        yield return new WaitForSecondsRealtime(0.05f);

        // Step 6: Unlock input
        _player.UnlockInput();

        // Destroy previous room (FLOOR-04, D-09)
        if (_currentRoom != null) Destroy(_currentRoom);
        _currentRoom = nextRoom;

        _transitioning = false;
    }

    private GameObject SelectNextRoom()
    {
        if (_roomPool == null || _roomPool.Length == 0) return _floor1RoomPrefab;
        return _roomPool[Random.Range(0, _roomPool.Length)];
    }

    private GameObject SpawnRoom(GameObject prefab, int floor)
    {
        Vector3 roomOrigin = new Vector3(0f, (floor - 1) * _roomHeight, 0f);
        GameObject room = Instantiate(prefab, roomOrigin, Quaternion.identity);

        // Spawn enemies at tagged spawn points — inactive until ActivateEnemies (FLOOR-03)
        var spawnPoints = room.GetComponentsInChildren<Transform>(true);
        int spawnIndex = 0;
        (int meleeCount, int rangedCount) = GetEnemyCount(floor);

        foreach (Transform sp in spawnPoints)
        {
            if (!sp.CompareTag("EnemySpawnPoint")) continue;
            GameObject prefabToSpawn = spawnIndex < meleeCount
                ? _meleeEnemyPrefab
                : (spawnIndex < meleeCount + rangedCount ? _rangedEnemyPrefab : null);

            if (prefabToSpawn == null) continue;

            // Pitfall 4: Instantiate WITH position — Awake runs at correct world pos
            GameObject enemy = Instantiate(prefabToSpawn, sp.position, Quaternion.identity);
            enemy.SetActive(false);  // FLOOR-03
            spawnIndex++;
        }
        return room;
    }

    private (int melee, int ranged) GetEnemyCount(int floor)
    {
        // D-07 difficulty table
        if (floor == 1) return (0, 0);   // floor 1: tutorial, no enemies
        if (floor <= 5)  return (Random.Range(2, 4), Random.Range(0, 2));
        if (floor <= 10) return (2, Random.Range(1, 3));
        return (2, Random.Range(2, 4));
    }
}
```

### PlayerController Additions (surgical)
```csharp
// Add to existing PlayerController.cs — no existing lines modified, only additions

// Field (after _jumpHeld):
private bool _inputLocked;

// Methods (add after existing public accessors):
public void LockInput()
{
    _inputLocked = true;
    _rb.linearVelocity = Vector2.zero;
}

public void UnlockInput() => _inputLocked = false;

// In FixedUpdate — wrap ApplyMovement:
private void FixedUpdate()
{
    CheckGround();
    if (!_inputLocked) ApplyMovement();
}

// In OnJumpPerformed — add locked check:
private void OnJumpPerformed(InputAction.CallbackContext ctx)
{
    if (_inputLocked || !_isGrounded) return;
    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
    _jumpHeld = true;
}
```

### RoomExit Script
```csharp
// Assets/Scripts/World/RoomExit.cs
using UnityEngine;

/// <summary>
/// D-01: Placed on a Trigger Collider2D child at the top of each Room prefab.
/// Notifies FloorSpawner when the player enters. FloorSpawner._transitioning guards double-fire.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomExit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        FloorSpawner.Instance?.AdvanceFloor();
    }
}
```

---

## Runtime State Inventory

This is not a rename/refactor phase. No runtime state migration required.

| Category | Items Found | Action Required |
|----------|-------------|-----------------|
| Stored data | None — no persistent data stores | None |
| Live service config | None — no external services | None |
| OS-registered state | None | None |
| Secrets/env vars | None — no .env files | None |
| Build artifacts | None — no stale artifacts related to this phase | None |

---

## Environment Availability

| Dependency | Required By | Available | Version | Fallback |
|------------|------------|-----------|---------|----------|
| Unity 6000.3.11f1 | All implementation | Assumed yes (project constraint) | 6000.3.11f1 | — |
| Unity TileMap (com.unity.2d.tilemap) | Room platform geometry | Yes — in Packages/manifest.json | 1.0.0 | Use SpriteRenderer quads instead |
| TextMeshPro (com.unity.ugui) | HUDController floor label | Yes — in use by HUDController | 2.0.0 | — |
| Physics2D (com.unity.modules.physics2d) | Trigger collision detection | Yes — active (Rigidbody2D in use) | 1.0.0 | — |

**Missing dependencies with no fallback:** None.

**Missing dependencies with fallback:** None.

**Note:** Room platform geometry can use either Tilemap (available) or plain SpriteRenderer + BoxCollider2D quads. For 4~5 hand-authored rooms in a prototype, SpriteRenderer quads are faster to author and have no difference in functionality. Tilemap is available if the designer prefers it.

---

## Validation Architecture

nyquist_validation is enabled in config.json.

### Test Framework
| Property | Value |
|----------|-------|
| Framework | Unity Test Framework (NUnit, com.unity.test-framework 1.6.0) |
| Config file | None detected — Wave 0 must create PlayMode assembly definition |
| Quick run command | Unity Editor: Window > General > Test Runner > Run All (PlayMode) |
| Full suite command | Same — all PlayMode tests |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| FLOOR-01 | `SelectNextRoom()` never returns null when pool has entries; floor 1 always returns `_floor1RoomPrefab` | unit | PlayMode: `FloorSpawnerTests.SelectNextRoom_ReturnsFloor1OnFirstFloor` | Wave 0 |
| FLOOR-01 | Room is instantiated at correct Y offset `(floor-1) * roomHeight` | unit | PlayMode: `FloorSpawnerTests.SpawnRoom_PositionedAtCorrectY` | Wave 0 |
| FLOOR-02 | `AdvanceFloor()` guarded — second call during `_transitioning=true` is a no-op | unit | PlayMode: `FloorSpawnerTests.AdvanceFloor_IgnoresDoubleTrigger` | Wave 0 |
| FLOOR-02 | After transition, `FloorManager.CurrentFloor` incremented by exactly 1 | unit | PlayMode: `FloorSpawnerTests.Transition_IncrementsCurrentFloor` | Wave 0 |
| FLOOR-02 | After transition, player position Y is above previous floor Y | PlayMode integration | PlayMode: `FloorTransitionTests.PlayerTeleportedAbovePreviousFloor` | Wave 0 |
| FLOOR-03 | Enemies spawned with `SetActive(false)`; active after transition step 4 | PlayMode integration | PlayMode: `FloorSpawnerTests.Enemies_InactiveOnSpawn_ActiveAfterTransition` | Wave 0 |
| FLOOR-04 | Previous room GameObject destroyed after transition complete | PlayMode integration | PlayMode: `FloorSpawnerTests.PreviousRoom_DestroyedAfterTransition` | Wave 0 |

**Manual-only verifications (no automated equivalent):**
- Room prefab content (platform placement, exit trigger position) — requires Unity Editor visual inspection
- Camera Y-snap smoothness — requires Play Mode human observation
- Input locked during transition (no movement response) — requires Play Mode human observation

### Sampling Rate
- **Per task commit:** Run `FloorSpawnerTests` in Test Runner (PlayMode)
- **Per wave merge:** Full PlayMode test suite
- **Phase gate:** All PlayMode tests green before `/gsd:verify-work`

### Wave 0 Gaps
- [ ] `Assets/Tests/PlayMode/FloorSpawnerTests.cs` — covers FLOOR-01, FLOOR-02, FLOOR-04
- [ ] `Assets/Tests/PlayMode/FloorTransitionTests.cs` — covers FLOOR-02, FLOOR-03 integration
- [ ] `Assets/Tests/PlayMode/PlayMode.asmdef` — PlayMode assembly definition (check if created in Phase 02-04 — PLAN `02-04-PLAN.md` is not yet executed per STATE.md)

**Note:** Plan 02-04 (PlayMode test infrastructure) is listed as uncompleted in ROADMAP.md. Wave 0 of Phase 5 must create the PlayMode assembly definition if it does not yet exist.

---

## Integration Points Resolved

| Integration Point | Finding | Action |
|------------------|---------|--------|
| `PlayerController` input lock | No `LockInput`/`UnlockInput` methods exist. No static instance. | Add `_inputLocked` bool, two public methods, guard in FixedUpdate + OnJumpPerformed. Wire via Inspector SerializeField. |
| `FloorSpawner` vs `FloorManager` separation | `FloorManager` is static data-only class (established pattern 04-01). No spawn logic should go there. | Create `FloorSpawner` MonoBehaviour. `FloorManager.CurrentFloor` is the only shared state. |
| Room exit detection | `OnTriggerEnter2D` on child — standard Unity pattern. | `RoomExit` script on exit child. Tag check `"Player"` (same tag used by `MeleeEnemy.OnTriggerEnter2D`). |
| Enemy spawn during transition | Enemy FSM uses `OnEnable`/`OnDisable` for `OnPlayerDeath` subscription — safe for `SetActive` cycling. | Spawn with `SetActive(false)`. Activate in step 4. `GetComponentsInChildren<IEnemy>(true)` with `includeInactive:true`. |
| Camera snap | `CameraFollow.LateUpdate` directly sets `transform.position = target.position + offset`. | Teleporting the player is the camera snap. One `yield return null` frame is sufficient. No camera code changes. |
| HUD floor counter | `HUDController.Update()` reads `FloorManager.CurrentFloor` every frame via `SetText("{0}", FloorManager.CurrentFloor)`. | Automatic — incrementing `FloorManager.CurrentFloor` in step 2 of transition is immediately reflected in HUD on next frame. |
| Death screen restart | `DeathScreenController.RestartGame()` resets `FloorManager.CurrentFloor = 1` then `SceneManager.LoadScene(0)`. | No changes needed. Scene reload destroys all spawned rooms. D-10 confirmed. |
| `CombatController` attack during transition | Player is input-locked during transition. `CombatController.Update()` checks `InputManager.Instance.AttackHeld`. | `_inputLocked` blocks `ApplyMovement` but does NOT directly block CombatController. Must also gate attack input. See open question below. |

---

## Open Questions

1. **CombatController input lock during transition**
   - What we know: `PlayerController._inputLocked` blocks movement in `FixedUpdate`. `CombatController` reads `InputManager.Instance.AttackHeld` independently in its own `Update()` — it does not check `PlayerController._inputLocked`.
   - What's unclear: Should CombatController also be locked during the 0.3~0.5s transition? If a player holds attack at the moment of exit, slow-motion activates mid-transition.
   - Recommendation: Add `public bool InputLocked => _inputLocked` property to PlayerController. Check in `CombatController.Update()` at the top: `if (GetComponent<PlayerController>().InputLocked) return;`. Cache the reference in `Awake`. This is a surgical 2-line change.

2. **Room height constant — 18 Unity units**
   - What we know: This is Claude's discretion (CONTEXT.md). CameraFollow offset is `(0, 1, -10)`. Current test floor is not measured.
   - What's unclear: Current test layout Y dimensions. If existing platform is at Y=0, a room of 18 units means exit trigger at Y=17, next room starts at Y=18.
   - Recommendation: Measure existing SampleScene platform height in Unity Editor before authoring Room prefabs. Set `_roomHeight` to match the desired visible room area. 18 units is a reasonable default for a landscape mobile view at 1920x1080 with default camera orthographic size.

3. **`_roomPool` weighted selection — equal weight sufficient?**
   - What we know: CONTEXT.md allows `Random.Range` array index for selection.
   - What's unclear: Whether any rooms should appear more often (e.g., Room_Combat more common early game).
   - Recommendation: Start with equal weight (single entry per room type). Post-playtest, add duplicate entries for weight adjustment (e.g., `[Room_Combat, Room_Combat, Room_Chase, Room_Dodge, Room_Gap]` gives Room_Combat 2/5 probability).

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Cinemachine for camera follow | Manual LateUpdate CameraFollow.cs | Phase 1 D-11/D-13 | Camera snaps immediately on position change — required for teleport-based floor transition |
| `WaitForSeconds` | `WaitForSecondsRealtime` | Phase 1 established | All timers immune to timeScale; critical for transitions near hit-freeze |
| `FindObjectsOfType<T>` | `room.GetComponentsInChildren<T>(true)` | Phase 3 established | Scoped, O(room children) not O(scene) |
| `Physics2D.OverlapCircle` with alloc | `Physics2D.OverlapCircleNonAlloc` with pre-alloc buffer | Established ROADMAP constraint | No GC in Update hot path |

---

## Project Constraints (from CLAUDE.md)

| Directive | Applies to Phase 5 |
|-----------|-------------------|
| Unity 6 LTS + C# only | All new scripts: C# MonoBehaviour targeting Unity 6000.3.11f1 |
| Android-first (ARM64, minSdk 25) | Memory: destroy previous room immediately (D-09, FLOOR-04). No more than 2 rooms in scene. |
| Prototype scope — no over-engineering | No object pools, no Room base class, no event bus. `FloorSpawner` is one file. |
| `Time.unscaledDeltaTime` for all timers | All `WaitForSecondsRealtime` in `FloorTransitionSequence`. No `WaitForSeconds` anywhere. |
| `Physics2D.OverlapCircleNonAlloc` — no LINQ in Update | `GetComponentsInChildren` used once per transition (not in Update), acceptable. |
| Animator Transition Duration = 0 | No animation on floor transition. N/A. |
| `Rigidbody2D`: Continuous + Interpolate | Player Rigidbody2D settings unchanged. Enemies instantiated with same Inspector settings as prefab. |
| `TextMeshProUGUI.SetText("{0}", int)` — zero allocation | HUDController already uses this. No change needed. |
| Scope: core mechanic validation only | 4~5 rooms sufficient. Do not build 14 rooms. |
| GSD workflow enforcement | No direct file edits outside GSD workflow. |

---

## Sources

### Primary (HIGH confidence)
- `Assets/Scripts/World/FloorManager.cs` — confirmed static class, `CurrentFloor` field only
- `Assets/Scripts/UI/HUDController.cs` — confirmed reads `FloorManager.CurrentFloor` via `SetText("{0}", int)` every Update
- `Assets/Scripts/UI/DeathScreenController.cs` — confirmed `FloorManager.CurrentFloor = 1` + `SceneManager.LoadScene(0)` in RestartGame
- `Assets/Scripts/Player/PlayerController.cs` — confirmed no `LockInput` method, no static instance, has `OnPlayerDeath` static event
- `Assets/Scripts/Camera/CameraFollow.cs` — confirmed direct `transform.position = target.position + offset` in LateUpdate
- `Assets/Scripts/Enemy/MeleeEnemy.cs` — confirmed `OnEnable`/`OnDisable` pattern, `WaitForSecondsRealtime` coroutine, `SetActive` safe
- `Assets/Scripts/Enemy/RangedEnemy.cs` — confirmed same pattern as MeleeEnemy
- `Assets/Scripts/Enemy/IEnemy.cs` — confirmed `IsAlive`, `OnDashHit()`, `ClearHighlight()` only
- `Assets/Scripts/Player/InvincibilityHandler.cs` — confirmed `WaitForSecondsRealtime` pattern
- `.planning/phases/05-procedural-map-infinite-stages/05-CONTEXT.md` — D-01 through D-10 locked decisions
- `.planning/REQUIREMENTS.md` — FLOOR-01, FLOOR-02, FLOOR-03, FLOOR-04 requirements
- `.planning/STATE.md` — stack constraints table, established decisions

### Secondary (MEDIUM confidence)
- Unity documentation (training data, August 2025 cutoff): `GetComponentsInChildren<T>(includeInactive: bool)` parameter behavior confirmed by reasoning from Unity API design — the `includeInactive` parameter is documented since Unity 5.x and has not changed
- `OnEnable`/`OnDisable` Coroutine behavior (SetActive stops coroutines) — confirmed by Unity MonoBehaviour lifecycle documentation pattern, consistent with project code (enemies stop coroutines in OnPlayerDied)

### Tertiary (LOW confidence)
- Room height recommendation of 18 Unity units — estimated based on default orthographic camera size and 1920x1080 resolution. Must be validated against actual SampleScene geometry before room authoring.

---

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — all from direct codebase inspection
- Architecture: HIGH — patterns extracted directly from existing MeleeEnemy, CameraFollow, and FloorManager code
- Pitfalls: HIGH — each traced to specific code behavior in read files
- Room height constant: LOW — must be measured in Unity Editor

**Research date:** 2026-06-17
**Valid until:** 2026-07-17 (stable Unity 6 project — no external dependencies changing)
