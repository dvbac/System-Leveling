using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(EnemyStats), typeof(Enemy), typeof(Rigidbody2D))]
public sealed class RainbowSlime : MonoBehaviour
{
    [SerializeField] private RedTomeCard redCardPrefab;
    private EnemyStats stats;
    private Enemy movement;
    private Rigidbody2D body;
    private SpriteRenderer visual;
    private TextMesh label;
    private Vector2 home, direction;
    private float turnAt;
    private bool expired, rewardDropped;
    private EnemySpawner spawner;
    private Text notice;
    public float RemainingSeconds { get; private set; } = 60f;
    public bool Expired => expired;
    public bool RewardDropped => rewardDropped;

    private void Awake()
    {
        stats = GetComponent<EnemyStats>(); movement = GetComponent<Enemy>(); movement.chasesPlayer = false;
        body = GetComponent<Rigidbody2D>(); visual = GetComponent<SpriteRenderer>(); home = transform.position;
        var title = new GameObject("Rainbow Slime Timer"); title.transform.SetParent(transform, false); title.transform.localPosition = new Vector3(0, 0.9f);
        label = title.AddComponent<TextMesh>(); label.anchor = TextAnchor.LowerCenter; label.alignment = TextAlignment.Center;
        label.characterSize = 0.04f; label.fontSize = 48; label.color = Color.white;
        title.GetComponent<MeshRenderer>().sortingOrder = visual.sortingOrder + 5;
    }

    public void Configure(EnemySpawner owner)
    {
        spawner = owner;
        var hud = FindAnyObjectByType<PlayerHud>();
        Canvas canvas = hud != null ? hud.GetComponentInParent<Canvas>() : null;
        if (canvas == null) return;
        var go = new GameObject("Rainbow Slime Notice", typeof(RectTransform), typeof(Text)); go.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f); rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0, -8); rect.sizeDelta = new Vector2(360, 44);
        notice = go.GetComponent<Text>(); notice.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        notice.fontSize = 14; notice.alignment = TextAnchor.MiddleCenter; notice.raycastTarget = false;
    }
    private void Update() => Tick(Time.deltaTime);

    internal void Tick(float delta)
    {
        if (expired || stats.IsDead || GameOverController.GameplayStopped || Time.timeScale <= 0f || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        RemainingSeconds = Mathf.Max(0f, RemainingSeconds - delta);
        if (RemainingSeconds <= 0f)
        {
            expired = true;
            // Expiry is not a death: no Kill(), Soul, EXP, or red card reward.
            foreach (var collider in GetComponents<Collider2D>()) collider.enabled = false;
            body.simulated = false; gameObject.SetActive(false); Destroy(gameObject);
        }
    }

    private void LateUpdate()
    {
        if (notice != null) notice.gameObject.SetActive(!expired && !stats.IsDead && !GameOverController.GameplayStopped);
        if (expired || stats.IsDead || GameOverController.GameplayStopped) return;
        visual.color = Color.HSVToRGB(Mathf.Repeat(Time.time * 0.22f, 1f), 0.75f, 1f);
        label.text = "RAINBOW SLIME\n" + Mathf.CeilToInt(RemainingSeconds) + "s";
        if (notice != null && PlayerController.Instance != null)
        {
            Vector3 offset = transform.position - PlayerController.Instance.transform.position;
            string compass = Mathf.Abs(offset.x) > Mathf.Abs(offset.y) ? (offset.x > 0 ? "EAST" : "WEST") : (offset.y > 0 ? "NORTH" : "SOUTH");
            notice.color = visual.color;
            notice.text = "RAINBOW SLIME / " + Mathf.CeilToInt(RemainingSeconds) + "s\n" + compass + " / " + offset.magnitude.ToString("0") + "m / RED CARD REWARD";
        }
    }

    private void FixedUpdate()
    {
        if (expired || stats.IsDead || movement.IsKnockedBack || Time.timeScale <= 0f || GameOverController.GameplayStopped) return;
        if (Time.time >= turnAt)
        {
            turnAt = Time.time + Random.Range(0.8f, 1.6f);
            direction = Random.insideUnitCircle.normalized;
            if ((body.position - home).sqrMagnitude > 4f) direction = (home - body.position).normalized;
        }
        Vector2 next = body.position + direction * stats.currentMoveSpeed * Time.fixedDeltaTime;
        if (spawner != null && !spawner.IsWalkableForSpecial(next, 0.45f)) { turnAt = 0; return; }
        visual.flipX = direction.x < 0;
        body.MovePosition(next);
    }

    public bool Defeated()
    {
        if (expired || rewardDropped || !stats.IsDead || GameOverController.GameplayStopped || redCardPrefab == null) return false;
        rewardDropped = true;
        Instantiate(redCardPrefab, transform.position, Quaternion.identity);
        return true;
    }

    private void OnDestroy() { if (notice != null) Destroy(notice.gameObject); }
}
