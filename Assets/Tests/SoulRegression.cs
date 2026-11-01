#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class SoulRegression : MonoBehaviour
{
    private const BindingFlags Fields = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private readonly List<int> sizes = new List<int>();
    private int originalSize = -1, loads;
    private bool background, realHadKey;
    private int realSaved;
    private string realKey;
    private static readonly Dictionary<string, int> Rewards = new Dictionary<string, int>
    {
        { "Assets/Prefab/ReferenceEnemies/Slime.prefab", 1 },
        { "Assets/Prefab/ReferenceEnemies/Skeleton.prefab", 2 },
        { "Assets/Prefab/ReferenceEnemies/Spider.prefab", 3 },
        { "Assets/Prefab/ReferenceEnemies/Wolf.prefab", 4 },
        { "Assets/Prefab/ReferenceEnemies/Snow/IceSlime.prefab", 2 },
        { "Assets/Prefab/ReferenceEnemies/Snow/SnowWolf.prefab", 5 },
        { "Assets/Prefab/ReferenceEnemies/Snow/Snowman.prefab", 6 },
        { "Assets/Prefab/Boss/Rotwood.prefab", 100 }
    };

    [MenuItem("Tools/System Leveling/Run Soul Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<SoulRegression>() != null) return;
        string key = (string)typeof(SoulWallet).GetField("DefaultKey", Fields).GetRawConstantValue();
        bool had = PlayerPrefs.HasKey(key); int saved = PlayerPrefs.GetInt(key, 0);
        GameObject root = new GameObject("Soul regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root);
        SoulRegression runner = root.AddComponent<SoulRegression>(); runner.realKey = key; runner.realHadKey = had; runner.realSaved = saved;
        runner.StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground; Application.runInBackground = true; Time.timeScale = 1;
        SceneManager.sceneLoaded += Loaded;
        var stack = new Stack<IEnumerator>(); stack.Push(Tests()); string failure = "";
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
        SoulWallet.SimulateSaveFailure = false;
        Directory.CreateDirectory("Captures/Soul");
        File.WriteAllText("Captures/Soul/runtime.json", JsonUtility.ToJson(new Report { result = failure.Length == 0 ? "PASS" : "FAIL", checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("SOUL REGRESSION PASS: " + checks.Count);
        else Debug.LogError("SOUL REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        string key = (string)typeof(SoulWallet).GetField("saveKey", Fields).GetValue(null);
        Check(key.StartsWith("SystemLeveling.Tests.Soul.") && key != realKey && SoulWallet.Total == 0, "Test save is unique and separate from the player's real Soul");
        SoulWallet.Add(7);
        Check(SoulWallet.Total == 7 && !SoulWallet.HasUnsavedChanges && PlayerPrefs.GetInt(key, -1) == 7, "Award is flushed to persistent preferences immediately");
        SoulWallet.Add(0); Check(SoulWallet.Total == 7, "Zero reward cannot change the balance");
        bool rejected = false; try { SoulWallet.Add(-1); } catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected && SoulWallet.Total == 7, "Negative additions are rejected rather than spending currency");
        IDisposable first = SoulWallet.UseTestSave(), second = SoulWallet.UseTestSave(); SoulWallet.Add(1);
        first.Dispose();
        Check((string)typeof(SoulWallet).GetField("saveKey", Fields).GetValue(null) == key && SoulWallet.Total == 8, "Overlapping test scopes cannot restore the real wallet prematurely");
        second.Dispose();
        Check((string)typeof(SoulWallet).GetField("saveKey", Fields).GetValue(null) == key && SoulWallet.Total == 8, "Out-of-order test cleanup remains isolated until the final scope ends");
        PlayerPrefs.SetInt(key, 7); PlayerPrefs.Save();
        SoulWallet.Add(int.MaxValue); Check(SoulWallet.Total == int.MaxValue, "Large rewards saturate without integer overflow");
        SoulWallet.Add(1); Check(SoulWallet.Total == int.MaxValue, "A full wallet never wraps negative");
        PlayerPrefs.SetInt(key, -7); Check(SoulWallet.Total == 0, "Invalid negative saved values cannot become a negative wallet");
        PlayerPrefs.SetInt(key, 0); PlayerPrefs.Save();

        foreach (bool snow in new[] { false, true })
        {
            Vector2Int size = snow ? new Vector2Int(1024, 768) : new Vector2Int(1920, 1080); SetResolution(size.x, size.y);
            string path = snow ? LoadoutLauncher.SnowScene : LoadoutLauncher.GameplayScene;
            yield return SceneManager.LoadSceneAsync(path); yield return null;
            FindAnyObjectByType<EnemySpawner>().enabled = false;
            GameOverController run = FindAnyObjectByType<GameOverController>();
            Check(run.SoulEarned == 0, "Every new map run starts at zero earned Soul, independent of saved balance");
            int before = SoulWallet.Total;
            PlayerPrefs.SetInt(key, int.MaxValue);
            SoulBalanceView maximumView = FindAnyObjectByType<SoulBalanceView>(); maximumView.SendMessage("LateUpdate"); Verify(maximumView);
            PlayerPrefs.SetInt(key, before); PlayerPrefs.Save();
            foreach (var reward in Rewards)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(reward.Key);
                EnemyStats monster = Instantiate(prefab, new Vector3(10000, 10000), Quaternion.identity).GetComponent<EnemyStats>();
                if (monster.TryGetComponent(out Enemy movement)) movement.enabled = false;
                if (monster.TryGetComponent(out Rigidbody2D body)) body.simulated = false;
                int total = SoulWallet.Total;
                int runBefore = run.SoulEarned;
                monster.TakeDamage(1000000000);
                Check(monster.IsDead && monster.SoulReward == reward.Value && SoulWallet.Total == total + reward.Value, "Actual lethal damage awards configured Soul: " + prefab.name);
                Check(run.SoulEarned == runBefore + reward.Value, "Run earnings count the actual reward: " + prefab.name);
                monster.Kill(); monster.TakeDamage(1000000000);
                Check(SoulWallet.Total == total + reward.Value, "Repeated death callbacks never double-award: " + prefab.name);
                Check(run.SoulEarned == runBefore + reward.Value, "Repeated death never doubles run earnings: " + prefab.name);
                DestroyImmediate(monster.gameObject);
            }
            Check(SoulWallet.Total == before + 123, "All eight enemy/boss types award their distinct configured totals");
            SoulBalanceView view = FindAnyObjectByType<SoulBalanceView>(); view.SendMessage("LateUpdate");
            Verify(view); yield return new WaitForEndOfFrame(); Capture("hud-" + size + ".png");

            if (!snow)
            {
                int saved = SoulWallet.Total; SoulWallet.SimulateSaveFailure = true;
                EnemyStats monster = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Slime.prefab"), new Vector3(10000, 10000), Quaternion.identity).GetComponent<EnemyStats>();
                monster.Kill();
                Check(monster.IsDead && SoulWallet.Total == saved + 1 && SoulWallet.HasUnsavedChanges && PlayerPrefs.GetInt(key) == saved, "A failed save retains the award and does not stop actual enemy death");
                SoulWallet.Add(4); view.SendMessage("LateUpdate");
                Check(SoulWallet.Total == saved + 5 && view.caption.text.Contains("SAVE FAILED"), "Failed awards accumulate in memory and show a visible save warning");
                SoulWallet.SimulateSaveFailure = false;
                Check(SoulWallet.Save() && !SoulWallet.HasUnsavedChanges && PlayerPrefs.GetInt(key) == saved + 5, "Retry persists the exact pending total without losing awards");
                SoulWallet.Save(); view.SendMessage("LateUpdate");
                Check(SoulWallet.Total == saved + 5 && view.caption.text == "SOUL", "Repeated save is idempotent and clears the warning");
                DestroyImmediate(monster.gameObject);
            }

            GameObject zero = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Slime.prefab"), new Vector3(10000, 10000), Quaternion.identity);
            SerializedObject data = new SerializedObject(zero.GetComponent<EnemyStats>()); data.FindProperty("soulReward").intValue = -5; data.ApplyModifiedPropertiesWithoutUndo();
            before = SoulWallet.Total; zero.GetComponent<EnemyStats>().Kill();
            Check(SoulWallet.Total == before, "Invalid negative per-enemy reward cannot deduct or grant Soul"); DestroyImmediate(zero);
            GameObject alive = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Slime.prefab"), new Vector3(10000, 10000), Quaternion.identity);
            EnemySpawner clock = FindAnyObjectByType<EnemySpawner>();
            clock.Tick(65.25f);
            float finishedAt = clock.ElapsedTime; int earned = run.SoulEarned;
            if (snow) FindAnyObjectByType<PlayerStats>().Kill(); else run.Win();
            VerifySummary(run, finishedAt, earned);
            Check(run.HasWon == !snow, "Both victory and defeat show run results");
            clock.Tick(100f); run.Win();
            Check(clock.ElapsedTime == finishedAt && run.PlayedSeconds == finishedAt && run.SoulEarned == earned, "Finished time and earnings cannot advance or be overwritten");
            yield return new WaitForEndOfFrame(); Capture((snow ? "defeat-" : "victory-") + size + ".png");
            RunMenu menu = FindAnyObjectByType<RunMenu>(); menu.OpenPause();
            int loadBefore = loads; FindAnyObjectByType<GameOverController>().Restart(); yield return WaitForReload(loadBefore);
            Check(SceneManager.GetActiveScene().path == path && SoulWallet.Total == before, "Quick Restart retains Soul and unloading a living enemy grants nothing");
            Check(alive == null, "Previous run's living enemy was unloaded, not killed for rewards");
            Check(FindAnyObjectByType<GameOverController>().SoulEarned == 0, "Quick Restart resets run Soul without clearing persistent Soul");
            view = FindAnyObjectByType<SoulBalanceView>(); view.SendMessage("LateUpdate"); Verify(view);
            FindAnyObjectByType<PlayerStats>().Kill();
            EnemyStats afterDeath = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Slime.prefab"), new Vector3(10000, 10000), Quaternion.identity).GetComponent<EnemyStats>();
            afterDeath.Kill(); Check(SoulWallet.Total == before, "Game Over/cleanup kills cannot farm new Soul"); DestroyImmediate(afterDeath.gameObject);
            Check(FindAnyObjectByType<GameOverController>().SoulEarned == 0, "Post-death kills cannot change the finished run summary");
            loadBefore = loads; FindAnyObjectByType<GameOverController>().Restart(); yield return WaitForReload(loadBefore);
            Check(SoulWallet.Total == before, "Death and death Quick Restart never reset accumulated Soul");
            FindAnyObjectByType<PlayerStats>().Kill(); FindAnyObjectByType<GameOverController>().AnotherFate();
            yield return WaitForScene(LoadoutLauncher.SelectionScene);
            LoadoutSelection selection = FindAnyObjectByType<LoadoutSelection>(); LoadoutLauncher launcher = selection.GetComponent<LoadoutLauncher>();
            Check(SoulWallet.Total == before && !selection.ChoosingWeapon && LoadoutLauncher.Character == null, "Another Fate clears the run choices but preserves currency");
            view = FindAnyObjectByType<SoulBalanceView>(); view.SendMessage("LateUpdate"); Verify(view);
            yield return new WaitForEndOfFrame(); Capture("characters-" + size + ".png");
            Click(selection.characterView.cards[0]); view.SendMessage("LateUpdate"); Verify(view);
            yield return new WaitForEndOfFrame(); Capture("weapons-" + size + ".png");
            Click(selection.weaponView.cards[0]); view = FindAnyObjectByType<SoulBalanceView>(); view.SendMessage("LateUpdate"); Verify(view);
            Check(SoulWallet.Total == before, "Map selection reads the same persistent wallet");
            launcher.BackToWeapons(); selection.Back(); selection.Back(); yield return WaitForScene(MainMenu.ScenePath);
            Check(FindAnyObjectByType<SoulBalanceView>() == null, "Main Menu shows no Soul counter");
            yield return new WaitForEndOfFrame(); Capture("main-menu-" + size + ".png");
            Check(SoulWallet.Total == before && PlayerPrefs.GetInt(key, -1) == before, "Main Menu and new scene instances reload the saved total");
        }
        Check(PlayerPrefs.HasKey(realKey) == realHadKey && PlayerPrefs.GetInt(realKey, 0) == realSaved, "Real player save remains completely untouched by the test");
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene);
    }

    private void Verify(SoulBalanceView view)
    {
        Canvas.ForceUpdateCanvases();
        GameOverController run = view.GetComponentInParent<GameOverController>();
        int expected = run != null ? run.SoulEarned : SoulWallet.Total;
        Check(view.amount.text == expected.ToString("N0"), "Visible Soul counter shows run earnings in gameplay and total in selection");
        foreach (Text text in new[] { view.caption, view.amount }) Check(text.preferredWidth <= text.rectTransform.rect.width + 1 && text.preferredHeight <= text.rectTransform.rect.height + 1, "Soul label fits: " + text.name);
        var corners = new Vector3[4]; view.GetComponent<RectTransform>().GetWorldCorners(corners);
        foreach (Vector3 corner in corners) Check(corner.x >= 0 && corner.x <= Screen.width && corner.y >= 0 && corner.y <= Screen.height, "Soul panel stays inside the viewport");
        foreach (Graphic graphic in view.GetComponentsInChildren<Graphic>()) Check(!graphic.raycastTarget, "Soul display cannot intercept gameplay/menu clicks");
    }
    private void VerifySummary(GameOverController run, float time, int earned)
    {
        Text result = (Text)typeof(GameOverController).GetField("resultText", Fields).GetValue(run);
        int seconds = Mathf.FloorToInt(time);
        Check(run.PlayedSeconds == time && result.text.Contains($"TIME PLAYED  {seconds / 60:00}:{seconds % 60:00}") && result.text.Contains($"SOUL EARNED  {earned:N0}"), "Result shows the frozen gameplay time and run-only Soul earned");
        Canvas.ForceUpdateCanvases();
        Check(result.preferredHeight <= result.rectTransform.rect.height + 1 && result.preferredWidth <= result.rectTransform.rect.width + 1, "Run result text fits the existing modal");
    }
    private IEnumerator WaitForScene(string path)
    {
        float until = Time.realtimeSinceStartup + 20; while (SceneManager.GetActiveScene().path != path && Time.realtimeSinceStartup < until) yield return null;
        Check(SceneManager.GetActiveScene().path == path, "Reached " + path); yield return null;
    }
    private IEnumerator WaitForReload(int before)
    {
        float until = Time.realtimeSinceStartup + 20; while (loads == before && Time.realtimeSinceStartup < until) yield return null;
        Check(loads == before + 1, "Restart reloads exactly once"); yield return null;
    }
    private void SetResolution(int width, int height)
    {
        Assembly assembly = typeof(Editor).Assembly; Type type = assembly.GetType("UnityEditor.GameViewSizes");
        object instance = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null); object group = type.GetProperty("currentGroup", Fields).GetValue(instance);
        EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView")); PropertyInfo selected = window.GetType().GetProperty("selectedSizeIndex", Fields);
        if (originalSize < 0) originalSize = (int)selected.GetValue(window);
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        object size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new object[] { Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), width, height, "Soul temporary" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size }); sizes.Add(index); selected.SetValue(window, index); window.Show(); window.Focus();
    }
    private static void Click(Button button) => ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    private void Loaded(Scene scene, LoadSceneMode mode) => loads++;
    private void Check(bool pass, string text) { if (!pass) throw new Exception(text); checks.Add(text); }
    private static void Capture(string name)
    {
        Directory.CreateDirectory("Captures/Soul"); Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes("Captures/Soul/" + name, image.EncodeToPNG()); } finally { Destroy(image); }
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= Loaded; SoulWallet.SimulateSaveFailure = false; Application.runInBackground = background; Time.timeScale = 1;
        if (originalSize < 0) return;
        Assembly assembly = typeof(Editor).Assembly; EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView")); window.GetType().GetProperty("selectedSizeIndex", Fields).SetValue(window, originalSize);
        Type type = assembly.GetType("UnityEditor.GameViewSizes"); object instance = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null); object group = type.GetProperty("currentGroup", Fields).GetValue(instance);
        for (int i = sizes.Count - 1; i >= 0; i--) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { sizes[i] });
    }
    [Serializable] private class Report { public string result, failure; public string[] checks; }
}
#endif
