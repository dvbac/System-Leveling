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
using Object = UnityEngine.Object;

// Tests production ticks/physics/combat and the real win -> restart -> loss UI flow.
public class RotwoodBossRegression : MonoBehaviour
{
    private const string Output = "Captures/RotwoodBoss";
    private readonly List<string> checks = new List<string>();
    private readonly List<GameObject> fixtures = new List<GameObject>();
    private bool background;
    private float scale;
    private string sourceSnapshot;
    private string sceneText;

    [MenuItem("Tools/System Leveling/Rotwood/Run Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<RotwoodBossRegression>() != null) return;
        var go = new GameObject("Rotwood Regression", typeof(SoulTestIsolation));
        DontDestroyOnLoad(go);
        var runner = go.AddComponent<RotwoodBossRegression>();
        runner.background = Application.runInBackground;
        Application.runInBackground = true;
        EditorApplication.QueuePlayerLoopUpdate();
        EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();
    }

    private IEnumerator Start()
    {
        scale = Time.timeScale;
        Application.runInBackground = true;
        Time.timeScale = 1;
        sourceSnapshot = SourceSnapshot();
        sceneText = File.ReadAllText(LoadoutLauncher.GameplayScene);
        Directory.CreateDirectory(Output);
        var run = Tests();
        string failure = "";
        while (true)
        {
            object current;
            try { if (!run.MoveNext()) break; current = run.Current; }
            catch (Exception error) { failure = error.ToString(); break; }
            yield return current;
        }
        foreach (var fixture in fixtures) if (fixture != null) Destroy(fixture);
        Application.runInBackground = background;
        Time.timeScale = scale;
        File.WriteAllText(Output + "/runtime-regression.json", JsonUtility.ToJson(new Report
        { result = failure == "" ? "PASS" : "FAIL", checks = checks.ToArray(), failure = failure }, true));
        if (failure == "") Debug.Log($"ROTWOOD REGRESSION PASS: {checks.Count} checks.");
        else Debug.LogError("ROTWOOD REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        // A manual test may be launched after the previous run already ended.
        yield return SceneManager.LoadSceneAsync(LoadoutLauncher.GameplayScene);
        yield return null;
        PlayerStats player = PlayerController.Instance.GetComponent<PlayerStats>();
        Check(!player.IsDead && !GameOverController.GameplayStopped, "Fresh gameplay scene starts alive before regression");
        var movement = player.GetComponent<PlayerController>();
        movement.enabled = false;
        player.GetComponent<Rigidbody2D>().simulated = false;
        foreach (var enemy in FindObjectsByType<Enemy>()) enemy.enabled = false;
        foreach (var spawn in FindObjectsByType<EnemySpawner>()) spawn.enabled = false;
        foreach (var weapon in player.GetComponentsInChildren<WeaponController>()) weapon.enabled = false;
        player.currentRecovery = 0;
        player.currentHealth = 1000;
        var encounter = FindAnyObjectByType<RotwoodEncounter>();
        encounter.enabled = false;
        Check(Read<float>(encounter,"spawnAtSeconds") == 900f, "AC-001: authored encounter uses minute 15");
        float elapsed = encounter.ElapsedTime;
        Time.timeScale = 0;
        encounter.Tick(1000);
        Check(encounter.ElapsedTime == elapsed && !encounter.HasSpawned, "AC-001: pause does not advance encounter");
        Time.timeScale = 1;
        encounter.Tick(899f-elapsed);
        Check(!encounter.HasSpawned, "AC-001: no early spawn before 900 seconds");
        encounter.Tick(1.01f);
        Check(encounter.HasSpawned && encounter.Boss != null, "AC-001: spawns once after 900s");
        RotwoodBoss boss = encounter.Boss;
        boss.enabled = false;
        Check(!encounter.SpawnNow() && FindObjectsByType<RotwoodBoss>().Length == 1, "AC-001: repeated spawn cannot duplicate boss");
        EnemyStats stats = boss.Stats;
        Check(boss.GetComponent<Enemy>() == null && boss.GetComponent<EnemyAnimation>() == null,
            "AC-002: boss AI/animation has no legacy component competing for control");
        var sequences = Read<RotwoodBoss.Sequence[]>(boss,"motions");
        Check(sequences.Length == 7 && sequences.All(s => s.frames.Length > 0 && s.frames.All(f=>f!=null)),
            "AC-002: seven normalized animation sequences have valid sprites");
        Check(sequences[0].sound != null && sequences[2].sound != null && sequences[3].sound != null && sequences[4].sound != null && sequences[6].sound != null,
            "AC-002: authored walk/charge/stagger/death sounds connected");
        encounter.SendMessage("Update");
        Check(Read<Image>(encounter,"healthFill") != null, "AC-002: boss health UI exists");
        Transform hudRoot = FindAnyObjectByType<PlayerHud>().transform;
        Canvas.ForceUpdateCanvases();
        Rect bossBounds = ScreenBounds(Read<GameObject>(encounter,"hud").GetComponent<RectTransform>());
        foreach (string path in new[] { "HUD/Health Panel", "Weapon Panel", "HUD/Run Timer", "HUD/Soul Balance" })
        {
            Transform panel = hudRoot.Find(path);
            if (panel != null) Check(!bossBounds.Overlaps(ScreenBounds(panel.GetComponent<RectTransform>())), "Boss health does not overlap " + path);
        }
        encounter.enabled = true;
        Sprite before = boss.GetComponent<SpriteRenderer>().sprite;
        Call(boss,"Animate",0.2f);
        Check(boss.GetComponent<SpriteRenderer>().sprite != before, "AC-002: walk sprites actually advance");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Output + "/boss-health.png");

        // Keep later deterministic fixtures independent of streamed tilemap coordinates.
        boss.Configure(player,null,null,0);
        boss.enabled = false;
        Enter(boss,RotwoodBoss.ActionState.Chase);
        boss.Tick(3.1f);
        Check(boss.State == RotwoodBoss.ActionState.Roots, "AC-003: natural chase selects root attack");
        var attackRoots = Read<List<RotwoodRoot>>(boss,"roots");
        foreach (var root in attackRoots) root.enabled = false;
        Check(attackRoots.Count == 3 && attackRoots.All(r=>!r.HasStruck), "AC-003: three locked-position root warnings precede strikes");
        var rootHit = Fixture("Root Hit").AddComponent<RotwoodRoot>();
        rootHit.enabled = false;
        rootHit.transform.position = player.transform.position;
        rootHit.Configure(player,Read<Sprite>(boss,"warningRing"),Read<Sprite>(boss,"rootSprite"),0.8f,0.85f,5);
        Set(player,"isInvincible",false);
        float hp = player.currentHealth;
        rootHit.Tick(0.79f);
        Check(player.currentHealth == hp && !rootHit.HasStruck, "AC-003: roots do not damage before warning ends");
        Time.timeScale = 0;
        rootHit.Tick(1f);
        Check(!rootHit.HasStruck, "AC-003: roots pause with gameplay");
        Time.timeScale = 1;
        float speed = player.currentMoveSpeed;
        rootHit.Tick(0.02f);
        Check(player.currentHealth == hp-5 && movement.MovementMultiplier == 0.6f, "AC-003: root strike damages and slows");
        rootHit.Tick(0.1f);
        Check(player.currentHealth == hp-5, "AC-003: each root hits only once");
        var miss = Fixture("Root Miss").AddComponent<RotwoodRoot>();
        miss.enabled = false;
        miss.transform.position = player.transform.position + Vector3.right * 5;
        miss.Configure(player,Read<Sprite>(boss,"warningRing"),Read<Sprite>(boss,"rootSprite"),0.8f,0.85f,5);
        miss.Tick(0.81f);
        Check(player.currentHealth == hp-5, "AC-003: moving outside root warning avoids damage");
        player.currentMoveSpeed += 0.5f;
        yield return new WaitForSeconds(1.25f);
        Check(movement.MovementMultiplier == 1f && player.currentMoveSpeed == speed+0.5f,
            "AC-003: slow expires without losing movement upgrades");
        Destroy(rootHit.gameObject);
        Destroy(miss.gameObject);

        Vector3 anchor = player.transform.position;
        boss.transform.position = anchor + Vector3.left*4;
        Enter(boss,RotwoodBoss.ActionState.Windup);
        Vector2 aim = Read<Vector2>(boss,"chargeDirection");
        player.transform.position += Vector3.up*3;
        Time.timeScale = 0;
        boss.Tick(2);
        Check(boss.State == RotwoodBoss.ActionState.Windup, "AC-004: charge windup pauses");
        Time.timeScale = 1;
        boss.Tick(1.01f);
        Check(boss.State == RotwoodBoss.ActionState.Charge && Read<Vector2>(boss,"chargeDirection") == aim,
            "AC-004: charge locks direction despite target moving");
        boss.Tick(1f);
        Check(boss.State == RotwoodBoss.ActionState.Stagger, "AC-004: charge ends after bounded travel");
        stats.currentHealth = stats.enemyData.MaxHealth;
        boss.Tick(1.51f);
        Check(boss.State == RotwoodBoss.ActionState.Chase, "AC-006: high-health stagger does not expose heart");

        boss.transform.position = anchor;
        player.transform.position = anchor+Vector3.right*4;
        var wall = Fixture("Charge Obstacle");
        wall.layer = 31;
        wall.transform.position = anchor+Vector3.right*2;
        wall.AddComponent<BoxCollider2D>().size = new Vector2(0.5f,4);
        Set(boss,"obstacleLayers",(LayerMask)(1 << 31));
        Physics2D.SyncTransforms();
        Enter(boss,RotwoodBoss.ActionState.Windup);
        boss.Tick(1.01f); boss.Tick(1f);
        Check(boss.State == RotwoodBoss.ActionState.Stagger && boss.transform.position.x < wall.transform.position.x-0.7f,
            "AC-004: swept circle cast stops charge before solid wall");
        Destroy(wall);
        Set(boss,"obstacleLayers",(LayerMask)0);
        boss.transform.position = anchor;
        player.transform.position = anchor+Vector3.right*4;
        Set(player,"isInvincible",false);
        hp = player.currentHealth;
        Enter(boss,RotwoodBoss.ActionState.Windup);
        boss.Tick(1.01f); boss.Tick(1f);
        Check(player.currentHealth == hp-25, "AC-004: swept charge hits a player crossed between frames");

        boss.transform.position = anchor+Vector3.left*4;
        player.transform.position = anchor;
        for (int i=0; i<3; i++) { Enter(boss,RotwoodBoss.ActionState.Summon); boss.Tick(1.01f); }
        Check(boss.LivingWolves == 4, "AC-005: summon creates two wolves and caps living minions at four");
        foreach (var wolf in Read<List<EnemyStats>>(boss,"wolves")) wolf.GetComponent<Enemy>().enabled=false;
        stats.currentHealth = stats.enemyData.MaxHealth*0.49f;
        Enter(boss,RotwoodBoss.ActionState.Stagger);
        boss.Tick(1.51f);
        Check(boss.State == RotwoodBoss.ActionState.Exposed && boss.DamageTakenMultiplier == 1.5f,
            "AC-006: below 50% stagger opens weak point");
        hp = stats.currentHealth;
        stats.TakeDamage(10);
        Check(Mathf.Approximately(stats.currentHealth,hp-15), "AC-006: exposed heart increases incoming damage by 50%");
        encounter.SendMessage("Update");
        Check(Mathf.Approximately(Read<Image>(encounter,"healthFill").fillAmount,stats.currentHealth/stats.enemyData.MaxHealth),
            "AC-002: boss health fill reflects real incoming damage");
        boss.Tick(2.01f);
        Check(boss.DamageTakenMultiplier == 1f && boss.State == RotwoodBoss.ActionState.Chase,
            "AC-006: weak point closes after two seconds");

        var slashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Slash.prefab");
        var swordData = AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Sword.asset");
        var slash = Instantiate(slashPrefab).GetComponent<MeleeWeaponBehaviour>(); fixtures.Add(slash.gameObject);
        slash.enabled=false;
        slash.Initialize(swordData,player.transform,Vector2.right);
        hp=stats.currentHealth;
        Call(slash,"TryHit",boss.GetComponent<Collider2D>());
        Call(slash,"TryHit",boss.GetComponent<Collider2D>());
        Check(stats.currentHealth == hp-swordData.Damage*player.DamageMultiplier,
            "AC-008: real melee path hits boss once per swing");
        var spell = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Spell.prefab")).GetComponent<ProjectileWeaponBehaviour>();
        fixtures.Add(spell.gameObject); spell.enabled=false;
        hp=stats.currentHealth;
        Call(spell,"OnTriggerEnter2D",boss.GetComponent<Collider2D>());
        Check(stats.currentHealth < hp, "AC-008: real projectile collision path damages boss");
        Destroy(slash.gameObject); Destroy(spell.gameObject);
        var legacy = Fixture("Ordinary EnemyStats without animation");
        legacy.SetActive(false);
        var legacyStats = legacy.AddComponent<EnemyStats>();
        legacyStats.enemyData = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Wolf.prefab").GetComponent<EnemyStats>().enemyData;
        legacy.SetActive(true);
        legacyStats.Kill();
        yield return null;
        Check(legacy == null, "AC-008: legacy enemy without boss retains immediate death");

        Set(boss,"encounter",encounter);
        int deaths = 0;
        boss.DeathFinished += () => deaths++;
        // A fixture drop lets us verify idempotency without auto-collecting real EXP.
        var lootTemplate = Fixture("Boss Loot Template");
        lootTemplate.AddComponent<RotwoodLootMarker>();
        boss.GetComponent<DropRateManager>().drops = new List<DropRateManager.Drops>
        { new DropRateManager.Drops { name="test", itemPrefap=lootTemplate, dropRate=100 } };
        player.currentHealth = player.MaxHealth;
        FindAnyObjectByType<PlayerHud>().Refresh();
        stats.Kill(); stats.Kill(); stats.TakeDamage(99999);
        Check(stats.IsDead && boss.State == RotwoodBoss.ActionState.Dead &&
            !boss.GetComponent<Collider2D>().enabled && !boss.GetComponent<Rigidbody2D>().simulated,
            "AC-007: death is terminal and disables collision");
        Check(!GameOverController.GameplayStopped, "AC-007: victory waits for death animation");
        // The Destroy deadline is already running; death animation must tick from the same frame.
        boss.enabled=true;
        yield return null;
        Check(boss.LivingWolves==0 && FindObjectsByType<RotwoodRoot>().Length==0,
            "AC-007: death cleans up summoned wolves and roots");
        Check(FindObjectsByType<RotwoodLootMarker>().Length==2, "AC-007: repeated Kill drops only once");
        foreach(var loot in FindObjectsByType<RotwoodLootMarker>()) if(loot.gameObject!=lootTemplate) fixtures.Add(loot.gameObject);
        yield return new WaitForSeconds(1.5f);
        var result = FindAnyObjectByType<GameOverController>();
        Check(result.IsGameOver && result.HasWon && deaths==1, $"AC-007: death completes once and finishes run as victory (gameOver={result.IsGameOver}, won={result.HasWon}, deaths={deaths}, scale={Time.timeScale})");
        Check(!Read<GameObject>(encounter,"hud").activeSelf, "AC-002: boss health HUD hides after victory");
        Check(Read<Button>(result,"restartButton").interactable && Read<Button>(result,"mainMenuButton").interactable,
            "AC-007: victory retains Restart and Main Menu controls");
        Check(Read<Text>(result,"resultText").text.Contains("YOU WIN") &&
            Read<Text>(result,"resultText").transform.parent.Find("Heading").GetComponent<Text>().text=="VICTORY",
            "AC-007: victory heading replaces GAME OVER");
        float stoppedTime=encounter.ElapsedTime;
        encounter.Tick(5);
        Check(encounter.ElapsedTime==stoppedTime, "AC-001: encounter clock stops after run ends");
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Output+"/victory.png");
        foreach(var fixture in fixtures) if(fixture!=null) Destroy(fixture);
        fixtures.Clear();

        result.Restart();
        float timeout=Time.realtimeSinceStartup+15;
        while (FindAnyObjectByType<GameOverController>() == result && Time.realtimeSinceStartup<timeout) yield return null;
        yield return null;
        var fresh = FindAnyObjectByType<GameOverController>();
        var freshEncounter = FindAnyObjectByType<RotwoodEncounter>();
        Check(fresh!=null && fresh!=result && !fresh.IsGameOver && !fresh.HasWon && !freshEncounter.HasSpawned,
            "AC-008: real Restart creates a fresh 15-minute encounter");
        PlayerController.Instance.GetComponent<PlayerStats>().Kill();
        fresh.Win();
        Check(fresh.IsGameOver && !fresh.HasWon, "AC-008: a player defeat cannot be overwritten by late boss victory");
        Check(SourceSnapshot()==sourceSnapshot && File.ReadAllText(LoadoutLauncher.GameplayScene)==sceneText,
            "AC-002/008: tests preserve authored source files and scene/spawner");
    }

    private GameObject Fixture(string name) { var go=new GameObject(name); fixtures.Add(go); return go; }
    private static Rect ScreenBounds(RectTransform rect)
    {
        var corners = new Vector3[4]; rect.GetWorldCorners(corners);
        return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
    }
    private void Check(bool condition,string label) { if(!condition) throw new InvalidOperationException(label); checks.Add(label); }
    private static T Read<T>(object target,string field) => (T)target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(target);
    private static void Set(object target,string field,object value) => target.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(target,value);
    private static object Call(object target,string method,params object[] args) => target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);
    private static void Enter(RotwoodBoss boss,RotwoodBoss.ActionState state) => Call(boss,"Enter",state);
    private static string SourceSnapshot()
    {
        using (var sha = SHA256.Create())
            return string.Join("\n", Directory.GetFiles("Assets/MyAssets/Monster/Boss/Rotwood")
                .Where(p => p.EndsWith(".png") || p.EndsWith(".wav")).OrderBy(p=>p)
                .Select(p=>p+":"+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(p)))));
    }
    [Serializable] private class Report { public string result; public string[] checks; public string failure; }
}
public class RotwoodLootMarker : MonoBehaviour { }
#endif
