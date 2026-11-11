using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// SCRUM-13 — Projectile Launching.
/// Genereaza proiectilul in catapulta si il lanseaza cu viteza calculata de CatapultAiming.
///
/// Flux: Load() -> proiectilul sta in punctul de lansare (urmareste bratul)
///       FireRequested (Space / eliberare mouse) -> TryLaunch()
///       proiectilul se termina (oprit / pierdut / distrus) -> reincarcare automata
///
/// Hook-uri pentru alte task-uri:
///   - Limited Projectiles (SCRUM-18): ascultati ProjectileLaunched si setati
///     LaunchingEnabled = false cand nu mai sunt proiectile.
///   - Basic Gameplay UI (SCRUM-19): ShotsFired, IsReady, evenimentele de mai jos.
///   - Projectile Physics (SCRUM-14): modificati prefab-ul proiectilului (masa, material etc.).
///     Lansarea seteaza viteza direct, deci masa nu schimba traiectoria.
/// </summary>
[RequireComponent(typeof(CatapultAiming))]
public class ProjectileLauncher : MonoBehaviour
{
    [Header("Proiectil")]
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("Parintele proiectilelor lansate in ierarhie (optional, doar pentru ordine).")]
    [SerializeField] private Transform projectilesParent;

    [Header("Reincarcare")]
    [SerializeField] private bool loadOnStart = true;
    [Tooltip("true = urmatorul proiectil apare abia dupa ce cel lansat s-a terminat. false = dupa reloadDelay.")]
    [SerializeField] private bool waitForProjectileToFinish = true;
    [SerializeField] private float reloadDelay = 0.5f;

    [Header("Feedback")]
    [Tooltip("Ascunde indicatorul de directie cand catapulta e goala.")]
    [SerializeField] private bool hideIndicatorWhenEmpty = true;

    /// <summary>Un proiectil nou a fost pus in catapulta.</summary>
    public event Action<Projectile> ProjectileLoaded;
    /// <summary>Un proiectil a fost lansat.</summary>
    public event Action<Projectile> ProjectileLaunched;
    /// <summary>Un proiectil lansat s-a terminat (oprit / pierdut / distrus).</summary>
    public event Action<Projectile> ProjectileFinished;

    /// <summary>Daca e false, catapulta nu mai incarca si nu mai lanseaza (ex. fara munitie, nivel terminat).</summary>
    public bool LaunchingEnabled
    {
        get => launchingEnabled;
        set
        {
            launchingEnabled = value;
            if (value && LoadedProjectile == null && ActiveProjectile == null && reloadRoutine == null && started)
                Load();
            UpdateIndicator();
        }
    }

    public Projectile LoadedProjectile { get; private set; }
    public Projectile ActiveProjectile { get; private set; }
    public int ShotsFired { get; private set; }
    public bool IsReady => launchingEnabled && LoadedProjectile != null;
    public CatapultAiming Aiming => aiming;

    private CatapultAiming aiming;
    private Collider2D[] ownColliders;
    private Coroutine reloadRoutine;
    private bool launchingEnabled = true;
    private bool started;
    private bool destroyed;

    private void Awake()
    {
        aiming = GetComponent<CatapultAiming>();
        ownColliders = GetComponentsInChildren<Collider2D>();
    }

    private void OnEnable()
    {
        aiming.FireRequested += HandleFireRequested;
    }

    private void OnDisable()
    {
        aiming.FireRequested -= HandleFireRequested;
        reloadRoutine = null; // corutinele se opresc oricum la dezactivare
    }

    private void Start()
    {
        started = true;
        if (loadOnStart) Load();
        else UpdateIndicator();
    }

    private void OnDestroy()
    {
        destroyed = true;
        if (ActiveProjectile != null) ActiveProjectile.Finished -= HandleProjectileFinished;
    }

    private void HandleFireRequested() => TryLaunch();

    /// <summary>Pune un proiectil nou in catapulta. Returneaza false daca nu se poate.</summary>
    public bool Load()
    {
        if (destroyed || !launchingEnabled || LoadedProjectile != null) return false;
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"{name}: ProjectileLauncher nu are setat projectilePrefab.", this);
            return false;
        }

        Transform holder = aiming.LaunchPoint != null ? aiming.LaunchPoint : transform;
        Projectile p = Instantiate(projectilePrefab, aiming.LaunchPosition, Quaternion.identity, holder);
        p.name = $"{projectilePrefab.name}_{ShotsFired + 1}";
        p.Hold();
        IgnoreOwnCollisions(p);

        LoadedProjectile = p;
        UpdateIndicator();
        ProjectileLoaded?.Invoke(p);
        return true;
    }

    /// <summary>Lanseaza proiectilul incarcat cu unghiul si puterea curente.</summary>
    public bool TryLaunch()
    {
        if (!IsReady) return false;

        Projectile p = LoadedProjectile;
        LoadedProjectile = null;

        p.transform.SetParent(projectilesParent, worldPositionStays: true);
        p.transform.position = aiming.LaunchPosition;
        p.Finished += HandleProjectileFinished;

        ActiveProjectile = p;
        ShotsFired++;
        p.Launch(aiming.LaunchVelocity);

        UpdateIndicator();
        ProjectileLaunched?.Invoke(p);

        if (!waitForProjectileToFinish) ScheduleReload();
        return true;
    }

    private void HandleProjectileFinished(Projectile p)
    {
        p.Finished -= HandleProjectileFinished;
        if (destroyed) return;

        if (ActiveProjectile == p) ActiveProjectile = null;
        ProjectileFinished?.Invoke(p);

        if (waitForProjectileToFinish) ScheduleReload();
    }

    private void ScheduleReload()
    {
        if (destroyed || !isActiveAndEnabled || reloadRoutine != null) return;
        reloadRoutine = StartCoroutine(ReloadAfterDelay());
    }

    private IEnumerator ReloadAfterDelay()
    {
        if (reloadDelay > 0f) yield return new WaitForSeconds(reloadDelay);
        reloadRoutine = null;
        Load();
    }

    private void IgnoreOwnCollisions(Projectile p)
    {
        Collider2D[] projectileColliders = p.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D own in ownColliders)
        {
            if (own == null) continue;
            foreach (Collider2D pc in projectileColliders)
                Physics2D.IgnoreCollision(own, pc, true);
        }
    }

    private void UpdateIndicator()
    {
        if (!hideIndicatorWhenEmpty || aiming == null) return;
        aiming.SetIndicatorVisible(IsReady);
    }
}
