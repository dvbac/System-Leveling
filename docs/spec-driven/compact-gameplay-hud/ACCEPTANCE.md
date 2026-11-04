# Compact HUD acceptance
Status: Frozen. User approved corrected layout 2026-11-02. Based on PRD/TECH_DESIGN 2026-11-02.

All ACs blocking; evidence under Captures/CompactHud.
| ID | FR | Observable check |
|---|---|---|
| AC-001 | 001/002 | Both maps: short borderless HP top-left, inset 8, around 240x18; EXP flush bottom/full width; no large HUD backgrounds; compact ammo/LV/Soul row and timer. Values follow real stats. Screenshots 16:9/4:3. |
| AC-002 | 003 | Derived cursor half dimensions, hotspot centered, original image SHA unchanged, defaultCursor saved. |
| AC-003 | 004 | Pointer label tracks/clamps at all screen edges, ammo/max matches current weapon and upgrades, physical text height < cursor height; no pointer input blocked. Hidden for melee/blocked gameplay. |
| AC-004 | 005 | Bracket reload indicator follows above player, advances with actual reload fraction, vanishes on completion/weapon switch/pause/death; charging alone does not show reload. |
| AC-005 | 006 | Player/Soul/timer bindings valid on both maps, dialog buttons and boss bar still usable; restart works. Runtime/compile no new errors; setup repeat no duplicated HUD. |

Deferred: standalone build/device-DPI verification and long gameplay balancing. Done: all ACs backed by runtime checks and visual review, original source/currency unchanged.

## Acceptance matrix — 2026-11-02
Judgment: **ACCEPTED_WITH_CAVEATS**. All blocking ACs pass.

| AC | Result | Evidence | Caveat |
|---|---|---|---|
| AC-001 | Pass | runtime-regression.json; four `*-reload-*.png` screenshots; HP value above fill, 240x18 inset, edge EXP, real damage/EXP/Soul and clock | Small text deliberately compact |
| AC-002 | Pass | setup.json, runtime asset dimensions/hotspot assertions, source SHA read-back | OS hardware cursor not captured by Unity screenshots; build/DPI deferred |
| AC-003 | Pass | real equipped book/bow/crossbow state; 4 pointer corners per map/ratio; physical text-height comparison; non-raycast UI; hide/resume and melee checks | None |
| AC-004 | Pass | bracket follows player and real reload progress; paused real reload stays frozen and feedback hides; settings/charge/completion/switch/death checks; screenshots | Font-based bracket, no extra VFX asset |
| AC-005 | Pass | actual two-map bindings, boss spawn/no HUD overlap, level-up buttons, real death/restart, no Soul change; setup applied twice with one feedback group per scene | Existing spawn-pool warnings retained, not modified |

Final compile succeeded (job daf4e1a9), no Console errors. Runtime: **257 PASS**. Existing assertions that expected weapon names were updated to accept the intentionally compact ammo/max format without changing their state validation.
