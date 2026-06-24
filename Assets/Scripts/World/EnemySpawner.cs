using UnityEngine;

/// <summary>
/// Room 프리팹의 각 스폰 포인트에 부착하는 마커 컴포넌트.
/// FloorSpawner가 Spawn() 호출 → 적을 자식으로 비활성 상태로 생성.
/// FloorSpawner가 Activate() 호출 → 적 활성화 (FLOOR-03).
/// 룸이 Destroy()될 때 자식인 적도 함께 제거된다.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged }

    [SerializeField] private EnemyType _type = EnemyType.Melee;

    private GameObject _spawned;

    public void Spawn(GameObject meleePrefab, GameObject rangedPrefab)
    {
        GameObject prefab = _type == EnemyType.Melee ? meleePrefab : rangedPrefab;
        if (prefab == null) return;

        _spawned = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        _spawned.SetActive(false);
    }

    public void Activate()
    {
        if (_spawned != null)
            _spawned.SetActive(true);
    }
}
