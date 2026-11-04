using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class LevelUpSelection : MonoBehaviour
{
    [SerializeField] private PlayerStats player;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button[] buttons;
    [SerializeField] private Text[] titles;
    [SerializeField] private Text[] descriptions;
    [SerializeField] private Text[] symbols;
    [SerializeField] private Text queueText;

    private static LevelUpSelection active;
    private static int blockedThroughFrame = -1;
    private readonly UpgradeKind[] choices = new UpgradeKind[3];
    private readonly TomeKind[] tomeChoices = new TomeKind[3];
    private bool choosingTome;
    private Graphic[] themeGraphics;
    private Color[] normalGraphicColors;
    private ColorBlock[] normalButtonColors;
    private bool selecting;
    private bool ownsPause;
    private float previousTimeScale;
    private int openedFrame;

    public bool IsOpen => ownsPause;
    public static bool InputBlocked => GameOverController.GameplayStopped || RunMenu.InputBlocked || (active != null && active.IsOpen) || Time.frameCount <= blockedThroughFrame;
    public UpgradeKind ChoiceAt(int index) => choices[index];
    public TomeKind TomeChoiceAt(int index) => tomeChoices[index];
    public bool IsChoosingTome => choosingTome;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        active = null;
        blockedThroughFrame = -1;
    }

    public void Configure(PlayerStats target, GameObject modal, Button[] cards, Text[] names,
        Text[] details, Text[] icons, Text queue)
    {
        player = target;
        panel = modal;
        buttons = cards;
        titles = names;
        descriptions = details;
        symbols = icons;
        queueText = queue;
    }

    private void OnEnable()
    {
        if (player == null || panel == null || queueText == null ||
            buttons == null || buttons.Length != 3 || titles == null || titles.Length != 3 ||
            descriptions == null || descriptions.Length != 3 || symbols == null || symbols.Length != 3)
        {
            Debug.LogError("LevelUpSelection is missing its UI references. Run Tools/System Leveling/Build HUD.", this);
            enabled = false;
            return;
        }
        active = this;
        if (themeGraphics == null)
        {
            themeGraphics = panel.GetComponentsInChildren<Graphic>(true);
            normalGraphicColors = themeGraphics.Select(graphic => graphic.color).ToArray();
            normalButtonColors = buttons.Select(button => button.colors).ToArray();
        }
        for (int i = 0; i < buttons.Length; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() => Choose(index));
        }
        player.Changed += Sync;
        panel.SetActive(false);
        Sync();
    }

    private void OnDisable()
    {
        if (player != null)
            player.Changed -= Sync;
        if (buttons != null)
            foreach (Button button in buttons)
                if (button != null)
                    button.onClick.RemoveAllListeners();
        ApplyTheme(false);
        Close();
        if (active == this)
            active = null;
    }

    public void Sync()
    {
        if (player.IsDead || GameOverController.GameplayStopped)
        {
            Close();
            return;
        }
        if (selecting)
            return;
        if (RunMenu.IsPaused) return;
        if (player.PendingUpgrades <= 0 && player.PendingTomeChoices <= 0)
        {
            Close();
            return;
        }
        if (!ownsPause)
        {
            previousTimeScale = Time.timeScale;
            ownsPause = true;
            Time.timeScale = 0f;
            panel.SetActive(true);
            panel.transform.SetAsLastSibling();
            PresentChoices();
        }
        queueText.text = choosingTome ? "RED CARD / CHOOSE ONE TOME / POSITIVES AND NEGATIVES APPLY FOR THIS RUN" : player.PendingUpgrades > 1
            ? $"{player.PendingUpgrades} UPGRADES WAITING  /  CHOOSE ONE TO CONTINUE"
            : "CHOOSE ONE UPGRADE TO CONTINUE";
    }

    private void PresentChoices()
    {
        choosingTome = player.PendingTomeChoices > 0;
        ApplyTheme(choosingTome);
        Transform window = queueText.transform.parent;
        Transform headingTransform = window.Find("Heading"), subtitleTransform = window.Find("Subtitle");
        if (headingTransform != null && headingTransform.TryGetComponent(out Text heading)) heading.text = choosingTome ? "RED CARD" : "LEVEL UP";
        if (subtitleTransform != null && subtitleTransform.TryGetComponent(out Text subtitle)) subtitle.text = choosingTome ? "Choose one special Tome. Read its cost carefully." : "Forge your path. Choose your next advantage.";
        for (int i = 0; i < buttons.Length; i++)
        {
            titles[i].resizeTextForBestFit = true; titles[i].resizeTextMinSize = 12; titles[i].resizeTextMaxSize = 22;
            descriptions[i].resizeTextForBestFit = true; descriptions[i].resizeTextMinSize = 10; descriptions[i].resizeTextMaxSize = 16;
            symbols[i].resizeTextForBestFit = true; symbols[i].resizeTextMinSize = 10; symbols[i].resizeTextMaxSize = 20;
        }
        if (choosingTome)
        {
            TomeKind[] tomePool = ((TomeKind[])System.Enum.GetValues(typeof(TomeKind))).Where(player.CanReceiveTome).ToArray();
            for (int i = 0; i < tomeChoices.Length; i++)
            {
                int pick = Random.Range(i, tomePool.Length);
                TomeKind swap = tomePool[i]; tomePool[i] = tomePool[pick]; tomePool[pick] = swap;
                tomeChoices[i] = tomePool[i];
                symbols[i].color = new Color32(255, 105, 125, 255);
                titles[i].text = TomeInfo.Title(tomePool[i]); descriptions[i].text = TomeInfo.Description(tomePool[i]); symbols[i].text = "TOME";
            }
            ArmChoiceInput();
            return;
        }
        UpgradeKind[] pool = ((UpgradeKind[])System.Enum.GetValues(typeof(UpgradeKind))).Where(player.CanReceiveUpgrade).ToArray();
        for (int i = 0; i < choices.Length; i++)
        {
            int pick = Random.Range(i, pool.Length);
            UpgradeKind swap = pool[i];
            pool[i] = pool[pick];
            pool[pick] = swap;
            choices[i] = pool[i];
            titles[i].text = Title(pool[i]);
            descriptions[i].text = Description(pool[i]);
            symbols[i].text = Symbol(pool[i]);
            var tree = UpgradeCatalog.Find(pool[i]);
            symbols[i].color = tree != null ? tree.Color : Color.white;
            if (tree != null)
            {
                int tier = pool[i] == tree.Root ? 1 : pool[i] == tree.Final ? 3 : 2;
                symbols[i].text += " / T" + tier;
            }
        }
        ArmChoiceInput();
    }

    private void ApplyTheme(bool red)
    {
        if (themeGraphics == null) return;
        for (int i = 0; i < themeGraphics.Length; i++)
        {
            Graphic graphic = themeGraphics[i];
            if (graphic == null) continue;
            if (!red) { graphic.color = normalGraphicColors[i]; continue; }
            if (graphic is Text)
                graphic.color = graphic.name == "Heading" || graphic.name == "Title" ? new Color32(255, 235, 236, 255)
                    : graphic.name == "Description" || graphic.name == "Eyebrow" || graphic.name == "Queue" ? new Color32(235, 183, 191, 255) : new Color32(255, 105, 125, 255);
            else
                graphic.color = graphic.name.Contains("Border") ? new Color32(246, 63, 86, 255)
                    : graphic.gameObject == panel ? new Color32(27, 2, 9, 210)
                    : graphic.name.Contains("Card") ? new Color32(74, 16, 31, 255) : new Color32(35, 8, 16, 255);
        }
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            ColorBlock colors = normalButtonColors[i];
            if (red)
            {
                colors.highlightedColor = colors.selectedColor = new Color(1.45f, 1.08f, 1.08f);
                colors.pressedColor = new Color(0.8f, 0.6f, 0.65f);
            }
            buttons[i].colors = colors;
        }
    }

    private void ArmChoiceInput()
    {
        openedFrame = Time.frameCount;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
    }

    public void Choose(int index)
    {
        // Ignore the click that opened this offer, repeated callbacks, and closing-frame clicks.
        if (player.IsDead || GameOverController.GameplayStopped || !ownsPause || selecting || index < 0 || index >= choices.Length || Time.frameCount <= openedFrame)
            return;
        selecting = true;
        bool applied = choosingTome ? player.TryApplyTome(tomeChoices[index]) : player.TryApplyUpgrade(choices[index]);
        selecting = false;
        if (!applied)
            return;
        GameAudio.Play(GameSound.Upgrade);
        if (player.PendingUpgrades > 0 || player.PendingTomeChoices > 0)
            PresentChoices();
        Sync();
    }

    private void Close()
    {
        if (panel != null)
            panel.SetActive(false);
        if (!ownsPause)
            return;
        if (!GameOverController.GameplayStopped)
            Time.timeScale = previousTimeScale;
        ownsPause = false;
        blockedThroughFrame = Time.frameCount + 1;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void DismissForGameOver() => Close();

    public static string Title(UpgradeKind kind)
    {
        if (UpgradeCatalog.IsNew(kind)) return UpgradeCatalog.Title(kind);
        switch (kind)
        {
            case UpgradeKind.Damage: return "POWER";
            case UpgradeKind.MoveSpeed: return "SWIFTNESS";
            case UpgradeKind.Magnet: return "ATTRACTION";
            case UpgradeKind.Ammo: return "CAPACITY";
            case UpgradeKind.Reload: return "RECHARGE";
            case UpgradeKind.DoubleShot: return "DOUBLE SHOT";
            case UpgradeKind.Focus: return "FOCUS";
            case UpgradeKind.PowerBullet: return "POWER BULLET";
            case UpgradeKind.QuickHands: return "QUICK HANDS";
            case UpgradeKind.Critical: return "CRITICAL";
            default: return "VITALITY";
        }
    }

    public static string Description(UpgradeKind kind)
    {
        if (UpgradeCatalog.IsNew(kind)) return UpgradeCatalog.Description(kind);
        switch (kind)
        {
            case UpgradeKind.Damage: return "+1 BASE DAMAGE\n\nApplied before damage\npercentage bonuses.";
            case UpgradeKind.MoveSpeed: return "+0.2 MOVE SPEED\n\nStay one step ahead\nof the approaching horde.";
            case UpgradeKind.Magnet: return "+20% PICKUP RANGE\n\nDraw experience closer.\nLeave nothing behind.";
            case UpgradeKind.Ammo: return "+1 AMMO CAPACITY\n\nCrossbow: higher charge ceiling\nand a stronger maximum shot.";
            case UpgradeKind.Reload: return "+RELOAD SPEED\n\nCrossbow also reaches\nfull charge sooner.";
            case UpgradeKind.DoubleShot: return "+1 PROJECTILE\n+10 DEG SPREAD\n-10% PROJECTILE DAMAGE";
            case UpgradeKind.Focus: return "+20% PROJECTILE SPEED\n-15 DEG SPREAD";
            case UpgradeKind.PowerBullet: return "+15% DAMAGE\n+20% KNOCKBACK";
            case UpgradeKind.QuickHands: return "+5% RELOAD SPEED\n+5% FIRE RATE";
            case UpgradeKind.Critical: return "+10% CRITICAL CHANCE\nCRITICAL HITS DEAL x2 DAMAGE";
            default: return "+3 MAX HEALTH\n\nRecover 3 health now.\nEndure the next battle.";
        }
    }

    public static string Symbol(UpgradeKind kind)
    {
        if (UpgradeCatalog.IsNew(kind)) return UpgradeCatalog.Find(kind).Symbol;
        switch (kind)
        {
            case UpgradeKind.Damage: return "ATK";
            case UpgradeKind.MoveSpeed: return "SPD";
            case UpgradeKind.Magnet: return "EXP";
            case UpgradeKind.Ammo: return "AMMO";
            case UpgradeKind.Reload: return "RLD";
            case UpgradeKind.DoubleShot: return "2X";
            case UpgradeKind.Focus: return "AIM";
            case UpgradeKind.PowerBullet: return "DMG";
            case UpgradeKind.QuickHands: return "FAST";
            case UpgradeKind.Critical: return "CRIT";
            default: return "HP";
        }
    }
}
