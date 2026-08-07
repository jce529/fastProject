/// <summary>D-12: 모듈 선택 UI가 순회하는 배열 기반 레지스트리. 향후 DeadEye(Phase 20)/
/// MAX(Phase 22)/NOVA(Phase 23)는 이 배열에 엔트리 한 줄만 추가하면 된다(하드코딩 나열 금지).
/// 실제 IPlayerCombatModule 구현 클래스를 전혀 참조하지 않는다 — CombatModuleId enum + 문자열
/// requiredBossId만으로 UI 레이어가 전투 모듈 레이어와 완전히 decoupled된다.</summary>
public enum CombatModuleId { Basic, Overclock, Samurai }

public readonly struct CombatModuleEntry
{
    public readonly string DisplayName;
    public readonly CombatModuleId ModuleId;
    public readonly string RequiredBossId; // null/"" = 상시 해금

    public CombatModuleEntry(string displayName, CombatModuleId moduleId, string requiredBossId)
    {
        DisplayName = displayName;
        ModuleId = moduleId;
        RequiredBossId = requiredBossId;
    }

    public bool IsUnlocked => string.IsNullOrEmpty(RequiredBossId) || BossUnlockManager.IsUnlocked(RequiredBossId);
}

public static class CombatModuleRegistry
{
    public static readonly CombatModuleEntry[] All =
    {
        new CombatModuleEntry("기본전투모듈", CombatModuleId.Basic, requiredBossId: null),           // D-16: 상시 해금
        new CombatModuleEntry("Overclock", CombatModuleId.Overclock, requiredBossId: "Fiora"),         // D-17: 기존 게이팅 그대로 유지 (BossUnlockManager.IsUnlocked("Fiora"))
        new CombatModuleEntry("사무라이 전투형 모듈", CombatModuleId.Samurai, requiredBossId: "Samurai"), // SAMURAI-01
    };
}
