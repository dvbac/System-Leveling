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

public class RunCombatUpgradeRegression : MonoBehaviour
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private readonly List<Object> temporary = new List<Object>();
    private bool background;
    private UnityEngine.Random.State random;

    [MenuItem("Tools/System Leveling/Run Combat Upgrade Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<RunCombatUpgradeRegression>() != null) return;
        var root = new GameObject("Combat upgrade regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root);
        root.AddComponent<RunCombatUpgradeRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground; random = UnityEngine.Random.state;
        Application.runInBackground = true; Time.timeScale = 1f;
        string before = Snapshot(); string failure = "";
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
        if (before != Snapshot()) failure += " Source stat assets were changed.";
        Directory.CreateDirectory("Captures/RunCombatUpgrades");
        File.WriteAllText("Captures/RunCombatUpgrades/runtime.json", JsonUtility.ToJson(new Report
        { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("COMBAT UPGRADE REGRESSION PASS: " + checks.Count);
        else Debug.LogError("COMBAT UPGRADE REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        UnityEngine.Random.InitState(35091);
        int criticals = 0;
        for (int i = 0; i < 10000; i++) if (WeaponScriptableObject.RollDamage(10f, 0.1f) == 20f) criticals++;
        Check(criticals > 850 && criticals < 1150, "Actual critical roll yields approximately 10% double-damage hits");
        Check(WeaponScriptableObject.RollDamage(10, 0) == 10 && WeaponScriptableObject.RollDamage(10, 1) == 20, "Zero crit never doubles and 100% crit always doubles");
        foreach (string name in new[] { "MagicBook", "Bow", "Crossbow" })
        {
            yield return SceneManager.LoadSceneAsync(name == "Bow" ? LoadoutLauncher.SnowScene : LoadoutLauncher.GameplayScene); yield return null;
            FindAnyObjectByType<EnemySpawner>().enabled = false;
            PlayerStats player = PlayerController.Instance.GetComponent<PlayerStats>();
            PlayerController movement = player.GetComponent<PlayerController>(); movement.enabled = false;
            Set(player, "isInvincible", true); Set(player, "invincibilityTimer", 10000f);
            WeaponHolder holder = player.GetComponentInChildren<WeaponHolder>();
            Check(holder.Equip(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Weapons/{name}.prefab")), "Equip real weapon: " + name);
            yield return null;
            var weapon = (RangedWeaponController)holder.CurrentWeapon; weapon.enabled = false;
            LevelUpSelection selection = FindAnyObjectByType<LevelUpSelection>();
            Check(player.ExtraProjectiles == 0 && player.CriticalChanceBonus == 0 && Near(player.ProjectileSpeedMultiplier, 1), "Fresh run has no combat upgrade modifiers");
            Check(!player.TryApplyUpgrade(UpgradeKind.DoubleShot), "No combat upgrade without an earned level");
            yield return Choose(player, selection, UpgradeKind.DoubleShot, name == "MagicBook");
            Check(player.ExtraProjectiles == 1 && player.SpreadModifier == 10 && Near(player.ProjectileDamageMultiplier, 0.9f), "Double Shot grants +1 path/+10 spread/-10% projectile damage");
            holder.PointerPosition = (Vector2)holder.transform.position + Vector2.right * 10;
            weapon.enabled = true;
            ProjectileWeaponBehaviour[] first = Fire(weapon);
            foreach (var projectile in first) projectile.enabled = false;
            Check(first.Length == 2 && Near(Vector2.Angle(first[0].transform.right, first[1].transform.right), 10f), name + ": real shot creates a symmetric ten-degree two-projectile fan");
            Check(first.All(shot => Near(shot.CurrentDamage, weapon.weaponData.Damage * 0.9f * weapon.State.ShotDamageFactor)), "Double Shot damage penalty applies to each path, including crossbow charge");
            Check(weapon.IsAttacking && Near(movement.MovementMultiplier, 0.2f), "Firing reduces actual movement multiplier to 20%");
            Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>(); Vector2 beforeMove = playerBody.position;
            Set(movement, "moveInput", Vector2.right); Call(movement, "FixedUpdate");
            yield return new WaitForFixedUpdate();
            Check(Mathf.Abs(playerBody.position.x - beforeMove.x - player.currentMoveSpeed * 0.2f * Time.fixedDeltaTime) < 0.002f, "Real Rigidbody2D movement covers only 20% of the normal attack-frame distance");
            Set(movement, "moveInput", Vector2.zero);
            weapon.Tick(0.01f, false);
            Check(!weapon.IsAttacking && Near(movement.MovementMultiplier, 1f), "Trigger release/reload restores full movement speed");
            weapon.enabled = false;
            float oldDamage = first[0].CurrentDamage, oldSpeed = first[0].CurrentSpeed;
            yield return Choose(player, selection, UpgradeKind.Focus, name == "MagicBook");
            yield return Choose(player, selection, UpgradeKind.PowerBullet, name == "MagicBook");
            yield return Choose(player, selection, UpgradeKind.QuickHands, name == "MagicBook");
            yield return Choose(player, selection, UpgradeKind.Critical, name == "MagicBook");
            Check(Near(player.ProjectileSpeedMultiplier, 1.2f) && player.SpreadModifier == -5, "Focus increases speed by 20% and subtracts fifteen spread degrees");
            Check(Near(player.DamageMultiplier, 1.15f) && Near(player.KnockbackMultiplier, 1.2f), "Power Bullet increases damage by 15% and knockback by 20%");
            Check(Near(player.ReloadSpeedMultiplier, 1.05f) && Near(player.FireRateMultiplier, 1.05f) && Near(player.CriticalChanceBonus, 0.1f), "Quick Hands and Critical apply the requested rate/chance modifiers");
            Check(first[0] != null && Near(first[0].CurrentDamage, oldDamage) && Near(first[0].CurrentSpeed, oldSpeed), "Already-fired projectiles keep their damage/speed snapshot after upgrades");
            weapon.enabled = true;
            ProjectileWeaponBehaviour[] upgraded = Fire(weapon);
            Check(upgraded.Length == 2 && Vector2.Dot(upgraded[0].transform.right, upgraded[1].transform.right) > 0.9999f, "Focus clamps a negative effective spread to zero without losing a path");
            Check(upgraded.All(shot => Near(shot.CurrentSpeed, weapon.weaponData.Speed * 1.2f) && Near(shot.CurrentKnockback, weapon.weaponData.Knockback * 1.2f) && Near(shot.CriticalChance, 0.1f)), "Real projectiles snapshot speed, knockback and critical chance");
            Check(upgraded.All(shot => Near(shot.CurrentDamage, weapon.weaponData.Damage * 1.15f * 0.9f * weapon.State.ShotDamageFactor)), "Power Bullet and Double Shot combine on actual shot damage");
            Check(Near(weapon.State.ReloadDuration, weapon.weaponData.ReloadTime / 1.05f), "Quick Hands divides reload duration by 1.05, not subtracts 5% time");
            var scheduler = new RangedWeaponState(weapon.weaponData); scheduler.SetUpgrades(0, 1, 1.05f, 1.05f);
            Check(Near(scheduler.ReloadDuration, weapon.weaponData.ReloadTime / (1.05f + weapon.weaponData.ReloadSpeedPerLevel)), "Quick Hands combines additively with the existing Reload upgrade");
            if (name != "Crossbow")
            {
                scheduler = new RangedWeaponState(weapon.weaponData); scheduler.SetUpgrades(0, 0, 1.05f, 1.05f);
                Check(scheduler.Tick(0, true) && !scheduler.Tick(weapon.weaponData.Cooldown / 1.05f - 0.001f, true) && scheduler.Tick(0.002f, true), "Quick Hands enforces the actual 5% faster firing interval");
            }
            else
            {
                Check(Near(weapon.State.ChargeDuration, weapon.weaponData.ChargeTime / 1.05f), "Crossbow keeps its existing reload-speed-to-charge behavior");
                var fastReload = Instantiate(weapon.weaponData); temporary.Add(fastReload);
                var edit = new SerializedObject(fastReload); edit.FindProperty("reloadTime").floatValue = 0.05f; edit.FindProperty("cooldown").floatValue = 1f; edit.ApplyModifiedPropertiesWithoutUndo();
                foreach (float rate in new[] { 1f, 1.05f })
                {
                    var crossRate = new RangedWeaponState(fastReload); crossRate.SetUpgrades(0, 0, 1f, rate);
                    crossRate.Tick(0, true); crossRate.Tick(0, false); crossRate.Tick(0.05f, false); crossRate.Tick(0, false);
                    crossRate.Tick(0, true); crossRate.Tick(0.91f, true);
                    Check(crossRate.Tick(0, false) == (rate > 1f), "Crossbow release also honors the fire-rate limit when reload is faster than its cooldown");
                }
            }
            weapon.Tick(0.01f, false); weapon.Tick(100f, false);
            holder.PointerPosition = (Vector2)holder.transform.position + Vector2.right * 10;
            weapon.Tick(0f, true);
            movement.ApplySlow(0.6f, 0.4f);
            Check(Near(movement.MovementMultiplier, 0.2f), "Attack slowdown takes priority over the weaker Rotwood root slow");
            weapon.enabled = false;
            Check(Near(movement.MovementMultiplier, 0.6f), "Disabling a weapon removes only attack slowdown, preserving the root slow");
            yield return new WaitForSeconds(0.45f);
            Check(Near(movement.MovementMultiplier, 1f), "Ending all slows restores original movement without changing the player's base speed");
            weapon.enabled = true; weapon.Tick(0f, false); weapon.Tick(100f, false); weapon.Tick(0f, true, true);
            Check(!weapon.IsAttacking && Near(movement.MovementMultiplier, 1f), "Hovering/clicking UI does not apply attack slowdown");
            weapon.enabled = false;

            yield return HitTests(player, weapon.weaponData);
            Check(holder.Equip(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/Sword.prefab")), "Equip sword after ranged upgrades");
            yield return null;
            var sword = (MeleeWeaponController)holder.CurrentWeapon;
            Check(!player.CanReceiveUpgrade(UpgradeKind.DoubleShot) && !player.CanReceiveUpgrade(UpgradeKind.Focus) && !player.CanReceiveUpgrade(UpgradeKind.QuickHands), "Sword never offers ranged-only upgrades");
            Call(sword, "Attack");
            Check(sword.IsAttacking && Near(movement.MovementMultiplier, 0.2f), "Actual sword swing also slows movement to 20%");
            sword.enabled = false;
            Check(Near(movement.MovementMultiplier, 1f), "Unequip/disable during a swing cannot leave movement slowed");
            yield return Choose(player, selection, UpgradeKind.PowerBullet, false);
            for (int i = 0; i < 10; i++)
            {
                player.IncreaseExperience(player.experienceCap - player.experience);
                if (player.CanReceiveUpgrade(UpgradeKind.Critical)) Check(player.TryApplyUpgrade(UpgradeKind.Critical), "Critical stacks while below its cap");
            }
            int pending = player.PendingUpgrades;
            Check(Near(player.CriticalChanceBonus, 0.5f) && !player.TryApplyUpgrade(UpgradeKind.Critical) && player.PendingUpgrades == pending, "Critical caps at five choices and a rejected capped choice consumes no level");
            Call(selection, "PresentChoices");
            Check(Enumerable.Range(0, 3).All(i => player.CanReceiveUpgrade(selection.ChoiceAt(i)) && selection.ChoiceAt(i) != UpgradeKind.Critical), "Real card pool excludes capped and incompatible upgrades");
            Check(holder.Equip(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/Weapons/{name}.prefab")), "Restore ranged weapon to test projectile cap");
            yield return null;
            holder.CurrentWeapon.enabled = false;
            while (player.ExtraProjectiles < 2)
            {
                player.IncreaseExperience(player.experienceCap - player.experience);
                Check(player.TryApplyUpgrade(UpgradeKind.DoubleShot), "Double Shot stacks up to two choices");
            }
            pending = player.PendingUpgrades;
            Check(!player.CanReceiveUpgrade(UpgradeKind.DoubleShot) && !player.TryApplyUpgrade(UpgradeKind.DoubleShot) && player.PendingUpgrades == pending, "At two Double Shot choices, the upgrade is no longer offered and spends no pending level");
            player.Kill();
            Check(!player.TryApplyUpgrade(UpgradeKind.PowerBullet), "Death rejects further combat upgrades");
            FindAnyObjectByType<GameOverController>().Restart();
            yield return WaitForFreshRun(player);
            PlayerStats fresh = PlayerController.Instance.GetComponent<PlayerStats>();
            Check(fresh.ExtraProjectiles == 0 && fresh.CriticalChanceBonus == 0 && fresh.SpreadModifier == 0 && Near(fresh.DamageMultiplier, 1) && Near(fresh.KnockbackMultiplier, 1) && Near(fresh.ReloadSpeedMultiplier, 1) && Near(fresh.FireRateMultiplier, 1), "Actual Quick Restart clears every combat upgrade");
        }
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene);
    }

    private IEnumerator Choose(PlayerStats player, LevelUpSelection selection, UpgradeKind kind, bool capture)
    {
        player.IncreaseExperience(player.experienceCap - player.experience);
        int pick = -1;
        for (int attempt = 0; attempt < 100 && pick < 0; attempt++)
        {
            Call(selection, "PresentChoices");
            for (int i = 0; i < 3; i++) if (selection.ChoiceAt(i) == kind) pick = i;
        }
        Check(selection.IsOpen && pick >= 0 && Time.timeScale == 0f, "Actual level-up UI offers " + kind);
        Check(selection.ChoiceAt(0) != selection.ChoiceAt(1) && selection.ChoiceAt(1) != selection.ChoiceAt(2) && selection.ChoiceAt(0) != selection.ChoiceAt(2), "Offered upgrades are distinct");
        Canvas.ForceUpdateCanvases();
        Text[] titles = Get<Text[]>(selection, "titles"), descriptions = Get<Text[]>(selection, "descriptions");
        Check(titles[pick].preferredWidth <= titles[pick].rectTransform.rect.width + 1 && descriptions[pick].preferredHeight <= descriptions[pick].rectTransform.rect.height + 1 && descriptions[pick].preferredWidth <= descriptions[pick].rectTransform.rect.width + 1, "Card text fits: " + kind);
        yield return null;
        if (capture) { yield return new WaitForEndOfFrame(); Capture(kind + ".png"); }
        int pending = player.PendingUpgrades;
        selection.Choose(pick);
        Check(player.PendingUpgrades == pending - 1 && !selection.IsOpen && Time.timeScale == 1f, "Click applies exactly one " + kind + " upgrade and resumes play");
        yield return null; yield return null;
    }

    private ProjectileWeaponBehaviour[] Fire(RangedWeaponController weapon)
    {
        var before = new HashSet<int>(FindObjectsByType<ProjectileWeaponBehaviour>().Select(shot => shot.GetInstanceID()));
        weapon.Tick(100f, false); weapon.Tick(0f, false); // Crossbow reload completion needs a released input frame before a fresh charge.
        int ammo = weapon.State.Ammo, attacks = weapon.ShotsFired;
        weapon.Tick(0f, true);
        if (weapon.weaponData.FireMode == WeaponFireMode.Crossbow)
        {
            Check(weapon.IsAttacking, "Crossbow charging counts as active weapon use");
            weapon.Tick(weapon.State.ChargeDuration, true); weapon.Tick(0f, false);
        }
        Check(weapon.ShotsFired == attacks + 1 && weapon.State.Ammo == ammo - 1, "A multishot attack consumes one round and counts as one attack");
        return FindObjectsByType<ProjectileWeaponBehaviour>().Where(shot => !before.Contains(shot.GetInstanceID())).ToArray();
    }

    private IEnumerator HitTests(PlayerStats player, WeaponScriptableObject data)
    {
        var clone = Instantiate(data); temporary.Add(clone);
        SerializedObject so = new SerializedObject(clone); so.FindProperty("criticalChance").floatValue = 1f; so.ApplyModifiedPropertiesWithoutUndo();
        var enemy = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Skeleton.prefab"), new Vector3(10000, 10000), Quaternion.identity).GetComponent<EnemyStats>();
        enemy.currentHealth = 100000; enemy.currentMoveSpeed = 0;
        yield return null;
        var shot = Instantiate(data.Prefab, new Vector3(9990, 10000), Quaternion.identity).GetComponent<ProjectileWeaponBehaviour>();
        shot.InitializeShot(clone, player, Vector3.right, 1f, 1f); shot.enabled = false;
        float hp = enemy.currentHealth;
        Call(shot, "OnTriggerEnter2D", enemy.GetComponent<Collider2D>());
        Check(Near(enemy.currentHealth, hp - shot.CurrentDamage * 2f), "Actual projectile collision applies x2 critical damage");
        Check(Near(enemy.GetComponent<Rigidbody2D>().linearVelocity.x, data.Knockback * player.KnockbackMultiplier), "Actual hit applies the upgraded knockback velocity");
        float after = enemy.currentHealth;
        Call(shot, "OnTriggerEnter2D", enemy.GetComponent<Collider2D>());
        Check(enemy.currentHealth == after, "Duplicate projectile contact cannot double-hit or roll another critical");
        Vector3 start = enemy.transform.position;
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
        Check(enemy.transform.position.x > start.x, "Knockback moves the enemy through real Rigidbody2D physics");
        yield return new WaitForSeconds(0.2f);
        Check(enemy.GetComponent<Rigidbody2D>().linearVelocity.sqrMagnitude < 0.001f, "Knockback stops rather than drifting indefinitely");
        var swordData = Instantiate(AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Sword.asset")); temporary.Add(swordData);
        so = new SerializedObject(swordData); so.FindProperty("criticalChance").floatValue = 1f; so.ApplyModifiedPropertiesWithoutUndo();
        var slash = Instantiate(swordData.Prefab).GetComponent<MeleeWeaponBehaviour>();
        slash.Initialize(swordData, player.transform, Vector2.right); slash.enabled = false;
        hp = enemy.currentHealth; Call(slash, "TryHit", enemy.GetComponent<Collider2D>());
        Check(Near(enemy.currentHealth, hp - swordData.Damage * player.DamageMultiplier * 2f), "Melee also receives Power Bullet damage and x2 critical hits, without the projectile damage penalty");
        Destroy(enemy.gameObject); if (shot != null) Destroy(shot.gameObject); Destroy(slash.gameObject);
    }

    private IEnumerator WaitForFreshRun(PlayerStats previous)
    {
        float deadline = Time.realtimeSinceStartup + 15f;
        while (previous != null && Time.realtimeSinceStartup < deadline) yield return null;
        yield return null;
        Check(previous == null && PlayerController.Instance != null, "Quick Restart loads a new gameplay player");
    }
    private static void Capture(string name)
    {
        Directory.CreateDirectory("Captures/RunCombatUpgrades"); var image = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes("Captures/RunCombatUpgrades/" + name, image.EncodeToPNG()); } finally { Destroy(image); }
    }
    private static string Snapshot() => string.Join("", Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories).OrderBy(path => path).Select(File.ReadAllText));
    private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.01f;
    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    private static void Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Fields).Invoke(target, args);
    private void Check(bool pass, string text) { if (!pass) throw new Exception(text); checks.Add(text); }
    private void OnDestroy()
    {
        foreach (Object asset in temporary) if (asset != null) Destroy(asset);
        Application.runInBackground = background; UnityEngine.Random.state = random; Time.timeScale = 1f;
    }
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
