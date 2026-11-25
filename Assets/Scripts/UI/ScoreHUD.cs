using TMPro;
using UnityEngine;

public class ScoreHUD : MonoBehaviour
{
    [SerializeField] private TMP_Text scoreText;

    private ScoreManager score;

    private void Start()
    {
        if (GameManager.Instance == null) return;
        score = GameManager.Instance.Score;
        score.ScoreChanged += OnScoreChanged;
        OnScoreChanged(score.Score);
    }

    private void OnDestroy()
    {
        if (score != null) score.ScoreChanged -= OnScoreChanged;
    }

    private void OnScoreChanged(int value)
    {
        if (scoreText != null) scoreText.SetText("Scor: {0}", value);
    }
}
