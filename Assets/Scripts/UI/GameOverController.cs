using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverController : MonoBehaviour
{
    [SerializeField] private PlayerStats player;
    [SerializeField] private LevelUpSelection levelUp;
    [SerializeField] private GameObject panel;
    [SerializeField] private Text resultText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button anotherFateButton;
    [SerializeField] private Button settingsButton;

    private static GameOverController active;
    public static bool GameplayStopped => active != null && active.IsGameOver;
    public bool IsGameOver { get; private set; }
    public bool IsRestarting { get; private set; }
    public bool HasWon { get; private set; }
    public int SoulEarned { get; private set; }
    public float PlayedSeconds { get; private set; }

    internal static void RecordSoulReward(int amount)
    {
        if (active != null && !active.IsGameOver)
            active.SoulEarned = (int)Math.Min(int.MaxValue, (long)active.SoulEarned + amount);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => active = null;

    public void Configure(PlayerStats target, LevelUpSelection upgrades, GameObject modal, Text result, Button restart)
    {
        player = target;
        levelUp = upgrades;
        panel = modal;
        resultText = result;
        restartButton = restart;
    }

    private void OnEnable()
    {
        if (player == null || panel == null || resultText == null || restartButton == null)
        {
            Debug.LogError("GameOverController needs Player and UI references. Run Build HUD.", this);
            enabled = false;
            return;
        }
        active = this;
        player.Died += EndRun;
        restartButton.onClick.AddListener(Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        if (anotherFateButton != null) anotherFateButton.onClick.AddListener(AnotherFate);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        panel.SetActive(IsGameOver);
        if (player.IsDead)
            EndRun();
    }

    private void OnDisable()
    {
        if (player != null) player.Died -= EndRun;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        if (anotherFateButton != null) anotherFateButton.onClick.RemoveListener(AnotherFate);
        if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
        // Disabling a view must not resurrect a finished run.
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }

    private void EndRun()
    {
        Finish(false);
    }

    public void Win()
    {
        if (player == null || player.IsDead) return;
        Finish(true);
    }

    private void Finish(bool won)
    {
        if (IsGameOver)
            return;
        IsGameOver = true;
        HasWon = won;
        EnemySpawner clock = FindAnyObjectByType<EnemySpawner>();
        PlayedSeconds = clock != null ? clock.ElapsedTime : 0f;
        if (TryGetComponent(out RunMenu menu)) menu.DismissForGameOver();
        if (levelUp != null) levelUp.DismissForGameOver();
        // Gameplay is gated independently so living enemies can continue their Idle clips.
        Time.timeScale = 1f;
        foreach (Enemy enemy in FindObjectsByType<Enemy>())
            enemy.HoldForGameOver();
        if (player.TryGetComponent(out PlayerController controller))
            controller.HoldForGameOver();
        int seconds = Mathf.FloorToInt(PlayedSeconds);
        resultText.text = (won ? "YOU WIN / ROTWOOD DEFEATED\n" : "")
            + $"LEVEL REACHED  {player.level:00}\nTIME PLAYED  {seconds / 60:00}:{seconds % 60:00}\nSOUL EARNED  {SoulEarned:N0}";
        Transform heading = resultText.transform.parent.Find("Heading");
        if (heading != null && heading.TryGetComponent(out Text headingText))
            headingText.text = won ? "VICTORY" : "GAME OVER";
        Transform eyebrow = resultText.transform.parent.Find("Eyebrow");
        if (eyebrow != null && eyebrow.TryGetComponent(out Text eyebrowText))
            eyebrowText.text = won ? "SYSTEM // RUN COMPLETE" : "SYSTEM // RUN TERMINATED";
        SetNavigationEnabled(true);
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
    }

    public void Restart()
    {
        if ((!IsGameOver && !RunMenu.IsPaused) || IsRestarting)
            return;
        LoadScene(gameObject.scene.path, "STARTING A NEW RUN...");
    }

    public void ReturnToMainMenu()
    {
        if ((!IsGameOver && !RunMenu.IsPaused) || IsRestarting) return;
        LoadScene(MainMenu.ScenePath, "RETURNING TO MAIN MENU...");
    }

    public void AnotherFate()
    {
        if (!IsGameOver || IsRestarting) return;
        LoadScene(LoadoutLauncher.SelectionScene, "ANOTHER FATE AWAITS...");
    }

    private void OpenSettings() { if (IsGameOver && !IsRestarting && TryGetComponent(out RunMenu menu)) menu.OpenSettings(); }
    public void FocusSettings() { if (settingsButton != null) settingsButton.Select(); }

    private void SetNavigationEnabled(bool enabled)
    {
        foreach (Button button in new[] { restartButton, mainMenuButton, anotherFateButton, settingsButton })
            if (button != null) button.interactable = enabled;
    }

    private void LoadScene(string path, string message)
    {
        if (string.IsNullOrEmpty(path) || !Application.CanStreamedLevelBeLoaded(path))
        {
            resultText.text = "Scene unavailable. Add this scene to Build Settings and retry.";
            if (TryGetComponent(out RunMenu unavailableMenu)) unavailableMenu.status.text = resultText.text;
            return;
        }
        IsRestarting = true;
        SetNavigationEnabled(false);
        if (TryGetComponent(out RunMenu menu)) menu.SetLoading(true);
        resultText.text = message;
        if (levelUp != null) levelUp.DismissForGameOver();
        Time.timeScale = 1f;
        try
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            if (loading == null) throw new InvalidOperationException("Scene load did not start.");
        }
        catch (Exception ex)
        {
            IsRestarting = false;
            SetNavigationEnabled(true);
            if (TryGetComponent(out RunMenu failedMenu)) failedMenu.SetLoading(false);
            resultText.text = "SCENE LOAD FAILED. PLEASE TRY AGAIN.";
            if (TryGetComponent(out RunMenu errorMenu)) errorMenu.status.text = resultText.text;
            if (RunMenu.IsPaused) Time.timeScale = 0f;
            Debug.LogException(ex, this);
        }
    }
}
