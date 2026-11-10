using UnityEngine;


[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PhysicsBlock : MonoBehaviour
{
    [Header("Durabilitate")]
    [SerializeField] private float health = 20f;
    [Tooltip("Sub aceasta viteza relativa, impactul e ignorat (blocurile care stau unul peste altul nu se strica).")]
    [SerializeField] private float minImpactSpeed = 2f;
    [SerializeField] private float damageMultiplier = 1f;

    [Header("Efecte (optional)")]
    [SerializeField] private GameObject destroyEffect;

    private float currentHealth;

    private void Awake()
    {
        currentHealth = health;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < minImpactSpeed) return;

       
        float otherMass = collision.rigidbody != null ? collision.rigidbody.mass : 1f;
        TakeDamage(impactSpeed * otherMass * damageMultiplier);
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0f) Break();
    }

    private void Break()
    {
        if (destroyEffect != null)
            Instantiate(destroyEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}