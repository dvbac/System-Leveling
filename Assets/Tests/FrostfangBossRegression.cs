#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FrostfangBossRegression : MonoBehaviour
{
    private const string Output = "Captures/FrostfangBoss";
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private readonly List<GameObject> fixtures = new List<GameObject>();
    private bool background;
    private float scale;

    [MenuItem("Tools/System Leveling/Frostfang/Run Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<FrostfangBossRegression>() != null) return;
        var go = new GameObject("Frostfang Regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(go);
        var runner = go.AddComponent<FrostfangBossRegression>();
        runner.background = Application.runInBackground; runner.scale = Time.timeScale;
        Application.runInBackground = true; Time.timeScale = 1;
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
        EditorApplication.QueuePlayerLoopUpdate();
    }

    private IEnumerator Start()
    {
        Directory.CreateDirectory(Output); var run = Tests(); string failure = "";
        while (true)
        {
            object current;
            try { if (!run.MoveNext()) break; current = run.Current; }
            catch (Exception error) { failure = error.ToString(); break; }
            yield return current;
        }
        foreach (var fixture in fixtures) if (fixture != null) Destroy(fixture);
        Time.timeScale = scale; Application.runInBackground = background;
        File.WriteAllText(Output + "/runtime-regression.json", JsonUtility.ToJson(new Report
        { result = failure == "" ? "PASS" : "FAIL", checks = checks.ToArray(), failure = failure }, true));
        if (failure == "") Debug.Log("FROSTFANG REGRESSION PASS: " + checks.Count);
        else Debug.LogError("FROSTFANG REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        string source = SourceSnapshot(), grass = File.ReadAllText(LoadoutLauncher.GameplayScene), snow = File.ReadAllText(LoadoutLauncher.SnowScene);
        int persistedSoul = PlayerPrefs.GetInt("SystemLeveling.Soul.Total.v1", 0);
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.GameplayScene); yield return null;
        Check(FindAnyObjectByType<FrostfangEncounter>() == null, "AC-001: forest map has no Frostfang encounter");
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.SnowScene); yield return null;
        var player = PlayerController.Instance.GetComponent<PlayerStats>();
        var movement = player.GetComponent<PlayerController>(); movement.enabled = false;
        player.GetComponent<Rigidbody2D>().simulated = false; player.currentRecovery = 0; player.currentHealth = 1000;
        foreach (var spawner in FindObjectsByType<EnemySpawner>()) spawner.enabled = false;
        foreach (var enemy in FindObjectsByType<Enemy>()) enemy.enabled = false;
        foreach (var weapon in player.GetComponentsInChildren<WeaponController>()) weapon.enabled = false;
        Check(!GameOverController.GameplayStopped && !player.IsDead, "AC-001: snow starts alive");
        var encounter = FindAnyObjectByType<FrostfangEncounter>(); encounter.enabled = false;
        Check(Read<float>(encounter, "spawnAtSeconds") == 1020, "AC-001: authored spawn is 17 minutes");
        float elapsed = encounter.ElapsedTime; Time.timeScale = 0; encounter.Tick(1000);
        Check(encounter.ElapsedTime == elapsed && !encounter.HasSpawned, "AC-001: paused spawn clock does not advance");
        Time.timeScale = 1; encounter.Tick(900 - elapsed);
        Check(!encounter.HasSpawned, "AC-001: old minute-15 milestone no longer spawns a boss");
        encounter.Tick(119);
        Check(!encounter.HasSpawned, "AC-001: no early spawn at 1019 seconds");
        encounter.Tick(1.01f);
        Check(encounter.HasSpawned && encounter.Boss != null, "AC-001: one boss at 1020 seconds on streamed snow ground");
        var boss = encounter.Boss; boss.enabled = false; var stats = boss.Stats;
        Check(!encounter.SpawnNow() && FindObjectsByType<FrostfangBoss>().Length == 1, "AC-001: repeated spawn cannot duplicate boss");
        Check(FindObjectsByType<RotwoodEncounter>().All(e => !e.enabled), "AC-006: forest encounter stays disabled on snow");
        var motions = Read<FrostfangBoss.Sequence[]>(boss, "motions");
        Check(motions.Length == 7 && motions.Select(s => s.frames.Length).SequenceEqual(new[] { 8, 8, 12, 16, 10, 16, 12 }), "AC-002: seven motions have actual source frame counts");
        Check(motions.All(s => s.frames.All(f => f != null)) && boss.GetComponent<Enemy>() == null && boss.GetComponent<EnemyAnimation>() == null && boss.GetComponent<Animator>() == null,
            "AC-002: valid sprites, no competing ordinary AI or Animator");
        foreach (string field in new[] { "impactFrames", "breathFrames", "pillarFrames", "blizzardFrames" })
            Check(Read<Sprite[]>(boss, field).Length >= 8 && Read<Sprite[]>(boss, field).All(s => s != null), "AC-002: imported FX " + field);
        var visual = boss.GetComponent<SpriteRenderer>(); Sprite before = visual.sprite; Call(boss, "Animate", 0.2f);
        Check(visual.sprite != before, "AC-002: animation actually advances");
        Time.timeScale = 0; before = visual.sprite; Call(boss, "Animate", 0.5f);
        Check(visual.sprite == before, "AC-004: body animation pauses"); Time.timeScale = 1;
        encounter.SendMessage("Update"); Canvas.ForceUpdateCanvases();
        Check(Read<Image>(encounter, "healthFill") != null && Read<GameObject>(encounter, "hud").GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget), "AC-005: boss HP exists and never blocks shooting input");
        Rect bossBounds = Bounds(Read<GameObject>(encounter, "hud").GetComponent<RectTransform>());
        var hud = FindAnyObjectByType<PlayerHud>();
        Check(!bossBounds.Overlaps(Bounds((RectTransform)hud.transform.Find("HUD/Health Panel"))), "AC-005: boss HP does not overlap player HP");

        // Deterministic geometry fixtures use the same production code, without streamed-ground timing.
        boss.Configure(player, null, 0); boss.enabled = false;
        Vector3 anchor = player.transform.position;
        boss.transform.position = anchor + Vector3.left * 4;
        Enter(boss, FrostfangBoss.ActionState.Chase); boss.Tick(2.51f);
        Check(boss.State == FrostfangBoss.ActionState.PounceWindup, "AC-003: natural attack cycle begins with telegraphed pounce");
        boss.transform.position = anchor + Vector3.left * 4; Enter(boss, FrostfangBoss.ActionState.PounceWindup);
        Vector2 landing = Read<Vector2>(boss, "landing"); float hp = player.currentHealth;
        player.transform.position += Vector3.up * 4;
        Time.timeScale = 0; boss.Tick(2);
        Check(boss.State == FrostfangBoss.ActionState.PounceWindup, "AC-003: pounce windup pauses");
        Time.timeScale = 1; boss.Tick(0.76f);
        Check(boss.State == FrostfangBoss.ActionState.Pounce && Read<Vector2>(boss, "landing") == landing && player.currentHealth == hp, "AC-003: target locked during windup; no early damage");
        boss.Tick(0.51f);
        Check(boss.State == FrostfangBoss.ActionState.Recover && player.currentHealth == hp && Vector2.Distance(boss.transform.position, landing) < 0.01f,
            $"AC-003: dodging landing circle avoids damage (state={boss.State}, hp={player.currentHealth}/{hp}, position={boss.transform.position}, landing={landing})");
        boss.transform.position = anchor + Vector3.left * 4; player.transform.position = anchor;
        Enter(boss, FrostfangBoss.ActionState.PounceWindup); boss.Tick(0.76f); Set(player, "isInvincible", false); hp = player.currentHealth; boss.Tick(0.51f);
        Check(player.currentHealth == hp - 25, "AC-003: pounce hits once at landing");
        boss.Tick(0.1f); Check(player.currentHealth == hp - 25, "AC-003: pounce cannot repeatedly hit during recovery");
        var wall = Fixture("Frostfang Test Wall"); wall.layer = 31; wall.transform.position = anchor + Vector3.left * 2;
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 6);
        boss.transform.position = anchor + Vector3.left * 4; Set(boss, "obstacleLayers", (LayerMask)(1 << 31)); Physics2D.SyncTransforms();
        Enter(boss, FrostfangBoss.ActionState.PounceWindup); boss.Tick(0.76f); boss.Tick(0.51f);
        Check(boss.transform.position.x < wall.transform.position.x - 0.85f, "AC-003: swept pounce stops before wall");
        Set(boss, "obstacleLayers", (LayerMask)0); Destroy(wall);

        boss.transform.position = anchor + Vector3.left * 2; player.transform.position = boss.transform.position + new Vector3(3.8f, 0.9f);
        Enter(boss, FrostfangBoss.ActionState.BreathWindup); Vector2 aim = Read<Vector2>(boss, "aim");
        Vector2 mouth = (Vector2)boss.transform.position + new Vector2(0.8f, 0.9f);
        Check(boss.InBreath(mouth + aim * 3) && !boss.InBreath(mouth + aim * 6) && !boss.InBreath(mouth - aim) && !boss.InBreath(mouth + aim * 3 + Vector2.up * 2), "AC-004: cone geometry excludes behind, beyond range and outside edges");
        hp = player.currentHealth; boss.Tick(0.74f); Check(player.currentHealth == hp, "AC-004: breath windup cannot damage");
        boss.Tick(0.02f); Set(player, "isInvincible", false); boss.Tick(0.01f);
        Check(boss.State == FrostfangBoss.ActionState.Breath && player.currentHealth == hp - 10, "AC-004: breath starts after warning and damages in cone");
        hp = player.currentHealth; Set(player, "isInvincible", false); boss.Tick(0.2f);
        Check(player.currentHealth == hp, "AC-004: breath rate is bounded, not damage per frame");
        boss.Tick(0.31f); Check(player.currentHealth == hp - 10, "AC-004: next breath damage at 0.5s interval");
        player.transform.position = mouth + aim * 3 + Vector2.up * 3; hp = player.currentHealth; Set(player, "isInvincible", false); boss.Tick(0.51f);
        Check(player.currentHealth == hp && Read<Vector2>(boss, "aim") == aim, "AC-004: dodge cone avoids damage and aim stays locked");
        Call(boss, "Animate", 0.1f); yield return new WaitForEndOfFrame(); Capture("breath.png");
        var effect = Read<List<FrostfangEffect>>(boss, "effects").Last(e => e != null); effect.enabled = false;
        float effectTime = Read<float>(effect, "elapsed"); Time.timeScale = 0; effect.Tick(1); boss.Tick(1);
        Check(Read<float>(effect, "elapsed") == effectTime && boss.State == FrostfangBoss.ActionState.Breath, "AC-004: pause freezes FX and damage state"); Time.timeScale = 1;
        boss.Tick(1); Check(boss.State == FrostfangBoss.ActionState.Recover, "AC-004: breath ends within authored duration");

        player.transform.position = anchor; Enter(boss, FrostfangBoss.ActionState.Pillars);
        var pillars = Read<List<FrostfangIcePillar>>(boss, "pillars"); foreach (var pillar in pillars) pillar.enabled = false;
        Check(pillars.Count == 3 && pillars.All(p => !p.HasStruck), "AC-004: three pillar warnings before strikes");
        var first = pillars[0]; player.transform.position = first.transform.position; Set(player, "isInvincible", false); hp = player.currentHealth;
        first.Tick(0.74f); Check(player.currentHealth == hp && !first.HasStruck, "AC-004: pillar cannot hit before warning");
        Time.timeScale = 0; first.Tick(2); Check(!first.HasStruck, "AC-004: pillar warning pauses"); Time.timeScale = 1;
        float speed = player.currentMoveSpeed; first.Tick(0.02f);
        Check(first.HasStruck && player.currentHealth == hp - 20 && movement.MovementMultiplier == 0.6f, "AC-004: pillar strikes once and applies temporary slow");
        Set(player, "isInvincible", false); first.Tick(0.5f); Check(player.currentHealth == hp - 20, "AC-004: held pillar does not repeatedly damage");
        player.transform.position = anchor + Vector3.up * 4; hp = player.currentHealth; pillars[1].Tick(1);
        Check(player.currentHealth == hp, "AC-004: leaving pillar warning avoids strike");
        Call(boss, "Animate", 0.65f); yield return new WaitForEndOfFrame(); Capture("pillars.png");
        player.currentMoveSpeed += 0.5f; yield return new WaitForSeconds(1.25f);
        Check(movement.MovementMultiplier == 1 && player.currentMoveSpeed == speed + 0.5f, "AC-004: slow expires without losing movement upgrade");

        player.transform.position = anchor; boss.transform.position = anchor + Vector3.left * 2;
        Enter(boss, FrostfangBoss.ActionState.Howl); hp = player.currentHealth; boss.Tick(0.99f);
        Check(player.currentHealth == hp && boss.State == FrostfangBoss.ActionState.Howl, "AC-004: howl warns for one second without damage");
        boss.Tick(0.02f); Set(player, "isInvincible", false); boss.Tick(0.01f);
        Check(boss.State == FrostfangBoss.ActionState.Blizzard && player.currentHealth == hp - 6, "AC-004: blizzard damages inside radius after howl");
        Set(player, "isInvincible", false); hp = player.currentHealth; boss.Tick(0.5f);
        Check(player.currentHealth == hp, "AC-004: blizzard damage bounded by 0.75s interval");
        player.transform.position = boss.transform.position + Vector3.up * 9; boss.Tick(0.3f);
        Check(player.currentHealth == hp, "AC-004: outside blizzard radius avoids damage");
        player.transform.position = anchor; Call(boss, "Animate", 0.1f);
        yield return new WaitForEndOfFrame(); Capture("blizzard.png");
        boss.Tick(4); Check(boss.State == FrostfangBoss.ActionState.Recover, "AC-004: blizzard finishes at four seconds");

        var slash = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Slash.prefab")).GetComponent<MeleeWeaponBehaviour>(); fixtures.Add(slash.gameObject); slash.enabled = false;
        var sword = AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Sword.asset");
        slash.Initialize(sword, player.transform, Vector2.right); hp = stats.currentHealth;
        Call(slash, "TryHit", boss.GetComponent<Collider2D>()); float afterHit = stats.currentHealth; Call(slash, "TryHit", boss.GetComponent<Collider2D>());
        Check(afterHit < hp && stats.currentHealth == afterHit, "AC-005: real melee collision hits once per swing");
        var spell = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Spell.prefab")).GetComponent<ProjectileWeaponBehaviour>(); fixtures.Add(spell.gameObject); spell.enabled = false;
        hp = stats.currentHealth; Call(spell, "OnTriggerEnter2D", boss.GetComponent<Collider2D>());
        Check(stats.currentHealth < hp, "AC-005: real projectile collision damages boss");
        encounter.SendMessage("Update"); Check(Mathf.Approximately(Read<Image>(encounter, "healthFill").fillAmount, stats.currentHealth / stats.enemyData.MaxHealth), "AC-005: HP fill follows actual stats");
        var ordinary = Fixture("Ordinary Enemy Death"); ordinary.SetActive(false); var ordinaryStats = ordinary.AddComponent<EnemyStats>(); ordinaryStats.enemyData = stats.enemyData; ordinary.SetActive(true); ordinaryStats.Kill();
        yield return null; Check(ordinary == null, "AC-006: ordinary enemy still dies immediately without animation");
        if (slash != null) Destroy(slash.gameObject);
        if (spell != null) Destroy(spell.gameObject);
        Set(boss, "encounter", encounter); encounter.enabled = true;
        stats.GetComponent<DropRateManager>().drops.Clear();
        int souls = SoulWallet.Total, deaths = 0; boss.DeathFinished += () => deaths++;
        Enter(boss, FrostfangBoss.ActionState.Pillars); Enter(boss, FrostfangBoss.ActionState.Blizzard);
        stats.Kill(); stats.Kill(); stats.TakeDamage(99999);
        Check(stats.IsDead && boss.State == FrostfangBoss.ActionState.Dead && !boss.GetComponent<Collider2D>().enabled && !boss.GetComponent<Rigidbody2D>().simulated, "AC-005: death is terminal and disables collisions");
        Check(SoulWallet.Total == souls + stats.SoulReward, "AC-005: repeated Kill awards Soul once in isolated test wallet");
        Check(!GameOverController.GameplayStopped, "AC-005: victory waits for Death animation");
        boss.enabled = true; yield return null;
        Check(FindObjectsByType<FrostfangEffect>().Length == 0 && FindObjectsByType<FrostfangIcePillar>().Length == 0, "AC-005: death cleans all FX and pillars");
        yield return new WaitForSeconds(1.6f);
        var result = FindAnyObjectByType<GameOverController>();
        Check(result.IsGameOver && result.HasWon && deaths == 1, "AC-005: Death completes once then VICTORY");
        Check(Read<Text>(result, "resultText").text.Contains("FROSTFANG ALPHA DEFEATED") && !Read<Text>(result, "resultText").text.Contains("ROTWOOD"),
            "AC-005: victory names Frostfang, not the forest boss");
        Check(!Read<GameObject>(encounter, "hud").activeSelf && Read<Button>(result, "restartButton").interactable, "AC-005: HP hides and restart remains usable after victory");
        yield return new WaitForEndOfFrame(); Capture("victory.png");
        result.Restart(); float timeout = Time.realtimeSinceStartup + 15;
        while (FindAnyObjectByType<GameOverController>() == result && Time.realtimeSinceStartup < timeout) yield return null;
        yield return null;
        var fresh = FindAnyObjectByType<GameOverController>();
        Check(fresh != result && !fresh.IsGameOver && !FindAnyObjectByType<FrostfangEncounter>().HasSpawned, "AC-005: restart resets encounter");
        var freshEncounter = FindAnyObjectByType<FrostfangEncounter>();
        Check(freshEncounter.SpawnNow(), "AC-005: fresh encounter can spawn after restart");
        var freshBoss = freshEncounter.Boss;
        Enter(freshBoss, FrostfangBoss.ActionState.Pillars); Enter(freshBoss, FrostfangBoss.ActionState.Blizzard);
        PlayerController.Instance.GetComponent<PlayerStats>().Kill(); fresh.Win();
        Check(fresh.IsGameOver && !fresh.HasWon, "AC-005: player defeat cannot be overwritten by boss victory");
        freshBoss.Tick(0.01f); yield return null;
        Check(FindObjectsByType<FrostfangEffect>().Length == 0 && FindObjectsByType<FrostfangIcePillar>().Length == 0,
            "AC-005: defeat also cleans active FX and pillars");
        Check(SourceSnapshot() == source && File.ReadAllText(LoadoutLauncher.GameplayScene) == grass && File.ReadAllText(LoadoutLauncher.SnowScene) == snow, "AC-006: source art and authored scenes preserved by regression");
        Check(PlayerPrefs.GetInt("SystemLeveling.Soul.Total.v1", 0) == persistedSoul, "AC-006: real persisted Soul balance unchanged by regression");
    }

    private GameObject Fixture(string name) { var go = new GameObject(name); fixtures.Add(go); return go; }
    private void Check(bool condition, string label) { if (!condition) throw new InvalidOperationException(label); checks.Add(label); }
    private static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Flags).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Flags).Invoke(target, args);
    private static void Enter(FrostfangBoss boss, FrostfangBoss.ActionState state)
    { Physics2D.SyncTransforms(); Call(boss, "Enter", state); }
    private static Rect Bounds(RectTransform rect) { var p = new Vector3[4]; rect.GetWorldCorners(p); return Rect.MinMaxRect(p[0].x, p[0].y, p[2].x, p[2].y); }
    private static void Capture(string name) { var texture = ScreenCapture.CaptureScreenshotAsTexture(); try { File.WriteAllBytes(Output + "/" + name, texture.EncodeToPNG()); } finally { Destroy(texture); } }
    private static string SourceSnapshot()
    {
        using (var sha = SHA256.Create()) return string.Join("\n", Directory.GetFiles("Assets/MyAssets/Monster/Boss/Frostfang", "*.png").OrderBy(p => p)
            .Select(p => p + ":" + BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)))));
    }
    [Serializable] private class Report { public string result; public string[] checks; public string failure; }
}
#endif
