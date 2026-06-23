using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public static class MainMenuSceneBuilder
{
    [MenuItem("Fast/Build MainMenu Scene")]
    public static void BuildMainMenuScene()
    {
        // 1. Create empty scene
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // 2. Camera
        var cameraGO = new GameObject("Main Camera");
        var cam = cameraGO.AddComponent<Camera>();
        cameraGO.AddComponent<AudioListener>();
        cameraGO.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        // 3. Canvas
        var canvasGO = new GameObject("Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasGO.AddComponent<GraphicRaycaster>();

        // 4. EventSystem
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 5. Title text
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(canvasGO.transform, false);
        var titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.7f);
        titleRT.anchorMax = new Vector2(0.5f, 0.7f);
        titleRT.anchoredPosition = Vector2.zero;
        titleRT.sizeDelta = new Vector2(600f, 120f);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "Fast";
        titleTMP.fontSize = 96f;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.color = Color.white;

        // 6. Start button
        var startBtnGO = CreateButton(canvasGO, "StartButton", new Vector2(0f, 0f), new Vector2(0.5f, 0.45f), "Start");

        // 7. Quit button
        var quitBtnGO = CreateButton(canvasGO, "QuitButton", new Vector2(0f, -120f), new Vector2(0.5f, 0.45f), "Quit");

        // 8. MainMenuController
        var controllerGO = new GameObject("MainMenuController");
        controllerGO.transform.SetParent(canvasGO.transform, false);
        var controller = controllerGO.AddComponent<MainMenuController>();

        // Wire Start button
        var startBtn = startBtnGO.GetComponent<Button>();
        startBtn.onClick.AddListener(new UnityAction(controller.OnStartClicked));

        // Wire Quit button
        var quitBtn = quitBtnGO.GetComponent<Button>();
        quitBtn.onClick.AddListener(new UnityAction(controller.OnQuitClicked));

        // 9. Save scene
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");

        // 10. Update Build Settings
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        string sampleScenePath = "Assets/Scenes/SampleScene.unity";

        var mainMenuEntry = new EditorBuildSettingsScene(mainMenuPath, true);
        var sampleSceneEntry = new EditorBuildSettingsScene(sampleScenePath, true);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { mainMenuEntry, sampleSceneEntry };

        Debug.Log("[MainMenuSceneBuilder] MainMenu.unity created and Build Settings updated.");
    }

    private static GameObject CreateButton(GameObject parent, string name, Vector2 anchoredPos, Vector2 anchor, string label)
    {
        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent.transform, false);
        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(400f, 80f);
        var img = btnGO.AddComponent<Image>();
        img.color = Color.white;
        btnGO.AddComponent<Button>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 48f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;

        return btnGO;
    }
}
