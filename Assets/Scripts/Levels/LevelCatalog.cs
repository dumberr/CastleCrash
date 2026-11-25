using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCatalog", menuName = "CastleCrash/Level Catalog", order = 1)]
public class LevelCatalog : ScriptableObject
{
    [SerializeField] private List<LevelData> levels = new();
    [SerializeField] private string mainMenuScene;

    public IReadOnlyList<LevelData> Levels => levels;
    public int Count => levels.Count;
    public string MainMenuScene => mainMenuScene;

    public LevelData Get(int index) => index >= 0 && index < levels.Count ? levels[index] : null;

    public int IndexOf(LevelData level) => level != null ? levels.IndexOf(level) : -1;

    public LevelData FindByScene(string sceneName)
    {
        foreach (LevelData level in levels)
            if (level != null && level.SceneName == sceneName) return level;
        return null;
    }

    public LevelData Next(LevelData level)
    {
        int i = IndexOf(level);
        return i >= 0 ? Get(i + 1) : null;
    }
}
