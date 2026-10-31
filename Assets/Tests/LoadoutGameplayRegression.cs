#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadoutGameplayRegression : MonoBehaviour
{
    private const string Output = "Captures/LoadoutGameplay";
    private readonly List<string> checks = new List<string>();
    private bool background;
    private int gameplayLoads;
    private int pairings;
    private Keyboard keyboard;

    [MenuItem("Tools/System Leveling/Test Loadout Gameplay (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<LoadoutGameplayRegression>() != null) return;
        GameObject root = new GameObject("Loadout Gameplay Regression");
        DontDestroyOnLoad(root); root.AddComponent<LoadoutGameplayRegression>();
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
    }

    private IEnumerator Start()
    {
        background = Application.runInBackground; Application.runInBackground = true;
        keyboard = InputSystem.AddDevice<Keyboard>();
        SceneManager.sceneLoaded += Loaded;
        IEnumerator run = Tests(); string failure = "";
        while (true)
        {
            object current;
            try { if (!run.MoveNext()) break; current = run.Current; }
            catch (Exception error) { failure = error.ToString(); break; }
            yield return current;
        }
        Cleanup();
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/regression.json", JsonUtility.ToJson(new Report
        { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log($"LOADOUT GAMEPLAY REGRESSION PASS: {checks.Count} checks, {pairings} prefab pairings and restart.");
        else Debug.LogError("LOADOUT GAMEPLAY REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        string before = Snapshot();
        Check(EditorBuildSettings.scenes[0].enabled && EditorBuildSettings.scenes[0].path == LoadoutLauncher.SelectionScene,
            "Selection is the enabled build entry scene");
        Check(File.ReadAllText("ProjectSettings/EditorBuildSettings.asset").Contains(LoadoutLauncher.SelectionScene),
            "Selection scene is persisted in project Build Settings");
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene);
        Check(LoadoutLauncher.Character == null && LoadoutLauncher.Weapon == null, "Selection clears the previous run");
        LoadoutSelection catalog = FindAnyObjectByType<LoadoutSelection>();
        int characterCount = catalog.characterPrefabs.Length, weaponCount = catalog.weaponPrefabs.Length;
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.GameplayScene);
        yield return null;
        PlayerStats baseline = FindAnyObjectByType<PlayerStats>();
        CharacterScriptableObject mage = baseline.characterData;
        WeaponHolder baselineHand = baseline.GetComponentInChildren<WeaponHolder>();
        Check(baselineHand.CurrentWeapon.weaponData == mage.StartingWeapon.GetComponent<WeaponController>().weaponData, "Direct gameplay still uses its authored default");

        for (int c = 0; c < characterCount; c++)
        for (int w = 0; w < weaponCount; w++)
        {
            yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene);
            LoadoutSelection ui = FindAnyObjectByType<LoadoutSelection>();
            GameObject character = ui.characterPrefabs[c];
            CharacterScriptableObject characterData = character.GetComponent<PlayerStats>().characterData;
            WeaponScriptableObject weaponData = ui.weaponPrefabs[w].GetComponent<WeaponController>().weaponData;
            int loadsBefore = gameplayLoads;
            Click(ui.characterView.cards[c]);
            Check(ui.ChoosingWeapon && ui.CharacterIndex == c && !ui.GetComponent<LoadoutLauncher>().IsLoading, "Character click selects without launching");
            Click(ui.weaponView.cards[w]);
            Check(ui.GetComponent<LoadoutLauncher>().IsLoading && !ui.GetComponent<CanvasGroup>().interactable, "Weapon click launches and locks further input");
            ui.GetComponent<LoadoutLauncher>().Launch();
            float timeout = Time.realtimeSinceStartup + 20;
            while (SceneManager.GetActiveScene().path != LoadoutLauncher.GameplayScene && Time.realtimeSinceStartup < timeout) yield return null;
            Check(SceneManager.GetActiveScene().path == LoadoutLauncher.GameplayScene, "Gameplay loaded");
            yield return null;
            Check(gameplayLoads == loadsBefore + 1, "Repeated launch does not load twice");

            PlayerStats player = FindAnyObjectByType<PlayerStats>();
            FindAnyObjectByType<EnemySpawner>().enabled = false;
            WeaponHolder holder = player.GetComponentInChildren<WeaponHolder>();
            Check(FindObjectsByType<PlayerStats>().Length == 1 && PlayerController.Instance == player.GetComponent<PlayerController>(), "Exactly one player owns the singleton");
            Check(player.characterData == characterData && player.currentHealth == characterData.MaxHealth && player.MaxHealth == characterData.MaxHealth &&
                player.currentMoveSpeed == characterData.MoveSpeed && player.currentMagnet == characterData.Magnet && player.currentRecovery == characterData.Recovery,
                characterData.name + "/" + weaponData.name + ": chosen stats initialized before gameplay");
            Check(holder.CurrentWeapon.weaponData == weaponData && holder.GetComponentsInChildren<WeaponController>(true).Length == 1, "Explicit weapon survives default-equipment Awake");
            Check(player.GetComponent<Animator>().runtimeAnimatorController == character.GetComponent<Animator>().runtimeAnimatorController, "Correct character animation controller");
            CapsuleCollider2D source = character.GetComponent<CapsuleCollider2D>();
            CapsuleCollider2D body = player.GetComponent<CapsuleCollider2D>();
            Check(body.size == source.size && body.offset == source.offset &&
                holder.transform.localPosition == character.GetComponentInChildren<WeaponHolder>().transform.localPosition, "Body and hand calibration preserved");
            Check(FindAnyObjectByType<CinemachineCamera>().Follow == player.transform &&
                Read<PlayerStats>(FindAnyObjectByType<PlayerHud>(), "player") == player &&
                Read<PlayerStats>(FindAnyObjectByType<WeaponHud>(), "player") == player &&
                Read<Transform>(FindAnyObjectByType<EnemySpawner>(), "player") == player.transform, "Camera, both HUDs and spawner still target the same player");
            PlayerHud hud = FindAnyObjectByType<PlayerHud>(); hud.Refresh();
            Check(Read<Text>(hud, "healthText").text == $"{Mathf.CeilToInt(player.MaxHealth)} / {Mathf.CeilToInt(player.MaxHealth)}", "Health HUD uses the selected character maximum");
            Vector3 origin = player.transform.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return null;
            yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
            Check(player.transform.position.x > origin.x + 0.001f, "Real keyboard movement works after selection");
            holder.PointerPosition = (Vector2)player.transform.position + Vector2.right * 10;
            RangedWeaponController ranged = holder.CurrentWeapon as RangedWeaponController;
            if (ranged != null)
            {
                ranged.Tick(10, false); ranged.Tick(0, true); ranged.Tick(0.2f, true); ranged.Tick(0, false);
                Check(ranged.LastProjectile != null && ranged.LastProjectile.weaponData == weaponData, "Selected ranged weapon fires its projectile");
                WeaponHud ammo = FindAnyObjectByType<WeaponHud>(); ammo.SendMessage("Update");
                Check(Read<Text>(ammo, "label").text.Contains(weaponData.name.ToUpperInvariant()), "Weapon HUD follows selected ammo state");
            }
            else
            {
                holder.CurrentWeapon.SendMessage("Attack");
                Check(FindAnyObjectByType<MeleeWeaponBehaviour>() != null, "Selected sword creates a slash");
            }
            pairings++;
            if (w == weaponCount - 1)
            {
                yield return new WaitForEndOfFrame();
                Capture(characterData.name.ToLowerInvariant() + "-" + weaponData.name.ToLowerInvariant() + ".png");
            }
            if (c == characterCount - 1 && w == weaponCount - 1)
            {
                player.IncreaseExperience(player.experienceCap - player.experience);
                Check(player.TryApplyUpgrade(UpgradeKind.Damage) && player.DamageMultiplier > 1, "Existing level-up upgrades still apply");
                player.Kill();
                GameOverController gameOver = FindAnyObjectByType<GameOverController>();
                Check(gameOver.IsGameOver, "Existing game-over flow still runs");
                gameOver.Restart();
                timeout = Time.realtimeSinceStartup + 20;
                while (gameplayLoads == loadsBefore + 1 && Time.realtimeSinceStartup < timeout) yield return null;
                yield return null;
                PlayerStats restarted = FindAnyObjectByType<PlayerStats>();
                Check(restarted != player && !restarted.IsDead && restarted.characterData == characterData && restarted.currentHealth == characterData.MaxHealth &&
                    restarted.GetComponentInChildren<WeaponHolder>().CurrentWeapon.weaponData == weaponData && restarted.DamageMultiplier == 1 && !GameOverController.GameplayStopped,
                    "Restart retains character/weapon and resets health, upgrades and game-over state");
            }
        }
        Check(Snapshot() == before, "All character/weapon source assets remain byte-identical");
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene);
        Check(LoadoutLauncher.Character == null && LoadoutLauncher.Weapon == null, "Returning to selection clears stale run state");
    }

    private void Loaded(Scene scene, LoadSceneMode mode) { if (scene.path == LoadoutLauncher.GameplayScene) gameplayLoads++; }
    private void Check(bool condition, string message) { if (!condition) throw new Exception(message); checks.Add(message); }
    private static T Read<T>(object target, string name) => (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    private static void Click(Button button) => ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
    private static string Snapshot() => string.Join("\n", Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories).OrderBy(p => p).Select(File.ReadAllText));
    private static void Capture(string name)
    {
        Directory.CreateDirectory(Output);
        Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
        try { File.WriteAllBytes(Output + "/" + name, texture.EncodeToPNG()); }
        finally { Destroy(texture); }
    }
    private void Cleanup()
    {
        SceneManager.sceneLoaded -= Loaded;
        if (keyboard != null) { InputSystem.RemoveDevice(keyboard); keyboard = null; }
        Application.runInBackground = background;
    }
    private void OnDestroy() => Cleanup();
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
