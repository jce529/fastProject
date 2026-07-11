using UnityEngine;

/// <summary>
/// Attach to FallZone_Left and FallZone_Right trigger colliders.
/// Player가 진입하면 FallDetector.OnFall()을 호출한다 (기존 동작, 변경 없음).
/// Enemy(MeleeEnemy/RangedEnemy 공통 "Enemy" 태그)가 진입하면 D-02/D-03: 사망 이펙트 없이
/// 즉시 Destroy — 화면 밖에서 벌어지는 낙사이므로 연출은 불필요, 좀비 상태 방지가 목적이다.
/// </summary>
public class FallZoneTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var fallDetector = other.GetComponent<FallDetector>();
            if (fallDetector != null) fallDetector.OnFall();
            return;
        }

        if (other.CompareTag("Enemy"))
        {
            // D-02/D-03: MeleeEnemy/RangedEnemy 공통 — VFX/사운드 없이 즉시 제거.
            // OnDisable()이 자동 호출되어 PlayerController.OnPlayerDeath 구독 해제, 코루틴도 자동 정지됨.
            Destroy(other.gameObject);
        }
    }
}
