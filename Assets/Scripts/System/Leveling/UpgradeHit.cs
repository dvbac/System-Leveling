using UnityEngine;

// Capture on firing so later level-ups cannot change an attack already in flight.
public struct UpgradeHit
{
    public PlayerStats Owner;
    public bool Burn, Freeze, Frostbite, Shatter, Reaper, Secondary;
    public float BurnDamage, FreezeDuration;
    public static UpgradeHit Capture(PlayerStats owner)
    {
        if (owner == null) return default;
        return new UpgradeHit
        {
            Owner = owner, Burn = owner.HasUpgrade(UpgradeKind.PyroMage), Freeze = owner.HasUpgrade(UpgradeKind.FrostMage),
            Frostbite = owner.HasUpgrade(UpgradeKind.Frostbite), Shatter = owner.HasUpgrade(UpgradeKind.Shatter),
            Reaper = owner.HasUpgrade(UpgradeKind.ReaperRounds),
            BurnDamage = 3f * owner.BurnDamageMultiplier * (owner.HasUpgrade(UpgradeKind.IntenseBurn) ? 1.35f : 1f),
            FreezeDuration = 1.5f * owner.FreezeDurationMultiplier
        };
    }

    public void Apply(EnemyStats enemy, float damage, bool guaranteedFreeze = false, bool guaranteedBurn = false)
    {
        if (enemy == null || enemy.IsDead || Owner == null || Owner.IsDead || GameOverController.GameplayStopped) return;
        enemy.UpgradeAttacker = Owner; enemy.SecondaryUpgradeHit = Secondary;
        bool freeze = guaranteedFreeze || (Freeze && Random.value < .35f);
        bool burn = guaranteedBurn || (Burn && Random.value < .5f);
        if (freeze || burn)
        {
            var status = enemy.GetComponent<EnemyUpgradeStatus>();
            if (status == null) status = enemy.gameObject.AddComponent<EnemyUpgradeStatus>();
            if (freeze) status.Freeze(this);
            if (burn) status.Burn(this);
        }
        if (!enemy.IsDead && damage > 0f) enemy.TakeDamage(damage);
    }
}
