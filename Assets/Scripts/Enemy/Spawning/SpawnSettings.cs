using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WeightedEnemySpawn
{
    public GameObject prefab;
    [Min(0f)] public float weight = 1f;

    public WeightedEnemySpawn() { }
    public WeightedEnemySpawn(GameObject prefab, float weight = 1f) { this.prefab = prefab; this.weight = weight; }

    public bool IsUsable => prefab != null && prefab.activeSelf && weight > 0f && !float.IsInfinity(weight) &&
        prefab.GetComponent<Enemy>() != null && prefab.TryGetComponent(out EnemyStats stats) && stats.enemyData != null;
}

[Serializable]
public class SpawnPhase
{
    public string label = "Phase";
    [Min(0f)] public float startTime;
    [Min(0.1f)] public float interval = 2.5f;
    [Min(1)] public int enemiesPerBatch = 1;
    public List<WeightedEnemySpawn> enemies = new List<WeightedEnemySpawn>();
}

[Serializable]
public class HordeSpawnSettings
{
    public bool enabled = true;
    [Header("Layer 1 - activation")]
    [Min(0.1f)] public float activationInterval = 20f;
    [Range(0f, 100f)] public float activationChance = 25f;
    [Header("Layer 2 - escalating spawn chance")]
    [Min(0.1f)] public float checkInterval = 5f;
    [Range(0f, 100f)] public float initialChance = 10f;
    [Range(0f, 100f)] public float increasePerFailure = 15f;
    [Min(0.1f)] public float maxWait = 30f;
    [Header("Population and frame budget")]
    [Min(1)] public int populationCap = 30;
    [Min(1)] public int minCount = 10;
    [Min(1)] public int maxCount = 20;
    [Range(1, 64)] public int spawnsPerFrame = 4;
    [Header("One-side cluster and bounded placement")]
    [Min(0f)] public float clusterRadius = 2f;
    [Min(0.1f)] public float maxDeploymentTime = 3f;
    [Range(1, 64)] public int maxPlacementFailures = 12;
    public List<WeightedEnemySpawn> enemies = new List<WeightedEnemySpawn>();

    public void Validate()
    {
        activationInterval = SpawnSelection.Positive(activationInterval, 20);
        checkInterval = SpawnSelection.Positive(checkInterval, 5);
        maxWait = SpawnSelection.Positive(maxWait, 30);
        maxDeploymentTime = SpawnSelection.Positive(maxDeploymentTime, 3);
        activationChance = SpawnSelection.Percent(activationChance);
        initialChance = SpawnSelection.Percent(initialChance);
        increasePerFailure = SpawnSelection.Percent(increasePerFailure);
        populationCap = Mathf.Max(1, populationCap);
        minCount = Mathf.Clamp(minCount, 1, populationCap);
        maxCount = Mathf.Clamp(maxCount, minCount, populationCap);
        spawnsPerFrame = Mathf.Clamp(spawnsPerFrame, 1, 64);
        maxPlacementFailures = Mathf.Clamp(maxPlacementFailures, 1, 64);
        clusterRadius = SpawnSelection.NonNegative(clusterRadius);
    }
}

public enum SpawnRegion { ScreenEdges, PlayerRing }

[Serializable]
public class SpawnPlacementSettings
{
    public SpawnRegion region = SpawnRegion.ScreenEdges;
    public bool requireOutsideCamera = true;
    [Header("Screen-edge distance in world units")]
    [Min(0f)] public float minEdgeDistance = 2f;
    [Min(0f)] public float maxEdgeDistance = 4f;
    [Header("Player-ring alternative")]
    [Min(0f)] public float minRingRadius = 10f;
    [Min(0f)] public float maxRingRadius = 14f;
    [Header("Safety")]
    [Min(0f)] public float minPlayerDistance = 5f;
    [Min(0f)] public float cameraPadding = 0.5f;
    [Min(0f)] public float clearanceRadius = 0.35f;
    [Min(0f)] public float separation = 0.8f;
    [Range(1, 64)] public int attemptsPerEnemy = 16;

    public void Validate()
    {
        minEdgeDistance = SpawnSelection.NonNegative(minEdgeDistance);
        maxEdgeDistance = Mathf.Max(minEdgeDistance, SpawnSelection.NonNegative(maxEdgeDistance));
        minRingRadius = SpawnSelection.NonNegative(minRingRadius);
        maxRingRadius = Mathf.Max(minRingRadius, SpawnSelection.NonNegative(maxRingRadius));
        minPlayerDistance = SpawnSelection.NonNegative(minPlayerDistance);
        cameraPadding = SpawnSelection.NonNegative(cameraPadding);
        clearanceRadius = SpawnSelection.NonNegative(clearanceRadius);
        separation = SpawnSelection.NonNegative(separation);
        attemptsPerEnemy = Mathf.Clamp(attemptsPerEnemy, 1, 64);
    }
}

public static class SpawnSelection
{
    public static float NonNegative(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Mathf.Max(0f, value);
    public static float Positive(float value, float fallback) => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Max(0.1f, value);
    public static float Percent(float value) => Mathf.Clamp(NonNegative(value), 0f, 100f);

    // Works even if Inspector phase order is changed. Later equal-time entries win.
    public static SpawnPhase PhaseAt(List<SpawnPhase> phases, float elapsed)
    {
        SpawnPhase selected = null;
        if (phases == null) return null;
        foreach (SpawnPhase phase in phases)
            if (phase != null && phase.startTime <= elapsed && (selected == null || phase.startTime >= selected.startTime))
                selected = phase;
        return selected;
    }

    public static GameObject Pick(List<WeightedEnemySpawn> entries, float unitRoll)
    {
        if (entries == null) return null;
        double total = 0;
        foreach (WeightedEnemySpawn entry in entries) if (entry != null && entry.IsUsable) total += entry.weight;
        if (total <= 0) return null;
        double target = Mathf.Clamp01(unitRoll) * total;
        GameObject last = null;
        foreach (WeightedEnemySpawn entry in entries)
        {
            if (entry == null || !entry.IsUsable) continue;
            last = entry.prefab;
            if (target < entry.weight) return entry.prefab;
            target -= entry.weight;
        }
        return last; // Unity Random.value may equal 1.
    }
}
