#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class WeaponAmmoRegression : MonoBehaviour
{
    private readonly List<string> checks = new List<string>();
    private GameObject sandbox;
    private int loads;
    private bool background;
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/System Leveling/Run Weapon Ammo Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<WeaponAmmoRegression>() != null) return;
        GameObject root = new GameObject("Weapon Ammo Regression"); DontDestroyOnLoad(root);
        root.AddComponent<WeaponAmmoRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground; Application.runInBackground = true; Time.timeScale = 1;
        SceneManager.sceneLoaded += Loaded;
        IEnumerator tests = Tests(); string error = "";
        while (true)
        {
            bool more = false; object wait = null;
            try { more = tests.MoveNext(); if (more) wait = tests.Current; }
            catch (Exception exception) { error = exception.ToString(); }
            if (!more || error.Length > 0) break;
            yield return wait;
        }
        SceneManager.sceneLoaded -= Loaded;
        if (sandbox != null) DestroyImmediate(sandbox);
        Time.timeScale = 1; Application.runInBackground = background;
        Directory.CreateDirectory("Captures/WeaponAmmo");
        File.WriteAllText("Captures/WeaponAmmo/regression.json", JsonUtility.ToJson(new Report
        { result = error.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = error }, true));
        if (error.Length == 0) Debug.Log("WEAPON AMMO REGRESSION PASS: " + checks.Count);
        else Debug.LogError("WEAPON AMMO REGRESSION FAIL: " + error);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
        float until = Time.realtimeSinceStartup + 15;
        while (loads < 1 && Time.realtimeSinceStartup < until) yield return null;
        Check(loads == 1, "Clean gameplay baseline loaded"); yield return null;
        FindAnyObjectByType<EnemySpawner>().enabled = false;
        RangedWeaponController sceneWeapon = FindAnyObjectByType<PlayerStats>().GetComponentInChildren<RangedWeaponController>();
        sceneWeapon.enabled = false;
        Check(sceneWeapon is MagicBookController && sceneWeapon.State.Ammo == 8, "Scene Mage has one initialized book with eight rounds");
        Check(FindAnyObjectByType<PlayerStats>().GetComponentsInChildren<WeaponController>().Length == 1, "Scene does not fire a second weapon");
        PureTests();
        sandbox = new GameObject("Weapon Test Fixtures");
        HoldingTests();
        foreach (string name in new[] { "Knight", "Archer", "Hunter" })
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Characters/{name}.prefab");
            WeaponController[] weapons = prefab.GetComponentsInChildren<WeaponController>(true);
            Check(weapons.Length == 0, name + " is neutral and has no baked weapon child");
            Check(prefab.GetComponent<Animator>() != null && prefab.GetComponentInChildren<PlayerCollector>() != null && prefab.GetComponent<CapsuleCollider2D>() != null,
                name + " retains animation, collector and body");
            WeaponController defaultWeapon = prefab.GetComponent<PlayerStats>().characterData.StartingWeapon.GetComponent<WeaponController>();
            Check(name == "Knight" ? defaultWeapon is MeleeWeaponController : defaultWeapon is RangedWeaponController,
                name + " retains the approved default weapon in data");
        }
        RangedWeaponController cross = Fixture("Hunter");
        cross.Tick(0, true); cross.Tick(3, true); cross.Tick(0, false);
        Check(cross.ShotsFired == 1 && cross.State.Ammo == 0, "Actual crossbow release instantiates one bolt and consumes one round");
        ProjectileWeaponBehaviour bolt = cross.LastProjectile;
        Check(Vector3.Distance(bolt.transform.position, cross.GetComponent<WeaponGrip>().MuzzlePosition) < 0.002f,
            "Actual crossbow bolt originates at its configured muzzle");
        Check(Near(bolt.CurrentDamage, 96) && Near(bolt.CurrentSpeed, 18), "Bolt receives charged damage and speed from the firing weapon");
        GameObject baseBolt = cross.weaponData.Prefab;
        Check(Near(bolt.transform.localScale.x, baseBolt.transform.localScale.x * 2), "Charged bolt sprite scale doubles");
        Collider2D shape = bolt.GetComponent<Collider2D>(); Physics2D.SyncTransforms();
        Check(shape != null && shape.bounds.size.x > 1.2f, "Charged bolt collider grows with its visual");
        float damage = bolt.CurrentDamage;
        PlayerStats owner = cross.GetComponentInParent<PlayerStats>();
        owner.experienceCap = 100; owner.IncreaseExperience(100);
        Check(owner.TryApplyUpgrade(UpgradeKind.Damage) && bolt.CurrentDamage == damage, "Already-fired projectile keeps its damage snapshot after an upgrade");
        // The same EnemyStats can have multiple colliders; one projectile must only damage it once.
        GameObject enemy = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Skeleton.prefab"), new Vector3(20000, 20000), Quaternion.identity, sandbox.transform);
        enemy.GetComponent<Enemy>().enabled = false;
        EnemyStats target = enemy.GetComponent<EnemyStats>();
        MethodInfo hit = typeof(ProjectileWeaponBehaviour).GetMethod("OnTriggerEnter2D", Fields);
        ProjectileWeaponBehaviour gentle = Instantiate(baseBolt, new Vector3(20000, 20000), Quaternion.identity, sandbox.transform).GetComponent<ProjectileWeaponBehaviour>();
        gentle.InitializeShot(cross.weaponData, null, Vector3.right, 1, 1);
        float health = target.currentHealth;
        hit.Invoke(gentle, new object[] { enemy.GetComponent<Collider2D>() });
        float after = target.currentHealth;
        hit.Invoke(gentle, new object[] { enemy.GetComponent<Collider2D>() });
        Check(after < health && target.currentHealth == after, "Duplicate target collisions do not deal damage twice");
        owner.IncreaseExperience(owner.experienceCap);
        Check(owner.TryApplyUpgrade(UpgradeKind.Ammo), "Ammo upgrade can be selected on a real ranged owner");
        owner.IncreaseExperience(owner.experienceCap);
        Check(owner.TryApplyUpgrade(UpgradeKind.Reload), "Reload upgrade can be selected on a real ranged owner");
        cross.Tick(1.5f, false);
        Check(cross.State.Capacity == 1 && Near(cross.State.ChargeDuration, 3.5f / 1.2f), "Runtime crossbow upgrades add charge ceiling and shorten actual charge duration");
        cross.Tick(0, true); cross.Tick(cross.State.ChargeDuration, true); cross.Tick(0, false);
        Check(Near(cross.LastProjectile.CurrentDamage, 24 * 4.5f * owner.DamageMultiplier), "Upgraded crossbow applies both charge and owner damage bonuses");
        RangedWeaponController bow = Fixture("Archer");
        bow.Tick(0, true);
        Check(bow.LastProjectile != null && Near(bow.LastProjectile.CurrentDamage, 22), "Actual bow emits an arrow with configured damage");
        int rounds = bow.State.Ammo;
        bow.Tick(100, true); Check(bow.State.Ammo == rounds - 1, "Controller long frame fires at most once");
        rounds = bow.State.Ammo; bow.Tick(1.1f, false);
        Check(bow.State.Ammo == rounds + 1, "Actual bow recovers one arrow when released");
        RangedWeaponController book = Fixture("Mage");
        int count = book.State.Ammo;
        GameObject original = book.weaponData.Prefab;
        // Clone only the data under test; never edit an asset to exercise invalid references.
        WeaponScriptableObject invalidData = Instantiate(book.weaponData);
        SerializedObject so = new SerializedObject(invalidData); so.FindProperty("prefab").objectReferenceValue = null; so.ApplyModifiedPropertiesWithoutUndo();
        book.weaponData = invalidData;
        book.Tick(1, true); Check(book.State.Ammo == count && book.ShotsFired == 0, "Missing projectile prefab does not spend ammo");
        book.weaponData = AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Magic Book.asset"); DestroyImmediate(invalidData);
        book.Tick(0, true); Check(book.LastProjectile != null && Near(book.LastProjectile.CurrentDamage, 14), "Book projectile uses migrated weapon data rather than stale prefab values");
        cross.Tick(2, false); cross.Tick(0, true); cross.Tick(1, true);
        count = cross.ShotsFired; Time.timeScale = 0; cross.Tick(20, true);
        Check(!cross.State.IsCharging && cross.ShotsFired == count, "Pause cancels charge without emitting a bolt");
        Time.timeScale = 1; cross.Tick(0, false);
        Check(cross.ShotsFired == count, "Releasing after pause does not fire a stored charge");
        book.Tick(0, true, true); count = book.ShotsFired; book.Tick(1, true);
        Check(book.ShotsFired == count, "UI click requires a release before firing resumes");
        book.Tick(0, false); book.Tick(0.4f, true);
        Check(book.ShotsFired == count + 1, "Ranged input works again after UI release gate");
        while (book.State.Ammo > 0) book.Tick(0.4f, true);
        float reloadBeforePause = book.State.ReloadFraction;
        Time.timeScale = 0; book.Tick(100, false);
        Check(book.State.Ammo == 0 && book.State.ReloadFraction == reloadBeforePause, "Pause freezes an actual in-progress magazine reload");
        Time.timeScale = 1; book.Tick(0.3f, false);
        Check(book.State.ReloadFraction > reloadBeforePause && book.State.Ammo == 0, "Magazine reload resumes rather than completing instantly after pause");
        WeaponHud hud = FindAnyObjectByType<WeaponHud>();
        hud.SendMessage("Update");
        Check(Get<Text>(hud, "label").text.Contains("8 / 8"), "Weapon HUD displays the actual scene book ammo");
        Check(LevelUpSelection.Title(UpgradeKind.Ammo) == "CAPACITY" && LevelUpSelection.Title(UpgradeKind.Reload) == "RECHARGE", "Level-up has labels for both new upgrades");
        RangedWeaponState loading = new RangedWeaponState(Data("Magic Book"));
        for (int i = 0; i < 8; i++) loading.Tick(0.4f, true);
        loading.Tick(0.5f, false); float progress = loading.ReloadFraction;
        Check(loading.IsReloading && Near(progress, 0.5f / 1.8f), "Magazine exposes measured reload progress for HUD");

        // Exercise the actual level-up offers and buttons on a ranged scene owner.
        PlayerStats scenePlayer = sceneWeapon.GetComponentInParent<PlayerStats>();
        LevelUpSelection selection = FindAnyObjectByType<LevelUpSelection>();
        foreach (UpgradeKind kind in new[] { UpgradeKind.Ammo, UpgradeKind.Reload })
        {
            scenePlayer.IncreaseExperience(scenePlayer.experienceCap - scenePlayer.experience);
            int pick = -1;
            for (int attempt = 0; attempt < 50 && pick < 0; attempt++)
            {
                typeof(LevelUpSelection).GetMethod("PresentChoices", Fields).Invoke(selection, null);
                for (int i = 0; i < 3; i++) if (selection.ChoiceAt(i) == kind) pick = i;
            }
            Check(selection.IsOpen && Time.timeScale == 0 && pick >= 0, "Actual ranged level-up pool offers " + kind);
            yield return null;
            int beforeShots = sceneWeapon.ShotsFired;
            sceneWeapon.Tick(10, true);
            Check(sceneWeapon.ShotsFired == beforeShots, "Level-up modal blocks ranged trigger for " + kind);
            selection.Choose(pick);
            Check((kind == UpgradeKind.Ammo ? scenePlayer.AmmoUpgradeLevel : scenePlayer.ReloadUpgradeLevel) == 1 && !selection.IsOpen,
                "Actual UI selection applies " + kind + " and resumes gameplay");
            yield return null; yield return null;
        }
        sceneWeapon.Tick(0, false);
        Check(sceneWeapon.State.Capacity == 9 && sceneWeapon.State.Ammo == 8 && Near(sceneWeapon.State.ReloadDuration, 1.5f), "Scene book consumes both real UI upgrades without granting free ammo");
        GameObject knight = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Characters/Knight.prefab"), new Vector3(15000, 15000), Quaternion.identity, sandbox.transform);
        knight.GetComponent<PlayerController>().enabled = false;
        knight.GetComponent<Rigidbody2D>().simulated = false;
        MeleeWeaponController sword = knight.GetComponentInChildren<MeleeWeaponController>(); sword.enabled = false;
        sword.SendMessage("Start");
        int slashes = FindObjectsByType<MeleeWeaponBehaviour>().Length;
        typeof(MeleeWeaponController).GetMethod("Attack", Fields).Invoke(sword, null);
        Check(FindObjectsByType<MeleeWeaponBehaviour>().Length == slashes + 1, "Knight sword still creates a real melee slash with the unchanged controller");
        Check(!knight.GetComponent<PlayerStats>().HasRangedWeapon && !knight.GetComponent<PlayerStats>().TryApplyUpgrade(UpgradeKind.Ammo), "Melee owner does not accept ranged-only ammo upgrades");
        // A real death event, then the real Restart path (not a scheduler reset).
        scenePlayer.Kill(); count = bow.ShotsFired; int ammo = bow.State.Ammo;
        bow.Tick(100, true);
        Check(GameOverController.GameplayStopped && bow.ShotsFired == count && bow.State.Ammo == ammo, "Game Over prevents firing and ammo/reload changes");
        FindAnyObjectByType<GameOverController>().Restart();
        until = Time.realtimeSinceStartup + 15;
        while (loads < 2 && Time.realtimeSinceStartup < until) yield return null;
        Check(loads == 2, "Actual Restart reloads the scene"); yield return null;
        PlayerStats fresh = FindAnyObjectByType<PlayerStats>();
        RangedWeaponController freshBook = fresh.GetComponentInChildren<RangedWeaponController>();
        Check(fresh.AmmoUpgradeLevel == 0 && fresh.ReloadUpgradeLevel == 0 && freshBook.State.Ammo == 8 && !freshBook.State.IsReloading && !freshBook.State.IsCharging,
            "Restart restores full starting magazine, clears upgrades and charge/reload state");
    }

    private void HoldingTests()
    {
        int firstCheck = checks.Count;
        GameObject root = new GameObject("Aim fixture"); root.transform.SetParent(sandbox.transform);
        WeaponHolder holder = root.AddComponent<WeaponHolder>();
        Check(Vector2.Dot(holder.GetDirection(), Vector2.right) > 0.999f, "Holder keeps its authored direction until a pointer is supplied");
        root.transform.localScale = new Vector3(2, 3, 1);
        foreach (Vector2 direction in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
        {
            holder.PointerPosition = (Vector2)root.transform.position + direction * 10;
            Check(Vector2.Dot(holder.GetDirection(), direction) > 0.999f, "Aim follows pointer immediately: " + direction);
            Check(Mathf.Abs(root.transform.localScale.x) == 2 && Mathf.Abs(root.transform.localScale.y) == 3,
                "Facing preserves authored holder scale: " + direction);
        }
        holder.PointerPosition = Vector2.left * 10;
        holder.PointerPosition = Vector2.up * 10;
        Check(root.transform.localScale.y < 0, "Vertical aim does not flicker between facing sides");
        Vector2 before = holder.GetDirection();
        holder.PointerPosition = root.transform.position;
        Check(Vector2.Dot(holder.GetDirection(), before) > 0.999f, "Pointer on the grip does not reset the aim");
        holder.PointerPosition = new Vector2(float.NaN, 1);
        Check(Vector2.Dot(holder.GetDirection(), before) > 0.999f, "Invalid pointer cannot corrupt the transform");
        Time.timeScale = 0; holder.PointerPosition = Vector2.right * 10;
        Check(Vector2.Dot(holder.GetDirection(), before) > 0.999f, "Aim stays frozen during pause");
        Time.timeScale = 1;
        Check(Vector2.Dot(holder.GetDirection(), Vector2.right) > 0.999f, "Aim resumes from the latest pointer after pause");
        root.transform.localScale = Vector3.one;

        foreach (string name in new[] { "MagicBook", "Bow", "Crossbow", "Sword" })
        {
            GameObject weapon = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Weapons/{name}.prefab"), root.transform);
            foreach (WeaponController controller in weapon.GetComponentsInChildren<WeaponController>()) controller.enabled = false;
            WeaponGrip grip = weapon.GetComponent<WeaponGrip>();
            Check(grip != null, name + " has its own holding calibration");
            grip.Apply();
            SerializedObject so = new SerializedObject(grip);
            SpriteRenderer visual = (SpriteRenderer)so.FindProperty("visual").objectReferenceValue;
            float targetSize = so.FindProperty("visualSize").floatValue;
            float actual = Mathf.Max(visual.sprite.bounds.size.x * visual.transform.localScale.x, visual.sprite.bounds.size.y * visual.transform.localScale.y);
            Check(Near(actual, targetSize) && targetSize <= 0.52f, name + " uses calibrated size rather than stale texture PPU scale");
            Vector2 point = so.FindProperty("spriteGrip").vector2Value;
            Vector2 spritePoint = (Vector2.Scale(visual.sprite.rect.size, point) - visual.sprite.pivot) / visual.sprite.pixelsPerUnit;
            Vector3 actualGrip = visual.transform.TransformPoint(spritePoint);
            Vector3 expectedGrip = visual.transform.parent.TransformPoint(so.FindProperty("holdOffset").vector2Value);
            Check(Vector3.Distance(actualGrip, expectedGrip) < 0.001f, name + " sprite handle sits exactly on its hand point");
            if (name == "Bow" || name == "Crossbow")
            {
                Vector3 artForward = name == "Bow" ? new Vector3(-1, 1) : new Vector3(1, 1);
                foreach (Vector2 direction in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
                {
                    holder.PointerPosition = direction * 10;
                    Vector3 barrel = visual.transform.TransformVector(artForward).normalized;
                    Check(Vector2.Dot(barrel, holder.GetDirection()) > 0.999f, name + " art points along its firing direction: " + direction);
                }
            }
            DestroyImmediate(weapon);
        }

        // Reproduce the exact class of importer change without touching a user texture or .meta file.
        GameObject synthetic = new GameObject("PPU fixture"); synthetic.transform.SetParent(root.transform, false);
        GameObject image = new GameObject("Visual", typeof(SpriteRenderer)); image.transform.SetParent(synthetic.transform, false);
        Texture2D texture = new Texture2D(32, 32);
        Sprite at32 = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32, 0, SpriteMeshType.FullRect);
        Sprite at64 = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 64, 0, SpriteMeshType.FullRect);
        try
        {
            SpriteRenderer visual = image.GetComponent<SpriteRenderer>(); visual.sprite = at32;
            WeaponGrip grip = synthetic.AddComponent<WeaponGrip>();
            SerializedObject so = new SerializedObject(grip); so.FindProperty("visual").objectReferenceValue = visual; so.ApplyModifiedPropertiesWithoutUndo();
            grip.Apply(); float originalSize = visual.sprite.bounds.size.x * visual.transform.localScale.x;
            visual.sprite = at64; grip.Apply();
            Check(Near(originalSize, visual.sprite.bounds.size.x * visual.transform.localScale.x), "Changing PPU 32 to 64 does not change held weapon size");
        }
        finally { DestroyImmediate(synthetic); DestroyImmediate(at32); DestroyImmediate(at64); DestroyImmediate(texture); }
        Directory.CreateDirectory("Captures/WeaponHolder");
        File.WriteAllText("Captures/WeaponHolder/holding-regression.json", JsonUtility.ToJson(new Report
        { result = "PASS", utc = DateTime.UtcNow.ToString("o"), checks = checks.GetRange(firstCheck, checks.Count - firstCheck).ToArray(), failure = "" }, true));
        DestroyImmediate(root);
    }

    private void PureTests()
    {
        WeaponScriptableObject book = Data("Magic Book"), bow = Data("Bow"), cross = Data("Crossbow");
        RangedWeaponState state = new RangedWeaponState(book);
        Check(state.Ammo == 8 && !state.Tick(1, false), "Book begins full and does not auto-fire");
        Check(state.Tick(0, true) && !state.Tick(0.39f, true) && state.Tick(0.02f, true), "Book enforces 0.4s fire interval");
        for (int i = 0; i < 6; i++) state.Tick(0.4f, true);
        Check(state.Ammo == 0 && state.IsReloading, "Eighth book shot starts magazine reload");
        state.Tick(1.79f, true); Check(state.Ammo == 0, "Book does not refill early");
        state.Tick(0.02f, true); Check(state.Ammo == 8 && !state.IsReloading, "Book refills its entire magazine at 1.8s");
        state.SetUpgrades(1, 1); Check(state.Capacity == 9 && state.Ammo == 8 && Near(state.ReloadDuration, 1.5f), "Magazine upgrades increase cap without free ammo and shorten reload");
        state = new RangedWeaponState(bow);
        for (int i = 0; i < 7; i++) Check(state.Tick(i == 0 ? 0 : 0.25f, true), "Bow can fire ready arrow " + i);
        Check(state.Ammo == 0 && !state.Tick(100, true) && state.Ammo == 0, "An empty bow cannot regenerate while trigger is held");
        state.Tick(0.45f, false); state.Tick(0.64f, false); Check(state.Ammo == 0, "Bow waits idle delay plus per-arrow recovery");
        state.Tick(0.01f, false); Check(state.Ammo == 1, "Bow recovers exactly one arrow after 1.10s");
        state.Tick(0.65f, false); Check(state.Ammo == 2, "Later bow arrows recover at 0.65s each");
        state.Tick(0.3f, false); state.Tick(0, true); state.Tick(1.09f, false); Check(state.Ammo == 1, "Resuming fire discards partial recovery rather than duplicating arrows");
        state.Tick(0.02f, false); Check(state.Ammo == 2, "Recovery restarts normally after fire interruption");
        state.Interrupt(); state.Tick(1.1f, false);
        Check(state.Ammo == 3, "A released trigger recovers bow arrows even while clearing an input fence");
        state = new RangedWeaponState(cross);
        state.Tick(0, true); state.Tick(1.5f, true);
        Check(!state.Tick(0, true) && state.IsCharging && Near(state.ChargeFraction, 0.5f), "Crossbow holds half charge without auto-firing");
        Check(state.Tick(0, false) && Near(state.ShotDamageFactor, 2.5f) && Near(state.ShotSizeFactor, 1.5f), "Early crossbow release interpolates damage and size");
        state.Tick(1.5f, true); state.Tick(10, true); Check(!state.IsCharging, "Holding through reload does not automatically charge a second shot");
        state.Tick(0, false); state.Tick(0, true); state.Tick(100, true);
        Check(Near(state.ChargeFraction, 1) && state.Ammo == 1, "Crossbow clamps at full charge and waits for release");
        Check(state.Tick(0, false) && Near(state.ShotDamageFactor, 4) && Near(state.ShotSizeFactor, 2), "Full crossbow release reaches approved damage and scale");
        state.SetUpgrades(1, 1);
        Check(state.Capacity == 1 && Near(state.ChargeDuration, 3.5f / 1.2f) && Near(state.ReloadDuration, 1.5f / 1.2f), "Crossbow ammo upgrade is charge ceiling, not extra arrows");
        state.Tick(2, false); state.Tick(0, true); state.Tick(100, true); state.Tick(0, false);
        Check(Near(state.ShotDamageFactor, 4.5f) && Near(state.ShotSizeFactor, 2 + 1f / 6f), "Extended ceiling has a greater damage and size reward");
        state = new RangedWeaponState(book);
        Check(state.Tick(1000, true) && state.Ammo == 7, "A long frame does not catch up an unlimited burst");
        Check(!state.Tick(float.NaN, true) && !state.Tick(-1, true), "Invalid delta cannot spend ammo");
        WeaponScriptableObject invalid = Instantiate(cross);
        SerializedObject bad = new SerializedObject(invalid); bad.FindProperty("magazineSize").intValue = -1;
        bad.FindProperty("reloadTime").floatValue = 0; bad.FindProperty("chargeTime").floatValue = float.NaN;
        bad.ApplyModifiedPropertiesWithoutUndo(); invalid.ValidateRangedSettings();
        Check(invalid.MagazineSize == 1 && invalid.ReloadTime > 0 && invalid.ChargeTime > 0, "Data validation normalizes invalid ammo and charge/reload times");
        DestroyImmediate(invalid);
    }

    private RangedWeaponController Fixture(string character)
    {
        GameObject root = new GameObject(character + " weapon fixture"); root.SetActive(false); root.transform.SetParent(sandbox.transform);
        root.transform.position = new Vector3(10000, 10000);
        PlayerStats owner = root.AddComponent<PlayerStats>(); owner.characterData = AssetDatabase.LoadAssetAtPath<CharacterScriptableObject>($"Assets/Scriptable Object/Character/{character}.asset");
        owner.enabled = false; owner.experienceCap = 100;
        owner.levelRanges = new List<PlayerStats.LevelRange> { new PlayerStats.LevelRange { startLevel = 1, endLevel = 999, experienceCapIncrease = 100 } };
        GameObject holder = new GameObject("WeaponHolder"); holder.transform.SetParent(root.transform, false); holder.AddComponent<WeaponHolder>();
        root.SetActive(true);
        RangedWeaponController weapon = root.GetComponentInChildren<RangedWeaponController>();
        Check(weapon != null, character + " equips StartingWeapon through the actual WeaponHolder");
        weapon.enabled = false; weapon.SendMessage("Start");
        return weapon;
    }

    private static WeaponScriptableObject Data(string name) => AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>($"Assets/Scriptable Object/Weapon/{name}.asset");
    private static T Get<T>(object instance, string name) => (T)instance.GetType().GetField(name, Fields).GetValue(instance);
    private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.001f;
    private void Check(bool pass, string name) { if (!pass) throw new Exception(name); checks.Add(name); }
    private void Loaded(Scene scene, LoadSceneMode mode) { GameplayRegressionBaseline.Prepare(); loads++; }
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
