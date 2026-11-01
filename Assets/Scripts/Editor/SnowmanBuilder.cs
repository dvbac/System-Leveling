using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SnowmanBuilder
{
    private const string Source = "Assets/MyAssets/Monster/SnowVariants/Snowman/Snowman.png";
    private const string Art = "Assets/MyAssets/Monster/SnowVariants/Snowman/Generated";
    private const string Template = "Assets/MyAssets/Monster/ReferenceAnimated/Skeleton";
    private const string Prefab = "Assets/Prefab/ReferenceEnemies/Snow/Snowman.prefab";
    private const string Data = "Assets/Scriptable Object/Enemy/Snow/Snowman.asset";
    private static readonly string[] States = { "Idle", "Move", "Hit", "Die" };
    private static readonly EditorCurveBinding SpriteBinding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };

    [MenuItem("Tools/System Leveling/Build Snowman Enemy")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build Snowman outside Play Mode.");
        if (!File.Exists(Source)) throw new FileNotFoundException(Source);
        GameObject template = Load<GameObject>("Assets/Prefab/ReferenceEnemies/Skeleton.prefab");
        Directory.CreateDirectory(Art); Directory.CreateDirectory(Path.GetDirectoryName(Data)); Directory.CreateDirectory(Path.GetDirectoryName(Prefab));
        AssetDatabase.Refresh();
        Texture2D image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        try
        {
            if (!image.LoadImage(File.ReadAllBytes(Source))) throw new InvalidDataException(Source);
            if (image.width > 48 || image.height > 48) throw new InvalidOperationException("Expected the supplied Snowman sprite up to 48x48.");
            Color32[] pixels = image.GetPixels32();
            int floor = image.height;
            for (int y = 0; y < image.height; y++)
                for (int x = 0; x < image.width; x++) if (pixels[y * image.width + x].a > 0) floor = Mathf.Min(floor, y);
            if (floor == image.height) throw new InvalidDataException("Snowman image is fully transparent.");
            foreach (string state in States)
            {
                string sheet = Art + "/" + state + ".png";
                int count = Frames(Template + "/" + state + ".png").Length;
                if (!File.Exists(sheet))
                {
                    Copy(Template + "/" + state + ".png", sheet); // Reuse the existing 64px cell layout and foot pivot.
                    GenerateSheet(pixels, image.width, image.height, floor, state, count, sheet);
                    TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(sheet);
                    importer.spritePixelsPerUnit = 48; importer.filterMode = FilterMode.Point;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
                AnimationClip original = Load<AnimationClip>(Template + "/" + state + ".anim");
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Art + "/" + state + ".anim");
                if (clip == null)
                {
                    Copy(Template + "/" + state + ".anim", Art + "/" + state + ".anim");
                    clip = Load<AnimationClip>(Art + "/" + state + ".anim");
                    Dictionary<string, Sprite> sprites = Frames(sheet).ToDictionary(sprite => sprite.name);
                    ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(original, SpriteBinding);
                    for (int k = 0; k < keys.Length; k++) keys[k].value = sprites[keys[k].value.name];
                    AnimationUtility.SetObjectReferenceCurve(clip, SpriteBinding, keys); AssetDatabase.SaveAssetIfDirty(clip);
                }
                overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(original, clip));
            }
        }
        finally { Object.DestroyImmediate(image); }

        AnimatorOverrideController controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(Art + "/Snowman.overrideController");
        if (controller == null)
        {
            controller = new AnimatorOverrideController(template.GetComponent<Animator>().runtimeAnimatorController);
            controller.ApplyOverrides(overrides); AssetDatabase.CreateAsset(controller, Art + "/Snowman.overrideController");
        }
        EnemyScriptableObject data = AssetDatabase.LoadAssetAtPath<EnemyScriptableObject>(Data);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyScriptableObject>();
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("maxHealth").floatValue = 45;
            so.FindProperty("moveSpeed").floatValue = 1.25f;
            so.FindProperty("damage").floatValue = 8;
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(data, Data);
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Prefab) == null)
        {
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(template, preview);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                root.name = "Snowman"; root.GetComponent<EnemyStats>().enemyData = data;
                root.GetComponent<SpriteRenderer>().sprite = Frames(Art + "/Idle.png")[0];
                root.GetComponent<Animator>().runtimeAnimatorController = controller;
                SerializedObject movement = new SerializedObject(root.GetComponent<Enemy>());
                movement.FindProperty("spriteFacesLeft").boolValue = false; movement.ApplyModifiedPropertiesWithoutUndo();
                CapsuleCollider2D collider = root.GetComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Vertical;
                collider.size = new Vector2(0.5f, 0.9f); collider.offset = new Vector2(0, 0.45f);
                if (PrefabUtility.SaveAsPrefabAsset(root, Prefab) == null) throw new IOException(Prefab);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        Validate();
        Debug.Log($"SNOWMAN BUILT: HP{data.MaxHealth}, speed{data.MoveSpeed}, contact damage{data.Damage}; Idle/Move/Hit/Die generated from the supplied image. Scene/spawn unchanged.");
    }

    private static void GenerateSheet(Color32[] source, int width, int height, int floor, string state, int count, string path)
    {
        const int cell = 64;
        Color32[] output = new Color32[cell * count * cell];
        for (int frame = 0; frame < count; frame++)
        {
            float phase = frame * Mathf.PI * 2f / count;
            float progress = frame / (float)(count - 1);
            float angle = 0, sx = 1, sy = 1, dx = 0, dy = 0, alpha = 1, flash = 0;
            if (state == "Idle") { angle = Mathf.Sin(phase) * 1.5f; dy = Mathf.Round((1 - Mathf.Cos(phase)) * 0.5f); }
            if (state == "Move") { angle = Mathf.Sin(phase) * 3; dy = Mathf.Round(Mathf.Abs(Mathf.Sin(phase)) * 2); sx += Mathf.Cos(phase) * 0.025f; sy -= Mathf.Cos(phase) * 0.02f; }
            if (state == "Hit") { angle = (frame % 2 == 0 ? -3 : 3) * (1 - progress); dx = new[] { -2f, 2f, -1f, 0f }[frame]; flash = frame == 1 ? 0.55f : frame == 2 ? 0.25f : 0; }
            if (state == "Die") { sx += progress * 0.25f; sy = 1 - progress * 0.9f; alpha = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(0.55f, 1, progress)); }
            float cos = Mathf.Cos(angle * Mathf.Deg2Rad), sin = Mathf.Sin(angle * Mathf.Deg2Rad);
            for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    float px = x - 32 - dx, py = y - 10 - dy;
                    int u = Mathf.RoundToInt((cos * px + sin * py) / sx + width / 2f);
                    int v = Mathf.RoundToInt((-sin * px + cos * py) / sy + floor);
                    if (u < 0 || u >= width || v < 0 || v >= height) continue;
                    Color color = source[v * width + u];
                    Color lit = Color.Lerp(color, Color.white, flash); lit.a = color.a * alpha;
                    output[y * cell * count + frame * cell + x] = lit;
                }
            if (state == "Die")
                for (int shard = 0; shard < 6; shard++)
                {
                    float direction = shard * Mathf.PI / 3;
                    int x = Mathf.RoundToInt(32 + Mathf.Cos(direction) * (3 + progress * 13));
                    int y = Mathf.RoundToInt(13 + Mathf.Abs(Mathf.Sin(direction)) * (5 + progress * 8));
                    Color32 snow = new Color32(210, 244, 255, (byte)Mathf.RoundToInt(255 * (1 - progress)));
                    output[y * cell * count + frame * cell + x] = snow;
                }
        }
        Texture2D sheet = new Texture2D(cell * count, cell, TextureFormat.RGBA32, false);
        try { sheet.SetPixels32(output); sheet.Apply(); File.WriteAllBytes(path, sheet.EncodeToPNG()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate); }
        finally { Object.DestroyImmediate(sheet); }
    }

    [MenuItem("Tools/System Leveling/Validate Snowman Enemy")]
    public static void Validate()
    {
        var checks = new List<string>();
        GameObject prefab = Load<GameObject>(Prefab);
        EnemyScriptableObject data = prefab.GetComponent<EnemyStats>().enemyData;
        Check(data != null && data.MaxHealth > 0 && data.MoveSpeed > 0 && data.Damage > 0, "Snowman has separate positive enemy stats", checks);
        Check(AssetDatabase.GetAssetPath(data) == Data && prefab.GetComponent<Enemy>() != null && prefab.GetComponent<EnemyAnimation>() != null && prefab.CompareTag("Enemy"), "Snowman uses the existing enemy systems", checks);
        Check(prefab.GetComponent<Rigidbody2D>().gravityScale == 0 && prefab.GetComponent<CapsuleCollider2D>().size == new Vector2(0.5f, 0.9f) && prefab.GetComponent<DropRateManager>().drops.Count > 0, "Physics, body collider and loot are configured", checks);
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, preview);
            Animator animator = instance.GetComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate; animator.Rebind(); animator.Update(0);
            foreach (string state in States)
            {
                Sprite[] frames = Frames(Art + "/" + state + ".png");
                int count = Frames(Template + "/" + state + ".png").Length;
                AnimationClip clip = Load<AnimationClip>(Art + "/" + state + ".anim");
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, SpriteBinding);
                Check(frames.Length == count && keys.Length == count + 1 && keys.All(key => frames.Contains(key.value as Sprite)), state + " has complete frame bindings", checks);
                Check(AnimationUtility.GetAnimationClipSettings(clip).loopTime == (state == "Idle" || state == "Move"), state + " has correct loop behavior", checks);
                foreach (Sprite frame in frames) Check(frame.pixelsPerUnit == 48 && frame.pivot == new Vector2(32, 10), state + "/" + frame.name + " keeps foot pivot/PPU", checks);
                animator.Play(state, 0, 0); animator.Update(0);
                Sprite first = instance.GetComponent<SpriteRenderer>().sprite; animator.Update(1.1f / clip.frameRate);
                Check(animator.GetCurrentAnimatorStateInfo(0).IsName(state) && frames.Contains(first) && frames.Contains(instance.GetComponent<SpriteRenderer>().sprite) && first != instance.GetComponent<SpriteRenderer>().sprite,
                    state + " actually plays generated Snowman sprites", checks);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        Directory.CreateDirectory("Captures/Snowman");
        File.WriteAllText("Captures/Snowman/asset-validation.json", JsonUtility.ToJson(new Report { result = "PASS", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray() }, true));
        Debug.Log("SNOWMAN ASSET VALIDATION PASS: " + checks.Count);
    }

    private static Sprite[] Frames(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
    private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);
    private static void Copy(string source, string target) { if (!AssetDatabase.CopyAsset(source, target)) throw new IOException(target); }
    private static void Check(bool valid, string message, List<string> checks) { if (!valid) throw new InvalidOperationException(message); checks.Add(message); }
    [Serializable] private class Report { public string result, utc; public string[] checks; }
}
