# Elite enemies

- One elite is scheduled at **05:00, 10:00, 15:00…** on both gameplay maps, using the existing survival clock. Pause/level-up stops the clock; victory/defeat stops spawning. A fresh scene/Quick Restart starts the schedule again at 05:00. Temporarily disabling the spawner does not reset it.
- The prefab is selected from the **current map's current normal spawn phase**, with its existing weights. Grass uses grass enemies; Snow uses its snow variants/Snowman. Elites do not replace normal enemies, Horde members or Rotwood, and do not consume normal/Horde population slots. A surviving elite does not prevent the next five-minute elite.
- In **EnemySpawner → Elite**, defaults are **size ×1.6, HP ×5, movement speed ×1.25, contact damage ×2**. These modify only the spawned instance. Prefabs, art, animation clips and shared combat data are unchanged; existing AI, hit/death animation, drops and Soul rewards are reused.
- Elite EXP is **3× the enemy data's base reward**, applied to its dropped orb. The shared EXP prefab and Soul reward are not multiplied. See `docs/level-progression.md`.
- In addition to that existing loot, a defeated elite drops **exactly one treasure chest**. Duplicate death callbacks cannot duplicate the chest; despawning/destroying an elite without defeating it does not drop one.
- Eight one-pixel-offset red silhouettes behind the original sprite form the red outline. They follow the original sprite's animation, flipping, visibility and sorting layer. A SortingGroup keeps the outline behind the body but above terrain. `Assets/Resources/EliteSilhouette.shader` draws solid red using the source alpha without tinting the original sprite, and is included through Resources. The per-elite material is released on destruction.
- Placement reuses normal off-camera sampling but expands footprint/camera clearance and enemy separation for the elite's size. If no safe location/prefab is available, one pending elite retries once per gameplay second with bounded placement attempts. It never forces a spawn into obstacles or inside the camera.
- Like normal spawning, a single long frame crossing multiple milestones produces at most **one** pending elite, not a catch-up burst. There is no artificial elite lifetime or live-elite cap: an undefeated elite remains alive alongside later elites.

## Treasure chest

- `Assets/Resources/EliteTreasureChest.prefab` uses the supplied `Assets/MyAssets/Other/Drop/treasure-chest.png`. Source pixels stay unchanged; import uses Point filtering, no mipmaps and uncompressed pixels for crisp enlarged pixel art. Scenes are untouched. Recreate it with **Tools > System Leveling > Build Elite Treasure Chest**.
- At drop time, roll **2 or 3 threshold costs** with equal probability. At **pickup time**, sum those costs on `PlayerStats.ExperienceForLevel`, retaining fractional EXP. On the current curve, level1 grants20 or45 XP; level10 grants200 or315 XP. Because level20 costs312 but level21 costs267, large carried XP can produce an extra level rather than being discarded (level20/300 XP +579 XP reaches level23/20 XP).
- Pickup also grants **50 bonus Soul** (initial tunable value on the chest prefab), through the existing persistent `SoulWallet` and run-earned summary. Base kill Soul and the elite's original EXP orb remain unchanged.
- Must touch the chest with the character, not just its EXP magnet. Pause, dead player and victory/defeat prevent collection. Mark the chest collected before notifying EXP/UI, so multiple colliders, repeat pickup calls or re-entrant UI callbacks cannot grant twice.
- Each earned level queues its normal upgrade choice. If Soul saving fails, the wallet retains pending Soul and EXP still works. EXP totals preserve fractions up to the existing integer limit; exact2–3-level advancement assumes carried XP fits the destination threshold and the reward has not saturated. Normal elite orbs now grant4.5 XP (1.5 ×3).

## Validation

`Tools/System Leveling/Run Elite Enemy Regression (Play Mode)` tests both actual maps at accelerated clock boundaries, biome selection, independent normal cap, multipliers, duplicate configuration, animation/outline synchronization, movement, damage/death and one-time Soul payment, pause/victory/defeat, safe-placement retries, expanded collision clearance, restart and prefab/data preservation. Evidence is written to `Captures/EliteEnemies/` using an isolated Soul save.

Editor integration: **58 elite checks PASS**, including screenshot-pixel checks that the red outline is actually visible above terrain. Visual evidence: `grass-elite.png`, `snow-elite.png`. Existing survival spawning/Horde regression: **140 PASS**; Rotwood boss: **49 PASS**; Soul/run summaries: **264 PASS**. Initial validation exposed a terrain-sorting issue and a strict floating-point test comparison; both were corrected before these final results. Prefab and combat-data content remained unchanged.

Standalone shader inclusion and long-session/target-device rendering performance still require a build test. Each elite adds eight silhouette renderers; this is intended for sparse five-minute spawns, not mass elite hordes.

Chest and red-card palette regression: **Tools > System Leveling > Run Elite Treasure Regression (Play Mode)**. Evidence: `Captures/EliteTreasure/regression.json` and chest/red-theme/normal-theme screenshots from both maps. Uses isolated Soul storage.

Latest Play Mode validation: **55 treasure/theme checks PASS**, including actual Physics2D pickup, duplicate/re-entrant payout protection, 2/3-level rewards at levels 1/7/25, saved Soul and full red-to-blue palette restoration. Existing **98 progression checks** and **58 elite checks** also pass with the extra chest enabled. Standalone build and long-run reward pacing have not been tested.
