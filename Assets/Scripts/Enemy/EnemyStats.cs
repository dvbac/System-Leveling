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
        currentDamage = enemyData.Damage;
    }

    public void TakeDamage(float dmg)
    {
        if (isDead || GameOverController.GameplayStopped)
            return;

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
        isDead = true;

        if (Application.isPlaying && enemyData != null && !GameOverController.GameplayStopped)
            SoulWallet.Add(SoulReward);

        if (TryGetComponent(out DropRateManager dropManager))
        {
            dropManager.DropItem();
        }

        float deathDuration = TryGetComponent(out RotwoodBoss boss)
            ? boss.BeginDeath()
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
