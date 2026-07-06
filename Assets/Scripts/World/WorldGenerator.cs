using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phase 9: 플레이어 위치 기반 수평 Room+Corridor 무한 체인 생성/정리.
/// SampleScene에서 FloorSpawner/TestWorldGenerator 대신 배치한다.
/// D-08: FloorSpawner.cs는 Phase 9에서 수정하지 않는다.
/// D-09: 체인 List에서 corridor = 해당 room의 왼쪽(ENT 방향) 길.
/// </summary>
public class WorldGenerator : MonoBehaviour
{
    [Header("Room Pool")]
    [SerializeField] private GameObject[] _roomPrefabs;

    [Header("Corridor Prefabs")]
    [SerializeField] private GameObject _corridorFlat;
    [SerializeField] private GameObject _corridorUp;
    [SerializeField] private GameObject _corridorDown;

    [Header("References")]
    [SerializeField] private Transform _playerTransform;
    [SerializeField] private CameraFollow _cameraFollow;  // Pitfall 7: SnapToRoom 초기화용

    [Header("Chain Settings")]
    [SerializeField] private int _lookaheadCount  = 2;   // GEN-01: 앞 N개 유지
    [SerializeField] private int _lookbehindCount = 2;   // GEN-02: 뒤 N개 유지

    [Header("Y Drift Bounds")]
    [SerializeField] private float _minYDrift = -12f;    // D-02
    [SerializeField] private float _maxYDrift = +12f;    // D-02

    [Header("Next Floor Standby (Phase 10 연동)")]
    [SerializeField] private float _floorHeight = 40f;   // D-04: nextFloorBaseY 계산용

    [Header("Exit Portal (Phase 10)")]
    [SerializeField] private GameObject _exitPortalPrefab;
    [SerializeField, Range(0f, 1f)] private float _exitSpawnChance = 0.15f;  // EXIT-01: 기본 15%
    [SerializeField] private int _maxExitsActive = 1;                        // EXIT-02: 최대 동시 활성 개수

    [Header("References (Phase 10 추가)")]
    [SerializeField] private PlayerController _player;           // LockInput/UnlockInput 호출용
    [SerializeField] private CombatController _combatController; // ForceExitCombatState 호출용

    // Runtime state
    private List<(GameObject room, GameObject corridor)> _chain
        = new List<(GameObject, GameObject)>();
    private float _currentYDrift;         // D-01: 누적 Y 변위
    private Vector3 _chainHeadExitPos;    // 다음 Corridor ENT 스폰 기준점
    private int _playerCurrentIndex;      // 플레이어가 현재 위치한 체인 인덱스
    private int _activeExitCount;         // D-08: 현재 활성 포탈 수

    public static WorldGenerator Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (_roomPrefabs == null || _roomPrefabs.Length == 0)
        {
            Debug.LogError("[WorldGenerator] _roomPrefabs is empty — Inspector에서 할당 필요");
            return;
        }

        // 1. 시작 룸 스폰 — 반드시 Vector3.zero 기준 (Pitfall 2: AlignByEntry 공식 성립 조건)
        var startRoomPrefab = _roomPrefabs[Random.Range(0, _roomPrefabs.Length)];
        var startRoom = Instantiate(startRoomPrefab, Vector3.zero, Quaternion.identity);
        _chain.Add((startRoom, null)); // D-09: 첫 룸은 왼쪽 Corridor 없음
        TrySpawnExitPortal(startRoom); // EXIT-01: 시작 룸도 포탈 스폰 대상 (예외 없음)

        // 시작 룸 EXIT 위치 → 첫 Corridor 스폰 기준점
        var startExit = FindConnector(startRoom, RoomConnector.Direction.Right);
        _chainHeadExitPos = startExit != null ? startExit.transform.position : Vector3.zero;

        // 2. 초기 lookahead 스폰 (GEN-01: 시작 시 앞 2쌍 미리 생성)
        for (int i = 0; i < _lookaheadCount; i++)
            SpawnNextPair();

        // 3. CameraFollow bounds 초기화 (Pitfall 7: FloorSpawner가 설정한 _hasBounds 잔류 방지)
        if (_cameraFollow != null)
            _cameraFollow.SnapToRoom(Vector3.zero);
    }

    private void SpawnNextPair()
    {
        // Corridor 선택 및 스폰 (GEN-03: 3종 랜덤, D-01~D-03: Y drift 제약)
        var corridorPrefab = SelectCorridor();
        var corridor = Instantiate(corridorPrefab, Vector3.zero, Quaternion.identity);
        AlignByEntry(corridor, _chainHeadExitPos);

        // Corridor EXIT → Room ENT 스폰 기준점
        var corridorExit = FindConnector(corridor, RoomConnector.Direction.Right);
        var roomEntryPos = corridorExit != null ? corridorExit.transform.position : _chainHeadExitPos;

        // Room 스폰 (GEN-03: 룸 풀 랜덤 선택)
        var roomPrefab = _roomPrefabs[Random.Range(0, _roomPrefabs.Length)];
        var room = Instantiate(roomPrefab, Vector3.zero, Quaternion.identity);
        AlignByEntry(room, roomEntryPos);
        TrySpawnExitPortal(room);

        // 다음 스폰 기준점 업데이트 (Room EXIT 위치)
        var roomExit = FindConnector(room, RoomConnector.Direction.Right);
        _chainHeadExitPos = roomExit != null ? roomExit.transform.position : roomEntryPos;

        // D-09: corridor = 이 room의 왼쪽 길로 체인 등록
        _chain.Add((room, corridor));
    }

    private void RemoveTail()
    {
        var (room, corridor) = _chain[0];

        // D-08: 이 room이 보유한 포탈의 대기룸을 함께 정리 — 대기룸 메모리 누수 방지 + 포탈 스폰 기회 복원
        ExitPortal portal = room.GetComponentInChildren<ExitPortal>(true);
        if (portal != null && portal.StandbyRoom != null)
        {
            Destroy(portal.StandbyRoom);
            _activeExitCount--;
            Debug.Log($"[WorldGenerator] _activeExitCount = {_activeExitCount}");
        }

        if (corridor != null) Destroy(corridor);
        Destroy(room);
        _chain.RemoveAt(0);
    }

    private GameObject SelectCorridor()
    {
        // D-01~D-03: 현재 Y drift 범위 내 유효 후보만 선택
        var candidates = new List<GameObject>(3);
        if (_currentYDrift < _maxYDrift) candidates.Add(_corridorUp);     // +4 여유 있을 때만
        candidates.Add(_corridorFlat);                                      // 항상 허용
        if (_currentYDrift > _minYDrift) candidates.Add(_corridorDown);    // -4 여유 있을 때만

        var chosen = candidates[Random.Range(0, candidates.Count)];

        // D-01: 선택된 Corridor에 따라 drift 누적
        if      (chosen == _corridorUp)   _currentYDrift += 4f;
        else if (chosen == _corridorDown) _currentYDrift -= 4f;

        return chosen;
    }

    private void AlignByEntry(GameObject go, Vector3 targetWorldPos)
    {
        // Source: TestWorldGenerator.cs — Phase 8에서 검증된 패턴
        // CRITICAL: Instantiate(prefab, Vector3.zero, Quaternion.identity) 직후에만 호출해야 함 (Pitfall 2)
        RoomConnector entry = FindConnector(go, RoomConnector.Direction.Left);
        if (entry == null)
        {
            Debug.LogWarning($"[WorldGenerator] {go.name} has no Left RoomConnector — placed at {targetWorldPos}");
            go.transform.position = targetWorldPos;
            return;
        }
        // entry.transform.position == 루트 원점 기준 로컬 오프셋 (root가 Vector3.zero이므로)
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

    /// <summary>
    /// EXIT-01/EXIT-02: Room 스폰 직후 호출된다. 확률 롤을 통과하고 활성 포탈 수가
    /// _maxExitsActive 미만이면, room의 ExitSpawnPoint 후보 중 하나에 포탈을 생성하고
    /// 다음 층 대기룸을 즉시(비활성 상태로) 함께 스폰해 D-08 정리 대상으로 연결한다.
    /// </summary>
    private void TrySpawnExitPortal(GameObject room)
    {
        if (_activeExitCount >= _maxExitsActive) return;
        if (Random.value > _exitSpawnChance) return;

        if (_exitPortalPrefab == null)
        {
            Debug.LogWarning("[WorldGenerator] _exitPortalPrefab is empty — Inspector에서 할당 필요");
            return;
        }

        var points = room.GetComponentsInChildren<ExitSpawnPoint>(true);
        if (points.Length == 0) return; // ExitSpawnPoint 마커 미배치 (D-03 수동 배치 대기)

        var point = points[Random.Range(0, points.Length)];
        var portalGO = Instantiate(_exitPortalPrefab, point.transform.position, Quaternion.identity, room.transform);
        var portal = portalGO.GetComponent<ExitPortal>();

        // D-04: 다음 층 대기룸을 지금 미리 스폰 — Vector3.zero 기준 Instantiate 후 X=0 고정 배치
        // (AlignByEntry는 적용 불가 — D-07이 옛 체인을 전부 파괴하므로 수평 연속성 요구가 없다)
        var standbyPrefab = _roomPrefabs[Random.Range(0, _roomPrefabs.Length)];
        var standbyPos = new Vector3(0f, _chainHeadExitPos.y + _floorHeight, 0f);
        var standbyRoom = Instantiate(standbyPrefab, standbyPos, Quaternion.identity);
        standbyRoom.SetActive(false);
        portal.StandbyRoom = standbyRoom;

        _activeExitCount++;
        Debug.Log($"[WorldGenerator] Portal spawned in {room.name}");
        Debug.Log($"[WorldGenerator] _activeExitCount = {_activeExitCount}");
    }

    private void Update()
    {
        if (_playerTransform == null || _chain.Count == 0) return;
        UpdatePlayerIndex();

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
    }

    private void UpdatePlayerIndex()
    {
        // 플레이어 X > 현재 룸의 Door/EXIT X → 다음 룸 인덱스로 진행
        for (int i = _playerCurrentIndex; i < _chain.Count - 1; i++)
        {
            var exitConnector = FindConnector(_chain[i].room, RoomConnector.Direction.Right);
            if (exitConnector == null) break;
            if (_playerTransform.position.x > exitConnector.transform.position.x)
                _playerCurrentIndex = i + 1;
            else
                break;
        }
    }

    /// <summary>
    /// EXIT-03: ExitPortal.OnTriggerEnter2D()가 호출한다. 전환 코루틴은 반드시 WorldGenerator(this)에서
    /// 실행되어야 한다 — 시퀀스 도중 옛 체인(포탈이 속한 room 포함)을 Destroy하기 때문에,
    /// ExitPortal 자신에게서 StartCoroutine을 호출하면 Destroy 시점에 코루틴이 즉시 중단된다 (Pitfall 1).
    /// </summary>
    public void EnterPortal(ExitPortal portal)
    {
        StartCoroutine(FloorTransitionSequence(portal));
    }

    private IEnumerator FloorTransitionSequence(ExitPortal portal)
    {
        Debug.Log($"[WorldGenerator] EnterPortal → Floor {FloorManager.CurrentFloor}");

        // Step 1 — 입력 잠금 (D-04 6단계 중 1단계). ForceExitCombatState는 LockInput 이전에 호출해야 한다.
        _combatController?.ForceExitCombatState();
        _player.LockInput();

        FloorManager.CurrentFloor++;

        // D-07 — 기존 체인(현재 room+corridor 전부) 즉시 Destroy
        // 리뷰 수정: 입장한 portal 외 다른 미사용 포탈의 대기룸도 함께 Destroy
        // (RemoveTail()의 D-08 정리 패턴과 동일 — _maxExitsActive > 1일 때 고아 GameObject 누수 방지)
        foreach (var (chainRoom, chainCorridor) in _chain)
        {
            var orphanPortal = chainRoom.GetComponentInChildren<ExitPortal>(true);
            if (orphanPortal != null && orphanPortal != portal && orphanPortal.StandbyRoom != null)
            {
                Destroy(orphanPortal.StandbyRoom);
            }

            if (chainCorridor != null) Destroy(chainCorridor);
            Destroy(chainRoom);
        }
        _chain.Clear();
        _activeExitCount = 0; // 옛 체인에 있던 모든 포탈(입장한 포탈 + 정리된 미사용 포탈)이 함께 사라짐
        Debug.Log($"[WorldGenerator] _activeExitCount = {_activeExitCount}");

        GameObject newRoom = portal.StandbyRoom;
        newRoom.SetActive(true);
        _chain.Add((newRoom, null));
        _playerCurrentIndex = 0;
        _currentYDrift = 0f; // 새 층은 드리프트 예산 초기화

        var exit = FindConnector(newRoom, RoomConnector.Direction.Right);
        _chainHeadExitPos = exit != null ? exit.transform.position : newRoom.transform.position;

        // Step 2 — ExitSpawnPoint 텔레포트 (RoomEntry 대체 — 포탈 스폰과 동일 마커 재사용, 10-TRANSITION-DESIGN.md)
        var spawnPoints = newRoom.GetComponentsInChildren<ExitSpawnPoint>(true);
        Vector3 teleportPos = spawnPoints.Length > 0
            ? spawnPoints[Random.Range(0, spawnPoints.Length)].transform.position
            : newRoom.transform.position;
        var rb = _playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        _playerTransform.position = teleportPos;

        // Step 3 — 카메라 스냅
        CameraBound cb = newRoom.GetComponentInChildren<CameraBound>(true);
        if (_cameraFollow != null)
        {
            if (cb != null) _cameraFollow.SnapToRoom(cb.GetWorldBounds());
            else _cameraFollow.SnapToRoom(teleportPos);
        }

        yield return null; // Step 3.5 — LateUpdate가 카메라 위치를 반영하도록 한 프레임 양보

        // Step 4 — 적 활성화: WorldGenerator는 현재 EnemySpawner.Spawn()을 호출하지 않으므로 의도적 no-op
        // (Pitfall 5 — 적 스폰 배선은 EXIT-01/02/03 범위 밖. 이 단계는 구조적 자리만 유지한다.)

        yield return new WaitForSecondsRealtime(0.05f); // Step 5

        _player.UnlockInput(); // Step 6
    }
}
