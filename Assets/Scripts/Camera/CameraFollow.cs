using UnityEngine;

/// <summary>
/// Camera Follow (Sprint 2) — camera urmareste proiectilul lansat si revine la catapulta.
///
/// Flux: Acasa (pozitia si zoom-ul de start)
///       -> proiectil lansat: camera il urmareste (cu putin "look-ahead" in directia zborului)
///          si poate face zoom (followOrthoSize)
///       -> proiectilul s-a terminat / a disparut: camera mai sta returnDelay secunde
///          pe locul impactului, apoi revine lin acasa.
///
/// Limite: implicit camera nu iese din cadrul de start (InitialView), ca sa nu se vada
/// marginea fundalului. Pentru niveluri mai late folositi Custom si setati customBounds.
/// Se pune pe Main Camera; catapulta e gasita automat daca nu e setata.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public enum BoundsMode { InitialView, Custom, None }

    [Tooltip("Gol = se cauta automat in scena.")]
    [SerializeField] private ProjectileLauncher launcher;

    [Header("Urmarire")]
    [SerializeField, Min(0f)] private float followSmoothTime = 0.2f;
    [Tooltip("Camera priveste putin inainte, in directia in care zboara proiectilul (secunde de zbor).")]
    [SerializeField, Min(0f)] private float lookAheadTime = 0.25f;
    [SerializeField, Min(0f)] private float maxLookAhead = 2f;
    [SerializeField] private Vector2 followOffset = new Vector2(0f, 0.5f);
    [Tooltip("Zoom-ul (orthographic size) cat timp urmareste proiectilul. 0 = pastreaza zoom-ul de start.")]
    [SerializeField, Min(0f)] private float followOrthoSize = 4.5f;
    [SerializeField, Min(0f)] private float zoomSmoothTime = 0.4f;

    [Header("Revenire")]
    [Tooltip("Cat sta camera pe locul impactului dupa ce proiectilul s-a terminat.")]
    [SerializeField, Min(0f)] private float returnDelay = 0.8f;
    [SerializeField, Min(0f)] private float returnSmoothTime = 0.45f;

    [Header("Limite")]
    [SerializeField] private BoundsMode boundsMode = BoundsMode.InitialView;
    [Tooltip("Zona (world) pe care o poate arata camera, folosita la BoundsMode.Custom.")]
    [SerializeField] private Rect customBounds = new Rect(-20f, -8f, 40f, 16f);

    private enum State { Home, Following, Holding, Returning }

    public bool IsFollowing => state == State.Following;
    /// <summary>true cand camera e (aproape) inapoi la pozitia de start.</summary>
    public bool IsHome => state == State.Home;

    private Camera cam;
    private State state = State.Home;
    private Projectile target;
    private Vector2 homePosition;
    private float homeSize;
    private Vector2 holdPosition;
    private float holdTimer;
    private Vector2 velocity;
    private float sizeVelocity;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        homePosition = transform.position;
        homeSize = cam.orthographicSize;
        holdPosition = homePosition;
        if (launcher == null) launcher = FindFirstObjectByType<ProjectileLauncher>();
    }

    private void OnEnable()
    {
        if (launcher == null) return;
        launcher.ProjectileLaunched += OnProjectileLaunched;
        launcher.ProjectileFinished += OnProjectileFinished;
    }

    private void OnDisable()
    {
        if (launcher == null) return;
        launcher.ProjectileLaunched -= OnProjectileLaunched;
        launcher.ProjectileFinished -= OnProjectileFinished;
    }

    private void OnProjectileLaunched(Projectile projectile)
    {
        target = projectile;
        state = State.Following;
    }

    private void OnProjectileFinished(Projectile projectile)
    {
        if (projectile == target) StartHolding();
    }

    private void StartHolding()
    {
        target = null;
        holdPosition = transform.position;
        holdTimer = returnDelay;
        state = State.Holding;
    }

    /// <summary>Trimite camera inapoi la catapulta imediat (fara pauza).</summary>
    public void ReturnHome()
    {
        target = null;
        state = State.Returning;
    }

    /// <summary>Muta camera instant acasa (ex. la restart).</summary>
    public void SnapHome()
    {
        target = null;
        state = State.Home;
        velocity = Vector2.zero;
        sizeVelocity = 0f;
        if (cam.orthographic) cam.orthographicSize = homeSize;
        SetPosition(Clamp(homePosition));
    }

    private void LateUpdate()
    {
        Vector2 desired;
        float desiredSize = homeSize;
        float smooth = returnSmoothTime;

        switch (state)
        {
            case State.Following:
                if (target == null) // distrus inainte sa fie "terminat" (ex. DestroyZone)
                {
                    StartHolding();
                    goto case State.Holding;
                }
                desired = (Vector2)target.transform.position + followOffset + LookAhead(target);
                if (followOrthoSize > 0f) desiredSize = followOrthoSize;
                smooth = followSmoothTime;
                break;

            case State.Holding:
                desired = holdPosition;
                if (followOrthoSize > 0f) desiredSize = followOrthoSize;
                holdTimer -= Time.deltaTime;
                if (holdTimer <= 0f) state = State.Returning;
                break;

            default: // Home / Returning
                desired = homePosition;
                break;
        }

        if (cam.orthographic)
            cam.orthographicSize = Mathf.SmoothDamp(cam.orthographicSize, desiredSize, ref sizeVelocity, zoomSmoothTime);

        desired = Clamp(desired);
        Vector2 next = Vector2.SmoothDamp(transform.position, desired, ref velocity, smooth);
        SetPosition(Clamp(next));

        if (state == State.Returning &&
            ((Vector2)transform.position - homePosition).sqrMagnitude < 0.0004f &&
            Mathf.Abs(cam.orthographicSize - homeSize) < 0.01f)
        {
            state = State.Home;
        }
    }

    private Vector2 LookAhead(Projectile projectile)
    {
        Rigidbody2D body = projectile.Body;
        if (body == null || !body.simulated) return Vector2.zero;
        return Vector2.ClampMagnitude(body.linearVelocity * lookAheadTime, maxLookAhead);
    }

    private void SetPosition(Vector2 p)
    {
        transform.position = new Vector3(p.x, p.y, transform.position.z);
    }

    /// <summary>Tine centrul camerei astfel incat cadrul sa nu iasa din limite.</summary>
    private Vector2 Clamp(Vector2 center)
    {
        if (boundsMode == BoundsMode.None || !cam.orthographic) return center;

        Rect bounds = boundsMode == BoundsMode.Custom ? customBounds : InitialViewRect();
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        center.x = ClampAxis(center.x, bounds.xMin + halfW, bounds.xMax - halfW);
        center.y = ClampAxis(center.y, bounds.yMin + halfH, bounds.yMax - halfH);
        return center;
    }

    private static float ClampAxis(float value, float min, float max) =>
        min > max ? (min + max) * 0.5f : Mathf.Clamp(value, min, max);

    private Rect InitialViewRect()
    {
        float halfH = homeSize;
        float halfW = halfH * cam.aspect;
        return new Rect(homePosition.x - halfW, homePosition.y - halfH, halfW * 2f, halfH * 2f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (boundsMode != BoundsMode.Custom) return;
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.8f);
        Gizmos.DrawWireCube(customBounds.center, customBounds.size);
    }
#endif
}
