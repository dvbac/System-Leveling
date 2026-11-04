using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class RainbowSlimeBuilder
{
    public const string SlimePath = "Assets/Resources/RainbowSlime.prefab";
    public const string CardPath = "Assets/Prefab/RedTomeCard.prefab";
    private const string Art = "Assets/MyAssets/Monster/RainbowSlime";

    [MenuItem("Tools/System Leveling/Build Rainbow Slime")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build outside Play Mode.");
        Directory.CreateDirectory(Art); Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();
        var parent = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Slime.prefab");
        var binding = new EditorCurveBinding { path = "", type = typeof(SpriteRenderer), propertyName = "m_Sprite" };
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (string state in new[] { "Idle", "Move", "Hit", "Die" })
        {
            string source = "Assets/MyAssets/Monster/ReferenceAnimated/Slime/" + state;
            string target = Art + "/" + state;
            if (!File.Exists(target + ".png"))
            {
                if (!AssetDatabase.CopyAsset(source + ".png", target + ".png")) throw new IOException(target);
                var image = new Texture2D(2, 2);
                try
                {
                    image.LoadImage(File.ReadAllBytes(source + ".png"));
                    var pixels = image.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        byte light = Math.Max(pixels[i].r, Math.Max(pixels[i].g, pixels[i].b));
                        pixels[i] = new Color32(light, light, light, pixels[i].a);
                    }
                    image.SetPixels32(pixels); image.Apply(); File.WriteAllBytes(target + ".png", image.EncodeToPNG());
                    AssetDatabase.ImportAsset(target + ".png", ImportAssetOptions.ForceUpdate);
                }
                finally { Object.DestroyImmediate(image); }
            }
            var original = AssetDatabase.LoadAssetAtPath<AnimationClip>(source + ".anim");
            var replacement = AssetDatabase.LoadAssetAtPath<AnimationClip>(target + ".anim");
            if (replacement == null)
            {
                if (!AssetDatabase.CopyAsset(source + ".anim", target + ".anim")) throw new IOException(target);
                replacement = AssetDatabase.LoadAssetAtPath<AnimationClip>(target + ".anim");
                var sprites = AssetDatabase.LoadAllAssetsAtPath(target + ".png").OfType<Sprite>().ToDictionary(s => s.name);
                var keys = AnimationUtility.GetObjectReferenceCurve(original, binding);
                for (int i = 0; i < keys.Length; i++) keys[i].value = sprites[keys[i].value.name];
                AnimationUtility.SetObjectReferenceCurve(replacement, binding, keys); AssetDatabase.SaveAssetIfDirty(replacement);
            }
            overrides.Add(new KeyValuePair<AnimationClip, AnimationClip>(original, replacement));
        }
        string controllerPath = Art + "/RainbowSlime.overrideController";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath);
        if (controller == null)
        {
            controller = new AnimatorOverrideController(parent.GetComponent<Animator>().runtimeAnimatorController);
            controller.ApplyOverrides(overrides); AssetDatabase.CreateAsset(controller, controllerPath);
        }
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CardPath) == null)
            {
                var card = new GameObject("Red Tome Card", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(RedTomeCard));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(card, preview);
                var sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/MyAssets/Other/Drop/RedCard.png").OfType<Sprite>().First();
                card.GetComponent<SpriteRenderer>().sprite = sprite; card.GetComponent<SpriteRenderer>().sortingOrder = 6;
                card.transform.localScale = Vector3.one * 2f;
                var trigger = card.GetComponent<CircleCollider2D>(); trigger.radius = 0.18f; trigger.isTrigger = true;
                if (PrefabUtility.SaveAsPrefabAsset(card, CardPath) == null) throw new IOException(CardPath);
            }
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SlimePath) == null)
            {
                var data = Object.Instantiate(parent.GetComponent<EnemyStats>().enemyData);
                data.name = "Rainbow Slime";
                var settings = new SerializedObject(data);
                settings.FindProperty("maxHealth").floatValue = 120f; settings.FindProperty("moveSpeed").floatValue = 1.2f;
                settings.FindProperty("damage").floatValue = 0f; settings.FindProperty("experienceReward").floatValue = 0;
                settings.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(data, Art + "/RainbowSlime.asset");
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(parent, preview); instance.name = "Rainbow Slime";
                instance.transform.localScale *= 1.5f;
                instance.GetComponent<EnemyStats>().enemyData = data; instance.GetComponent<Enemy>().chasesPlayer = false;
                Object.DestroyImmediate(instance.GetComponent<DropRateManager>());
                instance.GetComponent<Animator>().runtimeAnimatorController = controller;
                instance.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAllAssetsAtPath(Art + "/Idle.png").OfType<Sprite>().First();
                var rainbow = instance.AddComponent<RainbowSlime>(); var serialized = new SerializedObject(rainbow);
                serialized.FindProperty("redCardPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CardPath).GetComponent<RedTomeCard>();
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.SaveAsPrefabAsset(instance, SlimePath) == null) throw new IOException(SlimePath);
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        Debug.Log("RAINBOW SLIME BUILT: passive wandering, 120 HP, 60s lifetime, red Tome card. Both map spawners load the resource automatically. Scenes untouched.");
    }
}
