using UnityEngine;

/// <summary>
/// D-01 (05-CONTEXT.md): 각 Room 프리팹의 위쪽에 자식 오브젝트로 배치되는 출구 트리거.
/// 플레이어가 Trigger Collider2D에 닿으면 FloorSpawner.AdvanceFloor()를 호출한다.
/// 이중 발동은 FloorSpawner._transitioning 플래그가 차단한다 (Pitfall 1 — RESEARCH.md).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomExit : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        ScoreManager.AddRoomClearBonus();
        FloorSpawner.Instance?.AdvanceFloor();
    }
}
