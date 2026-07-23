using UnityEngine;
using UnityEngine.SceneManagement;

// 어느 씬에서 Play해도 항상 MainMenu로 부트스트랩 (DebugScene은 예외 — 디버그 씬은 그 자체로 격리 실행)
public static class GameBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureMainMenu()
    {
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName != "MainMenu" && activeSceneName != "DebugScene")
            SceneManager.LoadScene("MainMenu");
    }
}
