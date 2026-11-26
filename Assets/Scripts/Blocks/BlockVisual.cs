using UnityEngine;

[RequireComponent(typeof(PhysicsBlock), typeof(SpriteRenderer))]
public class BlockVisual : MonoBehaviour
{
    [SerializeField] private bool tileTexture = true;
    [SerializeField] private Sprite[] damageStages;
    [SerializeField] private Color crackColor = new(0.15f, 0.1f, 0.08f, 0.85f);
    [SerializeField, Range(0f, 1f)] private float damagedBrightness = 0.7f;

    public int Stage { get; private set; }

    private PhysicsBlock block;
    private SpriteRenderer sr;
    private SpriteRenderer cracks;
    private Color baseColor;

    private void Awake()
    {
        block = GetComponent<PhysicsBlock>();
        sr = GetComponent<SpriteRenderer>();
        baseColor = sr.color;

        if (tileTexture) ApplyTiling();
        CreateCrackLayer();
    }

    private void OnEnable() => block.Damaged += OnDamaged;
    private void OnDisable() => block.Damaged -= OnDamaged;

    private void ApplyTiling()
    {
        Vector3 scale = transform.localScale;
        var size = new Vector2(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        if (sr.sprite == null || (Mathf.Approximately(size.x, 1f) && Mathf.Approximately(size.y, 1f))) return;

        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = Vector2.Scale(sr.sprite.bounds.size, size);
        transform.localScale = new Vector3(Mathf.Sign(scale.x), Mathf.Sign(scale.y), scale.z);

        foreach (BoxCollider2D box in GetComponents<BoxCollider2D>())
        {
            box.size = Vector2.Scale(box.size, size);
            box.offset = Vector2.Scale(box.offset, size);
        }
    }

    private void CreateCrackLayer()
    {
        if (damageStages == null || damageStages.Length == 0 || damageStages[0] == null) return;

        Vector2 extent = sr.drawMode == SpriteDrawMode.Simple ? (Vector2)sr.sprite.bounds.size : sr.size;
        Vector2 crackSize = damageStages[0].bounds.size;

        var go = new GameObject("Cracks");
        go.transform.SetParent(transform, false);
        go.transform.localScale = new Vector3(extent.x / crackSize.x, extent.y / crackSize.y, 1f);

        cracks = go.AddComponent<SpriteRenderer>();
        cracks.sharedMaterial = sr.sharedMaterial;
        cracks.sortingLayerID = sr.sortingLayerID;
        cracks.sortingOrder = sr.sortingOrder + 1;
        cracks.color = crackColor;
        cracks.enabled = false;
    }

    private void OnDamaged(PhysicsBlock _)
    {
        float hp = block.HealthPercent;

        float k = Mathf.Lerp(1f, damagedBrightness, 1f - hp);
        sr.color = new Color(baseColor.r * k, baseColor.g * k, baseColor.b * k, baseColor.a);

        if (cracks == null) return;
        int n = damageStages.Length;
        int stage = 0;
        for (int i = 1; i <= n; i++)
            if (hp <= 1f - (float)i / (n + 1)) stage = i; // 66%, 33%

        if (stage == Stage) return;
        Stage = stage;
        cracks.sprite = stage > 0 ? damageStages[stage - 1] : null;
        cracks.enabled = stage > 0;
    }
}
