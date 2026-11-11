#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Genereaza prefab-urile pentru catapulta (SCRUM-12/13) si proiectil,
/// in acelasi stil ca BlockPrefabCreator (placeholder-e din forme simple).
///
/// Meniu:
///   CastleCrash/Create Catapult Prefabs        -> creeaza / actualizeaza prefab-urile
///   CastleCrash/Add Catapult To Open Scene     -> pune catapulta la "CatapultAnchor"
/// </summary>
public static class CatapultPrefabCreator
{
    private const string SquareSpritePath = "Assets/Sprites/WhiteSquare.png";
    private const string CircleSpritePath = "Assets/Sprites/WhiteCircle.png";
    private const string MaterialsDir = "Assets/Materials";
    private const string ProjectilesDir = "Assets/Prefabs/Projectiles";
    private const string CatapultDir = "Assets/Prefabs/Catapult";
    private const string ProjectilePrefabPath = ProjectilesDir + "/Projectile_Stone.prefab";
    private const string CatapultPrefabPath = CatapultDir + "/Catapult.prefab";
    private const string AimMaterialPath = MaterialsDir + "/AimIndicator.mat";

    private static readonly Color WoodDark = new Color(0.40f, 0.26f, 0.14f);
    private static readonly Color WoodLight = new Color(0.62f, 0.42f, 0.22f);
    private static readonly Color Iron = new Color(0.20f, 0.20f, 0.22f);
    private static readonly Color StoneColor = new Color(0.45f, 0.45f, 0.48f);

    [MenuItem("CastleCrash/Create Catapult Prefabs")]
    public static void CreatePrefabs()
    {
        EnsureTag("Projectile");
        EnsureFolder("Assets/Sprites");
        EnsureFolder(MaterialsDir);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(ProjectilesDir);
        EnsureFolder(CatapultDir);

        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
        if (square == null)
        {
            Debug.LogError($"CastleCrash: lipseste {SquareSpritePath}. Ruleaza intai CastleCrash/Create Block Prefabs.");
            return;
        }
        Sprite circle = GetOrCreateCircleSprite();

        GameObject projectilePrefab = CreateProjectilePrefab(circle);
        CreateCatapultPrefab(square, circle, projectilePrefab.GetComponent<Projectile>());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"CastleCrash: prefab-urile au fost create: {ProjectilePrefabPath}, {CatapultPrefabPath}");
    }

    [MenuItem("CastleCrash/Add Catapult To Open Scene")]
    public static void AddToScene()
    {
        var existing = Object.FindAnyObjectByType<CatapultAiming>();
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("CastleCrash: scena are deja o catapulta (selectata).", existing);
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(CatapultPrefabPath) == null) CreatePrefabs();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatapultPrefabPath);
        if (prefab == null) return;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        GameObject anchor = GameObject.Find("CatapultAnchor");
        if (anchor != null)
        {
            instance.transform.position = anchor.transform.position;
        }
        else
        {
            Debug.LogWarning("CastleCrash: nu am gasit 'CatapultAnchor'; catapulta a fost pusa in origine.");
        }

        Undo.RegisterCreatedObjectUndo(instance, "Add Catapult");
        Selection.activeGameObject = instance;
        EditorSceneManager.MarkSceneDirty(instance.scene);
    }

    // ---------------------------------------------------------------- Proiectil

    private static GameObject CreateProjectilePrefab(Sprite circle)
    {
        var go = new GameObject("Projectile_Stone");
        go.tag = "Projectile";
        go.transform.localScale = Vector3.one * 0.5f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = circle;
        sr.color = StoneColor;
        sr.sortingOrder = 3;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.sharedMaterial = GetOrCreatePhysicsMaterial("Projectile_Stone", 0.5f, 0.1f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 2f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        go.AddComponent<Projectile>();

        PrefabUtility.SaveAsPrefabAsset(go, ProjectilePrefabPath);
        Object.DestroyImmediate(go);
        return AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
    }

    // ---------------------------------------------------------------- Catapulta

    private static void CreateCatapultPrefab(Sprite square, Sprite circle, Projectile projectile)
    {
        var root = new GameObject("Catapult");

        // Corpul (static: collider fara Rigidbody2D)
        var body = CreateSprite("Base", root.transform, square, WoodDark, 1,
            new Vector2(0f, 0.3f), new Vector2(1.6f, 0.4f));
        body.AddComponent<BoxCollider2D>();

        CreateSprite("Wheel_L", root.transform, circle, Iron, 2, new Vector2(-0.55f, 0.15f), Vector2.one * 0.35f);
        CreateSprite("Wheel_R", root.transform, circle, Iron, 2, new Vector2(0.55f, 0.15f), Vector2.one * 0.35f);
        CreateSprite("Post", root.transform, square, WoodDark, 1, new Vector2(-0.1f, 0.65f), new Vector2(0.18f, 0.5f));

        // Bratul se roteste in jurul pivotului; e desenat pe +X local
        var pivot = new GameObject("ArmPivot").transform;
        pivot.SetParent(root.transform, false);
        pivot.localPosition = new Vector2(-0.1f, 0.85f);

        CreateSprite("Arm", pivot, square, WoodLight, 2, new Vector2(0.55f, 0f), new Vector2(1.3f, 0.14f));
        CreateSprite("Bucket", pivot, square, WoodDark, 2, new Vector2(1.15f, 0.12f), new Vector2(0.3f, 0.12f));

        var launchPoint = new GameObject("LaunchPoint").transform;
        launchPoint.SetParent(pivot, false);
        launchPoint.localPosition = new Vector2(1.15f, 0.4f);

        // Indicator directie / putere
        var indicatorGo = new GameObject("AimIndicator");
        indicatorGo.transform.SetParent(root.transform, false);
        var line = indicatorGo.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.startWidth = 0.08f;
        line.endWidth = 0.02f;
        line.numCapVertices = 2;
        line.sortingOrder = 5;
        line.sharedMaterial = GetOrCreateAimMaterial();
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.5f, 0.1f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.3f, 1f) });
        line.colorGradient = gradient;

        var aiming = root.AddComponent<CatapultAiming>();
        var so = new SerializedObject(aiming);
        so.FindProperty("armPivot").objectReferenceValue = pivot;
        so.FindProperty("launchPoint").objectReferenceValue = launchPoint;
        so.FindProperty("aimIndicator").objectReferenceValue = line;
        so.ApplyModifiedPropertiesWithoutUndo();

        var launcher = root.AddComponent<ProjectileLauncher>();
        var lso = new SerializedObject(launcher);
        lso.FindProperty("projectilePrefab").objectReferenceValue = projectile;
        lso.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, CatapultPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static GameObject CreateSprite(string name, Transform parent, Sprite sprite, Color color,
        int sortingOrder, Vector2 localPos, Vector2 scale)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        return go;
    }

    // ---------------------------------------------------------------- Asset-uri

    private static Material GetOrCreateAimMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(AimMaterialPath);
        if (mat != null) return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        mat = new Material(shader) { name = "AimIndicator" };
        AssetDatabase.CreateAsset(mat, AimMaterialPath);
        return mat;
    }

    private static PhysicsMaterial2D GetOrCreatePhysicsMaterial(string name, float friction, float bounciness)
    {
        string path = $"{MaterialsDir}/{name}.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D(name);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.friction = friction;
        mat.bounciness = bounciness;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Sprite GetOrCreateCircleSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        if (existing != null) return existing;

        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x + 0.5f - r, dy = y + 0.5f - r;
            float alpha = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy)); // margine anti-aliased
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(CircleSpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(CircleSpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(CircleSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size; // 64px = 1 unitate Unity (diametru)
        importer.filterMode = FilterMode.Bilinear;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static void EnsureTag(string tag)
    {
        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;

        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagManager.ApplyModifiedProperties();
    }
}
#endif
