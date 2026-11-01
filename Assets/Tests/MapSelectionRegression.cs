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

public class MapSelectionRegression : MonoBehaviour
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private bool background;
    private int gameplayLoads;

    [MenuItem("Tools/System Leveling/Run Map Selection Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<MapSelectionRegression>() != null) return;
        GameObject root = new GameObject("Map selection regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root);
        root.AddComponent<MapSelectionRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground; Application.runInBackground = true; Time.timeScale = 1;
        SceneManager.sceneLoaded += Loaded;
        IEnumerator tests = Tests(); string failure = "";
        while (true)
        {
            bool more = false; object wait = null;
            try { more = tests.MoveNext(); if (more) wait = tests.Current; }
            catch (Exception error) { failure = error.ToString(); }
            if (!more || failure.Length > 0) break;
            yield return wait;
        }
        Directory.CreateDirectory("Captures/MapSelection");
        File.WriteAllText("Captures/MapSelection/runtime.json", JsonUtility.ToJson(new Report { result = failure.Length == 0 ? "PASS" : "FAIL", checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("MAP SELECTION REGRESSION PASS: " + checks.Count);
        else Debug.LogError("MAP SELECTION REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        foreach (bool snow in new[] { false, true })
        {
            yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene);
            LoadoutSelection ui = FindAnyObjectByType<LoadoutSelection>(); LoadoutLauncher launcher = ui.GetComponent<LoadoutLauncher>();
            Check(!launcher.SnowSelected && launcher.SelectedGameplayScene == LoadoutLauncher.GameplayScene && LoadoutLauncher.Character == null, "New selection defaults to grass and clears old loadout");
            Check(!launcher.mapPage.activeSelf && !launcher.grassMapButton.gameObject.activeInHierarchy, "Map choice is not visible before weapon selection");
            int character = snow ? ui.characterPrefabs.Length - 1 : 0;
            int weapon = snow ? ui.weaponPrefabs.Length - 1 : 0;
            GameObject chosenCharacter = ui.characterPrefabs[character], chosenWeapon = ui.weaponPrefabs[weapon];
            Click(ui.characterView.cards[character]); ui.Back();
            Check(!ui.ChoosingWeapon, "Back from weapons returns to character selection");
            Click(ui.characterView.cards[character]);
            int before = gameplayLoads; string expectedScene = snow ? LoadoutLauncher.SnowScene : LoadoutLauncher.GameplayScene;
            Click(ui.weaponView.cards[weapon]);
            Check(launcher.ChoosingMap && !launcher.IsLoading && !launcher.loadoutPage.activeSelf && launcher.mapPage.activeSelf, "Weapon choice opens the dedicated map screen without launching");
            Click(launcher.mapBackButton); Check(!launcher.ChoosingMap && ui.ChoosingWeapon && !ui.Confirmed, "Map Back returns to weapons and permits selecting the same weapon again");
            Click(ui.weaponView.cards[weapon]);
            yield return new WaitForEndOfFrame(); Capture(snow ? "selection-snow.png" : "selection-grass.png");
            Click(snow ? launcher.snowMapButton : launcher.grassMapButton); launcher.Launch();
            launcher.SelectGrassMap(); launcher.SelectSnowMap();
            Check(launcher.IsLoading && launcher.SelectedGameplayScene == expectedScene && !ui.GetComponent<CanvasGroup>().interactable, "Launch locks map/loadout input and ignores duplicate launch");
            float deadline = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != expectedScene && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            Check(SceneManager.GetActiveScene().path == expectedScene && gameplayLoads == before + 1, "Selected map scene loads exactly once: " + expectedScene);
            PlayerStats player = FindAnyObjectByType<PlayerStats>();
            Check(FindObjectsByType<PlayerStats>().Length == 1 && player.characterData == chosenCharacter.GetComponent<PlayerStats>().characterData &&
                player.GetComponentInChildren<WeaponHolder>().CurrentWeapon.weaponData == chosenWeapon.GetComponent<WeaponController>().weaponData, "Character and weapon selection apply in the chosen map");
            EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>(); spawner.enabled = false;
            HordeSpawnSettings horde = Get<HordeSpawnSettings>(spawner, "horde");
            horde.activationInterval = horde.checkInterval = 0.1f; horde.activationChance = horde.initialChance = 100;
            horde.minCount = horde.maxCount = horde.populationCap = 3; horde.spawnsPerFrame = 3;
            spawner.SendMessage("Awake"); spawner.Tick(2.6f); spawner.Tick(0.11f);
            Check(spawner.TotalNormalSpawned > 0 && spawner.TotalHordeSpawned == 3, "Actual scene spawner deploys normal enemies and forced Horde");
            var allowed = spawner.CurrentPhase.enemies.Concat(horde.enemies).Where(entry => entry != null && entry.IsUsable).Select(entry => entry.prefab.name + "(Clone)").ToArray();
            foreach (EnemyStats enemy in FindObjectsByType<EnemyStats>())
                Check(allowed.Contains(enemy.name), "Live normal/Horde enemy belongs only to chosen map: " + enemy.name);
            Check(FindObjectsByType<RotwoodEncounter>().All(encounter => encounter.enabled == !snow), "Grass boss is not enabled in the snow map");
            yield return new WaitForEndOfFrame(); Capture(snow ? "snow-start.png" : "grass-start.png");

            MapController map = FindAnyObjectByType<MapController>(); map.enabled = false;
            Camera camera = new GameObject("Streaming test camera").AddComponent<Camera>(); camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 6;
            map.referenceCamera = camera;
            foreach (float y in new[] { 0f, 80f, -80f, 0f })
            {
                camera.transform.position = new Vector3(0, y, -10); map.GetWorldRectFromViewport();
                Call(map, "HandleChunkCulling"); Call(map, "HandleChunkSpawning", Vector2.zero, true);
                yield return null;
                PropRandomizer[] chunks = FindObjectsByType<PropRandomizer>(FindObjectsInactive.Include);
                Check(chunks.All(chunk => map.terrainChunks.Any(prefab => chunk.name == prefab.name || chunk.name == prefab.name + "(Clone)")), "Map remains one terrain type at Y=" + y);
                var streamed = map.GetComponentsInChildren<PropRandomizer>(true);
                Check(streamed.Select(chunk => chunk.transform.position).Distinct().Count() == streamed.Length, "No duplicate chunks while travelling in " + expectedScene);
                foreach (PropRandomizer chunk in chunks.Where(chunk => chunk.gameObject.activeInHierarchy))
                    foreach (GameObject point in chunk.propSpawnPoints)
                        Check(point.transform.childCount == 1 && chunk.propPrefabs.Any(prefab => point.transform.GetChild(0).name == prefab.name + "(Clone)"), "Prop comes from chosen map and spawns once");
            }
            player.Kill(); GameOverController gameOver = FindAnyObjectByType<GameOverController>(); before = gameplayLoads;
            Check(gameOver.IsGameOver, "Game Over works in " + expectedScene);
            gameOver.Restart(); gameOver.Restart();
            deadline = Time.realtimeSinceStartup + 20;
            while (gameplayLoads == before && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null;
            PlayerStats restarted = FindAnyObjectByType<PlayerStats>();
            Check(SceneManager.GetActiveScene().path == expectedScene && gameplayLoads == before + 1 && restarted != player && !restarted.IsDead && !GameOverController.GameplayStopped &&
                restarted.characterData == chosenCharacter.GetComponent<PlayerStats>().characterData && restarted.GetComponentInChildren<WeaponHolder>().CurrentWeapon.weaponData == chosenWeapon.GetComponent<WeaponController>().weaponData,
                "Restart keeps selected map/character/weapon and resets the run");
            restarted.Kill(); FindAnyObjectByType<GameOverController>().ReturnToMainMenu();
            deadline = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != MainMenu.ScenePath && Time.realtimeSinceStartup < deadline) yield return null;
            Check(SceneManager.GetActiveScene().path == MainMenu.ScenePath, "Both maps return to Main Menu");
        }
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene);
        Check(!FindAnyObjectByType<LoadoutLauncher>().SnowSelected, "A new run does not inherit the previous snow choice");
    }

    private void Loaded(Scene scene, LoadSceneMode mode) { if (scene.path == LoadoutLauncher.GameplayScene || scene.path == LoadoutLauncher.SnowScene) gameplayLoads++; }
    private static T Get<T>(object target, string field) => (T)target.GetType().GetField(field, Fields).GetValue(target);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Fields).Invoke(target, args);
    private static void Click(Button button) => ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    private void Check(bool pass, string message) { if (!pass) throw new Exception(message); checks.Add(message); }
    private static void Capture(string name)
    {
        Directory.CreateDirectory("Captures/MapSelection"); Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes("Captures/MapSelection/" + name, image.EncodeToPNG()); } finally { Destroy(image); }
    }
    private void OnDestroy() { SceneManager.sceneLoaded -= Loaded; Application.runInBackground = background; Time.timeScale = 1; }
    [Serializable] private class Report { public string result, failure; public string[] checks; }
}
#endif
