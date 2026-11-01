# Main menu and navigation

Open `Assets/Scenes/MainMenu.unity` and enter Play Mode. This is the first enabled build scene.

## Flow

```text
Main Menu → Character → Weapon → Map → Gameplay
Back/Esc: Map → Weapon → Character → Main Menu
Gameplay Esc: Continue / Quick Restart / Settings / Main Menu
Game Over: Quick Restart / Another Fate / Settings / Main Menu
Another Fate → fresh Character selection
```

- **Start Run:** opens the existing prefab-driven selection scene. Repeated activation while loading is ignored.
- **Controls:** opens a modal with the actual movement/aim/attack/crossbow/level-up controls. Back or Escape closes it and restores menu focus. It blocks pointer and keyboard activation of the underlying actions.
- **Quit:** closes a standalone game. In the Editor it displays an explanation without stopping your editing session.
- **Selection Back / Escape:** maps return to weapons; weapons return to characters; characters return to Main Menu.
- **Game Over:** Quick Restart keeps the same character, weapon and map; Another Fate starts fresh character selection. Settings and Main Menu are also available. All navigation locks while a scene is loading.

Esc pause and session settings are implemented. Soul is saved locally but hidden on Main Menu; selection shows the saved total and gameplay shows this run's earnings. Run state and settings remain session-only. See `docs/soul-currency.md`.

## Design and assets

The menu uses the supplied `Assets/MyAssets/UI/Background/Background.png` directly, without changing its pixels or import settings. It fills the screen without stretching. A generated transparent gradient (`Assets/MyAssets/UI/MainMenu/menu-shade.png`) improves left-side readability while leaving the portal bright.

Title and actions sit on the left. Cream text, teal emphasis and understated navy button fills follow the selection UI. The fixed 1280×720 safe layout is centered and scales by width, supporting 16:9 and 4:3. Background cropping is right-aligned so the portal remains visible at narrower aspect ratios; menu controls remain visible.

## Setup and verification

`Tools > System Leveling > Build Main Menu` creates the new scene only if it does not already exist. Run it outside Play Mode with LoadoutSelection open and all scenes saved. Backups of the existing selection/gameplay scenes and Build Settings go to `Captures/MainMenu/Before/`. Existing catalogs, item prefabs and gameplay player wiring are preserved. Only the scene HUD's Game Over buttons are adjusted; the HUD prefab is not overwritten.

Enabled scenes include MainMenu, LoadoutSelection, SampleScene (grass), and SnowScene (snow). Start Run goes through character, weapon, then a dedicated map view before launching. `Connect Loadout to Gameplay` also preserves MainMenu as the entry when it exists; Quick Restart retains the current map. Esc pause and expanded result menu are described in `docs/run-menus.md`.

`Tools > System Leveling > Test Main Menu Flow (Play Mode)` verifies button raycasts and bounds, Controls blocking/focus, Editor Quit behavior, duplicate-load protection, both selection Back routes, chosen equipment reaching gameplay, and Game Over returning to Main Menu at 1920×1080 and 1024×768. Results and captures go to `Captures/MainMenu/`.

The existing loadout gameplay regression still verifies every prefab pairing and Restart. Actual standalone process termination and packaged-build behavior are not covered by Editor checks.
