using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MapSelectionSetup
{
    [MenuItem("Tools/System Leveling/Create Separate Maps and Selection")]
    public static void Configure()
    {
        Require(!Application.isPlaying && !File.Exists(LoadoutLauncher.SnowScene), "Run outside Play Mode; SnowScene must not already exist.");
        foreach (string path in new[] { LoadoutLauncher.GameplayScene, LoadoutLauncher.SelectionScene, "ProjectSettings/EditorBuildSettings.asset" }) Backup(path);
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isDirty || (scene.path != LoadoutLauncher.GameplayScene && scene.path != LoadoutLauncher.SelectionScene)) continue;
            string liveCopy = "Captures/MapSelection/Before/Live-" + scene.name + ".unity";
            if (!File.Exists(liveCopy)) Require(EditorSceneManager.SaveScene(scene, liveCopy, true), "Could not preserve unsaved scene edits.");
        }
        PropRandomizer[] snow = new[] { "Snow Chunk", "Snow Chunk 2", "Snow Chunk 3" }
            .Select(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Chunk/" + name + ".prefab")?.GetComponent<PropRandomizer>()).ToArray();
        GameObject[] enemies = new[] { "IceSlime", "SnowWolf", "Snowman" }
            .Select(name => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Snow/" + name + ".prefab")).ToArray();
        Require(snow.All(chunk => chunk != null && chunk.propPrefabs.Count > 0 && chunk.propPrefabs.All(prop => prop != null && AssetDatabase.GetAssetPath(prop).StartsWith("Assets/Prefab/Props/Snow/"))), "Configure the three snow chunk prop pools first.");
        Require(enemies.All(enemy => new WeightedEnemySpawn(enemy).IsUsable), "Snow enemy prefabs are incomplete.");

        Scene grass = Open(LoadoutLauncher.GameplayScene);
        MapController grassMap = Find<MapController>(grass);
        Require(grassMap.terrainChunks.Length > 0 && grassMap.terrainChunks.All(chunk => chunk != null), "Grass chunks are missing.");
        EditorSceneManager.MarkSceneDirty(grass);
        Require(EditorSceneManager.SaveScene(grass), "Could not save the grass map.");
        Require(AssetDatabase.CopyAsset(LoadoutLauncher.GameplayScene, LoadoutLauncher.SnowScene), "Could not create SnowScene.");
        Scene snowScene = Open(LoadoutLauncher.SnowScene);
        try
        {
            MapController map = Find<MapController>(snowScene); map.terrainChunks = snow;
            // Only remove copied ground chunks; never alter the user's grass scene or prefab assets.
            foreach (PropRandomizer chunk in snowScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PropRandomizer>(true)).ToArray())
                Object.DestroyImmediate(chunk.gameObject);
            EnemySpawner spawner = Find<EnemySpawner>(snowScene);
            SerializedObject so = new SerializedObject(spawner);
            SerializedProperty phases = so.FindProperty("phases");
            for (int i = 0; i < phases.arraySize; i++) Pool(phases.GetArrayElementAtIndex(i).FindPropertyRelative("enemies"), enemies);
            Pool(so.FindProperty("horde").FindPropertyRelative("enemies"), enemies);
            so.FindProperty("allowedGround").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();
            foreach (RotwoodEncounter encounter in snowScene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<RotwoodEncounter>(true))) encounter.enabled = false;
            EditorSceneManager.MarkSceneDirty(snowScene);
            Require(EditorSceneManager.SaveScene(snowScene), "Could not save snow map.");
        }
        finally { EditorSceneManager.CloseScene(snowScene, true); }

        Scene selection = Open(LoadoutLauncher.SelectionScene);
        LoadoutSelection ui = Find<LoadoutSelection>(selection);
        LoadoutLauncher launcher = ui.GetComponent<LoadoutLauncher>();
        Require(launcher != null, "Selection scene is not connected to gameplay.");
        Transform page = ui.transform.Find("Safe Area");
        Require(page != null && page.Find("Map Choice") == null, "Selection layout missing or map selector already exists.");
        GameObject group = new GameObject("Map Choice", typeof(RectTransform)); group.transform.SetParent(page, false);
        Place((RectTransform)group.transform, 930, 520, 286, 88);
        Text title = Object.Instantiate(ui.selectionCount, group.transform); title.name = "Map Heading";
        Place(title.rectTransform, 0, 0, 286, 22); title.text = "MAP / CHOOSE BEFORE START"; title.fontSize = 11; title.alignment = TextAnchor.MiddleLeft;
        launcher.grassMapButton = MapButton(ui.back, group.transform, "Grass Map", 0);
        launcher.snowMapButton = MapButton(ui.back, group.transform, "Snow Map", 150);
        launcher.grassMapButton.GetComponentInChildren<Text>().text = "GRASS\nSELECTED";
        launcher.snowMapButton.GetComponentInChildren<Text>().text = "SNOW\nSELECT MAP";
        EditorUtility.SetDirty(launcher);
        EditorSceneManager.MarkSceneDirty(selection);
        Require(EditorSceneManager.SaveScene(selection), "Could not save map selection.");
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Where(scene => scene.path != LoadoutLauncher.SnowScene)
            .Append(new EditorBuildSettingsScene(LoadoutLauncher.SnowScene, true)).ToArray();
        SceneManager.SetActiveScene(selection);
        EditorSceneManager.CloseScene(grass, true);
        Validate();
        Debug.Log("SEPARATE MAPS READY: choose GRASS or SNOW in loadout, then character/weapon; Restart reloads the chosen scene. No regional border remains.");
    }

    [MenuItem("Tools/System Leveling/Validate Separate Maps")]
    public static void Validate()
    {
        foreach (string path in new[] { LoadoutLauncher.GameplayScene, LoadoutLauncher.SnowScene })
        {
            Scene scene = SceneManager.GetSceneByPath(path); bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                bool snow = path == LoadoutLauncher.SnowScene;
                MapController map = Find<MapController>(scene); EnemySpawner spawner = Find<EnemySpawner>(scene);
                Require(map.terrainChunks.Length == 3 && map.terrainChunks.All(chunk => chunk != null && AssetDatabase.GetAssetPath(chunk).Contains(snow ? "/Snow Chunk" : "/Chunk")), "Wrong chunk pool: " + path);
                Require(map.terrainChunks.All(chunk => chunk.propPrefabs.All(prop => prop != null && AssetDatabase.GetAssetPath(prop).Contains(snow ? "/Props/Snow/" : "/Props/Grass/"))), "Wrong props: " + path);
                SerializedObject so = new SerializedObject(spawner);
                SerializedProperty phases = so.FindProperty("phases");
                for (int i = 0; i < phases.arraySize; i++) ValidatePool(phases.GetArrayElementAtIndex(i).FindPropertyRelative("enemies"), snow);
                ValidatePool(so.FindProperty("horde").FindPropertyRelative("enemies"), snow);
                Require(Find<PlayerStats>(scene).GetComponent<GameplayLoadout>() != null && Find<GameOverController>(scene) != null, "Loadout/Restart missing: " + path);
                Require(EditorBuildSettings.scenes.Any(entry => entry.path == path && entry.enabled), "Map missing from Build Settings: " + path);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        Directory.CreateDirectory("Captures/MapSelection");
        File.WriteAllText("Captures/MapSelection/assets.txt", "PASS: separate grass/snow scenes, 3 matching chunks each, own props and normal/Horde pools, loadout/GameOver components and enabled Build Settings.");
    }

    private static Button MapButton(Button source, Transform parent, string name, float x)
    {
        Button button = Object.Instantiate(source, parent); button.name = name; button.onClick = new Button.ButtonClickedEvent();
        Place(button.GetComponent<RectTransform>(), x, 28, 136, 56); button.gameObject.SetActive(true);
        Text label = button.GetComponentInChildren<Text>();
        label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
        label.fontSize = 13; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
        return button;
    }

    private static void Pool(SerializedProperty pool, GameObject[] enemies)
    {
        pool.arraySize = enemies.Length;
        for (int i = 0; i < enemies.Length; i++)
        {
            pool.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = enemies[i];
            pool.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue = i == 0 ? 2 : 1;
        }
    }
    private static void ValidatePool(SerializedProperty pool, bool snow)
    {
        Require(pool.arraySize > 0, "Empty map enemy pool.");
        bool usable = false;
        for (int i = 0; i < pool.arraySize; i++)
        {
            GameObject enemy = (GameObject)pool.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue;
            float weight = pool.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue;
            if (!new WeightedEnemySpawn(enemy, weight).IsUsable) continue; // Retired grass entries are already ignored by SpawnSelection.
            usable = true;
            Require(AssetDatabase.GetAssetPath(enemy).Contains("/Snow/") == snow, "Wrong map enemy: " + enemy);
        }
        Require(usable, "Map enemy pool has no usable entries.");
    }
    private static Scene Open(string path) { Scene scene = SceneManager.GetSceneByPath(path); return scene.isLoaded ? scene : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
    }
    private static void Backup(string path)
    {
        string backup = "Captures/MapSelection/Before/" + path;
        if (File.Exists(backup)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path, backup);
    }
    private static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
}
