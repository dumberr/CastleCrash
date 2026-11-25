using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Projectile Types (Sprint 2) — butoiul exploziv.
///
/// Explodeaza la primul impact dupa lansare (sau mai devreme, daca jucatorul apasa
/// Space in zbor). Explozia:
///   - face damage blocurilor (PhysicsBlock) din raza, scazand cu distanta;
///   - impinge toate corpurile dinamice din raza (Rigidbody2D);
///   - afiseaza un efect (prefab optional, altfel un cerc care se extinde).
/// Dupa explozie proiectilul e ascuns si marcat ca terminat.
/// </summary>
[RequireComponent(typeof(Projectile))]
public class ExplosiveProjectile : MonoBehaviour
{
    [Header("Explozie")]
    [SerializeField, Min(0.1f)] private float radius = 2.5f;
    [Tooltip("Damage-ul in centrul exploziei. La marginea razei scade spre 0.")]
    [SerializeField, Min(0f)] private float maxDamage = 60f;
    [Tooltip("Impulsul aplicat corpurilor din centrul exploziei.")]
    [SerializeField, Min(0f)] private float maxForce = 12f;
    [Tooltip("Impulsul primeste si o componenta in sus, ca blocurile sa zboare spectaculos.")]
    [SerializeField, Range(0f, 1f)] private float upwardsModifier = 0.3f;
    [SerializeField] private LayerMask affectedLayers = ~0;

    [Header("Declansare")]
    [SerializeField] private bool explodeOnImpact = true;
    [Tooltip("Space in timpul zborului detoneaza butoiul.")]
    [SerializeField] private bool detonateOnInput = true;
    [Tooltip("Ignoram input-ul imediat dupa lansare (acelasi Space care a lansat).")]
    [SerializeField] private float inputGracePeriod = 0.2f;

    [Header("Efect")]
    [Tooltip("Optional. Daca lipseste, se creeaza un cerc simplu care se extinde si dispare.")]
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private Sprite fallbackEffectSprite;
    [SerializeField] private Color fallbackEffectColor = new Color(1f, 0.55f, 0.1f, 0.85f);
    [SerializeField] private float fallbackEffectDuration = 0.35f;

    public bool HasExploded { get; private set; }

    private Projectile projectile;
    private float launchTime;
    private readonly List<Collider2D> hits = new List<Collider2D>(64);

    private void Awake()
    {
        projectile = GetComponent<Projectile>();
    }

    private void OnEnable() => projectile.Launched += OnLaunched;
    private void OnDisable() => projectile.Launched -= OnLaunched;

    private void OnLaunched(Projectile p, Vector2 velocity) => launchTime = Time.time;

    private void Update()
    {
        if (!detonateOnInput || HasExploded || !projectile.IsLaunched || projectile.IsFinished) return;
        if (Time.timeScale == 0f || Time.time - launchTime < inputGracePeriod) return;

        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) Explode();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (explodeOnImpact && projectile.IsLaunched && !projectile.IsFinished) Explode();
    }

    /// <summary>Declanseaza explozia (o singura data).</summary>
    public void Explode()
    {
        if (HasExploded) return;
        HasExploded = true;

        Vector2 center = transform.position;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(affectedLayers);
        Physics2D.OverlapCircle(center, radius, filter, hits);

        var damaged = new HashSet<PhysicsBlock>();
        var pushed = new HashSet<Rigidbody2D>();
        Rigidbody2D ownBody = projectile.Body;

        foreach (Collider2D col in hits)
        {
            if (col == null) continue;

            Rigidbody2D body = col.attachedRigidbody;
            if (body == ownBody) continue;

            Vector2 closest = col.ClosestPoint(center);
            float distance = Vector2.Distance(center, closest);
            float falloff = 1f - Mathf.Clamp01(distance / radius);
            if (falloff <= 0f) continue;

            if (body != null && body.bodyType == RigidbodyType2D.Dynamic && pushed.Add(body))
            {
                Vector2 dir = (Vector2)body.worldCenterOfMass - center;
                if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
                dir = (dir.normalized + Vector2.up * upwardsModifier).normalized;
                body.AddForce(dir * (maxForce * falloff), ForceMode2D.Impulse);
            }

            PhysicsBlock block = col.GetComponentInParent<PhysicsBlock>();
            if (block != null && damaged.Add(block))
                block.TakeDamage(maxDamage * falloff);
        }

        SpawnEffect(center);
        HideSelf();
        projectile.Finish();
    }

    private void HideSelf()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;

        Rigidbody2D body = projectile.Body;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.simulated = false;
    }

    private void SpawnEffect(Vector2 center)
    {
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, center, Quaternion.identity);
            return;
        }
        if (fallbackEffectSprite == null) return;

        var go = new GameObject("Explosion_FX");
        go.transform.position = center;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = fallbackEffectSprite;
        sr.color = fallbackEffectColor;
        sr.sortingOrder = 10;
        go.AddComponent<ExplosionFlash>().Play(radius, fallbackEffectDuration);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
#endif
}
