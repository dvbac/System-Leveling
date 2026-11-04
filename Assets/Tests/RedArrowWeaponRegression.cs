#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RedArrowWeaponRegression : MonoBehaviour
{
    private const string Output = "Captures/RedArrowWeapon";
    private const string WeaponPath = "Assets/Prefab/Weapons/RedArrow.prefab";
    private const string Source = "Assets/MyAssets/Weapon/Base/RedArrow.png";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private readonly List<string> checks = new List<string>();
    private readonly List<GameObject> fixtures = new List<GameObject>();
    private bool background;
    private PlayerStats player;
    private readonly Vector3 arena = new Vector3(10000, 10000);

    [MenuItem("Tools/System Leveling/Red Arrow/Run Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<RedArrowWeaponRegression>() != null) return;
        var go = new GameObject("Red Arrow Regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(go);
        var runner = go.AddComponent<RedArrowWeaponRegression>();
        runner.background = Application.runInBackground; Application.runInBackground = true;
        Time.timeScale = 1; EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private IEnumerator Start()
    {
        Directory.CreateDirectory(Output); var run = Tests(); string failure = "";
        while (true)
        {
            object wait;
            try { if (!run.MoveNext()) break; wait = run.Current; }
            catch (Exception error) { failure = error.ToString(); break; }
            yield return wait;
        }
        foreach (var go in fixtures) if (go != null) Destroy(go);
        Application.runInBackground = background; Time.timeScale = 1;
        File.WriteAllText(Output + "/runtime.json", JsonUtility.ToJson(new Report
        { result = failure == "" ? "PASS" : "FAIL", checks = checks.ToArray(), failure = failure }, true));
        if (failure == "") Debug.Log("RED ARROW REGRESSION PASS: " + checks.Count);
        else Debug.LogError("RED ARROW REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        string source = Hash(Source), catalogFile = File.ReadAllText(LoadoutLauncher.SelectionScene);
        int realSouls = PlayerPrefs.GetInt("SystemLeveling.Soul.Total.v1", 0);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPath);
        var data = prefab.GetComponent<RedArrowController>().weaponData;
        Check(data.Damage == 3 && data.Speed == 6 && data.Cooldown == 0.7f && data.MagazineSize == 9 && data.ReloadTime == 1.5f && data.BaseBounces == 1 && data.Pierce == 1,
            "AC-002/004: retuned damage3, speed6, interval0.7s, ammo9, unchanged homing/bounce/reload");
        Check(data.Prefab.GetComponent<RedArrowProjectile>() != null && prefab.GetComponent<WeaponGrip>().IsConfigured,
            "AC-001: homing projectile and calibrated grip are saved in assets");
        foreach (string name in new[] { "MagicBook", "Bow", "Crossbow", "Sword" })
            Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/" + name + ".prefab").GetComponent<WeaponController>().weaponData.BaseBounces == 0,
                "AC-006: no new base bounce on legacy " + name);

        foreach (bool snow in new[] { false, true })
        {
            yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene); yield return null;
            var ui = FindAnyObjectByType<LoadoutSelection>(); int index = Array.IndexOf(ui.weaponPrefabs, prefab);
            Check(ui.weaponPrefabs.Length == 4 && ui.weaponPrefabs.All(p => p.GetComponent<MeleeWeaponController>() == null), "Sword is hidden from weapon selection while ranged choices remain");
            Check(index >= 0 && ui.weaponPrefabs.Count(p => p == prefab) == 1 && index == ui.weaponPrefabs.Length - 1,
                "AC-001: Red Arrow appears once at catalog end");
            ui.SelectCharacter(0); ui.PreviewWeapon(index);
            Check(ui.weaponView.description.text.Contains("chase") && ui.weaponView.statValues.text.Contains("9"), "AC-001: selection describes seeking arrows and correct ammo");
            if (!snow) { yield return new WaitForEndOfFrame(); Capture("selection.png"); }
            ui.SelectWeapon(index); var launcher = ui.GetComponent<LoadoutLauncher>();
            if (snow) launcher.SelectSnowMap(); else launcher.SelectGrassMap();
            string scene = snow ? LoadoutLauncher.SnowScene : LoadoutLauncher.GameplayScene;
            float timeout = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != scene && Time.realtimeSinceStartup < timeout) yield return null;
            yield return null; yield return null;
            Check(SceneManager.GetActiveScene().path == scene, "AC-001: selected weapon launches " + scene);
            player = PlayerController.Instance.GetComponent<PlayerStats>(); player.GetComponent<PlayerController>().enabled = false;
            foreach (var spawner in FindObjectsByType<EnemySpawner>()) spawner.enabled = false;
            foreach (var enemy in FindObjectsByType<EnemyStats>()) Destroy(enemy.gameObject);
            var holder = player.GetComponentInChildren<WeaponHolder>(); var weapon = holder.CurrentWeapon as RedArrowController;
            Check(weapon != null && weapon.weaponData == data, "AC-001: gameplay retains Red Arrow instead of default weapon");
            weapon.enabled = false; holder.PointerPosition = (Vector2)player.transform.position + Vector2.right * 10;
            weapon.Tick(0, false); weapon.Tick(0, true);
            Check(weapon.State.Ammo == 8 && weapon.LastProjectile is RedArrowProjectile, "AC-002: real controller fires one seeking arrow for one ammo");
            float angle = Vector3.SignedAngle(Vector3.right, Read<Vector3>(weapon.LastProjectile, "direction"), Vector3.forward);
            Check(Mathf.Abs(angle) <= 30.01f, "AC-002: initial spread bounded to plus/minus 30 degrees");
            weapon.Tick(0.69f, true); Check(weapon.State.Ammo == 8, "AC-002: cooldown prevents early shot");
            weapon.Tick(0.02f, true); Check(weapon.State.Ammo == 7, "AC-002: next shot after 0.7 seconds");
            for (int i = 0; i < 7; i++) weapon.Tick(0.71f, true);
            Check(weapon.State.Ammo == 0 && weapon.State.IsReloading, "AC-002: nine shots empty magazine and start reload");
            var hud = FindAnyObjectByType<WeaponHud>(); hud.SendMessage("Update");
            Check(Read<Text>(hud, "label").text == "0 / 9", "AC-002: actual HUD follows Red Arrow ammo");
            weapon.Tick(1.49f, false); Check(weapon.State.Ammo == 0, "AC-002: no reload completion before 1.5s");
            weapon.Tick(0.02f, false); Check(weapon.State.Ammo == 9 && !weapon.State.IsReloading, "AC-002: full refill after 1.5s");
            weapon.Tick(.7f, true); weapon.Tick(.2f, false);
            Check(weapon.State.IsReloading && weapon.State.Ammo == 8, "Idle Red Arrow starts partial recovery with ammo remaining");
            int recovered = weapon.State.AmmoRecovered;
            weapon.Tick(.51f, true);
            Check(!weapon.State.IsReloading && weapon.State.Ammo == 7 && weapon.State.AmmoRecovered == recovered, "Firing Red Arrow interrupts recovery and spends only existing ammo");
            weapon.Tick(weapon.State.ReloadDuration, false);
            int shots = weapon.ShotsFired; Time.timeScale = 0; weapon.Tick(10, true);
            Check(weapon.ShotsFired == shots && weapon.State.Ammo == 9, "AC-002: pause cannot fire or consume ammo"); Time.timeScale = 1;
            weapon.Tick(0, false); weapon.Tick(0, true, true);
            Check(weapon.ShotsFired == shots, "AC-002: pointer over UI cannot shoot through");
            yield return new WaitForSeconds(0.12f); yield return new WaitForEndOfFrame(); Capture(snow ? "snow.png" : "forest.png");
            foreach (var shot in FindObjectsByType<ProjectileWeaponBehaviour>()) Destroy(shot.gameObject);
            yield return null;
        }

        // Isolated far-away fixtures, using production target acquisition and hit callbacks.
        var contact = Target(arena);
        contact.gameObject.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        var contactShot = Shot(data, null, arena); contactShot.Initialize(Vector3.zero); contactShot.enabled = true;
        Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return null;
        Check(contact.currentHealth == 10000 - data.Damage, "AC-004/006: real trigger physics damages a kinematic boss-style target");
        if (contactShot != null) Destroy(contactShot.gameObject);
        Destroy(contact.gameObject); yield return null;
        var a = Target(arena + new Vector3(3, 1)); var b = Target(arena + new Vector3(4, -1));
        var seeking = Shot(data, null, arena);
        seeking.TickHoming(0.01f);
        float heading = Vector3.SignedAngle(Vector3.right, Read<Vector3>(seeking, "direction"), Vector3.forward);
        Check(seeking.Target == a && heading > 0 && heading <= 7.21f, "AC-003: nearest live target acquired with bounded turn speed");
        a.transform.position = arena + new Vector3(-2, 3); Physics2D.SyncTransforms();
        Vector3 previous = Read<Vector3>(seeking, "direction"); seeking.TickHoming(0.1f);
        Check(Vector3.Angle(previous, Read<Vector3>(seeking, "direction")) > 1 && Vector3.Angle(previous, Read<Vector3>(seeking, "direction")) <= 72.01f,
            "AC-003: steering follows target motion without instantly snapping");
        Time.timeScale = 0; previous = Read<Vector3>(seeking, "direction"); seeking.TickHoming(1);
        Check(Read<Vector3>(seeking, "direction") == previous, "AC-003: paused homing does not turn"); Time.timeScale = 1;
        a.Kill(); seeking.TickHoming(0.2f); Check(seeking.Target == b, "AC-003: dead target replaced by another live enemy");
        b.gameObject.SetActive(false); previous = Read<Vector3>(seeking, "direction"); seeking.TickHoming(0.2f);
        Check(seeking.Target == null && Read<Vector3>(seeking, "direction") == previous, "AC-003: inactive targets ignored; no target preserves heading");
        Destroy(seeking.gameObject); yield return null;
        a = Target(arena + Vector3.right * 2); b = Target(arena + Vector3.right * 4); var c = Target(arena + Vector3.right * 6);
        var shotA = Shot(data, null, a.transform.position); float hpA = a.currentHealth, hpB = b.currentHealth, hpC = c.currentHealth;
        Hit(shotA, a); Hit(shotA, a);
        Check(a.currentHealth == hpA - data.Damage && shotA.CurrentPierce == 1 && Read<int>(shotA, "bouncesRemaining") == 0, "AC-004: first hit consumes base bounce, repeated collider cannot hit same enemy twice");
        shotA.TickHoming(0.2f); Check(shotA.Target == b, "AC-003/004: post-bounce homing excludes previously hit target");
        Hit(shotA, b); Hit(shotA, c);
        Check(b.currentHealth == hpB - data.Damage && c.currentHealth == hpC, "AC-004: second enemy takes full damage, no third base hit");
        yield return null; Check(shotA == null, "AC-004: projectile destroyed when bounce budget exhausted");
        var piercing = Shot(data, null, a.transform.position, 1);
        Hit(piercing, a); Check(piercing.CurrentPierce == 1 && Read<int>(piercing, "bouncesRemaining") == 1, "AC-004: added pierce is spent before bounce");
        Hit(piercing, b); Check(Read<int>(piercing, "bouncesRemaining") == 0 && piercing.CurrentPierce == 1, "AC-004: bounce starts after pierce exhausted");
        Destroy(piercing.gameObject);

        Grant(UpgradeKind.RapidFire); Grant(UpgradeKind.RubberBullets);
        Check(player.GrantTomeChoice() && player.TryApplyTome(TomeKind.Elasticity), "AC-004: real Elasticity Tome applied");
        var upgraded = Shot(data, player, arena);
        Check(Read<int>(upgraded, "bouncesRemaining") == 4, "AC-004: base1 + Rubber1 + Elasticity2 stack");
        float damage = upgraded.CurrentDamage; Grant(UpgradeKind.Damage);
        var stronger = Shot(data, player, arena);
        Check(stronger.CurrentDamage > damage && upgraded.CurrentDamage == damage, "AC-005: damage upgrades apply to new shots, old shots keep snapshot");
        Destroy(upgraded.gameObject); Destroy(stronger.gameObject);
        Grant(UpgradeKind.Ammo); Grant(UpgradeKind.Reload); Grant(UpgradeKind.DoubleShot);
        var controller = (RedArrowController)player.GetComponentInChildren<WeaponHolder>().CurrentWeapon;
        yield return null; yield return null;
        controller.Tick(10, false);
        Check(controller.State.Capacity == 10 && controller.State.ReloadDuration < 1.5f, "AC-005: ammo/reload upgrades reach live weapon state");
        int before = FindObjectsByType<RedArrowProjectile>().Length; controller.Tick(0, true);
        Check(FindObjectsByType<RedArrowProjectile>().Length == before + 2, "AC-005: Double Shot creates two homing projectiles");
        Grant(UpgradeKind.PowerBullet); Grant(UpgradeKind.Splinter); Grant(UpgradeKind.FrostMage); Grant(UpgradeKind.IceShard);
        var statusShot = Shot(data, player, a.transform.position);
        statusShot.ConfigureUpgradeProjectile(true, true, false); Hit(statusShot, a);
        Check(a.GetComponent<EnemyUpgradeStatus>() != null && a.UpgradeMovementFactor < 1, "AC-005: homing ice fragment triggers existing guaranteed-freeze pipeline");
        var victim = Target(arena + Vector3.down * 3); victim.currentHealth = 0.01f;
        before = FindObjectsByType<RedArrowProjectile>().Length;
        UpgradeHit.Capture(player).Apply(victim, 100000);
        Check(FindObjectsByType<RedArrowProjectile>().Length == before + 3, "AC-005: Splinter kill spawns three homing fragments");
        var fragment = FindObjectsByType<RedArrowProjectile>().Last(p => p != statusShot && Read<UpgradeHit>(p, "upgradeHit").Secondary);
        var secondary = Target(arena + Vector3.down * 4); secondary.currentHealth = 0.01f;
        before = FindObjectsByType<RedArrowProjectile>().Length; Hit(fragment, secondary);
        Check(FindObjectsByType<RedArrowProjectile>().Length <= before, "AC-005: fragment kills cannot split recursively");

        var timed = Shot(data, null, arena + Vector3.up * 100); timed.enabled = true;
        yield return null; Time.timeScale = 0; float timerStart = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - timerStart < 0.1f) yield return null;
        Check(timed != null, "AC-003: projectile survives paused scaled lifetime");
        Time.timeScale = 1; yield return new WaitForSeconds(5.1f);
        Check(timed == null, "AC-003: real Start lifetime destroys projectile after five seconds");
        var late = Shot(data, null, a.transform.position); float hp = a.currentHealth; player.Kill(); Hit(late, a);
        Check(a.currentHealth == hp && GameOverController.GameplayStopped, "AC-003: no projectile damage after defeat");
        Check(Hash(Source) == source && File.ReadAllText(LoadoutLauncher.SelectionScene) == catalogFile, "AC-006: regression preserves original PNG and authored catalog");
        Check(PlayerPrefs.GetInt("SystemLeveling.Soul.Total.v1", 0) == realSouls, "AC-006: persistent real Soul untouched");
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene); yield return null;
        Check(LoadoutLauncher.Character == null && LoadoutLauncher.Weapon == null, "AC-006: regression clears its selected loadout for later baseline tests");
    }

    private EnemyStats Target(Vector3 position)
    {
        var go = new GameObject("Red Arrow Target"); go.SetActive(false); go.transform.position = position; fixtures.Add(go);
        var enemy = go.AddComponent<EnemyStats>();
        enemy.enemyData = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Skeleton.prefab").GetComponent<EnemyStats>().enemyData;
        go.AddComponent<CircleCollider2D>().radius = 0.2f; go.SetActive(true); enemy.currentHealth = 10000; return enemy;
    }
    private RedArrowProjectile Shot(WeaponScriptableObject data, PlayerStats owner, Vector3 position, int pierce = 0)
    {
        var shot = Instantiate(data.Prefab, position, Quaternion.identity).GetComponent<RedArrowProjectile>(); fixtures.Add(shot.gameObject);
        shot.InitializeShot(data, owner, Vector3.right, 1, 1, pierce); shot.enabled = false; return shot;
    }
    private void Grant(UpgradeKind kind)
    {
        player.IncreaseExperience(player.experienceCap - player.experience);
        Check(player.TryApplyUpgrade(kind) && Time.timeScale == 1, "AC-005: apply real upgrade " + kind);
    }
    private static void Hit(ProjectileWeaponBehaviour shot, EnemyStats enemy) => typeof(ProjectileWeaponBehaviour).GetMethod("OnTriggerEnter2D", Flags).Invoke(shot, new object[] { enemy.GetComponent<Collider2D>() });
    private void Check(bool pass, string label) { if (!pass) throw new InvalidOperationException(label); checks.Add(label); }
    private static T Read<T>(object target, string field)
    {
        for (Type type = target.GetType(); type != null; type = type.BaseType)
        { var info = type.GetField(field, Flags | BindingFlags.DeclaredOnly); if (info != null) return (T)info.GetValue(target); }
        throw new MissingFieldException(field);
    }
    private static string Hash(string path) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))); }
    private static void Capture(string name) { var image = ScreenCapture.CaptureScreenshotAsTexture(); try { File.WriteAllBytes(Output + "/" + name, image.EncodeToPNG()); } finally { Destroy(image); } }
    [Serializable] private class Report { public string result; public string[] checks; public string failure; }
}
#endif
