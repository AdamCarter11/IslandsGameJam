using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// End-of-run overlay: score, highscore, Play Again, and Main Menu.
/// Builds its own canvas at runtime so GameHUD does not need a rebuild.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    const string MainMenuSceneName = "MainMenu";
    const string MainGameSceneName = "MainGame";

    [SerializeField] GameObject root;
    [SerializeField] TMP_Text scoreText;
    [SerializeField] TMP_Text highscoreText;
    [SerializeField] TMP_Text newHighscoreText;
    [SerializeField] Button playAgainButton;
    [SerializeField] Button mainMenuButton;

    static readonly Color PanelColor = new(0.08f, 0.09f, 0.12f, 0.96f);
    static readonly Color BackdropColor = new(0f, 0f, 0f, 0.55f);
    static readonly Color TitleColor = new(1f, 0.92f, 0.45f);
    static readonly Color PlayAgainColor = new(0.25f, 0.55f, 0.3f, 1f);
    static readonly Color MainMenuColor = new(0.2f, 0.35f, 0.55f, 0.95f);

    public bool IsVisible => root != null && root.activeSelf;

    void Awake()
    {
        EnsureBuilt();
        Hide();
    }

    public void Show(int score, int highscore, bool isNewRecord)
    {
        EnsureBuilt();

        if (scoreText != null)
            scoreText.text = $"Score: {score}";
        if (highscoreText != null)
            highscoreText.text = $"Highscore: {highscore}";
        if (newHighscoreText != null)
        {
            newHighscoreText.gameObject.SetActive(isNewRecord);
            newHighscoreText.text = "New Highscore!";
        }

        root.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    void EnsureBuilt()
    {
        if (root != null && scoreText != null && playAgainButton != null && mainMenuButton != null)
        {
            WireButtons();
            return;
        }

        BuildRuntimeUi();
        WireButtons();
    }

    void WireButtons()
    {
        if (playAgainButton != null)
        {
            playAgainButton.onClick.RemoveAllListeners();
            playAgainButton.onClick.AddListener(OnPlayAgain);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(OnMainMenu);
        }
    }

    void OnPlayAgain()
    {
        AudioService.Instance?.PlayUiClick();
        if (GameManager.Main != null)
            GameManager.Main.PlayAgain();
        else
            ReloadNewGame();
    }

    void OnMainMenu()
    {
        AudioService.Instance?.PlayUiClick();
        if (GameManager.Main != null)
            GameManager.Main.ReturnToMainMenu();
        else
            LoadMainMenu();
    }

    public static void ReloadNewGame()
    {
        Time.timeScale = 1f;
        SaveGameService.DeleteSave();
        SaveGameService.BootMode = BootMode.New;
        UnityEngine.SceneManagement.SceneManager.LoadScene(MainGameSceneName);
    }

    public static void LoadMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(MainMenuSceneName);
    }

    void BuildRuntimeUi()
    {
        var canvasGo = new GameObject("GameOverCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        root = canvasGo;
        var rootRt = (RectTransform)canvasGo.transform;
        Stretch(rootRt);

        var backdropRt = CreateRect("Backdrop", rootRt);
        Stretch(backdropRt);
        var backdropImg = backdropRt.gameObject.AddComponent<Image>();
        backdropImg.color = BackdropColor;
        backdropImg.raycastTarget = true;

        var panelRt = CreateRect("Panel", rootRt);
        SetAnchored(panelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(480f, 360f));
        var panelImg = panelRt.gameObject.AddComponent<Image>();
        panelImg.color = PanelColor;
        panelImg.raycastTarget = true;

        var titleRt = CreateRect("Title", panelRt);
        SetAnchored(titleRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -28f), new Vector2(-40f, 48f));
        AddText(titleRt, "Time's Up!", 36, TextAnchor.MiddleCenter, TitleColor);

        var scoreRt = CreateRect("Score", panelRt);
        SetAnchored(scoreRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -96f), new Vector2(-40f, 36f));
        scoreText = AddText(scoreRt, "Score: 0", 28, TextAnchor.MiddleCenter, Color.white);

        var highscoreRt = CreateRect("Highscore", panelRt);
        SetAnchored(highscoreRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -140f), new Vector2(-40f, 36f));
        highscoreText = AddText(highscoreRt, "Highscore: 0", 28, TextAnchor.MiddleCenter, TitleColor);

        var newHsRt = CreateRect("NewHighscore", panelRt);
        SetAnchored(newHsRt, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -184f), new Vector2(-40f, 28f));
        newHighscoreText = AddText(newHsRt, "New Highscore!", 22, TextAnchor.MiddleCenter, new Color(0.55f, 1f, 0.55f));
        newHsRt.gameObject.SetActive(false);

        var playAgainRt = CreateRect("PlayAgain", panelRt);
        SetAnchored(playAgainRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(-110f, 36f), new Vector2(180f, 48f));
        playAgainButton = MakeButton(playAgainRt, "Play Again", PlayAgainColor);

        var mainMenuRt = CreateRect("MainMenu", panelRt);
        SetAnchored(mainMenuRt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(110f, 36f), new Vector2(180f, 48f));
        mainMenuButton = MakeButton(mainMenuRt, "Main Menu", MainMenuColor);
    }

    static Button MakeButton(RectTransform rt, string label, Color color)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        var button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = img;

        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        var labelRt = CreateRect("Label", rt);
        Stretch(labelRt);
        AddText(labelRt, label, 22, TextAnchor.MiddleCenter, Color.white);
        return button;
    }

    static TMP_Text AddText(RectTransform rt, string text, int fontSize, TextAnchor align, Color color)
    {
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = AlignToTmp(align);
        tmp.raycastTarget = false;

        var font = Resources.Load<TMP_FontAsset>("slapduck SDF");
        if (font != null)
        {
            tmp.font = font;
            if (font.material != null)
                tmp.fontSharedMaterial = font.material;
        }

        return tmp;
    }

    static TextAlignmentOptions AlignToTmp(TextAnchor align) => align switch
    {
        TextAnchor.MiddleLeft => TextAlignmentOptions.MidlineLeft,
        TextAnchor.MiddleRight => TextAlignmentOptions.MidlineRight,
        TextAnchor.UpperCenter => TextAlignmentOptions.Top,
        _ => TextAlignmentOptions.Center,
    };

    static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void SetAnchored(
        RectTransform rt,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPos,
        Vector2 sizeDelta)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }
}
