using UnityEngine;

public sealed class RedTomeCard : MonoBehaviour
{
    private bool collected;
    private Vector3 restingPosition;
    private SpriteRenderer visual;

    private void Awake() { restingPosition = transform.position; visual = GetComponent<SpriteRenderer>(); }
    private void Update()
    {
        if (GameOverController.GameplayStopped) return;
        visual.transform.position = restingPosition + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.06f);
        visual.color = Color.Lerp(Color.white, new Color(1f, 0.6f, 0.6f), (Mathf.Sin(Time.time * 5f) + 1f) * 0.2f);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);
    private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    private void TryCollect(Collider2D other)
    {
        if (other.GetComponent<PlayerCollector>() != null) return;
        PlayerStats player = other.GetComponentInParent<PlayerStats>();
        if (player != null) Collect(player);
    }

    public bool Collect(PlayerStats player)
    {
        if (collected || player == null || player.IsDead || Time.timeScale <= 0f || GameOverController.GameplayStopped) return false;
        // Claim before notifying UI so re-entrant or duplicate collider callbacks cannot grant a second offer.
        collected = true;
        if (!player.GrantTomeChoice()) { collected = false; return false; }
        foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
        GameAudio.Play(GameSound.LevelUp);
        Destroy(gameObject);
        return true;
    }
}
