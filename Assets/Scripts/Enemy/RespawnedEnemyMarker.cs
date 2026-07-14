using UnityEngine;

/// <summary>
/// D-10(999.2): 리스폰된 적 인스턴스에만 부착되는 빈 마커 컴포넌트 — IEnemy(3-member 계약, 잠금됨)를
/// 확장하지 않고 "이 적은 리스폰된 개체다"를 태깅하기 위한 additive 컴포넌트. ISpawnGatable(Phase 14)이
/// 같은 문제(IEnemy 미확장)를 해결한 방식과 동일한 프로젝트 컨벤션을 따른다.
/// WorldGenerator가 리스폰 경로에서만 AddComponent하고, CombatController.ExecuteDash()가
/// GetComponent&lt;RespawnedEnemyMarker&gt;()로 감소 점수 여부를 판정한다. 적 GameObject가 죽으면
/// (EnemyDeathEffect.PlayDeathSequence()의 Destroy(gameObject)) 이 마커도 함께 사라진다 — 별도 정리 불필요.
/// </summary>
public class RespawnedEnemyMarker : MonoBehaviour
{
}
