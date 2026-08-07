/// <summary>UNLOCK-02: 선택된 전투 모듈의 static 런타임 상태. AttackTypeSelector.Selected와
/// 동일한 static-선택 패턴. 기본값 index 0(기본전투모듈)은 항상 해금 상태이므로 안전한 폴백이다(D-16).</summary>
public static class CombatModuleSelector
{
    public static int SelectedIndex { get; private set; } = 0;
    public static CombatModuleId SelectedModuleId => CombatModuleRegistry.All[SelectedIndex].ModuleId;
    public static void SetSelected(int index) => SelectedIndex = index;
}
