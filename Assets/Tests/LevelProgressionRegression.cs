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
using Object = UnityEngine.Object;

public class LevelProgressionRegression : MonoBehaviour
{
    private const string Output = "Captures/LevelProgression";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private readonly List<GameObject> fixtures = new List<GameObject>();
    private bool background;

    [MenuItem("Tools/System Leveling/Run Level Progression Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<LevelProgressionRegression>() != null) return;
        var root = new GameObject("Level Progression Regression", typeof(SoulTestIsolation));
        DontDestroyOnLoad(root);
        root.AddComponent<LevelProgressionRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground;
        Application.runInBackground = true;
        var stack = new Stack<IEnumerator>(); stack.Push(Tests()); string failure = "";
        while (stack.Count > 0)
        {
            object wait = null; bool more;
            try { more = stack.Peek().MoveNext(); if (more) wait = stack.Peek().Current; }
            catch (Exception error) { failure = error.ToString(); break; }
            if (!more) { stack.Pop(); continue; }
            if (wait is IEnumerator nested) { stack.Push(nested); continue; }
            yield return wait;
        }
        foreach (GameObject fixture in fixtures) if (fixture != null) Destroy(fixture);
        Time.timeScale = 1;
        Application.runInBackground = background;
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/regression.json", JsonUtility.ToJson(new Report
        { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("LEVEL PROGRESSION REGRESSION PASS: " + checks.Count);
        else Debug.LogError("LEVEL PROGRESSION REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        string sources = Snapshot();
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.GameplayScene);
        yield return null;
        FindAnyObjectByType<EnemySpawner>().enabled = false;
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        player.GetComponent<PlayerController>().enabled = false;
        player.GetComponentInChildren<WeaponController>().enabled = false;
        player.invincibilityDuration = 999;
        LevelUpSelection ui = FindAnyObjectByType<LevelUpSelection>(); ui.enabled = false;
        Check(player.level == 1 && player.experience == 0 && player.experienceCap == 5, "Gameplay starts at level 1, 0/5 EXP");
        player.IncreaseExperience(1.5); player.IncreaseExperience(1.5); player.IncreaseExperience(1.5);
        player.IncreaseExperience(0); player.IncreaseExperience(-1); player.IncreaseExperience(double.NaN); player.IncreaseExperience(double.PositiveInfinity);
        Check(player.level == 1 && player.experience == 4.5 && player.PendingUpgrades == 0, "Fractional EXP is preserved and invalid gains cannot grant a level");
        player.IncreaseExperience(.5);
        Check(player.level == 2 && player.experience == 0 && player.experienceCap == 15 && player.PendingUpgrades == 1, "Exact threshold grants one level and sets the next cost");
        player.IncreaseExperience(20);
        Check(player.level == 3 && player.experience == 5 && player.experienceCap == 25 && player.PendingUpgrades == 2, "EXP remainder carries forward");
        player.IncreaseExperience(2000);
        Check(player.level == 20 && player.experience == 220 && player.experienceCap == 312 && player.PendingUpgrades == 19, "Large pickup queues all earned levels and reaches the special level-20 threshold");
        int[] levels = { int.MinValue, 0, 1, 10, 19, 20, 21, 39, 40, 59, 60, 61, 46340, 46341, int.MaxValue };
        int[] costs = { 5, 5, 5, 95, 185, 312, 267, 501, 632, 936, 3600, 3721, 2147395600, int.MaxValue, int.MaxValue };
        for (int i = 0; i < levels.Length; i++)
            Check(PlayerStats.ExperienceForLevel(levels[i]) == costs[i], "Exact supplied XP formula at level " + levels[i]);
        foreach (int boundary in new[] { 19, 20, 39, 40, 59, 60 })
        {
            var go = new GameObject("XP boundary " + boundary); go.SetActive(false); fixtures.Add(go);
            var probe = go.AddComponent<PlayerStats>(); probe.characterData = player.characterData; probe.enabled = false; go.SetActive(true);
            probe.level = boundary; probe.experienceCap = PlayerStats.ExperienceForLevel(boundary); probe.experience = probe.experienceCap - .5;
            probe.IncreaseExperience(1.5);
            Check(probe.level == boundary + 1 && probe.experience == 1 && probe.experienceCap == PlayerStats.ExperienceForLevel(boundary + 1) && probe.PendingUpgrades == 1,
                "Fractional rollover crosses XP band boundary " + boundary + " exactly once");
        }
        var maxLevel = new GameObject("Maximum level XP guard"); maxLevel.SetActive(false); fixtures.Add(maxLevel);
        var maxProbe = maxLevel.AddComponent<PlayerStats>(); maxProbe.characterData = player.characterData; maxProbe.enabled = false; maxLevel.SetActive(true);
        maxProbe.level = int.MaxValue; maxProbe.experienceCap = int.MaxValue; maxProbe.experience = int.MaxValue - .5;
        maxProbe.IncreaseExperience(1.5);
        Check(maxProbe.level == int.MaxValue && maxProbe.experience == int.MaxValue && maxProbe.PendingUpgrades == 0,
            "Maximum level and fractional XP saturate without wrapping or granting invalid levels");

        string[] names = { "Slime", "Spider", "Wolf", "Skeleton", "Snow/Snowman", "Snow/IceSlime", "Snow/SnowWolf" };
        for (int i = 0; i < names.Length; i++)
        {
            yield return KillAndCollect(player, "Assets/Prefab/ReferenceEnemies/" + names[i] + ".prefab", 1.5f, false);
            yield return KillAndCollect(player, "Assets/Prefab/ReferenceEnemies/" + names[i] + ".prefab", 4.5f, true);
        }
        Check(AssetDatabase.LoadAssetAtPath<EnemyScriptableObject>("Assets/Scriptable Object/Enemy/Blue Slime.asset").ExperienceReward == 1.5f, "Legacy Blue/Ice Slime reward is also 1.5");
        Check(AssetDatabase.LoadAssetAtPath<EnemyScriptableObject>("Assets/MyAssets/Monster/Boss/Frostfang/Generated/FrostfangStats.asset").ExperienceReward == 150 &&
            AssetDatabase.LoadAssetAtPath<EnemyScriptableObject>("Assets/MyAssets/Monster/RainbowSlime/RainbowSlime.asset").ExperienceReward == 0, "Frostfang and Rainbow Slime XP are unchanged");
        yield return KillAndCollect(player, "Assets/Prefab/Boss/Rotwood.prefab", 150, false);

        var expPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/EXP3.prefab");
        var prop = new GameObject("Non-enemy drop fixture"); fixtures.Add(prop);
        DropRateManager drops = prop.AddComponent<DropRateManager>();
        drops.drops = new List<DropRateManager.Drops> { new DropRateManager.Drops { name = "EXP", itemPrefap = expPrefab, dropRate = 100 } };
        var before = new HashSet<int>(FindObjectsByType<Experience>().Select(e => e.GetInstanceID()));
        drops.DropItem();
        Experience fallback = FindObjectsByType<Experience>().Single(e => !before.Contains(e.GetInstanceID())); fixtures.Add(fallback.gameObject);
        Check(fallback.experienceGranted == 15 && expPrefab.GetComponent<Experience>().experienceGranted == 15, "Non-enemy drops and shared EXP prefab retain the fallback value");

        WeaponHolder holder = player.GetComponentInChildren<WeaponHolder>();
        Check(holder.Equip(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/MagicBook.prefab")), "Equip a ranged weapon for cap checks");
        yield return null;
        holder.CurrentWeapon.enabled = false;
        UpgradeKind[] limited = { UpgradeKind.DoubleShot, UpgradeKind.Ammo, UpgradeKind.Reload, UpgradeKind.QuickHands, UpgradeKind.Critical };
        int[] limits = { 2, 4, 5, 5, 5 };
        for (int i = 0; i < limited.Length; i++)
        {
            for (int n = 0; n < limits[i]; n++)
            {
                Earn(player);
                Check(player.TryApplyUpgrade(limited[i]), limited[i] + " accepts earned choice " + (n + 1));
            }
            Earn(player); int pending = player.PendingUpgrades;
            Check(!player.CanReceiveUpgrade(limited[i]) && !player.TryApplyUpgrade(limited[i]) && player.PendingUpgrades == pending,
                limited[i] + " rejects choices beyond its cap without spending a reward");
        }
        Check(player.ExtraProjectiles == 2 && player.AmmoUpgradeLevel == 4 && player.ReloadUpgradeLevel == 5 &&
            player.QuickHandsUpgradeLevel == 5 && player.CriticalUpgradeLevel == 5 && Mathf.Approximately(player.CriticalChanceBonus, 0.5f), "All per-run counters and the 50% crit bonus match the limits");
        while (player.PendingUpgrades > 0) Check(player.TryApplyUpgrade(UpgradeKind.Damage), "Unlimited Power remains available to consume queued choices");
        Earn(player); ui.enabled = true;
        Check(ui.IsOpen && Time.timeScale == 0, "Earned reward still opens and pauses the actual three-card UI");
        for (int n = 0; n < 20; n++)
        {
            typeof(LevelUpSelection).GetMethod("PresentChoices", Flags).Invoke(ui, null);
            var choices = Enumerable.Range(0, 3).Select(ui.ChoiceAt).ToArray();
            Check(choices.Distinct().Count() == 3 && choices.All(kind => player.CanReceiveUpgrade(kind) && !limited.Contains(kind)), "Actual card pool omits capped upgrades and keeps three distinct choices");
        }
        yield return null;
        ui.Choose(0);
        Check(player.PendingUpgrades == 0 && !ui.IsOpen && Time.timeScale == 1, "Selecting one card consumes one reward and resumes play");
        yield return null; yield return null;
        Check(holder.Equip(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/Sword.prefab")), "Swap to sword");
        yield return null;
        holder.CurrentWeapon.enabled = false;
        Check(!player.CanReceiveUpgrade(UpgradeKind.Focus) && limited.All(kind => !player.CanReceiveUpgrade(kind)), "Sword filters ranged cards without resetting capped counters");
        Check(holder.Equip(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Weapons/Crossbow.prefab")), "Swap back to a ranged weapon");
        yield return null;
        Check(limited.All(kind => !player.CanReceiveUpgrade(kind)), "Weapon swapping cannot bypass the per-run caps");

        player.Kill(); double total = TotalExperience(player);
        player.IncreaseExperience(150);
        Check(TotalExperience(player) == total && player.PendingUpgrades == 0, "Dead player cannot gain EXP or keep upgrade rewards");
        FindAnyObjectByType<GameOverController>().Restart();
        float until = Time.realtimeSinceStartup + 20;
        while (player != null && Time.realtimeSinceStartup < until) yield return null;
        yield return null; yield return null;
        PlayerStats fresh = FindAnyObjectByType<PlayerStats>();
        Check(player == null && fresh != null && fresh.level == 1 && fresh.experience == 0 && fresh.experienceCap == 5 && fresh.PendingUpgrades == 0,
            "Actual Restart clears level, EXP and queued rewards");
        Check(fresh.ExtraProjectiles == 0 && fresh.AmmoUpgradeLevel == 0 && fresh.ReloadUpgradeLevel == 0 && fresh.QuickHandsUpgradeLevel == 0 && fresh.CriticalUpgradeLevel == 0 && fresh.BaseDamageBonus == 0,
            "Actual Restart resets every capped upgrade and flat damage");
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/SnowScene.unity");
        yield return null;
        fresh = FindAnyObjectByType<PlayerStats>();
        Check(fresh.level == 1 && fresh.experienceCap == 5 && fresh.PendingUpgrades == 0, "Snow map uses the same fresh-run curve");
        Check(Snapshot() == sources, "Runtime kill, elite, drops, upgrades and Restart leave source assets unchanged");
        yield return SceneManager.LoadSceneAsync(MainMenu.ScenePath);
    }

    private IEnumerator KillAndCollect(PlayerStats player, string path, float reward, bool elite)
    {
        var enemy = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), new Vector3(10000, 10000), Quaternion.identity);
        fixtures.Add(enemy);
        if (enemy.TryGetComponent(out Enemy movement)) movement.enabled = false;
        if (elite) enemy.AddComponent<EliteEnemy>().Configure(new EliteSpawnSettings());
        var before = new HashSet<int>(FindObjectsByType<Experience>().Select(e => e.GetInstanceID()));
        EnemyStats stats = enemy.GetComponent<EnemyStats>();
        stats.Kill(); stats.Kill();
        Experience[] spawned = FindObjectsByType<Experience>().Where(e => !before.Contains(e.GetInstanceID())).ToArray();
        Check(spawned.Length == 1 && spawned[0].experienceGranted == reward, (elite ? "Elite " : "Normal ") + path + " drops exactly one " + reward + " EXP orb, including repeated death callbacks");
        double total = TotalExperience(player);
        spawned[0].StartPull(player, 1); spawned[0].Collect(); spawned[0].Collect();
        Check(TotalExperience(player) == total + reward, "Real pickup grants " + reward + " EXP exactly once");
        if (enemy != null) Destroy(enemy);
        yield return null;
    }

    private static void Earn(PlayerStats player)
    {
        if (player.PendingUpgrades == 0) player.IncreaseExperience(player.experienceCap - player.experience);
    }
    private static double TotalExperience(PlayerStats player)
    {
        double total = player.experience;
        for (int level = 1; level < player.level; level++) total += PlayerStats.ExperienceForLevel(level);
        return total;
    }
    private static string Snapshot() => string.Join("\n", Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories)
        .Concat(new[] { "Assets/Prefab/EXP3.prefab", "Assets/MyAssets/Monster/Boss/Rotwood/Generated/RotwoodStats.asset" }).OrderBy(p => p).Select(File.ReadAllText));
    private void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add(message); }
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
