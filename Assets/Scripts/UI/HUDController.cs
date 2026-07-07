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

    [Header("Timer Flicker (D-05)")]
    [SerializeField] private float _flickerThreshold  = 20f;  // 이 값(초) 이하로 남았을 때만 점멸 시작
    [SerializeField] private float _minFlickerInterval = 0.08f; // 0초에 가까울 때 (가장 빠름)
    [SerializeField] private float _maxFlickerInterval = 0.4f;  // threshold 진입 시점 (가장 느림)

    private bool _flickerRed;

    private AttackType _lastType = (AttackType)(-1);

    private void Start()
    {
        if (_gauge == null)
            _gauge = FindFirstObjectByType<ChronoGaugeController>();

        StartCoroutine(TimerFlickerLoop());
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

    /// <summary>
    /// D-05: 남은 시간이 _flickerThreshold 이하로 줄어들면 _timerLabel 색상을 흰색↔빨간색으로 토글한다.
    /// 토글 간격은 남은 시간에 비례(초반엔 느리게, 0에 가까워질수록 빠르게) — InvincibilityHandler의
    /// 코루틴 + WaitForSecondsRealtime 패턴을 참고하되, 간격을 매 사이클 재계산하는 가변 버전이다.
    /// </summary>
    private IEnumerator TimerFlickerLoop()
    {
        while (true)
        {
            if (_timerLabel == null)
            {
                yield return new WaitForSecondsRealtime(0.2f);
                continue;
            }

            float remaining = FloorTimer.RemainingSeconds;

            if (remaining > _flickerThreshold)
            {
                _timerLabel.color = Color.white;
                yield return new WaitForSecondsRealtime(0.2f);
                continue;
            }

            _flickerRed = !_flickerRed;
            _timerLabel.color = _flickerRed ? Color.red : Color.white;

            float interval = Mathf.Lerp(_minFlickerInterval, _maxFlickerInterval, Mathf.Clamp01(remaining / _flickerThreshold));
            yield return new WaitForSecondsRealtime(interval);
        }
    }
}
