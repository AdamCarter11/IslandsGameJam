using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToolTipUI : MonoBehaviour
{
    public static ToolTipUI Main { get; private set; }

    private const float DefaultMaxWidth = 320f;
    private const float HorizontalPadding = 18f;
    private const float VerticalPadding = 12f;

    [SerializeField] 
    private Canvas canvas;
    [SerializeField]
    private Transform hoverPanel;
    [SerializeField]
    private TMP_Text hoverText;
    [SerializeField]
    private Vector3 worldOffset = new Vector3(0f, 0.85f, 0f);
    [SerializeField]
    private Vector2 screenOffset = new Vector2(0f, 14f);
    [SerializeField]
    private bool autoSizeToText = true;

    private RectTransform hoverPanelRectTransform;
    private RectTransform canvasRectTransform;
    private Transform target;
    private string currentText;
    private bool isVisible;

    public static ToolTipUI GetOrCreate()
    {
        if (Main != null)
            return Main;

        ToolTipUI existing = FindFirstObjectByType<ToolTipUI>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (!existing.gameObject.activeSelf)
                existing.gameObject.SetActive(true);

            existing.ResolveReferences();
            existing.Hide();
            Main = existing;
            return existing;
        }

        return BuildRuntimeUi();
    }

    private void Awake()
    {
        if (Main != null && Main != this)
        {
            Destroy(gameObject);
            return;
        }

        Main = this;
        ResolveReferences();
        Hide();
    }

    private void OnDestroy()
    {
        if (Main == this)
            Main = null;
    }

    private void LateUpdate()
    {
        if (!isVisible)
            return;

        if (target == null)
        {
            Hide();
            return;
        }

        FollowTarget();
    }

    public void Show(ToolTipObject toolTipObject, Transform followTarget)
    {
        if (toolTipObject == null || !toolTipObject.HasToolTip)
        {
            Hide();
            return;
        }

        Show(toolTipObject.ToolTip, followTarget);
    }

    public void Show(string text, Transform followTarget)
    {
        if (string.IsNullOrWhiteSpace(text) || followTarget == null)
        {
            Hide();
            return;
        }

        ResolveReferences();

        if (hoverPanel == null || hoverText == null || hoverPanelRectTransform == null || canvasRectTransform == null)
            return;

        string trimmedText = text.Trim();
        bool contentChanged = currentText != trimmedText;
        currentText = trimmedText;
        target = followTarget;
        isVisible = true;

        if (contentChanged)
        {
            hoverText.text = currentText;
            ResizeToText();
        }

        if (!hoverPanel.gameObject.activeSelf)
            hoverPanel.gameObject.SetActive(true);

        FollowTarget();
    }

    public void Hide()
    {
        target = null;
        currentText = null;
        isVisible = false;

        if (hoverPanel != null)
            hoverPanel.gameObject.SetActive(false);
    }

    private void ResolveReferences()
    {
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (hoverPanel == null)
            hoverPanel = transform;

        if (hoverText == null)
            hoverText = GetComponentInChildren<TMP_Text>(true);

        hoverPanelRectTransform = hoverPanel as RectTransform;
        canvasRectTransform = canvas != null ? canvas.transform as RectTransform : null;
    }

    private void FollowTarget()
    {
        if (target == null || hoverPanelRectTransform == null || canvasRectTransform == null)
            return;

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
            return;

        Vector3 targetPosition = target.position + worldOffset;
        Vector3 screenPosition = worldCamera.WorldToScreenPoint(targetPosition);
        Camera canvasCamera = GetCanvasCamera();

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform,
                screenPosition,
                canvasCamera,
                out Vector2 localPosition))
        {
            hoverPanelRectTransform.anchoredPosition = localPosition + screenOffset;
        }
    }

    private Camera GetCanvasCamera()
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    private void ResizeToText()
    {
        if (!autoSizeToText || hoverText == null || hoverPanelRectTransform == null)
            return;

        float textMaxWidth = DefaultMaxWidth - HorizontalPadding * 2f;
        Vector2 preferred = hoverText.GetPreferredValues(currentText, textMaxWidth, 0f);
        float width = Mathf.Clamp(preferred.x + HorizontalPadding * 2f, 120f, DefaultMaxWidth);
        float height = Mathf.Max(44f, preferred.y + VerticalPadding * 2f);

        hoverPanelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
        hoverPanelRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

        RectTransform textRectTransform = hoverText.rectTransform;
        textRectTransform.anchorMin = Vector2.zero;
        textRectTransform.anchorMax = Vector2.one;
        textRectTransform.offsetMin = new Vector2(HorizontalPadding, VerticalPadding);
        textRectTransform.offsetMax = new Vector2(-HorizontalPadding, -VerticalPadding);
    }

    private static ToolTipUI BuildRuntimeUi()
    {
        GameObject root = new GameObject(
            "ToolTipUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        root.SetActive(false);

        Canvas builtCanvas = root.GetComponent<Canvas>();
        builtCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        builtCanvas.sortingOrder = 450;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootRect = root.transform as RectTransform;
        Stretch(rootRect);

        RectTransform panel = CreateRect("HoverPanel", rootRect);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.sizeDelta = new Vector2(220f, 56f);

        Image background = panel.gameObject.AddComponent<Image>();
        background.color = new Color(0.08f, 0.09f, 0.12f, 0.94f);
        background.raycastTarget = false;

        RectTransform textRoot = CreateRect("Text", panel);
        Stretch(textRoot);

        TextMeshProUGUI text = textRoot.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = 18f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        TMP_FontAsset font = Resources.Load<TMP_FontAsset>("slapduck SDF");
        if (font != null)
        {
            text.font = font;
            if (font.material != null)
                text.fontSharedMaterial = font.material;
        }

        ToolTipUI ui = root.AddComponent<ToolTipUI>();
        ui.canvas = builtCanvas;
        ui.hoverPanel = panel;
        ui.hoverText = text;
        root.SetActive(true);
        ui.ResolveReferences();
        ui.Hide();
        Main = ui;
        return ui;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rectTransform = go.transform as RectTransform;
        rectTransform.SetParent(parent, false);
        return rectTransform;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
    }
}
