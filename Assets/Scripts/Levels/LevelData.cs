using UnityEngine;

[CreateAssetMenu(fileName = "Level_00", menuName = "CastleCrash/Level Data", order = 0)]
public class LevelData : ScriptableObject
{
    [SerializeField] private string sceneName;
    [SerializeField] private string displayName = "Nivelul 1";

    [Header("Stele")]
    [SerializeField, Min(0)] private int twoStarScore = 3000;
    [SerializeField, Min(0)] private int threeStarScore = 6000;

    public string SceneName => sceneName;
    public string DisplayName => displayName;
    public int TwoStarScore => twoStarScore;
    public int ThreeStarScore => threeStarScore;

    public int StarsFor(int score, bool won)
    {
        if (!won) return 0;
        if (score >= threeStarScore) return 3;
        if (score >= twoStarScore) return 2;
        return 1;
    }

    private void OnValidate()
    {
        threeStarScore = Mathf.Max(threeStarScore, twoStarScore);
    }
}
