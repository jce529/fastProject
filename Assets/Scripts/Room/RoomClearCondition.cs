using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 지정한 적이 모두 죽으면 targetObject를 활성화한다.
/// 씬 설정:
///   - enemies: 감시할 적 GameObject를 에디터에서 드래그 (IEnemy 구현체여야 함)
///   - targetObject: 전멸 시 나타낼 오브젝트 (문, 사다리, 출구 트리거 등)
/// targetObject는 게임 시작 시 자동으로 비활성화된다.
/// </summary>
public class RoomClearCondition : MonoBehaviour
{
    [Header("감시할 적")]
    public GameObject[] enemies;

    [Header("전멸 시 활성화할 오브젝트")]
    public GameObject targetObject;

    private IEnemy[] _enemyCache;
    private bool _activated;

    private void Start()
    {
        if (targetObject != null)
            targetObject.SetActive(false);

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
            // 동적 탐색 (FloorSpawner 스폰 적 — includeInactive:true 필수)
            var found = new List<IEnemy>();
            foreach (MonoBehaviour mb in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb is IEnemy e)
                    found.Add(e);
            }
            _enemyCache = found.ToArray();
        }

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
}
