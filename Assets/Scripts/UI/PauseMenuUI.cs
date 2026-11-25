using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseMenuUI : MonoBehaviour
{
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;

    private GameObject pausePanel;
    private TMP_Text musicLabel;
    private TMP_Text sfxLabel;
    private CatapultAiming aiming;
    private bool isPaused;

    void Start()
    {
        aiming = FindFirstObjectByType<CatapultAiming>();

        MusicVolume = PlayerPrefs.GetFloat("CastleCrash.MusicVolume", 1f);
        SfxVolume = PlayerPrefs.GetFloat("CastleCrash.SfxVolume", 1f);
        ApplyVolume();

        BuildPauseButton();
        BuildPauseMenu();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();
    }

    public void TogglePause()
    {
        if (GameManager.Instance != null && GameManager.Instance.State != GameState.Playing)
            return;

        isPaused = !isPaused;
        pausePanel.SetActive(isPaused);
        Time.timeScale = isPaused ? 0f : 1f;
        if (aiming != null) aiming.InputEnabled = !isPaused;
    }

    void SetMusicVolume(float value)
    {
        MusicVolume = value;
        PlayerPrefs.SetFloat("CastleCrash.MusicVolume", value);
        PlayerPrefs.Save();
        musicLabel.text = $"Muzica: {Mathf.RoundToInt(value * 100)}%";
        ApplyVolume();
    }

    void SetSfxVolume(float value)
    {
        SfxVolume = value;
        PlayerPrefs.SetFloat("CastleCrash.SfxVolume", value);
        PlayerPrefs.Save();
        sfxLabel.text = $"Efecte (SFX): {Mathf.RoundToInt(value * 100)}%";
        ApplyVolume();
    }

    void ApplyVolume()
    {
        AudioListener.volume = Mathf.Max(MusicVolume, SfxVolume);
    }

    void BuildPauseButton()
    {
        var btn = CreateButton(transform, "Pause (Esc)", new Color(0.25f, 0.25f, 0.32f, 0.9f), 160, 44, TogglePause);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -25f);
    }

    void BuildPauseMenu()
    {
        pausePanel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
        pausePanel.transform.SetParent(transform, false);
        Stretch(pausePanel.GetComponent<RectTransform>());
        pausePanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        box.transform.SetParent(pausePanel.transform, false);
        box.GetComponent<RectTransform>().sizeDelta = new Vector2(440, 470);
        box.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 0.96f);

        var layout = box.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 14;
        layout.padding = new RectOffset(25, 25, 25, 25);
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        CreateText(box.transform, "PAUZA", 38, Color.white, 380, 50);

        musicLabel = CreateText(box.transform, $"Muzica: {Mathf.RoundToInt(MusicVolume * 100)}%", 20, Color.white, 300, 28);
        CreateSlider(box.transform, MusicVolume, SetMusicVolume);

        sfxLabel = CreateText(box.transform, $"Efecte (SFX): {Mathf.RoundToInt(SfxVolume * 100)}%", 20, Color.white, 300, 28);
        CreateSlider(box.transform, SfxVolume, SetSfxVolume);

        CreateButton(box.transform, "Resume", new Color(0.2f, 0.65f, 0.25f), 240, 48, TogglePause);
        CreateButton(box.transform, "Restart", new Color(0.2f, 0.5f, 0.8f), 240, 48, () =>
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.Retry();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
        CreateButton(box.transform, "Menu", new Color(0.6f, 0.35f, 0.2f), 240, 48, () =>
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.GoToMainMenu();
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });

        pausePanel.SetActive(false);
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    TMP_Text CreateText(Transform parent, string text, float size, Color color, float w, float h)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        return tmp;
    }

    GameObject CreateButton(Transform parent, string label, Color bgColor, float w, float h, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        go.GetComponent<Image>().color = bgColor;
        go.GetComponent<Button>().onClick.AddListener(onClick);

        var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        Stretch(txtGo.GetComponent<RectTransform>());
        var tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    void CreateSlider(Transform parent, float initialValue, UnityEngine.Events.UnityAction<float> onChanged)
    {
        var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 24);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        Stretch(bg.GetComponent<RectTransform>());
        bg.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.3f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.GetComponent<RectTransform>();
        Stretch(fillRt);
        fill.GetComponent<Image>().color = new Color(0.95f, 0.65f, 0.15f);

        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        Stretch(handleArea.GetComponent<RectTransform>());

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20, 30);
        var handleImg = handle.GetComponent<Image>();
        handleImg.color = Color.white;

        var slider = go.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.fillRect = fillRt;
        slider.handleRect = handleRt;
        slider.targetGraphic = handleImg;
        slider.value = initialValue;
        slider.onValueChanged.AddListener(onChanged);
    }
}
