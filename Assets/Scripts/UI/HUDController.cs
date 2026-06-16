using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _floorLabel;
    [SerializeField] private Image           _gaugeFill;
    [SerializeField] private TextMeshProUGUI _attackTypeLabel;
    [SerializeField] private ChronoGaugeController _gauge;

    private AttackType _lastType = (AttackType)(-1);

    private void Start()
    {
        if (_gauge == null)
            _gauge = FindFirstObjectByType<ChronoGaugeController>();

        // [DIAG] 임시 진단 로그 — 원인 파악 후 제거
        Debug.Log($"[HUD] Start: _gauge={(_gauge != null ? "OK" : "NULL")}, _gaugeFill={(_gaugeFill != null ? "OK" : "NULL")}, _floorLabel={(_floorLabel != null ? "OK" : "NULL")}, _attackTypeLabel={(_attackTypeLabel != null ? "OK" : "NULL")}");
    }

    private void Update()
    {
        _floorLabel.SetText("Floor {0}", FloorManager.CurrentFloor);
        if (_gauge != null)
        {
            _gaugeFill.fillAmount = _gauge.Value;
            // [DIAG] 임시 진단 로그 — 0.5초(30프레임)마다 한 번
            if (Time.frameCount % 30 == 0)
                Debug.Log($"[HUD] Update: _gauge.Value={_gauge.Value:F3}, fillAmount={_gaugeFill.fillAmount:F3}");
        }

        AttackType t = AttackTypeSelector.Selected;
        if (t != _lastType)
        {
            _lastType = t;
            _attackTypeLabel.SetText(t == AttackType.Linear ? "LINEAR" : "FAN");
        }
    }
}
