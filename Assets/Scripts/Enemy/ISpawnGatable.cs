/// <summary>
/// SPWN-02: IEnemy의 3-member 계약(IsAlive, OnDashHit, ClearHighlight)과는 별도로 존재하는
/// additive 인터페이스 — IEnemy 자체는 변경하지 않는다 (Phase 15/16 BossEnemy 통합 전제 유지).
/// EnemySpawnEffect가 적 타입(Melee/Ranged/향후 Boss)에 캐스팅 없이 스폰 게이트를 여닫을 수 있게 한다.
/// </summary>
public interface ISpawnGatable
{
    /// <summary>true: 스폰 VFX 재생 중 — 구현체는 내부적으로 IsAlive를 false로 강제해야 한다.
    /// false: 스폰 완료 — IsAlive를 true로 복원해 정상 FSM/타겟팅을 재개한다.</summary>
    void SetSpawnGate(bool isSpawning);
}
