# Level progression and enemy EXP

The next level costs `45 + 15 × (current level − 1)` EXP: 45, 60, 75, 90, 105, …, 180 at level 10. `PlayerStats.ExperienceForLevel` is the shared source of truth. The old `levelRanges` configuration is removed from scenes/prefabs and builders; it no longer creates a plateau after level 5.

- A run starts at level 1 with 0 EXP. EXP overflow carries into the next level, and large pickups can queue multiple level-up rewards.
- Each level grants one choice from three distinct compatible upgrades. A level does not automatically heal the player.
- EXP additions and threshold calculations saturate at `int.MaxValue` instead of overflowing.

## Enemy rewards

| Enemy | EXP per dropped orb |
| --- | ---: |
| Slime / Blue Slime / IceSlime | 5 |
| Spider | 8 |
| Wolf / SnowWolf | 10 |
| Skeleton | 12 |
| Snowman | 15 |
| Rotwood | 150 |
| Elite | 3 × the original enemy reward |

Rewards live on `EnemyScriptableObject`, beside enemy combat stats. `DropRateManager` sets the value on the **spawned orb**, never on the shared EXP prefab. Current enemy drop tables contain one EXP entry at 100%, so each kill drops one orb. Non-enemy drops keep their configured prefab reward (currently 15). Soul, drop probability, potions and boss victory behavior are unchanged.

With guaranteed collection, level 2 requires 9 Slimes or 3 Snowmen; a 150-EXP boss orb from a fresh level-1 player reaches level 3 with 45/75 EXP remaining. This is an initial balance preset, not a claim about minutes per level: spawn density and actual collection still matter.

## Per-run upgrade caps

Double Shot: 2; Capacity: 4; Recharge: 5; Quick Hands: 5; Critical: 5 (+50% chance). Capped upgrades disappear from the card pool and direct requests reject them without consuming a reward. Other upgrades remain available, so there are always at least three valid choices. Weapon swaps do not reset counters; restarting does.

`Tools > System Leveling > Run Level Progression Regression (Play Mode)` verifies the curve, overflow/remainders, real kill/drop/collection, normal and elite rewards, source-asset preservation, caps/card filtering, weapon swaps, both maps and actual Restart. It uses isolated Soul test storage. Output: `Captures/LevelProgression/regression.json`.

Verified preset: progression **98 PASS** (including IceSlime/SnowWolf), combat upgrades **219 PASS**, ammo/reload **119 PASS**, HUD/level-up **43 PASS**. This verifies implementation, not long-session pacing or standalone builds.
