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

public sealed class RainbowSlimeRegression : MonoBehaviour
{
    private readonly List<string> checks = new List<string>();
    private GameObject sandbox;
    private EnemyScriptableObject enemyData;
    private int loads;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/System Leveling/Run Rainbow Slime Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        if (FindAnyObjectByType<RainbowSlimeRegression>() != null) return;
        var root = new GameObject("Rainbow Slime Regression", typeof(SoulTestIsolation)); DontDestroyOnLoad(root);
        root.AddComponent<RainbowSlimeRegression>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        string failure = ""; Time.timeScale = 1;
        SceneManager.sceneLoaded += Loaded;
        IEnumerator tests = Tests();
        while (true)
        {
            bool more = false; object wait = null;
            try { more = tests.MoveNext(); if (more) wait = tests.Current; }
            catch (Exception error) { failure = error.ToString(); }
            if (!more || failure.Length > 0) break;
            yield return wait;
        }
        SceneManager.sceneLoaded -= Loaded;
        Time.timeScale = 1;
        if (sandbox != null) DestroyImmediate(sandbox);
        if (enemyData != null) DestroyImmediate(enemyData);
        Directory.CreateDirectory("Captures/RainbowSlime");
        File.WriteAllText("Captures/RainbowSlime/regression.json", JsonUtility.ToJson(new Report
        { result = failure.Length == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failure = failure }, true));
        if (failure.Length == 0) Debug.Log("RAINBOW SLIME REGRESSION PASS: " + checks.Count);
        else Debug.LogError("RAINBOW SLIME REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private void Loaded(Scene scene, LoadSceneMode mode) => loads++;
    private void Check(bool ok, string name) { if (!ok) throw new InvalidOperationException(name); checks.Add(name); }
    private static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.001f;

    private IEnumerator Tests()
    {
        SceneManager.LoadSceneAsync(LoadoutLauncher.GameplayScene);
        float until = Time.realtimeSinceStartup + 20;
        while (loads < 1 && Time.realtimeSinceStartup < until) yield return null;
        Check(loads == 1, "Grass gameplay scene loaded"); yield return null;
        sandbox = new GameObject("Rainbow Test Fixtures");
        enemyData = Instantiate(AssetDatabase.LoadAssetAtPath<EnemyScriptableObject>("Assets/Scriptable Object/Enemy/ReferenceEnemies/Slime.asset"));
        var data = new SerializedObject(enemyData); data.FindProperty("maxHealth").floatValue = 500; data.ApplyModifiedPropertiesWithoutUndo();
        PlayerStats player = FindAnyObjectByType<PlayerStats>();
        foreach (var control in player.GetComponentsInChildren<WeaponController>()) control.enabled = false;
        player.GetComponent<PlayerController>().enabled = false;
        foreach (var enemy in FindObjectsByType<EnemyStats>()) DestroyImmediate(enemy.gameObject);
        EnemySpawner spawner = FindAnyObjectByType<EnemySpawner>(); spawner.enabled = false;
        ScheduleChecks();
        SpawnerChecks(spawner);
        foreach (var slime in FindObjectsByType<RainbowSlime>()) DestroyImmediate(slime.gameObject);
        TomeChecks(); ProjectileChecks(); LightningShotCheck();
        var old = sandbox.AddComponent<WeaponAmmoRegression>(); typeof(WeaponAmmoRegression).GetMethod("PureTests", Private).Invoke(old, null);
        Check(((List<string>)typeof(WeaponAmmoRegression).GetField("checks", Private).GetValue(old)).Count > 30, "Existing weapon scheduler regression still passes");
        var prefab = Resources.Load<RainbowSlime>("RainbowSlime");
        Check(prefab != null && prefab.GetComponent<DropRateManager>() == null, "Runtime prefab exists and has no ordinary loot roll");
        var expiring = Instantiate(prefab, new Vector3(10000, 0), Quaternion.identity, sandbox.transform);
        Check(!expiring.GetComponent<Enemy>().chasesPlayer && Near(expiring.GetComponent<EnemyStats>().currentHealth, 120), "Rainbow slime is non-chasing with 120 HP");
        int wallet = SoulWallet.Total, cards = FindObjectsByType<RedTomeCard>().Length;
        expiring.Tick(59.9f); Check(expiring.RemainingSeconds > 0 && !expiring.Expired, "Lifetime lasts until the 60-second boundary");
        float remaining = expiring.RemainingSeconds; Time.timeScale = 0; expiring.Tick(10);
        Check(Near(expiring.RemainingSeconds, remaining), "Pause does not spend slime lifetime"); Time.timeScale = 1;
        expiring.Tick(0.11f); expiring.GetComponent<EnemyStats>().Kill();
        Check(expiring.Expired && !expiring.gameObject.activeSelf && !expiring.RewardDropped && !expiring.GetComponent<EnemyStats>().IsDead, "Expiry disables immediately; a deferred hit cannot turn it into a rewarded death");
        Check(FindObjectsByType<RedTomeCard>().Length == cards && SoulWallet.Total == wallet, "Timeout grants no card or Soul");
        var killed = Instantiate(prefab, player.transform.position + Vector3.right * 3, Quaternion.identity, sandbox.transform);
        killed.Configure(spawner);
        yield return null;
        Directory.CreateDirectory("Captures/RainbowSlime"); ScreenCapture.CaptureScreenshot("Captures/RainbowSlime/rainbow-slime.png");
        yield return null;
        killed.Tick(59); killed.GetComponent<EnemyStats>().TakeDamage(1000);
        Check(killed.RewardDropped && FindObjectsByType<RedTomeCard>().Length == cards + 1, "Defeat before timeout drops exactly one real red-card prefab");
        killed.GetComponent<EnemyStats>().Kill(); killed.Defeated();
        Check(FindObjectsByType<RedTomeCard>().Length == cards + 1, "Repeated death callbacks never duplicate the card");
        var card = FindAnyObjectByType<RedTomeCard>(); var selection = FindAnyObjectByType<LevelUpSelection>();
        Time.timeScale = 0; Check(!card.Collect(player) && player.PendingTomeChoices == 0, "Card cannot be consumed while gameplay is paused"); Time.timeScale = 1;
        Check(card.Collect(player), "Actual card grants a special choice");
        Check(!card.Collect(player) && player.PendingTomeChoices == 1, "Repeated pickup callback cannot grant another offer");
        Check(selection.IsOpen && selection.IsChoosingTome && Time.timeScale == 0, "Existing gameplay modal presents the red-card choice and pauses");
        var offered = Enumerable.Range(0, 3).Select(selection.TomeChoiceAt).ToArray();
        Check(offered.Distinct().Count() == 3 && offered.All(player.CanReceiveTome), "Three distinct eligible Tomes are offered");
        selection.Choose(0); Check(player.PendingTomeChoices == 1, "Opening-frame click cannot select a Tome");
        Directory.CreateDirectory("Captures/RainbowSlime");
        ScreenCapture.CaptureScreenshot("Captures/RainbowSlime/red-card-choice.png");
        yield return null;
        selection.Choose(0); selection.Choose(1);
        Check(player.PendingTomeChoices == 0 && player.HasTome(offered[0]) && !player.HasTome(offered[1]) && Time.timeScale == 1, "Exactly one of three applies; repeated click ignored and gameplay resumes");
        Check(player.GrantTomeChoice(), "Second card remains available");
        Check(Enumerable.Range(0, 3).Select(selection.TomeChoiceAt).All(t => t != offered[0]), "Owned Tome is excluded from the second offer");
        yield return null; selection.Choose(0);
        Check(!player.GrantTomeChoice(), "Run cannot grant more than two special rewards");
        player.IncreaseExperience(player.experienceCap);
        Check(selection.IsOpen && !selection.IsChoosingTome && player.PendingUpgrades > 0, "Normal level-up remains a separate choice pool");
        yield return null; selection.Choose(0);
        Check(!selection.IsOpen && Time.timeScale == 1, "Normal upgrade selection still resumes gameplay");
        SceneManager.LoadSceneAsync(LoadoutLauncher.SnowScene);
        until = Time.realtimeSinceStartup + 20;
        while (loads < 2 && Time.realtimeSinceStartup < until) yield return null;
        Check(loads == 2, "Snow gameplay scene loaded"); yield return null;
        var snow = FindAnyObjectByType<EnemySpawner>(); snow.enabled = false;
        SpawnerChecks(snow);
        Check(FindAnyObjectByType<PlayerStats>().PendingTomeChoices == 0 && !FindAnyObjectByType<PlayerStats>().HasTome(TomeKind.Glass), "New run does not inherit Tome state");
    }

    private void ScheduleChecks()
    {
        var schedule = new RainbowSpawnSchedule(); int rolls = 0;
        schedule.Advance(419.99f, 0.5f, () => { rolls++; return 0; });
        Check(rolls == 0 && schedule.Pending == 0, "No spawn roll before minute 7");
        schedule.Advance(420, 0.5f, () => { rolls++; return 0; });
        Check(rolls == 1 && schedule.Pending == 1 && schedule.RecordSpawn(), "Minute 7 success is queued and recorded once");
        schedule.Advance(719.99f, 0.5f, () => { rolls++; return 0; }); Check(rolls == 1, "No repeated rolls between checkpoints");
        schedule.Advance(720, 0.5f, () => { rolls++; return 0; });
        Check(schedule.Pending == 1 && schedule.RecordSpawn() && schedule.Spawned == 2, "Minute 12 can create the second and final slime");
        schedule.Advance(99999, 1, () => { rolls++; return 0; });
        Check(rolls == 2 && schedule.Pending == 0 && !schedule.RecordSpawn(), "Long frame or further gameplay cannot exceed two spawns");
        schedule = new RainbowSpawnSchedule(); schedule.Advance(420, 0.5f, () => 0.9f); schedule.Advance(720, 0.5f, () => 0.9f);
        Check(schedule.Pending == 1, "Missed minute 7 guarantees one opportunity at minute 12");
        schedule = new RainbowSpawnSchedule(); schedule.Advance(420, 0.5f, () => 0); schedule.RecordSpawn(); schedule.Advance(720, 0.5f, () => 0.9f);
        Check(schedule.Spawned == 1 && schedule.Pending == 0, "Successful first roll and failed second produce one slime");
    }

    private void SpawnerChecks(EnemySpawner spawner)
    {
        var config = new SerializedObject(spawner);
        config.FindProperty("phases").ClearArray(); config.FindProperty("horde").FindPropertyRelative("enabled").boolValue = false;
        config.FindProperty("elite").FindPropertyRelative("enabled").boolValue = false;
        config.FindProperty("rainbowSpawnChance").floatValue = 1; config.ApplyModifiedPropertiesWithoutUndo();
        float elapsed = spawner.ElapsedTime; Time.timeScale = 0; spawner.Tick(500);
        Check(Near(spawner.ElapsedTime, elapsed) && spawner.TotalRainbowSpawned == 0, "Spawner schedule pauses with gameplay"); Time.timeScale = 1;
        spawner.Tick(420 - elapsed);
        Check(spawner.TotalRainbowSpawned == 1, "Real map spawner creates the minute-7 resource prefab");
        var slime = FindObjectsByType<RainbowSlime>().Last();
        Check(spawner.IsWalkableForSpecial(slime.transform.position, 0.45f), "Special enemy appears on safe walkable map ground");
        spawner.Tick(300); Check(spawner.TotalRainbowSpawned == 2, "Real map spawner creates minute-12 slime");
        spawner.Tick(1000); Check(spawner.TotalRainbowSpawned == 2, "Real map spawner enforces per-run cap");
    }

    private PlayerStats Player(TomeKind? tome = null)
    {
        var go = new GameObject("Tome fixture"); go.SetActive(false); go.transform.SetParent(sandbox.transform); go.transform.position = new Vector3(10000, 0);
        var stats = go.AddComponent<PlayerStats>(); stats.characterData = AssetDatabase.LoadAssetAtPath<CharacterScriptableObject>("Assets/Scriptable Object/Character/Mage.asset");
        stats.enabled = false; go.SetActive(true);
        if (tome.HasValue) { Check(stats.GrantTomeChoice() && stats.TryApplyTome(tome.Value), "Apply " + tome.Value); }
        return stats;
    }

    private void TomeChecks()
    {
        var p = Player(TomeKind.Elasticity); Check(p.BulletBounces == 2 && Near(p.ProjectileDamageMultiplier, 0.75f) && Near(p.MaxHealth, 11), "Elasticity values");
        p = Player(TomeKind.Electro); Check(Near(p.ReloadSpeedMultiplier, 1.25f) && Near(p.ProjectileDamageMultiplier, 0.84f), "Electro values");
        Check(p.GrantTomeChoice() && p.TryApplyTome(TomeKind.Elements) && Near(p.BurnDamageMultiplier, 1.35f) && Near(p.LightningDamageMultiplier, 1.35f) && Near(p.FreezeDurationMultiplier, 1.35f), "Elements scales all three elemental bonuses and requires an elemental source");
        p = Player(TomeKind.Glass);
        var dodge = new SerializedObject(p); dodge.FindProperty("dodgeChance").floatValue = 0.8f; dodge.ApplyModifiedPropertiesWithoutUndo();
        Check(Near(p.TomeDamageMultiplier, 2) && Near(p.EffectiveDodgeChance, 0), "Glass doubles damage and caps even an 80-percent dodge chance at zero"); p.TakeDamage(0.01f); Check(p.IsDead, "Glass makes even tiny accepted damage lethal");
        p = Player(TomeKind.Might); Check(Near(p.MaxHealth, 13) && Near(p.TotalBaseDamageBonus, 26), "Might includes its added max HP");
        p.IncreaseExperience(20); Check(p.TryApplyUpgrade(UpgradeKind.Vitality) && Near(p.TotalBaseDamageBonus, 32), "Might follows later max-HP changes");
        p = Player(TomeKind.Power); Check(Near(p.ProjectileDamageMultiplier, 1.5f) && Near(p.ProjectileSizeMultiplier, 2) && p.ExtraPierce == 1 && Near(p.FireRateMultiplier, 0.75f) && Near(p.MaxHealth, 11), "Power values");
        var book = AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Magic Book.asset");
        var powerState = new RangedWeaponState(book); powerState.SetUpgrades(0, 0, 1, p.FireRateMultiplier);
        Check(powerState.Tick(0, true) && !powerState.Tick(book.Cooldown, true) && powerState.Tick(book.Cooldown / 3f + 0.001f, true), "Power fire-rate penalty reaches the actual scheduler, not just the displayed stat");
        p = Player(TomeKind.Rage); Check(Near(p.FireRateMultiplier, 1.6f) && Near(p.SpreadModifier, 60) && p.BaseAmmoMultiplier == 3 && Near(p.ProjectileDamageMultiplier, 0.5f) && Near(p.KnockbackMultiplier, 0.05f), "Rage values");
        var state = new RangedWeaponState(AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Magic Book.asset"));
        state.SetUpgrades(2, 0, 1, 1, p.BaseAmmoMultiplier); Check(state.Capacity == 23 && state.Ammo == 7, "Rage triples base ammo but not Capacity upgrades and grants no free rounds");
        state = new RangedWeaponState(AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Crossbow.asset")); state.SetUpgrades(2, 0, 1, 1, 3);
        Check(state.Capacity == 3 && state.Ammo == 1, "Rage also expands crossbow base capacity without free ammo");
        p = Player(TomeKind.Speed); Check(Near(p.currentMoveSpeed, 4.5f) && Near(p.MaxHealth, 11), "Speed values");
        p = Player(TomeKind.Stillness); Check(Near(p.ProjectileDamageMultiplier, 2) && Near(p.ProjectileSizeMultiplier, 2) && Near(p.WalkSpeedMultiplier, 0) && Near(p.ReloadSpeedMultiplier, 0.5f), "Stillness values");
        var stillState = new RangedWeaponState(book); stillState.SetUpgrades(0, 0, p.ReloadSpeedMultiplier);
        Check(Near(stillState.ReloadDuration, book.ReloadTime * 2), "Stillness reload penalty reaches the real scheduler");
        p = Player(TomeKind.Vitality); Check(Near(p.MaxHealth, 16) && Near(p.currentHealth, 16), "Vitality grants and heals four HP");
        dodge = new SerializedObject(p); dodge.FindProperty("dodgeChance").floatValue = 0.8f; dodge.ApplyModifiedPropertiesWithoutUndo();
        Check(Near(p.EffectiveDodgeChance, 0.55f), "Vitality subtracts 25 percentage points of dodge");
        p = Player(TomeKind.Wind); Check(Near(p.currentMoveSpeed, 3.3f) && Near(p.TomeDamageMultiplier, 0.935f), "Wind includes its own move-speed bonus and damage penalty");
        p.IncreaseExperience(20); p.TryApplyUpgrade(UpgradeKind.MoveSpeed);
        Check(p.TomeDamageMultiplier > 0.935f, "Wind damage updates when normal speed upgrades are acquired");
        foreach (var kind in (TomeKind[])Enum.GetValues(typeof(TomeKind))) Check(TomeInfo.Title(kind).StartsWith("TOME OF ") && TomeInfo.Description(kind).Length > 10, "Tome name and description: " + kind);
    }

    private EnemyStats Target(Vector3 position)
    {
        var go = new GameObject("Lightning/bounce target"); go.SetActive(false); go.transform.SetParent(sandbox.transform); go.transform.position = position;
        var stats = go.AddComponent<EnemyStats>(); stats.enemyData = enemyData; go.AddComponent<CircleCollider2D>().isTrigger = true; go.SetActive(true); return stats;
    }

    private void ProjectileChecks()
    {
        var data = AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Magic Book.asset");
        var owner = Player(TomeKind.Power); var shot = Instantiate(data.Prefab, new Vector3(10000, 0), Quaternion.identity).GetComponent<ProjectileWeaponBehaviour>();
        float scale = shot.transform.localScale.x; shot.InitializeShot(data, owner, Vector3.right, 1, 1);
        Check(Near(shot.CurrentDamage, data.Damage * 1.5f) && Near(shot.transform.localScale.x, scale * 2) && shot.CurrentPierce == 2, "Actual projectile receives Power damage, size and piercing"); DestroyImmediate(shot.gameObject);
        owner = Player(TomeKind.Elasticity);
        var a = Target(new Vector3(10001, 0)); var b = Target(new Vector3(10002, 0)); var c = Target(new Vector3(10003, 0));
        shot = Instantiate(data.Prefab, new Vector3(10000, 0), Quaternion.identity).GetComponent<ProjectileWeaponBehaviour>(); shot.InitializeShot(data, owner, Vector3.right, 1, 1);
        shot.SendMessage("OnTriggerEnter2D", a.GetComponent<Collider2D>()); float health = a.currentHealth;
        shot.SendMessage("OnTriggerEnter2D", a.GetComponent<Collider2D>()); Check(Near(a.currentHealth, health) && shot.CurrentPierce == 1, "Ricochet never re-hits the same collider/target");
        shot.SendMessage("OnTriggerEnter2D", b.GetComponent<Collider2D>()); shot.SendMessage("OnTriggerEnter2D", c.GetComponent<Collider2D>());
        Check(shot.CurrentPierce == 0 && b.currentHealth < 500 && c.currentHealth < 500, "Two bounces reach two further enemies then exhaust the hit budget");
        DestroyImmediate(shot.gameObject); DestroyImmediate(a.gameObject); DestroyImmediate(b.gameObject); DestroyImmediate(c.gameObject);
    }

    private void LightningShotCheck()
    {
        var p = Player(TomeKind.Electro);
        Check(p.GrantTomeChoice() && p.TryApplyTome(TomeKind.Stillness), "Combine Electro with Stillness");
        var holderObject = new GameObject("Test hand", typeof(WeaponHolder)); holderObject.transform.SetParent(p.transform, false);
        // Hand Awake ran before parenting, so it does not equip a second weapon.
        var weaponObject = new GameObject("Test book", typeof(RangedWeaponController)); weaponObject.transform.SetParent(holderObject.transform, false);
        var weapon = weaponObject.GetComponent<RangedWeaponController>(); weapon.enabled = false;
        var data = Instantiate(AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Magic Book.asset"));
        var edit = new SerializedObject(data); edit.FindProperty("magazineSize").intValue = 1; edit.FindProperty("projectiles").intValue = 3; edit.ApplyModifiedPropertiesWithoutUndo();
        weapon.weaponData = data; weapon.SendMessage("Start");
        var target = Target(p.transform.position + Vector3.right * 2);
        int before = FindObjectsByType<ProjectileWeaponBehaviour>().Length;
        weapon.Tick(0, true);
        Check(weapon.ShotsFired == 1 && weapon.State.Ammo == 0 && Near(target.currentHealth, 478), "Last-ammo volley calls 22-damage lightning once, not once per projectile");
        Check(FindObjectsByType<ProjectileWeaponBehaviour>().Length == before + 4, "Stillness adds one real projectile to a three-projectile volley");
        DestroyImmediate(target.gameObject); DestroyImmediate(data);
        foreach (var projectile in FindObjectsByType<ProjectileWeaponBehaviour>()) DestroyImmediate(projectile.gameObject);
    }

    [Serializable] private class Report { public string result, utc, failure; public string[] checks; }
}
#endif
