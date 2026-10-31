#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Runs real spawning, movement, sprite playback, damage, death and drops in Play Mode.
public class ReferenceEnemyRegression : MonoBehaviour
{
    private GameObject sandbox;
    private readonly List<string> passed = new List<string>();
    private bool background;
    private float timeScale;
    private bool baselineLoaded;
    private EnemySpawner sceneSpawner;
    private bool sceneSpawnerWasEnabled;

    [MenuItem("Tools/System Leveling/Run Reference Enemy Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        GameObject root = new GameObject("Reference Enemy Regression");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<ReferenceEnemyRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground;
        timeScale = Time.timeScale;
        Application.runInBackground = true;
        Time.timeScale = 1;
        SceneManager.sceneLoaded += OnSceneLoaded;
        IEnumerator checks = Checks();
        string failure = null;
        while (true)
        {
            bool more = false;
            object wait = null;
            try { more = checks.MoveNext(); if (more) wait = checks.Current; }
            catch (Exception ex) { failure = ex.ToString(); }
            if (failure != null || !more) break;
            yield return wait;
        }
        var report = new Report { result = failure == null ? "PASS" : "FAIL", checks = passed.ToArray(), failure = failure ?? "" };
        Directory.CreateDirectory("Captures/ReferenceAnimation");
        File.WriteAllText("Captures/ReferenceAnimation/runtime-regression.json", JsonUtility.ToJson(report, true));
        foreach (var drop in Object.FindObjectsByType<ReferenceEnemyDropMarker>())
            Object.Destroy(drop.gameObject);
        Object.Destroy(sandbox);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (sceneSpawner != null) sceneSpawner.enabled = sceneSpawnerWasEnabled;
        Application.runInBackground = background;
        Time.timeScale = timeScale;
        if (failure == null) Debug.Log($"REFERENCE ENEMY REGRESSION PASS: {passed.Count} checks.");
        else Debug.LogError("REFERENCE ENEMY REGRESSION FAIL: " + failure);
        Object.Destroy(gameObject);
    }

    private IEnumerator Checks()
    {
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().path);
        float deadline = Time.realtimeSinceStartup + 15;
        while (!baselineLoaded && Time.realtimeSinceStartup < deadline) yield return null;
        Check(baselineLoaded && !GameOverController.GameplayStopped, "Fresh reference-enemy test baseline is not a previously dead run");
        yield return null;
        sceneSpawner = Object.FindAnyObjectByType<EnemySpawner>();
        sceneSpawnerWasEnabled = sceneSpawner.enabled;
        sceneSpawner.enabled = false;
        sandbox = new GameObject("Reference Enemy Test Sandbox");
        Check(PlayerController.Instance != null, "Gameplay player exists");
        Vector3 anchor = PlayerController.Instance.transform.position + new Vector3(100, 100);
        var fakePlayer = new GameObject("Spawn Anchor");
        fakePlayer.transform.SetParent(sandbox.transform);
        fakePlayer.transform.position = anchor;
        var fixture = new GameObject("Test Drop Template");
        fixture.transform.SetParent(sandbox.transform);
        fixture.AddComponent<ReferenceEnemyDropMarker>();
        int baseline = Object.FindObjectsByType<ReferenceEnemyDropMarker>().Length;

        foreach (string name in new[] { "Slime", "Skeleton", "Wolf", "Spider" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefab/ReferenceEnemies/{name}.prefab");
            var container = new GameObject(name + " Test Enemies");
            container.transform.SetParent(sandbox.transform);
            var spawnObject = new GameObject(name + " Test Spawner");
            spawnObject.transform.SetParent(sandbox.transform);
            spawnObject.SetActive(false);
            var spawner = spawnObject.AddComponent<EnemySpawner>();
            var serialized = new SerializedObject(spawner);
            serialized.FindProperty("player").objectReferenceValue = fakePlayer.transform;
            serialized.FindProperty("enemyContainer").objectReferenceValue = container.transform;
            var phases = serialized.FindProperty("phases");
            phases.arraySize = 1;
            var phase = phases.GetArrayElementAtIndex(0);
            phase.FindPropertyRelative("startTime").floatValue = 0;
            phase.FindPropertyRelative("interval").floatValue = 0.1f;
            phase.FindPropertyRelative("enemiesPerBatch").intValue = 1;
            var pool = phase.FindPropertyRelative("enemies");
            pool.arraySize = 1;
            pool.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = prefab;
            pool.GetArrayElementAtIndex(0).FindPropertyRelative("weight").floatValue = 1;
            serialized.FindProperty("initialDelay").floatValue = 0;
            serialized.FindProperty("normalPopulationCap").intValue = 1;
            serialized.FindProperty("horde").FindPropertyRelative("enabled").boolValue = false;
            serialized.FindProperty("debugLogging").boolValue = false;
            var placement = serialized.FindProperty("placement");
            placement.FindPropertyRelative("region").enumValueIndex = (int)SpawnRegion.PlayerRing;
            placement.FindPropertyRelative("requireOutsideCamera").boolValue = false;
            placement.FindPropertyRelative("minRingRadius").floatValue = 0;
            placement.FindPropertyRelative("maxRingRadius").floatValue = 0;
            placement.FindPropertyRelative("minPlayerDistance").floatValue = 0;
            placement.FindPropertyRelative("separation").floatValue = 0;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            spawnObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            Check(container.transform.childCount == 1, name + ": actual EnemySpawner created prefab");
            var enemy = container.transform.GetChild(0).gameObject;
            Vector3 initial = enemy.transform.position;
            var stats = enemy.GetComponent<EnemyStats>();
            var animator = enemy.GetComponent<Animator>();
            var renderer = enemy.GetComponent<SpriteRenderer>();
            Sprite first = renderer.sprite;
            yield return new WaitForSeconds(0.2f);
            Check(container.transform.childCount == 1, name + ": maxEnemies limit respected");
            Check((enemy.transform.position - initial).sqrMagnitude > 0.001f, name + ": hunts player");
            Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Move") && renderer.sprite != first,
                name + ": Move clip advances sprites");
            Check(renderer.flipX == (name != "Skeleton" && name != "Spider"), name + ": correct left/right facing");
            spawner.enabled = false;
            var drops = enemy.GetComponent<DropRateManager>();
            drops.drops = new List<DropRateManager.Drops>
            {
                new DropRateManager.Drops { name = "Regression drop", itemPrefap = fixture, dropRate = 100 }
            };
            float health = stats.currentHealth;
            stats.TakeDamage(1);
            yield return null;
            Check(stats.currentHealth == health - 1 && animator.GetCurrentAnimatorStateInfo(0).IsName("Hit"),
                name + ": nonlethal damage plays Hit");
            yield return new WaitForSeconds(0.4f);
            Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Move"), name + ": Hit returns to movement");
            stats.TakeDamage(10000);
            stats.Kill();
            stats.TakeDamage(1);
            yield return null;
            Check(enemy != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Die"), name + ": death animation before despawn");
            Check(!enemy.GetComponent<Enemy>().enabled && !enemy.GetComponent<Collider2D>().enabled &&
                !enemy.GetComponent<Rigidbody2D>().simulated, name + ": corpse cannot chase or collide");
            Check(Object.FindObjectsByType<ReferenceEnemyDropMarker>().Length == baseline + 1,
                name + ": one drop despite repeated death/damage calls");
            Vector3 deathPosition = enemy.transform.position;
            yield return new WaitForSeconds(0.2f);
            Check(enemy != null && enemy.transform.position == deathPosition, name + ": corpse stays still");
            yield return new WaitForSeconds(0.75f);
            Check(enemy == null, name + ": destroyed after Die clip");
            foreach (var drop in Object.FindObjectsByType<ReferenceEnemyDropMarker>())
                if (drop.gameObject != fixture) Object.Destroy(drop.gameObject);
            Object.Destroy(spawnObject);
            Object.Destroy(container);
            yield return null;
        }
        var legacyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Blue Slime.prefab");
        bool syntheticLegacy = legacyPrefab == null;
        if (syntheticLegacy) legacyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Slime.prefab");
        var legacy = Object.Instantiate(legacyPrefab, anchor, Quaternion.identity, sandbox.transform);
        if (syntheticLegacy) Object.DestroyImmediate(legacy.GetComponent<EnemyAnimation>());
        legacy.GetComponent<DropRateManager>().drops.Clear();
        legacy.GetComponent<EnemyStats>().Kill();
        yield return null;
        Check(legacy == null, "Legacy enemy without EnemyAnimation retains immediate destruction");
    }

    private void Check(bool condition, string label)
    {
        if (!condition) throw new InvalidOperationException(label);
        passed.Add(label);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { GameplayRegressionBaseline.Prepare(); baselineLoaded = true; }

    [Serializable]
    private class Report { public string result; public string[] checks; public string failure; }
}

public class ReferenceEnemyDropMarker : MonoBehaviour { }
#endif
