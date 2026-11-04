using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class RedArrowWeaponBuilder
{
    public const string Source = "Assets/MyAssets/Weapon/Base/RedArrow.png";
    public const string SpritePath = "Assets/MyAssets/Weapon/Generated/RedArrow.png";
    public const string WeaponPath = "Assets/Prefab/Weapons/RedArrow.prefab";
    public const string ProjectilePath = "Assets/Prefab/Weapons/RedArrowProjectile.prefab";
    public const string DataPath = "Assets/Scriptable Object/Weapon/Red Arrow.asset";
    private const string Output = "Captures/RedArrowWeapon";

    [MenuItem("Tools/System Leveling/Red Arrow/Build Weapon")]
    public static void Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build Red Arrow outside Play Mode.");
        if (!File.Exists(Source)) throw new FileNotFoundException(Source);
        Scene scene = SceneManager.GetSceneByPath(LoadoutLauncher.SelectionScene);
        if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save your LoadoutSelection scene changes first.");
        Directory.CreateDirectory(Output); Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));
        string backup = Output + "/Before-LoadoutSelection.unity";
        if (!File.Exists(backup)) File.Copy(LoadoutLauncher.SelectionScene, backup);
        string hash = SourceHash();
        if (!File.Exists(SpritePath)) File.Copy(Source, SpritePath);
        AssetDatabase.ImportAsset(SpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 96; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null) throw new InvalidOperationException("Red Arrow sprite did not import.");
        var data = AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<WeaponScriptableObject>(); data.name = "Red Arrow";
            var so = new SerializedObject(data);
            so.FindProperty("damage").floatValue = 3; so.FindProperty("speed").floatValue = 6;
            so.FindProperty("cooldown").floatValue = 0.7f; so.FindProperty("pierce").intValue = 1;
            so.FindProperty("projectiles").intValue = 1; so.FindProperty("spreadAngle").floatValue = 60;
            so.FindProperty("knockback").floatValue = 2; so.FindProperty("baseBounces").intValue = 1;
            so.FindProperty("fireMode").enumValueIndex = (int)WeaponFireMode.Magazine;
            so.FindProperty("magazineSize").intValue = 9; so.FindProperty("reloadTime").floatValue = 1.5f;
            so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.CreateAsset(data, DataPath);
        }
        var projectile = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath);
        if (projectile == null)
        {
            var go = new GameObject("Red Arrow Projectile"); go.SetActive(false);
            try
            {
                var visual = go.AddComponent<SpriteRenderer>(); visual.sprite = sprite; visual.sortingOrder = 12;
                visual.sharedMaterial = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/Arrow.prefab").GetComponent<SpriteRenderer>().sharedMaterial;
                var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
                var collider = go.AddComponent<CircleCollider2D>(); collider.isTrigger = true; collider.radius = 0.12f;
                var shot = go.AddComponent<RedArrowProjectile>(); shot.weaponData = data;
                go.SetActive(true); projectile = PrefabUtility.SaveAsPrefabAsset(go, ProjectilePath);
            }
            finally { Object.DestroyImmediate(go); }
        }
        if (data.Prefab == null)
        {
            var so = new SerializedObject(data); so.FindProperty("prefab").objectReferenceValue = projectile;
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data);
        }
        var weapon = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPath);
        if (weapon == null)
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/MagicBook.prefab"));
            try
            {
                go.name = "Red Arrow"; Object.DestroyImmediate(go.GetComponent<WeaponController>());
                go.AddComponent<RedArrowController>().weaponData = data;
                var visual = go.GetComponentInChildren<SpriteRenderer>(); visual.sprite = sprite;
                var grip = go.GetComponent<WeaponGrip>(); var so = new SerializedObject(grip);
                so.FindProperty("visual").objectReferenceValue = visual; so.FindProperty("visualSize").floatValue = 0.45f;
                so.FindProperty("spriteAngle").floatValue = 0; so.FindProperty("holdOffset").vector2Value = new Vector2(0.3f, 0);
                so.FindProperty("spriteGrip").vector2Value = Vector2.one * 0.5f;
                so.FindProperty("muzzleOffset").vector2Value = new Vector2(0.58f, 0);
                so.ApplyModifiedPropertiesWithoutUndo(); grip.Apply(); weapon = PrefabUtility.SaveAsPrefabAsset(go, WeaponPath);
            }
            finally { Object.DestroyImmediate(go); }
        }
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(LoadoutLauncher.SelectionScene, OpenSceneMode.Additive);
        try
        {
            var ui = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<LoadoutSelection>(true)).Single();
            var before = ui.weaponPrefabs.ToArray();
            if (!before.Contains(weapon))
            {
                var so = new SerializedObject(ui); var catalog = so.FindProperty("weaponPrefabs");
                catalog.arraySize++; catalog.GetArrayElementAtIndex(catalog.arraySize - 1).objectReferenceValue = weapon;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save weapon catalog.");
            }
            if (!ui.weaponPrefabs.Take(before.Length).SequenceEqual(before) || ui.weaponPrefabs.Count(w => w == weapon) != 1)
                throw new InvalidOperationException("Catalog order changed or duplicate Red Arrow.");
            LoadoutSelection.ValidatePrefabs(ui.characterPrefabs, ui.weaponPrefabs);
        }
        finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets();
        if (SourceHash() != hash) throw new InvalidOperationException("Original RedArrow PNG changed.");
        File.WriteAllText(Output + "/setup.json", "{\"result\":\"PASS\",\"sourceUnchanged\":true,\"sourceSHA256\":\"" + hash + "\",\"baseBounces\":1,\"catalogAppendOnly\":true}");
        Debug.Log("RED ARROW READY: homing + base bounce1; damage3, speed6, interval0.7s, ammo9, reload1.5s; appended to loadout catalog.");
    }

    private static string SourceHash()
    { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Source))); }
}
