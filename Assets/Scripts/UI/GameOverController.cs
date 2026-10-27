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

    private static GameOverController active;
    public static bool GameplayStopped => active != null && active.IsGameOver;
    public bool IsGameOver { get; private set; }
    public bool IsRestarting { get; private set; }

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
        panel.SetActive(IsGameOver);
        if (player.IsDead)
            EndRun();
    }

    private void OnDisable()
    {
        if (player != null) player.Died -= EndRun;
        if (restartButton != null) restartButton.onClick.RemoveListener(Restart);
        // Disabling a view must not resurrect a finished run.
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }

    private void EndRun()
    {
        if (IsGameOver)
            return;
        IsGameOver = true;
        if (levelUp != null) levelUp.DismissForGameOver();
        // Gameplay is gated independently so living enemies can continue their Idle clips.
        Time.timeScale = 1f;
        foreach (Enemy enemy in FindObjectsByType<Enemy>())
            enemy.HoldForGameOver();
        if (player.TryGetComponent(out PlayerController controller))
            controller.HoldForGameOver();
        resultText.text = $"LEVEL REACHED  {player.level:00}\n\nEvery defeat is a new beginning.";
        restartButton.interactable = true;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(restartButton.gameObject);
    }

    public void Restart()
    {
        if (!IsGameOver || IsRestarting)
            return;
        Scene scene = gameObject.scene;
        if (string.IsNullOrEmpty(scene.path) || !Application.CanStreamedLevelBeLoaded(scene.path))
        {
            resultText.text = "Scene unavailable. Add this scene to Build Settings and retry.";
            return;
        }
        IsRestarting = true;
        restartButton.interactable = false;
        resultText.text = "STARTING A NEW RUN...";
        if (levelUp != null) levelUp.DismissForGameOver();
        Time.timeScale = 1f;
        try
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync(scene.path, LoadSceneMode.Single);
            if (loading == null) throw new InvalidOperationException("Scene load did not start.");
        }
        catch (Exception ex)
        {
            IsRestarting = false;
            restartButton.interactable = true;
            resultText.text = "RESTART FAILED. PLEASE TRY AGAIN.";
            Debug.LogException(ex, this);
        }
    }
}
