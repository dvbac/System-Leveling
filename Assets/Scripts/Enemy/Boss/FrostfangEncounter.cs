using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class FrostfangEncounter : MonoBehaviour
{
    [SerializeField] private FrostfangBoss bossPrefab;
    [SerializeField, Min(0)] private float spawnAtSeconds = 1020;
    [SerializeField] private string groundTilemapName = "Tilemap";
    [SerializeField] private LayerMask obstacleLayers;
    private PlayerStats player;
    private FrostfangBoss boss;
    private bool spawned, victorySent;
    private float retryAt, refreshAt = -1;
    private readonly List<Tilemap> grounds = new List<Tilemap>();
    private GameObject hud;
    private Image healthFill;
    private Text healthLabel, actionLabel;
    private Sprite uiSprite;
    public float ElapsedTime { get; private set; }
    public bool HasSpawned => spawned;
    public FrostfangBoss Boss => boss;

    private void Update()
    {
        Tick(Time.deltaTime);
        if (hud == null) return;
        hud.SetActive(boss != null && !GameOverController.GameplayStopped);
        if (boss == null) return;
        float max = boss.Stats.enemyData.MaxHealth;
        healthFill.fillAmount = Mathf.Clamp01(boss.Stats.currentHealth / max);
        healthLabel.text = $"{Mathf.CeilToInt(Mathf.Max(0, boss.Stats.currentHealth))} / {max:0}";
        actionLabel.text = boss.State == FrostfangBoss.ActionState.PounceWindup ? "RENDING POUNCE" :
            boss.State == FrostfangBoss.ActionState.BreathWindup || boss.State == FrostfangBoss.ActionState.Breath ? "FREEZING BREATH" :
            boss.State == FrostfangBoss.ActionState.Pillars ? "ICE PILLARS" :
            boss.State == FrostfangBoss.ActionState.Howl || boss.State == FrostfangBoss.ActionState.Blizzard ? "WHITEOUT" : "THE FROZEN ALPHA";
    }

    internal void Tick(float delta)
    {
        if (player == null && PlayerController.Instance != null) player = PlayerController.Instance.GetComponent<PlayerStats>();
        if (GameOverController.GameplayStopped || player == null || player.IsDead || !player.gameObject.activeInHierarchy ||
            Time.timeScale <= 0 || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        ElapsedTime += delta;
        if (!spawned && ElapsedTime >= spawnAtSeconds && ElapsedTime >= retryAt)
        { retryAt = ElapsedTime + 1; SpawnNow(); }
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
            Vector3 point = player.transform.position + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * 6;
            if (!IsOnGround(point) || !IsOnGround(point + Vector3.up) || !IsOnGround(point - Vector3.up) ||
                !IsOnGround(point + Vector3.right) || !IsOnGround(point - Vector3.right) ||
                (obstacleLayers.value != 0 && Physics2D.OverlapCircle(point, 1, obstacleLayers))) continue;
            boss = Instantiate(bossPrefab, point, Quaternion.identity);
            boss.Configure(player, this, obstacleLayers); spawned = true;
            BuildHud(); Debug.Log("FROSTFANG ARRIVED at survival time " + ElapsedTime.ToString("F1")); return true;
        }
        return false;
    }

    public bool IsOnGround(Vector3 point)
    {
        if (Time.time >= refreshAt)
        {
            refreshAt = Time.time + 1; grounds.Clear();
            foreach (var map in FindObjectsByType<Tilemap>())
                if (map.name == groundTilemapName) grounds.Add(map);
        }
        foreach (var map in grounds)
            if (map != null && map.gameObject.activeInHierarchy && map.HasTile(map.WorldToCell(point))) return true;
        return false;
    }

    public void BossDefeated()
    {
        if (victorySent || player == null || player.IsDead || GameOverController.GameplayStopped) return;
        victorySent = true;
        var result = FindAnyObjectByType<GameOverController>();
        if (result != null) result.Win("FROSTFANG ALPHA");
    }

    private void BuildHud()
    {
        var playerHud = FindAnyObjectByType<PlayerHud>();
        Canvas canvas = playerHud != null ? playerHud.GetComponentInParent<Canvas>() : null;
        if (canvas == null) { Debug.LogError("Frostfang needs the gameplay HUD Canvas."); return; }
        hud = new GameObject("Frostfang Boss Health", typeof(RectTransform), typeof(Image));
        hud.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)hud.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1); rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -64); rect.sizeDelta = new Vector2(280, 66);
        var background = hud.GetComponent<Image>(); background.color = new Color32(12, 25, 39, 225); background.raycastTarget = false;
        Label("FROSTFANG ALPHA", 16, -11);
        actionLabel = Label("THE FROZEN ALPHA", 10, -28);
        var bar = new GameObject("Health Fill", typeof(RectTransform), typeof(Image)); bar.transform.SetParent(hud.transform, false);
        var barRect = (RectTransform)bar.transform; barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 1);
        barRect.anchoredPosition = new Vector2(0, -49); barRect.sizeDelta = new Vector2(256, 18);
        healthFill = bar.GetComponent<Image>(); var texture = Texture2D.whiteTexture;
        uiSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        healthFill.sprite = uiSprite; healthFill.type = Image.Type.Filled; healthFill.fillMethod = Image.FillMethod.Horizontal;
        healthFill.color = new Color32(80, 205, 240, 255); healthFill.raycastTarget = false;
        healthLabel = Label("", 12, -49);
    }

    private Text Label(string value, int size, float y)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text)); go.transform.SetParent(hud.transform, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(260, 20);
        var text = go.GetComponent<Text>(); text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size; text.alignment = TextAnchor.MiddleCenter; text.color = Color.white;
        text.raycastTarget = false; text.text = value; return text;
    }

    private void OnDestroy()
    {
        if (boss != null) Destroy(boss.gameObject);
        if (hud != null) Destroy(hud);
        if (uiSprite != null) Destroy(uiSprite);
    }
}
