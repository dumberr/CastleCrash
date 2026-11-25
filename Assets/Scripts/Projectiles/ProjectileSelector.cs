using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Projectile Types (Sprint 2) — alegerea tipului de proiectil inainte de lansare.
///
/// Controale:
///   1 / 2 / 3 ...  -> alege direct tipul cu indexul respectiv
///   Q / E          -> tipul anterior / urmator
///   (sau butoanele din ProjectileSelectorHUD)
///
/// Fiecare tip poate avea o limita proprie pe nivel (ex. un singur butoi exploziv).
/// Limita totala de proiectile ramane cea din LimitedProjectiles.
/// Cand tipul ales se termina, se trece automat pe primul tip disponibil.
/// </summary>
[RequireComponent(typeof(ProjectileLauncher))]
public class ProjectileSelector : MonoBehaviour
{
    [Serializable]
    public class Option
    {
        public Projectile prefab;
        [Tooltip("Cate proiectile de acest tip se pot lansa pe nivel. -1 = nelimitat (doar limita totala).")]
        public int limit = -1;
    }

    [SerializeField] private Option[] options = Array.Empty<Option>();
    [SerializeField, Min(0)] private int startIndex;
    [SerializeField] private bool enableKeyboard = true;

    /// <summary>S-a schimbat tipul selectat (index nou).</summary>
    public event Action<int> SelectionChanged;
    /// <summary>S-a schimbat cate proiectile mai sunt dintr-un tip (dupa o lansare).</summary>
    public event Action CountsChanged;

    public int Count => options.Length;
    public int SelectedIndex { get; private set; } = -1;
    public Projectile SelectedPrefab => IsValid(SelectedIndex) ? options[SelectedIndex].prefab : null;

    private static readonly Key[] DigitKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
    private static readonly Key[] NumpadKeys = { Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4, Key.Numpad5 };

    private ProjectileLauncher launcher;
    private int[] used;

    private void Awake()
    {
        launcher = GetComponent<ProjectileLauncher>();
        used = new int[options.Length];

        // Alegem tipul inainte de Start-ul launcher-ului, ca primul proiectil incarcat sa fie cel corect.
        int first = IsAvailable(startIndex) ? startIndex : FirstAvailable();
        if (first >= 0) ApplySelection(first, notify: false);
    }

    private void OnEnable() => launcher.ProjectileLaunched += OnProjectileLaunched;
    private void OnDisable() => launcher.ProjectileLaunched -= OnProjectileLaunched;

    private void Start()
    {
        CountsChanged?.Invoke();
        if (SelectedIndex >= 0) SelectionChanged?.Invoke(SelectedIndex);
    }

    private void Update()
    {
        if (!enableKeyboard || Time.timeScale == 0f || !launcher.Aiming.InputEnabled) return;
        var kb = Keyboard.current;
        if (kb == null) return;

        for (int i = 0; i < options.Length && i < DigitKeys.Length; i++)
        {
            if (kb[DigitKeys[i]].wasPressedThisFrame || kb[NumpadKeys[i]].wasPressedThisFrame)
            {
                Select(i);
                return;
            }
        }

        if (kb.qKey.wasPressedThisFrame) Cycle(-1);
        else if (kb.eKey.wasPressedThisFrame) Cycle(+1);
    }

    public Projectile GetPrefab(int index) => IsValid(index) ? options[index].prefab : null;

    /// <summary>Cate proiectile mai sunt din tipul dat. -1 = nelimitat.</summary>
    public int Remaining(int index)
    {
        if (!IsValid(index)) return 0;
        int limit = options[index].limit;
        return limit < 0 ? -1 : Mathf.Max(0, limit - used[index]);
    }

    public bool IsAvailable(int index) => IsValid(index) && Remaining(index) != 0;

    /// <summary>Alege tipul dat. Returneaza false daca nu e disponibil.</summary>
    public bool Select(int index)
    {
        if (!IsAvailable(index)) return false;
        if (index == SelectedIndex) return true;
        ApplySelection(index, notify: true);
        return true;
    }

    /// <summary>Trece la urmatorul (dir = +1) sau anteriorul (dir = -1) tip disponibil.</summary>
    public void Cycle(int dir)
    {
        if (options.Length == 0) return;
        int start = SelectedIndex < 0 ? 0 : SelectedIndex;
        for (int step = 1; step <= options.Length; step++)
        {
            int i = ((start + dir * step) % options.Length + options.Length) % options.Length;
            if (IsAvailable(i))
            {
                Select(i);
                return;
            }
        }
    }

    private void ApplySelection(int index, bool notify)
    {
        SelectedIndex = index;
        launcher.SetProjectilePrefab(options[index].prefab);
        if (notify) SelectionChanged?.Invoke(index);
    }

    private void OnProjectileLaunched(Projectile projectile)
    {
        // Proiectilul lansat e mereu din tipul selectat (schimbarea tipului inlocuieste proiectilul incarcat).
        if (!IsValid(SelectedIndex)) return;
        used[SelectedIndex]++;
        CountsChanged?.Invoke();

        if (!IsAvailable(SelectedIndex))
        {
            int next = FirstAvailable();
            if (next >= 0) ApplySelection(next, notify: true);
        }
    }

    private int FirstAvailable()
    {
        for (int i = 0; i < options.Length; i++)
            if (IsAvailable(i)) return i;
        return -1;
    }

    private bool IsValid(int index) =>
        index >= 0 && index < options.Length && options[index] != null && options[index].prefab != null;

    private void OnValidate()
    {
        if (options == null) options = Array.Empty<Option>();
        startIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, options.Length - 1));
    }
}
