using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private bool spriteFacesLeft;
    private EnemyStats enemy;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool heldForGameOver;
    private Rigidbody2D body;
    private Vector2 knockbackVelocity;
    private float knockbackTime;
    private const float KnockbackDuration = 0.15f;

    private void Start()
    {
        enemy = GetComponent<EnemyStats>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        body = GetComponent<Rigidbody2D>();
        FindPlayer();
    }

    private void Update()
    {
        if (GameOverController.GameplayStopped)
        {
            HoldForGameOver();
            return;
        }
        // Player có thể được tạo sau enemy (hoặc chưa tồn tại).
        if (player == null && !FindPlayer())
            return;

        FlipEnemy();
        MoveToPlayer();
    }

    public void HoldForGameOver()
    {
        if (heldForGameOver) return;
        heldForGameOver = true;
        knockbackTime = 0f;
        if (TryGetComponent(out Rigidbody2D body))
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
        if (TryGetComponent(out EnemyAnimation animation))
            animation.PlayGameOverIdle();
        else if (TryGetComponent(out Animator animator) && animator.HasState(0, Animator.StringToHash("Idle")))
            animator.Play("Idle", 0, 0f);
    }

    private bool FindPlayer()
    {
        player = PlayerController.Instance != null
            ? PlayerController.Instance.transform
            : null;

        return player != null;
    }

    private void MoveToPlayer()
    {
        if (enemy == null || knockbackTime > 0f)
            return;

        Vector2 direction =
            (player.position - transform.position).normalized;

        transform.position +=
            (Vector3)direction *
            enemy.currentMoveSpeed *
            Time.deltaTime;
    }

    public void ApplyKnockback(Vector2 direction, float strength)
    {
        if (GameOverController.GameplayStopped || Time.timeScale <= 0f || (enemy != null && enemy.IsDead) ||
            direction.sqrMagnitude <= 0f || float.IsNaN(direction.x) || float.IsNaN(direction.y) ||
            float.IsInfinity(direction.x) || float.IsInfinity(direction.y) || float.IsNaN(strength) || float.IsInfinity(strength) || strength <= 0f) return;
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (body == null || !body.simulated) return;
        knockbackVelocity = direction.normalized * strength;
        knockbackTime = KnockbackDuration;
        body.linearVelocity = knockbackVelocity;
    }

    private void FixedUpdate()
    {
        if (knockbackTime <= 0f || Time.timeScale <= 0f) return;
        knockbackTime = GameOverController.GameplayStopped || (enemy != null && enemy.IsDead)
            ? 0f : Mathf.Max(0f, knockbackTime - Time.fixedDeltaTime);
        if (body != null && body.simulated) body.linearVelocity = knockbackVelocity * (knockbackTime / KnockbackDuration);
    }

    private void OnDisable()
    {
        if (knockbackTime > 0f && body != null) body.linearVelocity = Vector2.zero;
        knockbackTime = 0f;
    }

    private void FlipEnemy()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX =
            (player.position.x < transform.position.x) != spriteFacesLeft;
    }
}
