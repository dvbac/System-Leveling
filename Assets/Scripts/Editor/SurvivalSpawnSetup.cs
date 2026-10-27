using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

public static class SurvivalSpawnSetup
{
    [MenuItem("Tools/System Leveling/Configure Survival Spawning")]
    public static void Configure()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Configure spawning outside Play Mode.");
        EnemySpawner spawner = Object.FindAnyObjectByType<EnemySpawner>();
        PlayerStats player = Object.FindAnyObjectByType<PlayerStats>();
        if (spawner == null || player == null) throw new InvalidOperationException("Open the gameplay scene first.");
        GameObject blue = Load("Blue Slime");
        GameObject slime = Load("ReferenceEnemies/Slime");
        GameObject skeleton = Load("ReferenceEnemies/Skeleton");
        GameObject spider = Load("ReferenceEnemies/Spider");
        GameObject wolf = Load("ReferenceEnemies/Wolf");
        Undo.RecordObject(spawner, "Configure Survival + Horde");
        SerializedObject so = new SerializedObject(spawner);
        so.FindProperty("player").objectReferenceValue = player.transform;
        so.FindProperty("spawnCamera").objectReferenceValue = Camera.main;
        Transform enemies = GameObject.Find("Enemies")?.transform;
        if (enemies != null) so.FindProperty("enemyContainer").objectReferenceValue = enemies;
        Tilemap ground = GameObject.Find("Grid/Tilemap")?.GetComponent<Tilemap>();
        so.FindProperty("allowedGround").objectReferenceValue = ground;
        so.FindProperty("initialDelay").floatValue = 1;
        so.FindProperty("normalPopulationCap").intValue = 100;
        so.FindProperty("normalSpawnsPerFrame").intValue = 2;
        so.FindProperty("debugLogging").boolValue = true;

        SerializedProperty phases = so.FindProperty("phases");
        phases.arraySize = 4;
        Phase(phases.GetArrayElementAtIndex(0), "Slimes / opening", 0, 2.5f, 1, blue, slime);
        Phase(phases.GetArrayElementAtIndex(1), "Skeleton unlocked", 30, 2, 1, blue, slime, skeleton);
        Phase(phases.GetArrayElementAtIndex(2), "Spider unlocked", 60, 1.5f, 2, blue, slime, skeleton, spider);
        Phase(phases.GetArrayElementAtIndex(3), "Wolf unlocked", 120, 1, 3, blue, slime, skeleton, spider, wolf);
        SerializedProperty horde = so.FindProperty("horde");
        horde.FindPropertyRelative("enabled").boolValue = true;
        horde.FindPropertyRelative("activationInterval").floatValue = 20;
        horde.FindPropertyRelative("activationChance").floatValue = 25;
        horde.FindPropertyRelative("checkInterval").floatValue = 5;
        horde.FindPropertyRelative("initialChance").floatValue = 10;
        horde.FindPropertyRelative("increasePerFailure").floatValue = 15;
        horde.FindPropertyRelative("maxWait").floatValue = 30;
        horde.FindPropertyRelative("populationCap").intValue = 30;
        horde.FindPropertyRelative("minCount").intValue = 10;
        horde.FindPropertyRelative("maxCount").intValue = 20;
        horde.FindPropertyRelative("spawnsPerFrame").intValue = 4;
        horde.FindPropertyRelative("clusterRadius").floatValue = 2;
        horde.FindPropertyRelative("maxDeploymentTime").floatValue = 3;
        horde.FindPropertyRelative("maxPlacementFailures").intValue = 12;
        Pool(horde.FindPropertyRelative("enemies"), slime, skeleton);
        SerializedProperty placement = so.FindProperty("placement");
        placement.FindPropertyRelative("region").enumValueIndex = (int)SpawnRegion.ScreenEdges;
        placement.FindPropertyRelative("requireOutsideCamera").boolValue = true;
        placement.FindPropertyRelative("minEdgeDistance").floatValue = 2;
        placement.FindPropertyRelative("maxEdgeDistance").floatValue = 4;
        placement.FindPropertyRelative("minRingRadius").floatValue = 10;
        placement.FindPropertyRelative("maxRingRadius").floatValue = 14;
        placement.FindPropertyRelative("minPlayerDistance").floatValue = 5;
        placement.FindPropertyRelative("cameraPadding").floatValue = 0.5f;
        placement.FindPropertyRelative("clearanceRadius").floatValue = 0.35f;
        placement.FindPropertyRelative("separation").floatValue = 0.8f;
        placement.FindPropertyRelative("attemptsPerEnemy").intValue = 16;
        so.ApplyModifiedProperties();
        spawner.ValidateConfiguration();
        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
        // Do not SaveAssets(): user character/weapon data outside this migration must remain untouched.
        EditorSceneManager.SaveScene(spawner.gameObject.scene);
        Selection.activeGameObject = spawner.gameObject;
        Debug.Log("Survival + two-layer Horde configured: phases 0/30/60/120s, normal cap100, Horde cap30. Inspect Enemy Spawner to tune.");
    }

    private static GameObject Load(string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/" + name + ".prefab");
        if (prefab == null) throw new InvalidOperationException("Missing enemy prefab: " + name);
        return prefab;
    }

    private static void Phase(SerializedProperty property, string label, float start, float interval, int batch, params GameObject[] prefabs)
    {
        property.FindPropertyRelative("label").stringValue = label;
        property.FindPropertyRelative("startTime").floatValue = start;
        property.FindPropertyRelative("interval").floatValue = interval;
        property.FindPropertyRelative("enemiesPerBatch").intValue = batch;
        Pool(property.FindPropertyRelative("enemies"), prefabs);
    }

    private static void Pool(SerializedProperty pool, params GameObject[] prefabs)
    {
        pool.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
        {
            pool.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = prefabs[i];
            pool.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue = i < 2 ? 2 : 1;
        }
    }
}

[CustomEditor(typeof(EnemySpawner))]
public class EnemySpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (!Application.isPlaying) return;
        EnemySpawner spawner = (EnemySpawner)target;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime diagnostics (read only)", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.FloatField("Survival seconds", spawner.ElapsedTime);
            EditorGUILayout.TextField("Current phase", spawner.CurrentPhase != null ? spawner.CurrentPhase.label : "No valid phase");
            EditorGUILayout.IntField("Normal alive", spawner.NormalAliveCount);
            EditorGUILayout.IntField("Horde alive", spawner.HordeAliveCount);
            EditorGUILayout.TextField("Horde state", spawner.HordeState.ToString());
            EditorGUILayout.FloatField("Horde chance (%)", spawner.HordeChance);
            EditorGUILayout.FloatField("Layer 2 waiting seconds", spawner.HordeWaitElapsed);
            EditorGUILayout.IntField("Remaining deployment", spawner.HordeRemaining);
            EditorGUILayout.TextField("Last rejected position", spawner.LastPlacementFailure ?? "");
        }
    }

    public override bool RequiresConstantRepaint() => Application.isPlaying;
}
