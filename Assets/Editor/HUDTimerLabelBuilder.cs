using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Fast/Phase11/Add Timer Label To HUD
/// 현재 열려 있는 씬의 HUDController를 찾아 기존 _scoreLabel을 템플릿으로 TimerLabel
/// TextMeshProUGUI를 같은 부모 아래 복제 생성하고, HUDController._timerLabel 필드에
/// SerializedObject로 자동 연결한다 (TIMER-01).
/// </summary>
public static class HUDTimerLabelBuilder
{
    [MenuItem("Fast/Phase11/Add Timer Label To HUD")]
    public static void Run()
    {
        var hud = Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
        if (hud == null)
        {
            Debug.LogError("[HUDTimerLabelBuilder] 씬에서 HUDController를 찾을 수 없습니다.");
            return;
        }

        var so = new SerializedObject(hud);
        var scoreLabelProp = so.FindProperty("_scoreLabel");
        var timerLabelProp = so.FindProperty("_timerLabel");

        if (timerLabelProp == null)
        {
            Debug.LogError("[HUDTimerLabelBuilder] HUDController에 _timerLabel 필드가 없습니다. Plan 11-03 완료 후 실행하세요.");
            return;
        }

        var scoreLabel = scoreLabelProp.objectReferenceValue as TextMeshProUGUI;
        if (scoreLabel == null)
        {
            Debug.LogError("[HUDTimerLabelBuilder] _scoreLabel이 연결되어 있지 않습니다. 먼저 ScoreLabel을 연결하세요.");
            return;
        }

        if (timerLabelProp.objectReferenceValue != null)
        {
            Debug.Log("[HUDTimerLabelBuilder] _timerLabel이 이미 연결되어 있습니다 — 건너뜁니다.");
            return;
        }

        var timerGO = Object.Instantiate(scoreLabel.gameObject, scoreLabel.transform.parent);
        timerGO.name = "TimerLabel";

        var timerRect = timerGO.GetComponent<RectTransform>();
        var scoreRect  = scoreLabel.GetComponent<RectTransform>();
        timerRect.anchoredPosition = scoreRect.anchoredPosition + new Vector2(0f, -50f);

        var timerTmp = timerGO.GetComponent<TextMeshProUGUI>();
        timerTmp.SetText("{0}", (int)FloorTimer.Duration);
        timerTmp.color = Color.white;

        timerLabelProp.objectReferenceValue = timerTmp;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);

        Debug.Log("[HUDTimerLabelBuilder] TimerLabel 생성 및 HUDController._timerLabel 연결 완료.");
    }
}
