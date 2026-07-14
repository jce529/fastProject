using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지정한 적이 모두 죽으면 targetObject를 활성화한다.
/// 씬 설정:
///   - enemies: 감시할 적 GameObject를 에디터에서 드래그 (IEnemy 구현체여야 함)
///   - targetObject: 전멸 시 나타낼 오브젝트 (문, 사다리, 출구 트리거 등)
/// targetObject는 게임 시작 시 자동으로 비활성화된다.
/// D-02/RoomRespawnGate(999.2): IsCleared로 "완전 클리어" 여부를 외부에 노출하고,
/// ResetForRespawn()으로 리스폰 직후 재클리어 판정을 재활성화한다.
/// </summary>
public class RoomClearCondition : MonoBehaviour
{
    [Header("감시할 적")]
    public GameObject[] enemies;

    [Header("전멸 시 활성화할 오브젝트")]
    public GameObject targetObject;

    private IEnemy[] _enemyCache;
    private bool _activated;

    /// <summary>D-02: RoomRespawnGate가 "완전히 클리어됐는지"를 확인하는 데 사용하는 읽기 전용 노출.</summary>
    public bool IsCleared => _activated;

    private void Start()
    {
        if (targetObject != null)
            targetObject.SetActive(false);

        DiscoverEnemies();

        // 적이 없으면 즉시 활성화
        if (_enemyCache == null || _enemyCache.Length == 0)
            Activate();
    }

    private void Update()
    {
        if (_activated || _enemyCache == null || _enemyCache.Length == 0) return;

        foreach (var enemy in _enemyCache)
        {
            if (enemy != null && enemy.IsAlive) return;
        }

        Activate();
    }

    private void Activate()
    {
        _activated = true;
        if (targetObject != null)
            targetObject.SetActive(true);
    }

    /// <summary>Start()의 적 탐색 로직을 분리 — ResetForRespawn()에서도 동일 로직을 재사용한다 (999.2 신규).</summary>
    private void DiscoverEnemies()
    {
        if (enemies != null && enemies.Length > 0)
        {
            // 정적 캐싱 (에디터에서 직접 연결한 경우)
            _enemyCache = new IEnemy[enemies.Length];
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null)
                    _enemyCache[i] = enemies[i].GetComponent<IEnemy>();
            }
        }
        else
        {
            // 동적 탐색 (WorldGenerator/RoomRespawnGate가 스폰한 적 — includeInactive:true 필수)
            var found = new List<IEnemy>();
            foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is IEnemy e)
                    found.Add(e);
            }
            _enemyCache = found.ToArray();
        }
    }

    /// <summary>
    /// D-02/999.2-RESEARCH.md Pitfall/Pattern 3: RoomRespawnGate가 리스폰 스폰(EnemySpawner.Spawn()
    /// 전부 완료) 직후 호출한다. _activated 영구 래치를 재무장하고, 새로 생성된(비활성) 적 자식들을
    /// 다시 스캔해 재클리어 판정이 다시 동작하도록 한다 — 호출하지 않으면 리스폰 이후 재클리어가
    /// 영원히 감지되지 않는다.
    /// </summary>
    public void ResetForRespawn()
    {
        _activated = false;
        if (targetObject != null)
            targetObject.SetActive(false);
        DiscoverEnemies();
    }
}
