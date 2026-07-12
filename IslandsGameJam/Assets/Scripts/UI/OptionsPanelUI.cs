using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared options overlay: SFX / Music volume sliders and Close.
/// Works on Main Menu (prefs only) and in-game (prefs + live AudioService).
/// </summary>
public class OptionsPanelUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Slider sfxSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Button closeButton;

    Action onClosed;
    bool syncingSliders;

    public bool IsOpen => PanelRoot != null && PanelRoot.activeSelf;

    GameObject PanelRoot => panel != null ? panel : gameObject;

    void Awake()
    {
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
        SyncSlidersFromSettings();
        PanelRoot.SetActive(true);
    }

    public void Close()
    {
        if (!IsOpen)
            return;

        PanelRoot.SetActive(false);
        onClosed?.Invoke();
    }

    void SyncSlidersFromSettings()
    {
        syncingSliders = true;
        if (sfxSlider != null)
            sfxSlider.value = AudioSettings.GetSfxVolume();
        if (musicSlider != null)
            musicSlider.value = AudioSettings.GetMusicVolume();
        syncingSliders = false;
    }

    void OnSfxSliderChanged(float value)
    {
        if (syncingSliders)
            return;
        AudioSettings.SetSfxVolume(value);
    }

    void OnMusicSliderChanged(float value)
    {
        if (syncingSliders)
            return;
        AudioSettings.SetMusicVolume(value);
    }

#if UNITY_EDITOR
    public void EditorAssign(GameObject panelRoot, Slider sfx, Slider music, Button close)
    {
        panel = panelRoot;
        sfxSlider = sfx;
        musicSlider = music;
        closeButton = close;
    }
#endif
}
