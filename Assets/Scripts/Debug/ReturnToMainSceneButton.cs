using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 디버그 씬(DebugScene.unity) 전용 — 우측 하단 버튼 onClick에 배선되어 즉시 SampleScene(메인 씬)으로 전환한다.
/// </summary>
public class ReturnToMainSceneButton : MonoBehaviour
{
    private const string MainSceneName = "SampleScene";

    public void ReturnToMain()
    {
        SceneManager.LoadScene(MainSceneName);
    }
}
