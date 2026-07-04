# Phase 9: 무한 양방향 생성 & 정리 - Research

**Researched:** 2026-07-01
**Domain:** Unity 6 C# — 수평 체인 기반 무한 프로시저럴 생성 / 메모리 정리
**Confidence:** HIGH

---

<user_constraints>
## User Constraints (from CONTEXT.md)

### Locked Decisions

| ID | Decision |
|----|----------|
| D-01 | WorldGenerator는 `_currentYDrift` (float)를 추적한다. Corridor_Up 선택 시 +4, Corridor_Down 선택 시 -4 누적 |
| D-02 | 범위: `_minYDrift = -12f`, `_maxYDrift = +12f`. Inspector 노출로 플레이테스트 후 조정 가능 |
| D-03 | 랜덤 선택 시 drift가 max에 도달하면 Corridor_Up 제외, min에 도달하면 Corridor_Down 제외. 범위 내 옵션 중 Random |
| D-04 | EXIT 포탈 스폰 시 다음 층 대기룸을 `nextFloorBaseY = currentFloorBaseY + _floorHeight`에 동시 스폰, SetActive(false) |
| D-05 | 대기룸은 수평 체인 List에 포함되지 않음. 별도 `_nextFloorRoom` 참조로 관리 |
| D-06 | Phase 9에서는 스폰까지만 구현; Phase 10에서 트리거 연동 |
| D-07 | 대기룸 파괴: Phase 10에서 결정 |
| D-08 | `FloorSpawner.cs`는 Phase 9에서 건드리지 않는다. WorldGenerator는 신규 MonoBehaviour로 작성, SampleScene에 FloorSpawner 대신 배치 |
| D-09 | 체인은 `List<(GameObject room, GameObject corridor)>` 로 관리. corridor는 해당 room의 왼쪽(ENT 방향) 길을 의미 |

### Claude's Discretion

(CONTEXT.md에 명시된 Discretion 항목 없음 — 모든 핵심 사항이 D-01~D-09로 잠겨있음)

### Deferred Ideas (OUT OF SCOPE)

- EXIT 포탈 트리거 로직 (Phase 10)
- 타이머/난이도 (Phase 11)
- 대기룸 파괴 로직 (Phase 10에서 결정)

</user_constraints>

---

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|------------------|
| GEN-01 | 플레이어 이동 방향 기준 앞 2개의 Room+Corridor가 자동으로 미리 생성된다 | AlignByEntry 패턴(TestWorldGenerator), chain List, Update 폴링 전략 |
| GEN-02 | 플레이어가 지나간 지점 기준 2개 초과 뒤의 Room+Corridor가 자동으로 Destroy된다 | 체인 인덱스 기반 tail 정리, Destroy(corridor + room) |
| GEN-03 | Room은 룸 풀에서, Corridor는 3종 중 랜덤 선택된다 | SelectCorridor() + Y drift 제약, roomPrefabs 배열 |

</phase_requirements>

---

## Summary

Phase 9의 핵심 목표는 기존 정적 TestWorldGenerator 패턴을 동적(런타임) 무한 체인으로 확장하는 것이다. Phase 8에서 구축된 인프라 — Corridor 3종 프리팹(CorridorBuilder 결과), RoomConnector 마커 시스템, AlignByEntry/FindConnector 헬퍼 패턴(TestWorldGenerator) — 가 Phase 9 WorldGenerator의 직접 기반이 된다.

**가장 중요한 발견:** 현재 Room 프리팹들에 RoomConnector 컴포넌트가 존재하지 않는다. Phase 8에서 `Add Room Connectors` 에디터 도구가 실행됐다는 SUMMARY가 있지만, 실제 디스크의 프리팹 파일(예: Room_Combat.prefab)에는 RoomConnector GUID(`9e9b49dbd437a5f40994f7a3ddf6d0db`)가 0건이다. Phase 9 Wave 0에서 반드시 해결해야 한다.

**두 번째 중요 발견:** CorridorBuilder.cs는 `UnityEditor` 네임스페이스를 사용하는 에디터 전용 스크립트다. 런타임에 호출 불가. WorldGenerator는 `Instantiate()`로 미리 빌드된 프리팹을 사용한다.

**Primary recommendation:** WorldGenerator.cs를 신규 MonoBehaviour로 작성하되, TestWorldGenerator의 FindConnector/AlignByEntry 패턴을 직접 이식하고, Update()에서 Player X 위치 폴링으로 스폰/정리를 제어한다. Wave 0에서 RoomMarkerTool 업데이트 + 에디터 실행이 선행되어야 한다.

---

## Standard Stack

### Core

| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| UnityEngine | 6000.3.11f1 | MonoBehaviour, Instantiate, Destroy, List | 프로젝트 고정 스택 |
| System.Collections.Generic | .NET Standard 2.1 | `List<(GameObject, GameObject)>` | CONTEXT.md D-09 결정 |
| UnityEngine.InputSystem | 1.19.0 | (WorldGenerator는 직접 사용 안함 — PlayerController가 처리) | — |

### Supporting

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| UnityEditor (RoomMarkerTool 업데이트) | 6000.3.11f1 | 프리팹에 RoomConnector 일괄 부착 | Wave 0 에디터 도구 실행 시 |
| UnityEngine.Tilemaps | 6000.3.11f1 | Corridor 프리팹 내부 타일맵 (WorldGenerator가 직접 쓰지 않음) | CorridorBuilder가 사용, 읽기 전용 |

### Alternatives Considered

| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Update() X position 폴링 | OnTriggerEnter2D 트리거 존 | 트리거 방식은 각 Corridor 입구마다 BoxCollider2D를 배치해야 해 복잡도 증가. 폴링은 float 비교 1회로 충분하므로 모바일 성능 영향 미미 |
| `transform.Find("Door/EXIT")` 직접 조회 | RoomConnector(Right) 컴포넌트 | 경로 하드코딩이지만 프리팹 수정 없이 동작. RoomConnector 방식은 타입 안전성 높음 |
| Coroutine으로 점진적 스폰 | Instantiate를 단일 Update 프레임에 | Coroutine은 프레임 분산에 유리하지만 Phase 9 단순화 우선 — 룸 크기(~2KB 프리팹)에서 동기 Instantiate 허용 |

**Installation:** 신규 패키지 설치 불필요. 모든 의존성이 프로젝트에 이미 존재.

---

## Architecture Patterns

### Recommended Project Structure

```
Assets/Scripts/World/
├── WorldGenerator.cs          # Phase 9 신규 — 무한 체인 생성/정리 MonoBehaviour
├── TestWorldGenerator.cs      # Phase 8 산출물 — Phase 9 이후 비활성화 가능
├── RoomConnector.cs           # 기존 — 방향 마커 컴포넌트 (Left/Right)
├── FloorSpawner.cs            # 기존 — D-08: Phase 9에서 건드리지 않음
Assets/Editor/
├── RoomMarkerTool.cs          # 기존 → Wave 0에서 전체 룸 커버로 업데이트
├── CorridorBuilder.cs         # 기존 — 에디터 전용, 런타임 미사용
Assets/Prefabs/
├── Corridors/
│   ├── Corridor_Flat/Corridor_Flat.prefab   # 존재 확인 ✓
│   ├── Corridor_Up/Corridor_Up.prefab       # 존재 확인 ✓
│   └── Corridor_Down/Corridor_Down.prefab  # 존재 확인 ✓
└── Rooms/
    ├── Room_Combat/Room_Combat.prefab       # Door/ENT(-9,1) Door/EXIT(9,1.5)
    └── (14개 Room 프리팹 — 각각 Door/ENT, Door/EXIT 자식 보유)
```

### Pattern 1: AlignByEntry (인계 패턴 — TestWorldGenerator에서 확인됨)

**What:** Instantiate 후 Left 커넥터 위치가 이전 오브젝트의 Right 커넥터 위치와 일치하도록 루트 Transform을 이동
**When to use:** 모든 스폰 시 (Corridor 스폰, Room 스폰 모두)

```csharp
// Source: Assets/Scripts/World/TestWorldGenerator.cs (Phase 8 검증 완료)

// IMPORTANT: go를 반드시 Vector3.zero로 Instantiate한 뒤 호출해야 한다.
// entry.transform.position == entry.transform.localPosition (root가 원점이므로)
private void AlignByEntry(GameObject go, Vector3 targetWorldPos)
{
    RoomConnector entry = FindConnector(go, RoomConnector.Direction.Left);
    if (entry == null)
    {
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

**핵심 제약**: `Instantiate(prefab, Vector3.zero, Quaternion.identity)` 호출 직후에만 사용 가능.
`entry.transform.position`이 루트 원점 기준 로컬 오프셋과 동일한 값을 갖기 때문.

### Pattern 2: 체인 데이터 구조 (D-09)

**What:** `List<(GameObject room, GameObject corridor)>` — corridor는 해당 room의 왼쪽(ENT 방향) 길
**When to use:** 스폰/정리 양방향 모두

```csharp
// Source: CONTEXT.md D-09 결정

// chain[0] = (StartRoom, null)      // 첫 룸, 왼쪽 Corridor 없음
// chain[1] = (Room2, Corridor1)     // Corridor1 = StartRoom과 Room2 사이
// chain[2] = (Room3, Corridor2)     // Corridor2 = Room2와 Room3 사이

private List<(GameObject room, GameObject corridor)> _chain
    = new List<(GameObject, GameObject)>();

// 스폰: 체인 head에 추가
private void AppendPair(GameObject room, GameObject corridor)
{
    _chain.Add((room, corridor));
}

// 정리: 체인 tail에서 제거
private void RemoveTail()
{
    var (room, corridor) = _chain[0];
    if (corridor != null) Destroy(corridor);
    Destroy(room);
    _chain.RemoveAt(0);
}
```

### Pattern 3: Y Drift 랜덤 선택 (D-01~D-03)

**What:** 축적된 Y 드리프트에 따라 Corridor 3종 중 유효한 선택지 필터링 후 Random
**When to use:** `AppendPair()` 호출 전 Corridor 선택 시

```csharp
// Source: CONTEXT.md D-01, D-02, D-03

[SerializeField] private float _minYDrift = -12f;
[SerializeField] private float _maxYDrift = +12f;
private float _currentYDrift;

private GameObject SelectCorridor()
{
    // 유효 후보 필터
    var candidates = new List<GameObject>();
    if (_currentYDrift < _maxYDrift) candidates.Add(_corridorUp);    // +4 여유 있을 때만
    candidates.Add(_corridorFlat);                                     // 항상 허용
    if (_currentYDrift > _minYDrift) candidates.Add(_corridorDown);   // -4 여유 있을 때만

    var chosen = candidates[Random.Range(0, candidates.Count)];

    // drift 업데이트
    if (chosen == _corridorUp)        _currentYDrift += 4f;
    else if (chosen == _corridorDown) _currentYDrift -= 4f;

    return chosen;
}
```

### Pattern 4: Player X 기반 Update 폴링 (GEN-01, GEN-02)

**What:** Update()에서 player X 위치와 체인 head/tail의 경계를 비교해 스폰/정리 결정
**When to use:** WorldGenerator의 Update()

```csharp
// 개념 스케치 (실제 코드는 Room의 exit X 좌표를 기준으로 계산)
private void Update()
{
    if (_chain.Count == 0) return;

    // HEAD: 앞 2개 보장 (GEN-01)
    // _playerCurrentIndex: 플레이어가 현재 몇 번째 룸에 있는지
    while (_chain.Count - 1 - _playerCurrentIndex < _lookaheadCount)
    {
        SpawnNextPair();
    }

    // TAIL: 뒤 2개 초과 정리 (GEN-02)
    while (_playerCurrentIndex - 0 > _lookbehindCount)
    {
        RemoveTail();
        _playerCurrentIndex--;
    }
}
```

**플레이어 현재 룸 인덱스 판단**: player.transform.position.x를 각 룸의 X 범위(Door/EXIT X 기준)와 비교.
간단한 구현: chain을 순회하며 `room.transform.position.x <= playerX < nextRoom.transform.position.x` 조건 체크.

### Pattern 5: WorldGenerator Inspector 구성

```csharp
public class WorldGenerator : MonoBehaviour
{
    [Header("Room Pool")]
    [SerializeField] private GameObject[] _roomPrefabs;     // 첫 룸 포함 전체 풀

    [Header("Corridor Prefabs")]
    [SerializeField] private GameObject _corridorFlat;
    [SerializeField] private GameObject _corridorUp;
    [SerializeField] private GameObject _corridorDown;

    [Header("References")]
    [SerializeField] private Transform _playerTransform;

    [Header("Chain Settings")]
    [SerializeField] private int _lookaheadCount  = 2;     // 앞 N개 유지 (GEN-01)
    [SerializeField] private int _lookbehindCount = 2;     // 뒤 N개 유지 (GEN-02)

    [Header("Y Drift Bounds")]
    [SerializeField] private float _minYDrift = -12f;      // D-02
    [SerializeField] private float _maxYDrift = +12f;      // D-02

    [Header("Next Floor Standby (Phase 10 연동)")]
    [SerializeField] private float _floorHeight = 40f;     // D-04: nextFloorBaseY 계산용

    // Runtime state
    private List<(GameObject room, GameObject corridor)> _chain = new();
    private float _currentYDrift;                           // D-01
    private Vector3 _chainHeadExitPos;                      // 다음 스폰 기준점
    private GameObject _nextFloorRoom;                      // D-05
}
```

### Anti-Patterns to Avoid

- **Awake에서 모든 룸 스폰 금지**: 초기 체인은 Start()에서만. Awake 의존성 순서 문제 방지.
- **CorridorBuilder.Run() 런타임 호출 금지**: CorridorBuilder는 UnityEditor 네임스페이스 — 빌드 시 존재하지 않음.
- **FindObjectsOfType 금지**: ROADMAP Stack Constraints — "never FindObjectsOfType in Update". 체인 리스트로 직접 참조.
- **Instantiate 시 Vector3.zero 아닌 위치 사용 금지**: AlignByEntry 공식은 원점 기준. 다른 위치로 Instantiate하면 정렬 오류.
- **FloorSpawner 수정 금지**: D-08 명시적 결정 — Phase 9에서 절대 건드리지 않음.

---

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| 오브젝트 정렬 | 수학 공식 직접 계산 | AlignByEntry (TestWorldGenerator 패턴) | Phase 8에서 검증된 패턴; 재발명 불필요 |
| 방향 마커 탐색 | 자식 이름 비교 | GetComponentsInChildren\<RoomConnector\>(true) | 네이밍 변경에 강함, Gizmo 포함 |
| 씬 오브젝트 관리 | static 리스트 | `List<(GameObject, GameObject)>` 인스턴스 필드 | FloorManager static 패턴은 데이터 전용; GO 수명 관리는 MonoBehaviour 소유 |
| Corridor 프리팹 생성 | 런타임 Tilemap 조작 | 기존 Corridor_Flat/Up/Down 프리팹 Instantiate | CorridorBuilder는 에디터 전용; 프리팹이 이미 존재 |

**Key insight:** Phase 8 산출물(Corridor 프리팹 3종 + RoomConnector 컴포넌트 + AlignByEntry 패턴)이 Phase 9 WorldGenerator의 90%를 구성한다. 핵심 로직은 TestWorldGenerator를 동적 버전으로 확장하는 것이다.

---

## Common Pitfalls

### Pitfall 1: Room 프리팹에 RoomConnector(Right) 없음 — 현재 실제 상태
**What goes wrong:** WorldGenerator의 `FindConnector(room, Direction.Right)` 가 null을 반환 → `prevExitPos = Vector3.zero` → 모든 다음 오브젝트가 원점 근처에 쌓임
**Why it happens:** `Assets/Editor/RoomMarkerTool.cs`는 현재 4개 룸의 `Door/ENT`에만 `Direction.Left` 커넥터를 추가. `Door/EXIT`에 `Direction.Right` 추가 로직 없음. 실제 디스크의 Room 프리팹(Room_Combat.prefab 확인)에 RoomConnector GUID 0건 확인.
**How to avoid:** Wave 0에서 RoomMarkerTool을 업데이트해 모든 룸의 `Door/ENT`(Left)와 `Door/EXIT`(Right) 양쪽에 커넥터를 멱등적으로 추가. Unity 에디터에서 실행 후 프리팹 저장 확인.
**Warning signs:** Play 모드에서 모든 룸이 (0,0) 근처에 겹쳐 생성됨.

### Pitfall 2: AlignByEntry를 Vector3.zero 아닌 위치에서 호출
**What goes wrong:** `go.transform.position = targetWorldPos - entry.transform.position` 공식은 go가 원점에 있을 때만 성립. `Instantiate(prefab, someOtherPos, ...)` 후 호출하면 정렬 오프셋 이중 적용.
**Why it happens:** `entry.transform.position`(월드 좌표)이 `entry.transform.localPosition`(로컬 좌표)과 같아지는 조건이 root.position == Vector3.zero 일 때뿐.
**How to avoid:** 항상 `Instantiate(prefab, Vector3.zero, Quaternion.identity)` → `AlignByEntry(go, target)` 순서 유지.
**Warning signs:** 체인이 점점 오른쪽으로 올바르게 가지만 Y가 이중으로 밀림.

### Pitfall 3: CorridorBuilder.Run() 런타임 호출 시도
**What goes wrong:** CorridorBuilder.cs에 `using UnityEditor;`가 있어 빌드 시 컴파일 오류. 에디터에서도 잘못된 위치(Assets/Editor 외부)에서 호출 불가.
**Why it happens:** CorridorBuilder는 `[MenuItem]` 에디터 도구. 오직 메뉴 실행 시에만 사용.
**How to avoid:** WorldGenerator는 `Instantiate(_corridorFlat/Up/Down, ...)` 만 사용. CorridorBuilder는 Phase 8 빌드 시 1회 실행으로 완료.
**Warning signs:** `Assets/Editor/` 밖에서 CorridorBuilder를 참조하려 할 때 컴파일 오류.

### Pitfall 4: 체인 tail 정리 시 인덱스 불일치
**What goes wrong:** `RemoveTail()`을 호출한 뒤 `_playerCurrentIndex`를 감소시키지 않으면 플레이어가 chain[0]이 아닌 위치에 있다고 오계산.
**Why it happens:** chain이 shift되면 인덱스가 같아도 참조 룸이 바뀜.
**How to avoid:** `RemoveTail()` 호출 직후 `_playerCurrentIndex--` 항상 쌍으로 실행.
**Warning signs:** 룸이 Destroy되는데 GEN-01 스폰이 멈추거나 반대로 과다 스폰됨.

### Pitfall 5: Room_Stair 등 수직 높이 차가 큰 룸을 초기 풀에 포함
**What goes wrong:** Room_Stair의 Door/EXIT Y가 Door/ENT Y보다 높으면(EDITOR-CHECKLIST 기준 +9 units), 다음 Corridor의 ENT 기준점이 예상보다 9 units 높아짐. Y drift 시스템이 Corridor의 ΔY(±4)만 추적하므로 룸 자체 ΔY를 놓침 → 총 Y 변화가 _maxYDrift를 초과 가능.
**Why it happens:** D-01~D-03의 drift 계산이 Corridor만 추적; Room의 ENT-EXIT Y 차이는 추적 안 함.
**How to avoid:** Phase 9 초기 `_roomPrefabs` 풀에는 ENT Y ≈ EXIT Y인 룸(Room_Combat, Room_Chase, Room_Dodge, Room_Gap 등)만 포함. Room_Stair, Room_Stair 계열은 Phase 9 이후 drift 공식 확장 후 추가.
**Warning signs:** 체인 Y가 예상 범위를 벗어남, 플레이어가 화면 밖으로 이탈.

### Pitfall 6: FloorSpawner와 WorldGenerator 동시 활성
**What goes wrong:** FloorSpawner는 Awake에서 Room을 스폰. WorldGenerator도 Start에서 Room을 스폰. 두 시스템이 동시에 동작하면 룸이 중복 생성되고 `FloorSpawner.Instance`가 충돌.
**Why it happens:** D-08에서 "FloorSpawner 대신 배치"라고 명시했지만 씬에서 비활성화하지 않으면 Awake가 실행됨.
**How to avoid:** SampleScene에서 FloorSpawner 컴포넌트를 Inspector에서 비활성화(checkbox 해제)하거나 해당 GameObject를 비활성화. TestWorldGenerator도 동일하게 비활성화.
**Warning signs:** Console에 NullReferenceException 또는 "Instance already set" 경고.

### Pitfall 7: CameraFollow bounds 잔류
**What goes wrong:** FloorSpawner가 `SnapCameraToRoom(Bounds)` 를 호출해 놓은 상태로 WorldGenerator가 시작하면 카메라가 이전 룸 bounds에 갇힘.
**Why it happens:** CameraFollow._hasBounds가 true로 잔류.
**How to avoid:** WorldGenerator.Start()에서 `_cameraFollow.SnapToRoom(Vector3.zero)` 호출해 `_hasBounds = false`로 초기화. 이후 플레이어를 자유 추적.
**Warning signs:** 플레이어가 오른쪽으로 이동해도 카메라가 고정된 채 따라오지 않음.

---

## Code Examples

### 체인 첫 스폰 (Start 시)

```csharp
// Source: TestWorldGenerator.cs 패턴 + D-09 결정 조합

private void Start()
{
    // 1. 시작 룸 스폰 (원점)
    var startRoomPrefab = _roomPrefabs[Random.Range(0, _roomPrefabs.Length)];
    var startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
    _chain.Add((startRoom, null));

    // 시작 룸의 Right connector = 다음 스폰 기준점
    var startRoomExit = FindConnector(startRoom, RoomConnector.Direction.Right);
    _chainHeadExitPos = startRoomExit != null
        ? startRoomExit.transform.position
        : Vector3.zero;

    // 2. 앞 2개 스폰 (GEN-01)
    for (int i = 0; i < _lookaheadCount; i++)
        SpawnNextPair();

    // 3. 카메라 bounds 초기화 (Pitfall 7 방지)
    if (_cameraFollow != null)
        _cameraFollow.SnapToRoom(Vector3.zero);
}
```

### SpawnNextPair() — 체인 head에 Corridor+Room 추가

```csharp
private void SpawnNextPair()
{
    // 1. Corridor 선택 (Y drift 제약 적용, GEN-03)
    var corridorPrefab = SelectCorridor();
    var corridor = Instantiate(corridorPrefab, Vector3.zero, Quaternion.identity);
    AlignByEntry(corridor, _chainHeadExitPos);

    // 2. Corridor의 Exit 위치 = 다음 Room의 기준점
    var corridorExit = FindConnector(corridor, RoomConnector.Direction.Right);
    var roomEntryPos = corridorExit != null ? corridorExit.transform.position : _chainHeadExitPos;

    // 3. Room 스폰 (랜덤 풀, GEN-03)
    var roomPrefab = _roomPrefabs[Random.Range(0, _roomPrefabs.Length)];
    var room = Instantiate(roomPrefab, Vector3.zero, Quaternion.identity);
    AlignByEntry(room, roomEntryPos);

    // 4. 다음 스폰 기준점 업데이트
    var roomExit = FindConnector(room, RoomConnector.Direction.Right);
    _chainHeadExitPos = roomExit != null ? roomExit.transform.position : roomEntryPos;

    // 5. 체인 등록 (corridor = 이 room의 왼쪽 길)
    _chain.Add((room, corridor));
}
```

### RoomMarkerTool 업데이트 패턴 (Wave 0)

```csharp
// Source: Assets/Editor/RoomMarkerTool.cs 확장 패턴

// 기존: Door/ENT 에만 Left 추가
// 추가 필요: Door/EXIT 에 Right 추가, 대상 룸 전체로 확장

private static readonly string[] AllRoomNames =
{
    "Room_Combat", "Room_Hunt", "Room_Ladder", "Room_LadderDanger",
    "Room_Gap", "Room_Fall", "Room_Sniper", "Room_Stair",
    "Room_Crossroad", "Room_Chase", "Room_Dodge", "Room_Chain",
    "Room_Recovery", "Room_Mixed"
};

// 각 룸에 대해:
AddConnector(root, "Door/ENT",  RoomConnector.Direction.Left);   // 기존
AddConnector(root, "Door/EXIT", RoomConnector.Direction.Right);  // 신규 추가
```

---

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| FloorSpawner 수직 층 전환 | WorldGenerator 수평 체인 | Phase 9 (신규) | 구조 완전 대체 — FloorSpawner 비활성화 |
| BoxCollider2D 기반 Corridor | TilemapCollider2D 기반 | Quick task 260701-k1e | WorldGenerator는 Instantiate만 → 내부 구조 무관 |
| TestWorldGenerator 정적 1회 배치 | WorldGenerator 동적 무한 배치 | Phase 9 | Update 기반 플레이어 위치 반응형 |

**Deprecated/outdated:**
- `TestWorldGenerator.cs`: Phase 8 플레이테스트 전용. Phase 9 WorldGenerator 완성 후 SampleScene에서 비활성화.
- `FloorSpawner.cs` 수직 스폰: v3.0에서 역할 종료. D-08에 따라 비활성화(삭제 X).

---

## Open Questions

1. **Room_Stair 등 수직 룸을 Phase 9 풀에 포함할 것인가?**
   - What we know: Door/EXIT Y가 Door/ENT Y보다 높음(EDITOR-CHECKLIST 기준 +9). Y drift 시스템은 Corridor 기준으로만 추적.
   - What's unclear: Room 자체 ΔY를 drift에 누적할지, 별도 계산을 할지.
   - Recommendation: Phase 9 초기 풀에서 제외. 검증 후 Phase 10~11에서 추가 검토.

2. **플레이어 현재 룸 인덱스 결정 방법**
   - What we know: player.transform.position.x를 체인 룸 위치와 비교.
   - What's unclear: 정확한 룸 경계를 어떻게 정의할 것인가 (Door/EXIT X? Room 루트 X + 반폭?).
   - Recommendation: `Door/EXIT` Right 커넥터의 X 좌표를 경계로 사용. 플레이어 X > 커넥터 X면 다음 룸.

3. **Next Floor Standby Room 트리거 — Phase 9 범위에서 어떻게 처리?**
   - What we know: D-04~D-06: EXIT 포탈 스폰 시 동시 스폰. 하지만 EXIT 포탈은 Phase 10.
   - What's unclear: Phase 9에서 대기룸을 언제 스폰할 것인가.
   - Recommendation: WorldGenerator에 `public void SpawnNextFloorStandbyRoom()` 메서드를 스텁(구현은 완성, 호출은 Phase 10이 담당)으로 추가. Phase 9 자체는 WorldGenerator.Start()에서 테스트 목적으로 한 번 호출해 스폰 확인 가능.

---

## Environment Availability

Step 2.6: SKIPPED — Phase 9는 순수 C# 코드 + Unity 에디터 도구 작성. 외부 도구/서비스 의존성 없음. Unity 6000.3.11f1 + 기존 패키지만 사용.

---

## Validation Architecture

nyquist_validation: true (config.json 확인됨).

### Test Framework

| Property | Value |
|----------|-------|
| Framework | Unity Test Framework 1.6.0 (NUnit-based) |
| Config file | 없음 (별도 설정 파일 없음) |
| Quick run command | Unity Test Runner → Edit Mode Tests 실행 |
| Full suite command | Unity Test Runner → All Tests (Edit + Play Mode) |

### Phase Requirements → Test Map

| Req ID | Behavior | Test Type | Automated Command | File Exists? |
|--------|----------|-----------|-------------------|-------------|
| GEN-01 | 앞 2개 Room+Corridor 자동 생성 | Play Mode (수동 관찰) | N/A — Scene Hierarchy에서 직접 확인 | ❌ Wave 0 필요 없음 (플레이테스트) |
| GEN-02 | 뒤 2개 초과 시 자동 Destroy | Play Mode (수동 관찰) | N/A — Hierarchy에서 직접 확인 | ❌ Wave 0 필요 없음 (플레이테스트) |
| GEN-03 | Corridor 3종 랜덤 선택 | Edit Mode Unit | `SelectCorridor()` 격리 테스트 | ❌ Wave 0 |

**GEN-01, GEN-02 자동화 불가 이유:** Unity Play Mode에서 GameObject 생성/소멸을 자동으로 검증하려면 복잡한 씬 셋업이 필요하고 유지 비용이 성과 대비 높음. 5회 Play 반복 플레이테스트(Success Criteria 3번)가 더 신뢰성 높음.

**GEN-03 Edit Mode 테스트 가능:** `SelectCorridor()`를 `public`으로 노출하거나 `internal`로 선언 후 테스트 어셈블리에서 호출. Y drift 경계 조건(drift = +12 시 Up 제외, drift = -12 시 Down 제외)을 단위 검증.

### Sampling Rate

- **Per task commit:** Console 오류 0개 확인 (컴파일)
- **Per wave:** Unity Play Mode에서 씬 실행, Hierarchy에서 체인 GO 수 확인
- **Phase gate:** Success Criteria 3항목 전부 충족 후 `/gsd:verify-work`

### Wave 0 Gaps

- [ ] `Assets/Tests/EditMode/WorldGeneratorTests.cs` — GEN-03 SelectCorridor 경계 조건 (optional, 우선순위 낮음)
- [x] 테스트 프레임워크: com.unity.test-framework 1.6.0 이미 설치됨 — 추가 설치 불필요

---

## Runtime State Inventory

> Phase 9는 FloorSpawner 대체지만 리네임/리팩터 아님 — 새 MonoBehaviour 추가. 그러나 기존 런타임 상태 점검 필요.

| Category | Items Found | Action Required |
|----------|-------------|------------------|
| Stored data | 없음 — 프로토타입에 DB/영구 저장 없음 | 없음 |
| Live service config | 없음 — 외부 서비스 미사용 | 없음 |
| OS-registered state | 없음 | 없음 |
| Secrets/env vars | 없음 | 없음 |
| Build artifacts | `Assets/Scenes/SampleScene.unity` — FloorSpawner 컴포넌트 활성 상태 | Wave 3에서 FloorSpawner 비활성화 + WorldGenerator 배치 |

**SampleScene 현황 (git status 기준):** SampleScene.unity가 `M`(수정됨, 미커밋) 상태. TestWorldGenerator 배치 관련 Phase 8 수동 작업 결과로 추정. Phase 9에서 WorldGenerator 추가 시 전체 씬 저장 필요.

---

## Project Constraints (from CLAUDE.md)

- `Time.unscaledDeltaTime` — 모든 타이머/쿨다운 (WorldGenerator에서 코루틴 타이밍 시 적용)
- `Physics2D.OverlapCircleNonAlloc()` — Update에서 LINQ/FindObjectsOfType 금지
- `Rigidbody2D`: Continuous detection + Interpolate
- 추가 패키지 설치 금지 (프로토타입 범위)
- GSD 워크플로우 준수: 코드 작성 전 반드시 PLAN 확인
- Phase 9 범위 외 기능(EXIT 포탈, 타이머, 난이도) 코드 추가 금지

---

## Sources

### Primary (HIGH confidence)

- `Assets/Scripts/World/TestWorldGenerator.cs` — AlignByEntry, FindConnector 패턴 직접 확인
- `Assets/Scripts/World/RoomConnector.cs` — Direction enum, connectedObject 필드 확인
- `Assets/Editor/CorridorBuilder.cs` — 에디터 전용 확인, ENT/EXIT 좌표 확인
- `Assets/Editor/RoomMarkerTool.cs` — 현재 4개 룸 + Left 전용 한계 직접 확인
- `.planning/phases/09-infinite-gen-cleanup/09-CONTEXT.md` — D-01~D-09 결정 원본
- `Assets/Prefabs/Rooms/Room_Combat/Room_Combat.prefab` — ENT(-9,1), EXIT(9,1.5), RoomConnector 0건 직접 확인
- `Assets/Prefabs/Corridors/Corridor_Flat/Corridor_Flat.prefab` — 3종 모두 존재 확인 (Glob)
- `Assets/Scripts/Camera/CameraFollow.cs` — _hasBounds 동작 확인

### Secondary (MEDIUM confidence)

- `.planning/phases/08-room-corridor-architecture/08-03-SUMMARY.md` — Phase 8 검증 완료 기록 (에디터 실행 기반)
- `.planning/phases/08-room-corridor-architecture/08-EDITOR-CHECKLIST.md` — 룸 ENT/EXIT 설계 수치 (실제 프리팹과 다를 수 있음)

### Tertiary (LOW confidence)

- 없음

---

## Metadata

**Confidence breakdown:**
- Standard Stack: HIGH — 모두 기존 프로젝트 패키지, 신규 설치 없음
- Architecture: HIGH — TestWorldGenerator.cs 코드 직접 확인, D-09 체인 구조 명확
- Pitfalls: HIGH — Room 프리팹 직접 검사로 RoomConnector 부재 확인, CorridorBuilder 에디터 전용 코드 확인
- Room 수치 데이터: MEDIUM — 실제 prefab 읽기(Room_Combat)만 확인, 나머지 13개 룸 개별 미검증

**Research date:** 2026-07-01
**Valid until:** 2026-07-31 (Phase 9 실행 전 Room 프리팹 추가 수정 없다는 전제)
