using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase12/Build Hit Spark Prefab
/// 기존 SwordGuardImpact.anim(스프라이트)을 재활용해 HitSparkEffect.prefab을 생성한다.
/// 신규 아트 없이 AutoDestroySelf로 자동 정리되는 히트 스파크 이펙트 프리팹(D-07).
/// </summary>
public static class HitSparkBuilder
{
    private const string PrefabPath = "Assets/Prefabs/Effects/HitSparkEffect.prefab";
    private const string ControllerPath = "Assets/Prefabs/Effects/HitSparkController.controller";
    private const string ClipPath = "Assets/DeadRevolver/PixelPrototypePlayerSprites/Art/Animations/SwordGuardImpact.anim";

    [MenuItem("Fast/Phase12/Build Hit Spark Prefab")]
    public static void Run()
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            Debug.LogError($"[HitSparkBuilder] SwordGuardImpact.anim을 찾을 수 없습니다: {ClipPath}");
            return;
        }

        string fullDir = Path.Combine(Application.dataPath, "Prefabs", "Effects");
        if (!Directory.Exists(fullDir))
            Directory.CreateDirectory(fullDir);
        AssetDatabase.Refresh();

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) != null)
            AssetDatabase.DeleteAsset(ControllerPath);

        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.layers[0].stateMachine.AddState("GuardImpact").motion = clip;

        var root = new GameObject("HitSparkEffect");
        root.AddComponent<SpriteRenderer>();
        var animator = root.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        root.AddComponent<AutoDestroySelf>();

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            AssetDatabase.DeleteAsset(PrefabPath);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[HitSparkBuilder] HitSparkEffect built at {PrefabPath}");
    }
}
