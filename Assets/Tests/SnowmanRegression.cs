#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public class SnowmanRegression : MonoBehaviour
{
    private readonly List<string> checks = new List<string>();
    private readonly List<GameObject> pausedRoots = new List<GameObject>();
    private GameObject sandbox;
    private PlayerController originalPlayer;
    private CharacterScriptableObject dummyData;
    private GameOverController originalGameOver;
    private static readonly FieldInfo GameOverActive = typeof(GameOverController).GetField("active", BindingFlags.Static | BindingFlags.NonPublic);
    private bool background, cleaned;
    private float previousScale;

    [MenuItem("Tools/System Leveling/Run Snowman Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<SnowmanRegression>() != null) return;
        GameObject root = new GameObject("Snowman Regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root);
        root.AddComponent<SnowmanRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        background = Application.runInBackground; previousScale = Time.timeScale;
        Application.runInBackground = true; originalPlayer = PlayerController.Instance;
        originalGameOver = (GameOverController)GameOverActive.GetValue(null);
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.activeSelf) { pausedRoots.Add(root); root.SetActive(false); }
        Time.timeScale = 1;
        // A hidden Game Over view deliberately keeps its gameplay gate; isolate only this test session.
        GameOverActive.SetValue(null, null);
        IEnumerator tests = Tests(); string failure = "";
        while (true)
        {
            bool more = false; object wait = null;
            try { more = tests.MoveNext(); if (more) wait = tests.Current; }
            catch (Exception error) { failure = error.ToString(); }
            if (!more || failure.Length > 0) break;
            yield return wait;
        }
        Cleanup();
        Directory.CreateDirectory("Captures/Snowman");
        File.WriteAllText("Captures/Snowman/runtime-validation.json", JsonUtility.ToJson(new Report
        { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("SNOWMAN RUNTIME PASS: " + checks.Count);
        else Debug.LogError("SNOWMAN RUNTIME FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Tests()
    {
        Check(!GameOverController.GameplayStopped, "Fixture is independent of a previously finished gameplay run");
        sandbox = new GameObject("Snowman fixtures");
        GameObject target = new GameObject("Snowman contact target"); target.SetActive(false); target.transform.SetParent(sandbox.transform);
        target.transform.position = new Vector3(10003, 10000); target.tag = "Player";
        dummyData = Instantiate(AssetDatabase.LoadAssetAtPath<CharacterScriptableObject>("Assets/Scriptable Object/Character/Mage.asset"));
        SerializedObject data = new SerializedObject(dummyData); data.FindProperty("maxHealth").floatValue = 100; data.FindProperty("recovery").floatValue = 0; data.ApplyModifiedPropertiesWithoutUndo();
        PlayerStats player = target.AddComponent<PlayerStats>(); player.characterData = dummyData; player.invincibilityDuration = 0.5f;
        target.AddComponent<PlayerController>().enabled = false;
        Rigidbody2D body = target.AddComponent<Rigidbody2D>(); body.gravityScale = 0; body.constraints = RigidbodyConstraints2D.FreezeAll;
        BoxCollider2D collider = target.AddComponent<BoxCollider2D>(); collider.size = new Vector2(0.5f, 0.9f); collider.offset = new Vector2(0, 0.45f);
        target.SetActive(true);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/ReferenceEnemies/Snow/Snowman.prefab");
        GameObject monster = Instantiate(prefab, new Vector3(10000, 10000), Quaternion.identity, sandbox.transform);
        EnemyStats stats = monster.GetComponent<EnemyStats>(); Enemy movement = monster.GetComponent<Enemy>();
        Animator animator = monster.GetComponent<Animator>(); animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Check(stats.currentHealth == 14 && stats.currentMoveSpeed == 1.6f && stats.currentDamage == 7, "Actual prefab initializes Snowman stats 14/1.6/7");
        float distance = Vector3.Distance(monster.transform.position, target.transform.position);
        yield return new WaitForSeconds(0.22f);
        Check(Vector3.Distance(monster.transform.position, target.transform.position) < distance - 0.1f, "Snowman follows the player using the existing Enemy component");
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Move"), "Moving plays the generated Move animation");
        movement.enabled = false;
        Rigidbody2D monsterBody = monster.GetComponent<Rigidbody2D>();
        monsterBody.linearVelocity = Vector2.zero; monsterBody.angularVelocity = 0; monsterBody.simulated = false;
        float idleDeadline = Time.realtimeSinceStartup + 1;
        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Idle") && Time.realtimeSinceStartup < idleDeadline) yield return null;
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Idle"), "Stopped Snowman returns to Idle");
        monsterBody.simulated = true; movement.enabled = true;
        target.transform.position = monster.transform.position + new Vector3(0.3f, 0); Physics2D.SyncTransforms();
        yield return new WaitForFixedUpdate(); yield return new WaitForFixedUpdate(); yield return new WaitForSeconds(0.06f);
        Check(player.currentHealth == 92, "Real 2D collision deals eight contact damage and respects player i-frames");
        target.transform.position += Vector3.right * 10; Physics2D.SyncTransforms();
        stats.TakeDamage(10); yield return null;
        Check(stats.currentHealth == 35 && !stats.IsDead, "Snowman takes actual enemy damage");
        Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Hit"), "Damage plays Hit without interrupting the enemy system");
        GameObject dropTemplate = new GameObject("Controlled snowman drop"); dropTemplate.transform.SetParent(sandbox.transform);
        dropTemplate.AddComponent<SnowmanDropMarker>();
        monster.GetComponent<DropRateManager>().drops = new List<DropRateManager.Drops>
        { new DropRateManager.Drops { name = "Test", itemPrefap = dropTemplate, dropRate = 100 } };
        int drops = FindObjectsByType<SnowmanDropMarker>().Length;
        Vector3 deathPosition = monster.transform.position;
        stats.TakeDamage(1000); stats.Kill();
        Check(stats.IsDead && !movement.enabled && !monster.GetComponent<Rigidbody2D>().simulated && !monster.GetComponent<Collider2D>().enabled,
            "Death immediately stops movement, physics and body collision");
        Check(FindObjectsByType<SnowmanDropMarker>().Length == drops + 1, "Repeated death drops loot exactly once");
        yield return null;
        Check(monster != null && animator.GetCurrentAnimatorStateInfo(0).IsName("Die"), "Snowman remains alive long enough to play its melt animation");
        yield return new WaitForSeconds(0.2f);
        Check(monster != null && monster.transform.position == deathPosition, "Death animation never drags the world transform");
        yield return new WaitForSeconds(0.7f);
        Check(monster == null, "Snowman is destroyed after the existing death-animation duration");
    }

    private void Cleanup()
    {
        if (cleaned) return; cleaned = true;
        foreach (SnowmanDropMarker drop in FindObjectsByType<SnowmanDropMarker>()) DestroyImmediate(drop.gameObject);
        if (sandbox != null) DestroyImmediate(sandbox);
        if (dummyData != null) DestroyImmediate(dummyData);
        foreach (GameObject root in pausedRoots) if (root != null) root.SetActive(true);
        if (originalPlayer != null) originalPlayer.SendMessage("Awake");
        GameOverActive.SetValue(null, originalGameOver);
        Application.runInBackground = background; Time.timeScale = previousScale;
    }
    private void OnDestroy() => Cleanup();
    private void Check(bool pass, string label) { if (!pass) throw new Exception(label); checks.Add(label); }
    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
public class SnowmanDropMarker : MonoBehaviour { }
#endif
