using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public CharacterScriptableObject characterData;


    public float currentHealth;
    [HideInInspector]
    public float currentRecovery;
    [HideInInspector]
    public float currentMoveSpeed;
    [HideInInspector]
    public float currentMight;
    [HideInInspector]
    public float currentProjectileSpeed;
    [HideInInspector]
    public float currentMagnet;

    [Header("Experience/Level")]
    public int experience = 0;
    public int level = 1;
    public int experienceCap;

    [System.Serializable]
    public class LevelRange
    {
        public int startLevel;
        public int endLevel;
        public int experienceCapIncrease;
    }

    [Header("I-frame")]
    public float invincibilityDuration;
    float invincibilityTimer;
    bool isInvincible;

    public List<LevelRange> levelRanges;

    public event Action Changed;
    public event Action Died;
    public bool IsDead { get; private set; }
    public float MaxHealth { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;
    public int PendingUpgrades { get; private set; }

    private void Awake()
    {
        if (characterData == null)
        {
            Debug.LogError(
                $"{name}: PlayerStats chưa gán characterData.",
                this
            );
            return;
        }

        MaxHealth = characterData.MaxHealth;
        currentHealth = MaxHealth;
        currentRecovery = characterData.Recovery;
        currentMoveSpeed = characterData.MoveSpeed;
        currentMight = characterData.Might;
        currentProjectileSpeed = characterData.ProjectileSpeed;
        currentMagnet = characterData.Magnet;
    }

    private void Start()
    {
        if (levelRanges == null || levelRanges.Count == 0)
        {
            Debug.LogError(
                $"{name}: PlayerStats cần ít nhất một LevelRange.",
                this
            );
            return;
        }

        experienceCap = Mathf.Max(1, levelRanges[0].experienceCapIncrease);
        Changed?.Invoke();
    }

    void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;

            if (invincibilityTimer <= 0)
            {
                isInvincible = false;
            }
        }
        Recover();
    }
    public void IncreaseExperience(int amount)
    {
        if (IsDead || amount <= 0)
            return;
        experience += amount;

        LevelUpChecker();
        Changed?.Invoke();
    }

    void LevelUpChecker()
    {
        // experienceCap <= 0 sẽ gây vòng lặp vô hạn.
        if (experienceCap <= 0)
            return;

        while (experience >= experienceCap)
        {
            experience -= experienceCap;
            level++;
            PendingUpgrades++;

            int experienceCapIncrease = 0;

            foreach (LevelRange range in levelRanges ?? new List<LevelRange>())
            {
                if (level >= range.startLevel &&
                    level <= range.endLevel)
                {
                    experienceCapIncrease =
                        range.experienceCapIncrease;

                    break;
                }
            }

            experienceCap = Mathf.Max(1, experienceCap + experienceCapIncrease);

            Debug.Log("Level Up! Current Level: " + level);
        }
    }

    public void TakeDamage(float dmg)
    {
        if (IsDead || dmg <= 0f || Time.timeScale <= 0f || GameOverController.GameplayStopped)
            return;
        if (!isInvincible) {
            currentHealth = Mathf.Max(0f, currentHealth - dmg);
            invincibilityTimer = invincibilityDuration;
            isInvincible = true;
            if (currentHealth <= 0)
            { Kill(); }
            else
            { Changed?.Invoke(); }
        }

        
    }

    public void Kill()
    {
        if (IsDead) return;
        IsDead = true;
        currentHealth = 0f;
        PendingUpgrades = 0;
        Died?.Invoke();
        Changed?.Invoke();
    }

    public void RestoreHealth(float amount)
    {
        if (IsDead || characterData == null)
            return;

        if(amount > 0f && currentHealth < MaxHealth)
        {
            currentHealth += amount;
            if (currentHealth > MaxHealth)
            {  currentHealth = MaxHealth; }
            Changed?.Invoke();
        }
    }
        void Recover()
    {
        if (IsDead || characterData == null)
            return;

        if (currentRecovery > 0f && currentHealth < MaxHealth && Time.deltaTime > 0f)
        {
            // Kẹp lại để hồi máu không vượt quá MaxHealth.
            currentHealth = Mathf.Min(
                currentHealth + currentRecovery * Time.deltaTime,
                MaxHealth
            );
            Changed?.Invoke();
        }
    }

    public bool TryApplyUpgrade(UpgradeKind kind)
    {
        if (IsDead || PendingUpgrades <= 0 || characterData == null)
            return false;

        switch (kind)
        {
            case UpgradeKind.Damage:
                DamageMultiplier += 0.1f;
                break;
            case UpgradeKind.MoveSpeed:
                currentMoveSpeed += characterData.MoveSpeed * 0.1f;
                break;
            case UpgradeKind.Magnet:
                currentMagnet += characterData.Magnet * 0.2f;
                break;
            case UpgradeKind.Vitality:
                MaxHealth += 20f;
                currentHealth = Mathf.Min(MaxHealth, currentHealth + 20f);
                break;
            default:
                return false;
        }

        PendingUpgrades--;
        Changed?.Invoke();
        return true;
    }
}
