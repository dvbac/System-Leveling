# UI border styles

`Tools > System Leveling > Apply UI Border Styles` reapplies the supplied UI artwork to the three scenes and shared HUD prefab. Run outside Play Mode with all scenes saved. Backups are kept in `Captures/UiBorders/Before/`; existing backups are never overwritten.

| Role | Artwork under `Assets/MyAssets/UI` |
| --- | --- |
| Primary action / vitals | `Border/panel-border-005.png` |
| Secondary actions / XP | `Border/panel-border-009.png` |
| Controls / upgrade window | `Panel/panel-014.png`, `Border/panel-border-014.png` |
| Stats / Game Over | `Border/panel-border-028.png` |
| Level badge / upgrade cards | `Border/panel-border-002.png`, `022.png`, `005.png` |
| Panel surfaces | `Transparent border/panel-transparent-border-015.png`, `Transparent center/panel-transparent-center-005.png` |
| Rules / accents | `Divider/divider-004.png`, `Divider Fade/divider-fade-003.png` |

Only the selected artwork is imported as single sprites, with point filtering and 9-slice borders. Original PNG pixels are untouched. Decorative images do not receive pointer raycasts. Circular loadout slots, runtime selection colors, prefab catalogs, layout and scene navigation are retained.

The older scene builders still build their original designs; rerun Apply UI Border Styles after rebuilding a screen. No runtime theme system was added.

Verification: `Captures/UiBorders/style-check.txt` plus the existing Main Menu, Loadout Selection, Loadout Gameplay and Game Over regression menus. Their output screenshots also show the updated styling.
