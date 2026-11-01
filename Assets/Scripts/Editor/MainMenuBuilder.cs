using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class MainMenuBuilder
{
    private static readonly Color Ink = new Color32(12, 28, 38, 255);
    private static readonly Color Cream = new Color32(241, 241, 218, 255);
    private static readonly Color Teal = new Color32(104, 224, 207, 255);
    private static readonly Color Muted = new Color32(185, 206, 206, 255);
    private static Font font;

    [MenuItem("Tools/System Leveling/Build Main Menu")]
    public static void Build()
    {
        Require(!Application.isPlaying && !File.Exists(MainMenu.ScenePath), "Build outside Play Mode; MainMenu scene must not already exist.");
        for (int i = 0; i < SceneManager.sceneCount; i++) Require(!SceneManager.GetSceneAt(i).isDirty, "Save open scenes first.");
        LoadoutSelection selection = Object.FindAnyObjectByType<LoadoutSelection>();
        Require(selection != null && selection.gameObject.scene.path == LoadoutLauncher.SelectionScene, "Open LoadoutSelection first.");
        const string backgroundPath = "Assets/MyAssets/UI/Background/Background.png";
        Require(File.Exists(backgroundPath), "Missing menu background: " + backgroundPath);
        foreach (string path in new[] { LoadoutLauncher.SelectionScene, LoadoutLauncher.GameplayScene, "ProjectSettings/EditorBuildSettings.asset" }) Backup(path);

        selection.back.gameObject.SetActive(true);
        EditorSceneManager.MarkSceneDirty(selection.gameObject.scene);
        Require(EditorSceneManager.SaveScene(selection.gameObject.scene), "Could not save selection Back navigation.");
        AddGameOverMenuButton();
        Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(menuScene);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Texture2D background = AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath);
        Require(background != null, "Background texture could not be loaded.");
        GameObject camera = new GameObject("Menu Camera", typeof(Camera)); camera.tag = "MainCamera";
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; camera.GetComponent<Camera>().backgroundColor = Ink;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        GameObject root = new GameObject("Main Menu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0;
        RawImage art = Stretch("Background Art", root.transform).gameObject.AddComponent<RawImage>();
        art.texture = background; art.raycastTarget = false;
        art.rectTransform.pivot = new Vector2(1, 0.5f);
        AspectRatioFitter cover = art.gameObject.AddComponent<AspectRatioFitter>();
        cover.aspectRatio = (float)background.width / background.height; cover.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        RawImage shade = Stretch("Left Readability Gradient", root.transform).gameObject.AddComponent<RawImage>();
        shade.texture = Shade(); shade.raycastTarget = false;
        RectTransform page = Rect("Safe Area", root.transform, 0, 0, 1280, 720);
        page.anchorMin = page.anchorMax = Vector2.one * 0.5f; page.anchoredPosition = new Vector2(-640, 360);
        MainMenu menu = root.AddComponent<MainMenu>();
        Label("Brand", page, "SYSTEM / LEVELING", 13, Muted, 96, 40, 480, 24, FontStyle.Bold);
        Label("Eyebrow", page, "ENTER THE UNKNOWN", 12, Teal, 100, 111, 480, 24, FontStyle.Bold);
        Text title = Label("Title", page, "SYSTEM\nLEVELING", 72, Cream, 96, 146, 550, 178, FontStyle.Bold);
        title.lineSpacing = 0.9f;
        Shadow shadow = title.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0.02f, 0.08f, 0.12f, 0.7f); shadow.effectDistance = new Vector2(2, -2);
        Label("Description", page, "Step through the gate.\nSurvive. Grow stronger. Begin again.", 17, Cream, 100, 340, 510, 58);
        Box("Accent", page, 96, 415, 48, 3, Teal);
        Box("Accent Trail", page, 156, 416, 280, 1, new Color(0.8f, 0.95f, 0.9f, 0.2f));
        RectTransform buttons = Rect("Actions", page, 96, 442, 340, 180); menu.buttons = buttons.gameObject.AddComponent<CanvasGroup>();
        menu.startButton = Button("Start Run", buttons, "START RUN     >", 0, 0, 340, 60, Teal, Ink, 18);
        menu.controlsButton = Button("Controls", buttons, "CONTROLS", 0, 72, 340, 48, new Color(0.045f, 0.11f, 0.15f, 0.78f), Cream, 15);
        menu.quitButton = Button("Quit", buttons, "QUIT", 0, 132, 340, 48, new Color(0.045f, 0.11f, 0.15f, 0.78f), Muted, 15);
        menu.status = Label("Status", page, "YOUR NEXT RUN STARTS HERE", 11, Muted, 100, 640, 620, 24);
        Label("Footer", page, "WASD TO MOVE  /  MOUSE TO AIM", 10, Muted, 100, 686, 650, 20);
        BuildControls(menu, root.transform);

        Require(EditorSceneManager.SaveScene(menuScene, MainMenu.ScenePath), "Could not save MainMenu scene.");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenu.ScenePath, true),
            new EditorBuildSettingsScene(LoadoutLauncher.SelectionScene, true),
            new EditorBuildSettingsScene(LoadoutLauncher.GameplayScene, true)
        }.Concat(EditorBuildSettings.scenes.Where(s => s.path != MainMenu.ScenePath && s.path != LoadoutLauncher.SelectionScene && s.path != LoadoutLauncher.GameplayScene)).ToArray();
        Require(EditorSceneManager.SaveScene(menuScene), "Could not save menu configuration.");
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene != menuScene) EditorSceneManager.CloseScene(scene, true);
        }
        SceneManager.SetActiveScene(menuScene);
        Selection.activeGameObject = root;
        Debug.Log("MAIN MENU READY: Start -> character -> weapon -> gameplay; Back returns through selection; Game Over offers Restart or Main Menu. Existing background and catalogs retained.");
    }

    private static void BuildControls(MainMenu menu, Transform parent)
    {
        RectTransform overlay = Stretch("Controls Panel", parent);
        Image dim = overlay.gameObject.AddComponent<Image>(); dim.color = new Color(0.02f, 0.06f, 0.08f, 0.8f);
        RectTransform window = Box("Controls Window", overlay, 0, 0, 720, 470, Ink);
        window.anchorMin = window.anchorMax = Vector2.one * 0.5f; window.anchoredPosition = new Vector2(-360, 235);
        Box("Top Accent", window, 32, 30, 44, 3, Teal);
        Label("Heading", window, "HOW TO PLAY", 30, Cream, 32, 54, 656, 50, FontStyle.Bold);
        Label("Keys", window, "WASD\n\nMOUSE\n\nLEFT CLICK\n\nHOLD + RELEASE\n\nLEVEL UP", 15, Teal, 32, 128, 210, 240, FontStyle.Bold);
        Label("Actions", window, "Move your character\n\nAim your weapon\n\nAttack / hold for ranged fire\n\nCharge and fire the crossbow\n\nChoose an upgrade to grow stronger", 15, Cream, 260, 128, 420, 240);
        menu.closeControlsButton = Button("Close Controls", window, "<  BACK", 32, 390, 656, 48, new Color32(27, 63, 74, 255), Cream, 15);
        menu.controlsPanel = overlay.gameObject; overlay.gameObject.SetActive(false);
    }

    private static void AddGameOverMenuButton()
    {
        Scene gameplay = EditorSceneManager.OpenScene(LoadoutLauncher.GameplayScene, OpenSceneMode.Additive);
        try
        {
            GameOverController gameOver = gameplay.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameOverController>(true)).Single();
            SerializedObject so = new SerializedObject(gameOver);
            Button restart = (Button)so.FindProperty("restartButton").objectReferenceValue;
            Require(restart != null, "Game Over Restart button is missing.");
            RectTransform retry = restart.GetComponent<RectTransform>();
            retry.anchoredPosition = new Vector2(-144, retry.anchoredPosition.y); retry.sizeDelta = new Vector2(260, 58);
            Text label = restart.GetComponentInChildren<Text>(); label.rectTransform.sizeDelta = new Vector2(240, 42); label.fontSize = 18;
            Button menu = (Button)so.FindProperty("mainMenuButton").objectReferenceValue;
            if (menu == null)
            {
                GameObject copy = Object.Instantiate(restart.gameObject, restart.transform.parent, false); copy.name = "Main Menu Button";
                menu = copy.GetComponent<Button>();
                menu.GetComponent<RectTransform>().anchoredPosition = new Vector2(144, retry.anchoredPosition.y);
                menu.GetComponentInChildren<Text>().text = "MAIN MENU";
                so.FindProperty("mainMenuButton").objectReferenceValue = menu; so.ApplyModifiedPropertiesWithoutUndo();
            }
            if (PrefabUtility.IsPartOfPrefabInstance(gameOver)) PrefabUtility.RecordPrefabInstancePropertyModifications(gameOver);
            if (PrefabUtility.IsPartOfPrefabInstance(retry)) PrefabUtility.RecordPrefabInstancePropertyModifications(retry);
            if (PrefabUtility.IsPartOfPrefabInstance(label)) PrefabUtility.RecordPrefabInstancePropertyModifications(label);
            if (PrefabUtility.IsPartOfPrefabInstance(label.rectTransform)) PrefabUtility.RecordPrefabInstancePropertyModifications(label.rectTransform);
            EditorSceneManager.MarkSceneDirty(gameplay);
            Require(EditorSceneManager.SaveScene(gameplay), "Could not save Game Over navigation.");
        }
        finally { EditorSceneManager.CloseScene(gameplay, true); }
    }

    private static Texture2D Shade()
    {
        const string path = "Assets/MyAssets/UI/MainMenu/menu-shade.png";
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Texture2D texture = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            for (int i = 0; i < 128; i++) texture.SetPixel(i, 0, new Color(0.035f, 0.08f, 0.11f, 0.55f * (1 - Mathf.SmoothStep(0, 1, i / 127f / 0.68f))));
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.mipmapEnabled = false; importer.wrapMode = TextureWrapMode.Clamp; importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
    {
        RectTransform r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1); r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(w, h); return r;
    }
    private static RectTransform Stretch(string name, Transform parent)
    {
        RectTransform r = Rect(name, parent, 0, 0, 0, 0); r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero; return r;
    }
    private static RectTransform Box(string name, Transform parent, float x, float y, float w, float h, Color color)
    {
        RectTransform r = Rect(name, parent, x, y, w, h); Image image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return r;
    }
    private static Text Label(string name, Transform parent, string text, int size, Color color, float x, float y, float w, float h, FontStyle style = FontStyle.Normal)
    {
        Text t = Rect(name, parent, x, y, w, h).gameObject.AddComponent<Text>(); t.font = font; t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = color; t.raycastTarget = false; return t;
    }
    private static Button Button(string name, Transform parent, string text, float x, float y, float w, float h, Color background, Color foreground, int size)
    {
        RectTransform r = Box(name, parent, x, y, w, h, background); Image image = r.GetComponent<Image>(); image.raycastTarget = true;
        Button button = r.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        ColorBlock colors = button.colors; colors.highlightedColor = colors.selectedColor = new Color(1.18f, 1.18f, 1.18f); colors.pressedColor = new Color(0.8f, 0.95f, 0.95f); button.colors = colors;
        Text label = Label("Label", r, text, size, foreground, 20, 0, w - 40, h, FontStyle.Bold); label.alignment = TextAnchor.MiddleLeft; return button;
    }
    private static void Backup(string path)
    {
        string destination = "Captures/MainMenu/Before/" + path;
        if (File.Exists(destination)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(path, destination);
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
