# Map selection, pause and death menus

## Start a run

**Main Menu → character → weapon → dedicated map screen → gameplay.**

Weapon selection no longer launches immediately. Choose GRASSLANDS or SNOWFIELDS on the full-page map screen to start that scene. Back/Esc on the map screen returns to weapons without discarding the character; choosing the same weapon again works. The map page is a separate view in LoadoutSelection, not a third gameplay scene.

## In game

**Esc** opens a pause menu with **Continue**, **Quick Restart**, **Settings**, **Main Menu**. Esc closes Settings back to pause; another Esc continues. Pause owns the previous time scale, freezes physics/spawn clocks, and uses the existing shared gameplay-input gate so movement and weapons cannot process menu clicks. The closing frame remains input-blocked.

Esc does not bypass the level-up choice. If an upgrade is queued while paused, Continue presents that upgrade and the game stays paused until it is chosen. Death while paused dismisses the pause/settings views and transfers control to Game Over, without clearing the death gate.

## On death (and victory)

The result screen has **Quick Restart**, **Another Fate**, **Settings**, **Main Menu**.

- Quick Restart: reload the current map, retain chosen character/weapon, reset HP/XP/upgrades/spawn clocks and UI state. This uses the existing restart/loadout path, not a partial in-place reset.
- Another Fate: load fresh character selection; old loadout state is cleared, then choose weapon and map again.
- Settings: return to this same result screen when closed; never resume a dead run.
- Main Menu: unload gameplay and restore normal menu time/input.

Scene navigation locks all competing buttons while loading and restores controls on failure. Missing scenes report an error without unpausing the pause menu.

## Settings

Master volume changes `AudioListener.volume` immediately (0–100%, non-finite values rejected). Fullscreen uses `Screen.fullScreen` in standalone; Editor play mode does not change the Editor window. Values last for this session only; no preferences/save system or resolution/quality/rebinding UI was added.

The separate **Soul wallet** is persistent and is not cleared by Quick Restart, Another Fate, Main Menu or a new application session. Selection shows the saved total; the gameplay HUD starts at zero and shows only Soul earned this run. Main Menu hides Soul. Both victory and defeat show the frozen gameplay time (MM:SS, pauses excluded) and Soul earned. Spending/upgrades are not implemented yet. See `docs/soul-currency.md`.

## Gameplay timer

Both maps now show **SURVIVAL TIME / MM:SS** at the top center of the HUD. `PlayerHud` reads the existing `EnemySpawner.ElapsedTime`, so the displayed time and survival phases/Horde share one clock. No second timer or change to spawning/boss timing was added. The label updates only when the whole second changes; runs over an hour display total minutes (e.g. `60:00`).

Esc pause, Settings, upgrades and Game Over stop the clock. Continue resumes it; Quick Restart creates a fresh scene clock and returns to `00:00`. The panel uses the existing level-badge style and does not intercept UI raycasts. Older unconfigured HUD prefabs still work without a timer binding.

Setup: `Tools/System Leveling/Add Gameplay Timer` (one-time, preserves both scene backups under `Captures/RunTimer/Before/`, refuses to overwrite an existing timer). Timer checks extend the menu-flow regression with initial value, second/minute/hour boundaries, pause/settings/upgrades/death, resume, both restart routes, text/viewport fit at 16:9 and 4:3. `60:00` in the screenshots is an accelerated test fixture, not an hour-long soak test. Current timer evidence is in `Captures/RunTimer/`: **278 menu/timer checks + 245 loadout checks PASS (523 total)**; compile `a335d659`, 0 errors. Editor returned to Edit Mode; standalone build validation was not performed.

## Authoring / checks

- Runtime: `LoadoutLauncher`, `LoadoutSelection`, `RunMenu`, `GameOverController`, `LevelUpSelection`.
- One-time migration: `Tools/System Leveling/Configure Map Screen and Run Menus`. Preserves disk and unsaved live scene copies in `Captures/RunFlow/Before/`; refuses to overwrite the completed map view or existing RunMenu components.
- Both gameplay scenes have scene-specific menu components/overrides; shared HUD prefab, gameplay assets and source stats are not rewritten.
- `Run Menu Flow Regression (Play Mode)`: real synthetic Esc input, both maps, map Back, movement/spawn pause, volume, queued upgrades, competing scene loads, pause/death Quick Restart, Another Fate, Main Menu, raycasts/text/viewport checks at 1920×1080 and 1024×768. Restores audio, keyboard device and Game View resolution after testing.
- Evidence: `Captures/RunFlow/runtime.json` and map/pause/settings/death PNGs. New menu runtime: **234 checks PASS**. Updated loadout (16 pairings): **245 PASS**; Main Menu/death navigation: **66 PASS**; separate-map streaming/spawn suite: **1,031 PASS**; selection layout: **3 PASS**; survival/Horde: **140 PASS**. Total: **1,719 checks PASS**.
- Compile `50f02298` completed with 0 errors. No new console error groups after migration. Source hash comparison preserves 128 grass chunk/prop and enemy prefab/data files. Scene backups and reports are retained; no prefab/stat/texture assets or package dependencies were rewritten.
- Editor finishes in saved LoadoutSelection, outside Play Mode. Standalone fullscreen, disk persistence and long-session balancing have not been tested/added.
