using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SoulCurrencySetup
{
    public static readonly Dictionary<string, int> Rewards = new Dictionary<string, int>
    {
        { "Assets/Prefab/ReferenceEnemies/Slime.prefab", 1 },
        { "Assets/Prefab/ReferenceEnemies/Skeleton.prefab", 2 },
        { "Assets/Prefab/ReferenceEnemies/Spider.prefab", 3 },
        { "Assets/Prefab/ReferenceEnemies/Wolf.prefab", 4 },
        { "Assets/Prefab/ReferenceEnemies/Snow/IceSlime.prefab", 2 },
        { "Assets/Prefab/ReferenceEnemies/Snow/SnowWolf.prefab", 5 },
        { "Assets/Prefab/ReferenceEnemies/Snow/Snowman.prefab", 6 },
        { "Assets/Prefab/Boss/Rotwood.prefab", 100 }
    };
    private static readonly string[] Scenes = { LoadoutLauncher.SelectionScene, LoadoutLauncher.GameplayScene, LoadoutLauncher.SnowScene };
    private static readonly Color Ink = new Color32(12, 28, 40, 255), Teal = new Color32(104, 224, 207, 255);

    [MenuItem("Tools/System Leveling/Configure Soul Currency")]
    public static void Configure()
    {
        Require(!Application.isPlaying, "Configure Soul outside Play Mode.");
        Sprite soul = AssetDatabase.LoadAllAssetsAtPath("Assets/MyAssets/Other/Drop/Soul.png").OfType<Sprite>().FirstOrDefault();
        Require(soul != null, "Soul sprite is not imported.");
        foreach (string path in Rewards.Keys) Require(AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<EnemyStats>() != null, "Missing enemy: " + path);
        foreach (string path in Scenes)
        {
            Backup(path);
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.isLoaded && loaded.isDirty)
            {
                string live = "Captures/Soul/Before/Live-" + loaded.name + ".unity";
                if (!File.Exists(live)) Require(EditorSceneManager.SaveScene(loaded, live, true), "Cannot preserve live scene edits.");
            }
        }
        // Preflight all scenes before any rewards/UI are written; never overwrite a completed migration.
        foreach (string path in Scenes)
        {
            Scene scene = Open(path); bool close = path != LoadoutLauncher.SelectionScene && scene != SceneManager.GetActiveScene();
            try { Require(!scene.GetRootGameObjects().Any(root => root.GetComponentInChildren<SoulBalanceView>(true) != null), "Soul UI already exists: " + path); }
            finally { if (close && !scene.isDirty) EditorSceneManager.CloseScene(scene, true); }
        }
        foreach (var entry in Rewards)
        {
            Backup(entry.Key);
            GameObject root = PrefabUtility.LoadPrefabContents(entry.Key);
            try
            {
                EnemyStats stats = root.GetComponent<EnemyStats>(); SerializedObject so = new SerializedObject(stats);
                so.FindProperty("soulReward").intValue = entry.Value; so.ApplyModifiedPropertiesWithoutUndo();
                if (PrefabUtility.IsPartOfPrefabInstance(stats)) PrefabUtility.RecordPrefabInstancePropertyModifications(stats);
                Require(PrefabUtility.SaveAsPrefabAsset(root, entry.Key) != null, "Cannot save reward: " + entry.Key);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(entry.Key).GetComponent<EnemyStats>().SoulReward == entry.Value, "Reward read-back failed.");
        }
        foreach (string path in Scenes)
        {
            Scene scene = Open(path);
            if (path == LoadoutLauncher.SelectionScene)
            {
                LoadoutLauncher launcher = Find<LoadoutLauncher>(scene);
                Counter(launcher.loadoutPage.transform, soul, 930, 540);
                Counter(launcher.mapPage.transform, soul, 930, 24);
            }
            else
            {
                Transform hud = Find<PlayerHud>(scene).transform.Find("HUD");
                RectTransform counter = Counter(hud, soul, 0, 0);
                counter.anchorMin = counter.anchorMax = counter.pivot = Vector2.one;
                counter.anchoredPosition = new Vector2(-144, -24);
            }
            EditorSceneManager.MarkSceneDirty(scene); Require(EditorSceneManager.SaveScene(scene), "Cannot save Soul UI: " + path);
            if (path != LoadoutLauncher.SelectionScene) EditorSceneManager.CloseScene(scene, true);
        }
        SceneManager.SetActiveScene(SceneManager.GetSceneByPath(LoadoutLauncher.SelectionScene));
        Directory.CreateDirectory("Captures/Soul");
        File.WriteAllText("Captures/Soul/setup.txt", "PASS: 8 prefab rewards; run Soul on both gameplay maps, total Soul on character/weapon and map selection. Main Menu has no Soul display. No upgrade/spending UI added.");
        Debug.Log("SOUL READY: persistent local wallet, per-prefab rewards, live totals on HUD/selection/menu. No upgrades implemented.");
    }

    private static RectTransform Counter(Transform parent, Sprite icon, float x, float y)
    {
        RectTransform root = Rect("Soul Balance", parent, x, y, 286, 68);
        Image panel = root.gameObject.AddComponent<Image>(); panel.color = Ink; panel.raycastTarget = false;
        panel.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MyAssets/UI/Transparent border/panel-transparent-border-015.png"); panel.type = Image.Type.Sliced;
        RectTransform edge = Rect("UI Border", root, 0, 0, 0, 0); edge.anchorMin = Vector2.zero; edge.anchorMax = Vector2.one; edge.offsetMin = edge.offsetMax = Vector2.zero;
        Image border = edge.gameObject.AddComponent<Image>(); border.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MyAssets/UI/Border/panel-border-005.png"); border.type = Image.Type.Sliced; border.color = Teal; border.raycastTarget = false;
        Image image = Rect("Soul Icon", root, 12, 10, 28, 48).gameObject.AddComponent<Image>(); image.sprite = icon; image.preserveAspect = true; image.raycastTarget = false;
        SoulBalanceView view = root.gameObject.AddComponent<SoulBalanceView>();
        view.caption = Label("Caption", root, "SOUL", 11, Teal, 54, 6, 220, 20);
        view.amount = Label("Amount", root, "0", 24, Color.white, 54, 26, 220, 34);
        return root;
    }
    private static Text Label(string name, Transform parent, string value, int size, Color color, float x, float y, float width, float height)
    {
        Text text = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size; text.color = color; text.text = value; text.raycastTarget = false; text.alignment = TextAnchor.MiddleLeft; return text;
    }
    private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
    }
    private static Scene Open(string path) { Scene scene = SceneManager.GetSceneByPath(path); return scene.isLoaded ? scene : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
    private static void Backup(string path) { string copy = "Captures/Soul/Before/" + path; if (File.Exists(copy)) return; Directory.CreateDirectory(Path.GetDirectoryName(copy)); File.Copy(path, copy); }
    private static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}
