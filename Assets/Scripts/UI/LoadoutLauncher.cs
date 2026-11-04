using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(LoadoutSelection), typeof(CanvasGroup))]
public class LoadoutLauncher : MonoBehaviour
{
    public const string GameplayScene = "Assets/Scenes/SampleScene.unity";
    public const string SnowScene = "Assets/Scenes/SnowScene.unity";
    public const string SelectionScene = "Assets/Scenes/LoadoutSelection.unity";

    public static CharacterScriptableObject Character { get; private set; }
    public static GameObject CharacterPrefab { get; private set; }
    public static GameObject Weapon { get; private set; }
    public bool IsLoading { get; private set; }
    public Button grassMapButton, snowMapButton;
    public GameObject loadoutPage, mapPage;
    public Button mapBackButton;
    public Text mapStatus;
    public bool ChoosingMap { get; private set; }
    public bool SnowSelected { get; private set; }
    public string SelectedGameplayScene => SnowSelected ? SnowScene : GameplayScene;
    private LoadoutSelection selection;
    private CanvasGroup input;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSelection()
    {
        Character = null; CharacterPrefab = null; Weapon = null;
    }

    private void Awake()
    {
        ResetSelection();
        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        selection = GetComponent<LoadoutSelection>();
        input = GetComponent<CanvasGroup>();
        selection.onConfirmed.AddListener(ShowMapSelection);
        if (grassMapButton != null) grassMapButton.onClick.AddListener(SelectGrassMap);
        if (snowMapButton != null) snowMapButton.onClick.AddListener(SelectSnowMap);
        if (mapBackButton != null) mapBackButton.onClick.AddListener(BackToWeapons);
    }

    private void OnDisable()
    {
        if (selection != null) selection.onConfirmed.RemoveListener(ShowMapSelection);
        if (grassMapButton != null) grassMapButton.onClick.RemoveListener(SelectGrassMap);
        if (snowMapButton != null) snowMapButton.onClick.RemoveListener(SelectSnowMap);
        if (mapBackButton != null) mapBackButton.onClick.RemoveListener(BackToWeapons);
    }

    public void ShowMapSelection()
    {
        if (IsLoading || !selection.Confirmed) return;
        ChoosingMap = true;
        loadoutPage.SetActive(false); mapPage.SetActive(true);
        mapStatus.text = selection.SelectedCharacter.name.ToUpperInvariant() + " / " + selection.SelectedWeapon.GetComponent<WeaponController>().weaponData.name.ToUpperInvariant();
        grassMapButton.Select();
    }

    public void BackToWeapons()
    {
        if (IsLoading || !ChoosingMap) return;
        ChoosingMap = false;
        GameAudio.Play(GameSound.Click);
        mapPage.SetActive(false); loadoutPage.SetActive(true);
        selection.ReopenWeapons();
    }

    public void SelectGrassMap() => SelectMap(false);
    public void SelectSnowMap() => SelectMap(true);

    private void SelectMap(bool snow)
    {
        if (IsLoading || !ChoosingMap) return;
        SnowSelected = snow;
        Launch();
    }

    public void Launch()
    {
        if (!Application.isPlaying || IsLoading || !ChoosingMap || !selection.Confirmed) return;
        try
        {
            GameObject weapon = selection.SelectedWeapon;
            if (selection.SelectedCharacter == null || selection.SelectedCharacterPrefab == null || weapon == null || !weapon.TryGetComponent(out WeaponController controller) || controller.weaponData == null ||
                !weapon.TryGetComponent(out WeaponGrip grip) || !grip.IsConfigured)
                throw new InvalidOperationException("Selected character or weapon is not configured.");
            Character = selection.SelectedCharacter; CharacterPrefab = selection.SelectedCharacterPrefab; Weapon = weapon;
            LoadScene(SelectedGameplayScene, SnowSelected ? "ENTERING THE SNOWFIELDS..." : "ENTERING THE GRASSLANDS...");
        }
        catch (Exception error)
        {
            ResetSelection();
            IsLoading = false;
            input.interactable = input.blocksRaycasts = true;
            mapStatus.text = "COULD NOT START THE RUN. PLEASE TRY AGAIN.";
            Debug.LogException(error, this);
        }
    }

    public void ReturnToMainMenu()
    {
        if (!Application.isPlaying || IsLoading) return;
        try { LoadScene(MainMenu.ScenePath, "RETURNING TO MAIN MENU..."); }
        catch (Exception error)
        {
            IsLoading = false;
            input.interactable = input.blocksRaycasts = true;
            selection.status.text = "COULD NOT OPEN MAIN MENU. PLEASE TRY AGAIN.";
            Debug.LogException(error, this);
        }
    }

    private void LoadScene(string path, string message)
    {
        if (!Application.CanStreamedLevelBeLoaded(path)) throw new InvalidOperationException("Scene is missing from Build Settings: " + path);
        IsLoading = true;
        GameAudio.Play(GameSound.Click);
        input.interactable = input.blocksRaycasts = false;
        if (ChoosingMap) mapStatus.text = message;
        else selection.status.text = message;
        Time.timeScale = 1f;
        if (SceneManager.LoadSceneAsync(path, LoadSceneMode.Single) == null) throw new InvalidOperationException("Scene load did not start: " + path);
    }
}
