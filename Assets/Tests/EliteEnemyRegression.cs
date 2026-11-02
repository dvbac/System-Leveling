#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class EliteEnemyRegression : MonoBehaviour
{
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private bool background;
    private UnityEngine.Random.State randomState;

    [MenuItem("Tools/System Leveling/Run Elite Enemy Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<EliteEnemyRegression>() != null) return;
        var root = new GameObject("Elite regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root);
        root.AddComponent<EliteEnemyRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground; randomState = UnityEngine.Random.state;
        Application.runInBackground = true; Time.timeScale = 1f; UnityEngine.Random.InitState(41024);
        string before = Snapshot();
        var stack = new Stack<IEnumerator>(); stack.Push(Tests()); string failure = "";
        while (stack.Count > 0)
        {
            bool more = false; object wait = null;
            try { more = stack.Peek().MoveNext(); if (more) wait = stack.Peek().Current; }
            catch (Exception error) { failure = error.ToString(); }
            if (failure.Length > 0) break;
            if (!more) { stack.Pop(); continue; }
            if (wait is IEnumerator nested) { stack.Push(nested); continue; }
            yield return wait;
        }
        if (before != Snapshot()) failure += " Enemy prefab/combat-data assets changed.";
        Directory.CreateDirectory("Captures/EliteEnemies");
        File.WriteAllText("Captures/EliteEnemies/runtime.json", JsonUtility.ToJson(new Report
        { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("ELITE REGRESSION PASS: " + checks.Count);
        else Debug.LogError("ELITE REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        Shader shader = Resources.Load<Shader>("EliteSilhouette");
        Check(shader != null && shader.isSupported && !ShaderUtil.ShaderHasError(shader), "Red silhouette shader imports, compiles and is included through Resources");
        var invalid = new EliteSpawnSettings { sizeMultiplier = float.NaN, healthMultiplier = float.PositiveInfinity, speedMultiplier = -1, damageMultiplier = 10000 };
        invalid.Validate();
        Check(invalid.sizeMultiplier == 1.6f && invalid.healthMultiplier == 5f && invalid.speedMultiplier == 1f && invalid.damageMultiplier == 100f, "Invalid Inspector multipliers are bounded and finite");
        foreach (bool snow in new[] { false, true })
        {
            string path = snow ? LoadoutLauncher.SnowScene : LoadoutLauncher.GameplayScene;
            yield return SceneManager.LoadSceneAsync(path); yield return null;
            EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>(); spawner.enabled = false;
            Get<HordeSpawnSettings>(spawner, "horde").enabled = false;
            Set(spawner, "normalPopulationCap", 1);
            PlayerStats player = PlayerController.Instance.GetComponent<PlayerStats>();
            player.GetComponent<PlayerController>().enabled = false;
            player.GetComponent<Rigidbody2D>().simulated = false;
            foreach (WeaponController weapon in player.GetComponentsInChildren<WeaponController>()) weapon.enabled = false;
            Set(player, "isInvincible", true); Set(player, "invincibilityTimer", 100000f);
            EliteSpawnSettings settings = Get<EliteSpawnSettings>(spawner, "elite");
            Check(settings.enabled && settings.sizeMultiplier == 1.6f && settings.healthMultiplier == 5f && settings.speedMultiplier == 1.25f && settings.damageMultiplier == 2f, "Both authored maps enable the default elite preset");
            spawner.Tick(299f - spawner.ElapsedTime);
            Check(spawner.TotalEliteSpawned == 0, "No elite appears before five gameplay minutes");
            Time.timeScale = 0f; float elapsed = spawner.ElapsedTime; spawner.Tick(300f);
            Check(spawner.ElapsedTime == elapsed && spawner.TotalEliteSpawned == 0, "Pause freezes the elite schedule");
            Time.timeScale = 1f; spawner.Tick(1f);
            Check(spawner.ElapsedTime == 300f && spawner.TotalEliteSpawned == 1 && spawner.EliteAliveCount == 1, "Exactly one elite spawns at 05:00");
            EliteEnemy elite = FindAnyObjectByType<EliteEnemy>(); EnemyStats stats = elite.GetComponent<EnemyStats>();
            GameObject prefab = spawner.CurrentPhase.enemies.First(entry => entry.IsUsable && entry.prefab.GetComponent<EnemyStats>().enemyData == stats.enemyData).prefab;
            Check(snow == prefab.name.StartsWith("Snow") || snow && prefab.name == "IceSlime", "Elite uses the active map's enemy pool, never the other biome");
            Check(spawner.NormalAliveCount == 1 && spawner.TotalNormalSpawned == 1, "Elite has its own population and still spawns when the normal cap is full");
            Check(Mathf.Approximately(stats.currentHealth, stats.enemyData.MaxHealth * 5f) && Mathf.Approximately(stats.currentDamage, stats.enemyData.Damage * 2f) && Mathf.Approximately(stats.currentMoveSpeed, stats.enemyData.MoveSpeed * 1.25f), $"Elite has multiplied runtime HP, damage and movement speed ({prefab.name}: {stats.currentHealth}/{stats.currentDamage}/{stats.currentMoveSpeed})");
            Check(elite.transform.localScale == prefab.transform.localScale * 1.6f, "Elite scales the original map enemy and collider together");
            float hp = stats.currentHealth; elite.Configure(settings);
            Check(stats.currentHealth == hp && elite.transform.localScale == prefab.transform.localScale * 1.6f, "Configuring twice cannot stack elite bonuses");
            Vector3 spawn = elite.transform.position;
            Check(spawner.TryCameraRect(out Rect cameraRect) && !cameraRect.Contains(spawn), "Elite starts outside the gameplay camera");
            Check(!spawner.IsPositionValid(spawn), "Normal/Horde placement respects a living elite's separation");
            elite.transform.position = player.transform.position + Vector3.right * 3f;
            elite.GetComponent<Rigidbody2D>().simulated = false;
            yield return null;
            Vector3 start = elite.transform.position;
            yield return new WaitForSeconds(0.15f);
            Check(Vector3.Distance(elite.transform.position, player.transform.position) < Vector3.Distance(start, player.transform.position), "Elite follows the player with existing Enemy movement");
            SpriteRenderer visual = elite.GetComponent<SpriteRenderer>();
            SpriteRenderer[] edges = elite.GetComponentsInChildren<SpriteRenderer>().Where(renderer => renderer != visual).ToArray();
            Check(edges.Length == 8 && edges.All(edge => edge.sharedMaterial.shader == shader && edge.color.r == 1f && edge.color.g < 0.1f), "Eight red silhouettes form the outline, without tinting the original enemy");
            visual.flipX = !visual.flipX; elite.SendMessage("LateUpdate");
            Check(edges.All(edge => edge.sprite == visual.sprite && edge.flipX == visual.flipX && edge.sortingOrder == visual.sortingOrder - 1), "Outline follows animation frames and flipping behind the original sprite");
            stats.TakeDamage(1); yield return null;
            Check(!stats.IsDead && stats.currentHealth == hp - 1, "Elite uses the real damage and hit-animation path");
            yield return new WaitForEndOfFrame(); Capture(snow ? "snow-elite.png" : "grass-elite.png", visual);

            spawner.Tick(299f);
            Check(spawner.TotalEliteSpawned == 1, "No duplicate elite between five-minute milestones");
            spawner.Tick(1f);
            Check(spawner.TotalEliteSpawned == 2 && spawner.EliteAliveCount == 2, "At 10:00 another elite appears even if the previous one survives");
            spawner.enabled = true; spawner.enabled = false; spawner.Tick(0.01f);
            Check(spawner.TotalEliteSpawned == 2, "Disabling/re-enabling the spawner cannot reset or duplicate the schedule");
            spawner.Tick(900f);
            Check(spawner.TotalEliteSpawned == 3, "A long frame crosses multiple milestones without a catch-up burst");

            int balance = SoulWallet.Total; int runSoul = FindAnyObjectByType<GameOverController>().SoulEarned;
            stats.TakeDamage(float.MaxValue); stats.Kill();
            Check(stats.IsDead && SoulWallet.Total == balance + stats.SoulReward && FindAnyObjectByType<GameOverController>().SoulEarned == runSoul + stats.SoulReward, "Elite death pays the original configured Soul once in wallet and run summary");
            yield return new WaitForSeconds(1.2f);
            Check(elite == null, "Elite and its outline are destroyed after normal death playback");

            // Impossible placement remains pending, then retries safely rather than forcing a spawn.
            SpawnPlacementSettings placement = Get<SpawnPlacementSettings>(spawner, "placement");
            LayerMask oldMask = Get<LayerMask>(spawner, "obstacleLayers");
            var blocker = new GameObject("Elite test obstacle", typeof(BoxCollider2D));
            blocker.transform.position = player.transform.position;
            blocker.GetComponent<BoxCollider2D>().size = Vector2.one * 10000f;
            Set(spawner, "obstacleLayers", (LayerMask)(1 << blocker.layer)); Physics2D.SyncTransforms();
            int count = spawner.TotalEliteSpawned;
            spawner.Tick(1800f - spawner.ElapsedTime);
            Check(spawner.TotalEliteSpawned == count, "Blocked elite placement never forces a spawn inside obstacles");
            Object.DestroyImmediate(blocker); Set(spawner, "obstacleLayers", oldMask); Physics2D.SyncTransforms();
            spawner.Tick(1f);
            Check(spawner.TotalEliteSpawned == count + 1, "A blocked milestone retries successfully when safe space returns");
            float separation = placement.separation; placement.separation = 0f;
            Vector3 sample = player.transform.position + Vector3.right * 100f;
            UnityEngine.Tilemaps.Tilemap oldGround = Get<UnityEngine.Tilemaps.Tilemap>(spawner, "allowedGround");
            Collider2D oldArea = Get<Collider2D>(spawner, "allowedArea");
            Set(spawner, "allowedGround", null); Set(spawner, "allowedArea", null);
            var edgeObstacle = new GameObject("Expanded footprint test", typeof(BoxCollider2D)); edgeObstacle.layer = 31;
            edgeObstacle.transform.position = sample + Vector3.right * placement.clearanceRadius * 1.4f;
            edgeObstacle.GetComponent<BoxCollider2D>().size = Vector2.one * 0.02f;
            Set(spawner, "obstacleLayers", (LayerMask)(1 << 31)); Physics2D.SyncTransforms();
            Check(spawner.IsPositionValid(sample) && !spawner.IsPositionValid(sample, 1.6f), "Larger elite footprint rejects an obstacle that a normal enemy clears");
            Object.DestroyImmediate(edgeObstacle); Set(spawner, "obstacleLayers", oldMask);
            Set(spawner, "allowedGround", oldGround); Set(spawner, "allowedArea", oldArea);
            placement.separation = separation;
            count = spawner.TotalEliteSpawned; player.Kill(); elapsed = spawner.ElapsedTime; spawner.Tick(300f);
            Check(spawner.TotalEliteSpawned == count && spawner.ElapsedTime == elapsed, "Defeat freezes elite spawning and its clock");
            yield return SceneManager.LoadSceneAsync(path); yield return null;
            spawner = FindAnyObjectByType<EnemySpawner>(); spawner.enabled = false;
            Check(spawner.TotalEliteSpawned == 0 && spawner.EliteAliveCount == 0 && spawner.ElapsedTime < 1f, "Fresh run resets elite schedule, counters and population");
            FindAnyObjectByType<GameOverController>().Win(); spawner.Tick(300f);
            Check(spawner.TotalEliteSpawned == 0, "Victory also blocks elite spawns");
        }
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SelectionScene);
    }

    private void Capture(string name, SpriteRenderer actor)
    {
        Directory.CreateDirectory("Captures/EliteEnemies"); Texture2D image = ScreenCapture.CaptureScreenshotAsTexture();
        try
        {
            File.WriteAllBytes("Captures/EliteEnemies/" + name, image.EncodeToPNG());
            Vector3 min = Camera.main.WorldToScreenPoint(actor.bounds.min), max = Camera.main.WorldToScreenPoint(actor.bounds.max);
            int red = 0;
            for (int y = Mathf.Max(0, (int)min.y - 8); y < Mathf.Min(image.height, (int)max.y + 8); y++)
                for (int x = Mathf.Max(0, (int)min.x - 8); x < Mathf.Min(image.width, (int)max.x + 8); x++)
                {
                    Color pixel = image.GetPixel(x, y);
                    if (pixel.r > 0.7f && pixel.g < 0.2f && pixel.b < 0.2f) red++;
                }
            Check(red > 10, "Actual rendered elite has visible red outline pixels, not hidden behind terrain");
        }
        finally { Destroy(image); }
    }
    private static string Snapshot() => string.Join("", Directory.GetFiles("Assets/Prefab/ReferenceEnemies", "*.prefab", SearchOption.AllDirectories)
        .Concat(Directory.GetFiles("Assets/Scriptable Object/Enemy", "*.asset", SearchOption.AllDirectories)).OrderBy(path => path).Select(File.ReadAllText));
    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    private void Check(bool pass, string label) { if (!pass) throw new Exception(label); checks.Add(label); }
    private void OnDestroy() { Application.runInBackground = background; Time.timeScale = 1f; UnityEngine.Random.state = randomState; }
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
