# Soul — persistent currency

Soul is credited **directly on enemy death**, not a pickup that must be collected. The provided `Assets/MyAssets/Other/Drop/Soul.png` sprite is reused without changing its pixels/importer.

## Default rewards

| Enemy | Soul |
| --- | ---: |
| Slime | 1 |
| Skeleton | 2 |
| Spider | 3 |
| Wolf | 4 |
| IceSlime | 2 |
| SnowWolf | 5 |
| Snowman | 6 |
| Rotwood boss | 100 |

Edit **EnemyStats → Soul Reward** on the relevant prefab to rebalance it; zero is allowed, negative serialized values are treated as zero. Snow variants override this field without copying/changing their shared combat data. Other/future enemies default to 1 unless configured differently.

## Awarding and saving

- The existing idempotent `EnemyStats.Kill()` path credits Soul once, before loot/death animation. Repeated damage/Kill callbacks do not pay twice. Scene unloading, culling/destruction, boss cleanup of living summons, and Game Over cleanup do not award Soul.
- The wallet is independent of character/loadout and run state: death, Quick Restart, Another Fate, Main Menu and scene changes do not clear it.
- Local storage uses Unity `PlayerPrefs`, key **`SystemLeveling.Soul.Total.v1`**. Each positive reward is saved immediately; no disk save is tied to only surviving a run. Fresh sessions read the stored total.
- If a write fails, the pending total remains in memory, the Soul caption says **SAVE FAILED**, and save is retried on subsequent rewards, view transitions, application pause/quit. Failed writes do not interrupt death/loot. A force-close while storage remains unavailable can still lose unsaved rewards.
- Negative additions are rejected; arithmetic saturates at **2,147,483,647** instead of overflowing. No spending/reset API was added yet.

## Display / scope

Gameplay HUDs show **Soul earned this run**, starting at zero on every new map/Quick Restart; character/weapon and map selection show the persistent total. **Main Menu does not display Soul.** The saved balance is never cleared to reset the run counter. Views update when the value/save status changes; decorative graphics do not intercept mouse clicks.

Victory and defeat both show **TIME PLAYED (MM:SS)** and **SOUL EARNED** alongside the final level. Time is captured from the existing gameplay clock (pauses excluded), and Soul is counted separately from the saved wallet so earnings stay correct even when the wallet is saturated. Results freeze when the run ends; reopening the view does not reset them. Rewards are still saved immediately, not awarded a second time at the results screen.

**Purchasing/upgrading characters or weapons is deliberately deferred.** No shop buttons, upgrade effects/prices, physical Soul drops, cloud sync, anti-tamper protection or account system were added. PlayerPrefs is per local application/profile (Editor and standalone use their respective Unity storage).

## Authoring / validation

- `Tools/System Leveling/Configure Soul Currency`: one-time reward/UI migration. Backs up affected prefab/scene files and unsaved live target scenes under `Captures/Soul/Before/`; refuses to overwrite existing Soul views. Does not alter enemy combat data or shared HUD/character/weapon assets.
- `Run Soul Regression (Play Mode)`: actual prefab lethal damage for all eight types, single award in both wallet/run counter, storage read-back, zero/negative/overflow handling, simulated storage failure/retry, victory/defeat summaries and frozen results, restart-to-zero, Another Fate, hidden Main Menu counter, viewport/text/raycast checks on both maps/aspect ratios, and proof the production save is unchanged.
- All regression runners now attach `SoulTestIsolation`: they use a unique temporary save key, restore the original wallet when finished, and delete only their own temporary key. Synthetic kills never add money to the player's real wallet.
- `Captures/Soul/runtime.json`: **264 checks PASS**, including overlapping/out-of-order test scopes remaining isolated. Screenshots `victory-(1920, 1080).png` and `defeat-(1024, 768).png` show the result summaries. Screenshot balances are isolated test money, not grants to the player's real wallet. The two simulated save-error messages are intentional recovery evidence, not real disk failures.

Separate OS-process/standalone build persistence and long-session save-performance profiling have not been performed. Editor scene navigation/restarts and actual PlayerPrefs save calls are tested.

Final integration checks: boss **49 PASS** (including four HUD overlap checks), reference enemies **51 PASS**, loadout gameplay **245 PASS**, selection UI **3 PASS**. Boss health now sits below timer/Soul without covering the weapon panel; final screenshot: `Captures/Soul/boss-hud-final.png`. Reports are copied into `Captures/Soul/`. Original Soul art/importer and enemy combat-data hashes remain unchanged (`preservation.json`).

After the run-summary change, Soul **264 PASS**, boss **49 PASS**, and Game Over **44 PASS** were rerun. The Game Over runner requires a gameplay scene (launching it from selection does not establish its expected baseline). The separate keyboard-driven menu-flow runner stopped at `Esc from map returns exactly one step to weapons`, before reaching gameplay; this failure is retained in `Captures/Soul/run-flow-escape-failed.json` and has not been diagnosed here. Soul's direct-button navigation checks passed.
