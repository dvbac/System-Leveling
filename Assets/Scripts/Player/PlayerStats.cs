using System;
using UnityEngine;

public partial class PlayerStats : MonoBehaviour
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
    public double experience = 0;
    public int level = 1;
    public int experienceCap = 5;

    public static int ExperienceForLevel(int currentLevel)
    {
        long current = Math.Max(1, currentLevel);
        long required = current < 20 ? 10 * current - 5
            : current == 20 ? 16 * current - 8
            : current < 40 ? 13 * current - 6
            : current < 60 ? 16 * current - 8
            : current * current;
        return (int)Math.Min(int.MaxValue, required);
    }

    [Header("I-frame")]
    public float invincibilityDuration;
    float invincibilityTimer;
    bool isInvincible;

    public event Action Changed;
    public event Action Died;
    public bool IsDead { get; private set; }
    public float MaxHealth { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;
    public float BaseDamageBonus { get; private set; }
    public int PendingUpgrades { get; private set; }
    public int AmmoUpgradeLevel { get; private set; }
    public int ReloadUpgradeLevel { get; private set; }
    public int QuickHandsUpgradeLevel { get; private set; }
    public int CriticalUpgradeLevel { get; private set; }
    public int ExtraProjectiles { get; private set; }
    public float SpreadModifier { get; private set; }
    public float ProjectileDamageMultiplier { get; private set; } = 1f;
    public float ProjectileSpeedMultiplier { get; private set; } = 1f;
    public float KnockbackMultiplier { get; private set; } = 1f;
    public float ReloadSpeedMultiplier { get; private set; } = 1f;
    public float FireRateMultiplier { get; private set; } = 1f;
    public float CriticalChanceBonus { get; private set; }
    public bool HasRangedWeapon
    {
        get
        {
            WeaponHolder holder = GetComponentInChildren<WeaponHolder>();
            return holder != null && holder.CurrentWeapon is RangedWeaponController && holder.CurrentWeapon.gameObject.activeInHierarchy;
        }
    }

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
        experienceCap = ExperienceForLevel(level);
        LevelUpChecker();
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
        TickUpgradeTrees();
    }
    public void IncreaseExperience(double amount)
    {
        if (IsDead || amount <= 0 || double.IsNaN(amount) || double.IsInfinity(amount))
            return;
        experience = Math.Min(int.MaxValue, experience + amount);

        LevelUpChecker();
        Changed?.Invoke();
    }

    void LevelUpChecker()
    {
        // experienceCap <= 0 sẽ gây vòng lặp vô hạn.
        if (experienceCap <= 0)
            return;

        while (experience >= experienceCap && level < int.MaxValue)
        {
            experience -= experienceCap;
            level++;
            PendingUpgrades++;

            experienceCap = ExperienceForLevel(level);

            Debug.Log("Level Up! Current Level: " + level);
            GameAudio.Play(GameSound.LevelUp);
        }
    }

    public void TakeDamage(float dmg)
    {
        if (IsDead || dmg <= 0f || Time.timeScale <= 0f || GameOverController.GameplayStopped)
            return;
        if (!isInvincible) {
            if (EffectiveDodgeChance > 0f && UnityEngine.Random.value < EffectiveDodgeChance) return;
            currentHealth = HasTome(TomeKind.Glass) ? 0f : Mathf.Max(0f, currentHealth - dmg);
            OnUpgradeHurt();
            GameAudio.Play(GameSound.PlayerHurt);
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
        PendingTomeChoices = 0;
        Died?.Invoke();
        Changed?.Invoke();
    }

    public void RestoreHealth(float amount)
    {
        if (IsDead || characterData == null)
            return;

        if(amount > 0f && currentHealth < MaxHealth)
        {
            UpgradeEffect.Burst(transform.position, new Color(.4f, 1f, .5f), 8, .8f);
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

    public bool CanReceiveUpgrade(UpgradeKind kind)
    {
        if (IsDead || GameOverController.GameplayStopped || characterData == null) return false;
        if (UpgradeCatalog.IsNew(kind)) return CanReceiveTreeUpgrade(kind);
        switch (kind)
        {
            case UpgradeKind.Ammo: return HasRangedWeapon && AmmoUpgradeLevel < 4;
            case UpgradeKind.Reload: return HasRangedWeapon && ReloadUpgradeLevel < 5;
            case UpgradeKind.DoubleShot:
                WeaponController weapon = GetComponentInChildren<WeaponHolder>()?.CurrentWeapon;
                return HasRangedWeapon && ExtraProjectiles < 2 && weapon.weaponData != null && weapon.weaponData.Projectiles + ExtraProjectiles < 32;
            case UpgradeKind.Focus: return HasRangedWeapon;
            case UpgradeKind.QuickHands: return HasRangedWeapon && QuickHandsUpgradeLevel < 5;
            case UpgradeKind.Critical: return CriticalUpgradeLevel < 5;
            case UpgradeKind.Damage:
            case UpgradeKind.MoveSpeed:
            case UpgradeKind.Magnet:
            case UpgradeKind.Vitality:
            case UpgradeKind.PowerBullet: return true;
            default: return false;
        }
    }

    public bool TryApplyUpgrade(UpgradeKind kind)
    {
        if (PendingUpgrades <= 0 || !CanReceiveUpgrade(kind))
            return false;

        switch (kind)
        {
            case UpgradeKind.Damage:
                BaseDamageBonus += 1f;
                break;
            case UpgradeKind.MoveSpeed:
                currentMoveSpeed += 0.2f;
                break;
            case UpgradeKind.Magnet:
                currentMagnet += characterData.Magnet * 0.2f;
                break;
            case UpgradeKind.Vitality:
                MaxHealth += 3f;
                currentHealth = Mathf.Min(MaxHealth, currentHealth + 3f);
                break;
            case UpgradeKind.Ammo:
                AmmoUpgradeLevel++;
                break;
            case UpgradeKind.Reload:
                ReloadUpgradeLevel++;
                break;
            case UpgradeKind.DoubleShot:
                ExtraProjectiles++;
                SpreadModifier += 10f;
                ProjectileDamageMultiplier *= 0.9f;
                break;
            case UpgradeKind.Focus:
                ProjectileSpeedMultiplier += 0.2f;
                SpreadModifier -= 15f;
                break;
            case UpgradeKind.PowerBullet:
                DamageMultiplier *= 1.15f;
                KnockbackMultiplier += 0.2f;
                break;
            case UpgradeKind.QuickHands:
                QuickHandsUpgradeLevel++;
                ReloadSpeedMultiplier += 0.05f;
                FireRateMultiplier += 0.05f;
                break;
            case UpgradeKind.Critical:
                CriticalUpgradeLevel++;
                CriticalChanceBonus = CriticalUpgradeLevel * 0.1f;
                break;
            default:
                if (!UpgradeCatalog.IsNew(kind)) return false;
                ApplyTreeUpgrade(kind);
                break;
        }

        RecordUpgrade(kind);
        PendingUpgrades--;
        Changed?.Invoke();
        return true;
    }
}
