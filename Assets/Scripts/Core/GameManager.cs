using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Playing, Won, Lost }

public readonly struct LevelResult
{
    public readonly bool Won;
    public readonly int Score;
    public readonly int Stars;
    public readonly bool NewBest;

    public LevelResult(bool won, int score, int stars, bool newBest)
    {
        Won = won;
        Score = score;
        Stars = stars;
        NewBest = newBest;
    }
}

[RequireComponent(typeof(ScoreManager))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private LevelCatalog catalog;
    [SerializeField] private LevelData levelOverride;

    [Header("Catapulta (gol = auto)")]
    [SerializeField] private ProjectileLauncher launcher;
    [SerializeField] private LimitedProjectiles ammo;

    [Header("Asteptare (s)")]
    [SerializeField, Min(0)] private float winDelay = 1.5f;
    [SerializeField, Min(0)] private float loseDelay = 2.5f;

    public event Action<GameState> StateChanged;
    public event Action<LevelResult> LevelEnded;

    public GameState State { get; private set; } = GameState.Playing;
    public LevelData CurrentLevel { get; private set; }
    public LevelCatalog Catalog => catalog;
    public ScoreManager Score { get; private set; }
    public LevelResult Result { get; private set; }
    public int TargetsTotal { get; private set; }
    public int TargetsRemaining { get; private set; }
    public bool HasNextLevel => catalog != null && catalog.Next(CurrentLevel) != null;

    private Coroutine endRoutine;
    private bool endingWon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("GameManager duplicat.", this);
            Destroy(gameObject);
            return;
        }
        Instance = this;

        Score = GetComponent<ScoreManager>();
        if (launcher == null) launcher = FindFirstObjectByType<ProjectileLauncher>();
        if (ammo == null && launcher != null) ammo = launcher.GetComponent<LimitedProjectiles>();

        CurrentLevel = levelOverride != null
            ? levelOverride
            : catalog != null ? catalog.FindByScene(SceneManager.GetActiveScene().name) : null;

        if (CurrentLevel == null)
            Debug.LogWarning($"Scena {SceneManager.GetActiveScene().name} lipseste din LevelCatalog.", this);
    }

    private void OnEnable()
    {
        if (launcher != null) launcher.ProjectileFinished += OnProjectileFinished;
    }

    private void OnDisable()
    {
        if (launcher != null) launcher.ProjectileFinished -= OnProjectileFinished;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        foreach (LevelTarget target in FindObjectsByType<LevelTarget>(FindObjectsSortMode.None))
        {
            target.Destroyed += OnTargetDestroyed;
            TargetsTotal++;
        }
        TargetsRemaining = TargetsTotal;

        if (TargetsTotal == 0)
            Debug.LogWarning("Nicio LevelTarget in scena: nivelul nu poate fi castigat.", this);
    }

    private void OnTargetDestroyed(LevelTarget target)
    {
        target.Destroyed -= OnTargetDestroyed;
        if (State != GameState.Playing) return;

        TargetsRemaining = Mathf.Max(0, TargetsRemaining - 1);
        if (TargetsRemaining > 0) return;

        if (endRoutine != null)
        {
            if (endingWon) return;
            StopCoroutine(endRoutine); // anulam infrangerea
        }
        endRoutine = StartCoroutine(EndAfter(winDelay, true));
    }

    private void OnProjectileFinished(Projectile projectile)
    {
        if (State != GameState.Playing || endRoutine != null || TargetsRemaining == 0) return;
        if (ammo != null && ammo.RemainingProjectiles == 0)
            endRoutine = StartCoroutine(EndAfter(loseDelay, false));
    }

    private IEnumerator EndAfter(float delay, bool won)
    {
        endingWon = won;
        if (won && launcher != null) launcher.LaunchingEnabled = false;

        yield return new WaitForSeconds(delay); // fizica se aseaza

        endRoutine = null;
        EndLevel(won);
    }

    private void EndLevel(bool won)
    {
        State = won ? GameState.Won : GameState.Lost;

        if (launcher != null)
        {
            launcher.LaunchingEnabled = false;
            launcher.Aiming.InputEnabled = false;
        }

        if (won && ammo != null) Score.AddSpareProjectileBonus(ammo.RemainingProjectiles);
        Score.Counting = false;

        int stars = CurrentLevel != null ? CurrentLevel.StarsFor(Score.Score, won) : (won ? 1 : 0);
        bool newBest = won && LevelProgression.RecordResult(CurrentLevel, Score.Score, stars);
        Result = new LevelResult(won, Score.Score, stars, newBest);

        Debug.Log($"{(won ? "VICTORIE" : "INFRANGERE")}: scor {Score.Score}, stele {stars}");
        StateChanged?.Invoke(State);
        LevelEnded?.Invoke(Result);
    }

    public void Retry() => LoadScene(SceneManager.GetActiveScene().name);

    public void LoadNextLevel()
    {
        LevelData next = catalog != null ? catalog.Next(CurrentLevel) : null;
        if (next != null) LoadScene(next.SceneName);
        else GoToMainMenu();
    }

    public void GoToMainMenu()
    {
        string menu = catalog != null ? catalog.MainMenuScene : null;
        if (!string.IsNullOrEmpty(menu)) LoadScene(menu);
        else if (catalog != null && catalog.Get(0) != null) LoadScene(catalog.Get(0).SceneName);
        else Retry();
    }

    public static void LoadScene(string sceneName)
    {
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Scena {sceneName} nu e in Build Profiles -> Scene List.");
            return;
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}
