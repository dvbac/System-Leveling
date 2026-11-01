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

public class MainMenuRegression : MonoBehaviour
{
    private const string Output = "Captures/MainMenu";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private readonly List<int> temporarySizes = new List<int>();
    private int originalSize = -1, selectionLoads;
    private bool background, cleaned;

    [MenuItem("Tools/System Leveling/Test Main Menu Flow (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<MainMenuRegression>() != null) return;
        GameObject root = new GameObject("Main Menu Regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root); root.AddComponent<MainMenuRegression>();
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
    }

    private IEnumerator Start()
    {
        background = Application.runInBackground; Application.runInBackground = true;
        SceneManager.sceneLoaded += Loaded;
        Directory.CreateDirectory(Output);
        IEnumerator run = Tests(); string failure = "";
        while (true)
        {
            object current;
            try { if (!run.MoveNext()) break; current = run.Current; }
            catch (Exception error) { failure = error.ToString(); break; }
            yield return current;
        }
        Cleanup();
        File.WriteAllText(Output + "/regression.json", JsonUtility.ToJson(new Report
        { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("MAIN MENU REGRESSION PASS: " + checks.Count + " checks; full menu/selection/gameplay/game-over flow at 16:9 and 4:3.");
        else Debug.LogError("MAIN MENU REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        Check(EditorBuildSettings.scenes[0].enabled && EditorBuildSettings.scenes[0].path == MainMenu.ScenePath, "Main Menu is the enabled build entry scene");
        Check(File.ReadAllText("ProjectSettings/EditorBuildSettings.asset").Contains(MainMenu.ScenePath), "Main Menu build entry is persisted on disk");
        foreach (Vector2Int size in new[] { new Vector2Int(1920, 1080), new Vector2Int(1024, 768) })
        {
            yield return SceneManager.LoadSceneAsync(MainMenu.ScenePath);
            SetResolution(size.x, size.y);
            yield return null; yield return null; yield return null;
            yield return new WaitForEndOfFrame();
            MainMenu menu = FindAnyObjectByType<MainMenu>();
            Check(menu != null && FindAnyObjectByType<PlayerStats>() == null && Time.timeScale == 1, size + ": menu contains no running gameplay player");
            Check(EventSystem.current.currentSelectedGameObject == menu.startButton.gameObject, "Start is focused for keyboard navigation");
            Verify(menu.startButton, menu.controlsButton, menu.quitButton);
            foreach (Text label in menu.GetComponentsInChildren<Text>()) Check(label.preferredHeight <= label.rectTransform.rect.height + 1, "Menu text fits: " + label.name);
            Capture("main-menu-" + size.x + "x" + size.y + ".png");
            Click(menu.controlsButton);
            Check(menu.controlsPanel.activeSelf && !menu.buttons.interactable, "Controls opens and disables underlying menu actions");
            menu.StartRun(); Check(!menu.IsLoading, "Start cannot bypass the Controls modal");
            yield return null; yield return new WaitForEndOfFrame();
            Verify(menu.closeControlsButton);
            Check(TopHit(menu.startButton) != menu.startButton.gameObject, "Controls overlay blocks pointer clicks through to Start");
            foreach (Text label in menu.controlsPanel.GetComponentsInChildren<Text>()) Check(label.preferredHeight <= label.rectTransform.rect.height + 1, "Controls text fits: " + label.name);
            Capture("controls-" + size.x + "x" + size.y + ".png");
            Click(menu.closeControlsButton);
            Check(!menu.controlsPanel.activeSelf && menu.buttons.interactable && EventSystem.current.currentSelectedGameObject == menu.controlsButton.gameObject, "Controls Back restores menu input and focus");
            Click(menu.quitButton);
            Check(menu.status.text.Contains("STANDALONE"), "Quit is wired and safely explains Editor behavior");

            int before = selectionLoads;
            Click(menu.startButton); menu.StartRun();
            Check(menu.IsLoading && !menu.GetComponent<CanvasGroup>().interactable, "Start locks input and rejects a repeated request");
            float until = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != LoadoutLauncher.SelectionScene && Time.realtimeSinceStartup < until) yield return null;
            Check(SceneManager.GetActiveScene().path == LoadoutLauncher.SelectionScene && selectionLoads == before + 1, "Start loads selection exactly once");
            yield return null;
            LoadoutSelection selection = FindAnyObjectByType<LoadoutSelection>();
            Check(!selection.ChoosingWeapon && selection.back.gameObject.activeInHierarchy, "Character selection exposes Back to Main Menu");
            Verify(selection.back);
            Click(selection.back);
            until = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != MainMenu.ScenePath && Time.realtimeSinceStartup < until) yield return null;
            Check(SceneManager.GetActiveScene().path == MainMenu.ScenePath, "Character Back returns to Main Menu");
            yield return null;
            menu = FindAnyObjectByType<MainMenu>(); Click(menu.startButton);
            until = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != LoadoutLauncher.SelectionScene && Time.realtimeSinceStartup < until) yield return null;
            yield return null;
            selection = FindAnyObjectByType<LoadoutSelection>();
            Check(selection != null && LoadoutLauncher.Character == null && LoadoutLauncher.Weapon == null, "New selection clears the previous loadout");
            Click(selection.characterView.cards[0]); Click(selection.back);
            Check(!selection.ChoosingWeapon && SceneManager.GetActiveScene().path == LoadoutLauncher.SelectionScene, "Weapon Back stays in selection and returns to characters");
            int character = selection.characterPrefabs.Length - 1;
            int weapon = Mathf.Min(1, selection.weaponPrefabs.Length - 1);
            CharacterScriptableObject expectedCharacter = selection.characterPrefabs[character].GetComponent<PlayerStats>().characterData;
            WeaponScriptableObject expectedWeapon = selection.weaponPrefabs[weapon].GetComponent<WeaponController>().weaponData;
            Click(selection.characterView.cards[character]); Click(selection.weaponView.cards[weapon]);
            Check(selection.GetComponent<LoadoutLauncher>().ChoosingMap, "Weapon selection opens a separate map screen");
            Click(selection.GetComponent<LoadoutLauncher>().grassMapButton);
            until = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != LoadoutLauncher.GameplayScene && Time.realtimeSinceStartup < until) yield return null;
            Check(SceneManager.GetActiveScene().path == LoadoutLauncher.GameplayScene, "Selecting a map enters gameplay");
            yield return null;
            PlayerStats player = FindAnyObjectByType<PlayerStats>();
            FindAnyObjectByType<EnemySpawner>().enabled = false;
            Check(player.characterData == expectedCharacter && player.GetComponentInChildren<WeaponHolder>().CurrentWeapon.weaponData == expectedWeapon, "Gameplay receives the chosen character and weapon");
            player.Kill();
            GameOverController gameOver = FindAnyObjectByType<GameOverController>();
            Button menuButton = (Button)typeof(GameOverController).GetField("mainMenuButton", Flags).GetValue(gameOver);
            Button restart = (Button)typeof(GameOverController).GetField("restartButton", Flags).GetValue(gameOver);
            Check(gameOver.IsGameOver && menuButton != null && restart != null, "Game Over offers Main Menu and the existing Restart");
            yield return null; yield return new WaitForEndOfFrame();
            Verify(menuButton, restart);
            Capture("game-over-" + size.x + "x" + size.y + ".png");
            Click(menuButton); gameOver.Restart();
            Check(gameOver.IsRestarting && !restart.interactable && !menuButton.interactable, "Game Over navigation locks both buttons against competing loads");
            until = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != MainMenu.ScenePath && Time.realtimeSinceStartup < until) yield return null;
            yield return null;
            Check(SceneManager.GetActiveScene().path == MainMenu.ScenePath && FindAnyObjectByType<PlayerStats>() == null && !GameOverController.GameplayStopped && Time.timeScale == 1,
                "Game Over Main Menu removes gameplay and clears its input lock");
        }
    }

    private void Check(bool condition, string text) { if (!condition) throw new Exception(text); checks.Add(text); }
    private void Loaded(Scene scene, LoadSceneMode mode) { if (scene.path == LoadoutLauncher.SelectionScene) selectionLoads++; }
    private static void Click(Button button) => ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    private static GameObject TopHit(Button button)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        var pointer = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
        var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits); return hits.Count > 0 ? hits[0].gameObject : null;
    }
    private static void Verify(params Button[] buttons)
    {
        Canvas.ForceUpdateCanvases();
        foreach (Button button in buttons)
        {
            if (TopHit(button) != button.gameObject) throw new Exception("Button blocked: " + button.name);
            var corners = new Vector3[4]; button.GetComponent<RectTransform>().GetWorldCorners(corners);
            foreach (Vector3 corner in corners)
                if (corner.x < 0 || corner.x > Screen.width || corner.y < 0 || corner.y > Screen.height) throw new Exception("Button outside viewport: " + button.name);
        }
    }
    private void SetResolution(int width, int height)
    {
        Assembly assembly = typeof(Editor).Assembly; Type type = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        object group = type.GetProperty("currentGroup", Flags).GetValue(sizes);
        EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        PropertyInfo property = window.GetType().GetProperty("selectedSizeIndex", Flags);
        if (originalSize < 0) originalSize = (int)property.GetValue(window);
        int index = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null);
        object size = Activator.CreateInstance(assembly.GetType("UnityEditor.GameViewSize"), new object[]
            { Enum.Parse(assembly.GetType("UnityEditor.GameViewSizeType"), "FixedResolution"), width, height, "Main Menu temporary" });
        group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size }); temporarySizes.Add(index); property.SetValue(window, index);
        window.Show(); window.Focus(); window.Repaint();
    }
    private static void Capture(string name)
    {
        Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Output + "/" + name, image.EncodeToPNG()); } finally { Destroy(image); }
    }
    private void Cleanup()
    {
        if (cleaned) return; cleaned = true;
        SceneManager.sceneLoaded -= Loaded; Application.runInBackground = background;
        if (originalSize < 0) return;
        Assembly assembly = typeof(Editor).Assembly; EditorWindow window = EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView"));
        window.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(window, originalSize);
        Type type = assembly.GetType("UnityEditor.GameViewSizes");
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(type).GetProperty("instance").GetValue(null);
        object group = type.GetProperty("currentGroup", Flags).GetValue(sizes);
        for (int i = temporarySizes.Count - 1; i >= 0; i--) group.GetType().GetMethod("RemoveCustomSize").Invoke(group, new object[] { temporarySizes[i] });
    }
    private void OnDestroy() => Cleanup();
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
