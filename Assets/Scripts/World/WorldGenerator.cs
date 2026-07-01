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

    // Runtime state
    private List<(GameObject room, GameObject corridor)> _chain
        = new List<(GameObject, GameObject)>();
    private float _currentYDrift;         // D-01: 누적 Y 변위
    private Vector3 _chainHeadExitPos;    // 다음 Corridor ENT 스폰 기준점
    private int _playerCurrentIndex;      // 플레이어가 현재 위치한 체인 인덱스
    private GameObject _nextFloorRoom;    // D-05: 다음 층 대기룸 (체인 외부 참조)

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

        // 다음 스폰 기준점 업데이트 (Room EXIT 위치)
        var roomExit = FindConnector(room, RoomConnector.Direction.Right);
        _chainHeadExitPos = roomExit != null ? roomExit.transform.position : roomEntryPos;

        // D-09: corridor = 이 room의 왼쪽 길로 체인 등록
        _chain.Add((room, corridor));
    }

    private void RemoveTail()
    {
        var (room, corridor) = _chain[0];
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
    /// Phase 10에서 ExitPortal.OnTriggerEnter2D()가 호출한다.
    /// D-04: _chainHeadExitPos.y + _floorHeight 위치에 룸을 비활성(SetActive false) 스폰.
    /// D-06: Phase 9에서는 스텁만 구현. 실제 로직은 Phase 10 범위.
    /// </summary>
    public void SpawnNextFloorStandbyRoom()
    {
        Debug.Log("[WorldGenerator] SpawnNextFloorStandbyRoom — stub (Phase 10에서 구현)");
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
}
