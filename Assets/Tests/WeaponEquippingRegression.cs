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

public class WeaponEquippingRegression : MonoBehaviour
{
    private readonly List<string> checks = new List<string>();
    private GameObject sandbox;
    private int loads;
    private bool background;
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/System Leveling/Run Weapon Equipping Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<WeaponEquippingRegression>() != null) return;
        GameObject root = new GameObject("Weapon Equipping Regression"); DontDestroyOnLoad(root);
        root.AddComponent<WeaponEquippingRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground; Application.runInBackground = true; Time.timeScale = 1;
        SceneManager.sceneLoaded += Loaded;
        IEnumerator tests = Tests(); string failure = "";
        while (true)
        {
            bool more = false; object wait = null;
            try { more = tests.MoveNext(); if (more) wait = tests.Current; }
            catch (Exception error) { failure = error.ToString(); }
            if (!more || failure.Length > 0) break;
            yield return wait;
        }
        SceneManager.sceneLoaded -= Loaded;
        if (sandbox != null) DestroyImmediate(sandbox);
        Time.timeScale = 1; Application.runInBackground = background;
        Directory.CreateDirectory("Captures/WeaponEquipping");
        File.WriteAllText("Captures/WeaponEquipping/regression.json", JsonUtility.ToJson(new Report
        { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("WEAPON EQUIPPING REGRESSION PASS: " + checks.Count);
        else Debug.LogError("WEAPON EQUIPPING REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        string before = Snapshot();
        SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
        float until = Time.realtimeSinceStartup + 15;
        while (loads == 0 && Time.realtimeSinceStartup < until) yield return null;
        Check(loads == 1, "Clean scene baseline loaded"); yield return null;
        FindAnyObjectByType<EnemySpawner>().enabled = false;
        PlayerStats mage = GameObject.Find("Player").GetComponent<PlayerStats>();
        mage.GetComponent<PlayerController>().enabled = false;
        mage.GetComponent<Rigidbody2D>().simulated = false;
        sandbox = new GameObject("Equipping test fixtures");
        List<PlayerStats> actors = new List<PlayerStats> { mage };
        string[] names = { "Archer", "Hunter", "Knight" };
        foreach (string name in names)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Characters/{name}.prefab");
            Check(prefab.GetComponentsInChildren<WeaponHolder>(true).Length == 1 && prefab.GetComponentsInChildren<WeaponController>(true).Length == 0,
                name + " prefab contains one neutral hand anchor and no baked weapon");
            GameObject actor = SpawnInactive(prefab);
            actor.transform.parent.gameObject.SetActive(true);
            actors.Add(actor.GetComponent<PlayerStats>());
        }
        yield return null;
        foreach (PlayerStats actor in actors)
        {
            WeaponHolder holder = actor.GetComponentInChildren<WeaponHolder>();
            Check(holder.CurrentWeapon != null && holder.CurrentWeapon.weaponData == actor.characterData.StartingWeapon.GetComponent<WeaponController>().weaponData,
                actor.characterData.name + " automatically uses its data default");
            holder.CurrentWeapon.enabled = false;
            Check(actor.GetComponent<Animator>() != null && actor.GetComponent<Collider2D>() != null && actor.GetComponentInChildren<PlayerCollector>() != null,
                actor.characterData.name + " retains animation, collision and pickups");
        }

        WeaponHud hud = FindAnyObjectByType<WeaponHud>();
        Text label = (Text)typeof(WeaponHud).GetField("label", Fields).GetValue(hud);
        string[] weapons = { "MagicBook", "Bow", "Crossbow", "Sword" };
        foreach (PlayerStats actor in actors)
        {
            WeaponHolder holder = actor.GetComponentInChildren<WeaponHolder>();
            CharacterScriptableObject data = actor.characterData;
            GameObject defaultPrefab = data.StartingWeapon;
            Vector3 anchor = holder.transform.localPosition;
            foreach (string name in weapons)
            {
                GameObject prefab = Weapon(name);
                WeaponController old = holder.CurrentWeapon;
                Check(holder.Equip(prefab), actor.characterData.name + " accepts " + name);
                WeaponController current = holder.CurrentWeapon;
                Check(old == current || old == null || !old.gameObject.activeSelf, "Previous weapon stops immediately before deferred destruction");
                current.enabled = false;
                // Disable automatic input, but initialize through the same production lifecycle method.
                if (old != current) current.SendMessage("Start");
                Check(current.transform.parent == holder.transform && current.transform.localPosition == Vector3.zero &&
                    current.transform.localRotation == Quaternion.identity && current.transform.localScale == Vector3.one,
                    actor.characterData.name + "/" + name + " mounts with local identity");
                WeaponGrip grip = current.GetComponent<WeaponGrip>();
                grip.Apply();
                SerializedObject profile = new SerializedObject(grip);
                SpriteRenderer visual = (SpriteRenderer)profile.FindProperty("visual").objectReferenceValue;
                Vector2 gripPoint = profile.FindProperty("spriteGrip").vector2Value;
                Vector2 localGrip = (Vector2.Scale(visual.sprite.rect.size, gripPoint) - visual.sprite.pivot) / visual.sprite.pixelsPerUnit;
                foreach (Vector2 direction in new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down })
                {
                    holder.PointerPosition = (Vector2)holder.transform.position + direction * 20;
                    Check(Vector2.Dot(holder.GetDirection(), direction) > 0.999f, actor.characterData.name + "/" + name + " aims " + direction);
                    Check(Vector3.Distance(visual.transform.TransformPoint(localGrip), visual.transform.parent.TransformPoint(profile.FindProperty("holdOffset").vector2Value)) < 0.002f,
                        "Weapon handle remains on the hand after rotating/flipping");
                    RangedWeaponController ranged = current as RangedWeaponController;
                    if (ranged != null)
                    {
                        if (ranged.weaponData.FireMode == WeaponFireMode.Crossbow)
                        {
                            ranged.Tick(2, false); ranged.Tick(0, true); ranged.Tick(0.1f, true); ranged.Tick(0, false);
                        }
                        else { ranged.Tick(0.5f, false); ranged.Tick(0, true); }
                        Check(ranged.LastProjectile != null && Vector3.Distance(ranged.LastProjectile.transform.position, grip.MuzzlePosition) < 0.002f,
                            actor.characterData.name + "/" + name + " fires at its muzzle");
                        Check(Vector2.Dot(ranged.LastProjectile.transform.right, direction) > 0.999f, "Projectile follows the selected character's aim");
                        DestroyImmediate(ranged.LastProjectile.gameObject);
                    }
                }
                Check(holder.Equip(prefab) && holder.CurrentWeapon == current, "Repeated selection reuses the same equipped instance");
                Check(holder.transform.localPosition == anchor && data.StartingWeapon == defaultPrefab, "Selecting a weapon does not alter the character anchor or default asset");
                if (actor == mage)
                {
                    hud.SendMessage("Update");
                    Check(current is RangedWeaponController ? label.text.Contains(current.weaponData.name.ToUpperInvariant()) : label.text == "MELEE",
                        "HUD follows current selection: " + name);
                    Check(actor.HasRangedWeapon == (current is RangedWeaponController), "Upgrade eligibility follows current weapon type");
                }
                yield return null;
                Check(holder.GetComponentsInChildren<WeaponController>(true).Length == 1, "Deferred cleanup leaves exactly one weapon controller");
            }
        }

        // Explicit choice before Awake must not be overwritten by the default.
        foreach (string name in names)
        {
            GameObject actor = SpawnInactive(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Characters/{name}.prefab"));
            WeaponHolder holder = actor.GetComponentInChildren<WeaponHolder>(true);
            GameObject choice = Weapon(name == "Archer" ? "Crossbow" : name == "Hunter" ? "Sword" : "Bow");
            Check(holder.Equip(choice), "Selection before activation is accepted: " + name);
            WeaponController selected = holder.CurrentWeapon;
            actor.transform.parent.gameObject.SetActive(true);
            selected.enabled = false;
            Check(holder.CurrentWeapon == selected && holder.GetComponentsInChildren<WeaponController>(true).Length == 1,
                "Awake preserves the explicit selection, not the class default: " + name);
        }
        WeaponHolder main = mage.GetComponentInChildren<WeaponHolder>();
        WeaponController retained = main.CurrentWeapon;
        Check(!main.Equip(null) && main.CurrentWeapon == retained, "Null selection preserves current weapon");
        GameObject invalidParent = new GameObject("Invalid source fixtures"); invalidParent.SetActive(false); invalidParent.transform.SetParent(sandbox.transform);
        GameObject invalid = new GameObject("Missing grip"); invalid.transform.SetParent(invalidParent.transform);
        invalid.AddComponent<WeaponController>().weaponData = retained.weaponData;
        Check(!main.Equip(invalid) && main.CurrentWeapon == retained && retained.gameObject.activeSelf, "Missing-grip selection cannot unequip the current weapon");
        GameObject missingData = Instantiate(Weapon("Bow"), invalidParent.transform); missingData.GetComponent<WeaponController>().weaponData = null;
        Check(!main.Equip(missingData) && main.CurrentWeapon == retained, "Missing-data selection preserves current weapon");

        // Revisit book after melee: the HUD must not retain its previous cached label/reference.
        Check(main.Equip(Weapon("MagicBook")), "Main character can return from melee to book");
        main.CurrentWeapon.enabled = false; main.CurrentWeapon.SendMessage("Start"); hud.SendMessage("Update");
        Check(label.text.Contains("MAGIC BOOK"), "HUD returns to ranged display after melee");
        RangedWeaponController book = (RangedWeaponController)main.CurrentWeapon;
        book.Tick(0, true); int ammo = book.State.Ammo;
        Check(main.Equip(Weapon("MagicBook")) && main.CurrentWeapon == book && book.State.Ammo == ammo, "Repeated selection does not reset spent ammo");
        if (book.LastProjectile != null) DestroyImmediate(book.LastProjectile.gameObject);
        Check(before == Snapshot(), "All character/weapon data and user texture import settings remain byte-identical");

        SceneManager.LoadSceneAsync("Assets/Scenes/SampleScene.unity");
        until = Time.realtimeSinceStartup + 15;
        while (loads < 2 && Time.realtimeSinceStartup < until) yield return null;
        Check(loads == 2, "Scene reload completes with neutral holders"); yield return null;
        WeaponHolder fresh = GameObject.Find("Player").GetComponentInChildren<WeaponHolder>();
        Check(fresh.CurrentWeapon is MagicBookController && ((RangedWeaponController)fresh.CurrentWeapon).State.Ammo == 8,
            "Fresh scene equips exactly the default book with a full magazine");
    }

    private GameObject SpawnInactive(GameObject prefab)
    {
        GameObject stage = new GameObject("Inactive spawn stage"); stage.SetActive(false); stage.transform.SetParent(sandbox.transform);
        GameObject actor = Instantiate(prefab, stage.transform); actor.transform.position = new Vector3(10000, 10000);
        actor.GetComponent<PlayerController>().enabled = false;
        actor.GetComponent<Rigidbody2D>().simulated = false;
        return actor;
    }

    private static GameObject Weapon(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Weapons/{name}.prefab");
    private static string Snapshot()
    {
        string snapshot = "";
        foreach (string path in Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories).OrderBy(path => path)) snapshot += path + File.ReadAllText(path);
        foreach (string name in new[] { "Bow", "Cross_bow" }) snapshot += File.ReadAllText($"Assets/MyAssets/Weapon/Base/{name}.png.meta");
        return snapshot;
    }
    private void Loaded(Scene scene, LoadSceneMode mode) { GameplayRegressionBaseline.Prepare(); loads++; }
    private void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks.Add(message); }
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
