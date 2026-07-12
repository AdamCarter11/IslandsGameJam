using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Creates MainMenu.unity (title + Continue / New Game) and sets Build Settings order.
/// </summary>
public static class MainMenuSceneBuilder
{
    const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    const string MainGamePath = "Assets/Scenes/MainGame.unity";
    const string DefaultTmpFontPath = "Assets/slapduck SDF.asset";

    static readonly Color BgColor = new(0.12f, 0.28f, 0.27f, 1f);
    static readonly Color PanelColor = new(0.08f, 0.09f, 0.12f, 0.92f);
    static readonly Color ContinueColor = new(0.25f, 0.55f, 0.3f, 1f);
    static readonly Color NewGameColor = new(0.2f, 0.35f, 0.55f, 0.95f);
    static readonly Color TitleColor = new(1f, 0.92f, 0.45f, 1f);

    [MenuItem("Tools/UI/Build Main Menu Scene")]
    public static void BuildMainMenu()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateEventSystem();
        var controller = CreateMenuUi();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MainMenuPath);
        AssetDatabase.Refresh();

        ApplyBuildSettings();

        Debug.Log($"[MainMenuSceneBuilder] Wrote {MainMenuPath} and updated Build Settings (MainMenu, MainGame).");
        Selection.activeObject = controller;
    }

    public static void BuildMainMenuBatch()
    {
        try
        {
            BuildMainMenu();
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[MainMenuSceneBuilder] Failed: {ex}");
            EditorApplication.Exit(1);
        }
    }

    static void CreateCamera()
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = BgColor;
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 1000f;
        camGo.AddComponent<AudioListener>();

        var urpType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urpType != null)
            camGo.AddComponent(urpType);
    }

    static void CreateEventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static MainMenuController CreateMenuUi()
    {
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var canvasRt = (RectTransform)canvasGo.transform;
        Stretch(canvasRt);

        var panelRt = CreateRect("Panel", canvasRt);
        SetAnchored(panelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(420f, 320f));
        var panelImg = panelRt.gameObject.AddComponent<Image>();
        panelImg.color = PanelColor;
        panelImg.raycastTarget = false;

        var titleRt = CreateRect("Title", panelRt);
        SetAnchored(titleRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -36f), new Vector2(-32f, 64f));
        AddText(titleRt, "Islands", 48, TextAnchor.MiddleCenter, TitleColor);

        var continueRt = CreateRect("ContinueButton", panelRt);
        SetAnchored(continueRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 10f), new Vector2(260f, 52f));
        var continueBtn = MakeButton(continueRt, "Continue", ContinueColor);

        var newGameRt = CreateRect("NewGameButton", panelRt);
        SetAnchored(newGameRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -62f), new Vector2(260f, 52f));
        var newGameBtn = MakeButton(newGameRt, "New Game", NewGameColor);

        var controller = canvasGo.AddComponent<MainMenuController>();
        controller.EditorAssign(continueBtn, newGameBtn);
        return controller;
    }

    static Button MakeButton(RectTransform rt, string label, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;
        ApplyButtonColors(button, color);

        var labelRt = CreateRect("Label", rt);
        Stretch(labelRt);
        AddText(labelRt, label, 24, TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    static void ApplyBuildSettings()
    {
        var mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        var mainGame = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainGamePath);
        if (mainMenu == null || mainGame == null)
        {
            Debug.LogError("[MainMenuSceneBuilder] Missing MainMenu or MainGame scene asset.");
            return;
        }

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuPath, true),
            new EditorBuildSettingsScene(MainGamePath, true),
        };
    }

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static TextMeshProUGUI AddText(RectTransform rt, string content, int fontSize, TextAnchor alignment, Color color)
    {
        var text = rt.gameObject.AddComponent<TextMeshProUGUI>();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DefaultTmpFontPath);
        if (font == null)
            font = TMP_Settings.defaultFontAsset;
        if (font != null)
            text.font = font;
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = ToTmpAlignment(alignment);
        text.color = color;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    static TextAlignmentOptions ToTmpAlignment(TextAnchor alignment) => alignment switch
    {
        TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
        TextAnchor.UpperCenter => TextAlignmentOptions.Top,
        TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
        TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
        TextAnchor.MiddleCenter => TextAlignmentOptions.Center,
        TextAnchor.MiddleRight => TextAlignmentOptions.Right,
        TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
        TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
        TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
        _ => TextAlignmentOptions.Center
    };

    static void ApplyButtonColors(Button button, Color normalColor)
    {
        var colors = button.colors;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.15f);
        colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.4f);
        button.colors = colors;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
    }
}
