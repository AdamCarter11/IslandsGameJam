using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared options overlay: SFX / Music volume sliders, crop-icon toggle, and Close.
/// Works on Main Menu (prefs only) and in-game (prefs + live AudioService).
/// </summary>
public class OptionsPanelUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Toggle hideCropStatusIconsToggle;
    [SerializeField] Button closeButton;

    Action onClosed;
    bool syncingControls;
    /// <summary>
    /// When PanelRoot is this GameObject and starts inactive, Awake runs on the first Open().
    /// Skip the default hide so we don't immediately close again.
    /// </summary>
    bool suppressHideOnAwake;

    public bool IsOpen => PanelRoot != null && PanelRoot.activeSelf;

    GameObject PanelRoot => panel != null ? panel : gameObject;

    void Awake()
    {
        EnsureHideCropStatusIconsToggle();

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        if (musicSlider != null)
        {
            musicSlider.minValue = 0f;
            musicSlider.maxValue = 1f;
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
        }

        if (hideCropStatusIconsToggle != null)
        {
            hideCropStatusIconsToggle.onValueChanged.RemoveListener(OnHideCropStatusIconsChanged);
            hideCropStatusIconsToggle.onValueChanged.AddListener(OnHideCropStatusIconsChanged);
        }

        if (!suppressHideOnAwake)
            PanelRoot.SetActive(false);
    }

    /// <summary>
    /// Optional callback invoked after the panel closes (e.g. resume from pause).
    /// </summary>
    public void SetOnClosed(Action callback)
    {
        onClosed = callback;
    }

    public void Open()
    {
        suppressHideOnAwake = true;
        EnsureHideCropStatusIconsToggle();
        SyncControlsFromSettings();
        PanelRoot.SetActive(true);
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        AudioService.Instance?.PlayUiClick();
        PanelRoot.SetActive(false);
        onClosed?.Invoke();
    }

    void SyncControlsFromSettings()
    {
        syncingControls = true;
        if (sfxSlider != null)
            sfxSlider.value = AudioSettings.GetSfxVolume();
        if (musicSlider != null)
            musicSlider.value = AudioSettings.GetMusicVolume();
        if (hideCropStatusIconsToggle != null)
            hideCropStatusIconsToggle.isOn = GameplaySettings.HideCropStatusIcons;
        syncingControls = false;
    }

    void OnSfxSliderChanged(float value)
    {
        if (syncingControls)
            return;
        AudioSettings.SetSfxVolume(value);
    }

    void OnMusicSliderChanged(float value)
    {
        if (syncingControls)
            return;
        AudioSettings.SetMusicVolume(value);
    }

    void OnHideCropStatusIconsChanged(bool value)
    {
        if (syncingControls)
            return;
        GameplaySettings.HideCropStatusIcons = value;
    }

    /// <summary>
    /// Builds the toggle under the options content panel when the scene wasn't rebuilt yet.
    /// </summary>
    void EnsureHideCropStatusIconsToggle()
    {
        if (hideCropStatusIconsToggle != null)
            return;

        Transform content = FindOptionsContent();
        if (content == null)
            return;

        if (content is RectTransform contentRt && contentRt.sizeDelta.y < 340f)
            contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, 340f);

        var rowGo = new GameObject("HideCropStatusIcons", typeof(RectTransform));
        var rowRt = (RectTransform)rowGo.transform;
        rowRt.SetParent(content, false);
        rowRt.anchorMin = new Vector2(0f, 1f);
        rowRt.anchorMax = new Vector2(1f, 1f);
        rowRt.pivot = new Vector2(0.5f, 1f);
        rowRt.anchoredPosition = new Vector2(0f, -224f);
        rowRt.sizeDelta = new Vector2(-48f, 32f);

        var toggleGo = new GameObject("Toggle", typeof(RectTransform));
        var toggleRt = (RectTransform)toggleGo.transform;
        toggleRt.SetParent(rowRt, false);
        toggleRt.anchorMin = new Vector2(0f, 0.5f);
        toggleRt.anchorMax = new Vector2(0f, 0.5f);
        toggleRt.pivot = new Vector2(0f, 0.5f);
        toggleRt.anchoredPosition = Vector2.zero;
        toggleRt.sizeDelta = new Vector2(28f, 28f);

        var bg = toggleGo.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.2f, 0.24f, 1f);

        var checkGo = new GameObject("Checkmark", typeof(RectTransform));
        var checkRt = (RectTransform)checkGo.transform;
        checkRt.SetParent(toggleRt, false);
        checkRt.anchorMin = Vector2.zero;
        checkRt.anchorMax = Vector2.one;
        checkRt.offsetMin = new Vector2(4f, 4f);
        checkRt.offsetMax = new Vector2(-4f, -4f);
        var checkImg = checkGo.AddComponent<Image>();
        checkImg.color = new Color(0.45f, 0.85f, 0.55f, 1f);

        var toggle = toggleGo.AddComponent<Toggle>();
        toggle.targetGraphic = bg;
        toggle.graphic = checkImg;
        toggle.isOn = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        var labelRt = (RectTransform)labelGo.transform;
        labelRt.SetParent(rowRt, false);
        labelRt.anchorMin = new Vector2(0f, 0f);
        labelRt.anchorMax = new Vector2(1f, 1f);
        labelRt.offsetMin = new Vector2(36f, 0f);
        labelRt.offsetMax = Vector2.zero;

        var label = labelGo.AddComponent<TextMeshProUGUI>();
        var font = Resources.Load<TMP_FontAsset>("slapduck SDF");
        if (font == null)
            font = TMP_Settings.defaultFontAsset;
        if (font != null)
            label.font = font;
        label.text = "Hide crop status icons";
        label.fontSize = 18;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.color = Color.white;
        label.raycastTarget = false;

        hideCropStatusIconsToggle = toggle;
        hideCropStatusIconsToggle.onValueChanged.RemoveListener(OnHideCropStatusIconsChanged);
        hideCropStatusIconsToggle.onValueChanged.AddListener(OnHideCropStatusIconsChanged);
    }

    Transform FindOptionsContent()
    {
        Transform root = PanelRoot != null ? PanelRoot.transform : transform;
        Transform panelChild = root.Find("Panel");
        return panelChild != null ? panelChild : root;
    }

#if UNITY_EDITOR
    public void EditorAssign(GameObject panelRoot, Slider sfx, Slider music, Toggle hideCropIcons, Button close)
    {
        panel = panelRoot;
        sfxSlider = sfx;
        musicSlider = music;
        hideCropStatusIconsToggle = hideCropIcons;
        closeButton = close;
    }
#endif
}
