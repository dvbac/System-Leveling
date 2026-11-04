# Hold fire and partial reload

Current behavior, including interruptible partial recovery:

- **Magic Book / Red Arrow / normal magazine weapons:** release the trigger with ammo missing to start a full-magazine reload. Press again with any ammo remaining to cancel recovery and fire at the normal cooldown. Cancellation never refills ammo, carries reload progress forward, or triggers ammo-recovery upgrades. At zero ammo, reload must finish; held fire resumes on completion without another click.
- **Bow:** with arrows remaining, normal held firing still does not regenerate arrows. Releasing starts recovery even if the bow is not empty, using the existing idle delay/per-arrow time. At zero arrows, recovery can proceed while the trigger is held; the recovered arrow fires immediately, then later arrows recover/fire one at a time. A stalled frame never produces a burst.
- **Crossbow:** hold to charge, release to fire; holding across an empty reload does not arm a new charge. With extra rounds from Tome of Rage, idle time also reloads a partial magazine; holding again with rounds remaining cancels recovery and starts a fresh charge. The charge-release frame itself never starts a premature partial reload.
- Pause, level-up, Game Over and UI input fences remain in force. Reload itself never demands a fresh click. For normal magazines, a real release during reload now clears the existing UI fence instead of being swallowed by the reload early-return.

## Root cause / scope

`RangedWeaponState.Tick` previously reset bow recovery whenever the trigger was held, including when ammo was zero. Its magazine reload branch returned before handling trigger release and before allowing the completed magazine to fire. Normal magazines also started reloading only after the last round was consumed.

The interruptible-recovery change is in the shared scheduler; controllers/HUD consume its existing state and recovery counter. No recovery is added to normal nonempty held firing. Pause/UI/death protections remain, and invalid time cannot cancel recovery or spend ammo.

## Current base tuning

| Weapon | Damage | Ammo | Reload/recovery |
| --- | --- | --- | --- |
| Magic Book | 6 | 7 | 1.4s full magazine |
| Bow | 6 | 6 | Existing 0.45s idle delay + 1s per arrow; 0.3s fire interval |
| Crossbow | 6 (15 fully charged) | 1 | 1s reload; 1.5s full charge unchanged |
| Red Arrow (called Blood Arrow in the tuning request) | 3 | 9 | 1.5s full magazine; speed6, 0.7s fire interval |

Bow and Crossbow now have **one extra base penetration**: each can damage two distinct targets before upgrades. The data field `pierce` counts total target hits, not extra penetrations, so it is set to 2 (the previous zero was clamped to one hit). Crossbow charge still adds up to two hits, giving 2/3/4 targets at low/half/full charge. Selection displays Bow's actual 1s per-arrow recovery, not its separate 0.45s idle delay. Existing interruptible reload behavior remains unchanged.

Sword is temporarily filtered out in `LoadoutSelection.SetPrefabs`, including dynamically rebuilt catalogs. Its prefab/data, Knight default equipment, and direct equip/combat remain intact. A sword-only catalog is rejected before changing existing UI. Removing that temporary filter restores selection without rebuilding assets. Selection descriptions/stats reflect the new behavior and values.

## Checks

The state regression now also checks the exact tuning above, early/late cancellation, cooldown enforcement, zero progress carry-over, input fences, Red Arrow interruption, and multi-round crossbow charging. Current results supersede the historical counts below.

Current Bow 0.3s/shot and 1s/arrow verification: **35 scheduler + 150 actual weapon/ammo checks PASS**, no Console errors. The extra 0.45s initial idle delay is unchanged. Evidence: `Captures/WeaponHoldReload/state.json` and `Captures/WeaponAmmo/regression.json`.

Prior tuning verification (before Bow interval changed from 0.6s to 0.3s): **487 checks PASS**: scheduler/tuning34, selection5 (all four actual stat previews and 16:9/4:3 layout), weapon ammo150, Red Arrow76, combat upgrades222. Actual collision tests prove Bow hits two targets and ignores a third; a full-charge Crossbow hits four and ignores a fifth. Low/half/full charge snapshots, collision deduplication, recovery/cancel timings and multishot pierce are verified. Standalone/long-run balance not retested.

Previous tuning verification: **1008 checks PASS**: scheduler/tuning34, selection4 (including 16:9/4:3 visual review), weapon ammo139, Red Arrow76, combat upgrades222, upgrade trees284, and loadout gameplay249 (all12 available character/weapon pairings). Evidence: `Captures/WeaponHoldReload/state.json`, `Captures/LoadoutSelection/regression.json`, `Captures/WeaponAmmo/regression.json`, `Captures/RedArrowWeapon/runtime.json`, `Captures/RunCombatUpgrades/runtime.json`, `Captures/UpgradeTrees/runtime.json`, `Captures/LoadoutGameplay/regression.json`. Reports rerun after further tuning supersede these historical counts.

### Historical verification before interruptible recovery/tuning

- `Tools/System Leveling/Test Hold Fire and Partial Reload`: calls production states directly, using existing asset data, and checks held empty reload, partial reload, cooldown, released/blocked input, bow per-arrow recovery, invalid time and unchanged crossbow charge. **22 checks PASS**; the failing baseline is archived as `Captures/WeaponHoldReload/before-fix.json`.
- `Run Weapon Ammo Regression (Play Mode)`: existing real-controller/projectile/HUD/upgrades/pause/death/restart suite extended for held empty bow and book reload plus partial book reload.
- Results: **22 state + 120 Play Mode + 3 selection UI checks PASS (145 total)**. Updated the visible book/bow descriptions and checked their text fit at 16:9/4:3. Compile jobs `2f15a8ab` (scheduler/tests) and `af568698` (selection description) completed with 0 errors. Source weapon/character assets remain hash-identical.
- Evidence: `Captures/WeaponHoldReload/`. No new ammo input, manual reload key, balancing changes or dependency were added.

Editor returned to saved LoadoutSelection outside Play Mode. Standalone build behavior was not separately tested.
