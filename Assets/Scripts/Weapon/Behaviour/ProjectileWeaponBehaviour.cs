using UnityEngine;
using System.Collections.Generic;

public class ProjectileWeaponBehaviour : MonoBehaviour
{
    public WeaponScriptableObject weaponData;

    [SerializeField]
    protected float destroyAfterSeconds = 5f;

    protected Vector3 direction;

    protected float currentDamage;
    protected float currentSpeed;
    protected int currentPierce;
    private readonly HashSet<int> hitTargets = new HashSet<int>();
    private int bouncesRemaining;
    private UpgradeHit upgradeHit;
    private bool iceShard, fireball;
    private float trailTime;
    private bool shotInitialized;
    public float CurrentDamage => currentDamage;
    public float CurrentSpeed => currentSpeed;
    public int CurrentPierce => currentPierce;
    public float CurrentKnockback { get; private set; }
    public float CriticalChance { get; private set; }

    protected virtual void Awake()
    {
        if (shotInitialized) return;
        if (weaponData == null)
        {
            Debug.LogError(
                $"{name}: ProjectileWeaponBehaviour chưa gán weaponData.",
                this
            );
            return;
        }

        ApplyOwnerStats(null);
        currentPierce = Mathf.Max(1, weaponData.Pierce);
    }

    protected virtual void Start()
    {
        Destroy(gameObject, destroyAfterSeconds);
    }

    public virtual void Initialize(Vector3 dir)
    {
        direction = dir.normalized;
    }

    public void InitializeShot(WeaponScriptableObject source, PlayerStats owner, Vector3 dir, float damageFactor, float sizeFactor, int pierceBonus = 0)
    {
        if (source == null) { Destroy(gameObject); return; }
        weaponData = source;
        shotInitialized = true;
        upgradeHit = UpgradeHit.Capture(owner);
        ApplyOwnerStats(owner);
        currentDamage *= WeaponScriptableObject.Safe(damageFactor, 1f);
        currentPierce = (int)System.Math.Min(int.MaxValue, (long)Mathf.Max(1, source.Pierce) + Mathf.Clamp(pierceBonus, 0, 32) + (owner != null ? owner.ExtraPierce : 0));
        bouncesRemaining = source.BaseBounces + (owner != null ? owner.BulletBounces : 0);
        transform.localScale *= WeaponScriptableObject.Safe(sizeFactor, 1f) * (owner != null ? owner.ProjectileSizeMultiplier : 1f);
        hitTargets.Clear();
        Initialize(dir);
        transform.right = direction;
    }

    public void ApplyOwnerStats(PlayerStats player)
    {
        if (weaponData == null) return;
        currentDamage = (WeaponScriptableObject.Safe(weaponData.Damage, 0f) + (player != null ? player.TotalBaseDamageBonus : 0f)) * (player != null ? player.DamageMultiplier * player.TomeDamageMultiplier * player.ProjectileDamageMultiplier * player.CombatDamageMultiplier : 1f);
        currentSpeed = WeaponScriptableObject.Safe(weaponData.Speed, 0f) * (player != null ? player.ProjectileSpeedMultiplier : 1f);
        CurrentKnockback = weaponData.Knockback * (player != null ? player.KnockbackMultiplier : 1f);
        CriticalChance = Mathf.Clamp01(weaponData.CriticalChance + (player != null ? player.CriticalChanceBonus : 0f));
    }

    protected virtual void Update()
    {
        if (GameOverController.GameplayStopped) return;
        transform.position +=
            direction * currentSpeed * Time.deltaTime;
        trailTime -= Time.deltaTime;
        if (Time.deltaTime > 0f && trailTime <= 0f && (fireball || iceShard || upgradeHit.Burn || upgradeHit.Freeze || upgradeHit.Reaper))
        {
            trailTime = .07f;
            UpgradeEffect.Create(transform.position, fireball || upgradeHit.Burn ? UpgradeEffect.Fire : UpgradeEffect.Ice,
                -direction * .3f, .22f, fireball ? .28f : .08f);
        }
    }

    public void ConfigureUpgradeProjectile(bool secondary, bool ice, bool fire)
    {
        upgradeHit.Secondary = secondary; iceShard = ice; fireball = fire;
        if (fire) { currentDamage = 20f * (upgradeHit.Owner != null ? upgradeHit.Owner.CombatDamageMultiplier : 1f); currentSpeed = 7f; currentPierce = 1; bouncesRemaining = 0; }
    }

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        if (GameOverController.GameplayStopped || currentPierce <= 0) return;
        // Dò theo component thay vì tag để không phụ thuộc TagManager.
        EnemyStats enemy = col.GetComponentInParent<EnemyStats>();

        if (enemy != null)
        {
            if (enemy.IsDead || !hitTargets.Add(enemy.GetInstanceID())) return;
            if (fireball) Explode(enemy);
            else if (upgradeHit.Owner != null) upgradeHit.Apply(enemy, WeaponScriptableObject.RollDamage(currentDamage, CriticalChance), iceShard);
            else enemy.TakeDamage(WeaponScriptableObject.RollDamage(currentDamage, CriticalChance));
            PlayImpact();
            UpgradeEffect.Burst(transform.position, fireball || upgradeHit.Burn ? UpgradeEffect.Fire : upgradeHit.Freeze || iceShard ? UpgradeEffect.Ice : new Color(1f, .85f, .45f), 4, .35f);
            if (!enemy.IsDead && enemy.TryGetComponent(out Enemy movement))
                movement.ApplyKnockback(direction, CurrentKnockback);
            if (!(upgradeHit.Reaper && enemy.IsDead) || fireball) ReducePierce();
            return;
        }

        BreakableProps breakable =
            col.GetComponentInParent<BreakableProps>();

        if (breakable != null)
        {
            if (!hitTargets.Add(breakable.GetInstanceID())) return;
            breakable.TakeDamage(WeaponScriptableObject.RollDamage(currentDamage, CriticalChance));
            PlayImpact();
            ReducePierce();
        }
    }

    private void PlayImpact() => GameAudio.Play(weaponData != null && weaponData.FireMode == WeaponFireMode.Magazine ? GameSound.MagicHit : GameSound.ArrowHit);

    private void Explode(EnemyStats target)
    {
        UpgradeEffect.Burst(transform.position, UpgradeEffect.Fire, 20, 1.8f);
        foreach (var enemy in FindObjectsByType<EnemyStats>())
            if (!enemy.IsDead && (enemy == target || (enemy.transform.position - transform.position).sqrMagnitude <= 3.24f))
                upgradeHit.Apply(enemy, currentDamage, false, true);
    }

    protected bool HasHit(EnemyStats enemy) => hitTargets.Contains(enemy.GetInstanceID());

    protected void ReducePierce()
    {
        currentPierce--;

        if (currentPierce <= 0)
        {
            if (bouncesRemaining > 0)
            {
                EnemyStats next = null; float closest = 25f;
                foreach (var enemy in FindObjectsByType<EnemyStats>(FindObjectsSortMode.None))
                {
                    float distance = (enemy.transform.position - transform.position).sqrMagnitude;
                    if (enemy.IsDead || hitTargets.Contains(enemy.GetInstanceID()) || distance > closest) continue;
                    closest = distance; next = enemy;
                }
                if (next != null)
                {
                    bouncesRemaining--; currentPierce = 1;
                    Initialize(next.transform.position - transform.position); transform.right = direction;
                    return;
                }
            }
            Destroy(gameObject);
        }
    }
}
