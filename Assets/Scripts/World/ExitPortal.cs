using UnityEngine;

/// <summary>
/// EXIT-03: WorldGenerator.TrySpawnExitPortal()이 Room 스폰 시 Instantiate하는 포탈 트리거.
/// 플레이어가 진입하면 WorldGenerator.Instance.EnterPortal(this)를 호출해 층 전환 코루틴을 시작시킨다.
///
/// CRITICAL: 코루틴은 반드시 WorldGenerator(영속 싱글톤)에서 실행되어야 한다.
/// D-07에 의해 전환 시퀀스 도중 이 포탈이 속한 room(및 이 컴포넌트 자신)이 Destroy되므로,
/// ExitPortal 자신에게서 StartCoroutine을 호출하면 Destroy 시점에 시퀀스가 즉시 중단된다
/// (Unity는 GameObject/컴포넌트가 파괴되면 그것이 소유한 모든 코루틴을 즉시 중단한다).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ExitPortal : MonoBehaviour
{
    /// <summary>
    /// D-08: 이 포탈에 연결된 다음 층 대기룸(비활성 상태로 미리 스폰됨).
    /// WorldGenerator.TrySpawnExitPortal()이 스폰 직후 채운다. 포탈별 저장이므로
    /// _maxExitsActive &gt; 1이어도 서로 다른 포탈의 대기룸을 덮어쓰지 않는다.
    /// </summary>
    public GameObject StandbyRoom { get; set; }

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;
        _triggered = true;
        WorldGenerator.Instance.EnterPortal(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.size);
        else
            Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
