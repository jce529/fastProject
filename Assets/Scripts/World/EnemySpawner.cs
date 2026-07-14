using UnityEngine;

/// <summary>
/// Room/Corridor 프리팹의 각 스폰 포인트에 부착하는 마커 컴포넌트.
/// D-01/D-02: Spawn()은 사전 생성(비활성 인스턴스) 전용 — 화면 밖 미리 생성 시점에 호출된다.
/// Activate()는 플레이어가 실제로 이 구간에 도달했을 때만 호출되며, 스폰 VFX 트리거의 유일한
/// 지점이다(STATE.md 제약 — Awake/OnEnable에서 트리거 금지). HasActivated로 1회성 스폰을 보장한다.
/// 룸이 Destroy()될 때 자식인 적도 함께 제거된다.
/// D-01/D-09(999.2): ResetForRespawn()으로 HasActivated 래치를 재무장하고 _spawned 댕글링 참조를
/// 명시적으로 비운다 — 999.2-RESEARCH.md Pitfall 1(재무장만 하고 _spawned를 안 비우면 Activate()가
/// 영구 no-op됨) 방지.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    public enum EnemyType { Melee, Ranged }

    [SerializeField] private EnemyType _type = EnemyType.Melee;
    public EnemyType Type => _type;

    private GameObject _spawned;

    /// <summary>D-02: 이미 활성화된 마커는 재호출해도 안전한 no-op — 1회성 스폰.</summary>
    public bool HasActivated { get; private set; }

    /// <summary>D-01(999.2): 리스폰 직후 새로 Instantiate된 인스턴스를 상위 호출자(WorldGenerator)가
    /// 필요 시 태깅(RespawnedEnemyMarker)할 수 있도록 반환한다. 기존 호출자(DebugRoomTeleporter,
    /// WorldGenerator.TrySpawnEnemies)는 반환값을 무시해도 그대로 컴파일된다.</summary>
    public GameObject Spawn(GameObject meleePrefab, GameObject rangedPrefab)
    {
        GameObject prefab = _type == EnemyType.Melee ? meleePrefab : rangedPrefab;
        if (prefab == null) return null;

        _spawned = Instantiate(prefab, transform.position, Quaternion.identity, transform);
        _spawned.SetActive(false);
        return _spawned;
    }

    /// <summary>
    /// D-01: 플레이어가 실제로 이 구간에 진입했을 때만 호출한다. portalEffectPrefab이 null이면
    /// 포탈 VFX 없이 걸어나오기+게이팅만 재생한다(DebugRoomTeleporter 등 호환).
    /// SPWN-02: SetActive(true) 이전에 스폰 게이트를 닫아, 같은 프레임 다른 스크립트의 Update()가
    /// 이 적을 감지/타겟팅하지 못하도록 한다 (Pitfall 1).
    /// </summary>
    public void Activate(GameObject portalEffectPrefab = null)
    {
        if (HasActivated || _spawned == null) return;
        HasActivated = true;

        var gate = _spawned.GetComponent<ISpawnGatable>();
        gate?.SetSpawnGate(true);

        _spawned.SetActive(true);

        var effect = _spawned.GetComponent<EnemySpawnEffect>();
        if (effect == null) effect = _spawned.AddComponent<EnemySpawnEffect>();
        effect.StartCoroutine(effect.PlaySpawnSequence(portalEffectPrefab, gate));
    }

    /// <summary>
    /// D-01/D-09(999.2): RoomRespawnGate/WorldGenerator가 재진입 리스폰을 트리거할 때, Spawn()을
    /// 다시 호출하기 직전에 호출한다. HasActivated만 리셋하고 _spawned를 그대로 두면 이전에
    /// Destroy()된 적 인스턴스를 가리키는 댕글링 참조가 남아 Activate()의 `_spawned == null` 가드가
    /// 예측 불가능하게 동작한다(999.2-RESEARCH.md Pitfall 1) — 반드시 _spawned도 함께 null로 비운다.
    /// </summary>
    public void ResetForRespawn()
    {
        HasActivated = false;
        _spawned = null;
    }
}
