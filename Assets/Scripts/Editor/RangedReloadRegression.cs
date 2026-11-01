using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class RangedReloadRegression
{
    [MenuItem("Tools/System Leveling/Test Hold Fire and Partial Reload")]
    public static void Run()
    {
        var checks = new List<string>(); var failures = new List<string>();
        Action<bool, string> check = (pass, message) => { if (pass) checks.Add(message); else failures.Add(message); };
        WeaponScriptableObject book = Data("Magic Book"), bow = Data("Bow"), cross = Data("Crossbow");
        RangedWeaponState state = Empty(book);
        check(!state.Tick(state.ReloadDuration - 0.01f, true) && state.Ammo == 0, "Empty magazine cannot fire before reload completes");
        check(state.Tick(0.011f, true) && state.Ammo == state.Capacity - 1, "Held magazine fires immediately when reload completes without releasing the trigger");
        check(!state.Tick(0, true), "Reload completion never bypasses shot cooldown");
        check(state.Tick(book.Cooldown, true), "Held fire continues after the first post-reload shot");

        state = new RangedWeaponState(book); state.Tick(0, true); int ammo = state.Ammo;
        check(!state.Tick(0.2f, false) && state.IsReloading && state.Ammo == ammo && state.ReloadFraction > 0, "Releasing a partly used magazine starts recovery before ammo reaches zero");
        check(!state.Tick(0.1f, true) && state.Ammo == ammo, "Holding again waits for the partial reload to finish");
        check(state.Tick(state.ReloadDuration, true) && state.Ammo == state.Capacity - 1, "Holding again resumes fire after a partial reload");
        state = new RangedWeaponState(book); state.Tick(0, true); state.Tick(0, false);
        check(!state.Tick(state.ReloadDuration, false) && state.Ammo == state.Capacity && !state.IsReloading, "Partial reload refills the magazine but never fires with the trigger released");

        state = Empty(book); state.Interrupt();
        check(!state.Tick(state.ReloadDuration, true) && state.Ammo == state.Capacity, "Reload completion does not defeat a pause/UI input fence");
        check(!state.Tick(1, true), "An interrupted held trigger still requires a real release");
        state.Tick(0, false); check(state.Tick(0, true), "Input resumes after the actual release");
        state = Empty(book); state.Interrupt(); state.Tick(0.1f, false);
        check(state.Tick(state.ReloadDuration, true), "A release observed during reload clears the UI fence, so a later held trigger can resume");

        state = Empty(bow);
        check(state.Tick(bow.BowRecoveryDelay + state.ReloadDuration, true) && state.Ammo == 0, "An empty bow recovers and fires while the trigger remains held");
        check(!state.Tick(0, true), "Empty bow does not fire twice without recovery time");
        check(state.Tick(state.ReloadDuration, true) && state.Ammo == 0, "Held empty bow keeps recovering one arrow and firing again");
        state = new RangedWeaponState(bow); state.Tick(0, true);
        check(!state.Tick(bow.BowRecoveryDelay, false) && state.Ammo == state.Capacity - 1, "A partly used bow starts its existing recovery delay without needing to empty");
        check(!state.Tick(state.ReloadDuration, false) && state.Ammo == state.Capacity, "A partly used bow recovers the missing arrow without firing");
        state = Empty(bow); state.Interrupt();
        check(!state.Tick(bow.BowRecoveryDelay + state.ReloadDuration, true) && state.Ammo == 1, "Bow recovery cannot emit a shot through a UI fence");
        check(!state.Tick(float.NaN, true) && !state.Tick(-1, true) && state.Ammo == 1, "Invalid time cannot consume recovered ammo");

        state = new RangedWeaponState(cross); state.Tick(0, true); state.Tick(state.ChargeDuration, true);
        check(state.Tick(0, false) && state.Ammo == 0, "Crossbow still fires only when a charge is released");
        state.Tick(state.ReloadDuration, true); state.Tick(1, true);
        check(!state.IsCharging && state.Ammo == 1, "Crossbow hold/release mechanics remain unchanged by the normal weapon fix");
        state.Tick(0, false); state.Tick(0, true); state.Tick(state.ChargeDuration, true);
        check(state.Tick(0, false) && state.ShotDamageFactor == cross.MaxChargeDamage, "Crossbow can still charge a fresh full-power shot");
        Directory.CreateDirectory("Captures/WeaponHoldReload");
        File.WriteAllText("Captures/WeaponHoldReload/state.json", JsonUtility.ToJson(new Report { result = failures.Count == 0 ? "PASS" : "FAIL", utc = DateTime.UtcNow.ToString("o"), checks = checks.ToArray(), failures = failures.ToArray() }, true));
        if (failures.Count > 0) Debug.LogError("HOLD FIRE / PARTIAL RELOAD FAIL: " + string.Join("; ", failures));
        else Debug.Log("HOLD FIRE / PARTIAL RELOAD PASS: " + checks.Count);
    }

    private static RangedWeaponState Empty(WeaponScriptableObject data)
    {
        var state = new RangedWeaponState(data);
        for (int i = 0; i < state.Capacity; i++) state.Tick(i == 0 ? 0 : data.Cooldown, true);
        return state;
    }
    private static WeaponScriptableObject Data(string name) => AssetDatabase.LoadAssetAtPath<WeaponScriptableObject>("Assets/Scriptable Object/Weapon/" + name + ".asset") ?? throw new InvalidOperationException(name);
    [Serializable] private class Report { public string result, utc; public string[] checks, failures; }
}
