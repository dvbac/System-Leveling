using System;
using UnityEngine;

// The production scheduler accepts input/time explicitly, so boundary tests do not simulate a second implementation.
public sealed class RangedWeaponState
{
    private readonly WeaponScriptableObject data;
    private int ammoLevel, reloadLevel;
    private float cooldown, reloadElapsed, idleElapsed, chargeElapsed;
    private bool wasHeld, requireRelease;

    public int Ammo { get; private set; }
    public int Capacity => data.FireMode == WeaponFireMode.Crossbow ? 1 : Mathf.Min(999, data.MagazineSize + ammoLevel);
    public bool IsReloading { get; private set; }
    public bool IsCharging { get; private set; }
    public float ReloadDuration => WeaponScriptableObject.Safe(data.ReloadTime, 0.05f) / ReloadSpeed;
    private float ReloadSpeed => 1f + reloadLevel * data.ReloadSpeedPerLevel;
    public float ChargeDuration => (data.ChargeTime + ammoLevel * data.ChargeTimePerAmmo) / ReloadSpeed;
    public float ChargeFraction => IsCharging ? Mathf.Clamp01(chargeElapsed / Mathf.Max(0.001f, ChargeDuration)) : 0f;
    public float ReloadFraction => IsReloading ? Mathf.Clamp01(reloadElapsed / ReloadDuration) : 0f;
    public float ShotDamageFactor { get; private set; } = 1f;
    public float ShotSizeFactor { get; private set; } = 1f;

    public RangedWeaponState(WeaponScriptableObject data)
    {
        this.data = data != null ? data : throw new ArgumentNullException(nameof(data));
        Ammo = Capacity;
    }

    public void SetUpgrades(int ammo, int reload)
    {
        ammoLevel = Mathf.Clamp(ammo, 0, 999);
        reloadLevel = Mathf.Clamp(reload, 0, 999);
        Ammo = Mathf.Min(Ammo, Capacity); // Increasing capacity never grants free rounds.
    }

    public void Interrupt()
    {
        IsCharging = false;
        chargeElapsed = 0;
        requireRelease = true;
    }

    // At most one shot or recovered bow arrow per Tick, even after a stalled frame.
    public bool Tick(float delta, bool held)
    {
        if (delta < 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return false;
        cooldown = Mathf.Max(0, cooldown - delta);
        if (IsReloading && data.FireMode != WeaponFireMode.Bow)
        {
            reloadElapsed += delta;
            if (reloadElapsed < ReloadDuration) { wasHeld = held; return false; }
            Ammo = Capacity;
            reloadElapsed = 0;
            IsReloading = false;
            wasHeld = held;
            return false;
        }
        bool triggerBlocked = requireRelease;
        if (!held) requireRelease = false;

        ShotDamageFactor = ShotSizeFactor = 1f;
        if (data.FireMode == WeaponFireMode.Crossbow)
        {
            if (triggerBlocked) { wasHeld = held; return false; }
            if (held && !wasHeld && Ammo > 0) { IsCharging = true; chargeElapsed = 0; }
            else if (held && IsCharging) chargeElapsed = Mathf.Min(chargeElapsed + delta, ChargeDuration);
            bool release = !held && wasHeld && IsCharging;
            wasHeld = held;
            if (!release) return false;
            float fraction = ChargeFraction;
            ShotDamageFactor = Mathf.Lerp(1f, data.MaxChargeDamage + ammoLevel * data.ChargeDamagePerAmmo, fraction);
            ShotSizeFactor = Mathf.Lerp(1f, data.MaxChargeSize + ammoLevel * data.ChargeSizePerAmmo, fraction);
            IsCharging = false;
            chargeElapsed = 0;
            return Shoot();
        }

        if (data.FireMode == WeaponFireMode.Bow)
        {
            if (held)
            {
                idleElapsed = reloadElapsed = 0;
                IsReloading = false;
            }
            else if (Ammo < Capacity)
            {
                float before = idleElapsed;
                idleElapsed += delta;
                reloadElapsed += Mathf.Max(0, idleElapsed - data.BowRecoveryDelay) - Mathf.Max(0, before - data.BowRecoveryDelay);
                IsReloading = idleElapsed >= data.BowRecoveryDelay;
                if (reloadElapsed + 0.000001f >= ReloadDuration)
                {
                    Ammo++;
                    reloadElapsed = 0;
                    IsReloading = Ammo < Capacity;
                }
            }
        }
        wasHeld = held;
        return !triggerBlocked && held && Ammo > 0 && cooldown <= 0 && Shoot();
    }

    private bool Shoot()
    {
        if (Ammo <= 0) return false;
        Ammo--;
        cooldown = WeaponScriptableObject.Safe(data.Cooldown, 0.01f);
        if (Ammo == 0 && data.FireMode != WeaponFireMode.Bow)
        {
            IsReloading = true;
            reloadElapsed = 0;
        }
        return true;
    }
}
