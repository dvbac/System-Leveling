# Implementation plan
Approval: "duyệt", 2026-10-29. Main agent owns all work, sequentially.

- TASK001 (FR001–005 / AC001–004): Assets/Scripts/Weapon/Base/{WeaponHolder,WeaponGrip}.cs, Assets/Scripts/UI/WeaponHud.cs, Assets/Scripts/Player/PlayerStats.cs — validate-and-replace Equip, CurrentWeapon, live HUD/type binding; preserve aim/shot contracts.
- TASK002 (FR002/006 / AC001/005): Assets/Scripts/Editor/{WeaponHolderSetup,WeaponAmmoSetup}.cs, Assets/Prefab/{Characters/,Weapons/,WeaponHolder.prefab}, Assets/Scenes/SampleScene.unity — backup, calibrate poses, remove only weapon children, retain actor roots and gameplay defaults. No texture import/balance edits.
- TASK003 (all AC): Assets/Tests/{WeaponEquippingRegression,WeaponAmmoRegression}.cs, Captures/WeaponEquipping/, docs/spec-driven/weapon-equipping/, docs/playable-characters.md, docs/weapon-holder-fix.md — real 16-combination tests and old regressions, evidence, usage for future UI.

No new character-select UI, inventory, save system, actor removal, weapon balancing or enemy/spawn production edits. Shared contract frozen: bool Equip(GameObject), read-only WeaponController CurrentWeapon; existing GetDirection/PointerPosition remain. Invalid selection returns false and preserves current, repeated same selection does not reset ammo.
Stop on contract/scope changes or a data-loss risk. Keep failures; after three failures of the same AC stop for a decision.
