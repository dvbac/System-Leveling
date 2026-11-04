using UnityEngine;

// Homing only: damage, hit deduplication, pierce, bounce and upgrade procs stay in the shared projectile.
public class RedArrowProjectile : ProjectileWeaponBehaviour
{
    private EnemyStats target;
    private Collider2D targetBody;
    private float searchIn;
    public EnemyStats Target => target;

    public override void Initialize(Vector3 dir)
    {
        base.Initialize(dir);
        target = null; targetBody = null; searchIn = 0;
    }

    protected override void Update()
    {
        TickHoming(Time.deltaTime);
        base.Update();
    }

    internal void TickHoming(float delta)
    {
        if (Time.timeScale <= 0 || GameOverController.GameplayStopped || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        searchIn -= delta;
        if (target != null && (target.IsDead || !target.gameObject.activeInHierarchy || HasHit(target) ||
            (target.transform.position - transform.position).sqrMagnitude > 64))
        { target = null; targetBody = null; }
        if (target == null && searchIn <= 0)
        {
            searchIn = 0.15f;
            float closest = 64;
            foreach (var enemy in FindObjectsByType<EnemyStats>(FindObjectsSortMode.None))
            {
                if (enemy.IsDead || !enemy.gameObject.activeInHierarchy || HasHit(enemy)) continue;
                float distance = (enemy.transform.position - transform.position).sqrMagnitude;
                if (distance > closest) continue;
                closest = distance; target = enemy;
            }
            targetBody = target != null ? target.GetComponent<Collider2D>() : null;
        }
        if (target == null) return;
        Vector3 offset = (targetBody != null ? targetBody.bounds.center : target.transform.position) - transform.position;
        if (offset.sqrMagnitude < 0.0001f) return;
        float current = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float desired = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
        float angle = Mathf.MoveTowardsAngle(current, desired, 720 * delta);
        direction = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        transform.right = direction;
    }
}
