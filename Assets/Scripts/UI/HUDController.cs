using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _floorLabel;
    [SerializeField] private Image           _gaugeFill;
    [SerializeField] private TextMeshProUGUI _attackTypeLabel;
    [SerializeField] private GaugeController _gauge;

    private AttackType _lastType = (AttackType)(-1);

    private void Update()
    {
        _floorLabel.SetText("Floor {0}", FloorManager.CurrentFloor);
        _gaugeFill.fillAmount = _gauge.Value;

        AttackType t = AttackTypeSelector.Selected;
        if (t != _lastType)
        {
            _lastType = t;
            _attackTypeLabel.SetText(t == AttackType.Linear ? "LINEAR" : "FAN");
        }
    }
}
