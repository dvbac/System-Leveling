# Red Arrow implementation
Status: Frozen. Owner: main agent, no delegation. Approval: user 2026-11-05.

1. Runtime (AC-002/003/004/005): WeaponScriptableObject baseBounces default0 + validation/property; shared projectile adds base bounce and protected hit-history query; ranged controller adds default-zero single-shot angle hook. New RedArrowController/RedArrowProjectile implement random launch spread and homing only. Old InitializeShot/hit/upgrade signatures preserved.
2. Assets/catalog (AC-001/006): new RedArrowWeaponBuilder; Generated/RedArrow sprite copy, RedArrow projectile/weapon prefabs and Weapon/Red Arrow.asset. Append to LoadoutSelection scene after backup/dirty check; update only special weapon description in LoadoutSelection.cs. Do not modify gameplay scenes or default weapons.
3. Verification/docs: new RedArrowWeaponRegression; Captures/RedArrowWeapon evidence; docs/red-arrow-weapon.md. Run existing WeaponAmmoRegression and UpgradeTreeRegression/RunCombatUpgradeRegression as feasible; preserve all original tests. All regression reward activity uses SoulTestIsolation.

Forbidden: original source PNG, weapon stats/prefabs of old weapons, existing user work, summon trees, Vampire authoring, boss/HUD redesign, save formats, package changes or commits.
Stop/report: changed approved behavior, dirty scene before setup, missing source/calibration assets, shared compatibility failures. Integration order runtime -> compile -> setup -> own checks -> shared regressions -> judge. Target acquisition bounded to at most once per0.15s per untargeted projectile, no per-frame global target array.
