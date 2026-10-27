using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private bool spriteFacesLeft;
    private EnemyStats enemy;
    private SpriteRenderer spriteRenderer;
    private Transform player;
    private bool heldForGameOver;

    private void Start()
    {
        enemy = GetComponent<EnemyStats>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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
        if (enemy == null)
            return;

        Vector2 direction =
            (player.position - transform.position).normalized;

        transform.position +=
            (Vector3)direction *
            enemy.currentMoveSpeed *
            Time.deltaTime;
    }

    private void FlipEnemy()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.flipX =
            (player.position.x < transform.position.x) != spriteFacesLeft;
    }
}
