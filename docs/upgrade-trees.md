# In-run upgrade trees (no summons)

Eight four-card trees inspired by 20 Minutes Till Dawn. The existing level-up modal still offers three distinct, weapon-compatible choices. The root unlocks both middle cards; **either** middle card unlocks the final. New cards can be taken once per run. Existing repeatable roots and their caps/effects are preserved for compatibility. Existing POWER, ATTRACTION, FOCUS, CAPACITY, RECHARGE and CRITICAL remain in the pool.

| Tree | Root | Middle choices | Final |
| --- | --- | --- | --- |
| Power | Power Bullet (existing) | Big Shot / Splinter | Reaper Rounds |
| Rapid fire | Rapid Fire | Light Bullets / Rubber Bullets | Siege |
| Multishot | Double Shot (existing) | Fan Fire / Split Fire | Fusillade |
| Reload | Quick Hands (existing) | Armed Ready / Fresh Clip | Kill Clip |
| Frost | Frost Mage | Frostbite / Ice Shard | Shatter |
| Flame | Pyro Mage | Fire Starter / Intense Burn | Soothing Warmth |
| Vitality | Vitality (existing) | Anger Point / Giant | Regeneration |
| Haste | Swiftness (existing) | Blazing Speed / Run and Gun | In the Wind |

Exact tuned values live in `UpgradeCatalog.Description`, next to each tree's name, symbol and color. These are adaptations, not a promise of 20MTD's original numbers. There are **27 new cards**, 32 tree nodes in total.

## Combat integration

- Sword supports attack rate, direct damage, flame/frost, health and movement. Ranged-only ammo, multishot, Splinter, Reaper, ricochet, Siege and last-round ice cards are filtered. Changing weapons never clears ownership or grants a second copy.
- Fire Starter works with sword too: every fifth attack emits a dedicated fireball, moving at 7 units/s, dealing 20 area damage (radius 1.8) and applying burn. It never instantiates a sword slash as a projectile.
- Projectiles/sword swings snapshot damage and elemental chances at creation. Existing projectiles keep their damage, size, speed, pierce and elemental profile after another level-up.
- Burn deals 3 DPS for 3s, in half-second ticks. Reapplication refreshes duration and retains the strongest active DPS, **not unlimited stacking**. Intense Burn and Elements Tome multiply burn DPS. Freeze uses Elements' duration multiplier; Elements can now appear with Pyro Mage or Frost Mage, not only Electro Tome.
- Freeze stops ordinary chase and knockback for 1.5s. Bosses receive only a 30% **chase** slow for 0.3s; charge/pounce/breath/other telegraphs and phases are not interrupted. Frostbite triggers once when a new freeze starts, removing 15% runtime max HP (1% boss). Elite max HP is tracked after its health multiplier, without editing shared source assets.
- Frozen deaths can Shatter for 7% runtime max HP in radius 2. Shatter damage does not apply more statuses. Splinter emits three 10%-damage fragments. Secondary attacks/burn kills can grant Kill Clip but cannot recursively split/shatter. This bounds chain explosions.
- Reaper preserves pierce on killed enemies; living enemies still consume a hit. Existing same-target collision deduplication and charge/Tome pierce are preserved.
- Fan Fire adds ten 15%-damage secondary bullets on the last round; Ice Shard adds three guaranteed-freeze shards; Split Fire adds a rear projectile. These do not consume extra ammo or count as extra attacks for Fire Starter.
- Tree ammo bonuses are separate from legacy CAPACITY levels/caps. Crossbow still has its existing base ammo rules: extra ammo levels improve charge rather than silently adding rounds. Fresh Clip triggers on actual ammo recovery (bow recovery included), not capacity increases or Siege refunds. Kill Clip caps at +100% reload rate and resets on recovery. Siege checks stationary movement input and refunds at most the round just fired.
- Run and Gun raises attack movement from 45% to 90%; roots and Stillness restrictions remain. In the Wind adds up to +40% damage/movement after 40 unhurt seconds, resetting only on actual damage, not dodge/invincibility.

## Effects

Runtime pixel sparks need no new package, scene setup, generated images or shared material edits:

- Tree-colored acquisition bursts and color-coded tier labels on cards.
- Muzzle/impact sparks; larger Big Shot projectiles; flame/frost/reaper projectile trails.
- Burning embers, icy motes, fireball impact bursts and shatter fragments.
- Blazing movement sparks, green healing sparks, red Anger Point bursts, blue Fresh Clip bursts and teal wind-stack pulses.
- Physical fan/rear/splinter/ice projectiles visibly express their upgrade, rather than only changing a stat.

At most 256 spark objects are active globally. All effect lifetimes and status timers respect pause; gameplay stop removes sparks. Character/enemy animation colors and materials are not overwritten. This is a lightweight first visual pass, not hand-drawn sprite-sheet animation or an audio redesign.

## Verification

In Editor Play Mode: `Tools/System Leveling/Run Upgrade Tree Regression (Play Mode)`.

The regression uses real gameplay scenes, player components, weapon prefabs, collisions, level-up cards and boss data. It checks both prerequisite paths for every tree, once-per-run ownership, sword filtering/fireballs, frost/burn/pause, elite/boss HP rules, secondary-chain limits, ammo callbacks, temporary buffs, card clicks, rendered FX and run reset. It isolates Soul rewards using `SoulTestIsolation` and verifies source stat assets are unchanged. Report and screenshots: `Captures/UpgradeTrees/`.

Also rerun combat-upgrade, weapon-ammo, level-progression and boss regressions after changing these shared paths. Standalone builds, prolonged balancing and worst-case horde profiling need separate testing.

Latest Editor verification: **946 checks PASS** across upgrade trees (284), legacy combat upgrades (222), weapon ammo (139), level progression (98), Rotwood (50), Frostfang (63), and Rainbow Slime/Tomes (90). The tree suite additionally checks the full six-tick burn lifetime, tree-modified scheduling for book/bow/crossbow, and Tome lightning kill attribution. Unity console: no errors. The card and frost/flame screenshots were visually reviewed.
