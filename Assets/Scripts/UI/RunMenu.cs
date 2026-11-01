using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-200)]
public class RunMenu : MonoBehaviour
{
    public GameObject pausePanel, settingsPanel;
    public Button continueButton, restartButton, settingsButton, mainMenuButton, settingsBackButton;
    public Slider volume;
    public Toggle fullscreen;
    public Text volumeValue, status;
    public CanvasGroup pauseInput, settingsInput;
    private GameOverController gameOver;
    private LevelUpSelection levelUp;
    private static RunMenu active;
    private static int blockedThroughFrame = -1;
    private float previousTimeScale;
    private bool ownsPause;
    public bool IsOpen => ownsPause || settingsPanel.activeSelf;
    public static bool IsPaused => active != null && active.ownsPause;
    public static bool InputBlocked => (active != null && active.IsOpen) || Time.frameCount <= blockedThroughFrame;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { active = null; blockedThroughFrame = -1; }

    private void Awake()
    {
        active = this; gameOver = GetComponent<GameOverController>(); levelUp = GetComponent<LevelUpSelection>();
        pausePanel.SetActive(false); settingsPanel.SetActive(false);
        continueButton.onClick.AddListener(Continue);
        restartButton.onClick.AddListener(gameOver.Restart);
        settingsButton.onClick.AddListener(OpenSettings);
        mainMenuButton.onClick.AddListener(gameOver.ReturnToMainMenu);
        settingsBackButton.onClick.AddListener(CloseSettings);
        volume.onValueChanged.AddListener(SetVolume);
        fullscreen.onValueChanged.AddListener(SetFullscreen);
    }

    private void Update()
    {
        if (gameOver.IsRestarting || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (settingsPanel.activeSelf) CloseSettings();
        else if (ownsPause) Continue();
        else OpenPause();
    }

    private void OnEnable() => active = this;

    public void OpenPause()
    {
        if (IsOpen || gameOver.IsGameOver || gameOver.IsRestarting || (levelUp != null && levelUp.IsOpen) || Time.timeScale <= 0) return;
        previousTimeScale = Time.timeScale; ownsPause = true; Time.timeScale = 0;
        pausePanel.SetActive(true); pausePanel.transform.SetAsLastSibling(); continueButton.Select();
    }

    public void Continue()
    {
        if (!ownsPause || settingsPanel.activeSelf || gameOver.IsRestarting) return;
        ownsPause = false; pausePanel.SetActive(false); Time.timeScale = previousTimeScale;
        blockedThroughFrame = Time.frameCount + 1;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        if (levelUp != null) levelUp.Sync();
    }

    public void OpenSettings()
    {
        if (gameOver.IsRestarting || (!ownsPause && !gameOver.IsGameOver)) return;
        volume.SetValueWithoutNotify(AudioListener.volume); volumeValue.text = Mathf.RoundToInt(volume.value * 100) + "%";
        fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
        pausePanel.SetActive(false); settingsPanel.SetActive(true); settingsPanel.transform.SetAsLastSibling();
        settingsBackButton.Select();
    }

    public void CloseSettings()
    {
        if (!settingsPanel.activeSelf || gameOver.IsRestarting) return;
        settingsPanel.SetActive(false);
        if (ownsPause) { pausePanel.SetActive(true); settingsButton.Select(); }
        else gameOver.FocusSettings();
    }

    public void SetVolume(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return;
        AudioListener.volume = Mathf.Clamp01(value); volumeValue.text = Mathf.RoundToInt(AudioListener.volume * 100) + "%";
    }

    public void SetFullscreen(bool value)
    {
        if (!Application.isEditor) Screen.fullScreen = value;
    }

    public void SetLoading(bool loading)
    {
        pauseInput.interactable = pauseInput.blocksRaycasts = !loading;
        settingsInput.interactable = settingsInput.blocksRaycasts = !loading;
    }

    public void DismissForGameOver()
    {
        ownsPause = false; pausePanel.SetActive(false); settingsPanel.SetActive(false);
    }

    private void OnDisable()
    {
        if (ownsPause) { ownsPause = false; Time.timeScale = previousTimeScale; }
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (active == this) active = null;
    }
}
