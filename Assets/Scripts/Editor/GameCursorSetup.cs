using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class GameCursorSetup
{
    public const string SourcePath = "Assets/MyAssets/Other/Cursor/Cursor.png";
    public const string CursorPath = "Assets/MyAssets/Other/Cursor/GameCursor.png";
    private const string Output = "Captures/CursorReset";

    [MenuItem("Tools/System Leveling/Reset Game Cursor")]
    public static void Reset()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Reset cursor outside Play Mode.");
        Directory.CreateDirectory(Output);
        if (!File.Exists(Output + "/settings-before.json"))
            File.WriteAllText(Output + "/settings-before.json", JsonUtility.ToJson(new Previous
            { texture = AssetDatabase.GetAssetPath(PlayerSettings.defaultCursor), hotspot = PlayerSettings.cursorHotspot }, true));
        string original = Hash(SourcePath);
        Scene scene = SceneManager.GetActiveScene(); bool dirty = scene.isDirty;
        var source = new Texture2D(2, 2);
        var canvas = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        try
        {
            if (!source.LoadImage(File.ReadAllBytes(SourcePath)) || source.width != source.height || (source.width != 16 && source.width != 32))
                throw new InvalidOperationException("Cursor artwork must be a square 16x16 or 32x32 PNG.");
            int sample = source.width / 16;
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++) canvas.SetPixel(x, y, source.GetPixel(x * sample, y * sample));
            canvas.Apply();
            File.WriteAllBytes(CursorPath, canvas.EncodeToPNG());
        }
        finally { Object.DestroyImmediate(source); Object.DestroyImmediate(canvas); }
        AssetDatabase.ImportAsset(CursorPath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(CursorPath);
        importer.textureType = TextureImporterType.Cursor;
        importer.spriteImportMode = SpriteImportMode.None;
        importer.isReadable = true; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
        var defaults = importer.GetDefaultPlatformTextureSettings();
        defaults.format = TextureImporterFormat.RGBA32; defaults.maxTextureSize = 32;
        defaults.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(defaults);
        var windows = importer.GetPlatformTextureSettings("Standalone");
        windows.overridden = true; windows.format = TextureImporterFormat.RGBA32; windows.maxTextureSize = 32;
        windows.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(windows);
        importer.SaveAndReimport();
        PlayerSettings.defaultCursor = AssetDatabase.LoadAssetAtPath<Texture2D>(CursorPath);
        PlayerSettings.cursorHotspot = new Vector2(8, 8);
        AssetDatabase.SaveAssets();
        Cursor.SetCursor(PlayerSettings.defaultCursor, PlayerSettings.cursorHotspot, CursorMode.Auto);
        if (Hash(SourcePath) != original || SceneManager.GetActiveScene() != scene || scene.isDirty != dirty)
            throw new InvalidOperationException("Cursor reset must preserve original artwork and live scene edits.");
        Validate();
        Selection.activeObject = PlayerSettings.defaultCursor;
        Debug.Log("CURSOR RESET: 16x16 RGBA32 cursor, centered hotspot, point filtering; original artwork and scenes untouched.");
    }

    [MenuItem("Tools/System Leveling/Validate Game Cursor")]
    public static void Validate()
    {
        Texture2D cursor = PlayerSettings.defaultCursor;
        Require(cursor != null && AssetDatabase.GetAssetPath(cursor) == CursorPath, "Fresh cursor asset is assigned");
        var importer = (TextureImporter)AssetImporter.GetAtPath(CursorPath);
        Require(importer.textureType == TextureImporterType.Cursor, "Imported as Cursor, not a sliced Sprite");
        Require(cursor.width == 16 && cursor.height == 16 && cursor.format == TextureFormat.RGBA32, "Exact 16x16 RGBA32 texture");
        Require(PlayerSettings.cursorHotspot == new Vector2(8, 8), "Hotspot aligns with the artwork center");
        Require(cursor.filterMode == FilterMode.Point && !importer.mipmapEnabled && importer.isReadable, "Point sampling, no mipmaps, readable pixels");
        Require(importer.GetPlatformTextureSettings("Standalone").format == TextureImporterFormat.RGBA32, "Standalone keeps uncompressed RGBA32");
        var source = new Texture2D(2, 2); var native = new Texture2D(2, 2);
        try
        {
            source.LoadImage(File.ReadAllBytes(SourcePath)); native.LoadImage(File.ReadAllBytes(CursorPath));
            int sample = source.width / 16, visible = 0;
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    Color32 expected = source.GetPixel(x * sample, y * sample);
                    Color32 pixel = native.GetPixel(x, y);
                    Require(pixel.Equals(expected), "Cursor pixels match centered original artwork");
                    if (pixel.a > 0) visible++;
                }
            Require(visible > 0, "Artwork is visible, not an empty cursor");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "/validation.json", JsonUtility.ToJson(new Report
            { result = "PASS", pixelChecks = 256, textureSize = 16, artworkSize = 16, sourceSHA256 = Hash(SourcePath),
                nativeSHA256 = Hash(CursorPath), note = "Asset/import/hotspot verified; OS hardware cursor appearance still needs Game View confirmation." }, true));
        }
        finally { Object.DestroyImmediate(source); Object.DestroyImmediate(native); }
    }

    private static string Hash(string path) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", ""); }
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    [Serializable] private class Previous { public string texture; public Vector2 hotspot; }
    [Serializable] private class Report { public string result, sourceSHA256, nativeSHA256, note; public int pixelChecks, textureSize, artworkSize; }
}
