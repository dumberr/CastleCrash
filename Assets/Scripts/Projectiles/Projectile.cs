using System;
using UnityEngine;

/// <summary>
/// Ciclul de viata al unui proiectil: tinut in catapulta -> lansat -> terminat.
/// (SCRUM-13). Fizica propriu-zisa a proiectilului (masa, material, damage etc.)
/// tine de SCRUM-14 — Projectile Physics si se poate adauga peste aceasta componenta.
///
/// Un proiectil e "terminat" cand:
///   - s-a oprit (viteza sub settleSpeed timp de settleTime secunde), sau
///   - a depasit maxLifetime, sau
///   - a cazut sub killHeight, sau
///   - a fost distrus (ex. de DestroyZone).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [Header("Terminare")]
    [SerializeField] private float settleSpeed = 0.3f;
    [SerializeField] private float settleTime = 1f;
    [SerializeField] private float maxLifetime = 12f;
    [Tooltip("Sub aceasta inaltime (Y world) proiectilul e considerat pierdut.")]
    [SerializeField] private float killHeight = -20f;

    [Header("Curatare")]
    [Tooltip("Proiectilul e distrus la cateva secunde dupa ce s-a terminat. Valoare negativa = ramane in scena.")]
    [SerializeField] private float destroyDelayAfterFinish = 3f;

    /// <summary>Proiectilul s-a terminat (oprit, pierdut sau distrus). Se apeleaza o singura data.</summary>
    public event Action<Projectile> Finished;

    /// <summary>Se apeleaza la lansare, cu viteza initiala.</summary>
    public event Action<Projectile, Vector2> Launched;

    public bool IsLaunched { get; private set; }
    public bool IsFinished { get; private set; }
    public Rigidbody2D Body => rb;

    private Rigidbody2D rb;
    private float launchTime;
    private float slowTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    /// <summary>Tine proiectilul nemiscat in catapulta (fara fizica).</summary>
    public void Hold()
    {
        IsLaunched = false;
        rb.simulated = false;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
    }

    /// <summary>Lanseaza proiectilul cu viteza data (independenta de masa).</summary>
    public void Launch(Vector2 velocity)
    {
        if (IsLaunched) return;

        IsLaunched = true;
        launchTime = Time.time;
        slowTimer = 0f;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.simulated = true;
        rb.WakeUp();
        rb.linearVelocity = velocity;

        Launched?.Invoke(this, velocity);
    }

    private void FixedUpdate()
    {
        if (!IsLaunched || IsFinished) return;

        if (transform.position.y < killHeight || Time.time - launchTime > maxLifetime)
        {
            Finish();
            return;
        }

        slowTimer = rb.linearVelocity.magnitude < settleSpeed ? slowTimer + Time.fixedDeltaTime : 0f;
        if (slowTimer >= settleTime) Finish();
    }

    /// <summary>Marcheaza proiectilul ca terminat (poate fi apelat si din exterior).</summary>
    public void Finish()
    {
        if (IsFinished) return;
        IsFinished = true;
        Finished?.Invoke(this);

        if (destroyDelayAfterFinish >= 0f)
            Destroy(gameObject, destroyDelayAfterFinish);
    }

    private void OnDestroy()
    {
        // Ex. distrus de DestroyZone inainte sa se opreasca.
        if (IsLaunched && !IsFinished)
        {
            IsFinished = true;
            Finished?.Invoke(this);
        }
    }
}
