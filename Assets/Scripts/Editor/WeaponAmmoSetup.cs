using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class WeaponAmmoSetup
{
    private const string Root = "Assets/Prefab/Weapons";
    private const string Data = "Assets/Scriptable Object/Weapon";
    private const string Backup = "Captures/WeaponAmmo/Before";

    [MenuItem("Tools/System Leveling/Configure Weapon Ammo")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Configure outside Play Mode.");
        PlayerStats player = Object.FindAnyObjectByType<PlayerStats>();
        if (player == null || player.gameObject.scene.path != "Assets/Scenes/SampleScene.unity")
            throw new InvalidOperationException("Open the gameplay scene first.");
        foreach (string path in new[] { "Assets/Scenes/SampleScene.unity", Data + "/Magic Book.asset", "Assets/Scriptable Object/Character/Mage.asset",
            "Assets/Scriptable Object/Character/Archer.asset", "Assets/Scriptable Object/Character/Hunter.asset", "Assets/Scriptable Object/Character/Knight.asset",
            "Assets/Prefab/Characters/Archer.prefab", "Assets/Prefab/Characters/Hunter.prefab", "Assets/Prefab/Characters/Knight.prefab" }) BackupFile(path);
        Directory.CreateDirectory(Root);
        AssetDatabase.Refresh();
        WeaponScriptableObject book = Load<WeaponScriptableObject>(Data + "/Magic Book.asset");
        WeaponScriptableObject bow = NewData("Bow");
        WeaponScriptableObject crossbow = NewData("Crossbow");
        ConfigureData(book, WeaponFireMode.Magazine, 8, 14, 9, 0.4f, 1.8f, 2);
        ConfigureData(bow, WeaponFireMode.Bow, 7, 22, 15, 0.25f, 0.65f, 1);
        ConfigureData(crossbow, WeaponFireMode.Crossbow, 1, 24, 18, 0.1f, 1.5f, 2);
        GameObject magicShot = Projectile("MagicBolt", book, null);
        Sprite arrow = SpriteAt("Assets/MyAssets/Weapon/Hit/Arows1.png");
        GameObject bowShot = Projectile("Arrow", bow, arrow);
        GameObject crossShot = Projectile("CrossbowBolt", crossbow, arrow);
        SetProjectile(book, magicShot); SetProjectile(bow, bowShot); SetProjectile(crossbow, crossShot);

        GameObject bookWeapon = Weapon("MagicBook", book, SpriteAt("Assets/MyAssets/Weapon/Base/Magic_book.png"));
        GameObject bowWeapon = Weapon("Bow", bow, SpriteAt("Assets/MyAssets/Weapon/Base/Bow.png"));
        GameObject crossWeapon = Weapon("Crossbow", crossbow, SpriteAt("Assets/MyAssets/Weapon/Base/Cross_bow.png"));
        GameObject sword = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Sword.prefab");
        if (sword == null)
        {
            MeleeWeaponController source = player.GetComponentInChildren<MeleeWeaponController>(true);
            GameObject copy = source != null ? Object.Instantiate(source.gameObject) : BuildSword();
            try { copy.name = "Sword"; copy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); sword = PrefabUtility.SaveAsPrefabAsset(copy, Root + "/Sword.prefab"); }
            finally { Object.DestroyImmediate(copy); }
        }
        StartingWeapon("Mage", bookWeapon); StartingWeapon("Archer", bowWeapon);
        StartingWeapon("Hunter", crossWeapon); StartingWeapon("Knight", sword);
        foreach (string name in new[] { "Archer", "Hunter", "Knight" })
        {
            string path = $"Assets/Prefab/Characters/{name}.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                WeaponHolder holder = contents.GetComponentInChildren<WeaponHolder>(true);
                if (holder == null) throw new InvalidOperationException("Missing user WeaponHolder: " + name);
                ClearWeapons(holder);
                PrefabUtility.SaveAsPrefabAsset(contents, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(contents); }
        }
        Undo.RegisterFullObjectHierarchyUndo(player.gameObject, "Use runtime starting weapon");
        WeaponHolder sceneHolder = player.GetComponentInChildren<WeaponHolder>(true);
        if (sceneHolder == null) throw new InvalidOperationException("Missing scene WeaponHolder.");
        ClearWeapons(sceneHolder);
        AddHud(player);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        Debug.Log("WEAPON AMMO SETUP COMPLETE: Mage book8, Archer bow7, Hunter crossbow1, Knight sword; original character hierarchies preserved.");
    }

    private static GameObject BuildSword()
    {
        GameObject root = new GameObject("Sword");
        MeleeWeaponController controller = root.AddComponent<MeleeWeaponController>();
        controller.weaponData = Load<WeaponScriptableObject>(Data + "/Sword.asset");
        GameObject pivot = new GameObject("SwordPivot"); pivot.transform.SetParent(root.transform, false);
        GameObject visual = new GameObject("Visual", typeof(SpriteRenderer)); visual.transform.SetParent(pivot.transform, false);
        Sprite sprite = SpriteAt("Assets/MyAssets/Weapon/Base/Sword.png");
        visual.GetComponent<SpriteRenderer>().sprite = sprite;
        visual.GetComponent<SpriteRenderer>().sortingOrder = 2;
        SerializedObject so = new SerializedObject(controller); so.FindProperty("swordPivot").objectReferenceValue = pivot.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        WeaponHolderSetup.ConfigureWeapon(root);
        return root;
    }

    internal static void ClearWeapons(WeaponHolder holder)
    {
        foreach (WeaponController weapon in holder.GetComponentsInChildren<WeaponController>(true))
        {
            if (weapon.gameObject == holder.gameObject) throw new InvalidOperationException("WeaponController is on WeaponHolder itself; migrate manually rather than deleting the holder.");
            Object.DestroyImmediate(weapon.gameObject);
        }
    }

    private static void StartingWeapon(string name, GameObject weapon)
    {
        CharacterScriptableObject data = Load<CharacterScriptableObject>($"Assets/Scriptable Object/Character/{name}.asset");
        SerializedObject so = new SerializedObject(data); so.FindProperty("startingWeapon").objectReferenceValue = weapon;
        so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(data);
    }

    private static WeaponScriptableObject NewData(string name)
    {
        string path = Data + "/" + name + ".asset";
        WeaponScriptableObject data = AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>(path);
        if (data == null) { data = ScriptableObject.CreateInstance<WeaponScriptableObject>(); AssetDatabase.CreateAsset(data, path); }
        else BackupFile(path);
        return data;
    }

    private static void ConfigureData(WeaponScriptableObject data, WeaponFireMode mode, int ammo, float damage, float speed, float interval, float reload, int pierce)
    {
        SerializedObject so = new SerializedObject(data);
        so.FindProperty("fireMode").enumValueIndex = (int)mode;
        so.FindProperty("magazineSize").intValue = ammo;
        so.FindProperty("damage").floatValue = damage; so.FindProperty("speed").floatValue = speed;
        so.FindProperty("cooldown").floatValue = interval; so.FindProperty("reloadTime").floatValue = reload;
        so.FindProperty("pierce").intValue = pierce;
        so.ApplyModifiedPropertiesWithoutUndo(); data.ValidateRangedSettings();
        AssetDatabase.SaveAssetIfDirty(data);
    }

    private static void SetProjectile(WeaponScriptableObject data, GameObject projectile)
    {
        SerializedObject so = new SerializedObject(data); so.FindProperty("prefab").objectReferenceValue = projectile;
        so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssetIfDirty(data);
    }

    private static GameObject Projectile(string name, WeaponScriptableObject data, Sprite arrow)
    {
        string path = Root + "/" + name + ".prefab";
        BackupFile(path);
        GameObject instance = Object.Instantiate(Load<GameObject>("Assets/Prefab/Spell.prefab"));
        try
        {
            instance.name = name;
            instance.GetComponent<ProjectileWeaponBehaviour>().weaponData = data;
            if (arrow != null)
            {
                SpriteRenderer visual = instance.GetComponent<SpriteRenderer>(); visual.sprite = arrow;
                float scale = 0.75f / arrow.bounds.size.x;
                instance.transform.localScale = new Vector3(scale, scale, 1);
                CapsuleCollider2D collider = instance.GetComponent<CapsuleCollider2D>();
                collider.direction = CapsuleDirection2D.Horizontal;
                collider.offset = arrow.bounds.center;
                collider.size = new Vector2(arrow.bounds.size.x * 0.9f, arrow.bounds.size.y * 0.6f);
                Rigidbody2D body = instance.GetComponent<Rigidbody2D>(); body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            }
            return PrefabUtility.SaveAsPrefabAsset(instance, path);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    private static GameObject Weapon(string name, WeaponScriptableObject data, Sprite sprite)
    {
        string path = Root + "/" + name + ".prefab";
        BackupFile(path);
        GameObject instance = new GameObject(name);
        try
        {
            RangedWeaponController controller = name == "MagicBook" ? instance.AddComponent<MagicBookController>() : instance.AddComponent<RangedWeaponController>();
            controller.weaponData = data;
            GameObject visual = new GameObject("Visual", typeof(SpriteRenderer)); visual.transform.SetParent(instance.transform, false);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = 2;
            renderer.sharedMaterial = Object.FindAnyObjectByType<PlayerStats>().GetComponent<SpriteRenderer>().sharedMaterial;
            WeaponHolderSetup.ConfigureWeapon(instance);
            return PrefabUtility.SaveAsPrefabAsset(instance, path);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    private static Sprite SpriteAt(string path)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(item => item.name).FirstOrDefault();
        if (sprite == null) throw new InvalidOperationException("Missing imported sprite: " + path);
        return sprite;
    }

    private static void AddHud(PlayerStats player)
    {
        WeaponHud existing = Object.FindAnyObjectByType<WeaponHud>(FindObjectsInactive.Include);
        if (existing != null)
        {
            if (existing.TryGetComponent(out Image image)) image.color = new Color32(7, 20, 42, 255);
            return;
        }
        PlayerHud hud = Object.FindAnyObjectByType<PlayerHud>();
        if (hud == null) throw new InvalidOperationException("Missing System HUD.");
        Transform canvas = hud.GetComponentInParent<Canvas>().transform;
        GameObject panel = new GameObject("Weapon Panel", typeof(RectTransform), typeof(Image));
        Undo.RegisterCreatedObjectUndo(panel, "Create weapon HUD");
        panel.transform.SetParent(canvas, false);
        RectTransform rect = panel.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(24, -146); rect.sizeDelta = new Vector2(410, 64);
        Image background = panel.GetComponent<Image>(); background.color = new Color32(7, 20, 42, 255); background.raycastTarget = false;
        GameObject textObject = new GameObject("Ammo", typeof(RectTransform), typeof(Text)); textObject.transform.SetParent(panel.transform, false);
        Text text = textObject.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 15;
        text.color = new Color32(229, 246, 255, 255); text.raycastTarget = false; text.text = "WEAPON";
        RectTransform labelRect = text.rectTransform; labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0, 9); labelRect.sizeDelta = new Vector2(382, 24);
        GameObject fillObject = new GameObject("Progress", typeof(RectTransform), typeof(Image)); fillObject.transform.SetParent(panel.transform, false);
        Image fill = fillObject.GetComponent<Image>(); fill.raycastTarget = false;
        Image health = (Image)new SerializedObject(hud).FindProperty("healthFill").objectReferenceValue;
        fill.sprite = health.sprite; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0;
        RectTransform fillRect = fill.rectTransform; fillRect.anchoredPosition = new Vector2(0, -16); fillRect.sizeDelta = new Vector2(382, 8);
        panel.AddComponent<WeaponHud>().Configure(player, text, fill);
    }

    private static T Load<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);
    private static void BackupFile(string path)
    {
        if (!File.Exists(path)) return;
        string destination = Backup + "/" + path;
        if (File.Exists(destination)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(path, destination);
    }
}
