using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class HudSceneBuilder
{
    private static readonly Color Ink = new Color32(7, 20, 42, 255);
    private static readonly Color Accent = new Color32(57, 184, 255, 255);
    private static readonly Color Cream = new Color32(229, 246, 255, 255);
    private static readonly Color Muted = new Color32(137, 175, 205, 255);
    private static Sprite frame;
    private static Sprite barSprite;
    private static Font font;

    [MenuItem("Tools/System Leveling/Build HUD")]
    public static void Build()
    {
        if (Application.isPlaying)
            throw new System.InvalidOperationException("Build HUD only outside Play Mode.");
        EnsureLegacySlimeIdle();
        if (Object.FindAnyObjectByType<PlayerHud>(FindObjectsInactive.Include) != null)
        {
            RefreshBarStyle();
            UpgradeExistingHud();
            Debug.Log("HUD upgraded to blue theme with Game Over; gameplay scene preserved.");
            return;
        }
        PlayerStats player = Object.FindAnyObjectByType<PlayerStats>();
        if (player == null)
            throw new System.InvalidOperationException("Open the gameplay scene containing PlayerStats first.");

        const string spritePath = "Assets/MyAssets/UI/Kenney/fantasy-frame.png";
        AssetDatabase.ImportAsset(spritePath);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.spriteBorder = new Vector4(10, 10, 10, 10);
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        frame = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RefreshBarStyle();

        GameObject root = new GameObject("System HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.SetActive(false);
        Undo.RegisterCreatedObjectUndo(root, "Create System HUD");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform hud = Rect("HUD", root.transform, new Vector2(0, 1), new Vector2(1, 1), Vector2.zero, new Vector2(0, 140));
        hud.pivot = new Vector2(0.5f, 1);
        RectTransform health = Rect("Health Panel", hud, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(342, 106));
        health.pivot = new Vector2(0, 1);
        Panel(health, Ink, Accent);
        Label("Name", health, "PLAYER / VITALS", 17, Accent, new Vector2(20, -16), new Vector2(150, 24));
        Text hpText = Label("Health Value", health, "200 / 200", 17, Cream, new Vector2(172, -16), new Vector2(148, 24), TextAnchor.MiddleRight);
        Image hpFill = Bar("Health", health, new Vector2(20, -50), new Vector2(302, 15), new Color32(192, 68, 73, 255));
        Label("Health Caption", health, "VITAL SIGNS", 11, Muted, new Vector2(20, -74), new Vector2(240, 16));

        RectTransform badge = Rect("Level Badge", hud, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -24), new Vector2(100, 106));
        badge.pivot = new Vector2(1, 1);
        Panel(badge, Ink, Accent);
        Label("Caption", badge, "LEVEL", 13, Accent, new Vector2(10, -12), new Vector2(80, 22), TextAnchor.MiddleCenter);
        Text level = Label("Level Value", badge, "01", 38, Cream, new Vector2(10, -36), new Vector2(80, 52), TextAnchor.MiddleCenter);

        RectTransform expPanel = Rect("Experience Panel", root.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 22), new Vector2(-48, 62));
        expPanel.pivot = new Vector2(0.5f, 0);
        Panel(expPanel, Ink, new Color32(84, 119, 142, 255));
        Label("Caption", expPanel, "EXPERIENCE", 12, Muted, new Vector2(20, -10), new Vector2(160, 18));
        Text expText = Label("Experience Value", expPanel, "0 / 100 EXP", 12, Cream, new Vector2(-190, -10), new Vector2(170, 18), TextAnchor.MiddleRight);
        expText.rectTransform.anchorMin = expText.rectTransform.anchorMax = new Vector2(1, 1);
        RectTransform expTrack = Rect("Experience Track", expPanel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 15), new Vector2(-40, 10));
        expTrack.pivot = new Vector2(0.5f, 0);
        Image expFill = Fill(expTrack, new Color32(73, 167, 209, 255));

        RectTransform modal = Rect("Level Up Modal", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image shade = modal.gameObject.AddComponent<Image>();
        shade.color = new Color(0.025f, 0.04f, 0.065f, 0.88f);
        shade.raycastTarget = true;
        RectTransform window = Rect("Upgrade Window", modal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960, 500));
        Panel(window, Ink, Accent);
        Label("Eyebrow", window, "THE SYSTEM RECOGNIZES YOUR GROWTH", 12, Muted, new Vector2(20, -28), new Vector2(920, 24), TextAnchor.MiddleCenter);
        Label("Heading", window, "LEVEL UP", 38, Cream, new Vector2(20, -60), new Vector2(920, 54), TextAnchor.MiddleCenter);
        Label("Subtitle", window, "Forge your path. Choose your next advantage.", 16, Accent, new Vector2(20, -120), new Vector2(920, 28), TextAnchor.MiddleCenter);

        Button[] buttons = new Button[3];
        Text[] titles = new Text[3];
        Text[] details = new Text[3];
        Text[] symbols = new Text[3];
        for (int i = 0; i < 3; i++)
        {
            RectTransform card = Rect("Upgrade Card " + (i + 1), window, new Vector2(0, 1), new Vector2(0, 1), new Vector2(46 + i * 294, -173), new Vector2(280, 244));
            card.pivot = new Vector2(0, 1);
            Image bg = Panel(card, new Color32(29, 40, 50, 255), new Color32(124, 112, 83, 255));
            bg.raycastTarget = true;
            buttons[i] = card.gameObject.AddComponent<Button>();
            buttons[i].targetGraphic = bg;
            ColorBlock colors = buttons[i].colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.45f, 1.35f, 1.1f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = new Color(0.7f, 0.8f, 0.85f);
            buttons[i].colors = colors;
            symbols[i] = Label("Symbol", card, "ATK", 23, Accent, new Vector2(20, -22), new Vector2(240, 38), TextAnchor.MiddleCenter);
            titles[i] = Label("Title", card, "POWER", 22, Cream, new Vector2(12, -66), new Vector2(256, 32), TextAnchor.MiddleCenter);
            details[i] = Label("Description", card, "+10% DAMAGE\n\nStronger sword strikes\nand magic projectiles.", 16, Muted, new Vector2(12, -105), new Vector2(256, 90), TextAnchor.UpperCenter);
            Label("Choose", card, "SELECT UPGRADE  >", 12, Accent, new Vector2(12, -206), new Vector2(256, 20), TextAnchor.MiddleCenter);
        }
        Text queue = Label("Queue", window, "CHOOSE ONE UPGRADE TO CONTINUE", 12, Muted, new Vector2(20, -444), new Vector2(920, 26), TextAnchor.MiddleCenter);
        modal.gameObject.SetActive(false);
        PlayerHud view = root.AddComponent<PlayerHud>();
        view.Configure(null, hpFill, expFill, hpText, expText, level);
        LevelUpSelection selection = root.AddComponent<LevelUpSelection>();
        selection.Configure(null, modal.gameObject, buttons, titles, details, symbols, queue);
        ApplyBlueDesign(root);

        Directory.CreateDirectory("Assets/Prefab/UI");
        PrefabUtility.SaveAsPrefabAssetAndConnect(root, "Assets/Prefab/UI/System HUD.prefab", InteractionMode.AutomatedAction);
        // Scene-only references are overrides, not dependencies of the reusable prefab.
        BindPlayer(view, player);
        BindPlayer(selection, player);
        BindPlayer(root.GetComponent<GameOverController>(), player);
        root.SetActive(true);
        PrefabUtility.RecordPrefabInstancePropertyModifications(root);
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject input = new GameObject("UI EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            input.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            Undo.RegisterCreatedObjectUndo(input, "Create UI EventSystem");
        }
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("System HUD prefab and scene integration created.");
    }

    private static void UpgradeExistingHud()
    {
        frame = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MyAssets/UI/Kenney/fantasy-frame.png");
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        const string path = "Assets/Prefab/UI/System HUD.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            ApplyBlueDesign(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        PlayerHud hud = Object.FindAnyObjectByType<PlayerHud>(FindObjectsInactive.Include);
        ApplyBlueDesign(hud.gameObject);
        PlayerStats player = Object.FindAnyObjectByType<PlayerStats>();
        BindPlayer(hud.gameObject.GetComponent<GameOverController>(), player);
        foreach (Component component in hud.GetComponentsInChildren<Component>(true))
            if (component != null && !(component is CanvasRenderer))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
        EditorSceneManager.SaveScene(hud.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    private static void ApplyBlueDesign(GameObject root)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.name;
            if (name == "Fantasy Border") image.color = Accent;
            else if (name == "Fill") image.color = image.transform.parent.name.Contains("Health")
                ? new Color32(232, 86, 111, 255) : new Color32(35, 157, 250, 255);
            else if (name.Contains("Modal")) image.color = new Color(0.01f, 0.035f, 0.085f, 0.77f);
            else if (name.Contains("Track")) image.color = new Color32(3, 11, 26, 255);
            else if (name.Contains("Card")) image.color = new Color32(13, 42, 72, 255);
            else image.color = Ink;
        }
        foreach (Text label in root.GetComponentsInChildren<Text>(true))
        {
            string name = label.name;
            label.color = name == "Heading" || name.Contains("Value") || name == "Title" ? Cream
                : name == "Description" || name == "Eyebrow" || name == "Queue" ? Muted : Accent;
        }
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
            StyleButton(button);

        Transform health = root.transform.Find("HUD/Health Panel");
        if (health != null)
        {
            health.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 112);
            health.Find("Name").GetComponent<Text>().text = "PLAYER / VITALS";
            health.Find("Name").GetComponent<Text>().fontSize = 14;
            health.Find("Name").GetComponent<RectTransform>().sizeDelta = new Vector2(180, 24);
            health.Find("Health Value").GetComponent<RectTransform>().anchoredPosition = new Vector2(210, -16);
            health.Find("Health Track").GetComponent<RectTransform>().sizeDelta = new Vector2(340, 15);
            health.Find("Health Caption").GetComponent<Text>().text = "SYSTEM STATUS / ACTIVE";
        }
        Transform exp = root.transform.Find("Experience Panel");
        if (exp != null)
        {
            RectTransform rect = exp.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.16f, 0);
            rect.anchorMax = new Vector2(0.84f, 0);
            rect.sizeDelta = new Vector2(0, 62);
            exp.Find("Caption").GetComponent<Text>().text = "EXPERIENCE / PROGRESSION";
            exp.Find("Caption").GetComponent<RectTransform>().sizeDelta = new Vector2(270, 18);
        }
        if (root.transform.Find("Game Over Modal") == null)
            AddGameOver(root);
    }

    private static void AddGameOver(GameObject root)
    {
        RectTransform modal = Rect("Game Over Modal", root.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image shade = modal.gameObject.AddComponent<Image>();
        shade.color = new Color(0.01f, 0.035f, 0.085f, 0.77f);
        shade.raycastTarget = true;
        RectTransform window = Rect("Game Over Window", modal, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, new Vector2(680, 440));
        Panel(window, Ink, Accent);
        Label("Eyebrow", window, "SYSTEM // RUN TERMINATED", 13, Accent, new Vector2(20, -34), new Vector2(640, 24), TextAnchor.MiddleCenter);
        Label("Heading", window, "GAME OVER", 48, Cream, new Vector2(20, -76), new Vector2(640, 70), TextAnchor.MiddleCenter);
        Text result = Label("Result", window, "LEVEL REACHED  01\n\nEvery defeat is a new beginning.", 18, Muted, new Vector2(40, -170), new Vector2(600, 96), TextAnchor.UpperCenter);
        RectTransform retry = Rect("Restart Button", window, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 105), new Vector2(292, 58));
        Image buttonImage = Panel(retry, new Color32(13, 66, 111, 255), Accent);
        buttonImage.raycastTarget = true;
        Button button = retry.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        StyleButton(button);
        Label("Restart Label", retry, "RESTART  >", 20, Cream, new Vector2(10, -8), new Vector2(272, 42), TextAnchor.MiddleCenter);
        Label("Footer", window, "A NEW RUN. A STRONGER YOU.", 12, Muted, new Vector2(20, -386), new Vector2(640, 24), TextAnchor.MiddleCenter);
        GameOverController controller = root.GetComponent<GameOverController>();
        if (controller == null) controller = root.AddComponent<GameOverController>();
        controller.Configure(null, root.GetComponent<LevelUpSelection>(), modal.gameObject, result, button);
        modal.gameObject.SetActive(false);
    }

    private static void StyleButton(Button button)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.6f, 1.8f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.7f, 0.9f, 1f);
        button.colors = colors;
    }

    private static void EnsureLegacySlimeIdle()
    {
        const string folder = "Assets/MyAssets/Monster/Creep/Blue_Slime/Animarion/";
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(folder + "Blue Slime.controller");
        if (controller == null) return;
        foreach (ChildAnimatorState state in controller.layers[0].stateMachine.states)
            if (state.state.name == "Idle") return;
        AnimationClip clip = new AnimationClip { name = "Idle", frameRate = 12 };
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/MyAssets/Monster/Creep/Blue_Slime/Slime.png");
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
            new[] { new ObjectReferenceKeyframe { time = 0, value = sprite }, new ObjectReferenceKeyframe { time = 1, value = sprite } });
        clip.SetCurve("", typeof(Transform), "m_LocalScale.y", new AnimationCurve(
            new Keyframe(0, 1), new Keyframe(0.5f, 0.94f), new Keyframe(1, 1)));
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        AssetDatabase.CreateAsset(clip, folder + "Idle.anim");
        AnimatorState idle = controller.layers[0].stateMachine.AddState("Idle");
        idle.motion = clip;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
    }

    private static void BindPlayer(Object component, PlayerStats player)
    {
        SerializedObject so = new SerializedObject(component);
        so.FindProperty("player").objectReferenceValue = player;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pos, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        return rect;
    }

    private static Image Panel(RectTransform rect, Color background, Color border)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = background;
        image.raycastTarget = false;
        RectTransform edge = Rect("Fantasy Border", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image outline = edge.gameObject.AddComponent<Image>();
        outline.sprite = frame;
        outline.type = Image.Type.Sliced;
        outline.color = border;
        outline.raycastTarget = false;
        return image;
    }

    private static Text Label(string name, Transform parent, string value, int size, Color color, Vector2 position, Vector2 dimensions, TextAnchor alignment = TextAnchor.MiddleLeft)
    {
        RectTransform rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), position, dimensions);
        rect.pivot = new Vector2(0, 1);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;
        return text;
    }

    private static Image Bar(string name, Transform parent, Vector2 position, Vector2 dimensions, Color color)
    {
        RectTransform track = Rect(name + " Track", parent, new Vector2(0, 1), new Vector2(0, 1), position, dimensions);
        track.pivot = new Vector2(0, 1);
        return Fill(track, color);
    }

    private static Image Fill(RectTransform track, Color color)
    {
        track.gameObject.AddComponent<Image>().color = new Color32(8, 14, 21, 255);
        track.GetComponent<Image>().raycastTarget = false;
        RectTransform rect = Rect("Fill", track, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-4, -4));
        Image fill = rect.gameObject.AddComponent<Image>();
        // A white built-in sprite gives Image.Filled an actual quad to crop.
        fill.sprite = barSprite;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.color = color;
        fill.raycastTarget = false;
        return fill;
    }

    private static void RefreshBarStyle()
    {
        const string path = "Assets/MyAssets/UI/Kenney/bar-fill.png";
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        barSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        PlayerHud existing = Object.FindAnyObjectByType<PlayerHud>(FindObjectsInactive.Include);
        if (existing == null)
            return;
        foreach (Image image in existing.GetComponentsInChildren<Image>(true))
        {
            if (image.type != Image.Type.Filled)
                continue;
            image.sprite = barSprite;
            PrefabUtility.RecordPrefabInstancePropertyModifications(image);
        }
        const string prefabPath = "Assets/Prefab/UI/System HUD.prefab";
        GameObject prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            foreach (Image image in prefab.GetComponentsInChildren<Image>(true))
                if (image.type == Image.Type.Filled)
                    image.sprite = barSprite;
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
        EditorSceneManager.SaveScene(existing.gameObject.scene);
    }
}
