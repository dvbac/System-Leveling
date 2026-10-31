using UnityEngine;

public enum WeaponFireMode { Magazine, Bow, Crossbow }

[CreateAssetMenu(
    fileName = "WeaponData",
    menuName = "Scriptable Objects/Weapon"
)]
public class WeaponScriptableObject : ScriptableObject
{
    [Header("Prefab")]
    [SerializeField] private GameObject prefab;

    [Header("Base Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private float cooldown;
    [SerializeField] private int pierce;

    [Header("Melee Stats")]
    [SerializeField] private float swingAngle;
    [SerializeField] private float range;

    [Header("Ranged ammo (does not affect melee)")]
    [SerializeField] private WeaponFireMode fireMode;
    [SerializeField, Min(1)] private int magazineSize = 8;
    [SerializeField, Min(0.05f)] private float reloadTime = 1.8f;
    [SerializeField, Min(0f)] private float bowRecoveryDelay = 0.45f;
    [Header("Crossbow charge")]
    [SerializeField, Min(0.05f)] private float chargeTime = 3f;
    [SerializeField, Min(1f)] private float maxChargeDamage = 4f;
    [SerializeField, Min(1f)] private float maxChargeSize = 2f;
    [SerializeField, Min(0f)] private float chargeTimePerAmmo = 0.5f;
    [SerializeField, Min(0f)] private float chargeDamagePerAmmo = 0.5f;
    [SerializeField, Min(0f)] private float chargeSizePerAmmo = 1f / 6f;
    [Header("Reload upgrade")]
    [SerializeField, Min(0f)] private float reloadSpeedPerLevel = 0.2f;

    public GameObject Prefab => prefab;

    public float Damage => damage;
    public float Speed => speed;
    public float Cooldown => cooldown;
    public int Pierce => pierce;

    public float SwingAngle => swingAngle;
    public float Range => range;
    public WeaponFireMode FireMode => fireMode;
    public int MagazineSize => magazineSize;
    public float ReloadTime => reloadTime;
    public float BowRecoveryDelay => bowRecoveryDelay;
    public float ChargeTime => chargeTime;
    public float MaxChargeDamage => maxChargeDamage;
    public float MaxChargeSize => maxChargeSize;
    public float ChargeTimePerAmmo => chargeTimePerAmmo;
    public float ChargeDamagePerAmmo => chargeDamagePerAmmo;
    public float ChargeSizePerAmmo => chargeSizePerAmmo;
    public float ReloadSpeedPerLevel => reloadSpeedPerLevel;

    private void OnValidate() => ValidateRangedSettings();

    public void ValidateRangedSettings()
    {
        magazineSize = Mathf.Clamp(magazineSize, 1, 999);
        reloadTime = Safe(reloadTime, 0.05f);
        bowRecoveryDelay = Safe(bowRecoveryDelay, 0f);
        chargeTime = Safe(chargeTime, 0.05f);
        maxChargeDamage = Safe(maxChargeDamage, 1f);
        maxChargeSize = Safe(maxChargeSize, 1f);
        chargeTimePerAmmo = Safe(chargeTimePerAmmo, 0f);
        chargeDamagePerAmmo = Safe(chargeDamagePerAmmo, 0f);
        chargeSizePerAmmo = Safe(chargeSizePerAmmo, 0f);
        reloadSpeedPerLevel = Safe(reloadSpeedPerLevel, 0f);
    }

    internal static float Safe(float value, float min) => float.IsNaN(value) || float.IsInfinity(value) ? min : Mathf.Max(min, value);
}
