using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class RangedWeaponController : WeaponController
{
    private WeaponGrip grip;
    private RangedWeaponState state;
    private PlayerStats owner;
    public RangedWeaponState State => state;
    public int ShotsFired { get; private set; }
    public ProjectileWeaponBehaviour LastProjectile { get; private set; }

    protected override void Start()
    {
        base.Start();
        owner = GetComponentInParent<PlayerStats>();
        grip = GetComponent<WeaponGrip>();
        if (weaponData == null) { Debug.LogError(name + ": missing ranged weapon data.", this); return; }
        state = new RangedWeaponState(weaponData);
        SyncUpgrades();
    }

    private void SyncUpgrades() => state?.SetUpgrades(owner != null ? owner.AmmoUpgradeLevel : 0, owner != null ? owner.ReloadUpgradeLevel : 0);

    protected override void Update()
    {
        bool held = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        Tick(Time.deltaTime, held, overUI);
    }

    internal void Tick(float delta, bool held, bool overUI = false)
    {
        if (state == null) return;
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
        if (!state.Tick(delta, held)) return;
        Vector2 direction = wh.GetDirection();
        Vector3 origin = grip != null ? grip.MuzzlePosition : transform.position + (Vector3)direction * 0.45f;
        GameObject shot = Instantiate(weaponData.Prefab, origin, Quaternion.identity);
        LastProjectile = shot.GetComponent<ProjectileWeaponBehaviour>();
        LastProjectile.InitializeShot(weaponData, owner, direction, state.ShotDamageFactor, state.ShotSizeFactor);
        ShotsFired++;
    }

    private void OnDisable() => state?.Interrupt();
}
