using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class FrostfangBossBuilder
{
    public const string Source = "Assets/MyAssets/Monster/Boss/Frostfang";
    public const string Generated = Source + "/Generated";
    public const string PrefabPath = "Assets/Prefab/Boss/Frostfang.prefab";
    public const string Output = "Captures/FrostfangBoss";
    private static readonly string[] Names = { "Idle", "Run", "Pounce", "Breath", "Pillars", "Howl", "Death" };
    private static readonly int[] Counts = { 8, 8, 12, 16, 10, 16, 12 };
    private static readonly string[] Patterns = { "idle.png", "Move.png", "*Stop*.png", "*Spread*.png", "*Shift*.png", "*Widen*.png", "defeat.png" };

    [MenuItem("Tools/System Leveling/Frostfang/Build Snow Boss")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build Frostfang outside Play Mode.");
        Scene scene = SceneManager.GetSceneByPath(LoadoutLauncher.SnowScene);
        if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save your SnowScene changes first.");
        foreach (var pattern in Patterns)
            if (Directory.GetFiles(Source, pattern).Length != 1) throw new InvalidOperationException("Expected one source sheet: " + pattern);
        foreach (string name in new[] { "PounceImpact", "FrostBreath", "IcePillar", "Blizzard" })
            if (!File.Exists("Captures/FrostfangFX/Animations/" + name + ".png")) throw new FileNotFoundException(name);
        Directory.CreateDirectory(Generated); Directory.CreateDirectory(Output); Directory.CreateDirectory("Assets/Prefab/Boss");
        string before = SourceSnapshot();
        string backup = Output + "/Before-SnowScene.unity";
        if (!File.Exists(backup)) File.Copy(LoadoutLauncher.SnowScene, backup);
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(LoadoutLauncher.SnowScene, OpenSceneMode.Additive);
        try
        {
            var spawner = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemySpawner>(true)).Single();
            string spawnBefore = EditorJsonUtility.ToJson(spawner);
            Sprite[][] frames = new Sprite[Names.Length][];
            for (int i = 0; i < Names.Length; i++)
            {
                string path = Generated + "/" + Names[i] + ".png";
                Normalize(Directory.GetFiles(Source, Patterns[i]).Single(), path, Counts[i]);
                frames[i] = Import(path, 192, 160, Counts[i], 48, new Vector2(0.5f, 0.125f));
            }
            string[] fxNames = { "PounceImpact", "FrostBreath", "IcePillar", "Blizzard" };
            int[] fxCounts = { 8, 8, 12, 8 }, widths = { 128, 256, 64, 128 };
            Sprite[][] fx = new Sprite[4][];
            for (int i = 0; i < 4; i++)
            {
                string path = Generated + "/" + fxNames[i] + ".png";
                if (!File.Exists(path)) File.Copy("Captures/FrostfangFX/Animations/" + fxNames[i] + ".png", path);
                fx[i] = Import(path, widths[i], 128, fxCounts[i], 48,
                    i == 1 ? new Vector2(8f / 256, 0.5f) : i == 2 ? new Vector2(0.5f, 0.08f) : Vector2.one * 0.5f);
            }
            Sprite ring = Warning(false), cone = Warning(true);
            string dataPath = Generated + "/FrostfangStats.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyScriptableObject>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyScriptableObject>();
                var so = new SerializedObject(data);
                so.FindProperty("moveSpeed").floatValue = 1.6f; so.FindProperty("maxHealth").floatValue = 2200;
                so.FindProperty("damage").floatValue = 15; so.FindProperty("experienceReward").floatValue = 150;
                so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(data, dataPath);
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                var instance = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Snow/SnowWolf.prefab"));
                try
                {
                    instance.name = "Frostfang Alpha"; instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); instance.transform.localScale = Vector3.one;
                    Object.DestroyImmediate(instance.GetComponent<Enemy>()); Object.DestroyImmediate(instance.GetComponent<EnemyAnimation>());
                    Object.DestroyImmediate(instance.GetComponent<Animator>());
                    var stats = instance.GetComponent<EnemyStats>(); stats.enemyData = data;
                    var so = new SerializedObject(stats); so.FindProperty("soulReward").intValue = 150; so.ApplyModifiedPropertiesWithoutUndo();
                    var body = instance.GetComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
                    var collider = instance.GetComponent<CapsuleCollider2D>(); collider.size = new Vector2(1.35f, 1.4f); collider.offset = new Vector2(0, 0.6f);
                    var visual = instance.GetComponent<SpriteRenderer>(); visual.sprite = frames[0][0]; visual.sortingOrder = 10;
                    var boss = instance.AddComponent<FrostfangBoss>(); so = new SerializedObject(boss);
                    var motions = so.FindProperty("motions"); motions.arraySize = Names.Length;
                    for (int i = 0; i < Names.Length; i++)
                    {
                        var sequence = motions.GetArrayElementAtIndex(i); Assign(sequence.FindPropertyRelative("frames"), frames[i]);
                        sequence.FindPropertyRelative("fps").floatValue = i == 1 || i == 4 ? 10 : 8;
                        sequence.FindPropertyRelative("loop").boolValue = i < 2;
                    }
                    so.FindProperty("warningRing").objectReferenceValue = ring; so.FindProperty("warningCone").objectReferenceValue = cone;
                    string[] fields = { "impactFrames", "breathFrames", "pillarFrames", "blizzardFrames" };
                    for (int i = 0; i < 4; i++) Assign(so.FindProperty(fields[i]), fx[i]);
                    so.ApplyModifiedPropertiesWithoutUndo(); prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
                }
                finally { Object.DestroyImmediate(instance); }
            }
            var encounter = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<FrostfangEncounter>(true)).SingleOrDefault();
            if (encounter == null)
            {
                var go = new GameObject("Frostfang Alpha Encounter - 17 Minutes"); SceneManager.MoveGameObjectToScene(go, scene);
                encounter = go.AddComponent<FrostfangEncounter>();
                var so = new SerializedObject(encounter); so.FindProperty("bossPrefab").objectReferenceValue = prefab.GetComponent<FrostfangBoss>();
                var config = new SerializedObject(spawner);
                so.FindProperty("obstacleLayers").intValue = config.FindProperty("obstacleLayers").intValue;
                var ground = config.FindProperty("allowedGround").objectReferenceValue as Tilemap;
                if (ground == null)
                {
                    var map = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MapController>(true)).Single();
                    ground = map.terrainChunks[0].GetComponentInChildren<Tilemap>(true);
                }
                if (ground == null) throw new InvalidOperationException("Snow ground Tilemap missing.");
                so.FindProperty("groundTilemapName").stringValue = ground.name; so.ApplyModifiedPropertiesWithoutUndo();
            }
            var timing = new SerializedObject(encounter);
            timing.FindProperty("spawnAtSeconds").floatValue = 1020;
            timing.ApplyModifiedPropertiesWithoutUndo();
            if (EditorJsonUtility.ToJson(spawner) != spawnBefore || SourceSnapshot() != before) throw new InvalidOperationException("Source/spawner changed.");
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save SnowScene.");
            File.WriteAllText(Output + "/source-hashes.txt", before);
            File.WriteAllText(Output + "/setup.json", "{\"result\":\"PASS\",\"spawnAtSeconds\":1020,\"sourceUnchanged\":true,\"spawnerUnchanged\":true}");
            Debug.Log("FROSTFANG SETUP PASS: 7 motions, 4 FX, prefab/data and 1020s snow encounter. Original art/spawner preserved.");
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
    }

    [MenuItem("Tools/System Leveling/Frostfang/Spawn Now (Play Mode)")]
    public static void SpawnNow()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode on SnowScene first.");
        var encounter = Object.FindAnyObjectByType<FrostfangEncounter>();
        if (encounter == null || !encounter.SpawnNow()) throw new InvalidOperationException("Cannot spawn: wrong map, already spawned, no player, game over or no safe ground.");
    }

    private static void Assign(SerializedProperty property, Sprite[] frames)
    { property.arraySize = frames.Length; for (int i = 0; i < frames.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i]; }

    private static void Normalize(string input, string output, int count)
    {
        if (File.Exists(output)) return;
        var source = new Texture2D(2, 2); var sheet = new Texture2D(192 * count, 160, TextureFormat.RGBA32, false);
        try
        {
            source.LoadImage(File.ReadAllBytes(input));
            if (source.width % count != 0) throw new InvalidOperationException("Invalid frame count: " + input);
            sheet.SetPixels32(new Color32[sheet.width * sheet.height]);
            int width = source.width / count, baseline = -1;
            for (int i = 0; i < count; i++)
            {
                Color[] pixels = source.GetPixels(i * width, 0, width, source.height);
                int left = width, bottom = source.height, right = 0, top = 0;
                for (int y = 0; y < source.height; y++) for (int x = 0; x < width; x++)
                    if (pixels[y * width + x].a >= 0.5f) { left = Mathf.Min(left, x); right = Mathf.Max(right, x + 1); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y + 1); }
                if (right <= left) throw new InvalidOperationException("Empty source frame: " + input);
                if (baseline < 0) baseline = bottom;
                int w = right - left, h = top - bottom, destY = 20 + bottom - baseline;
                if (w > 188 || destY < 1 || destY + h >= 160) throw new InvalidOperationException("Frame would clip: " + input);
                sheet.SetPixels(i * 192 + (192 - w) / 2, destY, w, h, source.GetPixels(i * width + left, bottom, w, h));
            }
            sheet.Apply(); File.WriteAllBytes(output, sheet.EncodeToPNG());
        }
        finally { Object.DestroyImmediate(source); Object.DestroyImmediate(sheet); }
    }

    private static Sprite[] Import(string path, int width, int height, int count, float ppu, Vector2 pivot)
    {
        AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = ppu; importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true; importer.maxTextureSize = 4096; importer.npotScale = TextureImporterNPOTScale.None;
        importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
        if (sourceWidth != width * count || sourceHeight != height) throw new InvalidOperationException("Wrong sheet dimensions: " + path);
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var rects = new SpriteRect[count]; string name = Path.GetFileNameWithoutExtension(path);
        using (var hash = MD5.Create()) for (int i = 0; i < count; i++)
            rects[i] = new SpriteRect { name = name + "_" + i, rect = new Rect(i * width, 0, width, height),
                alignment = SpriteAlignment.Custom, pivot = pivot,
                spriteID = new GUID(BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(path + i))).Replace("-", "").ToLowerInvariant()) };
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply(); importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    private static Sprite Warning(bool cone)
    {
        string path = Generated + (cone ? "/WarningCone.png" : "/WarningRing.png");
        if (!File.Exists(path))
        {
            int height = cone ? 128 : 256;
            var texture = new Texture2D(256, height, TextureFormat.RGBA32, false); var pixels = new Color32[256 * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < 256; x++)
            {
                float across = Mathf.Abs(y - 63.5f);
                float radius = new Vector2((x - 127.5f) / 128, (y - 127.5f) / 128).magnitude;
                bool edge = cone ? x >= 1 && x <= 255 && across <= x * 0.25f && (across >= x * 0.25f - 2 || x >= 252) : radius >= 0.94f && radius <= 1;
                bool fill = cone ? x >= 1 && across <= x * 0.25f : radius <= 1;
                if (fill) pixels[y * 256 + x] = new Color32(255, 255, 255, edge ? (byte)255 : (byte)28);
            }
            texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
        }
        // Ring uses a square world footprint; cone width is one world unit before scaling.
        var sprites = Import(path, 256, cone ? 128 : 256, 1, 256, cone ? new Vector2(0, 0.5f) : Vector2.one * 0.5f);
        return sprites[0];
    }

    public static string SourceSnapshot()
    {
        using (var sha = SHA256.Create()) return string.Join("\n", Directory.GetFiles(Source, "*.png").OrderBy(p => p)
            .Select(p => Path.GetFileName(p) + ":" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)))));
    }
}
