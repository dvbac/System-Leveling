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
        Check(player.level == 1 && player.experience == 0 && player.experienceCap == 45, "Gameplay starts at level 1, 0/45 EXP");
        player.IncreaseExperience(44); player.IncreaseExperience(0); player.IncreaseExperience(-1);
        Check(player.level == 1 && player.experience == 44 && player.PendingUpgrades == 0, "Below-threshold EXP and invalid gains cannot grant a level");
        player.IncreaseExperience(1);
        Check(player.level == 2 && player.experience == 0 && player.experienceCap == 60 && player.PendingUpgrades == 1, "Exact threshold grants one level and sets the next cost");
        player.IncreaseExperience(65);
        Check(player.level == 3 && player.experience == 5 && player.experienceCap == 75 && player.PendingUpgrades == 2, "EXP remainder carries forward");
        player.IncreaseExperience(2000);
        Check(player.level == 15 && player.experience == 115 && player.experienceCap == 255 && player.PendingUpgrades == 14, "Large pickup queues all earned levels and keeps increasing past level 5");
        Check(PlayerStats.ExperienceForLevel(0) == 45 && PlayerStats.ExperienceForLevel(10) == 180 && PlayerStats.ExperienceForLevel(int.MaxValue) == int.MaxValue, "Curve handles invalid levels, level 10 and integer overflow safely");

        string[] names = { "Slime", "Spider", "Wolf", "Skeleton", "Snow/Snowman", "Snow/IceSlime", "Snow/SnowWolf" };
        int[] rewards = { 5, 8, 10, 12, 15, 5, 10 };
        for (int i = 0; i < names.Length; i++)
        {
            yield return KillAndCollect(player, "Assets/Prefab/ReferenceEnemies/" + names[i] + ".prefab", rewards[i], false);
            yield return KillAndCollect(player, "Assets/Prefab/ReferenceEnemies/" + names[i] + ".prefab", rewards[i] * 3, true);
        }
        Check(AssetDatabase.LoadAssetAtPath<EnemyScriptableObject>("Assets/Scriptable Object/Enemy/Blue Slime.asset").ExperienceReward == 5, "Legacy Blue/Ice Slime reward is also 5");
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

        player.Kill(); int total = TotalExperience(player);
        player.IncreaseExperience(150);
        Check(TotalExperience(player) == total && player.PendingUpgrades == 0, "Dead player cannot gain EXP or keep upgrade rewards");
        FindAnyObjectByType<GameOverController>().Restart();
        float until = Time.realtimeSinceStartup + 20;
        while (player != null && Time.realtimeSinceStartup < until) yield return null;
        yield return null; yield return null;
        PlayerStats fresh = FindAnyObjectByType<PlayerStats>();
        Check(player == null && fresh != null && fresh.level == 1 && fresh.experience == 0 && fresh.experienceCap == 45 && fresh.PendingUpgrades == 0,
            "Actual Restart clears level, EXP and queued rewards");
        Check(fresh.ExtraProjectiles == 0 && fresh.AmmoUpgradeLevel == 0 && fresh.ReloadUpgradeLevel == 0 && fresh.QuickHandsUpgradeLevel == 0 && fresh.CriticalUpgradeLevel == 0 && fresh.BaseDamageBonus == 0,
            "Actual Restart resets every capped upgrade and flat damage");
        yield return SceneManager.LoadSceneAsync("Assets/Scenes/SnowScene.unity");
        yield return null;
        fresh = FindAnyObjectByType<PlayerStats>();
        Check(fresh.level == 1 && fresh.experienceCap == 45 && fresh.PendingUpgrades == 0, "Snow map uses the same fresh-run curve");
        Check(Snapshot() == sources, "Runtime kill, elite, drops, upgrades and Restart leave source assets unchanged");
        yield return SceneManager.LoadSceneAsync(MainMenu.ScenePath);
    }

    private IEnumerator KillAndCollect(PlayerStats player, string path, int reward, bool elite)
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
        int total = TotalExperience(player);
        spawned[0].StartPull(player, 1); spawned[0].Collect(); spawned[0].Collect();
        Check(TotalExperience(player) == total + reward, "Real pickup grants " + reward + " EXP exactly once");
        if (enemy != null) Destroy(enemy);
        yield return null;
    }

    private static void Earn(PlayerStats player)
    {
        if (player.PendingUpgrades == 0) player.IncreaseExperience(player.experienceCap - player.experience);
    }
    private static int TotalExperience(PlayerStats player) =>
        (int)(45L * (player.level - 1) + 15L * (player.level - 1) * (player.level - 2) / 2 + player.experience);
    private static string Snapshot() => string.Join("\n", Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories)
        .Concat(new[] { "Assets/Prefab/EXP3.prefab", "Assets/MyAssets/Monster/Boss/Rotwood/Generated/RotwoodStats.asset" }).OrderBy(p => p).Select(File.ReadAllText));
    private void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add(message); }
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
