using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public EnemyScriptableObject enemyData;
    [SerializeField, Min(0)] private int soulReward = 1;
    public int SoulReward => Mathf.Max(0, soulReward);

    [HideInInspector]
    public float currentMoveSpeed;
    [HideInInspector]
    public float currentHealth;
    [HideInInspector]
    public float currentDamage;

    private bool isDead = false;
    public bool IsDead => isDead;
    public float MaxHealth { get; private set; }
    public PlayerStats UpgradeAttacker { get; set; }
    public bool SecondaryUpgradeHit { get; set; }
    public float UpgradeMovementFactor => TryGetComponent(out EnemyUpgradeStatus status) ? status.MovementFactor : 1f;

    void Awake()
    {
        if (enemyData == null)
        {
            Debug.LogError(
                $"{name}: EnemyStats chưa gán enemyData.",
                this
            );
            return;
        }

        currentMoveSpeed = enemyData.MoveSpeed;
        currentHealth = enemyData.MaxHealth;
        MaxHealth = currentHealth;
        currentDamage = enemyData.Damage;
    }

    public void ScaleHealth(float multiplier)
    {
        MaxHealth *= multiplier;
        currentHealth *= multiplier;
    }

    public void TakeDamage(float dmg)
    {
        if (isDead || GameOverController.GameplayStopped)
            return;
        if (TryGetComponent(out RainbowSlime rainbow) && rainbow.Expired) return;

        if (TryGetComponent(out RotwoodBoss boss))
            dmg *= boss.DamageTakenMultiplier;
        currentHealth -= dmg;
        if (currentHealth <= 0)
        {
            Kill();
        }
        else if (TryGetComponent(out EnemyAnimation animation))
        {
            animation.PlayHit();
        }
    }

    public void Kill()
    {
        if (isDead) return;
        if (TryGetComponent(out RainbowSlime expiredSlime) && expiredSlime.Expired) return;
        isDead = true;
        if (TryGetComponent(out EnemyUpgradeStatus status)) status.OnKilled();
        if (UpgradeAttacker != null) UpgradeAttacker.OnUpgradeKill(this, SecondaryUpgradeHit);
        if (TryGetComponent(out RainbowSlime rainbow)) rainbow.Defeated();

        if (Application.isPlaying && enemyData != null && !GameOverController.GameplayStopped)
            SoulWallet.Add(SoulReward);
        if (TryGetComponent(out EliteEnemy elite)) elite.DropTreasure();

        if (TryGetComponent(out DropRateManager dropManager))
        {
            dropManager.DropItem();
        }

        float deathDuration = TryGetComponent(out RotwoodBoss boss)
            ? boss.BeginDeath()
            : TryGetComponent(out FrostfangBoss frostfang) ? frostfang.BeginDeath()
            : TryGetComponent(out EnemyAnimation animation) ? animation.PlayDeath() : 0f;
        Destroy(gameObject, deathDuration);
    }

    private void OnCollisionStay2D(Collision2D col)
    {
        if (GameOverController.GameplayStopped || isDead || !col.gameObject.CompareTag("Player"))
            return;

        PlayerStats player =
            col.gameObject.GetComponentInParent<PlayerStats>();

        if (player != null)
            player.TakeDamage(currentDamage);
    }
}
