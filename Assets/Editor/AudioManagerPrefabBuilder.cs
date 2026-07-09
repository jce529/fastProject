using UnityEditor;
using UnityEngine;

/// <summary>
/// Resources/AudioManager.prefab을 생성하고 선별된 클립 4개(D-03/D-05/D-06)를 연결한다.
/// 실행: Tools > Audio > Build AudioManager Prefab. 재실행 시 기존 프리팹을 덮어쓴다(클립 교체 루프 지원).
/// </summary>
public static class AudioManagerPrefabBuilder
{
    // [선별] D-06 진입 = 상승 워프음 — Digital Audio phaserUp 계열, 상승 텔레포트음에 가장 부합 (≈0.5-0.8s)
    private const string PortalEnterPath = "Assets/Audio/Kenney_DigitalAudio/phaserUp1.ogg";
    // [선별] D-06 퇴장 = 하강 마무리음 — Digital Audio phaserDown 계열, phaserUp과 짝을 이루는 하강 톤
    private const string PortalExitPath = "Assets/Audio/Kenney_DigitalAudio/phaserDown1.ogg";
    // [선별] D-05 대시 처치 = 날카로운 슬래시 — Sci-Fi Sounds laserSmall, 즉각적이고 짧은(≤0.3s) 어택음
    private const string SlashPath = "Assets/Audio/Kenney_SciFiSounds/laserSmall_000.ogg";
    // [선별] D-05 사망 = 글리치/디지털 노이즈 — Digital Audio spaceTrash, 지글거리는 디지털 붕괴 질감
    private const string GlitchPath = "Assets/Audio/Kenney_DigitalAudio/spaceTrash1.ogg";

    private const string PrefabPath = "Assets/Resources/AudioManager.prefab";

    [MenuItem("Tools/Audio/Build AudioManager Prefab")]
    public static void Build()
    {
        var go = new GameObject("AudioManager");
        var mgr = go.AddComponent<AudioManager>();

        var so = new SerializedObject(mgr);
        AssignClip(so, "_portalEnter", PortalEnterPath);
        AssignClip(so, "_portalExit", PortalExitPath);
        AssignClip(so, "_slash", SlashPath);
        AssignClip(so, "_enemyDeathGlitch", GlitchPath);
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
        Debug.Log($"[AudioManagerPrefabBuilder] {PrefabPath} 생성 완료 — 클립 4개 연결");
    }

    private static void AssignClip(SerializedObject so, string field, string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogError($"[AudioManagerPrefabBuilder] 클립 없음: {path}");
        so.FindProperty(field).objectReferenceValue = clip;
    }
}
