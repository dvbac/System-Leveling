using UnityEngine;

// Optional visual adapter: legacy enemies without this component keep instant death.
[RequireComponent(typeof(Animator))]
public class EnemyAnimation : MonoBehaviour
{
    [SerializeField, Min(0f)] private float hitDuration = 4f / 12f;
    [SerializeField, Min(0f)] private float deathDuration = 8f / 10f;

    private Animator animator;
    private Vector3 lastPosition;
    private float hitUntil;
    private bool dying;
    private string state;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (GameOverController.GameplayStopped)
        {
            PlayGameOverIdle();
            return;
        }
        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;
        if (dying || Time.time < hitUntil)
            return;

        Play(delta.sqrMagnitude > 0.0000001f ? "Move" : "Idle");
    }

    public void PlayGameOverIdle()
    {
        if (dying) return;
        hitUntil = 0f;
        Play("Idle");
    }

    public void PlayHit()
    {
        if (dying) return;
        hitUntil = Time.time + hitDuration;
        Play("Hit", true);
    }

    public float PlayDeath()
    {
        if (dying) return deathDuration;
        dying = true;
        if (TryGetComponent(out Enemy movement))
            movement.enabled = false;
        foreach (Collider2D collider in GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
        if (TryGetComponent(out Rigidbody2D body))
            body.simulated = false;
        Play("Die", true);
        return deathDuration;
    }

    private void Play(string next, bool restart = false)
    {
        if (state == next && !restart) return;
        state = next;
        animator.Play(next, 0, 0f);
    }
}
