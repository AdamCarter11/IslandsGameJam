using UnityEngine;

/// <summary>
/// PlayerPrefs-backed volume settings. Usable from Main Menu without an AudioService.
/// When a live AudioService exists, setters/getters prefer it (which also persists).
/// </summary>
public static class AudioSettings
{
    public const string SfxVolumeKey = "sfxVolume";
    public const string MusicVolumeKey = "musicVolume";
    public const float DefaultVolume = 1f;

    public static float GetSfxVolume()
    {
        var audio = GameManager.Main?.AudioService;
        if (audio != null)
            return audio.GetSfxVolume();
        return LoadSfxVolume();
    }

    public static float GetMusicVolume()
    {
        var audio = GameManager.Main?.AudioService;
        if (audio != null)
            return audio.GetMusicVolume();
        return LoadMusicVolume();
    }

    public static void SetSfxVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        var audio = GameManager.Main?.AudioService;
        if (audio != null)
        {
            audio.SetSfxVolume(volume);
            return;
        }

        SaveSfxVolume(volume);
    }

    public static void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        var audio = GameManager.Main?.AudioService;
        if (audio != null)
        {
            audio.SetMusicVolume(volume);
            return;
        }

        SaveMusicVolume(volume);
    }

    public static float LoadSfxVolume() =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, DefaultVolume));

    public static float LoadMusicVolume() =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, DefaultVolume));

    public static void SaveSfxVolume(float volume)
    {
        PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }

    public static void SaveMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(volume));
        PlayerPrefs.Save();
    }
}
