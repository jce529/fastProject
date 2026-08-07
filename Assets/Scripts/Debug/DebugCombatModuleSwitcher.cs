using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>D-18: 실제 튜토리얼→로비 플로우 배선은 이번 Phase 범위 밖 — 대신 DebugScene에서
/// 숫자키로 즉시 모듈을 바꿔 SAMURAI-02/03/04를 기본전투모듈/사무라이 전투형 모듈/Overclock
/// 각각에 대해 반복 검증할 수 있게 하는 테스트 전용 컴포넌트.</summary>
public class DebugCombatModuleSwitcher : MonoBehaviour
{
    [SerializeField] private CombatController _combatController;

    private void Update()
    {
        if (Keyboard.current == null || _combatController == null) return;
        if (Keyboard.current.digit1Key.wasPressedThisFrame) _combatController.DebugSetActiveModule(CombatModuleId.Basic);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) _combatController.DebugSetActiveModule(CombatModuleId.Overclock);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) _combatController.DebugSetActiveModule(CombatModuleId.Samurai);
    }
}
