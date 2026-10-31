using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class LoadoutGameplaySetup
{
    [MenuItem("Tools/System Leveling/Connect Loadout to Gameplay")]
    public static void Configure()
    {
        Require(!Application.isPlaying, "Connect scenes outside Play Mode.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            Require(!SceneManager.GetSceneAt(i).isDirty, "Save open scenes before connecting gameplay.");
        LoadoutSelection ui = Object.FindAnyObjectByType<LoadoutSelection>();
        Require(ui != null && ui.gameObject.scene.path == LoadoutLauncher.SelectionScene, "Open LoadoutSelection scene first.");
        LoadoutSelection.ValidatePrefabs(ui.characterPrefabs, ui.weaponPrefabs);
        foreach (string path in new[] { LoadoutLauncher.SelectionScene, LoadoutLauncher.GameplayScene, "ProjectSettings/EditorBuildSettings.asset" })
            Backup(path);

        Scene gameplay = EditorSceneManager.OpenScene(LoadoutLauncher.GameplayScene, OpenSceneMode.Additive);
        try
        {
            PlayerStats[] players = gameplay.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerStats>(true)).ToArray();
            Require(players.Length == 1, "Gameplay scene must have exactly one PlayerStats.");
            PlayerStats player = players[0];
            Require(player.GetComponent<SpriteRenderer>() != null && player.GetComponent<Animator>() != null && player.GetComponent<CapsuleCollider2D>() != null &&
                player.GetComponentInChildren<WeaponHolder>(true) != null, "Gameplay Player is missing required components.");
            if (player.GetComponent<GameplayLoadout>() == null) Undo.AddComponent<GameplayLoadout>(player.gameObject);
            EditorSceneManager.MarkSceneDirty(gameplay);
            Require(EditorSceneManager.SaveScene(gameplay), "Could not save gameplay scene.");
        }
        finally { EditorSceneManager.CloseScene(gameplay, true); }

        if (ui.GetComponent<LoadoutLauncher>() == null) Undo.AddComponent<LoadoutLauncher>(ui.gameObject);
        Text notice = ui.transform.Find("Safe Area/Preview Notice").GetComponent<Text>();
        notice.text = "SELECT A CHARACTER / SELECT A WEAPON / ENTER THE ARENA";
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        Require(EditorSceneManager.SaveScene(ui.gameObject.scene), "Could not save selection scene.");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(LoadoutLauncher.SelectionScene, true),
            new EditorBuildSettingsScene(LoadoutLauncher.GameplayScene, true)
        }.Concat(EditorBuildSettings.scenes.Where(s => s.path != LoadoutLauncher.SelectionScene && s.path != LoadoutLauncher.GameplayScene)).ToArray();
        Require(EditorBuildSettings.scenes[0].enabled && EditorBuildSettings.scenes[0].path == LoadoutLauncher.SelectionScene, "Selection entry scene was not configured.");
        Debug.Log("LOADOUT GAMEPLAY CONNECTED: selection is the build entry scene; direct weapon click launches SampleScene with the chosen character and weapon. Restart retains the loadout.");
    }

    public static void EnsureDefaultCharacterPrefab()
    {
        Scene gameplay = SceneManager.GetSceneByPath(LoadoutLauncher.GameplayScene);
        bool opened = !gameplay.isLoaded;
        if (opened) gameplay = EditorSceneManager.OpenScene(LoadoutLauncher.GameplayScene, OpenSceneMode.Additive);
        try
        {
            PlayerStats player = gameplay.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PlayerStats>(true)).Single();
            string path = $"Assets/Prefab/Characters/{player.characterData.name}.prefab";
            if (File.Exists(path)) return;
            GameObject copy = Object.Instantiate(player.gameObject);
            try
            {
                copy.name = player.characterData.name;
                copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                GameplayLoadout applier = copy.GetComponent<GameplayLoadout>();
                if (applier != null) Object.DestroyImmediate(applier);
                Require(PrefabUtility.SaveAsPrefabAsset(copy, path) != null, "Could not create the scene character prefab.");
            }
            finally { Object.DestroyImmediate(copy); }
        }
        finally { if (opened) EditorSceneManager.CloseScene(gameplay, true); }
    }

    private static void Backup(string path)
    {
        string destination = "Captures/LoadoutGameplay/Before/" + path;
        if (File.Exists(destination)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(path, destination);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
