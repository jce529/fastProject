using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DeathScreenController : MonoBehaviour
{
    [SerializeField] private GameObject _deathPanel;
    [SerializeField] private Button     _restartButton;

    private void OnEnable()
    {
        PlayerController.OnPlayerDeath += HandleDeath;
        _restartButton.onClick.AddListener(RestartGame);
    }

    private void OnDisable()
    {
        PlayerController.OnPlayerDeath -= HandleDeath;
        _restartButton.onClick.RemoveListener(RestartGame);
    }

    private void HandleDeath()
    {
        _deathPanel.SetActive(true);
        Time.timeScale      = 0f;
        Time.fixedDeltaTime = 0f;
    }

    private void RestartGame()
    {
        Time.timeScale         = 1f;
        Time.fixedDeltaTime    = 0.02f;
        FloorManager.CurrentFloor = 1;
        SceneManager.LoadScene("AttackSelect");
    }
}
