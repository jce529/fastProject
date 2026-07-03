using UnityEngine;

/// <summary>
/// EXIT 포탈 스폰 후보 지점 마커. Room 프리팹 자식 오브젝트에 부착한다 (D-01).
/// WorldGenerator.TrySpawnExitPortal()이 GetComponentsInChildren&lt;ExitSpawnPoint&gt;(true)로
/// 후보를 찾아 랜덤으로 하나를 선택한다.
/// </summary>
public class ExitSpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
