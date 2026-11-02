# Character and weapon selection

Start from `Assets/Scenes/MainMenu.unity` and choose Start Run, or open `Assets/Scenes/LoadoutSelection.unity` directly in the Editor. Select a character, then a weapon, then choose **GRASSLANDS / SNOWFIELDS** on the dedicated map screen. Clicking a map starts gameplay. MainMenu is the first enabled build scene when present.

- Dark navy background, teal preview rings, warm gold confirmed weapon rings and stat values.
- Compact, icon-only circles generated from the supplied prefab arrays. The current defaults contain four characters and four weapons; the UI does not assume those counts or names.
- Reference-style placement: large name and description at the upper left; six-column icon grid below; base stats in the right-hand portrait region instead of a portrait. Back is at the lower left on both selection screens.
- Hover or keyboard/gamepad focus previews details without committing a choice.
- Click/submit a character icon to select it and advance immediately to weapons.
- Click/submit a weapon icon to open the map screen without loading gameplay. Click a map to load `SampleScene` (grass) or `SnowScene`. Input is locked while loading so repeated clicks cannot start a second load. Map Back/Esc returns to weapons; weapon Back returns to characters.
- Back or Escape returns from weapons to characters without losing either choice; from characters it opens MainMenu. Returning to characters clears confirmation, so a new run configuration can be chosen.
- No Choose Weapon or Confirm Loadout buttons.
- Mouse and standard UGUI keyboard/gamepad navigation use the existing Input System UI module.
- The canvas scales by width and centers a 1280×720 safe layout, keeping it visible at 16:9 and 4:3.

## Scope

Character/weapon stat assets and balance are not rewritten. Migration adds a Mage prefab copied from the authored gameplay Player so every catalog entry is a real prefab. Existing weapon-grip settings are reused; Unity can reserialize their calibrated visual transforms when saving imported weapon prefabs. No locked content or fictional passive abilities were added. The persistent Soul balance is displayed separately; Soul purchases/permanent upgrades are deferred (see `docs/soul-currency.md`). Run-only combat upgrades are described in `docs/run-combat-upgrades.md`.

## Populate from prefabs

```csharp
selection.SetPrefabs(characterPrefabs, weaponPrefabs);
```

Call this in Play Mode with non-empty `GameObject[]` arrays of prefab assets, not scene instances. Inspector fields `Character Prefabs` and `Weapon Prefabs` supply the initial arrays automatically in Awake. Character data comes from `PlayerStats.characterData`, character icons from `SpriteRenderer`, and weapon data/icons from the prefab's `WeaponController` and visual renderer. No name-based lookup, per-class list, separate option-data entries or manually placed buttons is needed. Descriptions are generic usage hints rather than hardcoded class biographies.

`Assets/Prefab/UI/LoadoutOption.prefab` is the one shared UI template. Each screen stores an empty `Options/Viewport/Content` container; the function instantiates one template per supplied prefab. The reference layout uses 96×96 cells in six columns, with 16-unit horizontal and 24-unit vertical gaps in a 1280×720 safe area. Icons scale proportionally inside their cells. Long lists scroll vertically, and keyboard focus scrolls its item into view. Calling the function again replaces both lists, clears selection, and resets scroll; inactive old buttons are removed without retaining click handlers. Invalid input is rejected before the existing list is changed. Rebuilding is blocked while a gameplay scene is loading.

`Tools > System Leveling > Apply Reference Selection Layout` updates only UI positions and list layout, preserving the prefab arrays, item template and launch wiring. It first backs up the live scene to `Captures/LoadoutSelection/BeforeReferenceLayout.unity`. Stats occupy the right-side region around x=930; names begin at (72,82), icons at (60,270), and Back occupies (48,646,350,46) in reference units. Colors remain navy/teal/gold. No character or weapon prefab is rewritten by this layout command.

For a legacy scene, use `Tools > System Leveling > Use Dynamic Prefab Lists` outside Play Mode. It saves a copy of the current live scene (including unsaved layout edits) to `Captures/LoadoutSelection/BeforeDynamic.unity`, preserves the rest of the layout and gameplay wiring, and replaces only old option buttons with list containers. The initial prefab arrays are discovered from `Assets/Prefab/Characters` and `Assets/Prefab/Weapons`; afterward, edit those arrays in Inspector or supply your own arrays to the function. To change the appearance of every item, edit the shared UI prefab.

`LoadoutLauncher` listens to `onConfirmed` to show its map page, then retains the selected character data, appearance prefab and weapon in memory when a map is clicked and loads that scene. This is session state, not a disk save. Opening selection clears the prior run; entering a new Play session clears statics even with domain reload disabled. See `docs/snow-biome.md` for the two maps and `docs/run-menus.md` for pause, Quick Restart and Another Fate.

`GameplayLoadout` on the existing scene Player runs at execution order -200, before PlayerController (-100), PlayerStats and WeaponHolder. It sets character data before stats initialize, copies the chosen prefab's animation, sprite, scale, body collider and calibrated hand transform, then calls the existing `WeaponHolder.Equip`. The original Player object is never replaced, so camera, input, HUD, collection, enemy targeting and spawner references stay valid. Runtime weapon defaults cannot replace the explicit selection.

Quick Restart reloads the same gameplay scene and retains the session's character/weapon, while resetting health, XP and upgrades. Esc opens the in-game pause menu. Game Over offers Quick Restart, Another Fate (fresh character selection), Settings and Main Menu. Starting `SampleScene` directly in a fresh Play session still uses its original default Mage and Magic Book. Loadout/run state is session-only; Soul persists separately and is not reset by these routes. See `docs/run-menus.md` for navigation.

## Connection setup

`Tools > System Leveling > Connect Loadout to Gameplay` runs outside Play Mode with the selection scene open and all open scenes saved. It:

- Backs up both scenes and Build Settings under `Captures/LoadoutGameplay/Before/`.
- Validates the supplied prefab lists, adds `LoadoutLauncher`/`CanvasGroup`, and updates the footer. It does not replace the supplied lists or look up characters by name.
- Adds only `GameplayLoadout` to the gameplay Player; existing scene objects and links are kept.
- Keeps MainMenu first when present, followed by the enabled selection/gameplay scenes; preserves other entries.

## Verification

`Tools > System Leveling > Validate Loadout Selection` checks prefab configuration in Edit Mode. In Play Mode it also checks the generated icons, direct selection and navigation, preview without commitment, retained choices, single confirmation, invalid indices, absence of extra buttons, and byte-identical source data.

In Play Mode, `Tools > System Leveling > Capture and Test Loadout Selection` checks different list counts, repeat population, invalid input, and 19-item vertical-grid focus scrolling, then checks real EventSystem raycasts, pointer navigation, six-column cell sizing, right-side stat placement, viewport bounds and text clipping at 1920×1080 and 1024×768. Captures and a JSON result go to `Captures/LoadoutSelection/`. The temporary Game View sizes and background execution setting are restored afterward.

UI-only checks temporarily disable the launcher so their simulated choices do not leave the scene. `Tools > System Leveling > Test Loadout Gameplay (Play Mode)` exercises real scene loads for every supplied character/weapon pairing, input movement, stats, animation/collider/hand calibration, camera/HUD/spawner links, ranged shots or melee slashes, duplicate-load protection, level-up, death and restart. It also checks the direct-gameplay default and byte-identical source data. Results and gameplay captures go to `Captures/LoadoutGameplay/`.

`Tools > System Leveling > Build Loadout Selection` creates the scene only if the output does not exist and all open scenes are saved. It refuses to overwrite existing work.

For the previous card layout, open the selection scene outside Play Mode and run `Tools > System Leveling > Update Compact Loadout Selection`. It backs up the live selection scene to `Captures/LoadoutSelection/BeforeCompact.unity`, keeps the current catalog and confirmation event, replaces only the generated Background/Safe Area, and saves the updated scene.
