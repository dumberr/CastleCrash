using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField, Min(0)] private float pointsPerBlockHealth = 10f;
    [SerializeField, Min(0)] private int pointsPerSpareProjectile = 1000;

    public event Action<int> ScoreChanged;

    public int Score { get; private set; }
    public bool Counting { get; set; } = true;

    private void Start()
    {
        foreach (PhysicsBlock block in FindObjectsByType<PhysicsBlock>(FindObjectsSortMode.None))
            block.Broken += OnBlockBroken;

        foreach (LevelTarget target in FindObjectsByType<LevelTarget>(FindObjectsSortMode.None))
            target.Destroyed += OnTargetDestroyed;

        ScoreChanged?.Invoke(Score);
    }

    public int AddSpareProjectileBonus(int spareProjectiles)
    {
        int bonus = Mathf.Max(0, spareProjectiles) * pointsPerSpareProjectile;
        Add(bonus);
        return bonus;
    }

    private void OnBlockBroken(PhysicsBlock block)
    {
        block.Broken -= OnBlockBroken;
        Add(Mathf.RoundToInt(block.MaxHealth * pointsPerBlockHealth));
    }

    private void OnTargetDestroyed(LevelTarget target)
    {
        target.Destroyed -= OnTargetDestroyed;
        Add(target.ScoreValue);
    }

    private void Add(int points)
    {
        if (!Counting || points <= 0) return;
        Score += points;
        ScoreChanged?.Invoke(Score);
    }
}
