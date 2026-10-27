using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ReferenceEnemyBuilder
{
    private const string Root = "Assets/MyAssets/Monster/ReferenceAnimated";
    private const string Prefabs = "Assets/Prefab/ReferenceEnemies";
    private const string Data = "Assets/Scriptable Object/Enemy/ReferenceEnemies";
    private static readonly string[] Monsters = { "Slime", "Skeleton", "Wolf", "Spider" };
    private static readonly string[] States = { "Idle", "Move", "Hit", "Die" };
    private static readonly int[] Counts = { 8, 8, 4, 8 };
    private static readonly float[] Fps = { 8, 12, 12, 10 };
    private static readonly float[] Speeds = { 2f, 1.8f, 3.6f, 3f };
    private static readonly float[] Health = { 10f, 25f, 18f, 12f };
    private static readonly float[] Damage = { 4f, 6f, 7f, 3f };

    [MenuItem("Tools/System Leveling/Build Reference Enemies")]
    public static void Build()
    {
        if (Application.isPlaying)
            throw new InvalidOperationException("Build outside Play Mode.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open SampleScene before wiring its spawner.");
        var spawners = Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include);
        if (spawners.Length == 0)
            throw new InvalidOperationException("No EnemySpawner in the loaded scene.");
        foreach (string name in Monsters)
            foreach (string state in States)
                if (!File.Exists($"{Root}/{name}/{state}.png"))
                    throw new FileNotFoundException($"Generate {name}/{state}.png first.");

        Directory.CreateDirectory(Prefabs);
        Directory.CreateDirectory(Data);
        AssetDatabase.Refresh();
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Blue Slime.prefab");
        if (template == null) throw new InvalidOperationException("Missing Blue Slime template.");
        var prefabs = new List<GameObject>();
        for (int i = 0; i < Monsters.Length; i++)
        {
            string name = Monsters[i];
            var clips = new List<AnimationClip>();
            for (int s = 0; s < States.Length; s++)
            {
                string imagePath = $"{Root}/{name}/{States[s]}.png";
                Slice(imagePath, States[s], Counts[s]);
                clips.Add(BuildClip(imagePath, States[s], Counts[s], Fps[s], s < 2));
            }
            AnimatorController controller = BuildController(name, clips);
            string dataPath = $"{Data}/{name}.asset";
            var data = AssetDatabase.LoadAssetAtPath<EnemyScriptableObject>(dataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyScriptableObject>();
                var serialized = new SerializedObject(data);
                serialized.FindProperty("moveSpeed").floatValue = Speeds[i];
                serialized.FindProperty("maxHealth").floatValue = Health[i];
                serialized.FindProperty("damage").floatValue = Damage[i];
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(data, dataPath);
            }
            string prefabPath = $"{Prefabs}/{name}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            // Existing generated prefabs/data are kept, including any subsequent user edits.
            if (prefab == null)
            {
                GameObject instance = Object.Instantiate(template);
                try
                {
                    instance.name = name;
                    instance.transform.position = Vector3.zero;
                    instance.transform.localScale = Vector3.one;
                    instance.GetComponent<EnemyStats>().enemyData = data;
                    var renderer = instance.GetComponent<SpriteRenderer>();
                    renderer.sprite = LoadFrames($"{Root}/{name}/Idle.png")[0];
                    renderer.flipX = false;
                    renderer.color = Color.white;
                    var animator = instance.GetComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;
                    instance.AddComponent<EnemyAnimation>();
                    var movement = new SerializedObject(instance.GetComponent<Enemy>());
                    movement.FindProperty("spriteFacesLeft").boolValue = name == "Skeleton" || name == "Spider";
                    movement.ApplyModifiedPropertiesWithoutUndo();
                    // Pivot is at the feet (original y=46). Collider covers the body above it.
                    var collider = instance.GetComponent<CapsuleCollider2D>();
                    collider.offset = new Vector2(0, name == "Slime" ? 0.25f : 0.4f);
                    collider.size = name == "Slime" ? new Vector2(0.65f, 0.5f)
                        : name == "Skeleton" ? new Vector2(0.4f, 0.8f) : new Vector2(0.65f, 0.7f);
                    prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                }
                finally { Object.DestroyImmediate(instance); }
            }
            prefabs.Add(prefab);
        }
        foreach (var spawner in spawners)
        {
            Undo.RecordObject(spawner, "Add reference enemies to spawner");
            var serialized = new SerializedObject(spawner);
            var array = serialized.FindProperty("enemyPrefabs");
            foreach (GameObject prefab in prefabs)
            {
                bool exists = false;
                for (int i = 0; i < array.arraySize; i++)
                    exists |= array.GetArrayElementAtIndex(i).objectReferenceValue == prefab;
                if (exists) continue;
                int index = array.arraySize++;
                array.GetArrayElementAtIndex(index).objectReferenceValue = prefab;
            }
            serialized.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(spawner);
            EditorUtility.SetDirty(spawner);
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Validate();
        Debug.Log("REFERENCE ENEMIES BUILT: 4 prefabs + data, 16 clips; existing spawn settings/prefabs preserved.");
    }

    private static void Slice(string path, string state, int count)
    {
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 48;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var rects = new SpriteRect[count];
        using (var md5 = MD5.Create())
            for (int i = 0; i < count; i++)
            {
                string id = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(path + "/" + i)))
                    .Replace("-", "").ToLowerInvariant();
                rects[i] = new SpriteRect
                {
                    name = state + "_" + i, rect = new Rect(i * 64, 0, 64, 64),
                    alignment = SpriteAlignment.Custom, pivot = new Vector2(0.5f, 10f / 64f),
                    spriteID = new GUID(id)
                };
            }
        provider.SetSpriteRects(rects);
        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.SaveAndReimport();
        if (LoadFrames(path).Length != count)
            throw new InvalidOperationException("Slice count mismatch: " + path);
    }

    private static Sprite[] LoadFrames(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    private static AnimationClip BuildClip(string sheet, string state, int count, float fps, bool loop)
    {
        string path = Path.ChangeExtension(sheet, ".anim");
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip != null) return clip;
        clip = new AnimationClip { name = state, frameRate = fps };
        Sprite[] sprites = LoadFrames(sheet);
        var keys = new ObjectReferenceKeyframe[count + 1];
        for (int i = 0; i < count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };
        keys[count] = new ObjectReferenceKeyframe { time = count / fps, value = loop ? sprites[0] : sprites[count - 1] };
        var binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static AnimatorController BuildController(string name, List<AnimationClip> clips)
    {
        string path = $"{Root}/{name}/{name}.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller != null) return controller;
        controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = controller.layers[0].stateMachine;
        for (int i = 0; i < States.Length; i++)
        {
            var state = machine.AddState(States[i], new Vector3(250, i * 65));
            state.motion = clips[i];
            state.writeDefaultValues = false;
            if (i == 0) machine.defaultState = state;
        }
        return controller;
    }

    [MenuItem("Tools/System Leveling/Validate Reference Enemies")]
    public static void Validate()
    {
        int sprites = 0;
        foreach (string name in Monsters)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Prefabs}/{name}.prefab");
            if (prefab == null || prefab.GetComponent<EnemyStats>().enemyData == null ||
                prefab.GetComponent<EnemyAnimation>() == null || prefab.GetComponent<Animator>().runtimeAnimatorController == null ||
                !prefab.CompareTag("Enemy") || prefab.GetComponent<Rigidbody2D>().gravityScale != 0 ||
                prefab.GetComponent<DropRateManager>().drops.Count == 0)
                throw new InvalidOperationException("Invalid reference prefab: " + name);
            for (int i = 0; i < States.Length; i++)
            {
                string sheet = $"{Root}/{name}/{States[i]}.png";
                Sprite[] frames = LoadFrames(sheet);
                if (frames.Length != Counts[i]) throw new InvalidOperationException("Wrong frame count: " + sheet);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Path.ChangeExtension(sheet, ".anim"));
                var keys = AnimationUtility.GetObjectReferenceCurve(clip,
                    new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" });
                if (keys == null || keys.Length != Counts[i] + 1 || keys.Any(k => k.value == null) ||
                    AnimationUtility.GetAnimationClipSettings(clip).loopTime != (i < 2))
                    throw new InvalidOperationException("Broken clip: " + sheet);
                sprites += frames.Length;
            }
            foreach (var spawner in Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include))
            {
                var serialized = new SerializedObject(spawner);
                var array = serialized.FindProperty("enemyPrefabs");
                int matches = 0;
                for (int i = 0; i < array.arraySize; i++)
                    if (array.GetArrayElementAtIndex(i).objectReferenceValue == prefab) matches++;
                if (matches != 1) throw new InvalidOperationException("Missing/duplicate spawner reference: " + name);
            }
        }
        Debug.Log($"REFERENCE ENEMY VALIDATION PASS: {sprites} sprites, 16 clips, 4 prefabs/data and spawner references.");
    }
}
