using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WeaponHolderSetup
{
    [MenuItem("Tools/System Leveling/Fix Weapon Holding")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Apply holding calibration outside Play Mode.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity") throw new InvalidOperationException("Open the gameplay scene first.");
        Backup(scene.path);
        foreach (string name in new[] { "MagicBook", "Bow", "Crossbow", "Sword" })
        {
            string path = $"Assets/Prefab/Weapons/{name}.prefab"; Backup(path);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try { ConfigureWeapon(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (string path in new[] { "Assets/Prefab/WeaponHolder.prefab", "Assets/Prefab/Characters/Archer.prefab", "Assets/Prefab/Characters/Hunter.prefab", "Assets/Prefab/Characters/Knight.prefab" })
        {
            Backup(path);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                WeaponHolder holder = root.GetComponentInChildren<WeaponHolder>(true);
                if (holder != null) SetDefaultHandPosition(holder);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (WeaponHolder holder in Object.FindObjectsByType<WeaponHolder>(FindObjectsInactive.Include))
        {
            Undo.RecordObject(holder.transform, "Move default weapon anchor to waist");
            SetDefaultHandPosition(holder);
            if (PrefabUtility.IsPartOfPrefabInstance(holder.transform))
                PrefabUtility.RecordPrefabInstancePropertyModifications(holder.transform);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("WEAPON HOLDING FIXED: PPU-independent size, authored grip/angle/muzzle, original scene players retained.");
    }

    private static void SetDefaultHandPosition(WeaponHolder holder)
    {
        // Respect deliberately authored anchors; only replace the old zero-origin default.
        if (holder.transform.localPosition == Vector3.zero)
            holder.transform.localPosition = new Vector3(0, -0.12f, 0);
    }

    public static void ConfigureWeapon(GameObject root)
    {
        SpriteRenderer visual = root.GetComponentInChildren<SpriteRenderer>(true);
        if (visual == null) throw new InvalidOperationException("Weapon has no visual: " + root.name);
        WeaponGrip grip = root.GetComponent<WeaponGrip>();
        if (grip == null) grip = root.AddComponent<WeaponGrip>();
        RangedWeaponController ranged = root.GetComponent<RangedWeaponController>();
        bool book = ranged != null && ranged.weaponData.FireMode == WeaponFireMode.Magazine;
        bool bow = ranged != null && ranged.weaponData.FireMode == WeaponFireMode.Bow;
        bool crossbow = ranged != null && ranged.weaponData.FireMode == WeaponFireMode.Crossbow;
        SerializedObject so = new SerializedObject(grip);
        so.FindProperty("visual").objectReferenceValue = visual;
        so.FindProperty("visualSize").floatValue = book ? 0.34f : ranged != null ? 0.4f : 0.52f;
        so.FindProperty("spriteAngle").floatValue = book ? 0 : bow ? -135 : crossbow ? -45 : -90;
        so.FindProperty("holdOffset").vector2Value = new Vector2(book ? 0.3f : bow ? 0.28f : crossbow ? 0.18f : 0.12f, 0);
        so.FindProperty("spriteGrip").vector2Value = crossbow ? new Vector2(0.3f, 0.3f) : ranged != null ? new Vector2(0.5f, 0.5f) : new Vector2(0.5f, 0.15f);
        so.FindProperty("muzzleOffset").vector2Value = new Vector2(book ? 0.52f : crossbow ? 0.62f : 0.55f, 0);
        so.ApplyModifiedPropertiesWithoutUndo();
        grip.Apply();
    }

    private static void Backup(string path)
    {
        string destination = "Captures/WeaponHolder/Before/" + path;
        if (!File.Exists(path)) throw new FileNotFoundException(path);
        if (File.Exists(destination)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(path, destination);
    }
}
