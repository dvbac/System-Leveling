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

public static class LoadoutSelectionBuilder
{
    public const string ScenePath = "Assets/Scenes/LoadoutSelection.unity";
    private static readonly Color Ink = new Color32(9, 19, 31, 255);
    private static readonly Color Panel = new Color32(17, 32, 47, 255);
    private static readonly Color White = new Color32(232, 240, 240, 255);
    private static readonly Color Gold = new Color32(234, 199, 132, 255);
    private static Font font;
    private static Sprite disc, ring;

    [MenuItem("Tools/System Leveling/Apply Reference Selection Layout")]
    public static void ApplyReference()
    {
        LoadoutSelection ui = Object.FindAnyObjectByType<LoadoutSelection>();
        Require(!Application.isPlaying && ui != null && ui.gameObject.scene.path == ScenePath, "Open LoadoutSelection outside Play Mode first.");
        const string backup = "Captures/LoadoutSelection/BeforeReferenceLayout.unity";
        if (!File.Exists(backup))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            Require(EditorSceneManager.SaveScene(ui.gameObject.scene, backup, true), "Could not preserve the current selection scene.");
        }
        ApplyReferencePositions(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        Require(EditorSceneManager.SaveScene(ui.gameObject.scene), "Could not save reference layout.");
        Debug.Log("REFERENCE SELECTION LAYOUT READY: six-column icon grid on the left, stats in the portrait region on the right; prefab catalog and gameplay unchanged.");
    }

    private static void ApplyReferencePositions(LoadoutSelection ui)
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Transform page = ui.transform.Find("Safe Area");
        Place((RectTransform)page.Find("Top Accent"), 72, 28, 20, 4);
        Text brand = page.Find("Brand").GetComponent<Text>();
        Place(brand.rectTransform, 104, 16, 420, 32); brand.fontSize = 18;
        Place(ui.summary.rectTransform, 650, 20, 580, 28); ui.summary.fontSize = 12;
        ui.stepOne.gameObject.SetActive(false); ui.stepTwo.gameObject.SetActive(false);
        page.Find("Step Arrow").gameObject.SetActive(false);
        ui.heading.gameObject.SetActive(false);
        Place(ui.selectionCount.rectTransform, 1000, 74, 230, 20); ui.selectionCount.fontSize = 10;
        ui.selectionCount.text = "PREFAB CATALOG";
        Place(ui.subtitle.rectTransform, 72, 244, 660, 20); ui.subtitle.fontSize = 10;
        foreach (LoadoutSelection.View view in new[] { ui.characterView, ui.weaponView })
        {
            Place(view.title.rectTransform, 72, 82, 720, 74); view.title.fontSize = 52;
            Place(view.category.rectTransform, 72, 158, 660, 24); view.category.fontSize = 13;
            Place(view.description.rectTransform, 72, 188, 660, 54); view.description.fontSize = 16;
            Place(view.stats.rectTransform, 930, 222, 200, 280); view.stats.fontSize = 18; view.stats.lineSpacing = 1.8f;
            Place(view.statValues.rectTransform, 1144, 222, 72, 280); view.statValues.fontSize = 18; view.statValues.lineSpacing = 1.8f;
            Place(view.note.rectTransform, 72, 602, 660, 20); view.note.fontSize = 10;
            if (view.root.transform.Find("Stats Caption") == null)
            {
                Label("Stats Caption", view.root.transform, "BASE STATS", 14, Gold, 930, 152, 286, 28, FontStyle.Bold);
                Box("Stats Rule", view.root.transform, 930, 194, 286, 1, new Color32(43, 65, 83, 255));
            }
            ConfigureGrid(view.options);
        }
        Place(ui.back.GetComponent<RectTransform>(), 48, 646, 350, 46);
        ui.back.GetComponentInChildren<Text>().rectTransform.sizeDelta = new Vector2(350, 46);
        ui.back.GetComponentInChildren<Text>().fontSize = 22;
        Place(ui.status.rectTransform, 450, 646, 780, 46); ui.status.fontSize = 11;
        page.Find("Footer Divider").gameObject.SetActive(false);
        page.Find("Preview Notice").gameObject.SetActive(false);
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
    }

    private static void ConfigureGrid(RectTransform content)
    {
        ScrollRect scroll = content.GetComponentInParent<ScrollRect>(true);
        Place(scroll.GetComponent<RectTransform>(), 60, 270, 680, 324);
        Place(scroll.viewport, 0, 0, 680, 324);
        HorizontalLayoutGroup old = content.GetComponent<HorizontalLayoutGroup>();
        if (old != null) Object.DestroyImmediate(old);
        GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = content.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(96, 96); grid.spacing = new Vector2(16, 24);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 6;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal; grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        ContentSizeFitter fit = content.GetComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(0, 1);
        content.anchoredPosition = Vector2.zero; content.sizeDelta = Vector2.zero;
        scroll.horizontal = false; scroll.vertical = true; scroll.normalizedPosition = new Vector2(0, 1);
    }

    [MenuItem("Tools/System Leveling/Use Dynamic Prefab Lists")]
    public static void UpdateDynamic()
    {
        LoadoutSelection ui = Object.FindAnyObjectByType<LoadoutSelection>();
        Require(!Application.isPlaying && ui != null && ui.gameObject.scene.path == ScenePath, "Open LoadoutSelection outside Play Mode first.");
        const string backup = "Captures/LoadoutSelection/BeforeDynamic.unity";
        if (!File.Exists(backup))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            Require(EditorSceneManager.SaveScene(ui.gameObject.scene, backup, true), "Could not back up current scene edits.");
        }
        LoadoutGameplaySetup.EnsureDefaultCharacterPrefab();
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        LoadoutOption template = OptionPrefab();
        GameObject[] characters = ui.characterPrefabs != null && ui.characterPrefabs.Length > 0 ? ui.characterPrefabs : FindPrefabs<PlayerStats>("Assets/Prefab/Characters");
        GameObject[] weapons = ui.weaponPrefabs != null && ui.weaponPrefabs.Length > 0 ? ui.weaponPrefabs : FindPrefabs<WeaponController>("Assets/Prefab/Weapons");
        LoadoutSelection.ValidatePrefabs(characters, weapons);
        foreach (LoadoutSelection.View view in new[] { ui.characterView, ui.weaponView })
        {
            foreach (LoadoutOption old in view.root.GetComponentsInChildren<LoadoutOption>(true)) Object.DestroyImmediate(old.gameObject);
            Transform oldList = view.root.transform.Find("Options");
            if (oldList != null) Object.DestroyImmediate(oldList.gameObject);
            view.options = Options(view.root.transform);
            view.cards = Array.Empty<Button>(); view.rings = Array.Empty<Image>();
        }
        ui.characterPrefabs = characters; ui.weaponPrefabs = weapons; ui.optionPrefab = template;
        ui.selectionCount = ui.transform.Find("Safe Area/Selection Count").GetComponent<Text>();
        ui.selectionCount.text = "PREFAB CATALOG / GENERATED AT RUNTIME";
        EditorUtility.SetDirty(ui);
        EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        Require(EditorSceneManager.SaveScene(ui.gameObject.scene), "Could not save dynamic selection scene.");
        Debug.Log("DYNAMIC LOADOUT READY: SetPrefabs(characterPrefabs, weaponPrefabs); no per-character or per-weapon buttons stored in the scene. Existing layout and gameplay links retained.");
    }

    [MenuItem("Tools/System Leveling/Build Loadout Selection")]
    public static void Build()
    {
        if (Application.isPlaying || File.Exists(ScenePath)) throw new InvalidOperationException("Build outside Play Mode; output scene must not already exist.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene open = SceneManager.GetSceneAt(i);
            if (open.isDirty) throw new InvalidOperationException("Save your open scenes before building the selection scene.");
        }
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject camera = new GameObject("Selection Camera", typeof(Camera));
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        camera.GetComponent<Camera>().backgroundColor = Ink;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        GameObject canvas = new GameObject("Loadout Selection", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0;
        LoadoutSelection ui = canvas.AddComponent<LoadoutSelection>();
        ui.characterPrefabs = FindPrefabs<PlayerStats>("Assets/Prefab/Characters");
        ui.weaponPrefabs = FindPrefabs<WeaponController>("Assets/Prefab/Weapons");
        Layout(ui);
        if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save selection scene.");
        Selection.activeGameObject = canvas;
        Validate();
        Debug.Log("LOADOUT SELECTION BUILT: standalone scene; existing gameplay and assets unchanged.");
    }

    [MenuItem("Tools/System Leveling/Update Compact Loadout Selection")]
    public static void UpdateCompact()
    {
        LoadoutSelection ui = Object.FindAnyObjectByType<LoadoutSelection>();
        Require(!Application.isPlaying && ui != null && ui.gameObject.scene.path == ScenePath, "Open the saved LoadoutSelection scene outside Play Mode first.");
        const string backup = "Captures/LoadoutSelection/BeforeCompact.unity";
        if (!File.Exists(backup))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            Require(EditorSceneManager.SaveScene(ui.gameObject.scene, backup, true), "Could not back up selection scene.");
        }
        foreach (string name in new[] { "Safe Area", "Background" })
        {
            Transform child = ui.transform.Find(name);
            if (child != null) Object.DestroyImmediate(child.gameObject);
        }
        Layout(ui);
        Validate();
        Require(EditorSceneManager.SaveScene(ui.gameObject.scene), "Could not save compact layout.");
        Debug.Log("COMPACT LOADOUT UPDATED: icon-only circles, hover preview, direct click selection, no confirmation buttons.");
    }

    private static void Layout(LoadoutSelection ui)
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        disc = CircleSprite("disc", false); ring = CircleSprite("ring", true);
        RectTransform background = Box("Background", ui.transform, 0, 0, 1280, 720, Ink);
        background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one;
        background.offsetMin = background.offsetMax = Vector2.zero;
        RectTransform page = Rect("Safe Area", ui.transform, 0, 0, 1280, 720);
        page.anchorMin = page.anchorMax = new Vector2(0.5f, 0.5f);
        page.anchoredPosition = new Vector2(-640, 360);

        Box("Top Accent", page, 48, 32, 28, 4, LoadoutSelection.Accent);
        Label("Brand", page, "SYSTEM / LEVELING", 15, White, 88, 22, 350, 25, FontStyle.Bold);
        ui.summary = Label("Loadout Summary", page, "", 11, Gold, 710, 25, 522, 20, FontStyle.Normal, TextAnchor.MiddleRight);
        Box("Top Divider", page, 48, 64, 1184, 1, new Color32(43, 65, 83, 255));
        ui.stepOne = Label("Character Step", page, "01   CHARACTER", 13, LoadoutSelection.Accent, 48, 86, 190, 25, FontStyle.Bold);
        Label("Step Arrow", page, "/", 16, LoadoutSelection.Muted, 244, 86, 30, 25);
        ui.stepTwo = Label("Weapon Step", page, "02   WEAPON", 13, LoadoutSelection.Muted, 280, 86, 180, 25, FontStyle.Bold);
        ui.selectionCount = Label("Selection Count", page, "PREFAB CATALOG", 11, Gold, 920, 86, 312, 25, FontStyle.Normal, TextAnchor.MiddleRight);
        ui.heading = Label("Heading", page, "", 12, LoadoutSelection.Muted, 48, 140, 1160, 24, FontStyle.Bold);
        ui.subtitle = Label("Subtitle", page, "", 11, LoadoutSelection.Muted, 48, 402, 1150, 24);
        ui.characterView = View("Character Screen", page);
        ui.weaponView = View("Weapon Screen", page);
        ui.optionPrefab = OptionPrefab();
        Box("Footer Divider", page, 48, 614, 1184, 1, new Color32(43, 65, 83, 255));
        ui.back = Button("Back", page, "<  BACK", 48, 634, 220, 48, Panel, White);
        ui.status = Label("Status", page, "", 12, LoadoutSelection.Muted, 304, 634, 928, 48, FontStyle.Normal, TextAnchor.MiddleLeft);
        Label("Preview Notice", page, ui.GetComponent<LoadoutLauncher>() != null ? "SELECT A CHARACTER / SELECT A WEAPON / ENTER THE ARENA" : "SELECTION PREVIEW / GAMEPLAY LAUNCH NOT CONNECTED", 9, LoadoutSelection.Muted, 48, 695, 1184, 15, FontStyle.Normal, TextAnchor.MiddleCenter);
        ui.heading.text = "SELECT YOUR CHARACTER";
        ui.subtitle.text = "HOVER TO INSPECT   /   CLICK AN ICON TO SELECT";
        ui.weaponView.root.SetActive(false); ui.back.gameObject.SetActive(false);
        ApplyReferencePositions(ui);
    }

    private static GameObject[] FindPrefabs<T>(string folder) where T : Component => AssetDatabase.FindAssets("t:Prefab", new[] { folder })
        .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
        .Where(prefab => prefab.GetComponent<T>() != null).OrderBy(prefab => prefab.name, StringComparer.Ordinal).ToArray();

    private static LoadoutSelection.View View(string name, Transform parent)
    {
        var v = new LoadoutSelection.View { root = Rect(name, parent, 0, 0, 1280, 720).gameObject };
        Transform root = v.root.transform;
        v.title = Label("Name", root, "", 42, White, 48, 174, 1184, 58, FontStyle.Bold);
        v.category = Label("Category", root, "", 12, LoadoutSelection.Accent, 48, 238, 1184, 24);
        v.stats = Label("Stats", root, "", 13, LoadoutSelection.Muted, 48, 282, 224, 116);
        v.statValues = Label("Stat Values", root, "", 13, Gold, 288, 282, 112, 116, FontStyle.Normal, TextAnchor.UpperRight);
        v.description = Label("Description", root, "", 17, White, 500, 282, 710, 100);
        v.note = Label("Selection Note", root, "", 11, LoadoutSelection.Muted, 48, 574, 1184, 24);
        v.options = Options(root);
        return v;
    }

    private static LoadoutOption OptionPrefab()
    {
        const string path = "Assets/Prefab/UI/LoadoutOption.prefab";
        LoadoutOption existing = AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<LoadoutOption>();
        if (existing != null) return existing;
        if (File.Exists(path)) throw new InvalidOperationException("Existing option prefab is invalid; refusing to overwrite it.");
        Directory.CreateDirectory("Assets/Prefab/UI");
        disc = CircleSprite("disc", false); ring = CircleSprite("ring", true);
        Button card = Button("Loadout Option", null, "", 0, 0, 104, 104, Panel, White);
        Object.DestroyImmediate(card.GetComponentInChildren<Text>().gameObject);
        Image target = card.GetComponent<Image>(); target.sprite = disc; target.alphaHitTestMinimumThreshold = 0.1f;
        LoadoutOption option = card.gameObject.AddComponent<LoadoutOption>();
        option.button = card;
        Image border = Rect("Selection Ring", card.transform, 0, 0, 104, 104).gameObject.AddComponent<Image>();
        border.sprite = ring; border.raycastTarget = false; option.ring = border;
        option.icon = Rect("Icon", card.transform, 18, 12, 68, 80).gameObject.AddComponent<Image>();
        option.icon.preserveAspect = true; option.icon.raycastTarget = false;
        try { return PrefabUtility.SaveAsPrefabAsset(card.gameObject, path).GetComponent<LoadoutOption>(); }
        finally { Object.DestroyImmediate(card.gameObject); }
    }

    private static RectTransform Options(Transform parent)
    {
        RectTransform root = Rect("Options", parent, 60, 270, 680, 324);
        ScrollRect scroll = root.gameObject.AddComponent<ScrollRect>();
        scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 40;
        RectTransform viewport = Rect("Viewport", root, 0, 0, 680, 324);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image hitArea = viewport.gameObject.AddComponent<Image>(); hitArea.color = Color.clear;
        RectTransform content = Rect("Content", viewport, 0, 0, 680, 0);
        content.gameObject.AddComponent<ContentSizeFitter>();
        scroll.viewport = viewport; scroll.content = content;
        ConfigureGrid(content);
        return content;
    }

    private static Sprite CircleSprite(string name, bool outline)
    {
        const string folder = "Assets/MyAssets/UI/Loadout";
        string path = folder + "/" + name + ".png";
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(folder);
            Texture2D texture = new Texture2D(104, 104, TextureFormat.RGBA32, false);
            for (int y = 0; y < 104; y++)
                for (int x = 0; x < 104; x++)
                {
                    float radius = Vector2.Distance(new Vector2(x, y), new Vector2(51.5f, 51.5f));
                    bool visible = outline ? radius >= 49 && radius <= 51 || radius >= 45 && radius <= 46 : radius < 49;
                    texture.SetPixel(x, y, visible ? Color.white : Color.clear);
                }
            File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.isReadable = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
    {
        RectTransform r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        r.SetParent(parent, false); r.anchorMin = r.anchorMax = r.pivot = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, -y); r.sizeDelta = new Vector2(width, height);
        return r;
    }

    private static RectTransform Box(string name, Transform parent, float x, float y, float width, float height, Color color)
    {
        RectTransform r = Rect(name, parent, x, y, width, height);
        Image image = r.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
        return r;
    }

    private static Text Label(string name, Transform parent, string text, int size, Color color, float x, float y, float width, float height, FontStyle style = FontStyle.Normal, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        Text label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<Text>();
        label.font = font; label.text = text; label.fontSize = size; label.color = color; label.fontStyle = style;
        label.alignment = alignment; label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    private static Button Button(string name, Transform parent, string text, float x, float y, float width, float height, Color background, Color foreground)
    {
        RectTransform r = Box(name, parent, x, y, width, height, background);
        r.GetComponent<Image>().raycastTarget = true;
        Button b = r.gameObject.AddComponent<Button>(); b.targetGraphic = r.GetComponent<Image>();
        ColorBlock colors = b.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.selectedColor = new Color(1.1f, 1.1f, 1.1f); b.colors = colors;
        Label("Label", r, text, 13, foreground, 0, 0, width, height, FontStyle.Bold, TextAnchor.MiddleCenter);
        return b;
    }

    [MenuItem("Tools/System Leveling/Validate Loadout Selection")]
    public static void Validate()
    {
        LoadoutSelection ui = Object.FindAnyObjectByType<LoadoutSelection>();
        Require(ui != null, "Open LoadoutSelection scene first.");
        LoadoutLauncher launcher = ui.GetComponent<LoadoutLauncher>();
        bool launchEnabled = launcher != null && launcher.enabled;
        try
        {
            if (launcher != null) launcher.enabled = false;
            ValidateSelection(ui);
        }
        finally { if (launcher != null) launcher.enabled = launchEnabled; }
    }

    private static void ValidateSelection(LoadoutSelection ui)
    {
        string before = AssetSnapshot();
        LoadoutSelection.ValidatePrefabs(ui.characterPrefabs, ui.weaponPrefabs);
        if (!Application.isPlaying)
        {
            Require(ui.optionPrefab != null && ui.characterView.options != null && ui.weaponView.options != null, "Missing template or list containers.");
            Debug.Log("LOADOUT PREFAB CONFIGURATION PASS. Enter Play Mode to test generated options.");
            return;
        }
        ui.Initialize();
        ui.Back();
        for (int i = 0; i < ui.characterPrefabs.Length; i++)
        {
            int weapon = i % ui.weaponPrefabs.Length;
            int previousCharacter = ui.CharacterIndex;
            ui.PreviewCharacter(i);
            CharacterScriptableObject data = ui.characterPrefabs[i].GetComponent<PlayerStats>().characterData;
            Require(ui.CharacterIndex == previousCharacter && !ui.ChoosingWeapon && ui.characterView.title.text == data.name.ToUpperInvariant(), "Character preview must not commit a choice.");
            ui.characterView.cards[i].onClick.Invoke();
            Require(ui.SelectedCharacter == data && ui.SelectedCharacterPrefab == ui.characterPrefabs[i], "Character card mismatch.");
            Require(ui.ChoosingWeapon && ui.weaponView.root.activeSelf && !ui.characterView.root.activeSelf, "Weapon screen navigation failed.");
            int confirmations = 0;
            UnityEngine.Events.UnityAction listener = () => confirmations++;
            ui.onConfirmed.AddListener(listener);
            ui.weaponView.cards[weapon].onClick.Invoke(); ui.weaponView.cards[weapon].onClick.Invoke();
            Require(ui.SelectedWeapon == ui.weaponPrefabs[weapon], "Weapon card mismatch.");
            Require(ui.Confirmed && confirmations == 1, "Confirmation must fire once.");
            ui.PreviewWeapon((weapon + 1) % ui.weaponPrefabs.Length);
            Require(ui.Confirmed && ui.WeaponIndex == weapon && confirmations == 1, "Hover must not replace a confirmed weapon.");
            ui.onConfirmed.RemoveListener(listener);
            ui.back.onClick.Invoke();
            Require(!ui.ChoosingWeapon && ui.CharacterIndex == i && ui.WeaponIndex == weapon && !ui.Confirmed, "Back must retain both choices.");
        }
        int retained = ui.CharacterIndex;
        ui.SelectCharacter(-1); ui.SelectWeapon(ui.weaponPrefabs.Length);
        Require(ui.CharacterIndex == retained, "Invalid choices must be ignored.");
        Require(before == AssetSnapshot(), "Selection modified source data.");
        ui.SelectCharacter(0); ui.SelectWeapon(0); ui.Back();
        Require(ui.GetComponentsInChildren<Button>(true).Length == ui.characterPrefabs.Length + ui.weaponPrefabs.Length + 1, "Only generated icon buttons and Back should exist.");
        Debug.Log("LOADOUT SELECTION PASS: generated prefab lists, hover does not commit, back retains choices, single confirmation, invalid indices, source assets unchanged.");
    }

    private static string AssetSnapshot() => string.Join("\n", Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories).OrderBy(p => p).Select(File.ReadAllText));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
