using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameScreensUI : MonoBehaviour
{
    private static bool skipMenuOnce = false;

    private GameObject mainMenuPanel;
    private GameObject endPanel;
    private TMP_Text endTitleText;
    private TMP_Text endInfoText;
    private GameObject nextBtnObj;

    private CatapultAiming aiming;

    void Start()
    {
        // Opreste cutia gri temporara de pe GameManager
        var oldPlaceholder = FindFirstObjectByType<LevelEndPlaceholder>();
        if (oldPlaceholder != null) oldPlaceholder.enabled = false;

        aiming = FindFirstObjectByType<CatapultAiming>();

        BuildMainMenu();
        BuildEndScreen();

        if (GameManager.Instance != null)
            GameManager.Instance.LevelEnded += OnLevelEnded;

        if (skipMenuOnce)
        {
            skipMenuOnce = false;
            mainMenuPanel.SetActive(false);
        }
        else
        {
            mainMenuPanel.SetActive(true);
            if (aiming != null) aiming.InputEnabled = false;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.LevelEnded -= OnLevelEnded;
    }

    void BuildMainMenu()
    {
        mainMenuPanel = CreateBackdrop("MainMenuPanel");
        Transform box = CreateBox(mainMenuPanel.transform, 460, 320);

        CreateText(box, "CASTLE CRASH", 44, Color.white, 420, 70);
        CreateButton(box, "PLAY", new Color(0.2f, 0.65f, 0.25f), () =>
        {
            mainMenuPanel.SetActive(false);
            if (aiming != null) aiming.InputEnabled = true;
        });
        CreateButton(box, "QUIT", new Color(0.75f, 0.25f, 0.2f), () =>
        {
            Application.Quit();
            Debug.Log("Quit apasat (inchide jocul in Build).");
        });
    }

    void BuildEndScreen()
    {
        endPanel = CreateBackdrop("LevelEndPanel");
        Transform box = CreateBox(endPanel.transform, 480, 400);

        endTitleText = CreateText(box, "LEVEL COMPLETE!", 38, Color.yellow, 440, 60);
        endInfoText = CreateText(box, "Scor: 0", 28, Color.white, 440, 75);

        nextBtnObj = CreateButton(box, "Next", new Color(0.2f, 0.65f, 0.25f), () =>
        {
            skipMenuOnce = true;
            if (GameManager.Instance != null) GameManager.Instance.LoadNextLevel();
        });

        CreateButton(box, "Retry", new Color(0.2f, 0.5f, 0.8f), () =>
        {
            skipMenuOnce = true;
            if (GameManager.Instance != null) GameManager.Instance.Retry();
        });

        CreateButton(box, "Menu", new Color(0.55f, 0.35f, 0.2f), () =>
        {
            skipMenuOnce = false;
            if (GameManager.Instance != null) GameManager.Instance.Retry();
        });

        endPanel.SetActive(false);
    }

    void OnLevelEnded(LevelResult r)
    {
        endPanel.SetActive(true);

        if (r.Won)
        {
            endTitleText.text = "LEVEL COMPLETE!";
            endTitleText.color = new Color(1f, 0.85f, 0.2f);
            endInfoText.text = $"Scor: {r.Score}\nStele: {r.Stars} / 3";
            nextBtnObj.SetActive(GameManager.Instance != null && GameManager.Instance.HasNextLevel);
        }
        else
        {
            endTitleText.text = "GAME OVER";
            endTitleText.color = new Color(1f, 0.35f, 0.35f);
            endInfoText.text = $"Scor: {r.Score}";
            nextBtnObj.SetActive(false);
        }
    }

    GameObject CreateBackdrop(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);
        return go;
    }

    Transform CreateBox(Transform parent, float w, float h)
    {
        var box = new GameObject("Box", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        box.transform.SetParent(parent, false);
        box.GetComponent<RectTransform>().sizeDelta = new Vector2(w, h);
        box.GetComponent<Image>().color = new Color(0.14f, 0.14f, 0.18f, 0.96f);

        var layout = box.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 16;
        layout.padding = new RectOffset(25, 25, 25, 25);
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        return box.transform;
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

    GameObject CreateButton(Transform parent, string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 52);
        go.GetComponent<Image>().color = bgColor;

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }
}