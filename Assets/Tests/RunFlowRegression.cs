#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunFlowRegression : MonoBehaviour
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private readonly List<int> sizes = new List<int>();
    private int originalSize = -1, loads;
    private Keyboard keyboard;
    private bool background;
    private float originalVolume;

    [MenuItem("Tools/System Leveling/Run Menu Flow Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<RunFlowRegression>() != null) return;
        GameObject root = new GameObject("Run flow regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root);
        root.AddComponent<RunFlowRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground; originalVolume = AudioListener.volume;
        Application.runInBackground = true; Time.timeScale = 1; keyboard = InputSystem.AddDevice<Keyboard>();
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
        Directory.CreateDirectory("Captures/RunFlow");
        File.WriteAllText("Captures/RunFlow/runtime.json", JsonUtility.ToJson(new Report { result = failure.Length == 0 ? "PASS" : "FAIL", checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("RUN FLOW REGRESSION PASS: " + checks.Count);
        else Debug.LogError("RUN FLOW REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        foreach (bool snow in new[] { false, true })
        {
            Vector2Int size = snow ? new Vector2Int(1024, 768) : new Vector2Int(1920, 1080); SetResolution(size.x, size.y);
            yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene); yield return null;
            LoadoutSelection selection = FindAnyObjectByType<LoadoutSelection>(); LoadoutLauncher launcher = selection.GetComponent<LoadoutLauncher>();
            Check(!launcher.ChoosingMap && !launcher.mapPage.activeInHierarchy, "Run begins at character selection, not map selection");
            Click(selection.characterView.cards[0]); Click(selection.weaponView.cards[0]);
            Check(launcher.ChoosingMap && !launcher.IsLoading && !launcher.loadoutPage.activeSelf, "Character > weapon > separate map screen, without starting gameplay");
            yield return Escape();
            Check(!launcher.ChoosingMap && selection.ChoosingWeapon && !selection.Confirmed, "Esc from map returns exactly one step to weapons");
            Click(selection.weaponView.cards[0]); yield return null; yield return new WaitForEndOfFrame();
            Verify(launcher.mapPage, launcher.grassMapButton, launcher.snowMapButton, launcher.mapBackButton); Capture("map-" + size + ".png");
            GameObject character = selection.SelectedCharacterPrefab, weapon = selection.SelectedWeapon;
            Click(snow ? launcher.snowMapButton : launcher.grassMapButton);
            string expected = snow ? LoadoutLauncher.SnowScene : LoadoutLauncher.GameplayScene;
            yield return WaitForScene(expected);
            Check(SceneManager.GetActiveScene().path == expected, "Map card loads the chosen map");
            PlayerStats player = FindAnyObjectByType<PlayerStats>(); EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>(); spawner.enabled = false;
            PlayerHud hud = FindAnyObjectByType<PlayerHud>(); Text timeText = Read<Text>(hud, "timeText");
            hud.SendMessage("LateUpdate");
            Check(Read<EnemySpawner>(hud, "runClock") == spawner && timeText.text == "00:00", "HUD timer uses this map's survival clock and starts at 00:00");
            var phases = Read<List<SpawnPhase>>(spawner, "phases"); HordeSpawnSettings horde = Read<HordeSpawnSettings>(spawner, "horde"); bool hordeEnabled = horde.enabled;
            try
            {
                spawner.GetType().GetField("phases", Flags).SetValue(spawner, new List<SpawnPhase>()); horde.enabled = false;
                spawner.Tick(1 - spawner.ElapsedTime); hud.SendMessage("LateUpdate"); Check(timeText.text == "00:01", "Timer displays whole elapsed seconds");
                spawner.Tick(58); hud.SendMessage("LateUpdate"); Check(timeText.text == "00:59", "Timer formats seconds with two digits");
                spawner.Tick(1); hud.SendMessage("LateUpdate"); Check(timeText.text == "01:00", "Timer rolls seconds into minutes correctly");
                spawner.Tick(3540); hud.SendMessage("LateUpdate"); Check(timeText.text == "60:00", "Long runs do not wrap the clock back to zero at one hour");
            }
            finally { spawner.GetType().GetField("phases", Flags).SetValue(spawner, phases); horde.enabled = hordeEnabled; }
            Canvas.ForceUpdateCanvases();
            Check(timeText.preferredHeight <= timeText.rectTransform.rect.height + 1 && timeText.preferredWidth <= timeText.rectTransform.rect.width + 1, "Timer text fits its HUD panel");
            var timerCorners = new Vector3[4]; timeText.transform.parent.GetComponent<RectTransform>().GetWorldCorners(timerCorners);
            foreach (Vector3 corner in timerCorners) Check(corner.x >= 0 && corner.x <= Screen.width && corner.y >= 0 && corner.y <= Screen.height, "Timer panel stays inside the viewport");
            foreach (Graphic graphic in timeText.transform.parent.GetComponentsInChildren<Graphic>()) Check(!graphic.raycastTarget, "Timer decoration does not intercept menu clicks");
            yield return new WaitForEndOfFrame(); Capture("timer-" + size + ".png");
            RunMenu menu = FindAnyObjectByType<RunMenu>(); GameOverController gameOver = FindAnyObjectByType<GameOverController>();
            LevelUpSelection levelUp = FindAnyObjectByType<LevelUpSelection>();
            yield return Escape();
            Check(RunMenu.IsPaused && menu.pausePanel.activeSelf && Time.timeScale == 0 && LevelUpSelection.InputBlocked, "Real Esc pauses the game and blocks gameplay input");
            float clock = spawner.ElapsedTime; Vector3 position = player.transform.position;
            spawner.Tick(10); InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            Check(spawner.ElapsedTime == clock && player.transform.position == position && player.GetComponent<PlayerController>().moveDir == Vector2.zero, "Pause freezes spawn clocks and actual movement input");
            Check(timeText.text == "60:00", "Esc pause freezes the HUD timer");
            yield return new WaitForEndOfFrame(); Verify(menu.pausePanel, menu.continueButton, menu.restartButton, menu.settingsButton, menu.mainMenuButton); Capture("pause-" + size + ".png");
            Click(menu.settingsButton); Check(menu.settingsPanel.activeSelf && !menu.pausePanel.activeSelf && Time.timeScale == 0, "Pause Settings keeps the game paused");
            spawner.Tick(10); hud.SendMessage("LateUpdate"); Check(timeText.text == "60:00", "Settings does not advance the HUD timer");
            menu.volume.value = 0.25f;
            Check(Mathf.Approximately(AudioListener.volume, 0.25f) && menu.volumeValue.text == "25%", "Settings slider applies real master volume");
            float saved = AudioListener.volume; menu.SetVolume(float.NaN); Check(AudioListener.volume == saved, "Invalid volume cannot corrupt audio state");
            yield return new WaitForEndOfFrame(); Verify(menu.settingsPanel, menu.settingsBackButton); Capture("settings-" + size + ".png");
            yield return Escape(); Check(menu.pausePanel.activeSelf && !menu.settingsPanel.activeSelf && Time.timeScale == 0, "Settings Esc returns to the pause menu, not gameplay");
            yield return Escape(); Check(!menu.IsOpen && Time.timeScale == 1, "Pause Esc continues gameplay");
            yield return null; yield return null;
            menu.OpenPause(); player.IncreaseExperience(player.experienceCap - player.experience);
            Check(player.PendingUpgrades > 0 && !levelUp.IsOpen, "XP earned during pause is queued without a competing modal");
            Click(menu.continueButton); Check(levelUp.IsOpen && Time.timeScale == 0, "Continue presents the queued upgrade without unpausing it");
            spawner.Tick(10); hud.SendMessage("LateUpdate"); Check(timeText.text == "60:00", "Level-up selection freezes the HUD timer");
            yield return Escape(); Check(levelUp.IsOpen && !menu.IsOpen, "Esc cannot bypass a pending upgrade");
            levelUp.Choose(0); yield return null; yield return null;
            Check(!levelUp.IsOpen && Time.timeScale == 1, "Choosing the upgrade restores normal time");
            spawner.Tick(1); hud.SendMessage("LateUpdate"); Check(timeText.text == "60:01", "Timer resumes when gameplay continues");
            menu.OpenPause(); int before = loads; Click(menu.restartButton); gameOver.ReturnToMainMenu();
            Check(gameOver.IsRestarting && !menu.pauseInput.interactable, "Quick Restart locks navigation against competing scene loads");
            yield return WaitForReload(before);
            player = FindAnyObjectByType<PlayerStats>(); menu = FindAnyObjectByType<RunMenu>(); gameOver = FindAnyObjectByType<GameOverController>();
            Check(SceneManager.GetActiveScene().path == expected && loads == before + 1 && !RunMenu.IsPaused && Time.timeScale == 1 && player.PendingUpgrades == 0 && player.DamageMultiplier == 1 &&
                player.characterData == character.GetComponent<PlayerStats>().characterData && player.GetComponentInChildren<WeaponHolder>().CurrentWeapon.weaponData == weapon.GetComponent<WeaponController>().weaponData,
                "Pause Quick Restart retains map/character/weapon and resets upgrades, menus and clocks");
            hud = FindAnyObjectByType<PlayerHud>(); hud.SendMessage("LateUpdate");
            Check(Read<Text>(hud, "timeText").text == "00:00", "Pause Quick Restart resets the HUD timer");
            menu.OpenPause(); player.Kill();
            Check(gameOver.IsGameOver && !menu.IsOpen && !RunMenu.IsPaused && Time.timeScale == 1, "Death during pause dismisses pause ownership without resurrecting the run");
            spawner = FindAnyObjectByType<EnemySpawner>(); clock = spawner.ElapsedTime; spawner.Tick(10); hud.SendMessage("LateUpdate");
            Check(spawner.ElapsedTime == clock, "Death freezes the survival clock even though idle animations keep playing");
            Button retry = Read<Button>(gameOver, "restartButton"), fate = Read<Button>(gameOver, "anotherFateButton"), settings = Read<Button>(gameOver, "settingsButton"), main = Read<Button>(gameOver, "mainMenuButton");
            GameObject death = Read<GameObject>(gameOver, "panel");
            yield return new WaitForEndOfFrame(); Verify(death, retry, fate, settings, main); Capture("death-" + size + ".png");
            Click(settings); Check(menu.settingsPanel.activeSelf && GameOverController.GameplayStopped, "Death menu Settings does not resume gameplay");
            yield return Escape(); Check(!menu.settingsPanel.activeSelf && gameOver.IsGameOver && EventSystem.current.currentSelectedGameObject == settings.gameObject, "Settings returns focus to the death menu");
            before = loads; Click(retry); yield return WaitForReload(before);
            player = FindAnyObjectByType<PlayerStats>(); gameOver = FindAnyObjectByType<GameOverController>();
            Check(SceneManager.GetActiveScene().path == expected && !player.IsDead && !GameOverController.GameplayStopped && LoadoutLauncher.CharacterPrefab == character && LoadoutLauncher.Weapon == weapon,
                "Death Quick Restart preserves all three choices and clears Game Over");
            hud = FindAnyObjectByType<PlayerHud>(); hud.SendMessage("LateUpdate");
            Check(Read<Text>(hud, "timeText").text == "00:00", "Death Quick Restart resets the HUD timer");
            player.Kill(); Click(Read<Button>(gameOver, "anotherFateButton")); yield return WaitForScene(LoadoutLauncher.SelectionScene);
            selection = FindAnyObjectByType<LoadoutSelection>(); launcher = selection.GetComponent<LoadoutLauncher>();
            Check(!selection.ChoosingWeapon && !selection.Confirmed && !launcher.ChoosingMap && LoadoutLauncher.Character == null && LoadoutLauncher.Weapon == null, "Another Fate returns to fresh character selection, not weapons or map");
            Click(selection.characterView.cards[0]); Click(selection.weaponView.cards[0]); Click(snow ? launcher.snowMapButton : launcher.grassMapButton);
            yield return WaitForScene(expected); menu = FindAnyObjectByType<RunMenu>();
            menu.OpenPause(); Click(menu.mainMenuButton); yield return WaitForScene(MainMenu.ScenePath);
            Check(SceneManager.GetActiveScene().path == MainMenu.ScenePath && Time.timeScale == 1 && !RunMenu.IsPaused && !GameOverController.GameplayStopped, "Pause Main Menu unloads gameplay and clears all pause/input locks");
        }
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene);
    }

    private IEnumerator Escape()
    {
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); yield return null;
        InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
    }
    private IEnumerator WaitForScene(string path)
    {
        float deadline = Time.realtimeSinceStartup + 20;
        while (SceneManager.GetActiveScene().path != path && Time.realtimeSinceStartup < deadline) yield return null;
        Check(SceneManager.GetActiveScene().path == path, "Scene navigation reached " + path); yield return null;
    }
    private IEnumerator WaitForReload(int before)
    {
        float deadline = Time.realtimeSinceStartup + 20;
        while (loads == before && Time.realtimeSinceStartup < deadline) yield return null;
        Check(loads == before + 1, "Quick Restart reloads exactly once"); yield return null;
    }
    private void Verify(GameObject root, params Button[] buttons)
    {
        Canvas.ForceUpdateCanvases();
        foreach (Button button in buttons)
        {
            RectTransform rect = button.GetComponent<RectTransform>(); var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) }, hits);
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == button, "Unblocked menu raycast: " + button.name);
            var corners = new Vector3[4]; rect.GetWorldCorners(corners);
            foreach (Vector3 corner in corners) Check(corner.x >= 0 && corner.x <= Screen.width && corner.y >= 0 && corner.y <= Screen.height, "Menu button fits viewport: " + button.name);
        }
        foreach (Text label in root.GetComponentsInChildren<Text>()) Check(label.preferredHeight <= label.rectTransform.rect.height + 1, "Menu text fits: " + label.name);
    }
    private void SetResolution(int width, int height)
    {
        Assembly assembly = typeof(Editor).Assembly; Type type = assembly.GetType("UnityEditor.GameViewSizes");
        object sizesObject = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        object group = type.GetProperty("currentGroup", Flags).GetValue(sizesObject);
        EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView")); PropertyInfo selected = window.GetType().GetProperty("selectedSizeIndex", Flags);
        if (originalSize < 0) originalSize = (int)selected.GetValue(window);
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        object size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new object[] { Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), width, height, "Run flow temporary" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size }); sizes.Add(index); selected.SetValue(window, index); window.Show(); window.Focus();
    }
    private static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Flags).GetValue(target);
    private static void Click(Button button) => ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    private void Loaded(Scene scene, LoadSceneMode mode) => loads++;
    private void Check(bool pass, string text) { if (!pass) throw new Exception(text); checks.Add(text); }
    private static void Capture(string name)
    {
        Directory.CreateDirectory("Captures/RunFlow"); Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes("Captures/RunFlow/" + name, image.EncodeToPNG()); } finally { Destroy(image); }
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= Loaded; AudioListener.volume = originalVolume; Application.runInBackground = background; Time.timeScale = 1;
        if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
        if (originalSize < 0) return;
        Assembly assembly = typeof(Editor).Assembly; EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView")); window.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(window, originalSize);
        Type type = assembly.GetType("UnityEditor.GameViewSizes"); object instance = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null); object group = type.GetProperty("currentGroup", Flags).GetValue(instance);
        for (int i = sizes.Count - 1; i >= 0; i--) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { sizes[i] });
    }
    [Serializable] private class Report { public string result, failure; public string[] checks; }
}
#endif
