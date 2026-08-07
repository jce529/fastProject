using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>UNLOCK-02/UNLOCK-03/D-12/D-13: 배열 기반 N-way 모듈 선택 화면. Inspector 배열
/// 3개(_moduleButtons/_lockIcons/_labels)는 CombatModuleRegistry.All과 인덱스 정렬되어야 한다
/// (AttackSelectUIBuilder.cs가 절차적으로 배치+배선한다).</summary>
public class AttackSelectController : MonoBehaviour
{
    [SerializeField] private Button[] _moduleButtons;
    [SerializeField] private Image[] _lockIcons;
    [SerializeField] private TMP_Text[] _labels;

    private void Start()
    {
        var entries = CombatModuleRegistry.All;
        for (int i = 0; i < entries.Length; i++)
        {
            int index = i; // 클로저 캡처
            bool unlocked = entries[i].IsUnlocked;

            if (_labels != null && i < _labels.Length && _labels[i] != null)
                _labels[i].text = entries[i].DisplayName;

            if (_moduleButtons != null && i < _moduleButtons.Length && _moduleButtons[i] != null)
            {
                _moduleButtons[i].interactable = unlocked; // D-13: 클릭 자체 차단
                _moduleButtons[i].onClick.RemoveAllListeners(); // 씬에 남아있을 수 있는 구 Persistent Listener 대체
                _moduleButtons[i].onClick.AddListener(() => OnModuleClicked(index));
            }

            if (_lockIcons != null && i < _lockIcons.Length && _lockIcons[i] != null)
                _lockIcons[i].enabled = !unlocked; // D-13: 자물쇠 아이콘
        }
    }

    private void OnModuleClicked(int index)
    {
        CombatModuleSelector.SetSelected(index);
        SceneManager.LoadScene("SampleScene");
    }
}
