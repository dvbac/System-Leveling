using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class RotwoodBossBuilder
{
    public const string Source = "Assets/MyAssets/Monster/Boss/Rotwood";
    public const string Generated = Source + "/Generated";
    public const string PrefabPath = "Assets/Prefab/Boss/Rotwood.prefab";
    private static readonly string[] Names = { "Walk", "Root-Cast", "Charge-Windupt", "Charge-Loop", "Charge-Stagger", "Heart-Exposed", "Death" };

    [MenuItem("Tools/System Leveling/Rotwood/Build First Boss")]
    public static void Build()
    {
        if (Application.isPlaying || EditorSceneManager.GetActiveScene().path != "Assets/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene outside Play Mode first.");
        var spawner = Object.FindAnyObjectByType<EnemySpawner>();
        var result = Object.FindAnyObjectByType<GameOverController>(FindObjectsInactive.Include);
        if (spawner == null || result == null) throw new InvalidOperationException("Existing spawner and result HUD are required.");
        foreach (string name in Names)
            if (!File.Exists($"{Source}/{name}.png")) throw new FileNotFoundException(name);
        Directory.CreateDirectory(Generated);
        Directory.CreateDirectory("Assets/Prefab/Boss");
        Directory.CreateDirectory("Captures/RotwoodBoss");
        File.WriteAllText("Captures/RotwoodBoss/source-hashes-before.json", SourceSnapshot());
        string spawnerBefore = EditorJsonUtility.ToJson(spawner);
        File.WriteAllText("Captures/RotwoodBoss/spawner-before.json", spawnerBefore);

        var frames = new List<Sprite[]>();
        foreach (string name in Names) frames.Add(PrepareFrames(name));
        Sprite ring = Effect("WarningRing", false);
        Sprite roots = Effect("Roots", true);
        string dataPath = Generated + "/RotwoodStats.asset";
        var data = AssetDatabase.LoadAssetAtPath<EnemyScriptableObject>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyScriptableObject>();
            var serialized = new SerializedObject(data);
            serialized.FindProperty("moveSpeed").floatValue = 1.2f;
            serialized.FindProperty("maxHealth").floatValue = 1800f;
            serialized.FindProperty("damage").floatValue = 15f;
            serialized.FindProperty("experienceReward").intValue = 150;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(data, dataPath);
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Wolf.prefab");
            var instance = Object.Instantiate(template);
            try
            {
                instance.name = "Rotwood - The Rotten Sovereign";
                instance.transform.position = Vector3.zero;
                Object.DestroyImmediate(instance.GetComponent<EnemyAnimation>());
                Object.DestroyImmediate(instance.GetComponent<Enemy>());
                Object.DestroyImmediate(instance.GetComponent<Animator>());
                var stats = instance.GetComponent<EnemyStats>();
                stats.enemyData = data;
                var body = instance.GetComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0;
                var capsule = instance.GetComponent<CapsuleCollider2D>();
                capsule.size = new Vector2(1.25f,1.3f);
                capsule.offset = new Vector2(0,0.5f);
                instance.GetComponent<SpriteRenderer>().sprite = frames[0][0];
                instance.GetComponent<SpriteRenderer>().sortingOrder = 10;
                var audio = instance.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 0;
                audio.volume = 0.45f;
                var boss = instance.AddComponent<RotwoodBoss>();
                var serialized = new SerializedObject(boss);
                var motions = serialized.FindProperty("motions");
                motions.arraySize = Names.Length;
                for (int i = 0; i < Names.Length; i++)
                {
                    var sequence = motions.GetArrayElementAtIndex(i);
                    var sprites = sequence.FindPropertyRelative("frames");
                    sprites.arraySize = frames[i].Length;
                    for (int f = 0; f < frames[i].Length; f++) sprites.GetArrayElementAtIndex(f).objectReferenceValue = frames[i][f];
                    sequence.FindPropertyRelative("fps").floatValue = i == 6 ? 10 : i == 0 ? 15 : i == 2 ? 16 : i == 1 ? 12 : 8;
                    sequence.FindPropertyRelative("loop").boolValue = i == 0 || i == 3 || i == 5;
                    string sound = i == 2 ? "Charge-Windup" : Names[i];
                    sequence.FindPropertyRelative("sound").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Source}/{sound}.wav");
                }
                serialized.FindProperty("warningRing").objectReferenceValue = ring;
                serialized.FindProperty("rootSprite").objectReferenceValue = roots;
                serialized.FindProperty("wolfPrefab").objectReferenceValue = template;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                prefab = PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            }
            finally { Object.DestroyImmediate(instance); }
        }
        var encounter = Object.FindAnyObjectByType<RotwoodEncounter>(FindObjectsInactive.Include);
        if (encounter == null)
        {
            var go = new GameObject("Rotwood Boss Encounter - 15 Minutes");
            Undo.RegisterCreatedObjectUndo(go, "Create Rotwood encounter");
            encounter = go.AddComponent<RotwoodEncounter>();
            var serialized = new SerializedObject(encounter);
            serialized.FindProperty("bossPrefab").objectReferenceValue = prefab.GetComponent<RotwoodBoss>();
            serialized.FindProperty("spawnAtSeconds").floatValue = 900;
            var spawnConfig = new SerializedObject(spawner);
            var ground = spawnConfig.FindProperty("allowedGround").objectReferenceValue as Tilemap;
            serialized.FindProperty("groundTilemapName").stringValue = ground != null ? ground.name : "";
            serialized.FindProperty("obstacleLayers").intValue = spawnConfig.FindProperty("obstacleLayers").intValue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(encounter.gameObject.scene);
        EditorSceneManager.SaveScene(encounter.gameObject.scene);
        if (EditorJsonUtility.ToJson(spawner) != spawnerBefore) throw new InvalidOperationException("Spawner configuration changed.");
        if (SourceSnapshot() != File.ReadAllText("Captures/RotwoodBoss/source-hashes-before.json"))
            throw new InvalidOperationException("Source art/audio changed.");
        File.WriteAllText("Captures/RotwoodBoss/setup.json", "{\"result\":\"PASS\",\"spawnAtSeconds\":900,\"sourceUnchanged\":true,\"spawnerUnchanged\":true}");
        Debug.Log("ROTWOOD SETUP PASS: prefab + 7 animations + audio + health UI + 900s encounter; originals and spawner preserved.");
    }

    [MenuItem("Tools/System Leveling/Rotwood/Spawn Now (Play Mode)")]
    public static void SpawnNow()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var encounter = Object.FindAnyObjectByType<RotwoodEncounter>();
        if (encounter == null || !encounter.SpawnNow()) throw new InvalidOperationException("Cannot spawn: already spawned, no player, game over or no safe ground.");
    }

    private static Sprite[] PrepareFrames(string name)
    {
        string output = $"{Generated}/{name}.png";
        if (!File.Exists(output))
        {
            var texture = new Texture2D(2,2);
            texture.LoadImage(File.ReadAllBytes($"{Source}/{name}.png"));
            if (texture.width % 128 != 0 || texture.height % 128 != 0) throw new InvalidOperationException("Expected 128px cells: " + name);
            var cells = new List<Color[]>();
            for (int y = texture.height - 128; y >= 0; y -= 128)
                for (int x = 0; x < texture.width; x += 128)
                {
                    var pixels = texture.GetPixels(x,y,128,128);
                    if (pixels.Any(p => p.a > 0.05f)) cells.Add(pixels);
                }
            var sheet = new Texture2D(128*cells.Count,128,TextureFormat.RGBA32,false);
            for (int i = 0; i < cells.Count; i++) sheet.SetPixels(i*128,0,128,128,cells[i]);
            sheet.Apply();
            File.WriteAllBytes(output,sheet.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(sheet);
        }
        AssetDatabase.ImportAsset(output);
        var importer = (TextureImporter)AssetImporter.GetAtPath(output);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 48; // ~2.5x a 32px player, without changing collider scale.
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 4096;
        var textureAsset = AssetDatabase.LoadAssetAtPath<Texture2D>(output);
        int count = textureAsset.width / 128;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var rects = new SpriteRect[count];
        using (var hash = MD5.Create())
            for (int i = 0; i < count; i++)
                rects[i] = new SpriteRect
                {
                    name = name + "_" + i, rect = new Rect(i*128,0,128,128),
                    alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f,0.12f),
                    spriteID = new GUID(BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(output + i))).Replace("-", "").ToLowerInvariant())
                };
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name,r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(output).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_')+1))).ToArray();
    }

    private static Sprite Effect(string name, bool root)
    {
        string path = Generated + "/" + name + ".png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(64,64,TextureFormat.RGBA32,false);
            var pixels = new Color32[4096];
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    float radius = Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));
                    if (!root && radius > 27 && radius < 31) pixels[y*64+x] = Color.white;
                    if (root)
                    {
                        int stalk = (x+4)/12;
                        int height = 27 + (stalk*17)%28;
                        int lean = Mathf.RoundToInt(Mathf.Sin(y*0.12f+stalk)*3);
                        if (y < height && Mathf.Abs(x - (stalk*12+lean)) <= 3)
                            pixels[y*64+x] = x%3 == 0 ? new Color32(139,104,61,255) : new Color32(65,46,35,255);
                    }
                }
            texture.SetPixels32(pixels); texture.Apply();
            File.WriteAllBytes(path,texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
        }
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    public static string SourceSnapshot()
    {
        using (var sha = SHA256.Create())
            return string.Join("\n", Directory.GetFiles(Source).Where(p => p.EndsWith(".png") || p.EndsWith(".wav"))
                .OrderBy(p => p).Select(p => Path.GetFileName(p)+":"+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)))));
    }
}
