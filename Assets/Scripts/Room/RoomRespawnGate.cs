using UnityEngine;

/// <summary>
/// D-01/D-01a/D-02/D-03/D-04/D-09(999.2): Room 재진입 리스폰 게이트. Case A(체인 안에서 룸 인스턴스가
/// Destroy되지 않고 살아있는 동안의 재진입) 전용 — GameObject 자체가 이 상태를 들고 있으므로,
/// WorldGenerator.RemoveTail()/RemoveHead()로 Destroy()되면 이 컴포넌트와 _pendingRespawn 상태도
/// 함께 사라져, Case B(완전 체인 이탈 후 재생성)는 구조적으로 respawn을 트리거하지 않는다(D-01a).
/// WorldGenerator.UpdatePlayerIndex()가 계산하는 체인-인덱스 전이 신호(999.2-RESEARCH.md Pitfall 2)를
/// 트리거로 사용한다 — 별도의 카메라 경계 트리거 콜라이더를 새로 만들지 않는다.
/// D-04: Corridor/보스 룸 프리팹에는 이 컴포넌트를 부착하지 않는다(999.2-04에서 Complex_Room 6종에만
/// 부착) — WorldGenerator가 GetComponent로 조회하므로 미부착 오브젝트에서는 자연히 no-op된다.
/// </summary>
[RequireComponent(typeof(RoomClearCondition))]
public class RoomRespawnGate : MonoBehaviour
{
    private bool _pendingRespawn;

    public RoomClearCondition ClearCondition { get; private set; }

    /// <summary>true면 "클리어된 상태에서 나갔다가 아직 재진입하지 않음" — WorldGenerator가 이 값을 보고
    /// 실제 리스폰 실행 여부를 결정한다.</summary>
    public bool IsPendingRespawn => _pendingRespawn;

    private void Awake()
    {
        ClearCondition = GetComponent<RoomClearCondition>();
    }

    /// <summary>D-01: 플레이어가 이 room 노드를 떠날 때 WorldGenerator가 호출한다.
    /// D-02: 클리어된 상태에서 떠났을 때만 리스폰을 대기시킨다 — 클리어 전 이탈은 기존 1회성 동작 유지.</summary>
    public void MarkLeft()
    {
        if (ClearCondition.IsCleared)
            _pendingRespawn = true;
    }

    /// <summary>D-03: WorldGenerator가 실제 리스폰을 실행하기 직전 호출해 게이트를 소비한다 — 별도
    /// 쿨다운 타이머 없이 "나갔다가 다시 들어옴" 자체가 게이트 역할을 한다. 다음 클리어+이탈 사이클에서
    /// MarkLeft()가 다시 이 값을 true로 세팅할 수 있으므로 D-09(무제한 리스폰)를 그대로 지원한다.</summary>
    public void ConsumeRespawn()
    {
        _pendingRespawn = false;
    }
}
