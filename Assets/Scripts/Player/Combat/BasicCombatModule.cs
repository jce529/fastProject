/// <summary>
/// D-15/D-16: 기본전투모듈 — TapSwingCombatModuleBase의 스윙 로직을 그대로 사용, 패링 없음.
/// 튜토리얼 진입 시점부터 상시 해금(CombatModuleRegistry에서 requiredBossId=null로 등록됨,
/// 19-02 참고), 사무라이 전투형 모듈이 해금된 이후에도 계속 선택 가능하다(대체되지 않음).
/// </summary>
public class BasicCombatModule : TapSwingCombatModuleBase
{
}
