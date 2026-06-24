using UnityEngine;
using UnityEngine.SceneManagement;

// 어느 씬에서 Play해도 항상 MainMenu로 부트스트랩
public static class GameBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureMainMenu()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
            SceneManager.LoadScene("MainMenu");
    }
}
