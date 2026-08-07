using UnityEngine;

/// <summary>
/// SAMURAI-02: CombatController.Update()의 Overclock 전용 hold-slowmo→release-resolve
/// 상태머신을 완전히 우회하는 additive marker 인터페이스. IPlayerCombatModule을 대체하지 않고
/// 나란히 구현된다 — IEnemy를 닫힌 계약으로 유지하고 ISpawnGatable을 별도로 얹은 프로젝트의
/// 기존 컨벤션과 동일하다(19-RESEARCH.md §1).
/// </summary>
public interface IRealtimeCombatModule
{
    /// <summary>CombatController.Update()가 Overclock 분기 대신 매 프레임 호출한다.
    /// 이 모듈은 자신의 입력 폴링/타이밍/판정을 전부 스스로 소유한다.</summary>
    void Tick(CombatContext ctx);
}
