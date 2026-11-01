using UnityEngine;

// One world-space warning, one hit; lifetime is owned by the boss encounter.
public class RotwoodRoot : MonoBehaviour
{
    private PlayerStats player;
    private SpriteRenderer visual;
    private Sprite roots;
    private float elapsed;
    private float delay;
    private float radius;
    private float damage;
    private bool struck;
    public bool HasStruck => struck;

    public void Configure(PlayerStats target, Sprite ring, Sprite rootSprite, float warning, float hitRadius, float hitDamage)
    {
        player = target;
        roots = rootSprite;
        delay = warning;
        radius = hitRadius;
        damage = hitDamage;
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sprite = ring;
        visual.sortingOrder = 8;
        visual.color = new Color(1f, 0.65f, 0.15f, 0.8f);
        transform.localScale = Vector3.one * radius * 2;
    }

    private void Update() => Tick(Time.deltaTime);

    internal void Tick(float delta)
    {
        if (GameOverController.GameplayStopped || player == null || player.IsDead)
        { Destroy(gameObject); return; }
        if (Time.timeScale <= 0f) return;
        if (delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        elapsed += delta;
        if (!struck && elapsed >= delay)
        {
            struck = true;
            visual.sprite = roots;
            visual.color = Color.white;
            if (Vector2.Distance(player.transform.position, transform.position) <= radius + 0.2f)
            {
                player.TakeDamage(damage);
                if (player.TryGetComponent(out PlayerController movement)) movement.ApplySlow(0.6f, 1.2f);
            }
        }
        if (elapsed >= delay + 0.5f) Destroy(gameObject);
    }
}
