using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase12/Add Player Trail Renderer
/// 씬의 CombatController GameObject(Player)에 TrailRenderer를 신규 부착한다.
/// 프로젝트 전체에 TrailRenderer가 존재하지 않아 (2026-07-07 확인) "강화"가 아닌 신규 부착 + 강화 설정이다 (D-10).
/// 시각(색상/폭 커브)은 CombatController.Awake()의 ConfigureTrailVisuals()가 추가 설정한다.
/// </summary>
public static class PlayerTrailBuilder
{
    [MenuItem("Fast/Phase12/Add Player Trail Renderer")]
    public static void Run()
    {
        var combat = Object.FindFirstObjectByType<CombatController>(FindObjectsInactive.Include);
        if (combat == null)
        {
            Debug.LogError("[PlayerTrailBuilder] 씬에서 CombatController를 찾을 수 없습니다.");
            return;
        }

        var existing = combat.GetComponentInChildren<TrailRenderer>();
        if (existing != null)
        {
            Debug.Log("[PlayerTrailBuilder] TrailRenderer가 이미 존재 -- 건너뜁니다.");
            return;
        }

        var trail = combat.gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.25f;
        trail.emitting = false;
        trail.minVertexDistance = 0.05f;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
            trail.material = new Material(shader);

        EditorUtility.SetDirty(combat.gameObject);
        EditorSceneManager.MarkSceneDirty(combat.gameObject.scene);

        Debug.Log("[PlayerTrailBuilder] Player GameObject에 TrailRenderer 추가 완료.");
    }
}
