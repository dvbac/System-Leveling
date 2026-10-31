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

        currentDamage = weaponData.Damage;
        currentSpeed = weaponData.Speed;
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
        currentDamage = WeaponScriptableObject.Safe(source.Damage, 0f) * (owner != null ? owner.DamageMultiplier : 1f) * WeaponScriptableObject.Safe(damageFactor, 1f);
        currentSpeed = WeaponScriptableObject.Safe(source.Speed, 0f);
        currentPierce = Mathf.Max(1, source.Pierce);
        transform.localScale *= WeaponScriptableObject.Safe(sizeFactor, 1f);
        hitTargets.Clear();
        Initialize(dir);
        transform.right = direction;
    }

    public void ApplyOwnerStats(PlayerStats player)
    {
        if (weaponData != null)
            currentDamage = weaponData.Damage * (player != null ? player.DamageMultiplier : 1f);
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
            enemy.TakeDamage(currentDamage);
            ReducePierce();
            return;
        }

        BreakableProps breakable =
            col.GetComponentInParent<BreakableProps>();

        if (breakable != null)
        {
            if (!hitTargets.Add(breakable.GetInstanceID())) return;
            breakable.TakeDamage(currentDamage);
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
