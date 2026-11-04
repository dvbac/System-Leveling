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

public sealed class EliteTreasureRegression : MonoBehaviour
{
    private readonly List<string> checks = new List<string>();
    private int loads;
    private GameObject fixtures;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/System Leveling/Run Elite Treasure Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<EliteTreasureRegression>() != null) return;
        var root = new GameObject("Elite Treasure Regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root);
        root.AddComponent<EliteTreasureRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        string failure = ""; Time.timeScale = 1;
        SceneManager.sceneLoaded += Loaded;
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
        SceneManager.sceneLoaded -= Loaded; Time.timeScale = 1;
        if (fixtures != null) DestroyImmediate(fixtures);
        Directory.CreateDirectory("Captures/EliteTreasure");
        File.WriteAllText("Captures/EliteTreasure/regression.json", JsonUtility.ToJson(new Report
        { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("ELITE TREASURE REGRESSION PASS: " + checks.Count);
        else Debug.LogError("ELITE TREASURE REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private void Loaded(Scene scene, LoadSceneMode mode) => loads++;
    private void Check(bool ok, string label) { if (!ok) throw new InvalidOperationException(label); checks.Add(label); }
    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Private).GetValue(target);

    private IEnumerator Tests()
    {
        foreach (string scene in new[] { LoadoutLauncher.GameplayScene, LoadoutLauncher.SnowScene })
        {
            int beforeLoads = loads; SceneManager.LoadSceneAsync(scene);
            float until = Time.realtimeSinceStartup + 20;
            while (loads == beforeLoads && Time.realtimeSinceStartup < until) yield return null;
            Check(loads > beforeLoads, "Gameplay map loads: " + scene); yield return null;
            var player = FindAnyObjectByType<PlayerStats>(); var spawner = FindAnyObjectByType<EnemySpawner>(); spawner.enabled = false;
            player.GetComponent<PlayerController>().enabled = false;
            foreach (var weapon in player.GetComponentsInChildren<WeaponController>()) weapon.enabled = false;
            foreach (var enemy in FindObjectsByType<EnemyStats>()) DestroyImmediate(enemy.gameObject);
            var selection = FindAnyObjectByType<LevelUpSelection>();
            var panel = Field<GameObject>(selection, "panel"); var graphics = panel.GetComponentsInChildren<Graphic>(true);
            var originalColors = graphics.Select(g => g.color).ToArray(); var buttons = Field<UnityEngine.UI.Button[]>(selection, "buttons");
            var originalButtonColors = buttons.Select(b => b.colors).ToArray();
            GameObject enemyPrefab = spawner.CurrentPhase.enemies.First(e => e.IsUsable).prefab;
            var enemyObject = Instantiate(enemyPrefab, player.transform.position + Vector3.right * 3, Quaternion.identity);
            enemyObject.GetComponent<Enemy>().enabled = false;
            var elite = enemyObject.AddComponent<EliteEnemy>(); elite.Configure(new EliteSpawnSettings());
            var stats = enemyObject.GetComponent<EnemyStats>();
            Check(elite.DropTreasure() == null, "Living elite cannot drop a chest");
            var existingChests = new HashSet<int>(FindObjectsByType<EliteTreasureChest>().Select(c => c.GetInstanceID()));
            int wallet = SoulWallet.Total;
            stats.TakeDamage(stats.currentHealth + 1); stats.Kill(); elite.DropTreasure();
            var drops = FindObjectsByType<EliteTreasureChest>().Where(c => !existingChests.Contains(c.GetInstanceID())).ToArray();
            Check(drops.Length == 1, "Actual elite death creates exactly one treasure chest despite repeated callbacks");
            Check(SoulWallet.Total == wallet + stats.SoulReward, "Kill still awards only the original Soul; bonus waits inside chest");
            var chest = drops[0]; Check(chest.LevelsGranted >= 2 && chest.LevelsGranted <= 3 && chest.SoulGranted == 50, "Chest contains a fixed 2-or-3-level roll and 50 bonus Soul");
            Directory.CreateDirectory("Captures/EliteTreasure"); ScreenCapture.CaptureScreenshot("Captures/EliteTreasure/chest-" + SceneManager.GetActiveScene().name + ".png");
            yield return null;
            wallet = SoulWallet.Total; int runSoul = FindAnyObjectByType<GameOverController>().SoulEarned;
            double remainder = player.experience;
            int level = player.level, rewards = player.PendingUpgrades, gain = chest.LevelsGranted;
            Time.timeScale = 0; Check(!chest.Collect(player) && SoulWallet.Total == wallet && player.level == level, "Paused chest cannot grant or lose rewards"); Time.timeScale = 1;
            var collector = player.GetComponentInChildren<PlayerCollector>();
            chest.SendMessage("OnTriggerEnter2D", collector.GetComponent<Collider2D>());
            Check(player.level == level && SoulWallet.Total == wallet, "EXP magnet does not auto-open chest");
            int reentries = 0;
            Action callback = () => { reentries++; Check(!chest.Collect(player), "Re-entrant EXP notification cannot collect chest twice"); };
            player.Changed += callback;
            Check(chest.Collect(player), "Player can collect the dropped chest"); player.Changed -= callback;
            Check(reentries == 1 && !chest.Collect(player), "Duplicate pickup cannot repeat payouts");
            Check(player.level == level + gain && player.experience == remainder && player.PendingUpgrades == rewards + gain, "Real EXP path grants exactly 2-3 levels and all queued upgrade choices");
            Check(SoulWallet.Total == wallet + 50 && FindAnyObjectByType<GameOverController>().SoulEarned == runSoul + 50 && !SoulWallet.HasUnsavedChanges, "Chest Soul is saved and included in run-earned summary");
            Check(selection.IsOpen && !selection.IsChoosingTome, "Chest EXP uses ordinary upgrade cards, not the Tome pool");
            while (player.PendingUpgrades > 0) { yield return null; selection.Choose(0); }
            Check(Time.timeScale == 1 && !selection.IsOpen, "All chest level-up choices can be consumed and gameplay resumes");
            Check(player.GrantTomeChoice() && selection.IsChoosingTome, "Red card still opens the special reward pool");
            Check(graphics.All(g => g.color.r >= g.color.b && g.color.r >= g.color.g), "Every modal graphic uses a warm red palette, including backdrop/frame/text");
            Check(buttons.All(b => b.colors.selectedColor.r > b.colors.selectedColor.b && b.colors.highlightedColor.r > b.colors.highlightedColor.b), "Hover and keyboard/controller selection use red rather than blue");
            ScreenCapture.CaptureScreenshot("Captures/EliteTreasure/red-theme-" + SceneManager.GetActiveScene().name + ".png");
            yield return null; selection.Choose(0);
            player.IncreaseExperience(player.experienceCap - player.experience);
            var symbols = Field<Text[]>(selection, "symbols");
            Check(!selection.IsChoosingTome && graphics.Select((g, i) => symbols.Contains(g as Text) || g.color == originalColors[i]).All(x => x), "Next normal level-up restores original frame/text theme colors");
            Check(symbols.Select((symbol, i) => symbol.color == (UpgradeCatalog.Find(selection.ChoiceAt(i))?.Color ?? Color.white)).All(x => x), "Normal upgrade icons use their offered tree colors rather than the red Tome theme");
            Check(buttons.Select((b, i) => b.colors.Equals(originalButtonColors[i])).All(x => x), "Normal upgrade restores original hover/selection colors");
            ScreenCapture.CaptureScreenshot("Captures/EliteTreasure/normal-theme-" + SceneManager.GetActiveScene().name + ".png");
            yield return null; selection.Choose(0);
            if (scene == LoadoutLauncher.GameplayScene) { ArithmeticChecks(); yield return PhysicsPickupCheck(); }
        }
    }

    private IEnumerator PhysicsPickupCheck()
    {
        var go = new GameObject("Physical chest collector"); go.SetActive(false); go.transform.SetParent(fixtures.transform);
        go.transform.position = new Vector3(20000, 20000);
        var player = go.AddComponent<PlayerStats>(); player.characterData = AssetDatabase.LoadAssetAtPath<CharacterScriptableObject>("Assets/Scriptable Object/Character/Mage.asset");
        player.enabled = false; go.AddComponent<CircleCollider2D>().radius = 0.2f;
        var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeAll;
        go.SetActive(true);
        var chest = Instantiate(Resources.Load<EliteTreasureChest>("EliteTreasureChest"), go.transform.position, Quaternion.identity, fixtures.transform);
        int wallet = SoulWallet.Total, gain = chest.LevelsGranted;
        float until = Time.realtimeSinceStartup + 3;
        while (chest != null && Time.realtimeSinceStartup < until) yield return null;
        Check(chest == null && player.level == 1 + gain && SoulWallet.Total == wallet + 50, "Real Physics2D trigger contact opens the chest and grants rewards once");
    }

    private void ArithmeticChecks()
    {
        fixtures = new GameObject("Chest calculation fixtures");
        var prefab = Resources.Load<EliteTreasureChest>("EliteTreasureChest");
        Check(prefab != null && prefab.GetComponent<SpriteRenderer>().sprite != null && prefab.GetComponent<Collider2D>().isTrigger, "Build-included chest prefab has the supplied art and pickup trigger");
        Check(prefab.GetComponent<SpriteRenderer>().sprite.texture.filterMode == FilterMode.Point, "Chest pixel art uses point filtering instead of blurred enlargement");
        foreach (int level in new[] { 1, 7, 19, 20, 21, 25, 39, 40, 59, 60 })
            foreach (int gain in new[] { 2, 3 })
            {
                var go = new GameObject("Chest test player"); go.SetActive(false); go.transform.SetParent(fixtures.transform);
                var player = go.AddComponent<PlayerStats>(); player.characterData = AssetDatabase.LoadAssetAtPath<CharacterScriptableObject>("Assets/Scriptable Object/Character/Mage.asset");
                player.enabled = false; go.SetActive(true); player.level = level; player.experienceCap = PlayerStats.ExperienceForLevel(level); player.experience = 1.5;
                var chest = Instantiate(prefab, new Vector3(10000, 10000), Quaternion.identity, fixtures.transform);
                var edit = new SerializedObject(chest); edit.FindProperty("levelsGranted").intValue = gain; edit.ApplyModifiedPropertiesWithoutUndo();
                Check(chest.Collect(player) && player.level == level + gain && player.experience == 1.5 && player.PendingUpgrades == gain,
                    "Dynamic EXP grants " + gain + " levels at level " + level + " while preserving existing progress");
                Check(!chest.Collect(player), "Calculation fixture also prevents duplicate payouts");
            }
        Check(EliteTreasureChest.ExperienceReward(25, 3) > EliteTreasureChest.ExperienceReward(1, 3), "High-level chest is not stuck at low-level EXP values");
        Check(EliteTreasureChest.ExperienceReward(int.MaxValue, 3) == int.MaxValue, "Extreme level reward saturates without integer overflow");
        var boundary = new GameObject("Level-20 chest carryover"); boundary.SetActive(false); boundary.transform.SetParent(fixtures.transform);
        var probe = boundary.AddComponent<PlayerStats>(); probe.characterData = AssetDatabase.LoadAssetAtPath<CharacterScriptableObject>("Assets/Scriptable Object/Character/Mage.asset");
        probe.enabled = false; boundary.SetActive(true); probe.level = 20; probe.experienceCap = 312; probe.experience = 300;
        var boundaryChest = Instantiate(prefab, new Vector3(10000, 10000), Quaternion.identity, fixtures.transform);
        var settings = new SerializedObject(boundaryChest); settings.FindProperty("levelsGranted").intValue = 2; settings.ApplyModifiedPropertiesWithoutUndo();
        Check(boundaryChest.Collect(probe) && probe.level == 23 && probe.experience == 20 && probe.PendingUpgrades == 3,
            "The supplied level-20 cost drop can grant an extra chest level; all carried XP is conserved");
    }

    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
