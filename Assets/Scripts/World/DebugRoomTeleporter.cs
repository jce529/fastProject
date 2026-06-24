using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 디버그용 룸 순간이동 장치.
/// isTrigger Collider2D 안에 플레이어가 있는 상태에서 Up(↑ / W) 입력 시
/// targetRoomPrefab을 spawnY에 생성하고 플레이어를 ENT로 이동시킨다.
/// </summary>
public class DebugRoomTeleporter : MonoBehaviour
{
    [Header("Target Room")]
    [SerializeField] private GameObject targetRoomPrefab;
    [SerializeField] private float      spawnY          = 18f;
    [SerializeField] private bool       activateEnemies = false;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject _meleePrefab;
    [SerializeField] private GameObject _rangedPrefab;

    // 플레이어 참조는 Awake에서 자동 탐색 — 프리팹 직렬화 불필요
    private PlayerController _player;
    private Transform        _playerTransform;
    private GameObject       _debugRoom;
    private bool             _playerInZone;

    private void Awake()
    {
        _player          = FindFirstObjectByType<PlayerController>();
        _playerTransform = _player != null ? _player.transform : null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
            _playerInZone = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
            _playerInZone = false;
    }

    private void Update()
    {
        if (!_playerInZone || Keyboard.current == null) return;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame ||
            Keyboard.current.wKey.wasPressedThisFrame)
        {
            TeleportToRoom();
        }
    }

    private void TeleportToRoom()
    {
        if (targetRoomPrefab == null)
        {
            Debug.LogWarning("[DebugTeleport] targetRoomPrefab이 지정되지 않았습니다.");
            return;
        }

        if (_debugRoom != null) Destroy(_debugRoom);

        _debugRoom = Instantiate(targetRoomPrefab, new Vector3(0f, spawnY, 0f), Quaternion.identity);

        foreach (EnemySpawner spawner in _debugRoom.GetComponentsInChildren<EnemySpawner>(true))
        {
            spawner.Spawn(_meleePrefab, _rangedPrefab);
            if (activateEnemies) spawner.Activate();
        }

        RoomEntry entry = _debugRoom.GetComponentInChildren<RoomEntry>(true);
        Vector3 entryPos = entry != null
            ? entry.transform.position
            : _debugRoom.transform.position + Vector3.up * 2f;

        _player.LockInput();
        var rb = _playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        _playerTransform.position = entryPos;
        _player.UnlockInput();

        Debug.Log($"[DebugTeleport] → {targetRoomPrefab.name} | ENT: {entryPos}");
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        if (col is BoxCollider2D box)
            Gizmos.DrawCube(transform.position + (Vector3)box.offset, box.size);

        Gizmos.color = Color.cyan;
        if (col is BoxCollider2D box2)
            Gizmos.DrawWireCube(transform.position + (Vector3)box2.offset, box2.size);
    }
}
