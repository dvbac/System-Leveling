using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class LoadoutSelection : MonoBehaviour
{
    [Serializable]
    public class View
    {
        public GameObject root;
        public Text title, category, description, stats, statValues, note;
        public RectTransform options;
        [NonSerialized] public Button[] cards = Array.Empty<Button>();
        [NonSerialized] public Image[] rings = Array.Empty<Image>();
    }

    [Header("Prefab catalog (or call SetPrefabs at runtime)")]
    public GameObject[] characterPrefabs, weaponPrefabs;
    public LoadoutOption optionPrefab;
    public View characterView, weaponView;
    public Text heading, subtitle, stepOne, stepTwo, summary, status, selectionCount;
    public Button back;
    public UnityEvent onConfirmed = new UnityEvent();

    public int CharacterIndex { get; private set; }
    public int WeaponIndex { get; private set; }
    public bool ChoosingWeapon { get; private set; }
    public bool Confirmed { get; private set; }
    public GameObject SelectedCharacterPrefab => characterPrefabs[CharacterIndex];
    public CharacterScriptableObject SelectedCharacter => SelectedCharacterPrefab.GetComponent<PlayerStats>().characterData;
    public GameObject SelectedWeapon => weaponPrefabs[WeaponIndex];

    public static readonly Color Accent = new Color32(91, 221, 202, 255);
    public static readonly Color Muted = new Color32(133, 154, 174, 255);
    private static readonly Color Border = new Color32(43, 65, 83, 255);
    private Button boundBack;
    private int characterPreview, weaponPreview;

    private void Awake() => Initialize();

    public void Initialize()
    {
        if (boundBack != null && boundBack == back) return;
        SetPrefabs(characterPrefabs, weaponPrefabs);
        back.onClick.AddListener(Back);
        boundBack = back;
    }

    public void SetPrefabs(GameObject[] characters, GameObject[] weapons)
    {
        if (!Application.isPlaying) throw new InvalidOperationException("SetPrefabs generates the selection list in Play Mode.");
        ValidatePrefabs(characters, weapons);
        if (optionPrefab == null || optionPrefab.button == null || optionPrefab.icon == null || optionPrefab.ring == null ||
            characterView.options == null || weaponView.options == null)
            throw new InvalidOperationException("Assign the option prefab and both list containers first.");
        if (TryGetComponent(out LoadoutLauncher launcher) && launcher.IsLoading)
            throw new InvalidOperationException("Cannot replace the catalog while gameplay is loading.");
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        ClearOptions(characterView); ClearOptions(weaponView);
        characterPrefabs = (GameObject[])characters.Clone();
        weaponPrefabs = (GameObject[])weapons.Clone();
        CharacterIndex = WeaponIndex = characterPreview = weaponPreview = 0;
        ChoosingWeapon = Confirmed = false;
        CreateOptions(characterView, characterPrefabs, false);
        CreateOptions(weaponView, weaponPrefabs, true);
        Refresh();
    }

    public static void ValidatePrefabs(GameObject[] characters, GameObject[] weapons)
    {
        if (characters == null || characters.Length == 0 || weapons == null || weapons.Length == 0)
            throw new ArgumentException("Provide at least one character prefab and one weapon prefab.");
        foreach (GameObject prefab in characters)
            if (prefab == null || prefab.scene.IsValid() || !prefab.TryGetComponent(out PlayerStats stats) || stats.characterData == null ||
                prefab.GetComponent<SpriteRenderer>()?.sprite == null || prefab.GetComponent<Animator>()?.runtimeAnimatorController == null ||
                prefab.GetComponent<CapsuleCollider2D>() == null || prefab.GetComponentInChildren<WeaponHolder>(true) == null)
                throw new ArgumentException("Provide character prefab assets, not scene objects, with PlayerStats/data, sprite, animator, body collider and WeaponHolder.");
        foreach (GameObject prefab in weapons)
            if (prefab == null || prefab.scene.IsValid() || !prefab.activeSelf || !prefab.TryGetComponent(out WeaponController weapon) || weapon.weaponData == null ||
                !prefab.TryGetComponent(out WeaponGrip grip) || !grip.IsConfigured || prefab.GetComponentInChildren<SpriteRenderer>(true)?.sprite == null ||
                prefab.GetComponentsInChildren<WeaponController>(true).Length != 1 || prefab.GetComponentInChildren<WeaponHolder>(true) != null)
                throw new ArgumentException("Provide weapon prefab assets, not scene objects, with one root WeaponController/data, a calibrated WeaponGrip and a sprite.");
    }

    private void CreateOptions(View view, GameObject[] prefabs, bool weapon)
    {
        view.cards = new Button[prefabs.Length]; view.rings = new Image[prefabs.Length];
        for (int i = 0; i < prefabs.Length; i++)
        {
            int index = i;
            LoadoutOption item = Instantiate(optionPrefab, view.options);
            item.name = prefabs[i].name;
            item.Bind(prefabs[i].GetComponentInChildren<SpriteRenderer>(true).sprite, weapon,
                () => { if (weapon) SelectWeapon(index); else SelectCharacter(index); },
                () => { if (weapon) PreviewWeapon(index); else PreviewCharacter(index); });
            item.gameObject.SetActive(true);
            view.cards[i] = item.button; view.rings[i] = item.ring;
        }
        view.options.GetComponentInParent<ScrollRect>(true).normalizedPosition = new Vector2(0, 1);
    }

    private static void ClearOptions(View view)
    {
        foreach (Button card in view.cards)
            if (card != null)
            {
                card.gameObject.SetActive(false);
                card.transform.SetParent(null, false);
                Destroy(card.gameObject);
            }
    }

    private void Update()
    {
        if (ChoosingWeapon && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Back();
    }

    public void SelectCharacter(int index)
    {
        if (ChoosingWeapon || index < 0 || index >= characterPrefabs.Length) return;
        CharacterIndex = characterPreview = index;
        Confirmed = false;
        ChoosingWeapon = true;
        weaponPreview = WeaponIndex;
        Refresh();
        weaponView.cards[WeaponIndex].Select();
    }

    public void SelectWeapon(int index)
    {
        if (!ChoosingWeapon || index < 0 || index >= weaponPrefabs.Length) return;
        if (Confirmed && WeaponIndex == index) return;
        WeaponIndex = weaponPreview = index;
        Confirmed = true;
        Refresh();
        onConfirmed.Invoke();
    }

    public void Back()
    {
        ChoosingWeapon = false;
        Confirmed = false;
        characterPreview = CharacterIndex;
        weaponPreview = WeaponIndex;
        Refresh();
        characterView.cards[CharacterIndex].Select();
    }

    public void PreviewCharacter(int index)
    {
        if (ChoosingWeapon || index < 0 || index >= characterPrefabs.Length) return;
        characterPreview = index;
        Refresh();
    }

    public void PreviewWeapon(int index)
    {
        if (!ChoosingWeapon || index < 0 || index >= weaponPrefabs.Length) return;
        weaponPreview = index;
        Refresh();
    }

    private void Refresh()
    {
        characterView.root.SetActive(!ChoosingWeapon);
        weaponView.root.SetActive(ChoosingWeapon);
        heading.text = ChoosingWeapon ? "SELECT YOUR WEAPON" : "SELECT YOUR CHARACTER";
        subtitle.text = "HOVER TO INSPECT   /   CLICK AN ICON TO SELECT";
        stepOne.color = ChoosingWeapon ? Muted : Accent;
        stepTwo.color = ChoosingWeapon ? Accent : Muted;
        back.gameObject.SetActive(ChoosingWeapon);
        string selectedWeapon = SelectedWeapon.GetComponent<WeaponController>().weaponData.name.ToUpperInvariant();
        selectionCount.text = $"{(ChoosingWeapon ? weaponPrefabs.Length : characterPrefabs.Length):00} OPTIONS / ALL AVAILABLE";
        summary.text = SelectedCharacter.name.ToUpperInvariant() + "  /  " + (Confirmed ? selectedWeapon : "SELECT A WEAPON");
        status.text = Confirmed ? "SELECTED / " + SelectedCharacter.name.ToUpperInvariant() + " + " + selectedWeapon : (ChoosingWeapon ? "CLICK A WEAPON TO COMPLETE YOUR LOADOUT" : "CLICK A CHARACTER TO CONTINUE");
        status.color = Confirmed ? Accent : Muted;

        CharacterScriptableObject c = characterPrefabs[characterPreview].GetComponent<PlayerStats>().characterData;
        characterView.title.text = c.name.ToUpperInvariant();
        characterView.category.text = "CHARACTER";
        characterView.description.text = "Starting stats come from this character prefab. Choose any available weapon after selecting your character.";
        characterView.stats.text = "HEALTH\n\nMOVE SPEED\n\nRECOVERY\n\nPICKUP RADIUS";
        characterView.statValues.text = $"{c.MaxHealth:0.##}\n\n{c.MoveSpeed:0.##}\n\n{c.Recovery:0.##} /s\n\n{c.Magnet:0.##}";
        characterView.note.text = "FREE LOADOUT / CHOOSE ANY WEAPON NEXT";

        GameObject weapon = weaponPrefabs[weaponPreview];
        WeaponScriptableObject w = weapon.GetComponent<WeaponController>().weaponData;
        bool melee = weapon.GetComponent<MeleeWeaponController>() != null;
        weaponView.title.text = w.name.ToUpperInvariant();
        weaponView.category.text = melee ? "MELEE" : "RANGED / " + w.FireMode.ToString().ToUpperInvariant();
        weaponView.description.text = melee ? "Strike nearby enemies in a sweeping arc." : w.FireMode == WeaponFireMode.Crossbow
            ? "Hold to charge a stronger shot, then release to fire." : w.FireMode == WeaponFireMode.Bow
            ? "Fire arrows and recover ammunition over time." : "Fire projectiles toward your aim. Reload when the magazine is empty.";
        weaponView.stats.text = melee ? "DAMAGE\n\nCOOLDOWN\n\nSWING ANGLE\n\nREACH" : "DAMAGE\n\nCOOLDOWN\n\nMAGAZINE\n\n" + (w.FireMode == WeaponFireMode.Bow ? "RECOVERY" : "RELOAD");
        weaponView.statValues.text = melee
            ? $"{w.Damage:0.##}\n\n{w.Cooldown:0.##} s\n\n{w.SwingAngle:0.##} deg\n\n{w.Range:0.##}"
            : $"{w.Damage:0.##}\n\n{w.Cooldown:0.##} s\n\n{w.MagazineSize}\n\n" + (w.FireMode == WeaponFireMode.Bow ? $"{w.BowRecoveryDelay:0.##} s" : $"{w.ReloadTime:0.##} s");
        weaponView.note.text = "CHARACTER / " + SelectedCharacter.name.ToUpperInvariant() + "   /   ANY WEAPON ALLOWED";
        Highlight(characterView, characterPreview, CharacterIndex, ChoosingWeapon);
        Highlight(weaponView, weaponPreview, WeaponIndex, Confirmed);
    }

    private static void Highlight(View view, int preview, int selected, bool confirmed)
    {
        for (int i = 0; i < view.cards.Length; i++)
        {
            bool active = i == preview;
            view.rings[i].color = confirmed && i == selected ? new Color32(234, 199, 132, 255) : active ? Accent : Border;
            view.cards[i].GetComponent<Image>().color = active ? new Color32(23, 55, 65, 255) : new Color32(17, 32, 47, 255);
        }
    }
}
