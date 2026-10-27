using System.Collections.Generic;
using UnityEngine;

public class MeleeWeaponBehaviour : MonoBehaviour
{
    [SerializeField] private Transform slashVisual;

    private readonly HashSet<EnemyStats> hitEnemies =
        new HashSet<EnemyStats>();

    private readonly HashSet<BreakableProps> hitProps =
        new HashSet<BreakableProps>();

    private Transform owner;

    private float damage;
    private float speed;
    private float swingAngle;
    private float startAngle;
    private float sweptAngle;

    private bool initialized;
    private bool finished;

    public void Initialize(
        WeaponScriptableObject data,
        Transform attackOrigin,
        Vector2 direction
    )
    {
        if (data == null || attackOrigin == null || slashVisual == null)
        {
            FinishSwing();
            return;
        }

        if (data.Speed <= 0f || data.SwingAngle <= 0f)
        {
            FinishSwing();
            return;
        }

        owner = attackOrigin;

        damage = data.Damage;
        PlayerStats player = attackOrigin.GetComponentInParent<PlayerStats>();
        if (player != null)
            damage *= player.DamageMultiplier;
        speed = data.Speed;
        swingAngle = data.SwingAngle;

        slashVisual.localPosition = Vector3.right * data.Range;

        float aimAngle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        startAngle = aimAngle - swingAngle / 2f;
        sweptAngle = 0f;

        initialized = true;
        UpdatePose();
    }

    private void Update()
    {
        if (GameOverController.GameplayStopped || !initialized || finished)
            return;

        if (owner == null)
        {
            FinishSwing();
            return;
        }

        sweptAngle = Mathf.Min(
            sweptAngle + speed * Time.deltaTime,
            swingAngle
        );

        UpdatePose();

        if (sweptAngle >= swingAngle)
            FinishSwing();
    }

    private void UpdatePose()
    {
        transform.position = owner.position;

        transform.rotation = Quaternion.Euler(
            0f,
            0f,
            startAngle + sweptAngle
        );
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        if (GameOverController.GameplayStopped || !initialized || finished)
            return;

        EnemyStats enemy = other.GetComponentInParent<EnemyStats>();

        if (enemy != null)
        {
            if (hitEnemies.Add(enemy))
                enemy.TakeDamage(damage);

            return;
        }

        BreakableProps prop =
            other.GetComponentInParent<BreakableProps>();

        if (prop != null && hitProps.Add(prop))
            prop.TakeDamage(damage);
    }

    private void FinishSwing()
    {
        finished = true;
        Destroy(gameObject);
    }
}
