using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class WeaponEquippingSetup
{
    [MenuItem("Tools/System Leveling/Configure Neutral Weapon Holders")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Migrate outside Play Mode.");
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/SampleScene.unity") throw new InvalidOperationException("Open the gameplay scene first.");
        Backup(scene.path);
        foreach (string name in new[] { "Archer", "Hunter", "Knight" }) Backup($"Assets/Prefab/Characters/{name}.prefab");
        foreach (string name in new[] { "MagicBook", "Bow", "Crossbow", "Sword" }) Backup($"Assets/Prefab/Weapons/{name}.prefab");
        Backup("Assets/Prefab/WeaponHolder.prefab");
        const string liveSceneBackup = "Captures/WeaponEquipping/Before/Live-SampleScene.unity";
        if (!File.Exists(liveSceneBackup))
        {
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not preserve the current live scene.");
            File.Copy(scene.path, liveSceneBackup);
        }

        // Reuse the same PPU-independent calibration; no damage/ammo/default data is rewritten.
        WeaponHolderSetup.Configure();
        foreach (string name in new[] { "Archer", "Hunter", "Knight" })
        {
            string path = $"Assets/Prefab/Characters/{name}.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                WeaponHolder[] holders = root.GetComponentsInChildren<WeaponHolder>(true);
                if (holders.Length != 1) throw new InvalidOperationException(name + " must have one hand anchor.");
                ValidateDefault(root.GetComponent<PlayerStats>());
                WeaponAmmoSetup.ClearWeapons(holders[0]);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        foreach (WeaponHolder holder in Object.FindObjectsByType<WeaponHolder>(FindObjectsInactive.Include))
        {
            PlayerStats stats = holder.GetComponentInParent<PlayerStats>();
            if (stats == null || holder.gameObject.scene != scene) continue;
            ValidateDefault(stats);
            Undo.RegisterFullObjectHierarchyUndo(holder.gameObject, "Use runtime weapon selection");
            WeaponAmmoSetup.ClearWeapons(holder);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("NEUTRAL WEAPON HOLDERS READY: character/scene anchors retained; defaults come from CharacterData and selections use Equip(prefab).");
    }

    private static void ValidateDefault(PlayerStats stats)
    {
        GameObject prefab = stats != null && stats.characterData != null ? stats.characterData.StartingWeapon : null;
        if (prefab == null || !prefab.TryGetComponent(out WeaponController weapon) || weapon.weaponData == null ||
            !prefab.TryGetComponent(out WeaponGrip grip) || !grip.IsConfigured)
            throw new InvalidOperationException("Missing valid default weapon for " + (stats != null ? stats.name : "character"));
    }

    private static void Backup(string path)
    {
        string destination = "Captures/WeaponEquipping/Before/" + path;
        if (File.Exists(destination)) return;
        if (!File.Exists(path)) throw new FileNotFoundException(path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(path, destination);
    }
}
