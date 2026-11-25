using UnityEngine;

public static class LevelProgression
{
    private const string Prefix = "CastleCrash.";

    public static int GetBestStars(LevelData level) =>
        level != null ? PlayerPrefs.GetInt(Key(level, "Stars"), 0) : 0;

    public static int GetBestScore(LevelData level) =>
        level != null ? PlayerPrefs.GetInt(Key(level, "Score"), 0) : 0;

    public static bool IsCompleted(LevelData level) => GetBestStars(level) > 0;

    public static bool IsUnlocked(LevelCatalog catalog, int index)
    {
        if (catalog == null || index < 0 || index >= catalog.Count) return false;
        return index == 0 || IsCompleted(catalog.Get(index - 1));
    }

    // true = record nou
    public static bool RecordResult(LevelData level, int score, int stars)
    {
        if (level == null || stars <= 0) return false;

        bool newBest = score > GetBestScore(level);
        if (newBest) PlayerPrefs.SetInt(Key(level, "Score"), score);
        if (stars > GetBestStars(level)) PlayerPrefs.SetInt(Key(level, "Stars"), stars);
        PlayerPrefs.Save();
        return newBest;
    }

    public static void ResetAll(LevelCatalog catalog)
    {
        if (catalog == null) return;
        foreach (LevelData level in catalog.Levels)
        {
            if (level == null) continue;
            PlayerPrefs.DeleteKey(Key(level, "Stars"));
            PlayerPrefs.DeleteKey(Key(level, "Score"));
        }
        PlayerPrefs.Save();
    }

    private static string Key(LevelData level, string field) => $"{Prefix}{level.SceneName}.{field}";
}
