#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public class SurvivalSpawnRegression : MonoBehaviour
{
    private const string Output = "Captures/SurvivalSpawning";
    private static readonly BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<string> checks = new List<string>();
    private readonly List<string> trace = new List<string>();
    private GameObject sandbox;
    private EnemySpawner sceneSpawner;
    private PlayerController playerController;
    private PlayerStats player;
    private Rigidbody2D playerBody;
    private Tile testTile;
    private bool background, controllerEnabled, bodySimulated, sceneSpawnerEnabled, wasInvincible;
    private float invincibilityTimer;
    private UnityEngine.Random.State randomState;
    private int loaded;
    private string dataBefore;
    private bool cleaned;

    [MenuItem("Tools/System Leveling/Run Survival Spawn Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<SurvivalSpawnRegression>() != null) return;
        GameObject root = new GameObject("Survival Spawn Regression");
        DontDestroyOnLoad(root);
        root.AddComponent<SurvivalSpawnRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground;
        randomState = UnityEngine.Random.state;
        Application.runInBackground = true;
        UnityEngine.Random.InitState(19471);
        Time.timeScale = 1f;
        SceneManager.sceneLoaded += OnLoaded;
        Application.logMessageReceived += OnLog;
        dataBefore = Snapshot();
        IEnumerator test = Tests();
        string error = "";
        while (true)
        {
            bool next = false; object wait = null;
            try { next = test.MoveNext(); if (next) wait = test.Current; }
            catch (Exception ex) { error = ex.ToString(); }
            if (!next || error.Length > 0) break;
            yield return wait;
        }
        Cleanup();
        Directory.CreateDirectory(Output);
        Report report = new Report { result = error.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = error };
        File.WriteAllText(Output + "/regression.json", JsonUtility.ToJson(report, true));
        File.WriteAllLines(Output + "/state-log.txt", trace);
        if (error.Length == 0) Debug.Log($"SURVIVAL SPAWN REGRESSION PASS: {checks.Count} checks.");
        else Debug.LogError("SURVIVAL SPAWN REGRESSION FAIL: " + error);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().path);
        float deadline = Time.realtimeSinceStartup + 15;
        while (loaded == 0 && Time.realtimeSinceStartup < deadline) yield return null;
        Check(loaded == 1, "Loaded a clean gameplay scene baseline");
        yield return null;
        player = FindAnyObjectByType<PlayerStats>();
        sceneSpawner = FindAnyObjectByType<EnemySpawner>();
        sceneSpawnerEnabled = sceneSpawner.enabled;
        sceneSpawner.enabled = false;
        playerController = player.GetComponent<PlayerController>();
        playerBody = player.GetComponent<Rigidbody2D>();
        controllerEnabled = playerController.enabled;
        bodySimulated = playerBody.simulated;
        wasInvincible = Get<bool>(player, "isInvincible");
        invincibilityTimer = Get<float>(player, "invincibilityTimer");
        Set(player, "isInvincible", true);
        Set(player, "invincibilityTimer", 100000f);
        playerController.enabled = false;
        playerBody.simulated = false;
        sandbox = new GameObject("Spawn Regression Fixtures");

        CheckScenePreset();
        CheckPureLogic();
        GameObject template = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Slime.prefab"), new Vector3(10000, 10000), Quaternion.identity, sandbox.transform);
        template.name = "Stationary regression template (source asset untouched)";
        template.GetComponent<Enemy>().enabled = false;
        template.GetComponent<Rigidbody2D>().simulated = false;
        template.GetComponent<DropRateManager>().drops.Clear();
        foreach (Collider2D collider in template.GetComponentsInChildren<Collider2D>()) collider.enabled = false;

        CheckGeometry(template);
        HordeSpawnSettings horde = FastHorde(template);
        horde.minCount = horde.maxCount = 12;
        horde.populationCap = 12;
        horde.spawnsPerFrame = 1;
        horde.initialChance = 0;
        horde.increasePerFailure = 0;
        horde.maxWait = 0.3f;
        EnemySpawner spawning = MakeSpawner("Independent normal + forced Horde", template, horde);
        Set(spawning, "normalPopulationCap", 2);
        int normalWhileWaiting = 0, normalWhileSpawning = 0, normalWhileClearing = 0;
        bool sawDeferred = false, sawDeployment = false;
        float maxTickMilliseconds = 0;
        for (int frame = 0; frame < 35; frame++)
        {
            HordeSpawnState before = spawning.HordeState;
            int oldNormal = spawning.TotalNormalSpawned;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            spawning.Tick(0.1f);
            watch.Stop();
            maxTickMilliseconds = Mathf.Max(maxTickMilliseconds, (float)watch.Elapsed.TotalMilliseconds);
            Check(spawning.NormalSpawnedThisFrame <= 1 && spawning.HordeSpawnedThisFrame <= 1, "Per-frame normal/Horde budgets respected at frame " + frame);
            Check(spawning.NormalAliveCount <= 2 && spawning.HordeAliveCount <= 12, "Independent population caps respected at frame " + frame);
            int normalAdded = spawning.TotalNormalSpawned - oldNormal;
            if (before == HordeSpawnState.WaitingLayer2) normalWhileWaiting += normalAdded;
            if (before == HordeSpawnState.Spawning) normalWhileSpawning += normalAdded;
            if (before == HordeSpawnState.WaitingForClear) normalWhileClearing += normalAdded;
            if (spawning.HordeState == HordeSpawnState.WaitingLayer2) { sawDeferred = true; Check(spawning.TotalHordeSpawned == 0, "No immediate Horde after layer 1"); }
            if (spawning.HordeState == HordeSpawnState.Spawning) sawDeployment = true;
            // Keep normal slots available; Horde slots must not be affected by normal deaths.
            foreach (EnemyStats normal in Get<List<EnemyStats>>(spawning, "normalEnemies")) normal.Kill();
            yield return null;
        }
        trace.Add($"MEASURED shared production Tick: peak {maxTickMilliseconds:F3}ms in fixture (not a target-device benchmark).");
        Check(sawDeferred && sawDeployment && spawning.TotalHordeSpawned == 12 && spawning.HordeState == HordeSpawnState.WaitingForClear, "Forced Horde deploys all members and waits for clear");
        Check(normalWhileWaiting > 0 && normalWhileSpawning > 0 && normalWhileClearing > 0, "Normal spawn continues during layer 2, deployment and waiting-for-clear");
        List<EnemyStats> cohort = Get<List<EnemyStats>>(spawning, "hordeEnemies");
        CheckHordeGeometry(spawning, cohort);
        int hordeTotal = spawning.TotalHordeSpawned;
        spawning.Tick(100);
        Check(spawning.TotalHordeSpawned == hordeTotal && spawning.HordeState == HordeSpawnState.WaitingForClear, "Long frame cannot start a second living Horde or cause catch-up bursts");
        cohort[0].Kill();
        spawning.Tick(0.1f);
        Check(spawning.HordeState == HordeSpawnState.WaitingForClear && spawning.HordeAliveCount == 11, "Partial Horde defeat does not unlock layer 1");
        foreach (EnemyStats enemy in cohort.ToArray()) enemy.Kill();
        spawning.Tick(0.1f);
        Check(spawning.HordeAliveCount == 0 && spawning.HordeState == HordeSpawnState.CheckingLayer1 && spawning.HordeCheckTimer == 0, "Last dead Horde member releases cycle before its Die animation despawns");
        spawning.Tick(0.05f);
        Check(spawning.HordeState == HordeSpawnState.CheckingLayer1, "A fresh full layer 1 interval is required after Horde clear");
        spawning.Tick(0.05f);
        Check(spawning.HordeState == HordeSpawnState.WaitingLayer2, "Layer 1 can activate the next Horde after clear");

        float elapsed = spawning.ElapsedTime, normalTimer = spawning.NormalSpawnTimer, checkTimer = spawning.HordeCheckTimer;
        Time.timeScale = 0;
        spawning.Tick(10);
        Check(spawning.ElapsedTime == elapsed && spawning.NormalSpawnTimer == normalTimer && spawning.HordeCheckTimer == checkTimer, "Level-up pause freezes every clock and queued deployment pipeline");
        Time.timeScale = 1;

        // Infeasible screen placement: permitted area is entirely inside the camera.
        HordeSpawnSettings impossibleSettings = FastHorde(template);
        EnemySpawner impossible = MakeSpawner("Impossible placement", template, impossibleSettings, false);
        GameObject areaObject = new GameObject("Impossible allowed area", typeof(BoxCollider2D));
        areaObject.transform.SetParent(sandbox.transform);
        areaObject.transform.position = player.transform.position;
        BoxCollider2D area = areaObject.GetComponent<BoxCollider2D>();
        area.size = Vector2.one;
        area.isTrigger = true;
        Set(impossible, "allowedArea", area);
        for (int i = 0; i < 2; i++) impossible.Tick(0.1f);
        Check(impossible.TotalHordeSpawned == 0 && impossible.HordeState == HordeSpawnState.CheckingLayer1 && impossible.HordeRemaining == 0,
            "Impossible placement aborts safely within bounded attempts and releases reservation");
        Check(trace.Exists(line => line.Contains("No valid one-side anchor")), "Safety fallback emits a diagnostic warning");

        // Zero-radius cluster has room for one member only; later placement attempts must terminate.
        HordeSpawnSettings partialSettings = FastHorde(template);
        partialSettings.minCount = partialSettings.maxCount = 3;
        partialSettings.populationCap = 3;
        partialSettings.clusterRadius = 0;
        partialSettings.maxPlacementFailures = 2;
        EnemySpawner partial = MakeSpawner("Partial deployment", template, partialSettings, false);
        partial.Tick(0.1f); partial.Tick(0.1f); partial.Tick(0.1f);
        Check(partial.TotalHordeSpawned == 1 && partial.HordeRemaining == 0 && partial.HordeState == HordeSpawnState.WaitingForClear,
            "Unplaceable remainder is skipped without losing the first live Horde member");

        HordeSpawnSettings longFrameSettings = FastHorde(template);
        longFrameSettings.initialChance = 0;
        longFrameSettings.maxWait = 0.2f;
        EnemySpawner longFrame = MakeSpawner("Deadline across a long frame", template, longFrameSettings, false);
        longFrame.Tick(0.1f);
        longFrame.Tick(100);
        Check(longFrame.TotalHordeSpawned == 3 && longFrame.HordeState == HordeSpawnState.WaitingForClear,
            "A long waiting frame forces deployment without incorrectly consuming the new deployment timeout");

        // Real spawn pipeline must honor Game Over even with a Horde pending.
        elapsed = spawning.ElapsedTime; normalTimer = spawning.NormalSpawnTimer; checkTimer = spawning.HordeCheckTimer;
        int total = spawning.TotalNormalSpawned + spawning.TotalHordeSpawned;
        player.Kill();
        spawning.Tick(100);
        Check(GameOverController.GameplayStopped && spawning.ElapsedTime == elapsed && spawning.NormalSpawnTimer == normalTimer && spawning.HordeCheckTimer == checkTimer &&
            spawning.TotalNormalSpawned + spawning.TotalHordeSpawned == total, "Game Over freezes a pending Horde and normal spawn, not only idle layer 1");
        GameObject oldSandbox = sandbox;
        FindAnyObjectByType<GameOverController>().Restart();
        deadline = Time.realtimeSinceStartup + 15;
        while (loaded < 2 && Time.realtimeSinceStartup < deadline) yield return null;
        Check(loaded == 2, "Restart reloads the actual gameplay scene after a pending Horde");
        yield return null;
        EnemySpawner fresh = FindAnyObjectByType<EnemySpawner>();
        Check(oldSandbox == null && fresh.ElapsedTime < 1 && fresh.HordeState == HordeSpawnState.CheckingLayer1 && fresh.HordeChance == 10 && fresh.HordeAliveCount == 0 &&
            fresh.NormalAliveCount == 0 && fresh.TotalHordeSpawned == 0 && fresh.TotalNormalSpawned == 0, "Restart resets survival time, state, probability, populations and counters");
        Check(dataBefore == Snapshot(), "Character, weapon and enemy data files remain byte-identical");
    }

    private void CheckScenePreset()
    {
        List<SpawnPhase> phases = Get<List<SpawnPhase>>(sceneSpawner, "phases");
        Check(phases.Count == 4 && phases[0].startTime == 0 && phases[1].startTime == 30 && phases[2].startTime == 60 && phases[3].startTime == 120, "Inspector contains the four approved survival phase boundaries");
        Check(phases[0].interval == 2.5f && phases[2].enemiesPerBatch == 2 && phases[3].interval == 1 && phases[3].enemiesPerBatch == 3, "Inspector contains approved normal spawn cadence and batches");
        HordeSpawnSettings horde = Get<HordeSpawnSettings>(sceneSpawner, "horde");
        Check(horde.activationInterval == 20 && horde.activationChance == 25 && horde.checkInterval == 5 && horde.initialChance == 10 && horde.increasePerFailure == 15 && horde.maxWait == 30,
            "Inspector exposes approved two-layer probability parameters");
        Check(horde.minCount == 10 && horde.maxCount == 20 && horde.populationCap == 30 && horde.spawnsPerFrame == 4 && Get<int>(sceneSpawner, "normalPopulationCap") == 100,
            "Inspector contains separate normal100/Horde30 caps and 10-20/4-frame Horde preset");
        Check(Get<Camera>(sceneSpawner, "spawnCamera") != null && Get<Tilemap>(sceneSpawner, "allowedGround") != null, "Scene references active camera and actual ground Tilemap");
    }

    private void CheckPureLogic()
    {
        List<SpawnPhase> phases = Get<List<SpawnPhase>>(sceneSpawner, "phases");
        Check(SpawnSelection.PhaseAt(phases, 29.99f) == phases[0] && SpawnSelection.PhaseAt(phases, 30) == phases[1] && SpawnSelection.PhaseAt(phases, 60) == phases[2] && SpawnSelection.PhaseAt(phases, 120) == phases[3], "Phase lookup handles exact boundaries");
        List<SpawnPhase> shuffled = new List<SpawnPhase> {phases[3], phases[1], phases[0], phases[2]};
        Check(SpawnSelection.PhaseAt(shuffled, 90) == phases[2] && SpawnSelection.PhaseAt(new List<SpawnPhase> {phases[1]}, 0) == null, "Phase order is not required; future-only phases do not unlock early");
        string[] unlocks = { "Slime", "Skeleton", "Spider", "Wolf" };
        HashSet<GameObject> expected = new HashSet<GameObject>();
        GameObject retiredSlime = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Blue Slime.prefab");
        if (retiredSlime != null) expected.Add(retiredSlime);
        bool correctUnlocks = true;
        for (int i = 0; i < unlocks.Length; i++)
        {
            expected.Add(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/ReferenceEnemies/{unlocks[i]}.prefab"));
            correctUnlocks &= expected.SetEquals(phases[i].enemies.Where(entry => entry != null && entry.IsUsable).Select(entry => entry.prefab));
        }
        Check(correctUnlocks, "Normal enemy pools unlock only at configured phases (retired assets excluded)");
        // Keep weighted boundary fixtures independent of user-edited scene pools or retired prefabs.
        GameObject a = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Slime.prefab");
        GameObject b = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Skeleton.prefab");
        List<WeightedEnemySpawn> pool = new List<WeightedEnemySpawn> {new WeightedEnemySpawn(null, 99), new WeightedEnemySpawn(a, 0), new WeightedEnemySpawn(a, 1), new WeightedEnemySpawn(b, 3)};
        Check(SpawnSelection.Pick(pool, 0) == a && SpawnSelection.Pick(pool, 0.249f) == a && SpawnSelection.Pick(pool, 0.25f) == b && SpawnSelection.Pick(pool, 1) == b, "Weighted picker obeys cumulative boundaries and ignores null/zero entries");
        Check(SpawnSelection.Pick(new List<WeightedEnemySpawn> {new WeightedEnemySpawn(a, float.NaN), new WeightedEnemySpawn(b, -1)}, 0) == null, "Invalid weighted pools return no prefab without infinite retries");
        HordeSpawnSettings settings = new HordeSpawnSettings {activationInterval = 1, activationChance = 100, checkInterval = 5, initialChance = 10, increasePerFailure = 15, maxWait = 30};
        HordeSpawnCycle cycle = new HordeSpawnCycle(settings);
        cycle.Log = message => trace.Add("PURE: " + message);
        int rolls = 0;
        Func<float> fail = () => { rolls++; return 99; };
        Check(!cycle.Tick(1, true, 0, fail) && cycle.State == HordeSpawnState.WaitingLayer2 && cycle.WaitElapsed == 0, "Layer 1 success changes state but does not deploy immediately");
        cycle.Tick(4, true, 0, fail);
        Check(rolls == 1 && cycle.Chance == 10, "First layer 2 check waits for its full interval");
        cycle.Tick(1, true, 0, fail);
        Check(cycle.Chance == 25 && rolls == 2, "First failed 10% roll raises chance to 25 percentage points");
        cycle.Tick(5, true, 0, fail); Check(cycle.Chance == 40, "Second failed roll raises chance to 40%");
        cycle.Tick(5, true, 0, fail); cycle.Tick(5, true, 0, fail); cycle.Tick(5, true, 0, fail);
        Check(cycle.Chance == 85 && cycle.State == HordeSpawnState.WaitingLayer2, "Escalation follows 10/25/40/55/70/85% schedule");
        Check(cycle.Tick(5, true, 0, fail) && cycle.State == HordeSpawnState.Spawning && rolls == 6, "30-second deadline forces deployment without another roll");
        Check(!cycle.Tick(100, true, 0, fail), "Active deployment cannot trigger another Horde");
        cycle.DeploymentFinished(2); cycle.Tick(100, true, 1, fail);
        Check(cycle.State == HordeSpawnState.WaitingForClear, "A surviving Horde member blocks layer 1");
        cycle.Tick(1, true, 0, fail);
        Check(cycle.State == HordeSpawnState.CheckingLayer1 && cycle.CheckTimer == 0 && cycle.Chance == 10, "Horde clear resets timer and probability before the next cycle");
        settings.maxWait = 0.5f; settings.checkInterval = 5;
        cycle.Reset(); cycle.Tick(1, true, 0, fail);
        Check(cycle.Tick(0.5f, true, 0, fail), "Deadline shorter than layer 2 interval still forces deployment");
        settings.maxWait = 100; settings.checkInterval = 1; settings.initialChance = 95; settings.increasePerFailure = 15;
        cycle.Reset(); cycle.Tick(1, true, 0, fail); cycle.Tick(1, true, 0, fail);
        Check(cycle.Chance == 100, "Probability escalation clamps to 100%");
        Check(cycle.Tick(1, true, 0, () => 100), "100% chance succeeds even at inclusive upper roll boundary");
        settings.activationChance = 0; cycle.Reset(); cycle.Tick(1, true, 0, () => 0);
        Check(cycle.State == HordeSpawnState.CheckingLayer1, "0% activation never succeeds on zero roll");
        settings.activationChance = 100; cycle.Reset(); rolls = 0;
        cycle.Tick(1, false, 0, fail);
        Check(rolls == 0 && cycle.State == HordeSpawnState.CheckingLayer1, "Layer 1 cannot arm a Horde without capacity and a usable pool");
        cycle.Tick(1, true, 0, () => 100);
        Check(cycle.State == HordeSpawnState.WaitingLayer2, "100% layer 1 includes the upper roll boundary");
        float before = cycle.Elapsed; cycle.Tick(0, true, 0, fail);
        Check(cycle.Elapsed == before, "Zero delta cannot advance scheduler clocks");
        settings.activationInterval = 0; settings.checkInterval = -1; settings.activationChance = 999; settings.initialChance = -5;
        settings.populationCap = 3; settings.minCount = 10; settings.maxCount = -1; settings.spawnsPerFrame = 999;
        settings.Validate();
        Check(settings.activationInterval == 0.1f && settings.checkInterval == 0.1f && settings.activationChance == 100 && settings.initialChance == 0 && settings.minCount == 3 && settings.maxCount == 3 && settings.spawnsPerFrame == 64,
            "Invalid Inspector intervals, probabilities, counts and frame budgets are normalized safely");
    }

    private void CheckGeometry(GameObject template)
    {
        EnemySpawner geometry = MakeSpawner("Geometry validation", template, new HordeSpawnSettings {enabled = false}, false);
        SpawnPlacementSettings placement = Get<SpawnPlacementSettings>(geometry, "placement");
        placement.requireOutsideCamera = true;
        placement.minPlayerDistance = 0;
        Check(geometry.TryCameraRect(out Rect rect) && !geometry.IsPositionValid(new Vector3(rect.center.x, rect.center.y)), "Camera interior is rejected");
        Vector3 outside = new Vector3(rect.xMax + 10, rect.center.y);
        Check(geometry.IsPositionValid(outside), "An unobstructed outside-camera point is permitted");
        placement.minPlayerDistance = 5;
        Check(!geometry.IsPositionValid(player.transform.position), "Player safety distance is enforced");
        placement.minPlayerDistance = 0;
        placement.requireOutsideCamera = false;
        GameObject grid = new GameObject("Ground fixture", typeof(Grid)); grid.transform.SetParent(sandbox.transform);
        GameObject tileObject = new GameObject("Ground", typeof(Tilemap)); tileObject.transform.SetParent(grid.transform, false);
        Tilemap ground = tileObject.GetComponent<Tilemap>();
        testTile = ScriptableObject.CreateInstance<Tile>(); ground.SetTile(Vector3Int.zero, testTile);
        Set(geometry, "allowedGround", ground);
        Vector3 center = new Vector3(0.5f, 0.5f);
        Check(geometry.IsPositionValid(center) && !geometry.IsPositionValid(new Vector3(1.5f, 0.5f)), "Allowed Tilemap rejects empty ground cells");
        Check(!geometry.IsPositionValid(new Vector3(0.95f, 0.5f)), "Spawn footprint cannot overhang the allowed ground");
        Set(geometry, "allowedGround", null);
        GameObject box = new GameObject("Area and obstacle fixture", typeof(BoxCollider2D)); box.transform.SetParent(sandbox.transform);
        box.transform.position = center;
        BoxCollider2D collider = box.GetComponent<BoxCollider2D>(); collider.size = Vector2.one;
        Physics2D.SyncTransforms();
        Set(geometry, "obstacleLayers", (LayerMask)1);
        Check(!geometry.IsPositionValid(center), "Solid obstacle layer overlap is rejected without allocations");
        collider.isTrigger = true; Physics2D.SyncTransforms();
        Check(geometry.IsPositionValid(center), "Non-obstacle trigger volumes are ignored by obstacle checks");
        Set(geometry, "obstacleLayers", (LayerMask)0);
        Set(geometry, "allowedArea", collider);
        Check(geometry.IsPositionValid(center) && !geometry.IsPositionValid(new Vector3(2, 2)), "Optional allowed-area Collider bounds are respected");
    }

    private void CheckHordeGeometry(EnemySpawner spawner, List<EnemyStats> members)
    {
        Check(spawner.TryCameraRect(out Rect rect), "Spawn geometry uses the actual orthographic camera");
        int side = Get<int>(spawner, "hordeSide");
        foreach (EnemyStats member in members)
        {
            Vector3 p = member.transform.position;
            bool sameSide = side == 0 ? p.x < rect.xMin : side == 1 ? p.x > rect.xMax : side == 2 ? p.y < rect.yMin : p.y > rect.yMax;
            Check(!rect.Contains(p) && sameSide && Vector2.Distance(p, player.transform.position) >= 5, "Horde member remains outside camera on the selected side");
        }
        bool separated = true;
        for (int i = 0; i < members.Count; i++)
            for (int j = i + 1; j < members.Count; j++)
                separated &= Vector2.Distance(members[i].transform.position, members[j].transform.position) >= 0.799f;
        Check(separated, "Horde placement preserves configured member separation");
        Check(!spawner.IsPositionValid(members[0].transform.position), "Placement cannot overlap an existing tracked enemy");
    }

    private HordeSpawnSettings FastHorde(GameObject template) => new HordeSpawnSettings
    {
        activationInterval = 0.1f, activationChance = 100, checkInterval = 0.1f, initialChance = 100, maxWait = 0.5f,
        minCount = 3, maxCount = 3, populationCap = 3, spawnsPerFrame = 3,
        enemies = new List<WeightedEnemySpawn> {new WeightedEnemySpawn(template)}
    };

    private EnemySpawner MakeSpawner(string name, GameObject template, HordeSpawnSettings settings, bool normal = true)
    {
        GameObject root = new GameObject(name); root.transform.SetParent(sandbox.transform); root.SetActive(false);
        EnemySpawner spawner = root.AddComponent<EnemySpawner>(); spawner.enabled = false;
        GameObject container = new GameObject("Spawned enemies"); container.transform.SetParent(root.transform);
        Set(spawner, "player", player.transform); Set(spawner, "enemyContainer", container.transform); Set(spawner, "spawnCamera", Camera.main);
        Set(spawner, "initialDelay", 0f); Set(spawner, "normalPopulationCap", 2); Set(spawner, "normalSpawnsPerFrame", 1);
        Set(spawner, "horde", settings); Set(spawner, "debugLogging", true);
        Set(spawner, "placement", new SpawnPlacementSettings());
        Set(spawner, "phases", normal ? new List<SpawnPhase> {new SpawnPhase {interval = 0.1f, enemiesPerBatch = 5, enemies = new List<WeightedEnemySpawn> {new WeightedEnemySpawn(template)}}} : new List<SpawnPhase>());
        root.SetActive(true);
        return spawner;
    }

    private static T Get<T>(object target, string name) => (T)target.GetType().GetField(name, Fields).GetValue(target);
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, Fields).SetValue(target, value);
    private void Check(bool condition, string label) { if (!condition) throw new Exception(label); checks.Add(label); }
    private void OnLoaded(Scene scene, LoadSceneMode mode) { GameplayRegressionBaseline.Prepare(); loaded++; }
    private void OnLog(string message, string stack, LogType type) { if (message.StartsWith("[Spawn")) trace.Add(type + ": " + message); }
    private static string Snapshot()
    {
        string text = "";
        foreach (string path in Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories)) text += path + File.ReadAllText(path);
        return text;
    }

    private void Cleanup()
    {
        if (cleaned) return; cleaned = true;
        SceneManager.sceneLoaded -= OnLoaded;
        Application.logMessageReceived -= OnLog;
        if (sandbox != null) DestroyImmediate(sandbox);
        if (testTile != null) DestroyImmediate(testTile);
        if (sceneSpawner != null) sceneSpawner.enabled = sceneSpawnerEnabled;
        if (playerController != null) playerController.enabled = controllerEnabled;
        if (playerBody != null) playerBody.simulated = bodySimulated;
        if (player != null) { Set(player, "isInvincible", wasInvincible); Set(player, "invincibilityTimer", invincibilityTimer); }
        UnityEngine.Random.state = randomState;
        Application.runInBackground = background;
        Time.timeScale = 1;
    }
    private void OnDestroy() => Cleanup();
    [Serializable] private class Report { public string result; public string utc; public string[] checks; public string failure; }
}
#endif
