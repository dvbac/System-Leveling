using System;
using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStats
{
    private readonly HashSet<TomeKind> tomes = new HashSet<TomeKind>();
    public int PendingTomeChoices { get; private set; }
    [SerializeField, Range(0, 1)] private float dodgeChance;
    public float EffectiveDodgeChance => HasTome(TomeKind.Glass) ? 0f : Mathf.Clamp01(dodgeChance - (HasTome(TomeKind.Vitality) ? 0.25f : 0f));
    public float TomeDamageMultiplier => (HasTome(TomeKind.Glass) ? 2f : 1f) * (HasTome(TomeKind.Wind)
        ? 0.85f * (1f + Mathf.Max(0f, currentMoveSpeed / Mathf.Max(0.001f, characterData.MoveSpeed) - 1f)) : 1f);
    public float TotalBaseDamageBonus => BaseDamageBonus + (HasTome(TomeKind.Might) ? 2f * MaxHealth : 0f);
    public float ProjectileSizeMultiplier => TreeProjectileSize * (HasTome(TomeKind.Power) ? 2f : 1f) * (HasTome(TomeKind.Stillness) ? 2f : 1f);
    public int ExtraPierce => TreePierce + (HasTome(TomeKind.Power) ? 1 : 0);
    public int BulletBounces => TreeBounces + (HasTome(TomeKind.Elasticity) ? 2 : 0);
    public int BaseAmmoMultiplier => HasTome(TomeKind.Rage) ? 3 : 1;
    public float WalkSpeedMultiplier => HasTome(TomeKind.Stillness) ? 0f : 1f + WindBonus;
    public float BurnDamageMultiplier => HasTome(TomeKind.Elements) ? 1.35f : 1f;
    public float LightningDamageMultiplier => HasTome(TomeKind.Elements) ? 1.35f : 1f;
    public float FreezeDurationMultiplier => HasTome(TomeKind.Elements) ? 1.35f : 1f;

    public bool HasTome(TomeKind kind) => tomes.Contains(kind);
    public bool CanReceiveTome(TomeKind kind) => Enum.IsDefined(typeof(TomeKind), kind) && !IsDead && characterData != null && !GameOverController.GameplayStopped && !HasTome(kind)
        && (kind != TomeKind.Elements || HasTome(TomeKind.Electro) || HasUpgrade(UpgradeKind.PyroMage) || HasUpgrade(UpgradeKind.FrostMage))
        && (MaxHealth > 1f || (kind != TomeKind.Elasticity && kind != TomeKind.Power && kind != TomeKind.Speed));

    public bool GrantTomeChoice()
    {
        if (IsDead || GameOverController.GameplayStopped || characterData == null || tomes.Count + PendingTomeChoices >= 2) return false;
        PendingTomeChoices++;
        Changed?.Invoke();
        return true;
    }

    public bool TryApplyTome(TomeKind kind)
    {
        if (PendingTomeChoices <= 0 || !CanReceiveTome(kind)) return false;
        tomes.Add(kind);
        switch (kind)
        {
            case TomeKind.Elasticity: ProjectileDamageMultiplier *= 0.75f; ChangeMaxHealth(-1); break;
            case TomeKind.Electro: ReloadSpeedMultiplier *= 1.25f; ProjectileDamageMultiplier *= 0.84f; break;
            case TomeKind.Might: ChangeMaxHealth(1); break;
            case TomeKind.Power:
                ProjectileDamageMultiplier *= 1.5f; FireRateMultiplier *= 0.75f; ChangeMaxHealth(-1); break;
            case TomeKind.Rage:
                FireRateMultiplier *= 1.6f; SpreadModifier += 60f; ProjectileDamageMultiplier *= 0.5f; KnockbackMultiplier *= 0.05f; break;
            case TomeKind.Speed: currentMoveSpeed *= 1.5f; ChangeMaxHealth(-1); break;
            case TomeKind.Stillness: ProjectileDamageMultiplier *= 2f; ReloadSpeedMultiplier *= 0.5f; break;
            case TomeKind.Vitality: ChangeMaxHealth(4); break;
            case TomeKind.Wind: currentMoveSpeed *= 1.1f; break;
        }
        PendingTomeChoices--;
        Changed?.Invoke();
        return true;
    }

    private void ChangeMaxHealth(float delta)
    {
        MaxHealth += delta;
        currentHealth = Mathf.Min(MaxHealth, currentHealth + Mathf.Max(0f, delta));
    }

    public int CallLastAmmoLightning(Vector3 origin)
    {
        if (!HasTome(TomeKind.Electro) || IsDead || Time.timeScale <= 0f || GameOverController.GameplayStopped) return 0;
        int hits = 0;
        foreach (var enemy in FindObjectsByType<EnemyStats>(FindObjectsSortMode.None))
        {
            if (enemy.IsDead || (enemy.transform.position - origin).sqrMagnitude > 25f) continue;
            enemy.UpgradeAttacker = this; enemy.SecondaryUpgradeHit = false;
            enemy.TakeDamage((22f + (HasTome(TomeKind.Might) ? 2f * MaxHealth : 0f)) * LightningDamageMultiplier * DamageMultiplier * TomeDamageMultiplier * CombatDamageMultiplier);
            TomeLightningFlash.Create(enemy);
            hits++;
        }
        return hits;
    }
}
