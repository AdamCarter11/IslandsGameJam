using System;
using UnityEngine;

/// <summary>
/// PlayerPrefs-backed gameplay options (usable from Main Menu without a GameManager).
/// </summary>
public static class GameplaySettings
{
    public const string HideCropStatusIconsKey = "hideCropStatusIcons";

    /// <summary>Fired after <see cref="HideCropStatusIcons"/> changes.</summary>
    public static event Action OnHideCropStatusIconsChanged;

    /// <summary>
    /// When true, needs-water and harvest-ready crop icons are hidden.
    /// Default false (icons shown).
    /// </summary>
    public static bool HideCropStatusIcons
    {
        get => PlayerPrefs.GetInt(HideCropStatusIconsKey, 0) != 0;
        set
        {
            int next = value ? 1 : 0;
            if (PlayerPrefs.GetInt(HideCropStatusIconsKey, 0) == next)
                return;

            PlayerPrefs.SetInt(HideCropStatusIconsKey, next);
            PlayerPrefs.Save();
            OnHideCropStatusIconsChanged?.Invoke();
        }
    }
}
