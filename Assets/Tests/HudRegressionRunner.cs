#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Editor-only, repeatable Play Mode regression checks against real gameplay components.
// No assembly restructuring or NUnit dependency is required for this existing project.
public class HudRegressionRunner : MonoBehaviour
{
    private readonly List<string> passed = new List<string>();
    private GameObject sandbox;
    private GameObject testUi;
    private GameObject potionFixture;
    private GameObject experienceFixture;
    private PlayerHud liveHud;
    private bool liveHudWasActive;
    private float savedScale;
    private Mouse testMouse;
    private string originalAssets;
    private bool savedRunInBackground;

    [MenuItem("Tools/System Leveling/Run HUD Regression (Play Mode)")]
    public static void Begin()
    {
        if (!Application.isPlaying)
            throw new InvalidOperationException("Enter Play Mode before running HUD regression.");
        if (Object.FindAnyObjectByType<HudRegressionRunner>() != null)
            return;
        new GameObject("HUD Regression Runner").AddComponent<HudRegressionRunner>().StartCoroutine("Run");
    }

    private IEnumerator Run()
    {
        savedScale = Time.timeScale;
        savedRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        originalAssets = SourceSnapshot();
        IEnumerator checks = Checks();
        string failure = null;
        while (true)
        {
            bool more = false;
            object wait = null;
            try
            {
                more = checks.MoveNext();
                if (more) wait = checks.Current;
            }
            catch (Exception ex) { failure = ex.ToString(); }
            if (failure != null || !more)
                break;
            yield return wait;
        }
        Cleanup();
        var report = new RegressionReport { result = failure == null ? "PASS" : "FAIL", checks = passed.ToArray(), failure = failure ?? "", unity = Application.unityVersion };
        Directory.CreateDirectory("Captures/HudLevelUp");
        File.WriteAllText("Captures/HudLevelUp/regression.json", JsonUtility.ToJson(report, true));
        if (failure == null)
            Debug.Log($"HUD REGRESSION PASS: {passed.Count} checks. Captures/HudLevelUp/regression.json");
        else
            Debug.LogError("HUD REGRESSION FAIL: " + failure);
        Destroy(gameObject);
    }

    private IEnumerator Checks()
    {
        liveHud = Object.FindAnyObjectByType<PlayerHud>();
        Require(liveHud != null, "Scene HUD is wired");
        liveHudWasActive = liveHud.gameObject.activeSelf;
        liveHud.gameObject.SetActive(false);
        Time.timeScale = 1f;

        sandbox = new GameObject("Isolated HUD Test Player");
        sandbox.transform.position = new Vector3(5000, 5000, 0);
        sandbox.SetActive(false);
        PlayerStats player = sandbox.AddComponent<PlayerStats>();
        player.characterData = AssetDatabase.LoadAssetAtPath<CharacterScriptableObject>("Assets/Scriptable Object/Character/Mage.asset");
        player.levelRanges = new List<PlayerStats.LevelRange>
        {
            new PlayerStats.LevelRange {startLevel=1, endLevel=3, experienceCapIncrease=100},
            new PlayerStats.LevelRange {startLevel=4, endLevel=5, experienceCapIncrease=200}
        };
        sandbox.SetActive(true);
        sandbox.AddComponent<Rigidbody2D>().gravityScale = 0f;
        GameObject collector = new GameObject("Test Collector", typeof(CircleCollider2D), typeof(PlayerCollector));
        collector.transform.SetParent(sandbox.transform, false);
        collector.GetComponent<CircleCollider2D>().isTrigger = true;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/UI/System HUD.prefab");
        testUi = Instantiate(prefab);
        testUi.name = "Isolated HUD Test UI";
        PlayerHud hud = testUi.GetComponent<PlayerHud>();
        LevelUpSelection selection = testUi.GetComponent<LevelUpSelection>();
        Set(hud, "player", player);
        Set(selection, "player", player);
        GameOverController gameOver = testUi.GetComponent<GameOverController>();
        if (gameOver != null) Set(gameOver, "player", player);
        testUi.SetActive(true);
        yield return null;
        float initializationDeadline = Time.realtimeSinceStartup + 5f;
        while (player.experienceCap == 0 && Time.realtimeSinceStartup < initializationDeadline)
            yield return null;
        float baseHealth = player.characterData.MaxHealth;
        Require(player.MaxHealth == baseHealth && player.currentHealth == baseHealth && player.experienceCap == 100,
            $"Initial snapshot matches current Mage and LevelRange (max={player.MaxHealth}, HP={player.currentHealth}, cap={player.experienceCap})");
        Image hp = Get<Image>(hud, "healthFill");
        Image xp = Get<Image>(hud, "experienceFill");
        player.TakeDamage(baseHealth * 0.1f);
        string expectedHealthLabel = $"{Mathf.CeilToInt(baseHealth * 0.9f)} / {Mathf.CeilToInt(baseHealth)}";
        Require(Mathf.Approximately(hp.fillAmount, 0.9f) && Get<Text>(hud, "healthText").text == expectedHealthLabel, "Damage updates HUD");
        player.RestoreHealth(1000);
        Require(player.currentHealth == baseHealth && hp.fillAmount == 1, "Healing clamps and updates HUD");
        player.currentHealth = baseHealth * 0.5f;
        player.currentRecovery = 10;
        yield return new WaitForSeconds(0.12f);
        Require(player.currentHealth > baseHealth * 0.5f && player.currentHealth <= baseHealth, "Recovery still works");
        player.currentRecovery = 0;
        player.currentHealth = baseHealth * 0.5f;
        GameObject potionTemplate = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/red health potion.prefab");
        if (potionTemplate == null || potionTemplate.GetComponent<HealthPotion>() == null)
            potionTemplate = Object.FindAnyObjectByType<HealthPotion>().gameObject;
        Require(potionTemplate != null, "Configured potion pickup is available");
        GameObject potion = Instantiate(potionTemplate);
        potionFixture = potion;
        potion.transform.position = sandbox.transform.position + new Vector3(0.8f, 0, 0);
        potion.GetComponent<HealthPotion>().healthToRestore = 10;
        yield return new WaitForSeconds(0.12f);
        Require(potion == null && player.currentHealth == Mathf.Min(baseHealth, baseHealth * 0.5f + 10f),
            $"Potion trigger collects and heals once (remaining={potion != null}, HP={player.currentHealth})");
        GameObject drop = Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/EXP3.prefab"));
        experienceFixture = drop;
        drop.transform.position = sandbox.transform.position + new Vector3(0.4f, 0, 0);
        drop.GetComponent<Experience>().experienceGranted = 7;
        yield return new WaitForSeconds(0.2f);
        Require(drop == null && player.experience == 7, "Real EXP prefab is attracted and collected through PlayerCollector");
        player.IncreaseExperience(95 - player.experience);
        Require(Mathf.Approximately(xp.fillAmount, 0.95f), "EXP pickup updates fill");
        player.IncreaseExperience(555);
        Require(player.level == 4 && player.experience == 50 && player.experienceCap == 500 && player.PendingUpgrades == 3, "650 EXP preserves 50 remainder and queues exactly three upgrades");
        Require(selection.IsOpen && Time.timeScale == 0 && LevelUpSelection.InputBlocked, "Modal pauses gameplay and blocks input");
        Require(Get<Text>(hud, "levelText").text == "04" && Mathf.Approximately(xp.fillAmount, 0.1f), "HUD reflects final level and remainder");
        Require(selection.ChoiceAt(0) != selection.ChoiceAt(1) && selection.ChoiceAt(0) != selection.ChoiceAt(2) && selection.ChoiceAt(1) != selection.ChoiceAt(2), "Three offers are distinct");
        Vector3 position = Object.FindAnyObjectByType<PlayerController>().transform.position;
        Enemy enemy = Object.FindAnyObjectByType<Enemy>();
        Vector3 enemyPosition = enemy != null ? enemy.transform.position : Vector3.zero;
        yield return null;
        yield return null;
        Require(Object.FindAnyObjectByType<PlayerController>().transform.position == position && (enemy == null || enemy.transform.position == enemyPosition), "Player and enemy do not move while paused");
        int slashCount = Object.FindObjectsByType<MeleeWeaponBehaviour>().Length;
        testMouse = InputSystem.AddDevice<Mouse>("HUDRegressionMouse");
        InputSystem.QueueStateEvent(testMouse, new MouseState { position = new Vector2(2, 2), buttons = 1 });
        InputSystem.Update();
        foreach (MeleeWeaponController controller in Object.FindObjectsByType<MeleeWeaponController>())
            typeof(MeleeWeaponController).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(controller, null);
        Require(Object.FindObjectsByType<MeleeWeaponBehaviour>().Length == slashCount, "Mouse press while modal open creates no slash");
        InputSystem.RemoveDevice(testMouse);
        testMouse = null;

        Button[] buttons = Get<Button[]>(selection, "buttons");
        PointerEventData click = new PointerEventData(EventSystem.current);
        RectTransform cardRect = buttons[0].GetComponent<RectTransform>();
        click.position = RectTransformUtility.WorldToScreenPoint(null, cardRect.TransformPoint(cardRect.rect.center));
        List<RaycastResult> hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(click, hits);
        Require(hits.Count > 0 && hits[0].gameObject == buttons[0].gameObject, "GraphicRaycaster routes pointer to the upgrade card, not gameplay");
        ExecuteEvents.Execute(hits[0].gameObject, click, ExecuteEvents.pointerClickHandler);
        buttons[0].onClick.Invoke();
        Require(player.PendingUpgrades == 2 && Time.timeScale == 0, "Repeated button callbacks consume only one offer per frame");
        selection.Choose(-1);
        selection.Choose(3);
        Require(player.PendingUpgrades == 2, "Invalid card indices cannot consume offers");
        yield return null;
        buttons[1].onClick.Invoke();
        Require(player.PendingUpgrades == 1 && Time.timeScale == 0, "Second queued upgrade stays paused");
        yield return null;
        buttons[2].onClick.Invoke();
        Require(player.PendingUpgrades == 0 && Time.timeScale == 1 && !selection.IsOpen, "Final selection resumes previous time scale");
        Require(LevelUpSelection.InputBlocked, "Closing-frame input fence prevents click-through");
        yield return null;
        yield return null;
        Require(!LevelUpSelection.InputBlocked, "Input fence clears after closing");
        Require(!player.TryApplyUpgrade(UpgradeKind.Damage), "No reward without pending level");

        foreach (UpgradeKind kind in new[] {UpgradeKind.Damage, UpgradeKind.Damage, UpgradeKind.MoveSpeed, UpgradeKind.Magnet, UpgradeKind.Vitality})
        {
            float damage = player.DamageMultiplier;
            float speed = player.currentMoveSpeed;
            float magnet = player.currentMagnet;
            float maxHp = player.MaxHealth;
            float currentHp = player.currentHealth;
            player.IncreaseExperience(player.experienceCap - player.experience);
            Require(player.TryApplyUpgrade(kind), "Reward applied: " + kind);
            switch (kind)
            {
                case UpgradeKind.Damage:
                    Require(Mathf.Approximately(player.DamageMultiplier, damage + 0.1f), "Damage stacks additively");
                    break;
                case UpgradeKind.MoveSpeed:
                    Require(Mathf.Approximately(player.currentMoveSpeed, speed + player.characterData.MoveSpeed * 0.1f), "Move speed uses base 10 percent");
                    break;
                case UpgradeKind.Magnet:
                    Require(Mathf.Approximately(player.currentMagnet, magnet + player.characterData.Magnet * 0.2f), "Magnet uses base 20 percent");
                    break;
                case UpgradeKind.Vitality:
                    Require(player.MaxHealth == maxHp + 20 && player.currentHealth == Mathf.Min(player.MaxHealth, currentHp + 20), "Vitality adds 20 max HP and heals 20");
                    break;
            }
            yield return null;
        }
        player.RestoreHealth(9999);
        Require(player.currentHealth == player.MaxHealth && hp.fillAmount == 1, "Healing uses upgraded runtime maximum");

        GameObject slashPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Slash.prefab");
        GameObject slash = Instantiate(slashPrefab);
        WeaponScriptableObject sword = AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/Sword.asset");
        MeleeWeaponBehaviour melee = slash.GetComponent<MeleeWeaponBehaviour>();
        melee.Initialize(sword, player.transform, Vector2.right);
        Require(Mathf.Approximately(Get<float>(melee, "damage"), sword.Damage * player.DamageMultiplier), "Melee uses actual owner damage bonus");
        Destroy(slash);
        GameObject spellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Spell.prefab");
        GameObject spell = Instantiate(spellPrefab);
        ProjectileWeaponBehaviour projectile = spell.GetComponent<ProjectileWeaponBehaviour>();
        projectile.ApplyOwnerStats(player);
        Require(Mathf.Approximately(Get<float>(projectile, "currentDamage"), projectile.weaponData.Damage * player.DamageMultiplier), "Projectile uses actual owner damage bonus");
        projectile.ApplyOwnerStats(player);
        Require(Mathf.Approximately(Get<float>(projectile, "currentDamage"), projectile.weaponData.Damage * player.DamageMultiplier), "Applying projectile owner twice does not multiply twice");
        Destroy(spell);

        Time.timeScale = 0.5f;
        player.IncreaseExperience(player.experienceCap - player.experience);
        selection.enabled = false;
        Require(Time.timeScale == 0.5f && player.PendingUpgrades == 1, "Disabling modal restores prior scale and preserves pending reward");
        selection.enabled = true;
        Require(selection.IsOpen && Time.timeScale == 0, "Re-enable reopens pending offer");
        yield return null;
        selection.Choose(0);
        Require(Time.timeScale == 0.5f, "Selection restores non-default time scale");
        Time.timeScale = 0;
        player.IncreaseExperience(player.experienceCap - player.experience);
        yield return null;
        selection.Choose(0);
        Require(Time.timeScale == 0, "Pre-existing external pause is not released");
        Time.timeScale = 1;
        Require(originalAssets == SourceSnapshot(), "All original character/weapon/enemy assets remain byte-identical");
    }

    private void Require(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        passed.Add(name);
    }

    private static T Get<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).GetValue(target);
    }

    private static void Set(object target, string name, object value)
    {
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    private static string SourceSnapshot()
    {
        string result = "";
        foreach (string path in Directory.GetFiles("Assets/Scriptable Object", "*.asset", SearchOption.AllDirectories))
            result += path + File.ReadAllText(path);
        return result;
    }

    private void Cleanup()
    {
        if (testMouse != null) InputSystem.RemoveDevice(testMouse);
        if (potionFixture != null) DestroyImmediate(potionFixture);
        if (experienceFixture != null) DestroyImmediate(experienceFixture);
        if (testUi != null) DestroyImmediate(testUi);
        if (sandbox != null) DestroyImmediate(sandbox);
        if (liveHud != null) liveHud.gameObject.SetActive(liveHudWasActive);
        Time.timeScale = savedScale;
        Application.runInBackground = savedRunInBackground;
    }

    private void OnDestroy()
    {
        Cleanup();
    }

    [Serializable]
    private class RegressionReport
    {
        public string result;
        public string unity;
        public string[] checks;
        public string failure;
    }
}
#endif
