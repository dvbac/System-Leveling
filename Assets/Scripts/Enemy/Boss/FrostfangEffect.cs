using UnityEngine;

// Cosmetic playback only; damage never depends on particle bounds or alpha.
public class FrostfangEffect : MonoBehaviour
{
    private Sprite[] frames;
    private float fps, lifetime, elapsed;
    private bool loop;
    private SpriteRenderer visual;

    public void Configure(Sprite[] sprites, float rate, bool repeat, float seconds, int order = 9)
    {
        frames = sprites; fps = rate; loop = repeat; lifetime = seconds;
        visual = gameObject.AddComponent<SpriteRenderer>();
        visual.sortingOrder = order;
        visual.sprite = frames[0];
    }

    private void Update() => Tick(Time.deltaTime);

    internal void Tick(float delta)
    {
        if (GameOverController.GameplayStopped) { Destroy(gameObject); return; }
        if (Time.timeScale <= 0 || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        elapsed += delta;
        int index = Mathf.FloorToInt(elapsed * fps);
        visual.sprite = frames[loop ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
        if (elapsed >= lifetime) Destroy(gameObject);
    }
}
