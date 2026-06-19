using System.Collections;
using UnityEngine;

/// <summary>
/// Phase 5 — 층 스폰 및 전환 시퀀스 MonoBehaviour.
/// FloorManager(static data)와 분리된 씬 싱글톤 패턴 (04-01 FloorManager 패턴 유지).
///
/// 요구사항:
///   FLOOR-01: _floor1RoomPrefab(고정) + _roomPool(가중치 랜덤) 으로 층 프리팹 선택
///   FLOOR-02: AdvanceFloor() 호출 → FloorTransitionSequence() 6단계 코루틴
///   FLOOR-03: SpawnRoom() 내 적 Instantiate 직후 SetActive(false); ActivateEnemies()에서 true
///   FLOOR-04: 플레이어 순간이동 후 이전 층 Destroy()
///
/// Inspector 연결 필요:
///   _floor1RoomPrefab — Room_Combat 프리팹 (D-05: 1층 고정)
///   _roomPool         — 2층 이후 랜덤 선택 Room 프리팹 배열 (D-03: 4~5개)
///   _playerTransform  — Player GameObject의 Transform
///   _player           — PlayerController 컴포넌트
///   _meleeEnemyPrefab — MeleeEnemy 프리팹
///   _rangedEnemyPrefab— RangedEnemy 프리팹
///   _roomHeight       — 18 (Claude's discretion — 실제 Room 높이 측정 후 조정)
/// </summary>
public class FloorSpawner : MonoBehaviour
{
    // -- Inspector fields -------------------------------------------------------
    [SerializeField] private GameObject       _floor1RoomPrefab;   // D-05: 1층 고정 Room
    [SerializeField] private GameObject[]     _roomPool;            // D-03: 2층+ 랜덤 풀 (4~5개)
    [SerializeField] private Transform        _playerTransform;
    [SerializeField] private PlayerController _player;             // Pitfall 7: Inspector 연결, static 아님
    [SerializeField] private GameObject       _meleeEnemyPrefab;
    [SerializeField] private GameObject       _rangedEnemyPrefab;
    [SerializeField] private float            _roomHeight = 18f;    // Claude's discretion

    // -- Runtime state ----------------------------------------------------------
    private GameObject _currentRoom;
    private bool       _transitioning;

    // -- Singleton (씬 내 단일 인스턴스 보장) ----------------------------------
    public static FloorSpawner Instance { get; private set; }

    // -- Unity lifecycle --------------------------------------------------------

    private void Awake()
    {
        Instance = this;
        // D-05: 1층은 항상 고정 Room 스폰 (적 없음 — GetEnemyCount(1) returns (0,0))
        _currentRoom = SpawnRoom(_floor1RoomPrefab, 1);
    }

    // -- Public API (RoomExit.OnTriggerEnter2D가 호출) -------------------------

    /// <summary>
    /// 층 전환을 시작한다. _transitioning 플래그가 이중 호출을 방지한다 (Pitfall 1).
    /// </summary>
    public void AdvanceFloor()
    {
        if (_transitioning) return;
        StartCoroutine(FloorTransitionSequence());
    }

    // -- 전환 시퀀스 (FLOOR-02, D-08 6단계) -----------------------------------

    /// <summary>
    /// FLOOR-02: 6단계 층 전환 시퀀스.
    /// 모든 yield는 WaitForSecondsRealtime 사용 — Time.timeScale이 0일 때도 진행됨 (Pitfall 3).
    /// </summary>
    private IEnumerator FloorTransitionSequence()
    {
        _transitioning = true;

        // [Step 1] 조작 불가 — 이동·점프·공격 입력 차단
        _player.LockInput();

        // FloorManager 증가 및 다음 층 스폰 (적은 SetActive(false) 상태 — FLOOR-03)
        FloorManager.CurrentFloor++;
        GameObject nextRoom = SpawnRoom(SelectNextRoom(), FloorManager.CurrentFloor);

        // [Step 2] 순간이동 — 플레이어를 새 층 바닥 위 2유닛으로 즉시 이동
        // Pitfall 4: 위치는 Instantiate 시 이미 설정됨 — Enemy Awake가 정확한 위치로 실행됨
        float newY = (FloorManager.CurrentFloor - 1) * _roomHeight + 2f;
        var rb = _playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        _playerTransform.position = new Vector3(
            _playerTransform.position.x,
            newY,
            0f
        );

        // [Step 3] 카메라 Y스냅 — CameraFollow.LateUpdate가 target.position을 매 프레임 추적.
        // 플레이어 순간이동으로 자동 완성됨. 한 프레임 양보해 LateUpdate 실행 허용.
        yield return null;

        // [Step 4] 가림막 해제 — 새 층의 비활성 적들 활성화 (FLOOR-03)
        // Pitfall 2: includeInactive: true 필수 — SetActive(false)인 적도 반환
        ActivateEnemies(nextRoom);

        // [Step 5] 짧은 대기 — FSM이 첫 Update 사이클을 마칠 시간 (적이 플레이어 인식 시작)
        yield return new WaitForSecondsRealtime(0.05f);

        // [Step 6] 조작 재개
        _player.UnlockInput();

        // 이전 층 파괴 (FLOOR-04, D-09) — 모바일 메모리: 현재 층만 씬에 유지
        if (_currentRoom != null) Destroy(_currentRoom);
        _currentRoom = nextRoom;

        _transitioning = false;
    }

    // -- 내부 헬퍼 --------------------------------------------------------------

    /// <summary>
    /// 2층 이상에서 랜덤 Room 프리팹을 선택한다.
    /// 배열에 같은 프리팹을 여러 번 등록하면 가중치 효과 (Claude's discretion).
    /// </summary>
    private GameObject SelectNextRoom()
    {
        if (_roomPool == null || _roomPool.Length == 0) return _floor1RoomPrefab;
        return _roomPool[Random.Range(0, _roomPool.Length)];
    }

    /// <summary>
    /// Room 프리팹을 올바른 Y 위치에 Instantiate하고 적 스폰 포인트에서 적을 생성한다.
    /// 생성된 적은 SetActive(false) 상태로 유지 — ActivateEnemies()가 활성화함 (FLOOR-03).
    /// </summary>
    private GameObject SpawnRoom(GameObject prefab, int floor)
    {
        // Y = (floor - 1) * roomHeight  →  1층 = Y0, 2층 = Y18, 3층 = Y36, ...
        Vector3 roomOrigin = new Vector3(0f, (floor - 1) * _roomHeight, 0f);
        GameObject room = Instantiate(prefab, roomOrigin, Quaternion.identity);

        // EnemySpawnPoint 태그가 붙은 자식들에 적 Instantiate
        (int meleeCount, int rangedCount) = GetEnemyCount(floor);
        int spawnIndex = 0;

        foreach (Transform child in room.GetComponentsInChildren<Transform>(true))
        {
            if (!child.CompareTag("EnemySpawnPoint")) continue;

            GameObject enemyPrefab;
            if (spawnIndex < meleeCount)
                enemyPrefab = _meleeEnemyPrefab;
            else if (spawnIndex < meleeCount + rangedCount)
                enemyPrefab = _rangedEnemyPrefab;
            else
                break; // 스폰 포인트가 더 있어도 난이도 테이블 초과 시 중단

            if (enemyPrefab == null) { spawnIndex++; continue; }

            // Pitfall 4: Instantiate(prefab, position, rotation) — Awake가 올바른 위치로 실행됨
            GameObject enemy = Instantiate(enemyPrefab, child.position, Quaternion.identity);
            enemy.SetActive(false); // FLOOR-03: 전환 4단계까지 비활성
            spawnIndex++;
        }

        return room;
    }

    /// <summary>
    /// FLOOR-03: 전환 4단계에서 호출. Room 내 모든 IEnemy를 활성화한다.
    /// Pitfall 2: GetComponentsInChildren에 true(includeInactive) 필수 전달.
    /// </summary>
    private void ActivateEnemies(GameObject room)
    {
        // IEnemy를 직접 GetComponentsInChildren으로 가져올 수 없으므로 MonoBehaviour 경유
        foreach (MonoBehaviour mb in room.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb is IEnemy)
                mb.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// D-07: 층 번호 기반 적 수 난이도 테이블.
    /// 1층: 적 없음 (튜토리얼).
    /// 1~5층: 근접 위주 / 6~10층: 혼합 / 11층+: 원거리 비율 확대.
    /// </summary>
    private (int melee, int ranged) GetEnemyCount(int floor)
    {
        if (floor == 1)   return (0, 0);
        if (floor <= 5)   return (Random.Range(2, 4), Random.Range(0, 2));
        if (floor <= 10)  return (2, Random.Range(1, 3));
        return (2, Random.Range(2, 4));
    }
}
