using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public static class MainMenuSceneBuilder
{
    [MenuItem("Fast/Build MainMenu Scene")]
    public static void BuildMainMenuScene()
    {
        // 1. Create empty scene
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

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
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // 5. Title text — typeof(RectTransform) in constructor avoids null from GetComponent
        var titleGO = new GameObject("TitleText", typeof(RectTransform));
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

        // Wire Start button — AddPersistentListener serialises the binding into the scene asset
        var startBtn = startBtnGO.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(startBtn.onClick, controller.OnStartClicked);

        // Wire Quit button
        var quitBtn = quitBtnGO.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(quitBtn.onClick, controller.OnQuitClicked);

        // 9. Save scene — GetActiveScene() because NewScene() return value becomes stale
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), "Assets/Scenes/MainMenu.unity");

        // 10. Update Build Settings: MainMenu=0, SampleScene=1
        var mainMenuEntry = new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true);
        var sampleSceneEntry = new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { mainMenuEntry, sampleSceneEntry };

        Debug.Log("[MainMenuSceneBuilder] MainMenu.unity created and Build Settings updated.");
    }

    private static GameObject CreateButton(GameObject parent, string name, Vector2 anchoredPos, Vector2 anchor, string label)
    {
        // typeof(RectTransform) ensures RectTransform exists before GetComponent is called
        var btnGO = new GameObject(name, typeof(RectTransform));
        btnGO.transform.SetParent(parent.transform, false);
        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(400f, 80f);
        var img = btnGO.AddComponent<Image>();
        img.color = Color.white;
        btnGO.AddComponent<Button>();

        var textGO = new GameObject("Text", typeof(RectTransform));
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
