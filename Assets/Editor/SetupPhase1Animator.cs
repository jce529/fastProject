using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Menu: Fast/Setup Phase 1 Animator
/// Creates Assets/Animations/PlayerPhase1.controller with 4 states:
/// Idle, Run, JumpRise, JumpFall — wired to DeadRevolver prototype clips.
/// All transitions have duration=0 (per ROADMAP Stack Constraint).
/// </summary>
public static class SetupPhase1Animator
{
    private const string ControllerPath = "Assets/Animations/PlayerPhase1.controller";

    private const string IdlePath     = "Assets/DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/Idle.anim";
    private const string RunPath      = "Assets/DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/Run.anim";
    private const string JumpRisePath = "Assets/DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/JumpRise.anim";
    private const string JumpFallPath = "Assets/DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/JumpFall.anim";

    [MenuItem("Fast/Setup Phase 1 Animator")]
    public static void Run()
    {
        // Load clips
        var idle     = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdlePath);
        var run      = AssetDatabase.LoadAssetAtPath<AnimationClip>(RunPath);
        var jumpRise = AssetDatabase.LoadAssetAtPath<AnimationClip>(JumpRisePath);
        var jumpFall = AssetDatabase.LoadAssetAtPath<AnimationClip>(JumpFallPath);

        if (idle == null || run == null || jumpRise == null || jumpFall == null)
        {
            Debug.LogError("[Phase1Animator] One or more animation clips not found. Check DeadRevolver paths.");
            return;
        }

        // Ensure output folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        // Delete existing controller if present
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // Parameters
        controller.AddParameter("IsMoving",   AnimatorControllerParameterType.Bool);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("VelocityY",  AnimatorControllerParameterType.Float);

        var root = controller.layers[0].stateMachine;

        // States
        var stIdle     = root.AddState("Idle");
        var stRun      = root.AddState("Run");
        var stJumpRise = root.AddState("JumpRise");
        var stJumpFall = root.AddState("JumpFall");

        stIdle.motion     = idle;
        stRun.motion      = run;
        stJumpRise.motion = jumpRise;
        stJumpFall.motion = jumpFall;

        root.defaultState = stIdle;

        // Transitions (duration = 0, exitTime disabled on all)
        // Idle → Run
        AddInstantTransition(stIdle, stRun,
            ("IsMoving",   AnimatorConditionMode.If,      0f),
            ("IsGrounded", AnimatorConditionMode.If,      0f));

        // Run → Idle
        AddInstantTransition(stRun, stIdle,
            ("IsMoving",   AnimatorConditionMode.IfNot,   0f),
            ("IsGrounded", AnimatorConditionMode.If,      0f));

        // Idle → JumpRise
        AddInstantTransition(stIdle, stJumpRise,
            ("IsGrounded", AnimatorConditionMode.IfNot,   0f),
            ("VelocityY",  AnimatorConditionMode.Greater, 0f));

        // Run → JumpRise
        AddInstantTransition(stRun, stJumpRise,
            ("IsGrounded", AnimatorConditionMode.IfNot,   0f),
            ("VelocityY",  AnimatorConditionMode.Greater, 0f));

        // JumpRise → JumpFall
        AddInstantTransition(stJumpRise, stJumpFall,
            ("VelocityY",  AnimatorConditionMode.Less,    0f));

        // JumpFall → Idle
        AddInstantTransition(stJumpFall, stIdle,
            ("IsGrounded", AnimatorConditionMode.If,      0f),
            ("IsMoving",   AnimatorConditionMode.IfNot,   0f));

        // JumpFall → Run
        AddInstantTransition(stJumpFall, stRun,
            ("IsGrounded", AnimatorConditionMode.If,      0f),
            ("IsMoving",   AnimatorConditionMode.If,      0f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[Phase1Animator] Created {ControllerPath}. Assign to Player Animator component.");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
    }

    private static void AddInstantTransition(
        AnimatorState from, AnimatorState to,
        params (string param, AnimatorConditionMode mode, float threshold)[] conditions)
    {
        var t = from.AddTransition(to);
        t.hasExitTime     = false;
        t.duration        = 0f;
        t.offset          = 0f;
        t.canTransitionToSelf = false;

        foreach (var (param, mode, threshold) in conditions)
            t.AddCondition(mode, threshold, param);
    }
}
