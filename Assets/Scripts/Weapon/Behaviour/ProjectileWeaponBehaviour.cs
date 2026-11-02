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
    public float CurrentDamage => currentDamage;
    public float CurrentSpeed => currentSpeed;
    public float CurrentKnockback { get; private set; }
    public float CriticalChance { get; private set; }

    protected virtual void Awake()
    {
        if (weaponData == null)
        {
            Debug.LogError(
                $"{name}: ProjectileWeaponBehaviour chưa gán weaponData.",
                this
            );
            return;
        }

        ApplyOwnerStats(null);
        currentPierce = weaponData.Pierce;
    }

    protected virtual void Start()
    {
        Destroy(gameObject, destroyAfterSeconds);
    }

    public virtual void Initialize(Vector3 dir)
    {
        direction = dir.normalized;
    }

    public void InitializeShot(WeaponScriptableObject source, PlayerStats owner, Vector3 dir, float damageFactor, float sizeFactor)
    {
        if (source == null) { Destroy(gameObject); return; }
        weaponData = source;
        ApplyOwnerStats(owner);
        currentDamage *= WeaponScriptableObject.Safe(damageFactor, 1f);
        currentPierce = Mathf.Max(1, source.Pierce);
        transform.localScale *= WeaponScriptableObject.Safe(sizeFactor, 1f);
        hitTargets.Clear();
        Initialize(dir);
        transform.right = direction;
    }

    public void ApplyOwnerStats(PlayerStats player)
    {
        if (weaponData == null) return;
        currentDamage = (WeaponScriptableObject.Safe(weaponData.Damage, 0f) + (player != null ? player.BaseDamageBonus : 0f)) * (player != null ? player.DamageMultiplier * player.ProjectileDamageMultiplier : 1f);
        currentSpeed = WeaponScriptableObject.Safe(weaponData.Speed, 0f) * (player != null ? player.ProjectileSpeedMultiplier : 1f);
        CurrentKnockback = weaponData.Knockback * (player != null ? player.KnockbackMultiplier : 1f);
        CriticalChance = Mathf.Clamp01(weaponData.CriticalChance + (player != null ? player.CriticalChanceBonus : 0f));
    }

    protected virtual void Update()
    {
        if (GameOverController.GameplayStopped) return;
        transform.position +=
            direction * currentSpeed * Time.deltaTime;
    }

    protected virtual void OnTriggerEnter2D(Collider2D col)
    {
        if (GameOverController.GameplayStopped) return;
        // Dò theo component thay vì tag để không phụ thuộc TagManager.
        EnemyStats enemy = col.GetComponentInParent<EnemyStats>();

        if (enemy != null)
        {
            if (enemy.IsDead || !hitTargets.Add(enemy.GetInstanceID())) return;
            enemy.TakeDamage(WeaponScriptableObject.RollDamage(currentDamage, CriticalChance));
            if (!enemy.IsDead && enemy.TryGetComponent(out Enemy movement))
                movement.ApplyKnockback(direction, CurrentKnockback);
            ReducePierce();
            return;
        }

        BreakableProps breakable =
            col.GetComponentInParent<BreakableProps>();

        if (breakable != null)
        {
            if (!hitTargets.Add(breakable.GetInstanceID())) return;
            breakable.TakeDamage(WeaponScriptableObject.RollDamage(currentDamage, CriticalChance));
            ReducePierce();
        }
    }

    protected void ReducePierce()
    {
        currentPierce--;

        if (currentPierce <= 0)
        {
            Destroy(gameObject);
        }
    }
}
