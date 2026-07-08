using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase12/Patch Player Animator (Whiff+Roll Triggers)
///
/// FastPlayerAnimator.controller is missing the parameters/states that
/// CombatController.ExecuteWhiff() and RollController.RollCoroutine() already
/// call via SetTrigger:
///   - D-05: "Whiff" trigger + Whiff state (no such trigger exists yet).
///     Reuses AirSlash.anim as the Whiff motion (fits the "헛베기" theme,
///     no new art needed).
///   - D-06: "Roll" trigger + AnyState->Roll transition (only the IsRolling
///     bool exists today; the trigger-driven path coexists alongside it).
///
/// No C# gameplay code changes are needed -- CombatController.cs and
/// RollController.cs already call SetTrigger correctly; only the controller
/// asset was missing the corresponding parameters/states/transitions.
///
/// Idempotent: re-running the menu does not create duplicate
/// parameters/states/transitions.
/// </summary>
public static class PlayerAnimatorPatcher
{
    private const string ControllerPath = "Assets/Player/Resource/Animation/FastPlayerAnimator.controller";
    private const string WhiffClipPath = "Assets/DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/AirSlash.anim";

    [MenuItem("Fast/Phase12/Patch Player Animator (Whiff+Roll Triggers)")]
    public static void Run()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
        {
            Debug.LogError($"[PlayerAnimatorPatcher] Controller not found at {ControllerPath}");
            return;
        }

        var sm = controller.layers[0].stateMachine;

        // --- Whiff (D-05) ---
        if (!HasParameter(controller, "Whiff"))
            controller.AddParameter("Whiff", AnimatorControllerParameterType.Trigger);

        var whiffState = FindState(sm, "Whiff");
        if (whiffState == null)
        {
            var whiffClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(WhiffClipPath);
            if (whiffClip == null)
            {
                Debug.LogError($"[PlayerAnimatorPatcher] Whiff clip not found at {WhiffClipPath}");
                return;
            }

            whiffState = sm.AddState("Whiff");
            whiffState.motion = whiffClip;

            var anyToWhiff = sm.AddAnyStateTransition(whiffState);
            anyToWhiff.AddCondition(AnimatorConditionMode.If, 0, "Whiff");
            anyToWhiff.duration = 0f;
            anyToWhiff.hasExitTime = false;
            anyToWhiff.canTransitionToSelf = false;

            var idleState = FindState(sm, "Idle");
            var whiffToIdle = whiffState.AddTransition(idleState);
            whiffToIdle.duration = 0f;
            whiffToIdle.hasExitTime = true;
            whiffToIdle.exitTime = 0.9f;
        }

        // --- Roll (D-06) ---
        if (!HasParameter(controller, "Roll"))
            controller.AddParameter("Roll", AnimatorControllerParameterType.Trigger);

        var rollState = FindState(sm, "Roll");
        if (rollState != null && !HasAnyStateTransitionTo(sm, rollState, "Roll"))
        {
            var anyToRoll = sm.AddAnyStateTransition(rollState);
            anyToRoll.AddCondition(AnimatorConditionMode.If, 0, "Roll");
            anyToRoll.duration = 0f;
            anyToRoll.hasExitTime = false;
            anyToRoll.canTransitionToSelf = false;
        }
        // Existing IsRolling bool-driven transitions into Roll (Idle/Walk/Sprint -> Roll)
        // are left untouched -- the new Roll trigger via AnyState coexists alongside them.

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[PlayerAnimatorPatcher] Whiff/Roll 트리거+상태 패치 완료.");
    }

    private static bool HasParameter(AnimatorController controller, string name)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name)
                return true;
        }
        return false;
    }

    private static AnimatorState FindState(AnimatorStateMachine sm, string name)
    {
        foreach (var child in sm.states)
        {
            if (child.state.name == name)
                return child.state;
        }
        return null;
    }

    private static bool HasAnyStateTransitionTo(AnimatorStateMachine sm, AnimatorState target, string paramName)
    {
        foreach (var t in sm.anyStateTransitions)
        {
            if (t.destinationState != target)
                continue;

            foreach (var condition in t.conditions)
            {
                if (condition.parameter == paramName)
                    return true;
            }
        }
        return false;
    }
}
