
using System;
using UnityEngine;

[RequireComponent(typeof(ProjectileLauncher))]
public class LimitedProjectiles : MonoBehaviour
{
    [Header("Munitie")]
    [SerializeField, Min(1)] private int maxProjectiles = 3;

    private ProjectileLauncher launcher;

    public int MaxProjectiles => maxProjectiles;

    public int RemainingProjectiles =>
        Mathf.Max(0, maxProjectiles - launcher.ShotsFired);

    public event Action<int> AmmoChanged;

    private void Awake()
    {
        launcher = GetComponent<ProjectileLauncher>();
    }

    private void OnEnable()
    {
        launcher.ProjectileLaunched += OnProjectileLaunched;
    }

    private void OnDisable()
    {
        launcher.ProjectileLaunched -= OnProjectileLaunched;
    }

    private void OnProjectileLaunched(Projectile projectile)
    {

        AmmoChanged?.Invoke(RemainingProjectiles);

        if (RemainingProjectiles == 0)
        {
            launcher.LaunchingEnabled = false;
        }
    }
}
