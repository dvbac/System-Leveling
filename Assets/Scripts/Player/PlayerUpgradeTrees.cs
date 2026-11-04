using System.Collections.Generic;
using UnityEngine;

public partial class PlayerStats
{
    private readonly HashSet<UpgradeKind> learnedUpgrades = new HashSet<UpgradeKind>();
    private float angerUntil, freshUntil, unhurtTime, regenerationTime, blazeTime;
    private int killClipStacks, attackCount;
    private Vector3 lastUpgradePosition;
    private bool trackedUpgradePosition;
    private float lastWindBonus;
    public bool HasUpgrade(UpgradeKind kind) => learnedUpgrades.Contains(kind);
    public int TreeAmmoBonus => (HasUpgrade(UpgradeKind.LightBullets) ? 1 : 0) + (HasUpgrade(UpgradeKind.ArmedReady) ? 2 : 0);
    public float WindBonus => HasUpgrade(UpgradeKind.InTheWind) ? Mathf.Min(4, Mathf.FloorToInt(unhurtTime / 10f)) * .1f : 0f;
    public float CombatDamageMultiplier => (Time.time < angerUntil ? 1.5f : 1f) * (Time.time < freshUntil ? 1.5f : 1f) * (1f + WindBonus);
    public float EffectiveFireRate => FireRateMultiplier * (Time.time < angerUntil ? 1.5f : 1f);
    public float EffectiveReloadRate => ReloadSpeedMultiplier + killClipStacks * .05f;
    public float AttackMovementFactor => HasUpgrade(UpgradeKind.RunAndGun) ? .9f : .45f;
    public bool IsStationary => !TryGetComponent(out PlayerController movement) || movement.moveDir.sqrMagnitude < .01f;
    public float TreeProjectileSize => HasUpgrade(UpgradeKind.BigShot) ? 1.4f : 1f;
    public int TreePierce => HasUpgrade(UpgradeKind.ReaperRounds) ? 1 : 0;
    public int TreeBounces => HasUpgrade(UpgradeKind.RubberBullets) ? 1 : 0;

    private bool CanReceiveTreeUpgrade(UpgradeKind kind)
    {
        if (HasUpgrade(kind)) return false;
        var tree = UpgradeCatalog.Find(kind);
        if (tree == null) return false;
        bool ranged = tree.Root == UpgradeKind.DoubleShot || tree.Root == UpgradeKind.QuickHands ||
            kind == UpgradeKind.LightBullets || kind == UpgradeKind.RubberBullets || kind == UpgradeKind.Siege ||
            kind == UpgradeKind.ReaperRounds || kind == UpgradeKind.Splinter || kind == UpgradeKind.IceShard;
        if (ranged && !HasRangedWeapon) return false;
        if (kind == tree.Root) return true;
        if (!HasUpgrade(tree.Root)) return false;
        return kind != tree.Final || HasUpgrade(tree.A) || HasUpgrade(tree.B);
    }

    private void ApplyTreeUpgrade(UpgradeKind kind)
    {
        switch (kind)
        {
            case UpgradeKind.RapidFire: FireRateMultiplier += .25f; break;
            case UpgradeKind.BigShot: DamageMultiplier *= 1.35f; break;
            case UpgradeKind.ReaperRounds: DamageMultiplier *= 1.2f; break;
            case UpgradeKind.LightBullets: FireRateMultiplier += .15f; ProjectileSpeedMultiplier += .15f; break;
            case UpgradeKind.RubberBullets: FireRateMultiplier += .1f; break;
            case UpgradeKind.Fusillade: ExtraProjectiles++; SpreadModifier += 15f; ProjectileDamageMultiplier *= .8f; break;
            case UpgradeKind.ArmedReady: ReloadSpeedMultiplier += .1f; break;
            case UpgradeKind.FreshClip: ReloadSpeedMultiplier += .05f; break;
            case UpgradeKind.Giant: ChangeMaxHealth(6f); transform.localScale *= 1.15f; break;
            case UpgradeKind.BlazingSpeed: currentMoveSpeed += characterData.MoveSpeed * .1f; break;
            case UpgradeKind.Regeneration: regenerationTime = 0f; break;
            case UpgradeKind.InTheWind: unhurtTime = 0f; break;
        }
    }

    private void RecordUpgrade(UpgradeKind kind)
    {
        learnedUpgrades.Add(kind);
        var tree = UpgradeCatalog.Find(kind);
        UpgradeEffect.Burst(transform.position, tree != null ? tree.Color : new Color(1f, .85f, .4f), 16, 1.5f);
    }

    private void TickUpgradeTrees()
    {
        if (IsDead || GameOverController.GameplayStopped || Time.deltaTime <= 0f) return;
        unhurtTime += Time.deltaTime;
        if (WindBonus > lastWindBonus) UpgradeEffect.Burst(transform.position, new Color(.35f, 1f, .8f), 10, 1f);
        lastWindBonus = WindBonus;
        if (HasUpgrade(UpgradeKind.Regeneration))
        {
            regenerationTime += Time.deltaTime;
            if (regenerationTime >= 30f) { regenerationTime -= 30f; RestoreHealth(1); }
        }
        blazeTime -= Time.deltaTime;
        bool moved = trackedUpgradePosition && (transform.position - lastUpgradePosition).sqrMagnitude > .000001f;
        if (moved && HasUpgrade(UpgradeKind.BlazingSpeed) && blazeTime <= 0f)
        {
            blazeTime = .35f;
            UpgradeEffect.Burst(transform.position, new Color(1f, .4f, .1f), 5, .7f);
            foreach (var enemy in FindObjectsByType<EnemyStats>())
                if (!enemy.IsDead && (enemy.transform.position - transform.position).sqrMagnitude <= 2.25f)
                    UpgradeHit.Capture(this).Apply(enemy, 0, false, true);
        }
        lastUpgradePosition = transform.position; trackedUpgradePosition = true;
    }

    private void OnUpgradeHurt()
    {
        unhurtTime = 0f;
        if (!HasUpgrade(UpgradeKind.AngerPoint)) return;
        angerUntil = Time.time + 10f;
        UpgradeEffect.Burst(transform.position, new Color(1f, .25f, .3f), 12, 1.2f);
    }

    public void OnAmmoRecovered()
    {
        killClipStacks = 0;
        if (!HasUpgrade(UpgradeKind.FreshClip)) return;
        freshUntil = Time.time + 1f;
        UpgradeEffect.Burst(transform.position, new Color(.4f, .85f, 1f), 8, 1f);
    }

    public void OnUpgradeKill(EnemyStats enemy, bool secondary)
    {
        if (IsDead || GameOverController.GameplayStopped) return;
        if (HasUpgrade(UpgradeKind.KillClip)) killClipStacks = Mathf.Min(20, killClipStacks + 1);
        if (secondary || !HasUpgrade(UpgradeKind.Splinter) || !HasRangedWeapon) return;
        var weapon = GetComponentInChildren<WeaponHolder>().CurrentWeapon.weaponData;
        for (int i = 0; i < 3; i++) SpawnUpgradeProjectile(weapon, enemy.transform.position, Quaternion.Euler(0, 0, i * 120f) * Vector3.right, .1f, false, false);
    }

    public void OnUpgradeAttack(WeaponScriptableObject weapon, Vector3 origin, Vector3 direction, bool lastAmmo)
    {
        attackCount++;
        var color = HasUpgrade(UpgradeKind.PyroMage) ? UpgradeEffect.Fire : HasUpgrade(UpgradeKind.FrostMage) ? UpgradeEffect.Ice : new Color(1f, .85f, .4f);
        UpgradeEffect.Burst(origin, color, 3, .25f);
        if (HasUpgrade(UpgradeKind.FireStarter) && attackCount % 5 == 0)
            SpawnUpgradeProjectile(weapon, origin, direction, 1f, false, true);
        if (!HasRangedWeapon) return;
        if (HasUpgrade(UpgradeKind.SplitFire)) SpawnUpgradeProjectile(weapon, origin, -direction, 1f, false, false, false);
        if (lastAmmo && HasUpgrade(UpgradeKind.FanFire))
            for (int i = 0; i < 10; i++) SpawnUpgradeProjectile(weapon, origin, Quaternion.Euler(0, 0, i * 36f) * direction, .15f, false, false);
        if (lastAmmo && HasUpgrade(UpgradeKind.IceShard))
            for (int i = -1; i <= 1; i++) SpawnUpgradeProjectile(weapon, origin, Quaternion.Euler(0, 0, i * 18f) * direction, .25f, true, false);
    }

    private void SpawnUpgradeProjectile(WeaponScriptableObject weapon, Vector3 position, Vector3 direction, float factor, bool ice, bool fireball, bool secondary = true)
    {
        // Sword's slash prefab is not a projectile; fireballs use their own collider and visual.
        ProjectileWeaponBehaviour shot;
        if (fireball)
        {
            var go = new GameObject("Upgrade Fireball"); go.SetActive(false); go.transform.position = position;
            var body = go.AddComponent<Rigidbody2D>(); body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0;
            go.AddComponent<CircleCollider2D>().isTrigger = true; go.GetComponent<CircleCollider2D>().radius = .2f;
            shot = go.AddComponent<ProjectileWeaponBehaviour>();
        }
        else
        {
            if (weapon.Prefab == null || !weapon.Prefab.TryGetComponent<ProjectileWeaponBehaviour>(out _)) return;
            shot = Instantiate(weapon.Prefab, position, Quaternion.identity).GetComponent<ProjectileWeaponBehaviour>();
        }
        shot.InitializeShot(weapon, this, direction, factor, fireball ? 1f : .7f);
        shot.ConfigureUpgradeProjectile(secondary, ice, fireball);
        shot.gameObject.SetActive(true);
    }
}
