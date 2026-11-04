# Level progression and enemy EXP

`PlayerStats.ExperienceForLevel` implements the supplied table exactly. `level` is the current level; the result is the XP needed for the next level:

| Current level | Required XP |
| --- | --- |
| 1–19 | `10 * level - 5` |
| 20 only | `16 * level - 8` |
| 21–39 | `13 * level - 6` |
| 40–59 | `16 * level - 8` |
| 60+ | `level * level` |

Examples: level1=5, 19=185, 20=312, 21=267, 39=501, 40=632, 59=936, 60=3600. The decrease after level20 and the large jump at level60 are intentional fidelity to the user's table, not a smoothed curve. Invalid levels use level1; arithmetic uses `long` and clamps the threshold to `int.MaxValue`.

- A run starts at level 1 with 0 EXP. EXP overflow carries into the next level, and large pickups can queue multiple level-up rewards.
- Each level grants one choice from three distinct compatible upgrades. A level does not automatically heal the player.
- EXP totals use `double` to preserve fractional XP and still saturate at `int.MaxValue`. Enemy rewards/orbs use `float`; 1.5 and 4.5 are preserved exactly through drop, collection, rollover and HUD. NaN/infinite/nonpositive XP gains are ignored; integer-max levels cannot wrap negative. Thresholds, level and upgrade-choice counts stay integer.

## Enemy rewards

| Enemy | EXP per dropped orb |
| --- | ---: |
| Slime / Blue Slime / IceSlime | 1.5 |
| Spider | 1.5 |
| Wolf / SnowWolf | 1.5 |
| Skeleton | 1.5 |
| Snowman | 1.5 |
| Rotwood / Frostfang | 150 (unchanged) |
| Rainbow Slime | 0; Tome reward unchanged |
| Elite | 4.5 (base ×3, unchanged multiplier) |

Rewards live on `EnemyScriptableObject`, beside enemy combat stats. `DropRateManager` sets the value on the **spawned orb**, never on the shared EXP prefab. Current enemy drop tables contain one EXP entry at 100%, so each kill drops one orb. Non-enemy drops keep their configured prefab reward (currently 15). Soul, drop probability, potions and boss victory behavior are unchanged.

Elites additionally drop a treasure chest: pickup grants EXP equivalent to **2–3 threshold costs at the player's pickup-time level**, plus **50 bonus Soul**. This does not replace the base orb/kill Soul. Carried XP is never discarded: because level20's threshold exceeds levels21/22, a chest can occasionally grant an extra level when crossing that band with substantial existing XP. Example: level20 with300 XP + a two-threshold chest (579 XP) reaches level23 with20 XP. Other bands preserve valid fractional remainders normally. See `docs/elite-enemies.md`.

With guaranteed collection, level2 requires **four normal enemies** (6 XP, carrying1 into level2). A150-XP boss orb from fresh level1 reaches level6 with25/55 XP. Reaching level10 costs405 total XP. This is an initial preset, not a claim about minutes per level: spawn density/collection matter, and level60 requires3600 XP for one level. Upgrade caps, Soul, drop probabilities and combat stats are unchanged.

## Per-run upgrade caps

Double Shot: 2; Capacity: 4; Recharge: 5; Quick Hands: 5; Critical: 5 (+50% chance). Capped upgrades disappear from the card pool and direct requests reject them without consuming a reward. Other upgrades remain available, so there are always at least three valid choices. Weapon swaps do not reset counters; restarting does.

`Tools > System Leveling > Run Level Progression Regression (Play Mode)` verifies the curve, overflow/remainders, real kill/drop/collection, normal and elite rewards, source-asset preservation, caps/card filtering, weapon swaps, both maps and actual Restart. It uses isolated Soul test storage. Output: `Captures/LevelProgression/regression.json`.

Earlier verification counts for the old20/+10 preset are historical, not evidence for this change. The updated progression suite verifies every band edge, invalid/extreme levels, fractional kill/pickup rewards, elite scaling, boss/fallback preservation, caps and Restart. HUD and treasure suites verify fractional display and the chest band-boundary behavior. Reports under `Captures/LevelProgression/`, `Captures/HudLevelUp/`, and `Captures/EliteTreasure/` identify current results. Full-run balancing/standalone builds remain separate checks.

Current verification: **905 checks PASS**: progression120, HUD43, elite treasure86 (all band edges/fractional remainders and level20 extra rollover), upgrade trees284, combat upgrades222, weapon ammo150. A clean Play Mode entry followed by the treasure/HUD suites has no Console errors. No Soul save migration or combat/weapon-stat changes were made for this XP update.
