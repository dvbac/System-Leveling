#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class GameOverRegressionRunner : MonoBehaviour
{
    private const string Output = "Captures/BlueHudGameOver";
    private readonly List<string> checks = new List<string>();
    private readonly List<int> temporarySizes = new List<int>();
    private bool background;
    private int loads;
    private int originalSize = -1;
    private string sourceData;
    private bool cleaned;
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    [MenuItem("Tools/System Leveling/Run Game Over Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<GameOverRegressionRunner>() != null) return;
        GameObject root = new GameObject("Game Over Regression Runner");
        DontDestroyOnLoad(root);
        root.AddComponent<GameOverRegressionRunner>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground;
        Application.runInBackground = true;
        sourceData = SnapshotData();
        SceneManager.sceneLoaded += Loaded;
        Directory.CreateDirectory(Output);
        IEnumerator test = Tests();
        string failure = "";
        while (true)
        {
            bool next = false;
            object wait = null;
            try { next = test.MoveNext(); if (next) wait = test.Current; }
            catch (Exception ex) { failure = ex.ToString(); }
            if (!next || failure.Length > 0) break;
            yield return wait;
        }
        Cleanup();
        Report report = new Report { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure, reloads = loads };
        File.WriteAllText(Output + "/regression.json", JsonUtility.ToJson(report, true));
        if (failure.Length == 0) Debug.Log($"GAME OVER REGRESSION PASS: {checks.Count} checks, {loads} actual scene reloads.");
        else Debug.LogError("GAME OVER REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        // Always establish a clean baseline, even if the menu is started during Game Over.
        Time.timeScale = 1f;
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().path, LoadSceneMode.Single);
        float initializationDeadline = Time.realtimeSinceStartup + 15f;
        while (loads == 0 && Time.realtimeSinceStartup < initializationDeadline) yield return null;
        Require(loads == 1, "Test baseline loads the actual gameplay scene");
        loads = 0;
        yield return null;
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        while (player != null && player.experienceCap == 0 && Time.realtimeSinceStartup < initializationDeadline)
            yield return null;
        GameOverController gameOver = FindAnyObjectByType<GameOverController>();
        LevelUpSelection selection = FindAnyObjectByType<LevelUpSelection>();
        Require(player != null && gameOver != null && selection != null, "Scene has Player, blue HUD, level-up and Game Over references");
        Require(!player.IsDead && !gameOver.IsGameOver && Time.timeScale == 1, "Fresh run starts alive with gameplay running");
        player.invincibilityDuration = 999f;
        SetResolution(1280, 720);
        yield return null;
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Capture("hud-16x9.png");
        SetResolution(1280, 960);
        yield return null;
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Capture("hud-4x3.png");
        Require(Screen.width == 1280 && Screen.height == 960, "4:3 evidence uses actual 1280x960 Game View");

        player.IncreaseExperience(player.experienceCap - player.experience);
        Require(selection.IsOpen && Time.timeScale == 0, "Level-up pause still works before death");
        yield return new WaitForEndOfFrame();
        Capture("level-up-4x3.png");
        SetResolution(1280, 720);
        yield return null;
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Capture("level-up-16x9.png");
        Require(player.TryApplyUpgrade(UpgradeKind.Damage) && player.DamageMultiplier > 1, "Earned upgrade changes this run only");
        yield return null;

        string[] names = {"Blue Slime", "ReferenceEnemies/Slime", "ReferenceEnemies/Skeleton", "ReferenceEnemies/Spider", "ReferenceEnemies/Wolf"};
        List<Enemy> enemies = new List<Enemy>();
        for (int i = 0; i < names.Length; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/" + names[i] + ".prefab");
            bool syntheticLegacy = prefab == null && i == 0;
            if (syntheticLegacy) prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Slime.prefab");
            GameObject enemy = Instantiate(prefab, player.transform.position + new Vector3(-4 + i * 2, 4, 0), Quaternion.identity);
            if (syntheticLegacy)
            {
                DestroyImmediate(enemy.GetComponent<EnemyAnimation>());
                names[i] = "Legacy Slime (without EnemyAnimation)";
            }
            enemies.Add(enemy.GetComponent<Enemy>());
        }
        GameObject marker = new GameObject("Previous Run Marker");
        yield return null;
        int deathEvents = 0;
        player.Died += () => deathEvents++;
        // The scene may already have active enemies when a user starts this test menu.
        // Remove only the test subject's existing i-frame, then exercise real lethal damage.
        typeof(PlayerStats).GetField("isInvincible", Flags).SetValue(player, false);
        player.TakeDamage(player.MaxHealth + 1000);
        Require(player.IsDead && player.currentHealth == 0 && gameOver.IsGameOver && deathEvents == 1, "Lethal damage emits death once and opens Game Over");
        player.Kill();
        player.RestoreHealth(10000);
        player.currentRecovery = 100;
        player.IncreaseExperience(10000);
        Require(deathEvents == 1 && player.currentHealth == 0 && player.PendingUpgrades == 0 && !player.TryApplyUpgrade(UpgradeKind.Vitality), "Repeated death, healing, EXP and upgrades cannot resurrect a dead player");
        Require(!selection.IsOpen && LevelUpSelection.InputBlocked && GameOverController.GameplayStopped, "Game Over closes upgrades and blocks input");
        Require(Time.timeScale == 1, "Game Over keeps time running for Idle animation");
        Text result = Field<Text>(gameOver, "resultText");
        Require(result.text.Contains(player.level.ToString("00")), "Game Over displays final level");
        yield return null;
        yield return null;

        Vector3[] positions = new Vector3[enemies.Count];
        float[] animationTimes = new float[enemies.Count];
        for (int i = 0; i < enemies.Count; i++)
        {
            positions[i] = enemies[i].transform.position;
            Animator animator = enemies[i].GetComponent<Animator>();
            Require(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), names[i] + " enters Idle");
            animationTimes[i] = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }
        int enemyCount = FindObjectsByType<Enemy>().Length;
        Vector3 playerPosition = player.transform.position;
        EnemySpawner observedSpawner = FindAnyObjectByType<EnemySpawner>();
        float spawnerTimer = observedSpawner.NormalSpawnTimer;
        float hordeTimer = observedSpawner.HordeCheckTimer;
        float survivalTime = observedSpawner.ElapsedTime;
        yield return new WaitForEndOfFrame();
        Capture("game-over-16x9.png");
        SetResolution(1280, 960);
        yield return null;
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return new WaitForEndOfFrame();
        Capture("game-over-4x3.png");
        yield return new WaitForSecondsRealtime(5.5f);
        Require(FindObjectsByType<Enemy>().Length == enemyCount, "No new enemies spawn after more than the five-second spawn interval");
        Require(observedSpawner.NormalSpawnTimer == spawnerTimer && observedSpawner.HordeCheckTimer == hordeTimer && observedSpawner.ElapsedTime == survivalTime,
            "Normal, Horde and survival clocks are suspended during Game Over");
        for (int i = 0; i < enemies.Count; i++)
        {
            Rigidbody2D body = enemies[i].GetComponent<Rigidbody2D>();
            AnimatorStateInfo info = enemies[i].GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            Require(enemies[i].transform.position == positions[i] && body.linearVelocity == Vector2.zero && !body.simulated, names[i] + " remains stationary without physics drift");
            Require(info.IsName("Idle") && info.normalizedTime > animationTimes[i] + 0.1f, names[i] + " Idle continues animating while Game Over is visible");
        }
        Require(player.transform.position == playerPosition && player.currentHealth == 0, "Dead player stays still and cannot regenerate");
        Button restart = Field<Button>(gameOver, "restartButton");
        PointerEventData pointer = new PointerEventData(EventSystem.current);
        RectTransform retryRect = restart.GetComponent<RectTransform>();
        pointer.position = RectTransformUtility.WorldToScreenPoint(null, retryRect.TransformPoint(retryRect.rect.center));
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);
        Require(hits.Count > 0 && hits[0].gameObject == restart.gameObject, "Restart button receives the Game Over pointer raycast");
        ExecuteEvents.Execute(restart.gameObject, pointer, ExecuteEvents.pointerClickHandler);
        gameOver.Restart();
        gameOver.Restart();
        Require(gameOver.IsRestarting && !restart.interactable, "Repeated Restart requests are gated while a single load is pending");
        float until = Time.realtimeSinceStartup + 15;
        while (loads < 1 && Time.realtimeSinceStartup < until) yield return null;
        Require(loads == 1, "First Restart performs exactly one real scene reload");
        yield return null;
        player = FindAnyObjectByType<PlayerStats>();
        gameOver = FindAnyObjectByType<GameOverController>();
        selection = FindAnyObjectByType<LevelUpSelection>();
        CheckFreshRun(player, gameOver);
        Require(marker == null && enemies.TrueForAll(enemy => enemy == null), "Restart removes all previous run enemies and runtime objects");
        yield return null;
        Require(!LevelUpSelection.InputBlocked, "No stale modal input lock leaks into the new run");

        player.IncreaseExperience(player.experienceCap - player.experience);
        Require(selection.IsOpen && Time.timeScale == 0, "Second run can open a new upgrade modal");
        player.Kill();
        Require(gameOver.IsGameOver && !selection.IsOpen && Time.timeScale == 1, "Death overrides an already-open level-up modal without freezing Idle");
        selection.Choose(0);
        selection.enabled = false;
        Require(player.IsDead && GameOverController.GameplayStopped && Time.timeScale == 1 && player.PendingUpgrades == 0, "Choosing or disabling upgrades cannot resume a dead run");
        gameOver.enabled = false;
        Require(GameOverController.GameplayStopped, "Disabling the Game Over view does not resurrect gameplay");
        gameOver.enabled = true;
        gameOver.Restart();
        until = Time.realtimeSinceStartup + 15;
        while (loads < 2 && Time.realtimeSinceStartup < until) yield return null;
        Require(loads == 2, "Game Over and Restart work a second time");
        yield return null;
        CheckFreshRun(FindAnyObjectByType<PlayerStats>(), FindAnyObjectByType<GameOverController>());
        Require(SnapshotData() == sourceData, "Original character, weapon and enemy data assets are unchanged");
    }

    private void CheckFreshRun(PlayerStats player, GameOverController gameOver)
    {
        Require(!player.IsDead && player.currentHealth == player.MaxHealth && player.level == 1 && player.experience == 0 && player.PendingUpgrades == 0, "Restart restores full HP, level 1, zero EXP and no pending rewards");
        Require(player.DamageMultiplier == 1 && !gameOver.IsGameOver && !GameOverController.GameplayStopped && Time.timeScale == 1, "Restart resets upgrades, death flags and time scale");
    }

    private static T Field<T>(object target, string name) => (T)target.GetType().GetField(name, Flags).GetValue(target);
    private void Require(bool ok, string message) { if (!ok) throw new Exception(message); checks.Add(message); }
    private void Loaded(Scene scene, LoadSceneMode mode) { if (scene.name == "SampleScene") { GameplayRegressionBaseline.Prepare(); loads++; } }
    private static string SnapshotData()
    {
        string result = "";
        foreach (string path in Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories)) result += path + File.ReadAllText(path);
        return result;
    }

    private void SetResolution(int width, int height)
    {
        Assembly assembly = typeof(Editor).Assembly;
        Type sizesType = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
        object group = sizesType.GetProperty("currentGroup", Flags).GetValue(sizes);
        EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        PropertyInfo property = window.GetType().GetProperty("selectedSizeIndex", Flags);
        if (originalSize < 0) originalSize = (int)property.GetValue(window);
        Type sizeType = assembly.GetType("UnityEditor.GameViewSize");
        Type enumType = assembly.GetType("UnityEditor.GameViewSizeType");
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        object size = Activator.CreateInstance(sizeType, new object[] { Enum.Parse(enumType, "FixedResolution"), width, height, "Blue HUD temporary" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] {size});
        temporarySizes.Add(index);
        property.SetValue(window, index);
        window.Show();
        window.Focus();
        window.Repaint();
    }

    private static void Capture(string name)
    {
        Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Output + "/" + name, texture.EncodeToPNG());
        Destroy(texture);
    }

    private void Cleanup()
    {
        if (cleaned) return;
        cleaned = true;
        SceneManager.sceneLoaded -= Loaded;
        Application.runInBackground = background;
        if (originalSize < 0) return;
        Assembly assembly = typeof(Editor).Assembly;
        EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        window.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(window, originalSize);
        Type type = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        object group = type.GetProperty("currentGroup", Flags).GetValue(sizes);
        for (int i = temporarySizes.Count - 1; i >= 0; i--)
            group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] {temporarySizes[i]});
    }

    private void OnDestroy() => Cleanup();
    [Serializable] private class Report { public string result; public string utc; public string[] checks; public string failure; public int reloads; }
}
#endif
