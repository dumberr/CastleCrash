#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;


public static class BlockPrefabCreator
{
    private const string SpritePath = "Assets/Sprites/WhiteSquare.png";
    private const string MaterialsDir = "Assets/Materials";
    private const string PrefabsDir = "Assets/Prefabs/Blocks";

    [MenuItem("CastleCrash/Create Block Prefabs")]
    public static void Create()
    {
        EnsureTag("Block");
        EnsureFolder("Assets/Sprites");
        EnsureFolder(MaterialsDir);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(PrefabsDir);

        Sprite sprite = GetOrCreateSquareSprite();

   
        PhysicsMaterial2D baseMat = GetOrCreateMaterial("Block_Base", 0.6f, 0.05f);
        string basePath = $"{PrefabsDir}/Block_Base.prefab";
        GameObject baseGo = BuildBase(sprite, baseMat);
        PrefabUtility.SaveAsPrefabAsset(baseGo, basePath);
        Object.DestroyImmediate(baseGo);
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);

        //            nume          culoare                                   masa  frict  bounce health
        CreateVariant(basePrefab, "Block_Wood",  new Color(0.65f, 0.45f, 0.25f), 1.0f, 0.6f, 0.05f, 20f);
        CreateVariant(basePrefab, "Block_Stone", new Color(0.55f, 0.55f, 0.60f), 3.0f, 0.8f, 0.02f, 60f);
        CreateVariant(basePrefab, "Block_Glass", new Color(0.65f, 0.90f, 1.00f, 0.8f), 0.5f, 0.3f, 0.20f, 6f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("CastleCrash: prefab-urile blocurilor au fost create in " + PrefabsDir);
    }

    private static GameObject BuildBase(Sprite sprite, PhysicsMaterial2D mat)
    {
        var go = new GameObject("Block_Base");
        go.tag = "Block";

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;

        var col = go.AddComponent<BoxCollider2D>();
        col.sharedMaterial = mat;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.mass = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        go.AddComponent<PhysicsBlock>();
        return go;
    }

    private static void CreateVariant(GameObject basePrefab, string name, Color color,
        float mass, float friction, float bounciness, float health)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
        instance.name = name;

        instance.GetComponent<SpriteRenderer>().color = color;
        instance.GetComponent<Rigidbody2D>().mass = mass;
        instance.GetComponent<BoxCollider2D>().sharedMaterial =
            GetOrCreateMaterial(name, friction, bounciness);

        var so = new SerializedObject(instance.GetComponent<PhysicsBlock>());
        so.FindProperty("health").floatValue = health;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(instance, $"{PrefabsDir}/{name}.prefab");
        Object.DestroyImmediate(instance);
    }

    private static PhysicsMaterial2D GetOrCreateMaterial(string name, float friction, float bounciness)
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

    private static Sprite GetOrCreateSquareSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (existing != null) return existing;

        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(SpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size; // 32px = 1 unitate Unity
        importer.filterMode = FilterMode.Point;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
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