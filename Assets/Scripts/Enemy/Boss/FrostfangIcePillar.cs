using UnityEngine;

public class FrostfangIcePillar : MonoBehaviour
{
    private PlayerStats player;
    private Sprite[] frames;
    private SpriteRenderer visual;
    private float elapsed, warning, damage;
    public bool HasStruck { get; private set; }
    public const float Radius = 0.8f;

    public void Configure(PlayerStats target, Sprite ring, Sprite[] sprites, float delay, float hitDamage)
    {
        player = target; frames = sprites; warning = delay; damage = hitDamage;
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sortingOrder = 8;
        visual.sprite = ring;
        visual.color = new Color(1, 0.4f, 0.15f, 0.9f);
        transform.localScale = Vector3.one * Radius * 2;
    }

    private void Update() => Tick(Time.deltaTime);

    internal void Tick(float delta)
    {
        if (GameOverController.GameplayStopped || player == null || player.IsDead) { Destroy(gameObject); return; }
        if (Time.timeScale <= 0 || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        elapsed += delta;
        if (elapsed < warning) return;
        float age = elapsed - warning;
        visual.color = Color.white;
        transform.localScale = Vector3.one;
        int frame = age < 5f / 12 ? Mathf.Min(4, Mathf.FloorToInt(age * 12)) : age < 3 ? 6 :
            Mathf.Min(11, 8 + Mathf.FloorToInt((age - 3) * 12));
        visual.sprite = frames[frame];
        if (!HasStruck)
        {
            HasStruck = true;
            if (Vector2.Distance(player.transform.position, transform.position) <= Radius)
            {
                player.TakeDamage(damage);
                player.GetComponent<PlayerController>()?.ApplySlow(0.6f, 1.2f);
            }
        }
        if (age >= 3 + 4f / 12) Destroy(gameObject);
    }
}
