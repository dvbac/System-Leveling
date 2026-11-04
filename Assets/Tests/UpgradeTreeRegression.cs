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

public class UpgradeTreeRegression : MonoBehaviour
{
    private readonly List<string> checks = new List<string>();
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private UnityEngine.Random.State random;
    private bool background;
    private string assetsBefore;
    private const string Folder = "Captures/UpgradeTrees";

    [MenuItem("Tools/System Leveling/Run Upgrade Tree Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<UpgradeTreeRegression>() != null) return;
        var root = new GameObject("Upgrade tree regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root);
        root.AddComponent<UpgradeTreeRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        random = UnityEngine.Random.state; background = Application.runInBackground;
        Application.runInBackground = true; UnityEngine.Random.InitState(91731); assetsBefore = Snapshot();
        string failure = "";
        var stack = new Stack<IEnumerator>(); stack.Push(Tests());
        while (stack.Count > 0)
        {
            bool more = false; object wait = null;
            try { more = stack.Peek().MoveNext(); if (more) wait = stack.Peek().Current; }
            catch (Exception error) { failure = error.ToString(); }
            if (failure.Length > 0) break;
            if (!more) { stack.Pop(); continue; }
            if (wait is IEnumerator nested) { stack.Push(nested); continue; }
            yield return wait;
        }
        if (assetsBefore != Snapshot()) failure += " Source stat assets changed.";
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/runtime.json", JsonUtility.ToJson(new Report { result = failure.Length == 0 ? "PASS" : "FAIL", checks = checks.ToArray(), failure = failure, utc = DateTime.UtcNow.ToString("o") }, true));
        if (failure.Length == 0) Debug.Log("UPGRADE TREE REGRESSION PASS: " + checks.Count);
        else Debug.LogError("UPGRADE TREE REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Fresh(string weapon = "MagicBook")
    {
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.GameplayScene); yield return null;
        var spawner = FindAnyObjectByType<EnemySpawner>(); if (spawner != null) spawner.enabled = false;
        var p = PlayerController.Instance.GetComponent<PlayerStats>();
        p.GetComponent<PlayerController>().enabled = false;
        Set(p, "isInvincible", true); Set(p, "invincibilityTimer", 10000f);
        p.GetComponentInChildren<WeaponHolder>().Equip(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/" + weapon + ".prefab"));
        yield return null;
        foreach (var controller in p.GetComponentsInChildren<WeaponController>()) controller.enabled = false;
        foreach (var enemy in FindObjectsByType<EnemyStats>()) Destroy(enemy.gameObject);
        yield return null;
    }

    private PlayerStats Player => PlayerController.Instance.GetComponent<PlayerStats>();
    private void Grant(UpgradeKind kind)
    {
        var p = Player; p.IncreaseExperience(p.experienceCap - p.experience);
        Check(p.TryApplyUpgrade(kind), "Apply " + kind);
        Check(p.HasUpgrade(kind) && p.PendingUpgrades == 0 && Time.timeScale == 1f, "Record, spend one choice and resume: " + kind);
    }

    private IEnumerator Tests()
    {
        Check(UpgradeCatalog.Trees.Length == 8 && UpgradeCatalog.Trees.SelectMany(t => new[] { t.Root, t.A, t.B, t.Final }).Distinct().Count() == 32, "Eight distinct four-card trees, no summons");
        foreach (var tree in UpgradeCatalog.Trees)
        {
            yield return Fresh(); var treePlayer = Player;
            Check(!treePlayer.CanReceiveUpgrade(tree.A) && !treePlayer.CanReceiveUpgrade(tree.B) && !treePlayer.CanReceiveUpgrade(tree.Final), tree.Name + ": children locked before root");
            treePlayer.IncreaseExperience(treePlayer.experienceCap); int pending = treePlayer.PendingUpgrades;
            Check(!treePlayer.TryApplyUpgrade(tree.Final) && treePlayer.PendingUpgrades == pending, tree.Name + ": locked card cannot spend a choice");
            Check(treePlayer.TryApplyUpgrade(tree.Root), tree.Name + ": root accepts earned choice");
            Check(treePlayer.CanReceiveUpgrade(tree.A) && treePlayer.CanReceiveUpgrade(tree.B) && !treePlayer.CanReceiveUpgrade(tree.Final), tree.Name + ": root unlocks both branches only");
            Grant(tree.A);
            Check(treePlayer.CanReceiveUpgrade(tree.Final), tree.Name + ": branch A alone unlocks final");
            Grant(tree.Final); Grant(tree.B);
            Check(!treePlayer.CanReceiveUpgrade(tree.A) && !treePlayer.CanReceiveUpgrade(tree.B) && !treePlayer.CanReceiveUpgrade(tree.Final), tree.Name + ": child cards cannot repeat");
            foreach (var kind in new[] { tree.Root, tree.A, tree.B, tree.Final })
                Check(!string.IsNullOrEmpty(LevelUpSelection.Title(kind)) && !string.IsNullOrEmpty(LevelUpSelection.Description(kind)), kind + ": card has title/effect text");
            yield return Fresh(); Grant(tree.Root); Grant(tree.B);
            Check(Player.CanReceiveUpgrade(tree.Final), tree.Name + ": branch B alone unlocks final");
        }

        yield return Fresh("Sword");
        Check(!Player.CanReceiveUpgrade(UpgradeKind.DoubleShot) && !Player.CanReceiveUpgrade(UpgradeKind.QuickHands), "Sword never gets multishot/reload roots");
        Grant(UpgradeKind.RapidFire);
        Check(!Player.CanReceiveUpgrade(UpgradeKind.LightBullets) && !Player.CanReceiveUpgrade(UpgradeKind.RubberBullets), "Sword filters ranged rapid-fire branches");
        Grant(UpgradeKind.PyroMage); Grant(UpgradeKind.FireStarter);
        var sword = Player.GetComponentInChildren<WeaponHolder>().CurrentWeapon.weaponData;
        for (int i = 0; i < 5; i++) Player.OnUpgradeAttack(sword, Player.transform.position, Vector3.right, false);
        var fireball = FindObjectsByType<ProjectileWeaponBehaviour>().Single(); fireball.enabled = false;
        Check(Near(fireball.CurrentSpeed, 7) && Near(fireball.CurrentDamage, 20), "Sword fifth attack creates a calibrated real fireball, not a slash prefab");
        var target = Target(Player.transform.position + Vector3.right * 2); var nearby = Target(target.transform.position + Vector3.up * .6f);
        fireball.transform.position = target.transform.position;
        float hp = nearby.currentHealth; Call(fireball, "OnTriggerEnter2D", target.GetComponent<Collider2D>());
        Check(nearby.currentHealth < hp && nearby.GetComponent<EnemyUpgradeStatus>().IsBurning, "Fireball collision burns and damages nearby enemies");

        yield return Fresh(); Grant(UpgradeKind.FrostMage); Grant(UpgradeKind.Frostbite); Grant(UpgradeKind.Shatter);
        Grant(UpgradeKind.PyroMage); Grant(UpgradeKind.IntenseBurn);
        Check(Player.CanReceiveTome(TomeKind.Elements), "Elements Tome now supports actual flame/frost builds");
        target = Target(Player.transform.position + Vector3.right * 2); nearby = Target(target.transform.position + Vector3.up * .5f);
        var hit = UpgradeHit.Capture(Player); hp = target.currentHealth; hit.Apply(target, 0, true, true);
        var status = target.GetComponent<EnemyUpgradeStatus>();
        Check(status.IsFrozen && status.IsBurning && target.UpgradeMovementFactor == 0, "Guaranteed frost/burn applies real status and stops ordinary enemies");
        Check(Near(hp - target.currentHealth, target.MaxHealth * .15f), "Frostbite removes 15% runtime max HP");
        hp = target.currentHealth; hit.Apply(target, 0, true);
        Check(Near(hp, target.currentHealth), "Repeated frozen hits cannot repeatedly strip max HP");
        float pausedHealth = target.currentHealth; Time.timeScale = 0;
        yield return null; yield return null;
        Check(Near(target.currentHealth, pausedHealth) && status.IsFrozen, "Pause stops burn ticks and freeze countdown"); Time.timeScale = 1;
        yield return new WaitForSeconds(.55f);
        Check(target.currentHealth < pausedHealth, "Burn inflicts real damage over time");
        hp = nearby.currentHealth; target.TakeDamage(100000);
        Check(Near(hp - nearby.currentHealth, target.MaxHealth * .07f), "Frozen death deals 7% runtime max HP to nearby enemies");
        Check(nearby.GetComponent<EnemyUpgradeStatus>() == null, "Shatter damage does not recursively apply statuses");

        var burnTarget = Target(Player.transform.position + Vector3.up * 3);
        var burnOnly = hit; burnOnly.Freeze = false;
        hp = burnTarget.currentHealth; burnOnly.Apply(burnTarget, 0, false, true);
        yield return new WaitForSeconds(3.15f);
        Check(Near(hp - burnTarget.currentHealth, hit.BurnDamage * 3f), "A full burn deals six half-second ticks including its expiry boundary");

        foreach (string name in new[] { "Bow", "Crossbow", "MagicBook" })
        {
            var data = AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/" + (name == "MagicBook" ? "Magic Book" : name) + ".asset");
            var state = new RangedWeaponState(data); int initialAmmo = state.Ammo;
            state.SetUpgrades(3, 0, 1.15f, 1.4f);
            Check(state.Ammo == initialAmmo, name + ": capacity upgrades do not grant free ammo");
            state.Tick(100f, false); state.Tick(0, false);
            int ammoBefore = state.Ammo;
            int recoveredBefore = state.AmmoRecovered;
            state.Tick(0, false); state.Tick(0, true);
            if (data.FireMode == WeaponFireMode.Crossbow) { state.Tick(state.ChargeDuration, true); state.Tick(0, false); }
            Check(state.Ammo == ammoBefore - 1, name + ": tree ammo/rate bonuses retain one-round-per-attack scheduling without free capacity ammo");
            state.Tick(100f, false); state.Tick(0, false);
            Check(state.AmmoRecovered > recoveredBefore, name + ": scheduler exposes real recovery after tree modifiers");
        }

        target = Target(Player.transform.position + Vector3.left * 2);
        target.ScaleHealth(3); hp = target.currentHealth; hit.Apply(target, 0, true);
        Check(Near(hp - target.currentHealth, target.MaxHealth * .15f), "Frostbite respects elite runtime health scaling");
        var bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Boss/Frostfang.prefab");
        var boss = Instantiate(bossPrefab, Player.transform.position + Vector3.up * 5, Quaternion.identity).GetComponent<EnemyStats>();
        boss.GetComponent<FrostfangBoss>().enabled = false; hp = boss.currentHealth; hit.Apply(boss, 0, true);
        Check(Near(boss.UpgradeMovementFactor, .7f) && Near(hp - boss.currentHealth, boss.MaxHealth * .01f), "Boss freeze is only a 30% chase slow and 1% max-HP bite");
        yield return new WaitForSeconds(.35f);
        Check(Near(boss.UpgradeMovementFactor, 1), "Boss freeze ends after 0.3 seconds without changing base speed");
        Destroy(boss.gameObject);

        yield return Fresh(); var p = Player; var ranged = (RangedWeaponController)p.GetComponentInChildren<WeaponHolder>().CurrentWeapon;
        Grant(UpgradeKind.DoubleShot); Grant(UpgradeKind.FanFire); Grant(UpgradeKind.Fusillade); Grant(UpgradeKind.SplitFire);
        Grant(UpgradeKind.FrostMage); Grant(UpgradeKind.IceShard);
        var before = FindObjectsByType<ProjectileWeaponBehaviour>().Length;
        p.OnUpgradeAttack(ranged.weaponData, p.transform.position, Vector3.right, true);
        Check(FindObjectsByType<ProjectileWeaponBehaviour>().Length - before == 14, "Last-round effects create 10 fan bullets + rear bullet + 3 ice shards");
        foreach (var shot in FindObjectsByType<ProjectileWeaponBehaviour>()) Destroy(shot.gameObject);
        yield return null;
        Grant(UpgradeKind.PowerBullet); Grant(UpgradeKind.BigShot); Grant(UpgradeKind.ReaperRounds);
        var projectile = Instantiate(ranged.weaponData.Prefab, p.transform.position, Quaternion.identity).GetComponent<ProjectileWeaponBehaviour>();
        projectile.InitializeShot(ranged.weaponData, p, Vector3.right, 1, 1); projectile.enabled = false;
        int pierce = projectile.CurrentPierce; float damage = projectile.CurrentDamage;
        Grant(UpgradeKind.Damage);
        Check(Near(damage, projectile.CurrentDamage), "New tree attacks retain existing damage snapshot behavior");
        target = Target(p.transform.position + Vector3.right * 3); target.currentHealth = .01f;
        Call(projectile, "OnTriggerEnter2D", target.GetComponent<Collider2D>());
        Check(target.IsDead && projectile.CurrentPierce == pierce, "Reaper killed targets consume no pierce budget");
        Grant(UpgradeKind.Splinter);
        target = Target(p.transform.position + Vector3.left * 3); before = FindObjectsByType<ProjectileWeaponBehaviour>().Length;
        UpgradeHit.Capture(p).Apply(target, 100000);
        Check(FindObjectsByType<ProjectileWeaponBehaviour>().Length - before == 3, "A credited kill spawns exactly three Splinter projectiles");
        var fragment = FindObjectsByType<ProjectileWeaponBehaviour>().First(s => s != projectile); fragment.enabled = false;
        target = Target(p.transform.position + Vector3.up * 3); target.currentHealth = .01f; before = FindObjectsByType<ProjectileWeaponBehaviour>().Length;
        Call(fragment, "OnTriggerEnter2D", target.GetComponent<Collider2D>());
        Check(FindObjectsByType<ProjectileWeaponBehaviour>().Length <= before, "Fragment kills cannot split recursively");

        Grant(UpgradeKind.QuickHands); Grant(UpgradeKind.FreshClip); Grant(UpgradeKind.KillClip);
        target = Target(p.transform.position + Vector3.right * 4); UpgradeHit.Capture(p).Apply(target, 100000);
        Check(p.EffectiveReloadRate > p.ReloadSpeedMultiplier, "Kill Clip adds a real reload-rate bonus");
        Check(p.GrantTomeChoice() && p.TryApplyTome(TomeKind.Electro), "Grant real Electro Tome for kill-trigger integration");
        p.OnAmmoRecovered();
        target = Target(p.transform.position + Vector3.up * 4); target.currentHealth = .01f;
        p.CallLastAmmoLightning(p.transform.position);
        Check(target.IsDead && p.EffectiveReloadRate > p.ReloadSpeedMultiplier, "Tome lightning kills also grant Kill Clip credit");
        p.OnAmmoRecovered();
        Check(Near(p.EffectiveReloadRate, p.ReloadSpeedMultiplier) && Near(p.CombatDamageMultiplier, 1.5f), "Ammo recovery clears Kill Clip and activates Fresh Clip");
        var scheduler = new RangedWeaponState(ranged.weaponData); scheduler.Tick(0, true);
        scheduler.RefundRound(); Check(scheduler.Ammo == scheduler.Capacity && !scheduler.IsReloading, "Siege refund is capacity-bounded and clears empty reload");
        scheduler.Tick(1, true); scheduler.Tick(10, false);
        Check(scheduler.AmmoRecovered > 0, "Real scheduler reports ammo recovery separately from refunds");

        yield return Fresh(); Grant(UpgradeKind.Vitality); Grant(UpgradeKind.AngerPoint); Grant(UpgradeKind.Giant); Grant(UpgradeKind.Regeneration);
        p = Player; Check(Near(p.transform.localScale.x, 1.15f), "Giant visibly scales the real player");
        Set(p, "isInvincible", false); p.TakeDamage(1);
        Check(Near(p.CombatDamageMultiplier, 1.5f) && Near(p.EffectiveFireRate, 1.5f), "Taking real damage activates Anger Point");
        hp = p.currentHealth; Set(p, "regenerationTime", 30f); Call(p, "TickUpgradeTrees");
        Check(p.currentHealth > hp, "Regeneration heals through the production update path");
        Grant(UpgradeKind.MoveSpeed); Grant(UpgradeKind.BlazingSpeed); Grant(UpgradeKind.RunAndGun); Grant(UpgradeKind.InTheWind);
        Check(Near(p.AttackMovementFactor, .9f), "Run and Gun raises only the attack movement factor");
        Set(p, "unhurtTime", 50f); Check(Near(p.WindBonus, .4f), "Unhurt scaling caps at 40%");
        Set(p, "isInvincible", false); p.TakeDamage(1); Check(p.WindBonus == 0, "Actual damage resets wind scaling");
        target = Target(p.transform.position + Vector3.right * .7f);
        p.transform.position += Vector3.right * .1f; Set(p, "blazeTime", 0f); Call(p, "TickUpgradeTrees");
        Check(target.GetComponent<EnemyUpgradeStatus>() != null && target.GetComponent<EnemyUpgradeStatus>().IsBurning, "Moving with Blazing Speed actually ignites nearby enemies");

        yield return Fresh(); Grant(UpgradeKind.PyroMage);
        var selection = FindAnyObjectByType<LevelUpSelection>(); p = Player;
        p.IncreaseExperience(p.experienceCap); int choice = -1;
        for (int attempt = 0; attempt < 500 && choice < 0; attempt++)
        {
            Call(selection, "PresentChoices");
            for (int i = 0; i < 3; i++) if (selection.ChoiceAt(i) == UpgradeKind.FireStarter) choice = i;
            Check(Enumerable.Range(0, 3).All(i => p.CanReceiveUpgrade(selection.ChoiceAt(i))) && Enumerable.Range(0, 3).Select(selection.ChoiceAt).Distinct().Count() == 3, "Real UI only offers three distinct eligible cards");
        }
        Check(choice >= 0 && selection.IsOpen, "Unlocked branch appears in actual level-up UI");
        yield return null; yield return new WaitForEndOfFrame(); Capture("flame-card.png");
        selection.Choose(choice); Check(p.HasUpgrade(UpgradeKind.FireStarter) && !selection.IsOpen, "Actual card click applies the branch and resumes gameplay");
        Grant(UpgradeKind.FrostMage);
        target = Target(p.transform.position + Vector3.right * 1.5f); nearby = Target(p.transform.position + Vector3.left * 1.5f);
        UpgradeHit.Capture(p).Apply(target, 0, true); UpgradeHit.Capture(p).Apply(nearby, 0, false, true);
        p.OnUpgradeAttack(((RangedWeaponController)p.GetComponentInChildren<WeaponHolder>().CurrentWeapon).weaponData, p.transform.position, Vector3.up, false);
        yield return new WaitForSeconds(.1f); yield return new WaitForEndOfFrame(); Capture("frost-flame-fx.png");
        Check(FindObjectsByType<UpgradeEffect>().Length > 0 && FindObjectsByType<UpgradeEffect>().Length <= 256, "Live pixel effects are rendered and globally bounded");
        p.Kill(); Check(!p.TryApplyUpgrade(UpgradeKind.FireStarter), "Death rejects further tree upgrades");
        yield return Fresh();
        Check(Enum.GetValues(typeof(UpgradeKind)).Cast<UpgradeKind>().All(k => !Player.HasUpgrade(k)) && Near(Player.CombatDamageMultiplier, 1) && Player.TreeAmmoBonus == 0, "New run clears all tree ownership and transient bonuses");
    }

    private EnemyStats Target(Vector3 position)
    {
        var enemy = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Skeleton.prefab"), position, Quaternion.identity).GetComponent<EnemyStats>();
        enemy.currentHealth = 10000; enemy.currentMoveSpeed = 0;
        if (enemy.TryGetComponent(out Enemy movement)) movement.enabled = false;
        enemy.GetComponent<Collider2D>().isTrigger = true;
        return enemy;
    }
    private static void Capture(string name)
    {
        Directory.CreateDirectory(Folder); var image = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Folder + "/" + name, image.EncodeToPNG()); } finally { Destroy(image); }
    }
    private static string Snapshot() => string.Join("", Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories).OrderBy(p => p).Select(File.ReadAllText));
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);
    private static void Call(object target, string name, params object[] args) => target.GetType().GetMethod(name, Private).Invoke(target, args);
    private static bool Near(float a, float b) => Mathf.Abs(a - b) < .01f;
    private void Check(bool pass, string message) { if (!pass) throw new Exception(message); checks.Add(message); }
    private void OnDestroy() { UnityEngine.Random.state = random; Application.runInBackground = background; Time.timeScale = 1f; }
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
