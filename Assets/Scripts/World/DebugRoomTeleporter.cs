using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 디버그용 룸 순간이동 장치.
/// isTrigger Collider2D 안에 플레이어가 있는 상태에서 Enter 입력 시
/// targetRoomPrefab을 이전 스폰 위치 기준 +offsetX/+offsetY 위치에 생성하고 플레이어를 ENT로 이동시킨다.
/// </summary>
public class DebugRoomTeleporter : MonoBehaviour
{
    [Header("Target Room")]
    [SerializeField] private GameObject targetRoomPrefab;
    [SerializeField] private float      offsetX         = 30f;
    [SerializeField] private float      offsetY         = 30f;
    [SerializeField] private bool       activateEnemies = false;

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject _meleePrefab;
    [SerializeField] private GameObject _rangedPrefab;

    // 모든 인스턴스가 공유하는 static 상태 — 다른 텔레포터로 전환 시 이전 방 정리 보장
    private static GameObject s_lastDebugRoom;
    private static Vector3    s_nextSpawnPos = Vector3.zero;

    // 플레이어 참조는 Awake에서 자동 탐색 — 프리팹 직렬화 불필요
    private PlayerController _player;
    private Transform        _playerTransform;
    private bool             _playerInZone;

    private void Awake()
    {
        _player          = FindFirstObjectByType<PlayerController>();
        _playerTransform = _player != null ? _player.transform : null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[DebugTeleport] TriggerEnter: {other.name} (tag={other.tag})");
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            _playerInZone = true;
            Debug.Log("[DebugTeleport] _playerInZone = true");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Debug.Log($"[DebugTeleport] TriggerExit: {other.name} (tag={other.tag})");
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            _playerInZone = false;
            Debug.Log("[DebugTeleport] _playerInZone = false");
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            Debug.Log($"[DebugTeleport] UpArrow pressed | _playerInZone={_playerInZone} | Keyboard.current={Keyboard.current != null}");
        }

        if (!_playerInZone || Keyboard.current == null) return;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
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

        if (s_lastDebugRoom != null) Destroy(s_lastDebugRoom);

        s_nextSpawnPos += new Vector3(offsetX, offsetY, 0f);
        s_lastDebugRoom = Instantiate(targetRoomPrefab, s_nextSpawnPos, Quaternion.identity);

        foreach (EnemySpawner spawner in s_lastDebugRoom.GetComponentsInChildren<EnemySpawner>(true))
        {
            spawner.Spawn(_meleePrefab, _rangedPrefab);
            if (activateEnemies) spawner.Activate();
        }

        RoomEntry entry = s_lastDebugRoom.GetComponentInChildren<RoomEntry>(true);
        Vector3 entryPos = entry != null
            ? entry.transform.position
            : s_lastDebugRoom.transform.position + Vector3.up * 2f;

        _player.LockInput();
        var rb = _playerTransform.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        _playerTransform.position = entryPos;
        _player.UnlockInput();

        CameraFollow cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null)
        {
            CameraBound cb = s_lastDebugRoom.GetComponentInChildren<CameraBound>();
            if (cb != null)
                cam.SnapToRoom(cb.GetWorldBounds());
            else
                cam.SnapToRoom(entryPos);
        }

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
