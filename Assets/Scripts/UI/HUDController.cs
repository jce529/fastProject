using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _floorLabel;
    [SerializeField] private TextMeshProUGUI _scoreLabel;
    [SerializeField] private Image           _gaugeFill;
    [SerializeField] private TextMeshProUGUI _attackTypeLabel;
    [SerializeField] private ChronoGaugeController _gauge;
    [SerializeField] private TextMeshProUGUI _timerLabel;

    private AttackType _lastType = (AttackType)(-1);

    private void Start()
    {
        if (_gauge == null)
            _gauge = FindFirstObjectByType<ChronoGaugeController>();
    }

    private void Update()
    {
        _floorLabel.SetText("Floor {0}", FloorManager.CurrentFloor);
        _timerLabel?.SetText("{0}", Mathf.CeilToInt(FloorTimer.RemainingSeconds)); // TIMER-01
        _scoreLabel?.SetText("{0}", ScoreManager.Score); // SCORE-02: 기존 코드 — 변경 없음
        if (_gauge != null)
            _gaugeFill.fillAmount = _gauge.Value;

        AttackType t = AttackTypeSelector.Selected;
        if (t != _lastType)
        {
            _lastType = t;
            _attackTypeLabel.SetText(t == AttackType.Linear ? "LINEAR" : "FAN");
        }
    }
}
