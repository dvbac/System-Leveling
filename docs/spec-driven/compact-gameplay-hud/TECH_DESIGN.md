# Compact HUD technical design
Status: Frozen. User approved corrected layout 2026-11-02. Based on PRD 2026-11-02.

- Reuse PlayerHud, WeaponHud, SoulBalanceView, timer references and RangedWeaponState (Ammo/Capacity/IsReloading/ReloadFraction). Do not duplicate gameplay state.
- Editor layout migration modifies existing UI rects/texts, disables panel/border graphics and decorative labels without deleting unrelated user UI. Apply to both gameplay scenes, update reusable HUD prefab. Preserve menu/selection scenes and existing modal layouts.
- WeaponHud gains optional compact display and optional cursor ammo/reload references; legacy mode remains available to existing views.
- Screen-space Canvas text, camera WorldToScreenPoint and RectTransformUtility conversion for overlays. Use actual equipped weapon and current player bindings; no new gameplay polling singleton.
- Generate half-size cursor with Point sampling under generated asset path; defaultCursor/hotspot read back. Original PNG/hash unchanged.
- Scene backups before write, preserve unsaved live edits, idempotent setup. All static/overlay graphics raycastTarget=false. No new package.
- Runtime regression uses existing production state ticks for firing/reloading/upgrades, no currency mutation. Verify layouts/screenshots on both maps, 16:9/4:3, pause/menu/death, melee, near viewport edges and character tracking.
