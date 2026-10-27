using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    private bool selecting;
    private bool ownsPause;
    private float previousTimeScale;
    private int openedFrame;

    public bool IsOpen => ownsPause;
    public static bool InputBlocked => GameOverController.GameplayStopped || (active != null && active.IsOpen) || Time.frameCount <= blockedThroughFrame;
    public UpgradeKind ChoiceAt(int index) => choices[index];

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
        Close();
        if (active == this)
            active = null;
    }

    private void Sync()
    {
        if (player.IsDead || GameOverController.GameplayStopped)
        {
            Close();
            return;
        }
        if (selecting)
            return;
        if (player.PendingUpgrades <= 0)
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
            PresentChoices();
        }
        queueText.text = player.PendingUpgrades > 1
            ? $"{player.PendingUpgrades} UPGRADES WAITING  /  CHOOSE ONE TO CONTINUE"
            : "CHOOSE ONE UPGRADE TO CONTINUE";
    }

    private void PresentChoices()
    {
        UpgradeKind[] pool = { UpgradeKind.Damage, UpgradeKind.MoveSpeed, UpgradeKind.Magnet, UpgradeKind.Vitality };
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
        }
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
        bool applied = player.TryApplyUpgrade(choices[index]);
        selecting = false;
        if (!applied)
            return;
        if (player.PendingUpgrades > 0)
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
        switch (kind)
        {
            case UpgradeKind.Damage: return "POWER";
            case UpgradeKind.MoveSpeed: return "SWIFTNESS";
            case UpgradeKind.Magnet: return "ATTRACTION";
            default: return "VITALITY";
        }
    }

    public static string Description(UpgradeKind kind)
    {
        switch (kind)
        {
            case UpgradeKind.Damage: return "+10% DAMAGE\n\nStronger sword strikes\nand magic projectiles.";
            case UpgradeKind.MoveSpeed: return "+10% MOVE SPEED\n\nStay one step ahead\nof the approaching horde.";
            case UpgradeKind.Magnet: return "+20% PICKUP RANGE\n\nDraw experience closer.\nLeave nothing behind.";
            default: return "+20 MAX HEALTH\n\nRecover 20 health now.\nEndure the next battle.";
        }
    }

    public static string Symbol(UpgradeKind kind)
    {
        switch (kind)
        {
            case UpgradeKind.Damage: return "ATK";
            case UpgradeKind.MoveSpeed: return "SPD";
            case UpgradeKind.Magnet: return "EXP";
            default: return "HP";
        }
    }
}
