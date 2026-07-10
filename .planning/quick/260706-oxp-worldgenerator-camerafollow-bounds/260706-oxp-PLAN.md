---
phase: quick
plan: 260706-oxp
type: execute
wave: 1
depends_on: []
files_modified:
  - Assets/Scripts/World/WorldGenerator.cs
autonomous: false
requirements: []
---

<objective>
Two related changes to `Assets/Scripts/World/WorldGenerator.cs`:

1. **Bidirectional initial generation** — `Start()` currently only pre-generates `_lookaheadCount` Room+Corridor pairs to the RIGHT of the start room, leaving the player standing at a hard "world edge" on the left. Add a leftward-growing mirror (`SpawnPrevPair()`) that pre-generates `_lookbehindCount` pairs to the LEFT at startup, inserted at the front of `_chain`, with `_playerCurrentIndex` corrected to account for the shift.
2. **Merged camera bounds (continuous tracking)** — `CameraFollow.SnapToRoom(Bounds)` currently receives a single room's `CameraBound`, so the camera visibly stalls while the player crosses a Corridor or a narrow Room. Add a `RecomputeCameraBounds()` helper that merges (`Bounds.Encapsulate`) every `CameraBound` across the whole active `_chain` (Rooms AND Corridors — both prefab families already carry `CameraBound`, confirmed via `Assets/Prefabs/Corridors/*/*.prefab`) and feeds the merged `Bounds` to `CameraFollow.SnapToRoom(Bounds)`. Wire this in wherever the chain changes: end of `Start()`, after the extend/shrink while-loops in `Update()`, and at Step 3 of `FloorTransitionSequence()`.

Purpose: Eliminates the "start of the world" dead edge on the left and the camera freeze while transiting Corridors/narrow Rooms — both break the core loop's sense of continuous forward momentum.
Output: `Assets/Scripts/World/WorldGenerator.cs` updated in place. No new files, no `CameraFollow.cs` changes (its existing `SnapToRoom(Bounds)` overload and clamp math already support an arbitrarily-sized merged Bounds — confirmed via `Assets/Scripts/Camera/CameraFollow.cs`).
</objective>

<execution_context>
@D:/새 폴더/Fast/.claude/get-shit-done/workflows/execute-plan.md
</execution_context>

<context>
@.planning/STATE.md
@Assets/Scripts/World/WorldGenerator.cs
@Assets/Scripts/World/RoomConnector.cs
@Assets/Scripts/Camera/CameraBound.cs
@Assets/Scripts/Camera/CameraFollow.cs
</context>

<interfaces>
<!-- RoomConnector — direction marker used by FindConnector/AlignByEntry/AlignByExit -->
```csharp
public class RoomConnector : MonoBehaviour
{
    public enum Direction { Left, Right }
    [SerializeField] public Direction direction;
    [SerializeField] public GameObject connectedObject;
}
```

<!-- CameraBound — every Room and Corridor prefab (confirmed: Corridor_Flat/Up/Down all carry CameraBound too) exposes this -->
```csharp
public class CameraBound : MonoBehaviour
{
    public Bounds GetWorldBounds(); // center = transform.position, size = serialized _size
}
```

<!-- CameraFollow — NOT modified by this plan, shown for contract only -->
```csharp
public void SnapToRoom(Vector3 worldCenter);  // no-bounds fallback (used when a room/corridor has no CameraBound)
public void SnapToRoom(Bounds worldBounds);   // sets _hasBounds=true, _activeBounds=worldBounds; LateUpdate() clamps player-follow inside it
```

<!-- Current WorldGenerator.cs helpers to mirror/extend (verbatim from the file as of this plan) -->
```csharp
private void AlignByEntry(GameObject go, Vector3 targetWorldPos)
{
    RoomConnector entry = FindConnector(go, RoomConnector.Direction.Left);
    if (entry == null)
    {
        Debug.LogWarning($"[WorldGenerator] {go.name} has no Left RoomConnector — placed at {targetWorldPos}");
        go.transform.position = targetWorldPos;
        return;
    }
    go.transform.position = targetWorldPos - entry.transform.position;
}

private RoomConnector FindConnector(GameObject go, RoomConnector.Direction direction)
{
    foreach (RoomConnector rc in go.GetComponentsInChildren<RoomConnector>(true))
    {
        if (rc.direction == direction) return rc;
    }
    return null;
}
```
</interfaces>

<tasks>

<task type="auto">
  <name>Task 1: Bidirectional initial chain generation (SpawnPrevPair + Start() wiring)</name>
  <files>Assets/Scripts/World/WorldGenerator.cs</files>
  <action>
Add a leftward mirror of the existing rightward chain-growth so `Start()` pre-generates `_lookbehindCount` pairs to the LEFT of the start room, in addition to the existing `_lookaheadCount` pairs to the right. Do NOT touch camera logic in this task — that is Task 2.

**1. Add a new field** next to `_chainHeadExitPos` (around line 49):
```csharp
private Vector3 _chainHeadExitPos;    // 다음 Corridor ENT 스폰 기준점
private Vector3 _chainTailEntryPos;   // GEN-04: 다음 leftward Corridor EXIT 스폰 기준점 (신규)
```

**2. Add `AlignByExit()` helper** — left-right mirror of `AlignByEntry()`. Place it directly after `AlignByEntry()`:
```csharp
private void AlignByExit(GameObject go, Vector3 targetWorldPos)
{
    // AlignByEntry()의 좌우 대칭 버전 — go의 Right(EXIT) 커넥터가 targetWorldPos에 오도록 배치한다.
    // SpawnPrevPair()가 체인을 왼쪽으로 확장할 때 사용한다.
    // CRITICAL: Instantiate(prefab, Vector3.zero, Quaternion.identity) 직후에만 호출해야 함 (Pitfall 2와 동일 이유)
    RoomConnector exit = FindConnector(go, RoomConnector.Direction.Right);
    if (exit == null)
    {
        Debug.LogWarning($"[WorldGenerator] {go.name} has no Right RoomConnector — placed at {targetWorldPos}");
        go.transform.position = targetWorldPos;
        return;
    }
    go.transform.position = targetWorldPos - exit.transform.position;
}
```

**3. Add `SpawnPrevPair()` method** — place directly after `SpawnNextPair()`:
```csharp
/// <summary>
/// GEN-04: 시작 시점 좌측(lookbehind) 초기 생성 전용. SpawnNextPair()의 좌우 대칭 버전 —
/// Corridor+Room을 생성해 _chain의 맨 앞(index 0)에 삽입한다.
/// D-09 체인 표현("corridor = 해당 room의 왼쪽 길") 유지를 위해, 기존에 _chain[0]에 있던
/// (구) 좌측 끝 room의 corridor 필드(null)를 새로 생성한 corridor로 교체한 뒤, 새 room을
/// corridor=null 상태로 맨 앞에 삽입한다.
/// </summary>
private void SpawnPrevPair()
{
    // Corridor 선택 및 스폰 — 이 Corridor의 오른쪽(EXIT) 커넥터가 기존 체인 좌측 끝 ENT와 맞물려야 함
    var corridorPrefab = SelectCorridor();
    var corridor = Instantiate(corridorPrefab, Vector3.zero, Quaternion.identity);
    AlignByExit(corridor, _chainTailEntryPos);

    // Corridor ENT(왼쪽) → 새 Room의 EXIT(오른쪽) 스폰 기준점
    var corridorEntry = FindConnector(corridor, RoomConnector.Direction.Left);
    var roomExitPos = corridorEntry != null ? corridorEntry.transform.position : _chainTailEntryPos;

    // Room 스폰 (GEN-03: 룸 풀 랜덤 선택 — SpawnNextPair()와 동일 정책)
    var roomPrefab = _roomPrefabs[Random.Range(0, _roomPrefabs.Length)];
    var room = Instantiate(roomPrefab, Vector3.zero, Quaternion.identity);
    AlignByExit(room, roomExitPos);
    TrySpawnExitPortal(room);

    // 다음 leftward 스폰 기준점 업데이트 (새 Room의 ENT 위치)
    var roomEntry = FindConnector(room, RoomConnector.Direction.Left);
    _chainTailEntryPos = roomEntry != null ? roomEntry.transform.position : roomExitPos;

    // D-09: 기존 체인 맨 앞 room이 갖고 있던 corridor(null)를 새로 만든 corridor로 교체
    var (oldFrontRoom, _) = _chain[0];
    _chain[0] = (oldFrontRoom, corridor);

    // 새 room을 맨 앞에 삽입 — 왼쪽 Corridor 없음(다음 SpawnPrevPair 호출 시 교체될 수 있음)
    _chain.Insert(0, (room, null));
}
```

**4. Edit `Start()`** — insert `_chainTailEntryPos` initialization right after the existing `_chainHeadExitPos` initialization, and add the leftward generation loop right after the existing rightward loop. Leave everything else in `Start()` untouched (including the camera-bounds block at the end — Task 2 handles that).

Find:
```csharp
        // 시작 룸 EXIT 위치 → 첫 Corridor 스폰 기준점
        var startExit = FindConnector(startRoom, RoomConnector.Direction.Right);
        _chainHeadExitPos = startExit != null ? startExit.transform.position : Vector3.zero;

        // 2. 초기 lookahead 스폰 (GEN-01: 시작 시 앞 2쌍 미리 생성)
        for (int i = 0; i < _lookaheadCount; i++)
            SpawnNextPair();
```

Replace with:
```csharp
        // 시작 룸 EXIT 위치 → 첫 Corridor 스폰 기준점
        var startExit = FindConnector(startRoom, RoomConnector.Direction.Right);
        _chainHeadExitPos = startExit != null ? startExit.transform.position : Vector3.zero;

        // 시작 룸 ENT 위치 → 첫 leftward Corridor 스폰 기준점 (GEN-04)
        var startEntry = FindConnector(startRoom, RoomConnector.Direction.Left);
        _chainTailEntryPos = startEntry != null ? startEntry.transform.position : Vector3.zero;

        // 2. 초기 lookahead 스폰 (GEN-01: 시작 시 앞 _lookaheadCount쌍 미리 생성 — 오른쪽)
        for (int i = 0; i < _lookaheadCount; i++)
            SpawnNextPair();

        // 2.5 초기 lookbehind 스폰 (GEN-04: 시작 시 뒤 _lookbehindCount쌍 미리 생성 — 왼쪽).
        // 플레이어가 월드의 "시작 지점 왼쪽 끝"에 서 있는 느낌을 없애기 위함.
        // _lookbehindCount를 재사용하는 이유: Update()의 GEN-02 트리밍이 이미 "플레이어 뒤로
        // _lookbehindCount쌍을 유지"라는 불변식을 강제하므로, 그 불변식을 시작 시점부터
        // 성립시키는 것이 자연스럽다 (별도 필드를 두면 두 값이 어긋날 때 트리밍이 시작 직후
        // 방금 생성한 pair를 도로 지우는 모순이 생길 수 있음).
        for (int i = 0; i < _lookbehindCount; i++)
            SpawnPrevPair();
        _playerCurrentIndex = _lookbehindCount; // 앞에 삽입된 쌍 개수만큼 시작 룸의 인덱스가 밀림
```
  </action>
  <verify>
    <automated>MISSING — no automated test harness exists for WorldGenerator (MonoBehaviour driving prefab instantiation); verification is Task 3's manual Play Mode checkpoint. As a compile-time gate: Unity Editor Console must show 0 errors after this task's edits are saved.</automated>
  </verify>
  <done>
`WorldGenerator.cs` compiles with no errors. `SpawnPrevPair()` and `AlignByExit()` exist and mirror `SpawnNextPair()`/`AlignByEntry()`. `Start()` generates `_lookbehindCount` pairs to the left of the start room (front-inserted into `_chain`, D-09 corridor-ownership invariant preserved) in addition to the existing `_lookaheadCount` pairs to the right, and `_playerCurrentIndex` is set to `_lookbehindCount` to correctly point at the start room after the front-inserts.
  </done>
</task>

<task type="auto">
  <name>Task 2: Merged CameraBound recompute across the active chain</name>
  <files>Assets/Scripts/World/WorldGenerator.cs</files>
  <action>
Add a `RecomputeCameraBounds()` helper that merges every `CameraBound` across `_chain` (Rooms and Corridors both — confirmed both prefab families carry `CameraBound`) into one `Bounds` via `Bounds.Encapsulate`, and call it at every point the chain composition changes.

**1. Add the helper** — place it after `FindConnector()` and before `TrySpawnExitPortal()`:
```csharp
/// <summary>
/// 현재 _chain 전체(Room + Corridor)의 CameraBound를 순회해 하나로 병합한 Bounds를 계산하고
/// CameraFollow에 전달한다. 코리도어를 지나거나 CameraBound가 좁은 Room에 있을 때도 카메라가
/// 멈추지 않고 계속 추적하도록, 체인이 바뀔 때마다(Start/SpawnNextPair/RemoveTail/
/// FloorTransitionSequence) 호출해야 한다.
/// </summary>
private void RecomputeCameraBounds()
{
    if (_cameraFollow == null || _chain.Count == 0) return;

    bool hasBounds = false;
    Bounds merged = default;

    foreach (var (room, corridor) in _chain)
    {
        foreach (CameraBound cb in room.GetComponentsInChildren<CameraBound>(true))
        {
            if (!hasBounds) { merged = cb.GetWorldBounds(); hasBounds = true; }
            else merged.Encapsulate(cb.GetWorldBounds());
        }

        if (corridor == null) continue;
        foreach (CameraBound cb in corridor.GetComponentsInChildren<CameraBound>(true))
        {
            if (!hasBounds) { merged = cb.GetWorldBounds(); hasBounds = true; }
            else merged.Encapsulate(cb.GetWorldBounds());
        }
    }

    if (hasBounds) _cameraFollow.SnapToRoom(merged);
    else _cameraFollow.SnapToRoom(_playerTransform != null ? _playerTransform.position : Vector3.zero);
}
```

**2. Replace the camera-bounds block at the end of `Start()`.** Find:
```csharp
        // 3. CameraFollow bounds 초기화 (Pitfall 7: FloorSpawner가 설정한 _hasBounds 잔류 방지)
        // 사용자 발견 버그: Vector3.zero로 고정 스냅하면 플레이어가 ExitSpawnPoint로 텔레포트된 뒤
        // 카메라가 룸 원점만 비춰 플레이어가 화면 밖에 있는 것처럼 보임 (FloorTransitionSequence Step 3과 동일 패턴으로 교체)
        if (_cameraFollow != null)
        {
            CameraBound startCb = startRoom.GetComponentInChildren<CameraBound>(true);
            if (startCb != null) _cameraFollow.SnapToRoom(startCb.GetWorldBounds());
            else _cameraFollow.SnapToRoom(startTeleportPos);
        }
```
Replace with:
```csharp
        // 3. CameraFollow bounds 초기화 — 전체 체인(양방향 lookahead+lookbehind) 병합 Bounds 사용
        // (Pitfall 7: FloorSpawner가 설정한 _hasBounds 잔류 방지. 단일 룸 스냅 대신 병합 Bounds로
        // 교체해 Corridor 통과 중에도 카메라가 멈추지 않도록 한다.)
        RecomputeCameraBounds();
```

**3. Wire `Update()`'s extend/shrink loops.** Find:
```csharp
        // GEN-01: 플레이어 앞 _lookaheadCount개 Room+Corridor 보장
        while (_chain.Count - 1 - _playerCurrentIndex < _lookaheadCount)
            SpawnNextPair();

        // GEN-02: 플레이어 뒤 _lookbehindCount개 초과 시 tail 정리
        // Pitfall 4: RemoveTail 후 _playerCurrentIndex-- 반드시 쌍으로 실행
        while (_playerCurrentIndex > _lookbehindCount)
        {
            RemoveTail();
            _playerCurrentIndex--;
        }
```
Replace with:
```csharp
        // GEN-01: 플레이어 앞 _lookaheadCount개 Room+Corridor 보장
        bool chainChanged = false;
        while (_chain.Count - 1 - _playerCurrentIndex < _lookaheadCount)
        {
            SpawnNextPair();
            chainChanged = true;
        }

        // GEN-02: 플레이어 뒤 _lookbehindCount개 초과 시 tail 정리
        // Pitfall 4: RemoveTail 후 _playerCurrentIndex-- 반드시 쌍으로 실행
        while (_playerCurrentIndex > _lookbehindCount)
        {
            RemoveTail();
            _playerCurrentIndex--;
            chainChanged = true;
        }

        // 체인이 실제로 바뀐 프레임에만 재계산 — 매 프레임 GetComponentsInChildren 호출을 피해
        // 모바일 GC 압박을 줄인다 (CLAUDE.md 모바일 메모리 관리 원칙)
        if (chainChanged) RecomputeCameraBounds();
```

**4. Replace Step 3 of `FloorTransitionSequence()`.** Find:
```csharp
        // Step 3 — 카메라 스냅
        CameraBound cb = newRoom.GetComponentInChildren<CameraBound>(true);
        if (_cameraFollow != null)
        {
            if (cb != null) _cameraFollow.SnapToRoom(cb.GetWorldBounds());
            else _cameraFollow.SnapToRoom(teleportPos);
        }
```
Replace with:
```csharp
        // Step 3 — 카메라 스냅 (체인이 새 room 하나로 리셋된 직후이므로 병합 Bounds = 이 room의 Bounds)
        RecomputeCameraBounds();
```

Do NOT modify `Assets/Scripts/Camera/CameraFollow.cs` — its existing `SnapToRoom(Bounds)` overload and `LateUpdate()` clamp math already handle an arbitrarily wide merged Bounds correctly.
  </action>
  <verify>
    <automated>MISSING — no automated test harness exists for WorldGenerator/CameraFollow (runtime scene behavior); verification is Task 3's manual Play Mode checkpoint. As a compile-time gate: Unity Editor Console must show 0 errors after this task's edits are saved.</automated>
  </verify>
  <done>
`RecomputeCameraBounds()` exists and merges CameraBound from every Room+Corridor in `_chain` via `Bounds.Encapsulate`, falling back to player position if none found. It is called: once at the end of `Start()` (replacing the old single-room snap), once in `Update()` only on frames where the chain actually changed (replacing nothing — `Update()` had no camera logic before), and once at Step 3 of `FloorTransitionSequence()` (replacing the old single-room snap). `CameraFollow.cs` is unmodified.
  </done>
</task>

<task type="checkpoint:human-verify" gate="blocking">
  <what-built>
Bidirectional initial world generation (rooms/corridors now pre-spawn on both sides of the start room) and continuous merged-Bounds camera tracking (camera no longer freezes crossing Corridors or narrow Rooms).
  </what-built>
  <how-to-verify>
Open the project in Unity Editor, load `SampleScene`, enter Play Mode.

1. **Bidirectional generation:** Immediately on Play, check the Hierarchy/Scene view — there should be Room+Corridor pairs visible to BOTH the left AND right of the player's start position (not just to the right). Walk the player left past the start room; you should be able to keep walking left through the pre-generated pairs instead of hitting an empty void at the world origin.
2. **Continuous camera tracking:** Walk the player right through at least one full Room -> Corridor -> Room -> Corridor sequence. Confirm the camera keeps smoothly following the player the whole time — it must NOT freeze/stall while the player is inside a Corridor, and must NOT freeze if a Room's CameraBound is narrower than the camera viewport. Also walk left through a Corridor back toward the start room and confirm the same continuous tracking.
3. **Regression check:** Trigger a floor transition (walk into an EXIT portal) and confirm the camera still snaps correctly to the new floor's start room (no leftover merged-Bounds from the previous floor).
4. Confirm the Unity Console shows no new errors/warnings during any of the above.
  </how-to-verify>
  <resume-signal>Type "approved" if both behaviors work as described, or describe what's broken (e.g. "camera still freezes in corridors" or "no rooms generated on the left").</resume-signal>
</task>

</tasks>

<verification>
1. `Assets/Scripts/World/WorldGenerator.cs` compiles with 0 errors/warnings.
2. On Play, `_chain` contains `_lookbehindCount` pairs to the left of the start room and `_lookaheadCount` pairs to the right, with `_playerCurrentIndex` pointing at the start room's actual index.
3. Camera tracks the player continuously across Room/Corridor boundaries within the active chain span, with no visible freeze.
4. Floor transition (`FloorTransitionSequence`) still resets the chain and camera correctly to a single new room.
</verification>

<success_criteria>
Player never encounters a dead "start of the world" edge on the left at game start, and the camera never visibly stalls while traversing Corridors or narrow Rooms within the currently active chain.
</success_criteria>

<output>
No SUMMARY.md required by default for quick tasks, but given the scope (two behavior changes, chain-index math), write `.planning/quick/260706-oxp-worldgenerator-camerafollow-bounds/260706-oxp-SUMMARY.md` after human-verify approval, and add a row to STATE.md's Quick Tasks Completed table.
</output>
