# Phase 10: EXIT 포탈 & 층 전환 - Research

**Researched:** 2026-07-03
**Domain:** Unity 6 C# gameplay systems — trigger-based scene-graph transition, MonoBehaviour lifecycle/coroutine ownership, prefab marker components
**Confidence:** HIGH (this phase is 95% codebase-internal integration work; no new external packages or unverified libraries are involved)

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

**포탈 스폰 위치 마커**
- **D-01:** `ExitSpawnPoint` 신규 마커 컴포넌트를 만든다 — `EnemySpawner.cs` 패턴과 동일 (빈 컴포넌트, 자식 오브젝트 위치가 스폰 후보 지점).
- **D-02:** Complex_Room 6종(AllInOne/EdgeRun/GaugeOutpost/LastStand/RiskCrossing/Vertical_Gauntlet) 전부에 배치. 룸당 2~3개 지점.
- **D-03:** 배치는 사용자가 에디터에서 직접 수동으로 진행한다 — 자동 배치 도구는 만들지 않는다. (플랜/실행 단계에서 사용자 액션 항목으로 명시할 것)

**층 전환 시퀀스 & ENT 텔레포트**
- **D-04:** 옛 `FloorSpawner.FloorTransitionSequence()`의 6단계(입력잠금 → ENT 텔레포트 → 카메라 스냅 → 프레임 대기 → 적 활성화 → 조작 재개)를 그대로 재사용한다. 전부 `WaitForSecondsRealtime` 기반으로 timeScale 면역 유지.
- **D-05:** 수평 체인 진행(같은 층 내 룸→길→룸 이동)은 지금처럼 플레이어가 직접 걸어서 이동한다 — 텔레포트는 오직 층 전환(수직 이동) 순간에만 적용.
- **D-06 (folded todo, 2026-07-03-complex-room-ent):** RoomEntry(ENT) 마커가 없는 4개 Complex_Room(AllInOne/EdgeRun/LastStand/Vertical_Gauntlet)에 `RoomEntry` 컴포넌트를 직접 추가한다 — 코드 폴백을 유지하는 대신 근본 해결. GaugeOutpost/RiskCrossing은 이미 보유.

**WorldGenerator 리셋 범위**
- **D-07:** 포탈 진입 시 기존 수평 체인(`_chain` 리스트의 모든 room+corridor)을 즉시 전부 Destroy하고, 활성화된 대기룸(`_nextFloorRoom`) 하나를 새 체인의 시작점으로 삼아 재시작한다. GEN-02의 점진적 lookbehind 정리에 맡기지 않는다.

**미사용 포탈 소멸 처리**
- **D-08 (09-CONTEXT D-07 후속 결정):** GEN-02(lookbehind 정리)가 플레이어가 진입하지 않은 포탈을 보유한 룸을 Destroy할 때, 해당 포탈에 연결된 대기룸(`_nextFloorRoom`)도 함께 Destroy하고 활성 포탈 카운트(`_maxExitsActive` 카운터)를 감소시킨다 — 대기룸 메모리 누수 방지 + 신규 포탈 스폰 기회 복원.

### Claude's Discretion
- ExitPortal 컴포넌트의 트리거 콜라이더 크기/모양, EXIT 포탈 스폰 확률 롤 발생 시점(룸 스폰 직후 vs 특정 프레임) 등 세부 구현은 플래너/실행자 재량.
- `FloorSpawner.cs`, `RoomExit.cs` (Phase 5 유산, 현재 씬에서 미사용 고아 코드)는 이번 Phase에서 생성한 코드가 아니므로 삭제하지 않는다 — 언급만 하고 그대로 둔다. 신규 `ExitPortal.cs`가 `RoomExit.cs`의 역할을 대체한다.

### Deferred Ideas (OUT OF SCOPE)
없음 — 논의가 Phase 범위 내에 머무름. 타이머/난이도(Phase 11), 포탈 이펙트/사운드(REQUIREMENTS.md Out of Scope)는 이 Phase에 포함하지 않는다. SCORE-01/02는 어떤 Phase에도 아직 매핑되지 않은 요구사항(Unmapped) — Phase 10에서 ScoreManager 연동을 추가하지 않는다.
</user_constraints>

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| EXIT-01 | 각 Room 스폰 시 정해진 스폰 포인트 중 하나에 낮은 확률(기본 15%)로 EXIT 포탈이 생성된다 | See "Portal Spawn Roll" pattern below — `TrySpawnExitPortal()` helper called from both `Start()` (start room) and `SpawnNextPair()` (every subsequent room), using `ExitSpawnPoint` markers |
| EXIT-02 | 포탈 스폰 확률과 최대 동시 활성 개수를 인스펙터에서 조절할 수 있다 | `_exitSpawnChance` (float, [Range(0,1)]) and `_maxExitsActive` (int) as `[SerializeField]` on **WorldGenerator** (NOT FloorSpawner — see Pitfall "Stale FloorSpawner reference") |
| EXIT-03 | 플레이어가 EXIT 포탈에 진입하면 다음 층으로 전환되고 WorldGenerator가 초기화된다 | See "Floor Transition Sequence" pattern — `ExitPortal.OnTriggerEnter2D()` → `WorldGenerator.Instance.EnterPortal(this)` → coroutine on WorldGenerator (persistent object) doing chain teardown + rebuild + ENT teleport + camera snap |
</phase_requirements>

## Summary

Phase 10 is pure codebase-integration work inside an existing, well-established pattern family (marker components + `GetComponentsInChildren<T>(true)` discovery + `WaitForSecondsRealtime` transition coroutines). No new Unity packages, no external libraries, and no Context7/WebSearch-dependent APIs are involved — every building block this phase needs (RoomEntry, RoomConnector, EnemySpawner, CameraBound, CameraFollow.SnapToRoom, FloorManager.CurrentFloor, PlayerController.LockInput/UnlockInput, CombatController.ForceExitCombatState) already exists and was verified by reading the actual source files.

The one piece of genuinely new engineering is **where the 6-step transition coroutine lives**. The old Phase-5 pattern (`FloorSpawner.FloorTransitionSequence()`) ran its coroutine on `FloorSpawner` — a persistent singleton — even though the trigger (`RoomExit`) sat on the room being destroyed at the end of the sequence. Phase 10 must replicate this exact ownership pattern: the transition coroutine must run on `WorldGenerator` (already the persistent MonoBehaviour managing the chain), never on `ExitPortal` itself, because `EnterPortal()` will `Destroy()` the very room the portal is a child of partway through the sequence (D-07's "destroy entire chain" step). Destroying a GameObject/MonoBehaviour that owns a running coroutine stops that coroutine immediately — confirmed against Unity's documented behavior. Running the coroutine on `WorldGenerator` sidesteps this entirely, exactly as `FloorSpawner` did in Phase 5.

The second design clarification worth flagging to the planner: CONTEXT.md's D-08 says "해당 포탈에 연결된 대기룸" (the standby room *connected to that portal*) — this is a per-portal association, not a single global slot. The pre-existing `_nextFloorRoom` field (declared in Phase 9's `WorldGenerator.cs`, currently unused) was named for the common case of `_maxExitsActive = 1`, but the correct general design stores the standby-room reference **on the `ExitPortal` instance itself** (e.g. `public GameObject StandbyRoom`), so `RemoveTail()` (GEN-02 cleanup) can look up `room.GetComponentInChildren<ExitPortal>(true)?.StandbyRoom` directly without needing global bookkeeping beyond a simple int counter.

**Primary recommendation:** Add `ExitSpawnPoint` (empty marker, mirrors `RoomEntry`) and `ExitPortal` (trigger + portal-standby-room owner) components; add `_exitPortalPrefab`, `_exitSpawnChance`, `_maxExitsActive` fields plus a `WorldGenerator.Instance` singleton and `EnterPortal(ExitPortal)` coroutine method to `WorldGenerator.cs`; port the 6-step sequence verbatim from `FloorSpawner.FloorTransitionSequence()`, substituting the ENT-teleport target with the (already-spawned, previously-inactive) standby room's `RoomEntry` marker.

## Standard Stack

### Core
No new packages. This phase uses only Unity APIs and project-internal C# classes already present in the codebase.

| Component | Status | Purpose | Why reuse instead of new |
|-----------|--------|---------|---------------------------|
| `RoomEntry.cs` | Existing (empty marker) | ENT teleport target on standby room | D-06 explicitly requires attaching this exact component to 4 rooms, not inventing a new one |
| `RoomConnector.cs` | Existing | Left/Right direction marker, `GetComponentsInChildren<RoomConnector>()` pattern | `ExitSpawnPoint` should copy this exact discovery pattern (no direction needed, just position) |
| `EnemySpawner.cs` | Existing | Reference pattern only (marker-component-on-child convention) | D-01 cites this as the *pattern* to copy — the marker itself should stay as minimal as `RoomEntry.cs` (empty class), not replicate `EnemySpawner`'s `Spawn()/Activate()` API, since EXIT-01/02/03 have no spawn-then-activate lifecycle requirement |
| `CameraBound.cs` / `CameraFollow.cs` | Existing | Camera snap on floor transition | `SnapToRoom(Bounds)` / `SnapToRoom(Vector3)` already handle both cases; Complex_Room prefabs already carry `CameraBound` (added in quick-260701-sc7) |
| `FloorManager.CurrentFloor` | Existing (static int) | Floor counter, HUD-linked | Just `++` it — no HUD code changes needed (confirmed already wired in Phase 4) |
| `PlayerController.LockInput()/UnlockInput()` | Existing | Steps 1 and 6 of the transition sequence | Exact same calls `FloorSpawner` made |
| `CombatController.ForceExitCombatState()` | Existing | Clears slow-motion/attack-pending before lock, per Phase-5 fix note | Must be called **before** `LockInput()` (see source comment at `CombatController.cs:179`) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| `ExitSpawnPoint` as empty marker | Reuse `RoomConnector` with a third `Direction.Exit` enum value | Rejected by D-01 itself — decision explicitly calls for a new, separate, empty component |
| Coroutine on `WorldGenerator` | Coroutine on a new always-alive "GameManager" singleton | Unnecessary — `WorldGenerator` is already the persistent chain-owner; no reason to introduce a second singleton |
| Per-portal `StandbyRoom` field | Single `_nextFloorRoom` field on `WorldGenerator` (as literally named in Phase 9 stub comment) | Works correctly only while `_maxExitsActive == 1` (today's default); breaks silently if `_maxExitsActive` is ever raised in the Inspector, since a second portal would overwrite the first's reference. Per-portal storage costs nothing extra and is what D-08's wording ("해당 포탈에 연결된") actually describes. |

**Installation:** None — no new packages. No `npm install` / `UPM add` step applies to this phase.

**Version verification:** N/A — this phase does not add or bump any package. Unity 6000.3.11f1 and all currently-installed packages (URP 17.3.0, Input System 1.19.0, 2D Tilemap 1.0.0, etc.) are unchanged and already verified functional through Phase 9.

## Architecture Patterns

### Recommended Project Structure
No new folders. New scripts go alongside their siblings:
```
Assets/Scripts/World/
├── WorldGenerator.cs      # MODIFIED: singleton, portal roll, EnterPortal() coroutine
├── ExitPortal.cs          # NEW: trigger + StandbyRoom reference
├── ExitSpawnPoint.cs      # NEW: empty marker (mirrors RoomEntry.cs)
├── RoomEntry.cs           # UNCHANGED (reused)
├── RoomConnector.cs       # UNCHANGED (reused)
├── FloorManager.cs        # UNCHANGED (reused, CurrentFloor++ only)
├── FloorSpawner.cs        # UNCHANGED — orphaned Phase-5 code, do not delete (Claude's Discretion)
└── RoomExit.cs            # UNCHANGED — orphaned, replaced functionally by ExitPortal.cs (do not delete)
```

### Pattern 1: Coroutine Ownership Must Survive the Destroy It Triggers

**What:** The transition sequence Destroys the room the trigger itself is a child of. If the coroutine ran on the trigger's own component (or the room), Unity would stop the coroutine mid-execution the instant `Destroy()` takes effect on that GameObject/component.

**When to use:** Any trigger-initiated multi-frame sequence that ends by destroying the object holding the trigger — exactly what EXIT-03 requires (D-07: destroy entire old chain, including the room the portal is a child of).

**Verified precedent in this codebase:** `FloorSpawner.cs` (Phase 5) already solved this. `RoomExit.OnTriggerEnter2D()` calls `FloorSpawner.Instance?.AdvanceFloor()`, which does `StartCoroutine(FloorTransitionSequence())` **on `FloorSpawner`** (a persistent scene singleton), not on `RoomExit` or the room. At the very end of that coroutine, `Destroy(_currentRoom)` runs — the *old* room, which is where `RoomExit` itself lived — but by then `RoomExit` has already fired its one-shot job.

**Recommended Phase 10 pattern:**
```csharp
// ExitPortal.cs (new)
[RequireComponent(typeof(Collider2D))]
public class ExitPortal : MonoBehaviour
{
    // Set by WorldGenerator immediately after Instantiate — D-08's "해당 포탈에 연결된 대기룸"
    public GameObject StandbyRoom { get; set; }

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (other.GetComponentInParent<PlayerController>() == null) return;
        _triggered = true;
        WorldGenerator.Instance.EnterPortal(this); // starts coroutine on WorldGenerator, not here
    }
}
```
```csharp
// WorldGenerator.cs (additions)
public static WorldGenerator Instance { get; private set; }

private void Awake() { Instance = this; }  // NEW — WorldGenerator currently has no Awake()

public void EnterPortal(ExitPortal portal) => StartCoroutine(FloorTransitionSequence(portal));

private IEnumerator FloorTransitionSequence(ExitPortal portal)
{
    // Step 1 — lock (port verbatim from FloorSpawner.FloorTransitionSequence, Step 1)
    _combatController?.ForceExitCombatState();
    _player.LockInput();

    FloorManager.CurrentFloor++;

    // D-07 — destroy entire old chain synchronously, BEFORE any yield
    foreach (var (room, corridor) in _chain)
    {
        if (corridor != null) Destroy(corridor);
        Destroy(room);
    }
    _chain.Clear();
    _activeExitCount = 0; // all portals on the old chain are gone with it

    GameObject newRoom = portal.StandbyRoom;
    newRoom.SetActive(true);
    _chain.Add((newRoom, null));
    _playerCurrentIndex = 0;
    _currentYDrift = 0f; // fresh drift budget for the new floor

    var exit = FindConnector(newRoom, RoomConnector.Direction.Right);
    _chainHeadExitPos = exit != null ? exit.transform.position : newRoom.transform.position;

    // Step 2 — ENT teleport (D-06: all 6 Complex_Room prefabs now carry RoomEntry)
    RoomEntry entry = newRoom.GetComponentInChildren<RoomEntry>(true);
    Vector3 teleportPos = entry != null ? entry.transform.position : newRoom.transform.position;
    var rb = _playerTransform.GetComponent<Rigidbody2D>();
    if (rb != null) rb.linearVelocity = Vector2.zero;
    _playerTransform.position = teleportPos;

    // Step 3 — camera snap
    CameraBound cb = newRoom.GetComponentInChildren<CameraBound>();
    if (_cameraFollow != null)
    {
        if (cb != null) _cameraFollow.SnapToRoom(cb.GetWorldBounds());
        else _cameraFollow.SnapToRoom(teleportPos);
    }

    yield return null; // Step 3.5 — let LateUpdate run before enemy activation

    // Step 4 — enemy activation (see Pitfall: currently a no-op, WorldGenerator doesn't spawn via EnemySpawner)

    yield return new WaitForSecondsRealtime(0.05f); // Step 5

    _player.UnlockInput(); // Step 6
}
```
Note: `Update()`'s existing GEN-01 lookahead loop (`while (_chain.Count - 1 - _playerCurrentIndex < _lookaheadCount) SpawnNextPair();`) will automatically refill the new chain on the next frame once `_chain` contains only `[newRoom]` — no manual `SpawnNextPair()` call needed inside the coroutine.

### Pattern 2: Portal Spawn Roll + Eager Standby-Room Pre-Spawn

**What:** EXIT-01's probability roll and the standby-room pre-spawn (Phase 9's stubbed `SpawnNextFloorStandbyRoom()`) must happen at **room-spawn time**, not at portal-trigger time — this is required by D-08, which assumes the standby room already exists so it can be destroyed if GEN-02 removes its portal's room before the player ever reaches it.

**Recommended:**
```csharp
private void TrySpawnExitPortal(GameObject room)
{
    if (_activeExitCount >= _maxExitsActive) return;
    if (Random.value > _exitSpawnChance) return;

    var points = room.GetComponentsInChildren<ExitSpawnPoint>(true);
    if (points.Length == 0) return; // room prefab has no markers yet (D-03: manual placement pending)

    var point = points[Random.Range(0, points.Length)];
    var portalGO = Instantiate(_exitPortalPrefab, point.transform.position, Quaternion.identity, room.transform);
    var portal = portalGO.GetComponent<ExitPortal>();
    _activeExitCount++;

    // D-04: standby room spawned NOW, inactive, at (0, currentY + floorHeight, 0) — see Pitfall re: X=0
    var standbyPrefab = _roomPrefabs[Random.Range(0, _roomPrefabs.Length)];
    var standbyPos = new Vector3(0f, _chainHeadExitPos.y + _floorHeight, 0f);
    var standbyRoom = Instantiate(standbyPrefab, standbyPos, Quaternion.identity);
    standbyRoom.SetActive(false);
    portal.StandbyRoom = standbyRoom;
}
```
Call `TrySpawnExitPortal(room)` from **both**:
1. `Start()`, right after the start room is instantiated (so even floor 1 can roll a portal — matches EXIT-01's "각 Room 스폰 시" wording, no exception carved out for the very first room), and
2. `SpawnNextPair()`, right after the room half of the pair is instantiated.

### Anti-Patterns to Avoid
- **Calling `AlignByEntry()` on the standby room:** `AlignByEntry()` requires `Instantiate(prefab, Vector3.zero, Quaternion.identity)` immediately beforehand (documented constraint at `WorldGenerator.cs:121`, "Pitfall 2" in Phase 8/9 research) because it computes a position *offset* from the ENT connector assuming the object's local origin is world origin. The standby room has no horizontal continuity requirement with the old chain (D-07 destroys the old chain entirely), so it should be spawned directly at `(0, targetY, 0)` — exactly like the very first room in `Start()` — never through `AlignByEntry()`.
- **Starting the transition coroutine on `ExitPortal` or on the room `GameObject`:** see Pattern 1 — this would silently truncate the sequence when `Destroy()` fires.
- **Resurrecting `FloorSpawner`/`RoomExit` for Phase 10 work:** these are explicitly orphaned per CONTEXT.md discretion; `ExitPortal.cs` replaces `RoomExit.cs`'s *role*, not its file.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Multi-frame transition sequencing immune to slow-motion | Custom `Time.timeScale`-aware timer/state machine | `WaitForSecondsRealtime` + coroutine (already proven in `FloorSpawner`) | `Time.timeScale` can be ~0.2 (slow-mo) or briefly 0 (hit-freeze) when the player touches the portal; `WaitForSeconds` would stretch the 0.05s wait to 0.25s+ |
| Camera "did it snap correctly" bounds math | New camera-bounds calculation | `CameraBound.GetWorldBounds()` + `CameraFollow.SnapToRoom(Bounds)` | Already handles the both-cases-exist logic (room with/without `CameraBound`) |
| Marker discovery in a prefab hierarchy | Tag-based search, `Find()` by string path | `GetComponentsInChildren<T>(true)` (established convention across `RoomConnector`, `RoomEntry`, `EnemySpawner`) | `includeInactive: true` is required since the standby room is `SetActive(false)` until entered — a plain `GetComponentsInChildren<T>()` (default `false`) would silently return nothing |

**Key insight:** Every mechanical building block this phase needs was already built and proven in Phase 5 (`FloorSpawner`) or Phase 8/9 (`WorldGenerator`, `RoomConnector`, `RoomEntry`). The actual engineering work in Phase 10 is *wiring*, not invention — the main risk is subtly deviating from a proven pattern (e.g. forgetting `includeInactive: true`, or running the coroutine on the wrong object) rather than needing new algorithms.

## Common Pitfalls

### Pitfall 1: Coroutine Destroyed Mid-Sequence
**What goes wrong:** If `EnterPortal()`'s coroutine is started via `portal.StartCoroutine(...)` instead of `WorldGenerator.Instance.StartCoroutine(...)`, the sequence silently stops the instant the old room (parent of the portal) is `Destroy()`-ed inside step "destroy entire chain" — before the ENT teleport or camera snap ever runs.
**Why it happens:** Unity stops all coroutines owned by a MonoBehaviour/GameObject when that object is destroyed, even if `Destroy()`'s actual cleanup is deferred to end-of-frame.
**How to avoid:** Always call `StartCoroutine` on `WorldGenerator` (verified persistent singleton pattern, matching `FloorSpawner.Instance` precedent), never on `ExitPortal`.
**Warning signs:** Player teleports partially (or not at all) after entering a portal; console shows no errors because the coroutine just silently never resumes.

### Pitfall 2: `GetComponentsInChildren` Without `includeInactive: true`
**What goes wrong:** The standby room is `SetActive(false)` from spawn until the portal fires. Any lookup on it (`FindConnector`, `GetComponentInChildren<RoomEntry>()`, `GetComponentInChildren<CameraBound>()`) that omits the `true` argument returns null even though the component exists.
**Why it happens:** Unity's default `GetComponentsInChildren` behavior skips inactive GameObjects.
**How to avoid:** Every lookup against the (still-inactive-at-lookup-time or just-activated) standby room must pass `true` explicitly — this project already does this consistently (see `FloorSpawner.ActivateEnemies()`'s own comment: "Pitfall 2: includeInactive: true 필수").
**Warning signs:** `RoomEntry`/`CameraBound` fallback branches trigger unexpectedly on rooms that visibly do have the marker in the Inspector.

### Pitfall 3: Stale `FloorSpawner` Reference in REQUIREMENTS.md
**What goes wrong:** REQUIREMENTS.md's EXIT-02 text literally says "FloorSpawner 인스펙터에서 조절할 수 있다" — but `FloorSpawner` is orphaned Phase-5 code, disabled in `SampleScene.unity` (`m_IsActive: 0`), and not part of the active generation pipeline since Phase 9 replaced it with `WorldGenerator`.
**Why it happens:** REQUIREMENTS.md predates Phase 9's `FloorSpawner` → `WorldGenerator` migration and was never updated.
**How to avoid:** Treat CONTEXT.md's success criteria (which name `_exitSpawnChance` / `_maxExitsActive` with no mention of `FloorSpawner`) as authoritative. These fields belong on `WorldGenerator`.
**Warning signs:** None at runtime — this is a documentation-only trap that could cause a planner to (incorrectly) re-enable or modify `FloorSpawner.cs`.

### Pitfall 4: `_nextFloorRoom` Treated as a Global Singleton Slot
**What goes wrong:** If `_maxExitsActive` is ever raised above 1 in the Inspector (EXIT-02 explicitly makes this Inspector-adjustable), a single `WorldGenerator._nextFloorRoom` field would be overwritten by the second portal's standby room, orphaning the first portal's standby room in memory with no way to find/destroy it via D-08's GEN-02 cleanup path.
**Why it happens:** The field name and Phase 9's stub comment ("D-04: ... 룸을 비활성 스폰") suggest a singular global reference, but D-08's actual wording ("해당 포탈에 연결된 대기룸") describes a per-portal association.
**How to avoid:** Store the standby-room reference on `ExitPortal.StandbyRoom` (per-instance), and have `RemoveTail()` look it up via `room.GetComponentInChildren<ExitPortal>(true)?.StandbyRoom`. This costs nothing extra when `_maxExitsActive == 1` (today's default per STATE.md) and remains correct if that value is later raised.
**Warning signs:** Memory creep after many floor transitions with `_maxExitsActive > 1`; `Destroy()` never called on some standby rooms.

### Pitfall 5: Enemy Activation Step Is Currently a No-Op
**What goes wrong:** D-04 asks to port all 6 steps of `FloorTransitionSequence()` including "적 활성화" (enemy activation). But `WorldGenerator` (unlike `FloorSpawner`) never calls `EnemySpawner.Spawn(meleePrefab, rangedPrefab)` at any point in the current codebase — only `DebugRoomTeleporter.cs` (a debug-only tool) does. Verified: zero references to `EnemySpawner.Spawn(` or `.Activate()` outside `DebugRoomTeleporter.cs`, and zero `MeleeEnemy`/`RangedEnemy` component instances baked directly into `Room_AllInOne.prefab`.
**Why it happens:** Enemy population for the horizontal Room+Corridor chain was out of scope for Phase 8/9 (ARCH/GEN requirements don't mention enemies) and has not yet been wired up anywhere in the live gameplay path.
**How to avoid:** Do not add melee/ranged prefab wiring to `WorldGenerator` in this phase — that's a scope expansion beyond EXIT-01/02/03. Either omit the "enemy activation" step entirely from the ported sequence, or keep it as a forward-compatible no-op (e.g. iterate `GetComponentsInChildren<EnemySpawner>(true)` and call `.Activate()` — harmless since none currently have a `_spawned` instance to activate). Flag this explicitly for the planner/user rather than silently under- or over-building.
**Warning signs:** None — this is an intentional scope boundary, not a bug, but a planner unaware of it might try to "fully" replicate Phase 5's enemy system into Phase 10.

### Pitfall 6: Double-Trigger on Portal Collider
**What goes wrong:** `OnTriggerEnter2D` firing twice in adjacent physics steps (e.g. multiple player colliders, or the trigger box being large enough for re-entry before `Destroy()` takes effect at end of frame) would call `WorldGenerator.Instance.EnterPortal()` more than once, starting overlapping coroutines that both attempt to destroy the same chain.
**Why it happens:** `Destroy()` is deferred to end-of-frame — the destroyed room/portal GameObject (and its collider) remains "live" for physics purposes for the rest of the current frame.
**How to avoid:** Guard with a `_triggered` bool flag on `ExitPortal` (as in the code example above) — set `true` on first entry before doing anything else, checked at the top of `OnTriggerEnter2D`. This mirrors `FloorSpawner._transitioning`'s exact purpose ("Pitfall 1" in that file's original comments).
**Warning signs:** Console errors from `Destroy()` being called twice on the same object, or the chain ending up with duplicate/missing entries after a transition.

### Pitfall 7: `RoomEntry`/`ExitSpawnPoint` Missing on 4 of 6 Complex_Room Prefabs (User Action Required, Not Code)
**What goes wrong:** Verified directly: `Room_AllInOne.prefab` has zero `ExitSpawnPoint`-equivalent markers and zero `RoomEntry` markers today (only "EnemySpawn_*" children exist under an "EnemySpawns" parent, plus `END_Left`/`END_Right` `RoomConnector`s). `Room_GaugeOutpost.prefab` and (per CONTEXT.md) `Room_RiskCrossing.prefab` already have a `RoomEntry` on a child named **`ENTRY_Bottom`** — this is the naming convention to replicate for the other 4 rooms.
**Why it happens:** These are manual prefab edits (D-03, D-06) that the user must perform in the Unity Editor — no automated tool creates them this phase.
**How to avoid:** The plan must include explicit user-action checklist items (not code tasks): for each of the 6 Complex_Room prefabs, add 2–3 empty child GameObjects (recommend grouping under a new `ExitSpawnPoints` parent, mirroring the existing `EnemySpawns` grouping convention) with `ExitSpawnPoint` attached; for the 4 rooms lacking it, add one child named `ENTRY_Bottom` with `RoomEntry` attached, positioned on a safe walkable floor tile.
**Warning signs:** `TrySpawnExitPortal()` silently does nothing (`points.Length == 0` early-return) for rooms missing markers; ENT teleport falls back to `newRoom.transform.position` (likely mid-air) for rooms missing `RoomEntry`.

## Code Examples

### ExitSpawnPoint.cs (new, mirrors RoomEntry.cs exactly)
```csharp
// Source: Assets/Scripts/World/RoomEntry.cs pattern (existing project convention)
using UnityEngine;

/// <summary>
/// EXIT 포탈 스폰 후보 지점 마커. Room 프리팹 자식 오브젝트에 부착.
/// WorldGenerator가 GetComponentsInChildren&lt;ExitSpawnPoint&gt;(true)로 후보를 찾아 랜덤 선택한다.
/// </summary>
public class ExitSpawnPoint : MonoBehaviour { }
```

### RoomMarkerTool.cs Precedent (for reference — manual placement is primary per D-03, but confirms the AddComponent pattern used elsewhere in this codebase)
```csharp
// Source: Assets/Editor/RoomMarkerTool.cs (existing, Phase 9) — NOT to be extended this phase per D-03,
// shown only to confirm the idempotent AddComponent pattern already established for markers.
var rc = child.gameObject.AddComponent<RoomConnector>();
rc.direction = dir;
```

## State of the Art

| Old Approach (Phase 5) | Current Approach (Phase 10) | When Changed | Impact |
|--------------------------|------------------------------|---------------|--------|
| `RoomExit.OnTriggerEnter2D()` → `FloorSpawner.Instance.AdvanceFloor()` (vertical single-room-at-a-time floors) | `ExitPortal.OnTriggerEnter2D()` → `WorldGenerator.Instance.EnterPortal(this)` (horizontal Room+Corridor chain per floor) | Phase 9 (WorldGenerator introduced) → Phase 10 (portal wiring) | Floor transition now resets a *chain*, not a single room; ENT-teleport target is a chain's start room, not "the next room" |
| `FloorSpawner.SpawnRoom()` synchronously builds the next room's enemies at transition time | Standby room is pre-spawned (inactive) at portal-spawn time, well before the transition fires | Phase 10 (D-04/D-08) | Enables D-08's cleanup requirement — the standby room must already exist so GEN-02 can find and destroy it if unused |
| Single Room prefab pool, `Door/ENT` + `Door/EXIT` naming | Complex_Room pool, `END_Left`/`END_Right` naming, `ENTRY_Bottom` for RoomEntry (verified in `Room_GaugeOutpost.prefab`) | Phase 8 (Complex_Room introduced) | New `ExitSpawnPoint`/`RoomEntry` markers for the 4 remaining Complex_Room prefabs should follow the `ENTRY_Bottom` naming convention already set by `GaugeOutpost`/`RiskCrossing` |

**Deprecated/outdated:** `FloorSpawner.cs` and `RoomExit.cs` are functionally superseded but intentionally left in the codebase (orphaned, not deleted) per CONTEXT.md discretion — do not modify or delete them this phase.

## Open Questions

1. **Exact X position and prefab-selection weighting for the standby room**
   - What we know: D-04 specifies `_chainHeadExitPos.y + _floorHeight` for Y; X should be `0` (matching the `Start()` precedent, since `AlignByEntry()` doesn't apply — see Pitfall/Anti-pattern above).
   - What's unclear: Whether the standby room should use the same uniform-random pick from `_roomPrefabs` as `SpawnNextPair()` (GEN-03 pattern) or a different selection rule.
   - Recommendation: Reuse the exact same `_roomPrefabs[Random.Range(0, _roomPrefabs.Length)]` selection — no decision in CONTEXT.md suggests otherwise, and consistency avoids introducing a second selection algorithm.

2. **Whether the "enemy activation" step (D-04's step 4) should be a real call or an intentional no-op**
   - What we know: `WorldGenerator` has no melee/ranged prefab references and never calls `EnemySpawner.Spawn()` today (see Pitfall 5).
   - What's unclear: Whether wiring actual enemy spawn-then-activate into `WorldGenerator` is implicitly expected by "그대로 재사용" (reuse as-is) in D-04, or whether D-04 only means "reuse the *step structure*," accepting that step 4 currently does nothing.
   - Recommendation: Treat it as intentionally inert this phase (matches the phase's stated requirement scope EXIT-01/02/03, none of which mention enemies) and flag this explicitly to the user/planner as a scope boundary, not silently expand scope to wire up enemy spawning.

3. **Portal visual/collider size**
   - What we know: CONTEXT.md explicitly defers "트리거 콜라이더 크기/모양" to Claude's discretion, and REQUIREMENTS.md's Out of Scope table excludes "이펙트/사운드."
   - What's unclear: Whether any placeholder visual (e.g. a plain `SpriteRenderer`) is expected so the user can see/test the portal during manual playtesting, or a `Gizmos`-only debug visualization (like `RoomConnector.OnDrawGizmos()`) suffices.
   - Recommendation: A simple `BoxCollider2D` (isTrigger) sized to roughly player height/width, with an `OnDrawGizmos()` debug cube (matching `DebugRoomTeleporter.cs`'s and `RoomConnector.cs`'s existing gizmo convention) is sufficient for this phase; a real sprite/VFX is explicitly out of scope.

## Environment Availability

Skipped — Phase 10 introduces no new external dependencies, packages, CLIs, or services. All required Unity packages (2D Tilemap, Input System, URP) are already installed and verified functional through Phase 8/9.

## Validation Architecture

### Test Framework
| Property | Value |
|----------|-------|
| Framework | None — no NUnit/PlayMode test assembly exists in this project (`find **/*.Tests.asmdef` returns only Unity package-cache tests, zero project-owned test assemblies) |
| Config file | none |
| Quick run command | N/A — this project's established validation pattern (Phase 8/9 precedent) is Unity MCP-driven Play Mode automation (`Unity_RunCommand` + `Unity_GetConsoleLogs`) combined with human visual playtesting, not an automated test suite |
| Full suite command | N/A |

### Phase Requirements → Test Map
| Req ID | Behavior | Test Type | Verification Method | Exists? |
|--------|----------|-----------|---------------------|---------|
| EXIT-01 | `_exitSpawnChance=1.0f` → every Room spawn gets a portal; `0.0f` → never | manual + Unity MCP | Set field, Play Mode, scan Hierarchy for `ExitPortal` component count vs. Room count via `Unity_RunCommand`/console log (mirrors Phase 9's GEN-01 verification: "Play 직후 Hierarchy 스캔") | ❌ Wave 0 — add `Debug.Log($"[WorldGenerator] Portal spawned in {room.name}")` at `TrySpawnExitPortal()` success path for console-based verification |
| EXIT-02 | `_maxExitsActive=1` → never more than 1 active portal simultaneously | manual + Unity MCP | Extended playtest (walk past multiple rolled rooms), console-log `_activeExitCount` transitions, confirm never exceeds `_maxExitsActive` | ❌ Wave 0 — same logging addition as above, log `_activeExitCount` on increment/decrement |
| EXIT-03 | Entering EXIT portal → `FloorManager.CurrentFloor` +1, `WorldGenerator` chain reset, new chain starts | manual + Unity MCP | Confirm `FloorManager.CurrentFloor` value via HUD (already wired, Phase 4) or console log before/after; confirm Hierarchy shows old chain destroyed + new single room active post-transition (mirrors Phase 9's 09-03 verification style) | ❌ Wave 0 — add `Debug.Log($"[WorldGenerator] EnterPortal → Floor {FloorManager.CurrentFloor}")` at start of `FloorTransitionSequence()` |

### Sampling Rate
- **Per task commit:** Manual Play Mode smoke test in Unity Editor (enter Play mode, confirm no console errors, confirm expected Debug.Log lines appear) — this project has no automated "quick run" command.
- **Per wave merge:** Full manual playtest walkthrough (spawn portal, walk to it, confirm floor transition) — same pattern as Phase 9's 09-03 plan (`Unity_RunCommand` + `Unity_GetConsoleLogs` + human visual confirmation).
- **Phase gate:** All three success criteria in the phase description manually confirmed before `/gsd:verify-work`, exactly as Phase 9 did (see `09-03-SUMMARY.md`'s explicit criteria table).

### Wave 0 Gaps
- [ ] No test framework gap to fill — project intentionally uses Unity MCP + manual playtesting, consistent with Phase 8/9 precedent. No `pytest`/`jest`-equivalent setup is expected or appropriate for this Unity gameplay prototype.
- [ ] Recommend adding `[WorldGenerator]`-tagged `Debug.Log` statements at the 3 verification points above (portal spawn, active count change, floor transition start) — this is the established console-log-driven verification convention from Phase 9 (`SelectCorridor`, `SpawnNextFloorStandbyRoom` stub, etc. all follow this `[WorldGenerator]` log-tag convention already).

## Sources

### Primary (HIGH confidence — read directly from this codebase)
- `Assets/Scripts/World/WorldGenerator.cs` (183 lines, current state incl. `SpawnNextFloorStandbyRoom()` stub at line 147)
- `Assets/Scripts/World/FloorSpawner.cs` (218 lines, 6-step `FloorTransitionSequence()` reference pattern)
- `Assets/Scripts/World/RoomExit.cs`, `RoomEntry.cs`, `RoomConnector.cs`, `EnemySpawner.cs`, `DebugRoomTeleporter.cs`, `FloorManager.cs`, `ScoreManager.cs`
- `Assets/Scripts/Camera/CameraFollow.cs`, `CameraBound.cs`
- `Assets/Scripts/Player/PlayerController.cs` (LockInput/UnlockInput, lines 207/214), `CombatController.cs` (ForceExitCombatState, lines 176-185)
- `Assets/Editor/RoomMarkerTool.cs`
- `Assets/Prefabs/Rooms/Complex_Room/Room_AllInOne/Room_AllInOne.prefab` and `Room_GaugeOutpost/Room_GaugeOutpost.prefab` (grepped hierarchy names, confirmed `ENTRY_Bottom` naming and absence of markers in AllInOne)
- `.planning/phases/10-exit-portal-floor-transition/10-CONTEXT.md`, `.planning/REQUIREMENTS.md`, `.planning/STATE.md`
- `.planning/phases/09-infinite-gen-cleanup/09-01-SUMMARY.md`, `09-02-SUMMARY.md`, `09-03-SUMMARY.md`
- `.planning/todos/pending/2026-07-03-complex-room-ent.md`
- `.planning/config.json` (confirmed `nyquist_validation: true`, no test-related overrides)

### Secondary (MEDIUM confidence)
- [Coroutines: Halting - Unity, huh, how?](https://unity.huh.how/coroutines/disabling-objects.html) and [Destroying an object with running coroutine - Unity Discussions](https://answers.unity.com/questions/1389406/destroying-an-object-with-running-coroutine.html) — confirms Destroy() of a coroutine-owning GameObject/MonoBehaviour stops the coroutine; cross-checked against this project's own precedent (`FloorSpawner`'s singleton-owned coroutine pattern), which independently corroborates the same conclusion.

### Tertiary (LOW confidence)
None — no unverified claims were needed for this phase's research.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH — no new packages; every reused component read directly from source
- Architecture: HIGH — coroutine-ownership pattern directly precedented by `FloorSpawner.cs` in this same codebase, plus verified via web search on Unity's Destroy/coroutine interaction
- Pitfalls: HIGH — all 7 pitfalls derived from direct source inspection (grep for `EnemySpawner.Spawn`, prefab hierarchy dumps, field declarations) rather than speculation

**Research date:** 2026-07-03
**Valid until:** No expiry concern — this research is scoped entirely to this project's own, already-stable codebase (not a fast-moving external dependency). Revalidate only if Phase 9's `WorldGenerator.cs` or the Complex_Room prefabs are modified before Phase 10 execution begins.
