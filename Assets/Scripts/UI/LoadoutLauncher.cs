using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(LoadoutSelection), typeof(CanvasGroup))]
public class LoadoutLauncher : MonoBehaviour
{
    public const string GameplayScene = "Assets/Scenes/SampleScene.unity";
    public const string SelectionScene = "Assets/Scenes/LoadoutSelection.unity";

    public static CharacterScriptableObject Character { get; private set; }
    public static GameObject CharacterPrefab { get; private set; }
    public static GameObject Weapon { get; private set; }
    public bool IsLoading { get; private set; }
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
        selection.onConfirmed.AddListener(Launch);
    }

    private void OnDisable()
    {
        if (selection != null) selection.onConfirmed.RemoveListener(Launch);
    }

    public void Launch()
    {
        if (!Application.isPlaying || IsLoading || !selection.Confirmed) return;
        try
        {
            if (!Application.CanStreamedLevelBeLoaded(GameplayScene))
                throw new InvalidOperationException("Gameplay scene is missing from Build Settings. Run Connect Loadout to Gameplay.");
            GameObject weapon = selection.SelectedWeapon;
            if (selection.SelectedCharacter == null || selection.SelectedCharacterPrefab == null || weapon == null || !weapon.TryGetComponent(out WeaponController controller) || controller.weaponData == null ||
                !weapon.TryGetComponent(out WeaponGrip grip) || !grip.IsConfigured)
                throw new InvalidOperationException("Selected character or weapon is not configured.");
            Character = selection.SelectedCharacter; CharacterPrefab = selection.SelectedCharacterPrefab; Weapon = weapon;
            IsLoading = true;
            input.interactable = input.blocksRaycasts = false;
            selection.status.text = "ENTERING THE ARENA...";
            Time.timeScale = 1f;
            if (SceneManager.LoadSceneAsync(GameplayScene, LoadSceneMode.Single) == null)
                throw new InvalidOperationException("Gameplay scene load did not start.");
        }
        catch (Exception error)
        {
            ResetSelection();
            IsLoading = false;
            input.interactable = input.blocksRaycasts = true;
            selection.Back();
            selection.status.text = "COULD NOT START THE RUN. PLEASE TRY AGAIN.";
            Debug.LogException(error, this);
        }
    }
}
