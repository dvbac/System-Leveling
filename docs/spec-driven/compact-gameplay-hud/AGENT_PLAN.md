# Compact HUD plan
Owner: main agent, no delegation. Approved corrected layout 2026-11-02.
1. WeaponHud.cs: optional compact formatting and non-interactive cursor/reload overlays, existing state as source. Do not modify weapon state/controller or currency.
2. Editor/CompactHudSetup.cs: migrate gameplay HUDs and reusable prefab; derived half-size cursor; backups/idempotency. HudSceneBuilder calls the same compact layout to avoid reverting it on a future Build HUD.
3. Tests/CompactHudRegression.cs and directly impacted HUD assertions: verify both maps/ratios and fire/reload/pause/game-over/level-up/melee; capture evidence. Captures/CompactHud only.
Forbidden: source cursor PNG, wallet/save data, unrelated dialogs/menu assets, enemy/spawn configuration.
