using System.Collections;
using UnityEngine;

/// <summary>
/// Efect simplu de explozie (folosit de ExplosiveProjectile cand nu are un prefab de efect):
/// sprite-ul creste pana la raza exploziei si dispare treptat.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class ExplosionFlash : MonoBehaviour
{
    public void Play(float radius, float duration) => StartCoroutine(Run(radius, Mathf.Max(0.01f, duration)));

    private IEnumerator Run(float radius, float duration)
    {
        var sr = GetComponent<SpriteRenderer>();
        Color start = sr.color;
        float spriteSize = sr.sprite != null ? Mathf.Max(0.01f, sr.sprite.bounds.size.x) : 1f;
        float targetScale = radius * 2f / spriteSize; // diametrul = 2 * raza

        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float k = t / duration;
            float ease = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.one * (Mathf.Lerp(0.2f, 1f, ease) * targetScale);
            sr.color = new Color(start.r, start.g, start.b, start.a * (1f - k));
            yield return null;
        }
        Destroy(gameObject);
    }
}
