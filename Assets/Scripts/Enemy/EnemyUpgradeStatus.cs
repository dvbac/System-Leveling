using UnityEngine;

public class EnemyUpgradeStatus : MonoBehaviour
{
    private EnemyStats enemy;
    private UpgradeHit burnHit, frozenHit;
    private float burnUntil, freezeUntil, tickTime, visualTime, burnDamage, burnProcessedAt;
    public bool IsFrozen => Time.time < freezeUntil;
    public bool IsBurning => Time.time < burnUntil;
    public bool IsBoss => GetComponent<RotwoodBoss>() != null || GetComponent<FrostfangBoss>() != null;
    public float MovementFactor => IsFrozen ? (IsBoss ? .7f : 0f) : 1f;
    private void Awake() { enemy = GetComponent<EnemyStats>(); }

    public void Burn(UpgradeHit hit)
    {
        if (enemy == null || enemy.IsDead) return;
        bool wasBurning = IsBurning;
        burnHit = hit;
        // shortcut: burn refreshes and keeps the strongest DPS rather than stacking without limit; revisit after balance playtests.
        burnDamage = wasBurning ? Mathf.Max(burnDamage, hit.BurnDamage) : hit.BurnDamage;
        burnUntil = Time.time + 3f;
        if (!wasBurning) { tickTime = .5f; burnProcessedAt = Time.time; }
        UpgradeEffect.Burst(transform.position, UpgradeEffect.Fire, 5, .5f);
        if (hit.Owner != null && hit.Owner.HasUpgrade(UpgradeKind.SoothingWarmth) && Random.value < .005f) hit.Owner.RestoreHealth(1);
    }

    public void Freeze(UpgradeHit hit)
    {
        if (enemy == null || enemy.IsDead || IsFrozen) return;
        frozenHit = hit;
        freezeUntil = Time.time + (IsBoss ? .3f * hit.FreezeDuration / 1.5f : hit.FreezeDuration);
        if (!IsBoss && TryGetComponent(out Rigidbody2D body)) body.linearVelocity = Vector2.zero;
        UpgradeEffect.Burst(transform.position, UpgradeEffect.Ice, 10, .8f);
        if (hit.Frostbite) enemy.TakeDamage(enemy.MaxHealth * (IsBoss ? .01f : .15f));
    }

    private void Update()
    {
        if (enemy == null || enemy.IsDead || GameOverController.GameplayStopped || Time.deltaTime <= 0f) return;
        if (burnProcessedAt < burnUntil && burnHit.Owner != null && !burnHit.Owner.IsDead)
        {
            float through = Mathf.Min(Time.time, burnUntil);
            tickTime -= through - burnProcessedAt; burnProcessedAt = through;
            while (tickTime <= .0001f && !enemy.IsDead)
            {
                tickTime += .5f; enemy.UpgradeAttacker = burnHit.Owner; enemy.SecondaryUpgradeHit = burnHit.Secondary;
                enemy.TakeDamage(burnDamage * .5f);
            }
        }
        visualTime -= Time.deltaTime;
        if (visualTime <= 0f && (IsFrozen || IsBurning))
        {
            visualTime = .15f;
            UpgradeEffect.Create(transform.position + new Vector3(.25f * Mathf.Sin(Time.time * 12), .2f), IsFrozen ? UpgradeEffect.Ice : UpgradeEffect.Fire,
                Vector3.up * .5f, .3f, IsFrozen ? .14f : .1f);
        }
    }

    public void OnKilled()
    {
        if (!IsFrozen || !frozenHit.Shatter || enemy.SecondaryUpgradeHit || frozenHit.Owner == null || frozenHit.Owner.IsDead) return;
        UpgradeEffect.Burst(transform.position, UpgradeEffect.Ice, 18, 2f);
        float damage = enemy.MaxHealth * .07f;
        foreach (var other in FindObjectsByType<EnemyStats>())
        {
            if (other == enemy || other.IsDead || (other.transform.position - transform.position).sqrMagnitude > 4f) continue;
            other.UpgradeAttacker = frozenHit.Owner; other.SecondaryUpgradeHit = true;
            other.TakeDamage(damage);
        }
    }
}
