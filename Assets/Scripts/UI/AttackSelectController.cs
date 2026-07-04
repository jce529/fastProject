using UnityEngine;
using UnityEngine.SceneManagement;

public class AttackSelectController : MonoBehaviour
{
    public void OnLinearClicked()
    {
        AttackTypeSelector.SetType(AttackType.Linear);
        SceneManager.LoadScene("SampleScene");
    }

    public void OnFanClicked()
    {
        AttackTypeSelector.SetType(AttackType.Fan);
        SceneManager.LoadScene("SampleScene");
    }
}
