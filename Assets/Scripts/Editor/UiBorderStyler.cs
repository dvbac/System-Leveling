using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Scene/prefab authoring only: no runtime dependency or changes to navigation.
public static class UiBorderStyler
{
    private const string AssetsRoot = "Assets/MyAssets/UI/";
    private static readonly Color Ink = new Color32(12, 28, 40, 255);
    private static readonly Color Teal = new Color32(104, 224, 207, 255);
    private static readonly Color Gold = new Color32(234, 199, 132, 255);
    private static readonly Color Muted = new Color32(101, 140, 157, 255);
    private static readonly string[] Art = {
        "Border/panel-border-005.png", "Border/panel-border-009.png",
        "Border/panel-border-014.png", "Border/panel-border-028.png",
        "Border/panel-border-002.png", "Border/panel-border-022.png",
        "Panel/panel-014.png", "Transparent center/panel-transparent-center-005.png",
        "Transparent border/panel-transparent-border-015.png",
        "Divider/divider-004.png", "Divider Fade/divider-fade-003.png"
    };

    [MenuItem("Tools/System Leveling/Apply UI Border Styles")]
    public static void Apply()
    {
        Require(!Application.isPlaying, "Exit Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            Require(!SceneManager.GetSceneAt(i).isDirty, "Save open scenes first.");
        string original = SceneManager.GetActiveScene().path;
        Require(!string.IsNullOrEmpty(original), "Open a saved scene first.");
        string[] scenes = { MainMenu.ScenePath, LoadoutLauncher.SelectionScene, LoadoutLauncher.GameplayScene };
        const string hudPath = "Assets/Prefab/UI/System HUD.prefab";
        foreach (string path in scenes.Concat(new[] { hudPath }).Concat(Art.Select(p => AssetsRoot + p + ".meta")))
            Backup(path);
        foreach (string path in Art) Import(path);

        GameObject prefab = PrefabUtility.LoadPrefabContents(hudPath);
        try { Hud(prefab); PrefabUtility.SaveAsPrefabAsset(prefab, hudPath); }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        try
        {
            foreach (string path in scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (path == MainMenu.ScenePath) Menu(Object.FindAnyObjectByType<MainMenu>());
                else if (path == LoadoutLauncher.SelectionScene) Selection(Object.FindAnyObjectByType<LoadoutSelection>());
                else Hud(Object.FindAnyObjectByType<PlayerHud>(FindObjectsInactive.Include).gameObject);
                foreach (GameObject root in scene.GetRootGameObjects())
                    foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (PrefabUtility.IsPartOfPrefabInstance(image)) PrefabUtility.RecordPrefabInstancePropertyModifications(image);
                        if (image.name == "UI Border")
                            Require(!image.raycastTarget && image.sprite != null && image.sprite.border.sqrMagnitude > 0,
                                "Decorative border must be sliced and must not block input: " + image.transform.parent.name);
                    }
                EditorSceneManager.MarkSceneDirty(scene);
                Require(EditorSceneManager.SaveScene(scene), "Could not save " + path);
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Captures/UiBorders");
            File.WriteAllText("Captures/UiBorders/style-check.txt", "PASS: 3 scenes and shared HUD styled; sliced decorative borders do not block raycasts.\n" + string.Join("\n", Art));
            Debug.Log("UI BORDER STYLES PASS: menu, selection, shared HUD, upgrades and Game Over; circular slots and gameplay bindings retained.");
        }
        finally { EditorSceneManager.OpenScene(original, OpenSceneMode.Single); }
    }

    private static void Menu(MainMenu menu)
    {
        Require(menu != null, "Missing MainMenu.");
        Button(menu.startButton, "005", Teal, new Color32(24, 75, 77, 255));
        menu.startButton.GetComponentInChildren<Text>().color = new Color32(241, 241, 218, 255);
        Button(menu.controlsButton, "009", Muted, Ink);
        Button(menu.quitButton, "009", Muted, Ink);
        Button(menu.closeControlsButton, "005", Teal, Ink);
        Transform window = menu.controlsPanel.transform.Find("Controls Window");
        Surface(window, "Panel/panel-014.png", Ink);
        Edge(window, "014", Teal);
        Divider(menu.transform.Find("Safe Area/Accent Trail"), true, Gold, 8);
    }

    private static void Selection(LoadoutSelection ui)
    {
        Require(ui != null && ui.optionPrefab != null, "Missing dynamic selection.");
        Button(ui.back, "009", Teal, Ink);
        Divider(ui.transform.Find("Safe Area/Top Divider"), false, Muted, 7);
        foreach (LoadoutSelection.View view in new[] { ui.characterView, ui.weaponView })
        {
            Transform root = view.root.transform;
            Image stats = Box(root, "Stats Frame", 904, 132, 330, 390);
            Surface(stats.transform, "Transparent border/panel-transparent-border-015.png", Ink);
            Edge(stats.transform, "028", Gold);
            Image list = Box(root, "Catalog Frame", 48, 258, 704, 342);
            Surface(list.transform, "Transparent center/panel-transparent-center-005.png", new Color32(24, 44, 57, 255));
            Edge(list.transform, "005", Muted);
            Divider(root.Find("Stats Rule"), true, Gold, 8);
        }
        // Keep the shared circular hit target/ring: runtime highlighting recolors both.
        Require(ui.optionPrefab.button.image.sprite != null && ui.optionPrefab.ring.sprite != null,
            "Circular option prefab was not retained.");
    }

    private static void Hud(GameObject root)
    {
        foreach (Image old in root.GetComponentsInChildren<Image>(true))
            if (old.name == "Fantasy Border") old.enabled = false;
        Panel(root, "HUD/Health Panel", "005", Teal);
        Panel(root, "HUD/Level Badge", "002", Gold);
        Panel(root, "Experience Panel", "009", Muted);
        Panel(root, "Level Up Modal/Upgrade Window", "014", Teal);
        Panel(root, "Game Over Modal/Game Over Window", "028", Gold);
        string[] frames = { "005", "022", "002" };
        Color[] accents = { Teal, Gold, new Color32(157, 182, 238, 255) };
        for (int i = 0; i < 3; i++)
        {
            Transform card = root.transform.Find("Level Up Modal/Upgrade Window/Upgrade Card " + (i + 1));
            Require(card != null, "Missing upgrade card.");
            Button(card.GetComponent<Button>(), frames[i], accents[i], Ink);
        }
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            if (button.name == "Restart Button") Button(button, "005", Teal, new Color32(24, 61, 68, 255));
            else if (button.name == "Main Menu Button") Button(button, "009", Gold, Ink);
        }
    }

    private static void Panel(GameObject root, string path, string frame, Color accent)
    {
        Transform target = root.transform.Find(path);
        Require(target != null, "Missing UI panel: " + path);
        Surface(target, "Transparent border/panel-transparent-border-015.png", Ink);
        Edge(target, frame, accent);
    }

    private static void Button(Button button, string frame, Color accent, Color fill)
    {
        Require(button != null, "Missing button.");
        Surface(button.transform, "Transparent center/panel-transparent-center-005.png", fill);
        button.image.raycastTarget = true;
        Edge(button.transform, frame, accent);
    }

    private static void Surface(Transform target, string sprite, Color color)
    {
        Image image = target.GetComponent<Image>();
        Require(image != null, "Missing Image: " + target.name);
        image.sprite = Sprite(sprite); image.type = Image.Type.Sliced; image.color = color;
        image.pixelsPerUnitMultiplier = 1;
    }

    private static void Edge(Transform parent, string id, Color color)
    {
        Transform existing = parent.Find("UI Border");
        Image image = existing != null ? existing.GetComponent<Image>() : new GameObject("UI Border", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        RectTransform rect = image.rectTransform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        image.sprite = Sprite("Border/panel-border-" + id + ".png"); image.type = Image.Type.Sliced;
        image.color = color; image.raycastTarget = false;
    }

    private static Image Box(Transform parent, string name, float x, float y, float w, float h)
    {
        Transform existing = parent.Find(name);
        Image image = existing != null ? existing.GetComponent<Image>() : new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false); image.transform.SetAsFirstSibling(); image.raycastTarget = false;
        RectTransform rect = image.rectTransform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
        return image;
    }

    private static void Divider(Transform target, bool fade, Color color, float height)
    {
        Require(target != null, "Missing divider.");
        Image image = target.GetComponent<Image>();
        image.sprite = Sprite(fade ? "Divider Fade/divider-fade-003.png" : "Divider/divider-004.png");
        image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = false;
        image.rectTransform.sizeDelta = new Vector2(image.rectTransform.sizeDelta.x, height);
    }

    private static Sprite Sprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetsRoot + path);
        Require(sprite != null, "Missing sprite: " + path); return sprite;
    }

    private static void Import(string path)
    {
        string full = AssetsRoot + path;
        Require(File.Exists(full), "Missing UI art: " + full);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(full);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spriteBorder = path.StartsWith("Divider") ? new Vector4(8, 0, 8, 0) : new Vector4(12, 12, 12, 12);
        importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static void Backup(string path)
    {
        Require(File.Exists(path), "Cannot back up missing file: " + path);
        string backup = "Captures/UiBorders/Before/" + path;
        if (File.Exists(backup)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path, backup);
    }
    private static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}
