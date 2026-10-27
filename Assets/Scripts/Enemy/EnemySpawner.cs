using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemyContainer;
    [SerializeField] private Camera spawnCamera;
    [Tooltip("Optional: every sampled footprint point must have a ground tile.")]
    [SerializeField] private Tilemap allowedGround;
    [Tooltip("Optional additional world boundary. Use a trigger outside the obstacle mask.")]
    [SerializeField] private Collider2D allowedArea;
    [Tooltip("Solid obstacle layers only; do not include enemies, ground or pickups.")]
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Normal survival spawning")]
    [FormerlySerializedAs("firstSpawnDelay"), SerializeField, Min(0f)] private float initialDelay = 1f;
    [FormerlySerializedAs("maxEnemies"), SerializeField, Min(1)] private int normalPopulationCap = 100;
    [SerializeField, Range(1, 64)] private int normalSpawnsPerFrame = 2;
    [SerializeField] private List<SpawnPhase> phases = new List<SpawnPhase>();
    [Header("Horde (independent population cap)")]
    [SerializeField] private HordeSpawnSettings horde = new HordeSpawnSettings();
    [Header("Placement")]
    [SerializeField] private SpawnPlacementSettings placement = new SpawnPlacementSettings();
    [Header("Diagnostics")]
    [SerializeField] private bool debugLogging = true;
    // Preserve the original scene's prefab references; the setup menu migrates to weighted pools.
    [SerializeField, HideInInspector] private GameObject[] enemyPrefabs;

    private readonly List<EnemyStats> normalEnemies = new List<EnemyStats>();
    private readonly List<EnemyStats> hordeEnemies = new List<EnemyStats>();
    private readonly Collider2D[] obstacleHit = new Collider2D[1];
    private HordeSpawnCycle cycle;
    private System.Func<float> rollPercent;
    private int normalPending;
    private int reservedHorde;
    private int hordeRemaining;
    private int placementFailures;
    private int hordeSide;
    private float deploymentElapsed;
    private Vector3 hordeAnchor;
    private float normalSpawnTimer;

    public float ElapsedTime { get; private set; }
    public float NormalSpawnTimer => normalSpawnTimer;
    public float HordeCheckTimer => cycle != null ? cycle.CheckTimer : 0;
    public float HordeWaitElapsed => cycle != null ? cycle.WaitElapsed : 0;
    public HordeSpawnState HordeState => cycle != null ? cycle.State : HordeSpawnState.CheckingLayer1;
    public float HordeChance => cycle != null ? cycle.Chance : horde.initialChance;
    public int NormalAliveCount => normalEnemies.Count;
    public int HordeAliveCount => hordeEnemies.Count;
    public int HordeRemaining => hordeRemaining;
    public int NormalSpawnedThisFrame { get; private set; }
    public int HordeSpawnedThisFrame { get; private set; }
    public int TotalNormalSpawned { get; private set; }
    public int TotalHordeSpawned { get; private set; }
    public string LastPlacementFailure { get; private set; }
    public SpawnPhase CurrentPhase => SpawnSelection.PhaseAt(phases, ElapsedTime);

    private void Awake()
    {
        ValidateConfiguration();
        if (spawnCamera == null) spawnCamera = Camera.main;
        rollPercent = () => Random.Range(0f, 100f);
        cycle = new HordeSpawnCycle(horde);
        cycle.Log = Log;
        normalSpawnTimer = initialDelay;
        foreach (SpawnPhase phase in phases) if (phase != null) WarnInvalidEntries(phase.enemies);
        if (horde.enabled) WarnInvalidEntries(horde.enemies);
    }

    // No OnEnable reset: temporarily disabling a component must not forget a live horde.
    // Restart loads a fresh scene/component and naturally resets every clock and counter.
    private void OnValidate() => ValidateConfiguration();

    public void ValidateConfiguration()
    {
        initialDelay = SpawnSelection.NonNegative(initialDelay);
        normalPopulationCap = Mathf.Max(1, normalPopulationCap);
        normalSpawnsPerFrame = Mathf.Clamp(normalSpawnsPerFrame, 1, 64);
        if (placement == null) placement = new SpawnPlacementSettings();
        if (horde == null) horde = new HordeSpawnSettings();
        if (phases == null) phases = new List<SpawnPhase>();
        placement.Validate();
        horde.Validate();
        foreach (SpawnPhase phase in phases)
        {
            if (phase == null) continue;
            phase.startTime = SpawnSelection.NonNegative(phase.startTime);
            phase.interval = SpawnSelection.Positive(phase.interval, 2.5f);
            phase.enemiesPerBatch = Mathf.Max(1, phase.enemiesPerBatch);
        }
    }

    private void Update() => Tick(Time.deltaTime);

    // One production frame pipeline; an explicit delta also makes deterministic integration tests possible.
    internal void Tick(float delta)
    {
        NormalSpawnedThisFrame = 0;
        HordeSpawnedThisFrame = 0;
        if (GameOverController.GameplayStopped || player == null || !player.gameObject.activeInHierarchy ||
            Time.timeScale <= 0f || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta))
            return;
        RemoveDead(normalEnemies);
        RemoveDead(hordeEnemies);
        ElapsedTime += delta;
        TickNormal(delta);

        // A pending/spawning horde completes even if its toggle is disabled mid-event.
        if (horde.enabled || HordeState == HordeSpawnState.Spawning)
        {
            HordeSpawnState before = cycle.State;
            bool capacity = hordeEnemies.Count == 0 && horde.populationCap >= horde.minCount && HasUsableEntry(horde.enemies);
            bool deploy = cycle.Tick(delta, capacity, hordeEnemies.Count, rollPercent);
            if (before == HordeSpawnState.CheckingLayer1 && cycle.State == HordeSpawnState.WaitingLayer2)
            {
                reservedHorde = Random.Range(horde.minCount, Mathf.Min(horde.maxCount, horde.populationCap) + 1);
                Log($"Reserved {reservedHorde} independent horde slots.");
            }
            if (deploy)
            {
                BeginDeployment();
                // This delta belonged to layer 2, not a deployment which has only just begun.
                if (cycle.State == HordeSpawnState.Spawning) DeployFrame(0f);
            }
            else if (cycle.State == HordeSpawnState.Spawning) DeployFrame(delta);
        }
    }

    private void TickNormal(float delta)
    {
        SpawnPhase phase = CurrentPhase;
        if (phase == null) return;
        normalSpawnTimer -= delta;
        if (normalSpawnTimer <= 0f)
        {
            normalSpawnTimer = SpawnSelection.Positive(phase.interval, 2.5f);
            // Bounded queue: never catch up missed ticks in a burst or overwrite an active batch.
            if (normalPending == 0)
                normalPending = Mathf.Min(phase.enemiesPerBatch, Mathf.Max(0, normalPopulationCap - normalEnemies.Count));
        }
        for (int i = 0; i < normalSpawnsPerFrame && normalPending > 0; i++)
        {
            if (normalEnemies.Count >= normalPopulationCap) { normalPending = 0; break; }
            GameObject prefab = SpawnSelection.Pick(phase.enemies, Random.value);
            if (prefab == null) { normalPending = 0; break; }
            normalPending--;
            if (!TryPosition(false, out Vector3 position))
            {
                Log($"Normal placement skipped: {LastPlacementFailure}.");
                continue;
            }
            normalEnemies.Add(CreateEnemy(prefab, position));
            NormalSpawnedThisFrame++;
            TotalNormalSpawned++;
        }
    }

    private void BeginDeployment()
    {
        hordeRemaining = Mathf.Min(reservedHorde, Mathf.Max(0, horde.populationCap - hordeEnemies.Count));
        placementFailures = 0;
        deploymentElapsed = 0;
        hordeSide = Random.Range(0, 4);
        bool found = false;
        for (int i = 0; i < placement.attemptsPerEnemy; i++)
        {
            if (TrySample(hordeSide, out hordeAnchor) && IsPositionValid(hordeAnchor)) { found = true; break; }
        }
        if (!found || hordeRemaining == 0)
        {
            FinishDeployment("No valid one-side anchor or reserved slots; no unsafe spawn was forced.", true);
            return;
        }
        Log($"Deploying {hordeRemaining} horde enemies from side {hordeSide}; budget {horde.spawnsPerFrame}/frame.");
    }

    private void DeployFrame(float delta)
    {
        deploymentElapsed += delta;
        if (deploymentElapsed >= horde.maxDeploymentTime)
        {
            FinishDeployment("Deployment time budget exhausted.", true);
            return;
        }
        for (int i = 0; i < horde.spawnsPerFrame && hordeRemaining > 0; i++)
        {
            if (hordeEnemies.Count >= horde.populationCap)
            {
                FinishDeployment("Horde cap changed or reached; remaining members skipped.", true);
                return;
            }
            GameObject prefab = SpawnSelection.Pick(horde.enemies, Random.value);
            if (prefab == null)
            {
                FinishDeployment("Horde prefab pool is empty or invalid.", true);
                return;
            }
            if (!TryPosition(true, out Vector3 position))
            {
                placementFailures++;
                if (placementFailures >= horde.maxPlacementFailures)
                    FinishDeployment($"Placement budget exhausted: {LastPlacementFailure}.", true);
                break;
            }
            hordeEnemies.Add(CreateEnemy(prefab, position));
            HordeSpawnedThisFrame++;
            TotalHordeSpawned++;
            hordeRemaining--;
        }
        if (cycle.State == HordeSpawnState.Spawning && hordeRemaining == 0)
            FinishDeployment("All reserved horde members deployed.", false);
    }

    private void FinishDeployment(string reason, bool warning)
    {
        string message = $"[Spawn t={ElapsedTime:F1}] {reason} Alive={hordeEnemies.Count}, skipped={hordeRemaining}.";
        if (warning) Debug.LogWarning(message, this);
        else if (debugLogging) Debug.Log(message, this);
        hordeRemaining = 0;
        reservedHorde = 0;
        cycle.DeploymentFinished(hordeEnemies.Count);
    }

    private EnemyStats CreateEnemy(GameObject prefab, Vector3 position)
    {
        GameObject created = Instantiate(prefab, position, Quaternion.identity, enemyContainer);
        return created.GetComponent<EnemyStats>();
    }

    private bool TryPosition(bool isHorde, out Vector3 position)
    {
        position = Vector3.zero;
        for (int i = 0; i < placement.attemptsPerEnemy; i++)
        {
            if (isHorde)
            {
                Vector2 offset = Random.insideUnitCircle * horde.clusterRadius;
                position = hordeAnchor + new Vector3(offset.x, offset.y, 0);
            }
            else if (!TrySample(-1, out position)) continue;
            if (isHorde && !OnHordeSide(position)) { LastPlacementFailure = "Outside the selected horde side"; continue; }
            if (IsPositionValid(position)) return true;
        }
        return false;
    }

    private bool OnHordeSide(Vector3 position)
    {
        if (placement.region == SpawnRegion.PlayerRing)
        {
            float angle = hordeSide * Mathf.PI * 0.5f;
            return Vector2.Dot((Vector2)(position - player.position), new Vector2(Mathf.Cos(angle), Mathf.Sin(angle))) > 0;
        }
        if (!TryCameraRect(out Rect rect)) return false;
        float padding = placement.cameraPadding + placement.clearanceRadius;
        return hordeSide == 0 ? position.x < rect.xMin - padding : hordeSide == 1 ? position.x > rect.xMax + padding :
            hordeSide == 2 ? position.y < rect.yMin - padding : position.y > rect.yMax + padding;
    }

    private bool TrySample(int fixedSide, out Vector3 position)
    {
        position = player.position;
        int side = fixedSide >= 0 ? fixedSide : Random.Range(0, 4);
        if (placement.region == SpawnRegion.PlayerRing)
        {
            float angle = fixedSide >= 0 ? side * Mathf.PI * 0.5f + Random.Range(-Mathf.PI / 4, Mathf.PI / 4) : Random.Range(0, Mathf.PI * 2);
            float min = placement.minRingRadius;
            float max = placement.maxRingRadius;
            float radius = Mathf.Sqrt(Random.Range(min * min, max * max));
            position += new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            return true;
        }
        if (!TryCameraRect(out Rect rect)) { LastPlacementFailure = "Missing orthographic spawn camera"; return false; }
        float distance = Random.Range(placement.minEdgeDistance, placement.maxEdgeDistance);
        float x = Random.Range(rect.xMin, rect.xMax);
        float y = Random.Range(rect.yMin, rect.yMax);
        if (side == 0) x = rect.xMin - distance;
        else if (side == 1) x = rect.xMax + distance;
        else if (side == 2) y = rect.yMin - distance;
        else y = rect.yMax + distance;
        position = new Vector3(x, y, player.position.z);
        return true;
    }

    public bool TryCameraRect(out Rect rect)
    {
        rect = default;
        if (spawnCamera == null || !spawnCamera.orthographic || player == null) return false;
        float depth = Vector3.Dot(player.position - spawnCamera.transform.position, spawnCamera.transform.forward);
        Vector3 min = spawnCamera.ViewportToWorldPoint(new Vector3(0, 0, depth));
        Vector3 max = spawnCamera.ViewportToWorldPoint(new Vector3(1, 1, depth));
        rect = Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        return true;
    }

    public bool IsPositionValid(Vector3 position)
    {
        if (player == null) return Reject("No player");
        if (((Vector2)(position - player.position)).sqrMagnitude < placement.minPlayerDistance * placement.minPlayerDistance)
            return Reject("Too close to player");
        if (placement.requireOutsideCamera)
        {
            if (!TryCameraRect(out Rect rect)) return Reject("Missing orthographic spawn camera");
            float padding = placement.cameraPadding + placement.clearanceRadius;
            rect.xMin -= padding; rect.xMax += padding; rect.yMin -= padding; rect.yMax += padding;
            if (rect.Contains(position)) return Reject("Inside camera safety boundary");
        }
        float radius = placement.clearanceRadius;
        Vector3 diagonal = new Vector3(radius * 0.7071068f, radius * 0.7071068f, 0);
        if (!OnAllowedGround(position) || !OnAllowedGround(position + Vector3.right * radius) ||
            !OnAllowedGround(position - Vector3.right * radius) || !OnAllowedGround(position + Vector3.up * radius) ||
            !OnAllowedGround(position - Vector3.up * radius) || !OnAllowedGround(position + diagonal) ||
            !OnAllowedGround(position - diagonal) || !OnAllowedGround(position + new Vector3(diagonal.x, -diagonal.y)) ||
            !OnAllowedGround(position + new Vector3(-diagonal.x, diagonal.y))) return Reject("Outside allowed ground/area");
        if (obstacleLayers.value != 0)
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(obstacleLayers);
            filter.useTriggers = false;
            if (Physics2D.OverlapCircle(position, Mathf.Max(0.001f, radius), filter, obstacleHit) > 0)
                return Reject("Solid obstacle overlap");
        }
        if (TooClose(normalEnemies, position) || TooClose(hordeEnemies, position)) return Reject("Enemy separation");
        LastPlacementFailure = "";
        return true;
    }

    private bool OnAllowedGround(Vector3 position) => (allowedGround == null || allowedGround.HasTile(allowedGround.WorldToCell(position))) &&
        (allowedArea == null || allowedArea.OverlapPoint(position));

    private bool TooClose(List<EnemyStats> population, Vector3 position)
    {
        float minSquared = placement.separation * placement.separation;
        foreach (EnemyStats enemy in population)
            if (IsLiving(enemy) && ((Vector2)(enemy.transform.position - position)).sqrMagnitude < minSquared) return true;
        return false;
    }

    private bool Reject(string reason) { LastPlacementFailure = reason; return false; }
    private static bool IsLiving(EnemyStats enemy) => enemy != null && enemy.gameObject.activeInHierarchy && !enemy.IsDead;
    private static void RemoveDead(List<EnemyStats> population)
    {
        for (int i = population.Count - 1; i >= 0; i--)
            if (!IsLiving(population[i])) population.RemoveAt(i);
    }

    private static bool HasUsableEntry(List<WeightedEnemySpawn> entries)
    {
        if (entries == null) return false;
        foreach (WeightedEnemySpawn entry in entries) if (entry != null && entry.IsUsable) return true;
        return false;
    }

    private void WarnInvalidEntries(List<WeightedEnemySpawn> entries)
    {
        if (entries == null || entries.Count == 0) { Debug.LogWarning("Spawn pool is empty; no enemies will be created from it.", this); return; }
        foreach (WeightedEnemySpawn entry in entries)
            if (entry == null || (entry.weight != 0f && !entry.IsUsable))
                Debug.LogWarning("Spawn entry excluded: requires positive weight and a prefab with Enemy + EnemyStats.", this);
    }

    private void Log(string message)
    {
        if (debugLogging) Debug.Log($"[Spawn t={ElapsedTime:F1} state={HordeState}] {message}", this);
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null || placement == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, placement.minPlayerDistance);
        if (placement.region == SpawnRegion.PlayerRing)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position, placement.minRingRadius);
            Gizmos.DrawWireSphere(player.position, placement.maxRingRadius);
        }
        if (Application.isPlaying && HordeState == HordeSpawnState.Spawning)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(hordeAnchor, horde.clusterRadius);
        }
    }
}
