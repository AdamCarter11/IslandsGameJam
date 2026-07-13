using UnityEngine;

/// <summary>
/// PlayerPrefs-backed persistent highscore (best peak gold across completed runs).
/// Independent of Easy Save so <see cref="SaveGameService.DeleteSave"/> does not wipe it.
/// </summary>
public static class HighscoreSettings
{
    public const string HighscoreKey = "highscore";

    public static int Get() => PlayerPrefs.GetInt(HighscoreKey, 0);

    /// <summary>
    /// Writes <paramref name="score"/> if it exceeds the stored highscore.
    /// Always calls <see cref="PlayerPrefs.Save"/> on write.
    /// </summary>
    /// <returns>True if a new record was saved.</returns>
    public static bool TrySetIfHigher(int score)
    {
        int current = Get();
        if (score <= current)
            return false;

        PlayerPrefs.SetInt(HighscoreKey, score);
        PlayerPrefs.Save();
        return true;
    }
}
