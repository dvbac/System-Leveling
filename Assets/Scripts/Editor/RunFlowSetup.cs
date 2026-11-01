using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class RunFlowSetup
{
    private static readonly Color Ink = new Color32(12, 28, 40, 255), Teal = new Color32(104, 224, 207, 255), Gold = new Color32(234, 199, 132, 255);

    [MenuItem("Tools/System Leveling/Configure Map Screen and Run Menus")]
    public static void Configure()
    {
        Require(!Application.isPlaying, "Exit Play Mode first.");
        foreach (string path in new[] { LoadoutLauncher.SelectionScene, LoadoutLauncher.GameplayScene, LoadoutLauncher.SnowScene })
        {
            string backup = "Captures/RunFlow/Before/" + path;
            if (!File.Exists(backup)) { Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path, backup); }
            Scene loaded = SceneManager.GetSceneByPath(path);
            if (loaded.isLoaded && loaded.isDirty)
            {
                string live = "Captures/RunFlow/Before/Live-" + loaded.name + ".unity";
                if (!File.Exists(live)) Require(EditorSceneManager.SaveScene(loaded, live, true), "Cannot preserve unsaved edits.");
            }
        }
        Scene selection = Open(LoadoutLauncher.SelectionScene);
        LoadoutSelection ui = Find<LoadoutSelection>(selection); LoadoutLauncher launcher = ui.GetComponent<LoadoutLauncher>();
        Require(launcher.mapPage == null, "Map screen already exists; migration will not overwrite it.");
        Transform oldChoice = ui.transform.Find("Safe Area/Map Choice");
        Require(oldChoice != null, "Expected the existing compact map selector.");
        Button template = ui.back;
        launcher.loadoutPage = ui.transform.Find("Safe Area").gameObject;
        RectTransform page = Rect("Map Selection", ui.transform, 1280, 720);
        Center(page); launcher.mapPage = page.gameObject;
        Label("Brand", page, "SYSTEM / LEVELING", 18, Teal, 72, 24, 600, 32);
        Label("Heading", page, "CHOOSE YOUR FATE", 48, Color.white, 72, 96, 1136, 70);
        Label("Description", page, "Your character and weapon are ready. Select a map to begin.", 18, Gold, 72, 178, 1136, 36);
        launcher.mapStatus = Label("Loadout", page, "CHARACTER / WEAPON", 15, Teal, 72, 230, 1136, 32);
        launcher.grassMapButton = Button(template, page, "Grass Map", "GRASSLANDS\n\nFOREST CREATURES / ROTWOOD\n\nBEGIN RUN  >", 72, 298, 540, 252, 22);
        launcher.snowMapButton = Button(template, page, "Snow Map", "SNOWFIELDS\n\nICE SLIME / SNOW WOLF / SNOWMAN\n\nBEGIN RUN  >", 636, 298, 540, 252, 22);
        launcher.mapBackButton = Button(template, page, "Map Back", "<  BACK TO WEAPONS", 72, 622, 420, 52, 18);
        Object.DestroyImmediate(oldChoice.gameObject);
        page.gameObject.SetActive(false);
        EditorUtility.SetDirty(launcher); EditorSceneManager.MarkSceneDirty(selection);
        Require(EditorSceneManager.SaveScene(selection), "Cannot save map selection screen.");
        foreach (string path in new[] { LoadoutLauncher.GameplayScene, LoadoutLauncher.SnowScene })
        {
            Scene scene = Open(path);
            ConfigureGameplay(Find<GameOverController>(scene));
            EditorSceneManager.MarkSceneDirty(scene); Require(EditorSceneManager.SaveScene(scene), "Cannot save run menus: " + path);
            EditorSceneManager.CloseScene(scene, true);
        }
        SceneManager.SetActiveScene(selection);
        Debug.Log("RUN FLOW CONFIGURED: character > weapon > dedicated map screen; Esc pause, Quick Restart, Settings, Main Menu; death also offers Another Fate.");
    }

    private static void ConfigureGameplay(GameOverController gameOver)
    {
        Require(gameOver.GetComponent<RunMenu>() == null, "RunMenu already exists; do not overwrite manual edits.");
        SerializedObject so = new SerializedObject(gameOver);
        Button restart = (Button)so.FindProperty("restartButton").objectReferenceValue;
        Button main = (Button)so.FindProperty("mainMenuButton").objectReferenceValue;
        RectTransform deathWindow = (RectTransform)restart.transform.parent; deathWindow.sizeDelta = new Vector2(680, 600);
        Text result = (Text)so.FindProperty("resultText").objectReferenceValue;
        Place(result.rectTransform, 40, 150, 600, 100); result.alignment = TextAnchor.MiddleCenter;
        StyleButton(restart, "QUICK RESTART", 80, 270, 520, 52, 20);
        Button fate = Button(restart, deathWindow, "Another Fate Button", "ANOTHER FATE", 80, 334, 520, 52, 20);
        Button settings = Button(restart, deathWindow, "Settings Button", "SETTINGS", 80, 398, 520, 52, 20);
        StyleButton(main, "MAIN MENU", 80, 462, 520, 52, 20);
        Place(deathWindow.Find("Footer").GetComponent<RectTransform>(), 20, 552, 640, 28);
        so.FindProperty("anotherFateButton").objectReferenceValue = fate;
        so.FindProperty("settingsButton").objectReferenceValue = settings;
        so.ApplyModifiedPropertiesWithoutUndo();
        RunMenu menu = gameOver.gameObject.AddComponent<RunMenu>();
        RectTransform pause = Modal("Pause Menu", gameOver.transform, out RectTransform pauseWindow, 680, 560);
        menu.pausePanel = pause.gameObject; menu.pauseInput = pause.gameObject.AddComponent<CanvasGroup>();
        Label("Heading", pauseWindow, "PAUSED", 44, Color.white, 40, 40, 600, 68);
        Label("Subtitle", pauseWindow, "YOUR FATE CAN WAIT", 14, Teal, 40, 116, 600, 28);
        menu.continueButton = Button(restart, pauseWindow, "Continue", "CONTINUE", 80, 176, 520, 56, 20);
        menu.restartButton = Button(restart, pauseWindow, "Quick Restart", "QUICK RESTART", 80, 248, 520, 56, 20);
        menu.settingsButton = Button(restart, pauseWindow, "Settings", "SETTINGS", 80, 320, 520, 56, 20);
        menu.mainMenuButton = Button(restart, pauseWindow, "Main Menu", "MAIN MENU", 80, 392, 520, 56, 20);
        menu.status = Label("Status", pauseWindow, "ESC / CONTINUE YOUR RUN", 12, Gold, 40, 480, 600, 48);
        RectTransform setting = Modal("Settings Menu", gameOver.transform, out RectTransform settingsWindow, 680, 500);
        menu.settingsPanel = setting.gameObject; menu.settingsInput = setting.gameObject.AddComponent<CanvasGroup>();
        Label("Heading", settingsWindow, "SETTINGS", 44, Color.white, 40, 36, 600, 68);
        Label("Volume Label", settingsWindow, "MASTER VOLUME", 18, Teal, 60, 132, 400, 30);
        menu.volumeValue = Label("Volume Value", settingsWindow, "100%", 18, Gold, 500, 132, 120, 30);
        RectTransform slider = Rect("Volume", settingsWindow, 560, 36); Place(slider, 60, 184, 560, 36);
        Image track = slider.gameObject.AddComponent<Image>(); track.color = new Color32(37, 64, 75, 255);
        menu.volume = slider.gameObject.AddComponent<Slider>(); menu.volume.minValue = 0; menu.volume.maxValue = 1; menu.volume.value = 1;
        RectTransform fill = Rect("Fill", slider, 0, 0); Stretch(fill); fill.gameObject.AddComponent<Image>().color = Teal;
        RectTransform handle = Rect("Handle", slider, 24, 44); handle.gameObject.AddComponent<Image>().color = Gold;
        menu.volume.fillRect = fill; menu.volume.handleRect = handle; menu.volume.targetGraphic = handle.GetComponent<Image>();
        RectTransform toggle = Rect("Fullscreen", settingsWindow, 560, 48); Place(toggle, 60, 264, 560, 48);
        Image hit = toggle.gameObject.AddComponent<Image>(); hit.color = new Color32(23, 47, 59, 255);
        menu.fullscreen = toggle.gameObject.AddComponent<Toggle>(); menu.fullscreen.targetGraphic = hit;
        RectTransform check = Rect("Checkmark", toggle, 26, 26); Place(check, 12, 11, 26, 26); Image checkImage = check.gameObject.AddComponent<Image>(); checkImage.color = Teal;
        menu.fullscreen.graphic = checkImage;
        Label("Label", toggle, "FULLSCREEN", 18, Color.white, 60, 4, 480, 40);
        menu.settingsBackButton = Button(restart, settingsWindow, "Back", "<  BACK", 80, 368, 520, 56, 20);
        Label("Note", settingsWindow, "APPLIES TO THIS SESSION / FULLSCREEN IN STANDALONE", 10, Gold, 40, 442, 600, 28);
        pause.gameObject.SetActive(false); setting.gameObject.SetActive(false);
        foreach (Component component in gameOver.GetComponentsInChildren<Component>(true))
            if (component != null && !(component is CanvasRenderer) && PrefabUtility.IsPartOfPrefabInstance(component)) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    private static RectTransform Modal(string name, Transform parent, out RectTransform window, float width, float height)
    {
        RectTransform modal = Rect(name, parent, 0, 0); Stretch(modal);
        modal.gameObject.AddComponent<Image>().color = new Color(0.01f, 0.025f, 0.04f, 0.88f);
        window = Rect(name + " Window", modal, width, height); Center(window);
        Image image = window.gameObject.AddComponent<Image>(); image.color = Ink;
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MyAssets/UI/Transparent border/panel-transparent-border-015.png"); image.type = Image.Type.Sliced;
        RectTransform edge = Rect("UI Border", window, 0, 0); Stretch(edge);
        Image border = edge.gameObject.AddComponent<Image>(); border.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MyAssets/UI/Border/panel-border-028.png");
        border.color = Teal; border.type = Image.Type.Sliced; border.raycastTarget = false;
        return modal;
    }
    private static Button Button(Button source, Transform parent, string name, string text, float x, float y, float width, float height, int fontSize)
    {
        Button button = Object.Instantiate(source, parent); button.name = name; button.onClick = new Button.ButtonClickedEvent();
        StyleButton(button, text, x, y, width, height, fontSize); return button;
    }
    private static void StyleButton(Button button, string text, float x, float y, float width, float height, int fontSize)
    {
        Place(button.GetComponent<RectTransform>(), x, y, width, height);
        Text label = button.GetComponentInChildren<Text>(); Stretch(label.rectTransform); label.fontSize = fontSize; label.text = text; label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
    }
    private static Text Label(string name, Transform parent, string value, int size, Color color, float x, float y, float width, float height)
    {
        RectTransform rect = Rect(name, parent, width, height); Place(rect, x, y, width, height);
        Text text = rect.gameObject.AddComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.text = value;
        text.fontSize = size; text.color = color; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false; return text;
    }
    private static RectTransform Rect(string name, Transform parent, float width, float height)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.sizeDelta = new Vector2(width, height); return rect;
    }
    private static void Center(RectTransform rect) { rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f; rect.anchoredPosition = Vector2.zero; }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static void Place(RectTransform rect, float x, float y, float width, float height)
    { rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); }
    private static Scene Open(string path) { Scene scene = SceneManager.GetSceneByPath(path); return scene.isLoaded ? scene : EditorSceneManager.OpenScene(path, OpenSceneMode.Additive); }
    private static T Find<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).Single();
    private static void Require(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
}
