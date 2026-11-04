# Compact HUD delivery
Current state: accepted
Current loop: LOOP-003
Approved specification: PRD, TECH_DESIGN, ACCEPTANCE, 2026-11-02.
User correction: HP is top-left, short, readable; NOT thin full-width top-edge.
Next: user preview/playtest; no further scope authorized.

## LOOP-001
- Scope: all FRs/ACs. Main agent only.
- Inspected: PlayerHud, WeaponHud, RangedWeaponState/Controller, SoulBalanceView, GameplayLoadout, RunMenu, HudSceneBuilder, HudTimerSetup and SoulCurrencySetup.
- Existing dirty source cursor/config are user work; derive new cursor rather than overwriting source.
- Judgment: Pending.

## LOOP-003
- LOOP-002: 255 runtime checks PASS on both maps / both ratios, captured 4 reload screenshots.
- Visual review found HP text painted underneath the old fill sibling and low text contrast on snow. Release-blocking AC-001/004 visual correction: put HP value last, increase text-only black outline to 1px (no panel backgrounds).
- Add HP ordering assertion and exercise pause/settings during an actual in-progress reload, not after completion.
- Evidence of prior pass: runtime-loop002-pass-before-visual.json. Reapply idempotent setup then rerun all checks.
- Judgment: ACCEPTED_WITH_CAVEATS. 257 checks PASS; all 5 ACs pass.
- Commands/evidence: successful compile daf4e1a9, Console error read-back 0, Apply Compact Gameplay HUD rerun, real runtime report, 4 screenshots at both ratios. Source cursor SHA and exactly-one feedback group assertions pass after repeated setup. Player wallet total unchanged.
- Visual review: HP number now above fill; no large gameplay panels or decorative borders; small text outlined for snow; reload bracket above player; pointer ammo small. Hardware cursor not present in ScreenCapture by design, validated from actual 16x16 asset and hotspot.
- Files: WeaponHud, SoulBalanceView; CompactHudSetup plus Build HUD caller; directly impacted HUD-format/path regression assertions; compact regression and generated cursor/white sprite; reusable prefab and two gameplay scenes; PlayerSettings cursor assignment. Original cursor edits/deleted old material were pre-existing user work and preserved.
- Non-blocking limits: standalone build/DPI untested; old invalid spawn-pool warnings retained. Editor returned outside Play Mode.

## LOOP-002
- LOOP-001 compiled/applied both maps, passed layout/values/cursor bounds, then reload test failed: its first trigger was blocked by the real equip/UI release guard. Runtime read-back ammo=capacity, reload=False; no shot occurred.
- Evidence: Captures/CompactHud/runtime-loop001-failed.json. Production behavior not changed.
- Fix: fixture sends a release before its first held trigger, as production requires. Rerun full checks and screenshots.
- Judgment: Pending.
