using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RangedWeaponController : WeaponController
{
    private WeaponGrip grip;
    private RangedWeaponState state;
    private bool attacking;
    public override bool IsAttacking => isActiveAndEnabled && attacking && !LevelUpSelection.InputBlocked;
    public RangedWeaponState State => state;
    public int ShotsFired { get; private set; }
    public ProjectileWeaponBehaviour LastProjectile { get; private set; }

    protected override void Start()
    {
        base.Start();
        grip = GetComponent<WeaponGrip>();
        if (weaponData == null) { Debug.LogError(name + ": missing ranged weapon data.", this); return; }
        state = new RangedWeaponState(weaponData);
        SyncUpgrades();
    }

    private void SyncUpgrades() => state?.SetUpgrades(owner != null ? owner.AmmoUpgradeLevel : 0, owner != null ? owner.ReloadUpgradeLevel : 0,
        owner != null ? owner.ReloadSpeedMultiplier : 1f, owner != null ? owner.FireRateMultiplier : 1f);

    protected override void Update()
    {
        bool held = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        Tick(Time.deltaTime, held, overUI);
    }

    internal void Tick(float delta, bool held, bool overUI = false)
    {
        attacking = false;
        if (state == null || delta < 0f || float.IsNaN(delta) || float.IsInfinity(delta)) return;
        SyncUpgrades();
        if (Time.timeScale <= 0f || LevelUpSelection.InputBlocked || (owner != null && owner.IsDead))
        {
            state.Interrupt();
            return;
        }
        if (overUI)
        {
            state.Interrupt();
            state.Tick(delta, held);
            state.Interrupt(); // A release over UI may recover arrows, but cannot arm a click-through shot.
            return;
        }
        if (wh == null || weaponData.Prefab == null || !weaponData.Prefab.TryGetComponent<ProjectileWeaponBehaviour>(out _)) return;
        bool fired = state.Tick(delta, held);
        attacking = fired || state.IsCharging || (weaponData.FireMode != WeaponFireMode.Crossbow && held && state.Ammo > 0 && !state.IsReloading && !state.TriggerBlocked);
        if (!fired) return;
        Vector2 direction = wh.GetDirection();
        Vector3 origin = grip != null ? grip.MuzzlePosition : transform.position + (Vector3)direction * 0.45f;
        int count = Mathf.Clamp(weaponData.Projectiles + (owner != null ? owner.ExtraProjectiles : 0), 1, 32);
        float spread = Mathf.Clamp(weaponData.SpreadAngle + (owner != null ? owner.SpreadModifier : 0f), 0f, 180f);
        for (int i = 0; i < count; i++)
        {
            float angle = count == 1 ? 0f : Mathf.Lerp(-spread * 0.5f, spread * 0.5f, (float)i / (count - 1));
            Vector3 shotDirection = Quaternion.Euler(0f, 0f, angle) * direction;
            GameObject shot = Instantiate(weaponData.Prefab, origin, Quaternion.identity);
            LastProjectile = shot.GetComponent<ProjectileWeaponBehaviour>();
            LastProjectile.InitializeShot(weaponData, owner, shotDirection, state.ShotDamageFactor, state.ShotSizeFactor);
        }
        ShotsFired++;
    }

    private void OnDisable() { attacking = false; state?.Interrupt(); }
}
