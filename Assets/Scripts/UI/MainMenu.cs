using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class MainMenu : MonoBehaviour
{
    public const string ScenePath = "Assets/Scenes/MainMenu.unity";
    public Button startButton, controlsButton, quitButton, closeControlsButton;
    public GameObject controlsPanel;
    public CanvasGroup buttons;
    public Text status;
    public bool IsLoading { get; private set; }

    private void Awake()
    {
        Time.timeScale = 1f;
        controlsPanel.SetActive(false);
        startButton.onClick.AddListener(StartRun);
        controlsButton.onClick.AddListener(ShowControls);
        closeControlsButton.onClick.AddListener(HideControls);
        quitButton.onClick.AddListener(Quit);
    }

    private void Start() => startButton.Select();

    private void Update()
    {
        if (!IsLoading && controlsPanel.activeSelf && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            HideControls();
    }

    public void StartRun()
    {
        if (IsLoading || controlsPanel.activeSelf) return;
        try
        {
            if (!Application.CanStreamedLevelBeLoaded(LoadoutLauncher.SelectionScene))
                throw new InvalidOperationException("Selection scene is missing from Build Settings. Run Build Main Menu.");
            IsLoading = true;
            GameAudio.Play(GameSound.Click);
            GetComponent<CanvasGroup>().interactable = false;
            status.text = "OPENING CHARACTER SELECTION...";
            Time.timeScale = 1f;
            if (SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene, LoadSceneMode.Single) == null)
                throw new InvalidOperationException("Character selection load did not start.");
        }
        catch (Exception error)
        {
            IsLoading = false;
            GetComponent<CanvasGroup>().interactable = true;
            status.text = "COULD NOT OPEN SELECTION. PLEASE TRY AGAIN.";
            Debug.LogException(error, this);
        }
    }

    public void ShowControls()
    {
        if (IsLoading) return;
        controlsPanel.SetActive(true);
        GameAudio.Play(GameSound.Click);
        buttons.interactable = false;
        closeControlsButton.Select();
    }

    public void HideControls()
    {
        if (IsLoading) return;
        controlsPanel.SetActive(false);
        buttons.interactable = true;
        GameAudio.Play(GameSound.Click);
        controlsButton.Select();
    }

    public void Quit()
    {
        if (IsLoading || controlsPanel.activeSelf) return;
        GameAudio.Play(GameSound.Click);
        if (Application.isEditor) { status.text = "QUIT EXITS THE STANDALONE GAME."; return; }
        Application.Quit();
    }
}
