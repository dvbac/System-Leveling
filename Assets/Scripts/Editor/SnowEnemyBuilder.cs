using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SnowEnemyBuilder
{
    private const string Art = "Assets/MyAssets/Monster/SnowVariants";
    private const string Prefabs = "Assets/Prefab/ReferenceEnemies/Snow";
    private static readonly string[] Sources = { "Wolf", "Slime" };
    private static readonly string[] Variants = { "SnowWolf", "IceSlime" };
    private static readonly string[] States = { "Idle", "Move", "Hit", "Die" };
    private static readonly EditorCurveBinding SpriteBinding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };

    [MenuItem("Tools/System Leveling/Build Snow Enemy Variants")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build snow variants outside Play Mode.");
        foreach (string source in Sources)
        {
            Load<GameObject>($"Assets/Prefab/ReferenceEnemies/{source}.prefab");
            foreach (string state in States)
            {
                Load<AnimationClip>($"Assets/MyAssets/Monster/ReferenceAnimated/{source}/{state}.anim");
                if (!File.Exists($"Assets/MyAssets/Monster/ReferenceAnimated/{source}/{state}.png")) throw new FileNotFoundException(source + "/" + state);
            }
        }
        Directory.CreateDirectory(Prefabs);
        foreach (string name in Variants) Directory.CreateDirectory(Art + "/" + name);
        AssetDatabase.Refresh();
        for (int i = 0; i < Sources.Length; i++)
        {
            GameObject parent = Load<GameObject>($"Assets/Prefab/ReferenceEnemies/{Sources[i]}.prefab");
            var clips = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (string state in States)
            {
                string source = $"Assets/MyAssets/Monster/ReferenceAnimated/{Sources[i]}/{state}";
                string destination = $"{Art}/{Variants[i]}/{state}";
                if (!File.Exists(destination + ".png"))
                {
                    Copy(source + ".png", destination + ".png"); // Preserve slicing, pivots, PPU and transparency import settings.
                    Recolor(source + ".png", destination + ".png", i == 0);
                }
                AnimationClip original = Load<AnimationClip>(source + ".anim");
                AnimationClip replacement = AssetDatabase.LoadAssetAtPath<AnimationClip>(destination + ".anim");
                if (replacement == null)
                {
                    Copy(source + ".anim", destination + ".anim");
                    replacement = Load<AnimationClip>(destination + ".anim");
                    var sprites = Frames(destination + ".png").ToDictionary(sprite => sprite.name);
                    ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(original, SpriteBinding);
                    for (int k = 0; k < keys.Length; k++) keys[k].value = sprites[keys[k].value.name];
                    AnimationUtility.SetObjectReferenceCurve(replacement, SpriteBinding, keys);
                    AssetDatabase.SaveAssetIfDirty(replacement);
                }
                clips.Add(new KeyValuePair<AnimationClip, AnimationClip>(original, replacement));
            }
            string controllerPath = $"{Art}/{Variants[i]}/{Variants[i]}.overrideController";
            AnimatorOverrideController controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath);
            if (controller == null)
            {
                controller = new AnimatorOverrideController(parent.GetComponent<Animator>().runtimeAnimatorController);
                controller.ApplyOverrides(clips);
                AssetDatabase.CreateAsset(controller, controllerPath);
            }
            string prefabPath = $"{Prefabs}/{Variants[i]}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null) continue;
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(parent, preview);
                instance.name = Variants[i];
                instance.GetComponent<SpriteRenderer>().sprite = Frames($"{Art}/{Variants[i]}/Idle.png")[0];
                instance.GetComponent<Animator>().runtimeAnimatorController = controller;
                if (PrefabUtility.SaveAsPrefabAsset(instance, prefabPath) == null) throw new IOException("Could not save " + prefabPath);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        Validate();
        Debug.Log("SNOW ENEMIES BUILT: SnowWolf and IceSlime prefab variants, all four animation states; source stats/loot/scenes/spawn pools unchanged.");
    }

    private static void Recolor(string source, string destination, bool wolf)
    {
        Texture2D image = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!image.LoadImage(File.ReadAllBytes(source))) throw new InvalidDataException(source);
            Color32[] pixels = image.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a == 0) continue;
                Color color = pixels[i];
                float light = color.r * 0.2126f + color.g * 0.7152f + color.b * 0.0722f;
                Color snow;
                if (wolf)
                {
                    if (light < 0.13f) snow = Color.Lerp(new Color(0.035f, 0.075f, 0.13f), new Color(0.12f, 0.22f, 0.32f), light / 0.13f);
                    else snow = Color.Lerp(new Color(0.31f, 0.46f, 0.60f), new Color(0.96f, 0.99f, 1f), Mathf.InverseLerp(0.13f, 0.47f, light));
                    if ((light > 0.5f && color.g > 0.5f && color.b > color.r * 1.4f) ||
                        (color.r > color.g * 1.15f && color.r > color.b * 1.1f)) snow = new Color(0.35f, 0.9f, 1f);
                }
                else
                {
                    if (light < 0.3f) snow = Color.Lerp(new Color(0.035f, 0.13f, 0.24f), new Color(0.18f, 0.43f, 0.67f), light / 0.3f);
                    else snow = Color.Lerp(new Color(0.38f, 0.68f, 0.86f), new Color(0.96f, 1f, 1f), Mathf.InverseLerp(0.3f, 0.9f, light));
                }
                snow.a = color.a;
                pixels[i] = snow;
            }
            image.SetPixels32(pixels); image.Apply();
            File.WriteAllBytes(destination, image.EncodeToPNG());
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceUpdate);
        }
        finally { Object.DestroyImmediate(image); }
    }

    [MenuItem("Tools/System Leveling/Validate Snow Enemy Variants")]
    public static void Validate()
    {
        var passed = new List<string>();
        for (int i = 0; i < Sources.Length; i++)
        {
            GameObject parent = Load<GameObject>($"Assets/Prefab/ReferenceEnemies/{Sources[i]}.prefab");
            GameObject variant = Load<GameObject>($"{Prefabs}/{Variants[i]}.prefab");
            Check(PrefabUtility.GetPrefabAssetType(variant) == PrefabAssetType.Variant && PrefabUtility.GetCorrespondingObjectFromSource(variant) == parent,
                Variants[i] + " inherits its original prefab", passed);
            Check(variant.GetComponent<EnemyStats>().enemyData == parent.GetComponent<EnemyStats>().enemyData &&
                variant.GetComponent<Enemy>() != null && variant.GetComponent<EnemyAnimation>() != null &&
                variant.GetComponent<DropRateManager>().drops.Count == parent.GetComponent<DropRateManager>().drops.Count,
                Variants[i] + " retains stats, AI, animation adapter and loot", passed);
            Check(variant.GetComponent<CapsuleCollider2D>().size == parent.GetComponent<CapsuleCollider2D>().size &&
                variant.GetComponent<CapsuleCollider2D>().offset == parent.GetComponent<CapsuleCollider2D>().offset,
                Variants[i] + " retains collision size and offset", passed);
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(variant, preview);
                Animator animator = instance.GetComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0);
                foreach (string state in States)
                {
                    string source = $"Assets/MyAssets/Monster/ReferenceAnimated/{Sources[i]}/{state}";
                    string target = $"{Art}/{Variants[i]}/{state}";
                    Sprite[] originals = Frames(source + ".png"), replacements = Frames(target + ".png");
                    Check(originals.Length == replacements.Length && originals.Length > 0, Variants[i] + "/" + state + " has every frame", passed);
                    for (int f = 0; f < originals.Length; f++)
                        Check(originals[f].rect == replacements[f].rect && originals[f].pivot == replacements[f].pivot && originals[f].pixelsPerUnit == replacements[f].pixelsPerUnit,
                            Variants[i] + "/" + state + "/" + f + " preserves geometry", passed);
                    AnimationClip original = Load<AnimationClip>(source + ".anim"), clip = Load<AnimationClip>(target + ".anim");
                    var keys = AnimationUtility.GetObjectReferenceCurve(clip, SpriteBinding);
                    var originalKeys = AnimationUtility.GetObjectReferenceCurve(original, SpriteBinding);
                    Check(clip.frameRate == original.frameRate && AnimationUtility.GetAnimationClipSettings(clip).loopTime == AnimationUtility.GetAnimationClipSettings(original).loopTime &&
                        keys.Length == originalKeys.Length && keys.Select((key, index) => key.time == originalKeys[index].time && replacements.Contains(key.value as Sprite)).All(valid => valid),
                        Variants[i] + "/" + state + " keeps timing and uses only snow frames", passed);
                    animator.Play(state, 0, 0); animator.Update(0);
                    Sprite first = instance.GetComponent<SpriteRenderer>().sprite;
                    animator.Update(1.1f / clip.frameRate);
                    Check(animator.GetCurrentAnimatorStateInfo(0).IsName(state) && replacements.Contains(first) && replacements.Contains(instance.GetComponent<SpriteRenderer>().sprite) &&
                        first != instance.GetComponent<SpriteRenderer>().sprite, Variants[i] + "/" + state + " actually animates with the snow skin", passed);
                    CheckAlpha(source + ".png", target + ".png", passed);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        Directory.CreateDirectory("Captures/SnowEnemies");
        File.WriteAllText("Captures/SnowEnemies/validation.json", JsonUtility.ToJson(new Report { result = "PASS", checks = passed.ToArray(), utc = DateTime.UtcNow.ToString("o") }, true));
        Debug.Log("SNOW ENEMY VALIDATION PASS: " + passed.Count + " checks.");
    }

    private static void CheckAlpha(string source, string target, List<string> passed)
    {
        Texture2D a = new Texture2D(2, 2), b = new Texture2D(2, 2);
        try
        {
            a.LoadImage(File.ReadAllBytes(source)); b.LoadImage(File.ReadAllBytes(target));
            Color32[] original = a.GetPixels32(), snow = b.GetPixels32();
            Check(a.width == b.width && a.height == b.height && original.Select((pixel, index) => pixel.a == snow[index].a).All(same => same),
                target + " preserves every alpha pixel", passed);
            Check(original.Where((pixel, index) => pixel.a != 0 && !pixel.Equals(snow[index])).Any(), target + " has a different snow palette", passed);
        }
        finally { Object.DestroyImmediate(a); Object.DestroyImmediate(b); }
    }

    private static Sprite[] Frames(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
    private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);
    private static void Copy(string source, string target) { if (!AssetDatabase.CopyAsset(source, target)) throw new IOException("Could not copy " + source); }
    private static void Check(bool valid, string label, List<string> passed) { if (!valid) throw new InvalidOperationException(label); passed.Add(label); }
    [Serializable] private class Report { public string result, utc; public string[] checks; }
}
