# Knight, Archer, Hunter

## Assets

- Prefabs: `Assets/Prefab/Characters/{Knight,Archer,Hunter}.prefab`.
- Character data: `Assets/Scriptable Object/Character/{Knight,Archer,Hunter}.asset`.
- Copied source sheets, clips, controllers: `Assets/MyAssets/Character/Playable/<name>/`.
- Original files under `HE194657/Character` are untouched; copied sheets are byte-identical.

| Character | HP | Recovery (HP/s) | Move speed | Magnet radius | Role |
| --- | ---: | ---: | ---: | ---: | --- |
| Knight | 120 | 0.40 | 2.8 | 1.4 | More durable, slower, smaller pickup radius |
| Archer | 80 | 0.15 | 3.6 | 1.8 | Fragile, fastest movement |
| Hunter | 100 | 0.25 | 3.2 | 2.0 | Balanced survival, best pickup radius |

These are starting balance values, not a completed long-run balance pass. Might=4 and ProjectileSpeed=2 retain the existing baseline: current combat reads weapon data and DamageMultiplier, not these legacy character fields. No misleading class-specific damage/projectile bonus is introduced.

## Animation and integration

Each character has eight Idle frames at 8 fps and eight Walk frames at 10 fps. Both loop. `isMoving` switches Idle ↔ Walk without exit-time delay, matching `PlayerController`. Sprite sheets are sliced as eight square cells, Point filtered, uncompressed, no mipmaps. Pixels-per-unit equals each sheet's cell height, keeping a one-world-unit canvas despite Hunter's larger source images. Source pixels are not resampled.

Original build (2026-10-26): prefabs preserved the scene Player's controller/input references, Rigidbody2D, collector, weapon hierarchy, I-frames and level progression. Capsule size was 0.36 × 0.70, offset (0, -0.04). The later weapon migration below preserves subsequent user character edits.

Use one character at a time in place of the current Player, not alongside it: `PlayerController.Instance` is a singleton. Keep the existing Manager/PlayerInput, which enables the shared input map. When replacing the scene Player, reassign the spawner, HUD and camera references to the new instance. The supplied character art contains only Idle/Walk; no Attack/Die clips were invented.

## Current equipment (2026-10-29)

Defaults: Knight → Sword; Archer → Bow (7 arrows); Hunter → Crossbow (1 charged bolt); scene Mage → Magic Book (8 rounds). Weapon/projectile prefabs live in `Assets/Prefab/Weapons/`. Character prefabs now have neutral, empty holders: `StartingWeapon` is a fallback, not a class restriction. Future selection calls `holder.Equip(chosenPrefab)`; the same weapon works on every calibrated character without per-pair offsets. See `docs/spec-driven/weapon-equipping/USAGE.md` for the current contract and `docs/spec-driven/weapon-ammo-charge/` for combat behavior. The character-creation checks below are historical.

### Selection-to-gameplay connection (2026-10-29)

Start from `Assets/Scenes/LoadoutSelection.unity` (also build index 0). Clicking a character and then a weapon launches gameplay. `GameplayLoadout` applies the selected data, appearance, collider and hand calibration to the existing scene Player before its stats initialize, then calls `WeaponHolder.Equip`. This preserves the authored camera/HUD/spawner references. Restart retains the chosen loadout. See `docs/loadout-selection.md`; the 16-pairing runtime regression is recorded in `Captures/LoadoutGameplay/regression.json`.

## Verification (2026-10-26)

- Compile job `9c63bf12`: success, 0 errors.
- `Tools/System Leveling/Validate Playable Characters`: PASS, 39 checks. Includes all asset links, stats, input references, sprite sizes and live Animator evaluation: Idle advances → Walk advances → Idle.
- All three actual prefab instances were created in Play Mode; PlayerStats Awake/Start loaded the assigned HP, speed, recovery, magnet and EXP cap. Read-back: `Captures/PlayableCharacters/runtime-readback.json`.
- `SampleScene.unity` and `Mage.asset` hashes match their pre-build values; scene remains clean in Edit Mode. Source hashes are recorded in `Captures/PlayableCharacters/source-manifest.json`.
- Console contained one editor camera/frustum error at 08:19:33 UTC while authoring; it was not a C# compile error. No newer managed errors were returned after the runtime check. Existing historical regression failures were not cleared.
- Full combat, input movement and long-session balance were not re-tested for these characters. The runtime smoke check intentionally verifies prefab initialization, not a complete character-replacement game session.

The build menu refuses to overwrite any existing generated character assets. Use the validation menu to recheck assets; tune stats directly on their CharacterScriptableObject assets.
