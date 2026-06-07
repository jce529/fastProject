// ============================================================
// DEBUG ONLY — Remove before release build
// Attaches AttackTypeDebugOverlay to the Player GameObject
// once on editor load, then deletes itself.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AttachDebugOverlayOnce
{
    static AttachDebugOverlayOnce()
    {
        EditorApplication.delayCall += Run;
    }

    private static void Run()
    {
        // Find Player in the active scene
        GameObject player = GameObject.Find("Player");
        GameObject target = player;

        if (target == null)
        {
            // Fallback: create DebugManager
            target = new GameObject("DebugManager");
            Debug.LogWarning("[AttachDebugOverlayOnce] 'Player' not found — created 'DebugManager' instead.");
        }

        // Add component only if not already present
        if (target.GetComponent<AttackTypeDebugOverlay>() == null)
        {
            target.AddComponent<AttackTypeDebugOverlay>();
            Debug.Log($"[AttachDebugOverlayOnce] AttackTypeDebugOverlay attached to '{target.name}'.");

            // Save the scene
            Scene scene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(scene.path))
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[AttachDebugOverlayOnce] Scene saved.");
            }
        }
        else
        {
            Debug.Log("[AttachDebugOverlayOnce] Component already present — skipping.");
        }

        // Self-delete this editor script file
        string scriptPath = "Assets/Editor/AttachDebugOverlayOnce.cs";
        if (System.IO.File.Exists(System.IO.Path.GetFullPath(scriptPath)))
        {
            AssetDatabase.DeleteAsset(scriptPath);
            Debug.Log("[AttachDebugOverlayOnce] Self-deleted editor script.");
        }
    }
}
#endif
