# Compact gameplay HUD
Status: Frozen. Approved by user on 2026-11-02 after correction: top-left HP, option 1 but shorter.

## Goal and scope
Make gameplay readable without large opaque HUD panels. Applies to SampleScene and SnowScene, not menu/selection screens.
- FR-001: HP is a short horizontal bar at top-left, around 240 reference pixels wide and 18 high, inset 8 pixels. NOT a thin full-width top strip. EXP is a thin full-width strip flush with the bottom edge. No decorative frame or oversized panel. Preserve live values.
- FR-002: One compact text row immediately under HP: ammo/max, LV and Soul. Keep survival timer visible without a panel. Preserve current Soul meaning and save-failure notice; no currency writes from the new presentation.
- FR-003: Cursor image displays at 50% original width/height, center hotspot. Preserve the user's modified source PNG; use a derived image.
- FR-004: During active gameplay, small ammo/max text follows the cursor, clamped on-screen, text height smaller than the cursor. Hide for melee, missing mouse, pause, settings, level-up and ended run. All overlays must ignore pointer raycasts.
- FR-005: While reloading, show a small bracket/line/marker indicator `[|---------------------]` above the character, tracking actual ReloadFraction. Hide when done or blocked; distinguish reload from charging. Use current bow/crossbow/magazine behavior without changes.
- FR-006: Keep boss HP, game-over/win, restart, settings, level-up and loadout behavior; no new panels or combat changes.

Defaults approved after correction: HP 240x18 reference pixels, inset 8; EXP 6 high; compact row 18 high. Numbers readable at 16:9 and 4:3. Cursor half-size shared by game menus through default Player Settings; ammo/reload overlays gameplay only.
Non-goals: redesign dialogs, change reload timings/ammo counts, rebalance combat, edit Soul wallet, change source cursor art.
