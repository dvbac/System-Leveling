using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class RotwoodEncounter : MonoBehaviour
{
    [SerializeField] private RotwoodBoss bossPrefab;
    [SerializeField, Min(0)] private float spawnAtSeconds = 1020f;
    [SerializeField] private string groundTilemapName;
    [SerializeField] private LayerMask obstacleLayers;
    private PlayerStats player;
    private RotwoodBoss boss;
    private bool spawned;
    private bool victorySent;
    private float retryAt;
    private float groundRefreshAt = -1;
    private readonly List<Tilemap> grounds = new List<Tilemap>();
    private GameObject hud;
    private Image healthFill;
    private Text healthLabel;
    private Text phaseLabel;
    private float hudWidth;
    public float ElapsedTime { get; private set; }
    public RotwoodBoss Boss => boss;
    public bool HasSpawned => spawned;

    private void Update()
    {
        Tick(Time.deltaTime);
        if (hud != null)
        {
            hud.SetActive(boss != null && !GameOverController.GameplayStopped);
            if (boss != null)
            {
                float max = boss.Stats.enemyData.MaxHealth;
                healthFill.fillAmount = Mathf.Clamp01(boss.Stats.currentHealth / max);
                healthLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0,boss.Stats.currentHealth))} / {max:0}";
                phaseLabel.text = boss.State == RotwoodBoss.ActionState.Exposed ? "HEART EXPOSED - DAMAGE +50%" :
                    boss.Stats.currentHealth <= max * 0.5f ? "PHASE II" : "ANCIENT FOREST GUARDIAN";
            }
        }
    }

    internal void Tick(float delta)
    {
        if (player == null && PlayerController.Instance != null)
            player = PlayerController.Instance.GetComponent<PlayerStats>();
        if (GameOverController.GameplayStopped || player == null || player.IsDead ||
            !player.gameObject.activeInHierarchy || Time.timeScale <= 0 || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        ElapsedTime += delta;
        if (!spawned && ElapsedTime >= spawnAtSeconds && ElapsedTime >= retryAt)
        {
            retryAt = ElapsedTime + 1f;
            SpawnNow();
        }
    }

    public bool SpawnNow()
    {
        if (spawned || bossPrefab == null || GameOverController.GameplayStopped) return false;
        if (player == null && PlayerController.Instance != null) player = PlayerController.Instance.GetComponent<PlayerStats>();
        if (player == null || player.IsDead) return false;
        Physics2D.SyncTransforms();
        for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.PI * 2 / 32;
            Vector3 p = player.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 6f;
            bool footprint = IsOnGround(p) && IsOnGround(p + Vector3.up) && IsOnGround(p - Vector3.up) &&
                IsOnGround(p + Vector3.right) && IsOnGround(p - Vector3.right);
            if (!footprint || (obstacleLayers.value != 0 && Physics2D.OverlapCircle(p, 1f, obstacleLayers) != null)) continue;
            boss = Instantiate(bossPrefab, p, Quaternion.identity);
            boss.Configure(player, this, null, obstacleLayers);
            spawned = true;
            BuildHud();
            Debug.Log("ROTWOOD ARRIVED at survival time " + ElapsedTime.ToString("F1"));
            return true;
        }
        return false; // Retry after streaming more ground; never force an unsafe spawn.
    }

    public bool IsOnGround(Vector3 position)
    {
        if (string.IsNullOrEmpty(groundTilemapName)) return true;
        if (Time.time >= groundRefreshAt)
        {
            groundRefreshAt = Time.time + 1f;
            grounds.Clear();
            foreach (var map in FindObjectsByType<Tilemap>())
                if (map.name == groundTilemapName) grounds.Add(map);
        }
        foreach (var map in grounds)
            if (map != null && map.gameObject.activeInHierarchy && map.HasTile(map.WorldToCell(position))) return true;
        return false;
    }

    public void BossDefeated()
    {
        if (victorySent || player == null || player.IsDead || GameOverController.GameplayStopped) return;
        victorySent = true;
        var result = FindAnyObjectByType<GameOverController>();
        if (result != null) result.Win();
    }

    private void BuildHud()
    {
        var playerHud = FindAnyObjectByType<PlayerHud>();
        Canvas canvas = playerHud != null ? playerHud.GetComponentInParent<Canvas>() : null;
        if (canvas == null) { Debug.LogError("Rotwood needs the existing gameplay HUD Canvas."); return; }
        hud = new GameObject("Rotwood Boss Health", typeof(RectTransform), typeof(Image));
        hud.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)hud.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
        rect.pivot = new Vector2(0.5f, 1);
        // Below timer/Soul, with room for the left-hand weapon panel at 16:9 and 4:3.
        hudWidth = Mathf.Clamp(((RectTransform)canvas.transform).rect.width - 880, 180, 400);
        rect.anchoredPosition = new Vector2(0, -124);
        rect.sizeDelta = new Vector2(hudWidth, 78);
        hud.GetComponent<Image>().color = new Color32(12, 25, 39, 235);
        hud.GetComponent<Image>().raycastTarget = false;
        Label("ROTWOOD", 17, new Vector2(0,-13));
        phaseLabel = Label("ANCIENT FOREST GUARDIAN", 11, new Vector2(0,-31));
        var bar = new GameObject("Health Fill", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(hud.transform, false);
        var barRect = (RectTransform)bar.transform;
        barRect.anchorMin = barRect.anchorMax = new Vector2(0,1);
        barRect.pivot = new Vector2(0,1);
        barRect.anchoredPosition = new Vector2(14,-43);
        barRect.sizeDelta = new Vector2(hudWidth - 28,20);
        healthFill = bar.GetComponent<Image>();
        var texture = Texture2D.whiteTexture;
        uiSprite = Sprite.Create(texture, new Rect(0,0,texture.width,texture.height), Vector2.one*0.5f);
        healthFill.sprite = uiSprite;
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.color = new Color32(224,141,49,255);
        healthFill.raycastTarget = false;
        healthLabel = Label("", 13, new Vector2(0,-53));
    }

    private Sprite uiSprite;
    private Text Label(string text, int size, Vector2 position)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(hud.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f,1);
        rect.sizeDelta = new Vector2(hudWidth - 20,22);
        rect.anchoredPosition = position;
        var label = go.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = size;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = text;
        return label;
    }

    private void OnDestroy()
    {
        if (boss != null) Destroy(boss.gameObject);
        if (hud != null) Destroy(hud);
        if (uiSprite != null) Destroy(uiSprite);
    }
}
