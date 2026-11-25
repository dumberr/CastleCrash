using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Projectile Types (Sprint 2) — butoanele de alegere a proiectilului.
/// Se construieste singur la runtime (un Canvas propriu, jos pe centru), deci nu
/// trebuie modificata scena: e suficient sa stea pe catapulta, langa ProjectileSelector.
///
/// Fiecare buton arata tasta, numele tipului si cate proiectile mai sunt din el
/// (doar pentru tipurile limitate). Tipul ales e evidentiat.
/// </summary>
[RequireComponent(typeof(ProjectileSelector))]
public class ProjectileSelectorHUD : MonoBehaviour
{
    [SerializeField] private Vector2 buttonSize = new Vector2(200f, 64f);
    [SerializeField] private float spacing = 12f;
    [SerializeField] private float bottomMargin = 24f;
    [Tooltip("Sub Canvas-urile cu meniul de pauza / ecranele de final.")]
    [SerializeField] private int canvasSortingOrder = -1;
    [SerializeField] private Color normalTint = new Color(0.12f, 0.12f, 0.16f, 0.85f);
    [SerializeField] private Color selectedTint = new Color(0.95f, 0.75f, 0.2f, 0.95f);
    [SerializeField] private Color unavailableTint = new Color(0.12f, 0.12f, 0.16f, 0.35f);

    private class Slot
    {
        public Button button;
        public Image background;
        public Image swatch;
        public TMP_Text label;
    }

    private ProjectileSelector selector;
    private GameObject root;
    private readonly List<Slot> slots = new List<Slot>();

    private void Awake()
    {
        selector = GetComponent<ProjectileSelector>();
    }

    private void OnEnable()
    {
        selector.SelectionChanged += OnSelectionChanged;
        selector.CountsChanged += Refresh;
        if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnGameStateChanged;
    }

    private void OnDisable()
    {
        selector.SelectionChanged -= OnSelectionChanged;
        selector.CountsChanged -= Refresh;
        if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnGameStateChanged;
    }

    private void Start()
    {
        // GameManager.Instance poate aparea dupa OnEnable-ul nostru; ne abonam si aici.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.StateChanged -= OnGameStateChanged;
            GameManager.Instance.StateChanged += OnGameStateChanged;
        }

        if (selector.Count < 2) return; // un singur tip -> nu are rost selectorul
        Build();
        Refresh();
    }

    private void OnDestroy()
    {
        if (root != null) Destroy(root);
    }

    private void OnSelectionChanged(int index) => Refresh();

    private void OnGameStateChanged(GameState state)
    {
        if (root != null) root.SetActive(state == GameState.Playing);
    }

    private void Build()
    {
        root = new GameObject("ProjectileSelectorHUD", typeof(RectTransform));
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortingOrder;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        var row = new GameObject("Row", typeof(RectTransform));
        row.transform.SetParent(root.transform, false);
        var rowRt = (RectTransform)row.transform;
        rowRt.anchorMin = rowRt.anchorMax = new Vector2(0.5f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.anchoredPosition = new Vector2(0f, bottomMargin);
        rowRt.sizeDelta = new Vector2(selector.Count * buttonSize.x + (selector.Count - 1) * spacing, buttonSize.y);
        var layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;

        for (int i = 0; i < selector.Count; i++)
            slots.Add(CreateSlot(row.transform, i));
    }

    private Slot CreateSlot(Transform parent, int index)
    {
        var go = new GameObject($"Projectile_{index + 1}", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var slot = new Slot { background = go.AddComponent<Image>() };
        slot.button = go.AddComponent<Button>();
        slot.button.targetGraphic = slot.background;
        int captured = index;
        slot.button.onClick.AddListener(() => selector.Select(captured));

        // Patratel colorat cu culoarea tipului.
        var swatchGo = new GameObject("Swatch", typeof(RectTransform));
        swatchGo.transform.SetParent(go.transform, false);
        var swRt = (RectTransform)swatchGo.transform;
        swRt.anchorMin = swRt.anchorMax = new Vector2(0f, 0.5f);
        swRt.pivot = new Vector2(0f, 0.5f);
        swRt.anchoredPosition = new Vector2(12f, 0f);
        swRt.sizeDelta = new Vector2(buttonSize.y * 0.5f, buttonSize.y * 0.5f);
        slot.swatch = swatchGo.AddComponent<Image>();
        slot.swatch.raycastTarget = false;

        var labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);
        var lRt = (RectTransform)labelGo.transform;
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.offsetMin = new Vector2(buttonSize.y * 0.5f + 20f, 4f);
        lRt.offsetMax = new Vector2(-8f, -4f);
        var label = labelGo.AddComponent<TextMeshProUGUI>();
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        slot.label = label;

        return slot;
    }

    private void Refresh()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            Slot slot = slots[i];
            Projectile prefab = selector.GetPrefab(i);
            bool available = selector.IsAvailable(i);
            bool selected = i == selector.SelectedIndex;
            int remaining = selector.Remaining(i);

            slot.background.color = selected ? selectedTint : available ? normalTint : unavailableTint;
            slot.button.interactable = available;

            Color typeColor = prefab != null ? prefab.UiColor : Color.gray;
            slot.swatch.color = available ? typeColor : new Color(typeColor.r, typeColor.g, typeColor.b, 0.35f);

            string typeName = prefab != null ? prefab.DisplayName : "?";
            string count = remaining < 0 ? "" : $"  x{remaining}";
            slot.label.text = $"<b>{i + 1}</b>  {typeName}{count}";
            slot.label.color = selected ? Color.black : available ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        }
    }
}
