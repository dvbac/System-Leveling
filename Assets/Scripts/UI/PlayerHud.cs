using UnityEngine;
using UnityEngine.UI;

public class PlayerHud : MonoBehaviour
{
    [SerializeField] private PlayerStats player;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image experienceFill;
    [SerializeField] private Text healthText;
    [SerializeField] private Text experienceText;
    [SerializeField] private Text levelText;
    [SerializeField] private EnemySpawner runClock;
    [SerializeField] private Text timeText;
    private int lastSecond = -1;
    private int lastHealth = -1;
    private int lastMaxHealth = -1;
    private double lastExperience = -1;
    private int lastCap = -1;
    private int lastLevel = -1;

    public void Configure(PlayerStats target, Image hp, Image exp, Text hpLabel, Text expLabel, Text levelLabel)
    {
        player = target;
        healthFill = hp;
        experienceFill = exp;
        healthText = hpLabel;
        experienceText = expLabel;
        levelText = levelLabel;
    }

    private void OnEnable()
    {
        if (player == null || healthFill == null || experienceFill == null ||
            healthText == null || experienceText == null || levelText == null)
        {
            Debug.LogError("PlayerHud is missing its UI references. Run Tools/System Leveling/Build HUD.", this);
            enabled = false;
            return;
        }
        player.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (player != null)
            player.Changed -= Refresh;
    }

    private void LateUpdate()
    {
        if (runClock == null || timeText == null) return;
        int seconds = Mathf.FloorToInt(runClock.ElapsedTime);
        if (seconds == lastSecond) return;
        timeText.text = $"{seconds / 60:00}:{seconds % 60:00}";
        lastSecond = seconds;
    }

    public void Refresh()
    {
        healthFill.fillAmount = player.MaxHealth > 0f ? Mathf.Clamp01(player.currentHealth / player.MaxHealth) : 0f;
        experienceFill.fillAmount = player.experienceCap > 0 ? Mathf.Clamp01((float)player.experience / player.experienceCap) : 0f;
        int hp = Mathf.CeilToInt(player.currentHealth);
        int max = Mathf.CeilToInt(player.MaxHealth);
        if (hp != lastHealth || max != lastMaxHealth)
        {
            healthText.text = $"{hp} / {max}";
            lastHealth = hp;
            lastMaxHealth = max;
        }
        if (player.experience != lastExperience || player.experienceCap != lastCap)
        {
            experienceText.text = $"{player.experience:0.##} / {player.experienceCap} EXP";
            lastExperience = player.experience;
            lastCap = player.experienceCap;
        }
        if (player.level != lastLevel)
        {
            levelText.text = $"{player.level:00}";
            lastLevel = player.level;
        }
    }
}
