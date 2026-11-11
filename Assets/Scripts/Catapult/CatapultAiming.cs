using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// SCRUM-12 — Catapult Aiming.
/// Controleaza unghiul si puterea de lansare ale catapultei.
///
/// Controale:
///   Tastatura: W/S sau Sageata Sus/Jos  -> unghi
///              A/D sau Sageata Stanga/Dreapta -> putere
///              Space -> cere lansarea (FireRequested)
///   Mouse:     click + trage inapoi (ca la prastie) -> unghi + putere,
///              eliberare -> cere lansarea
///
/// Alte sisteme (Trajectory Preview, UI) citesc LaunchPosition / LaunchVelocity
/// si se pot abona la AimChanged.
/// </summary>
public class CatapultAiming : MonoBehaviour
{
    [Header("Unghi (grade, 0 = orizontal, 90 = vertical)")]
    [SerializeField] private float minAngle = 10f;
    [SerializeField] private float maxAngle = 80f;
    [SerializeField] private float startAngle = 45f;
    [Tooltip("Grade pe secunda cand se tine apasata tasta.")]
    [SerializeField] private float angleSpeed = 60f;

    [Header("Putere (viteza initiala a proiectilului, unitati/s)")]
    [SerializeField] private float minPower = 5f;
    [SerializeField] private float maxPower = 20f;
    [SerializeField] private float startPower = 12f;
    [Tooltip("Unitati de putere pe secunda cand se tine apasata tasta.")]
    [SerializeField] private float powerSpeed = 10f;

    [Header("Orientare")]
    [Tooltip("Catapulta trage spre dreapta (true) sau spre stanga (false).")]
    [SerializeField] private bool facingRight = true;

    [Header("Mouse drag")]
    [SerializeField] private bool enableMouseDrag = true;
    [Tooltip("Distanta (unitati world) de tragere pentru putere maxima.")]
    [SerializeField] private float maxDragDistance = 3f;
    [Tooltip("Sub aceasta distanta drag-ul e ignorat (click simplu nu lanseaza).")]
    [SerializeField] private float minDragDistance = 0.2f;
    [Tooltip("Daca > 0, drag-ul trebuie inceput la cel mult aceasta distanta de punctul de lansare. 0 = oriunde pe ecran.")]
    [SerializeField] private float dragStartRadius = 0f;

    [Header("Referinte vizuale (optional)")]
    [Tooltip("Pivotul bratului: se roteste pe Z dupa unghi.")]
    [SerializeField] private Transform armPivot;
    [Tooltip("De unde pleaca proiectilul. Daca lipseste, se foloseste pozitia obiectului.")]
    [SerializeField] private Transform launchPoint;
    [Tooltip("Linie scurta care arata directia si puterea (NU traiectoria — aia e SCRUM-20).")]
    [SerializeField] private LineRenderer aimIndicator;
    [SerializeField] private float aimIndicatorMaxLength = 2f;

    /// <summary>Se declanseaza cand unghiul sau puterea se schimba.</summary>
    public event Action AimChanged;

    /// <summary>Jucatorul a cerut lansarea (Space sau eliberarea mouse-ului dupa drag).</summary>
    public event Action FireRequested;

    /// <summary>Poate fi oprit din exterior (ex. cat zboara proiectilul, game over, pauza).</summary>
    public bool InputEnabled { get; set; } = true;

    public float Angle { get; private set; }
    public float Power { get; private set; }
    public float MinPower => minPower;
    public float MaxPower => maxPower;
    public float MinAngle => minAngle;
    public float MaxAngle => maxAngle;
    public bool FacingRight => facingRight;
    public bool IsDragging { get; private set; }

    /// <summary>Puterea normalizata 0..1 (pentru UI / bara de putere).</summary>
    public float NormalizedPower => Mathf.InverseLerp(minPower, maxPower, Power);

    /// <summary>Directia de lansare (vector unitate, in world space).</summary>
    public Vector2 Direction
    {
        get
        {
            float rad = Angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad) * (facingRight ? 1f : -1f), Mathf.Sin(rad));
        }
    }

    /// <summary>Viteza initiala pe care o primeste proiectilul.</summary>
    public Vector2 LaunchVelocity => Direction * Power;

    /// <summary>Punctul din care pleaca proiectilul.</summary>
    public Vector2 LaunchPosition => launchPoint != null ? (Vector2)launchPoint.position : (Vector2)transform.position;

    public Transform LaunchPoint => launchPoint;

    private Camera cam;
    private Vector2 dragStartWorld;

    private void Awake()
    {
        cam = Camera.main;
        SetAim(startAngle, startPower, notify: false);

        if (aimIndicator != null && aimIndicator.sharedMaterial == null)
            aimIndicator.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    private void Start()
    {
        // Notificam abia in Start ca sa fie toti ascultatorii abonati (Awake/OnEnable).
        AimChanged?.Invoke();
    }

    private void OnValidate()
    {
        maxAngle = Mathf.Clamp(maxAngle, 0f, 90f);
        minAngle = Mathf.Clamp(minAngle, 0f, maxAngle);
        startAngle = Mathf.Clamp(startAngle, minAngle, maxAngle);
        minPower = Mathf.Max(0f, minPower);
        maxPower = Mathf.Max(minPower, maxPower);
        startPower = Mathf.Clamp(startPower, minPower, maxPower);
        maxDragDistance = Mathf.Max(0.01f, maxDragDistance);
        minDragDistance = Mathf.Clamp(minDragDistance, 0f, maxDragDistance);
    }

    private void Update()
    {
        if (!InputEnabled)
        {
            IsDragging = false;
            return;
        }

        HandleKeyboard();
        if (enableMouseDrag) HandleMouse();
    }

    /// <summary>Seteaza direct unghiul si puterea (valorile sunt limitate la interval).</summary>
    public void SetAim(float angle, float power, bool notify = true)
    {
        float newAngle = Mathf.Clamp(angle, minAngle, maxAngle);
        float newPower = Mathf.Clamp(power, minPower, maxPower);
        bool changed = !Mathf.Approximately(newAngle, Angle) || !Mathf.Approximately(newPower, Power);

        Angle = newAngle;
        Power = newPower;
        UpdateVisuals();

        if (changed && notify) AimChanged?.Invoke();
    }

    /// <summary>Readuce catapulta la unghiul si puterea de start.</summary>
    public void ResetAim() => SetAim(startAngle, startPower);

    private void HandleKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        float angleInput = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) angleInput += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) angleInput -= 1f;

        float powerInput = 0f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) powerInput += 1f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) powerInput -= 1f;

        if (angleInput != 0f || powerInput != 0f)
        {
            SetAim(Angle + angleInput * angleSpeed * Time.deltaTime,
                   Power + powerInput * powerSpeed * Time.deltaTime);
        }

        if (kb.spaceKey.wasPressedThisFrame && !IsDragging)
            FireRequested?.Invoke();
    }

    private void HandleMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null || cam == null) return;

        Vector2 mouseWorld = cam.ScreenToWorldPoint(mouse.position.ReadValue());

        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (IsPointerOverUI()) return;
            if (dragStartRadius > 0f && Vector2.Distance(mouseWorld, LaunchPosition) > dragStartRadius) return;

            IsDragging = true;
            dragStartWorld = mouseWorld;
        }

        if (!IsDragging) return;

        // Tragem "inapoi" ca la prastie: directia de lansare e opusa directiei de tragere.
        Vector2 pull = dragStartWorld - mouseWorld;
        bool validDrag = pull.magnitude >= minDragDistance;

        if (validDrag)
        {
            float x = pull.x * (facingRight ? 1f : -1f);
            float angle = Mathf.Atan2(pull.y, x) * Mathf.Rad2Deg;
            float t = Mathf.Clamp01(pull.magnitude / maxDragDistance);
            SetAim(angle, Mathf.Lerp(minPower, maxPower, t));
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            IsDragging = false;
            if (validDrag) FireRequested?.Invoke();
        }
    }

    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private void UpdateVisuals()
    {
        if (armPivot != null)
        {
            // Bratul e desenat de-a lungul axei +X locale; spre stanga il rotim cu 180 - unghi.
            armPivot.localRotation = Quaternion.Euler(0f, 0f, facingRight ? Angle : 180f - Angle);
        }

        if (aimIndicator != null)
        {
            Vector3 start = LaunchPosition;
            Vector3 end = start + (Vector3)(Direction * (aimIndicatorMaxLength * Mathf.Max(0.15f, NormalizedPower)));
            aimIndicator.positionCount = 2;
            aimIndicator.useWorldSpace = true;
            aimIndicator.SetPosition(0, start);
            aimIndicator.SetPosition(1, end);
        }
    }

    /// <summary>Activeaza / ascunde indicatorul de directie (ex. cand zboara proiectilul).</summary>
    public void SetIndicatorVisible(bool visible)
    {
        if (aimIndicator != null) aimIndicator.enabled = visible;
    }

    private void LateUpdate()
    {
        // Bratul se poate misca (animatie etc.), deci tinem indicatorul lipit de punctul de lansare.
        if (aimIndicator != null && aimIndicator.enabled) UpdateVisuals();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 origin = LaunchPosition;
        float a = Application.isPlaying ? Angle : startAngle;
        float p = Application.isPlaying ? Power : startPower;
        float rad = a * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad) * (facingRight ? 1f : -1f), Mathf.Sin(rad));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + dir * (p / maxPower) * 2f);

        if (dragStartRadius > 0f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(origin, dragStartRadius);
        }
    }
#endif
}
