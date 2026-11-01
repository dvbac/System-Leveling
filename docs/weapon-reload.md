# Hold fire and partial reload

Current behavior (2026-11-01), replacing the old empty-bow/release-only restriction:

- **Magic Book / normal magazine weapons:** when the trigger is released and any ammo is missing, start the existing full-magazine reload without waiting for zero ammo. Pressing/holding again during reload waits for completion, then fires immediately and continues at the configured fire rate. Holding through an empty-magazine reload also resumes without another click.
- **Bow:** with arrows remaining, normal held firing still does not regenerate arrows. Releasing starts recovery even if the bow is not empty, using the existing idle delay/per-arrow time. At zero arrows, recovery can proceed while the trigger is held; the recovered arrow fires immediately, then later arrows recover/fire one at a time. A stalled frame never produces a burst.
- **Crossbow unchanged:** hold to charge, release to fire; holding across reload does not arm a new charge. No changes to its damage, charge, reload or upgrades.
- Pause, level-up, Game Over and UI input fences remain in force. Reload itself never demands a fresh click. For normal magazines, a real release during reload now clears the existing UI fence instead of being swallowed by the reload early-return.

## Root cause / scope

`RangedWeaponState.Tick` previously reset bow recovery whenever the trigger was held, including when ammo was zero. Its magazine reload branch returned before handling trigger release and before allowing the completed magazine to fire. Normal magazines also started reloading only after the last round was consumed.

The fix is confined to the shared scheduler. Controllers, HUD, prefabs, scenes, stat assets and cooldown/reload-time presets are not rewritten. In particular, recovery is not added to normal nonempty held firing.

## Checks

- `Tools/System Leveling/Test Hold Fire and Partial Reload`: calls production states directly, using existing asset data, and checks held empty reload, partial reload, cooldown, released/blocked input, bow per-arrow recovery, invalid time and unchanged crossbow charge. **22 checks PASS**; the failing baseline is archived as `Captures/WeaponHoldReload/before-fix.json`.
- `Run Weapon Ammo Regression (Play Mode)`: existing real-controller/projectile/HUD/upgrades/pause/death/restart suite extended for held empty bow and book reload plus partial book reload.
- Results: **22 state + 120 Play Mode + 3 selection UI checks PASS (145 total)**. Updated the visible book/bow descriptions and checked their text fit at 16:9/4:3. Compile jobs `2f15a8ab` (scheduler/tests) and `af568698` (selection description) completed with 0 errors. Source weapon/character assets remain hash-identical.
- Evidence: `Captures/WeaponHoldReload/`. No new ammo input, manual reload key, balancing changes or dependency were added.

Editor returned to saved LoadoutSelection outside Play Mode. Standalone build behavior was not separately tested.
