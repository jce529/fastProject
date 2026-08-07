using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu: Fast/Phase19/Build AttackSelect UI
/// D-12/D-13: AttackSelect.unity의 기존 Linear/Fan 2버튼을 CombatModuleRegistry.All 기반
/// 3버튼(모듈 선택)으로 재구성한다. 각 버튼에 라벨 + 잠금 아이콘을 배치하고
/// AttackSelectController의 _moduleButtons/_lockIcons/_labels 배열에 배선한다.
/// 재실행 시 멱등적 — "ModuleButton_" 이름 규칙으로 기존 생성물을 먼저 제거한다.
/// </summary>
public static class AttackSelectUIBuilder
{
    private const string ScenePath = "Assets/Scenes/AttackSelect.unity";

    [MenuItem("Fast/Phase19/Build AttackSelect UI")]
    public static void Build()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var panelGO = GameObject.Find("Panel");
        Transform parent = panelGO != null ? panelGO.transform : GameObject.Find("Canvas")?.transform;
        if (parent == null)
        {
            Debug.LogError("[AttackSelectUIBuilder] Panel/Canvas GameObject를 찾지 못했습니다.");
            return;
        }

        // 기존 Linear/Fan 버튼 제거 — D-12: 이 화면은 이제 Linear/Fan이 아니라 모듈을 선택한다.
        var linear = GameObject.Find("Linear");
        if (linear != null) Object.DestroyImmediate(linear);
        var fan = GameObject.Find("Fan");
        if (fan != null) Object.DestroyImmediate(fan);

        // 멱등성 — 이 도구가 이전에 생성한 버튼이 있으면 먼저 제거 후 재생성
        for (int i = 0; i < 3; i++)
        {
            var existing = GameObject.Find($"ModuleButton_{i}");
            if (existing != null) Object.DestroyImmediate(existing);
        }

        var entries = CombatModuleRegistry.All;
        var buttons = new Button[entries.Length];
        var lockIcons = new Image[entries.Length];
        var labels = new TMP_Text[entries.Length];

        for (int i = 0; i < entries.Length; i++)
        {
            var btnGO = new GameObject($"ModuleButton_{i}", typeof(RectTransform));
            btnGO.transform.SetParent(parent, false);
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(360f, 120f);
            rt.anchoredPosition = new Vector2(0f, 140f - i * 150f);

            btnGO.AddComponent<Image>();
            var button = btnGO.AddComponent<Button>();

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(btnGO.transform, false);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = entries[i].DisplayName;
            tmp.fontSize = 32f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;

            var lockGO = new GameObject("LockIcon", typeof(RectTransform));
            lockGO.transform.SetParent(btnGO.transform, false);
            var lockRT = lockGO.GetComponent<RectTransform>();
            lockRT.anchorMin = new Vector2(0.5f, 0.5f);
            lockRT.anchorMax = new Vector2(0.5f, 0.5f);
            lockRT.anchoredPosition = new Vector2(150f, 0f);
            lockRT.sizeDelta = new Vector2(36f, 36f);
            var lockImg = lockGO.AddComponent<Image>();
            lockImg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
            lockImg.enabled = false;

            buttons[i] = button;
            lockIcons[i] = lockImg;
            labels[i] = tmp;
        }

        var controller = Object.FindFirstObjectByType<AttackSelectController>();
        if (controller == null)
        {
            Debug.LogError("[AttackSelectUIBuilder] AttackSelectController 컴포넌트를 찾지 못했습니다.");
            return;
        }

        var so = new SerializedObject(controller);
        WriteArray(so, "_moduleButtons", buttons);
        WriteArray(so, "_lockIcons", lockIcons);
        WriteArray(so, "_labels", labels);
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Debug.Log("[AttackSelectUIBuilder] AttackSelect.unity에 CombatModuleRegistry.All 3개 모듈 버튼 배치 및 배선 완료.");
    }

    private static void WriteArray<T>(SerializedObject so, string propertyName, T[] values) where T : Object
    {
        var prop = so.FindProperty(propertyName);
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }
}
