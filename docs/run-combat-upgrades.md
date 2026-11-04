# In-run combat upgrades

See [upgrade trees](upgrade-trees.md) for the eight non-summon trees, elemental combat and pixel effects added on top of these existing upgrades. Legacy values/caps below remain in place; new branch ownership and temporary buffs are additional runtime state.

These choices join the existing random three-card level-up pool. They consume one earned level-up choice, **not Soul**, and reset on Quick Restart/new run. Existing upgrades remain available. Cards are distinct, and incompatible/capped upgrades are excluded from the pool and rejected by the apply API without consuming a pending choice.

| Upgrade | Effect per choice | Availability |
| --- | --- | --- |
| Double Shot | +1 projectile path, +10° total fan spread, ×0.9 projectile damage | Ranged |
| Focus | +20% base projectile speed, −15° spread | Ranged |
| Power Bullet | ×1.15 damage, +20% base knockback | All weapons |
| Quick Hands | +5% base reload speed, +5% base fire rate | Ranged |
| Critical | +10 percentage points critical chance; critical hits deal ×2 damage | All weapons |

- Double Shot and Power Bullet damage factors multiply per choice. Speed/reload/fire-rate/knockback bonuses and spread adjustments add per choice. POWER adds +1 base weapon damage per choice before percentage/charge/critical multipliers; SWIFTNESS adds +0.2 base movement speed before movement penalties. Both reset with the run and never modify source assets.
- A ranged attack distributes its paths symmetrically across the total spread; two paths with 10° spread aim at −5°/+5°. Spread is clamped to 0–180° when firing. Focus retains its signed accuracy modifier, including when the starting spread was zero. Paths can converge at zero spread, but remain separate projectiles.
- Each attack consumes **one round**, not one round per path; `ShotsFired` counts attacks. Double Shot is limited to **2 choices per run** (3 paths with the default weapon); the firing implementation retains a hard safety bound of 32 paths.
- Every path inherits crossbow charge damage/size/pierce and snapshots current damage, speed, knockback and crit chance. Already-fired projectiles do not change when a later upgrade is acquired. Critical rolls occur once per unique target hit, and repeated contacts cannot reroll or damage that target twice with the same path/swing. Critical is limited to **5 choices** (+50 percentage points per run); total chance including weapon base chance is clamped to 100%.
- Reload speed is a rate: one Quick Hands makes reload time `base / 1.05`, not `base × 0.95`. Fire interval becomes `base / 1.05`. The existing RECHARGE bonus adds to Quick Hands' reload-rate bonus (+10% per level for Crossbow, +20% for Bow/Book). Crossbow reload-speed upgrades also shorten charge time; it still requires a fresh charge/release and does not auto-fire after an empty reload. Its unupgraded full-charge damage is ×2.5 (15 damage), with 1.5s charge and 1s reload; Capacity adds 0.25s/0.25×/1⁄12 size per level.
- Capacity is limited to **4 choices**; Recharge and Quick Hands to **5 each**. Rejected capped choices do not spend an earned upgrade. Limits follow the player across weapon swaps and reset on a new run. Other choices remain uncapped.

## Added stats

`WeaponScriptableObject → Run combat upgrades`: **Projectiles** (default 1), **Spread Angle** (0°), **Knockback** (2 world units/sec initial velocity), **Critical Chance** (0–1, default 0). Existing Cooldown/ReloadTime remain the base fire/reload rates. Runtime modifiers are owned by `PlayerStats`, so they follow weapon swaps without editing shared data assets. Existing character projectileSpeed values are not reinterpreted or used to silently rescale calibrated weapon speeds.

Knockback briefly interrupts a normal/elite enemy's chase and uses its existing Rigidbody2D so physical collisions still apply. Velocity decays to zero over **0.15 seconds**. Killing an enemy does not push its corpse. Rotwood has a separate boss controller and is immune to knockback, preserving its charge/root telegraphs. Enemy contact damage, Soul rewards and elite multipliers are unchanged.

## Movement while attacking

Weapon use now leaves **45% normal movement speed** (55% reduction), increased from the previous 20% preset. This applies while holding active book/bow fire (including intervals between available shots), charging the crossbow, firing its release shot, or swinging the sword. Merely reloading, recovering arrows, hovering UI, having an empty blocked trigger, or carrying a weapon does not count. Release/disable/weapon swap removes this penalty.

The strongest active movement slow wins rather than multiplying slows together. For example, a Rotwood root at 60% speed and an attack at 45% result in 45%; stopping the attack restores the still-active 60% root slow. No base movement stat is overwritten. Pause/death/menu input gates remain in effect.

## Projectile speed and charged piercing

- Ranged base projectile speeds increase **15%**: Book **5.4 → 6.21**, Bow **9 → 10.35**, Crossbow **10.8 → 12.42** world units/sec. Sword swing speed and walking speed are unchanged. Focus multiplies these new base projectile speeds as before.
- Crossbow gains `floor(charge fraction × Max Charge Pierce)` extra target hits when released. `Max Charge Pierce` defaults to **2** and is editable on weapon data: below 50% charge +0, at 50–99% +1, full charge +2. With the current two-target base bolt, this means **2 / 3 / 4 distinct targets**. Bow also starts with two-target piercing. Each Double Shot path receives its own snapshot; charge does not consume extra ammo.
- Each enemy/prop consumes one hit at most per projectile. Once the budget is exhausted, further same-frame collisions are ignored even though Unity defers object destruction. Canceling a charge grants no bonus and cannot produce a stored shot.

Current tuning evidence: ammo/charged pierce **136 PASS**, compact HUD/charge meter **275 PASS**, combat upgrades/45% physical movement **222 PASS**, boss **49 PASS** (**682 total**). Reports and grass/snow charge screenshots are in `Captures/CrossbowChargeTuning/`. The existing direct 16×16 cursor and PlayerSettings were preserved byte-for-byte; an outdated test assumption that the source had to be 32×32 was corrected without changing cursor settings. Standalone and long-session balancing were not tested.

## Validation

`Tools/System Leveling/Run Combat Upgrade Regression (Play Mode)` exercises real level-up cards, book/bow/crossbow multishot, spread/charge/ammo, upgrade snapshots and caps, crit probability and real critical hits, Rigidbody2D knockback, 45% movement, sword compatibility, stacked slows, UI fences and actual Quick Restart. Reports/screenshots are under `Captures/RunCombatUpgrades/`; test Soul uses the existing isolated save helper. Standalone builds and prolonged balance/performance playtesting are not covered by the Editor regression.

Earlier integration evidence (before the level/cap rebalance): **321 combat-upgrade checks PASS**; ammo/reload **120**, HUD/level-up **42**, reference enemies **51**, elite enemies **58**, Rotwood boss **49** also passed. The smaller per-run caps remove the old 32-path/100%-crit stacking loops. Current reports are `Captures/RunCombatUpgrades/runtime.json`, `Captures/WeaponAmmo/regression.json` and `Captures/LevelProgression/regression.json`. Crossbow release still respects fire cooldown when custom reload settings are faster than the cooldown.
