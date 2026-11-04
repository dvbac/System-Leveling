using UnityEngine;

// Small pixel bursts share one sprite and respect gameplay pause, without changing animated character materials.
public class UpgradeEffect : MonoBehaviour
{
    private static Sprite pixel;
    private static int active;
    private SpriteRenderer visual;
    private Vector3 velocity;
    private float remaining, duration;
    private Color tint;
    public static readonly Color Fire = new Color(1f, .4f, .12f), Ice = new Color(.35f, .9f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { active = 0; }

    public static void Burst(Vector3 position, Color color, int count = 8, float radius = 1f)
    {
        if (!Application.isPlaying || GameOverController.GameplayStopped) return;
        for (int i = 0; i < count && active < 256; i++)
        {
            float angle = i * Mathf.PI * 2 / count;
            var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle));
            Create(position, color, direction * radius * 2f, .4f, .075f);
        }
    }

    public static void Create(Vector3 position, Color color, Vector3 velocity, float duration, float size)
    {
        if (active >= 256 || !Application.isPlaying) return;
        if (pixel == null)
        {
            var texture = new Texture2D(1, 1); texture.name = "Upgrade FX Pixel"; texture.filterMode = FilterMode.Point;
            texture.SetPixel(0, 0, Color.white); texture.Apply();
            pixel = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        }
        var go = new GameObject("Upgrade Spark"); go.transform.position = position; go.transform.localScale = Vector3.one * size;
        var effect = go.AddComponent<UpgradeEffect>();
        effect.visual = go.AddComponent<SpriteRenderer>(); effect.visual.sprite = pixel;
        effect.visual.sortingOrder = 50; effect.visual.color = color;
        effect.velocity = velocity; effect.duration = effect.remaining = duration; effect.tint = color;
        active++;
    }

    private void Update()
    {
        if (GameOverController.GameplayStopped) { Destroy(gameObject); return; }
        remaining -= Time.deltaTime;
        if (remaining <= 0) { Destroy(gameObject); return; }
        transform.position += velocity * Time.deltaTime;
        visual.color = new Color(tint.r, tint.g, tint.b, remaining / duration);
        transform.Rotate(0, 0, 120f * Time.deltaTime);
    }
    private void OnDestroy() { active = Mathf.Max(0, active - 1); }
}
