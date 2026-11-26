using System.IO;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

public static class BlockArtGenerator
{
    private const string Folder = "Assets/Art/Blocks";
    private const int Tile = 32;
    private const int CrackSize = 64;

    [MenuItem("CastleCrash/Generate Block Art")]
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        Save("Block_Wood", Wood(), Tile, true);
        Save("Block_Stone", Stone(), Tile, true);
        Save("Block_Glass", Glass(), Tile, true);
        Save("Cracks_1", Cracks(1), CrackSize, false);
        Save("Cracks_2", Cracks(2), CrackSize, false);
        AssetDatabase.Refresh();
        Debug.Log($"Block art generat in {Folder}");
    }

    private static Texture2D Wood()
    {
        var rng = new Random(1);
        var t = New(Tile);
        var baseC = new Color32(156, 104, 58, 255);
        const int plank = 8;

        for (int row = 0; row < Tile / plank; row++)
        {
            int seam = rng.Next(4, Tile - 4);
            int tone = rng.Next(-14, 14);
            for (int y = row * plank; y < (row + 1) * plank; y++)
            {
                float phase = (float)rng.NextDouble() * 6.28f;
                for (int x = 0; x < Tile; x++)
                {
                    float grain = Mathf.Sin(x * 6.28f / Tile * 2f + phase) * 10f + rng.Next(-5, 6);
                    Color32 c = Shade(baseC, tone + (int)grain);
                    if (y == row * plank) c = Shade(baseC, -55); // rost
                    else if (y == row * plank + 1) c = Shade(baseC, 22);
                    else if (x == seam) c = Shade(baseC, -45);
                    t.SetPixel(x, y, c);
                }
            }
        }
        t.Apply();
        return t;
    }

    private static Texture2D Stone()
    {
        var rng = new Random(2);
        var t = New(Tile);
        var baseC = new Color32(150, 150, 160, 255);
        var mortar = new Color32(88, 86, 94, 255);
        const int h = 8, w = 16;

        var tones = new int[Tile / h, Tile / w];
        for (int r = 0; r < Tile / h; r++)
            for (int b = 0; b < Tile / w; b++)
                tones[r, b] = rng.Next(-16, 16);

        for (int y = 0; y < Tile; y++)
        {
            int r = y / h;
            int offset = r % 2 == 0 ? 0 : w / 2;
            for (int x = 0; x < Tile; x++)
            {
                int bx = (x + offset) % Tile;
                Color32 c;
                if (y % h == 0 || bx % w == 0) c = mortar;
                else
                {
                    int tone = tones[r, bx / w] + rng.Next(-7, 8);
                    if (y % h == h - 1) tone += 18;
                    else if (y % h == 1) tone -= 14;
                    c = Shade(baseC, tone);
                }
                t.SetPixel(x, y, c);
            }
        }
        t.Apply();
        return t;
    }

    private static Texture2D Glass()
    {
        var t = New(Tile);
        var baseC = new Color32(170, 220, 240, 140);
        var frame = new Color32(225, 245, 255, 210);
        var shine = new Color32(255, 255, 255, 190);

        for (int y = 0; y < Tile; y++)
            for (int x = 0; x < Tile; x++)
            {
                int d = (x + y) % Tile;
                Color32 c = baseC;
                if (x == 0 || y == 0) c = frame;
                else if (d == 6 || d == 7 || d == 12) c = shine;
                t.SetPixel(x, y, c);
            }
        t.Apply();
        return t;
    }

    private static Texture2D Cracks(int stage)
    {
        var t = New(CrackSize);
        var clear = new Color32(0, 0, 0, 0);
        for (int y = 0; y < CrackSize; y++)
            for (int x = 0; x < CrackSize; x++)
                t.SetPixel(x, y, clear);

        var center = new Vector2(CrackSize * 0.45f, CrackSize * 0.55f);
        var rng = new Random(11);
        for (int i = 0; i < 3; i++)
            Crack(t, rng, center, i * 2.1f + 0.4f, 14, false);

        if (stage >= 2)
        {
            var rng2 = new Random(23);
            for (int i = 0; i < 6; i++)
                Crack(t, rng2, center, i * 1.047f + 0.2f, 34, true);
        }
        t.Apply();
        return t;
    }

    private static void Crack(Texture2D t, Random rng, Vector2 pos, float angle, int length, bool branch)
    {
        var white = new Color32(255, 255, 255, 255);
        for (int s = 0; s < length; s++)
        {
            angle += (float)(rng.NextDouble() - 0.5) * 0.45f;
            pos += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            int x = Mathf.RoundToInt(pos.x), y = Mathf.RoundToInt(pos.y);
            if (x < 1 || y < 1 || x >= t.width - 1 || y >= t.height - 1) return;
            t.SetPixel(x, y, white);
            if (branch && s > 6 && rng.NextDouble() < 0.08)
                Crack(t, rng, pos, angle + (rng.NextDouble() < 0.5 ? 0.6f : -0.6f), length / 3, false);
        }
    }

    private static Texture2D New(int size) =>
        new(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };

    private static Color32 Shade(Color32 c, int delta) => new(
        (byte)Mathf.Clamp(c.r + delta, 0, 255),
        (byte)Mathf.Clamp(c.g + delta, 0, 255),
        (byte)Mathf.Clamp(c.b + delta, 0, 255),
        c.a);

    private static void Save(string name, Texture2D tex, int ppu, bool repeat)
    {
        string path = $"{Folder}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect; // pt. tiling
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
    }
}
