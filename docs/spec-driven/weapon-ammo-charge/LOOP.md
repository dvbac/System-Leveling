# Delivery loop
Current state: accepted
Current loop: LOOP003
Approval: "chốt", 2026-10-29; PRD/TECH_DESIGN/ACCEPTANCE frozen.
Next: none blocking. Evidence: EVIDENCE.md and Captures/WeaponAmmo/summary.json. Long-session balance remains outside acceptance.

## LOOP001
- Existing user work: three character prefabs now have empty WeaponHolder (old sword removed), Arows1 and WeaponHolder.prefab new. Preserve them.
- Runtime facts: book auto-fire, melee own click handling, four upgrades, projectile data independently assigned.
- No production changes before approval. Judgment pending.

## LOOP001 results / LOOP002
- Compile jobs 3c20d990/90b325a1 succeeded. Initial migration found no scene sword (user removed old weapon); generated a Sword prefab with existing sprite, existing Sword data and unchanged melee controller instead of restoring/deleting user work.
- Migration complete; ranged production and actual UI-upgrade test PASS 78 checks, real Restart included.
- Reference regression reached 50 checks then failed on retired Assets/Prefab/Blue Slime.prefab (missing before weapon work). Preserved reference-loop001-failed.json. GameOver tests also depended on this missing legacy fixture.
- LOOP002: retain legacy behavior tests using a runtime clone of Reference Slime with EnemyAnimation removed when retired Blue Slime is absent; no production enemy/spawner edits. Weighted boundary fixtures now use explicit valid assets, not a potentially missing user scene entry. This preserves the assertions, not skips them.
- Next: compile, run all suites and inspect actual diff/preservation evidence. Judgment pending.

## LOOP002 results / LOOP003
- Weapon 78, Reference 51, HUD 42, GameOver 44 PASS.
- Spawn suite failed its old hardcoded 2/3/4/5 pool-entry count after user retired Blue Slime and removed its opening-phase entry. Not a weapon or spawning-code defect; scene had this live edit before migration.
- Preserved spawn-loop002-failed.json. Replace entry-count assertion with stricter per-phase actual prefab-set equality (Slime → +Skeleton → +Spider → +Wolf; include legacy Blue only when it exists). Null entries still exercise production's invalid-entry exclusion. No spawning code or scene spawn configuration changed.
- Next: final compile and full validation. Judgment pending.

## LOOP003 results — accepted
- Final five suites: Weapon 81, Reference 51, HUD 42, GameOver 44, Survival 140; total 358 PASS. Includes actual ranged owner UI upgrades and actual Restart, not only pure scheduler checks.
- Compile 077fe42d (runtime + tests) and 4f69fd36 (final editor setup) succeeded with 0 C# errors.
- Final visual check showed the book obscuring the character at the original 0.25 offset. Calibrated ranged visual offset to 0.55, book height to 0.45 world units, kept bow/crossbow 0.65; HUD panel made opaque blue. Reapplied the same approved preset, reran all five suites PASS, captured hud-final.png.
- Improved input fence so releasing over UI can still recover bow arrows but never emits a click-through shot. Regression covers this edge.
- No enemy/spawner production code or Sword data modified. Character prefab changes were in-place, retaining user-empty WeaponHolder/collector/body/animator; original pre-migration YAML backed up.
- Existing Blue Slime retirement remains unchanged. Tests retain legacy no-animation coverage with a synthetic Slime fixture; scene pools can warn about retired references, which production excludes safely. No restoring deleted user assets.
- Scene saved and clean, Editor stopped. No new gameplay C# errors observed in final run; earlier fixture/setup failure artifacts retained.
- Judgment: ACCEPTED_WITH_CAVEATS. AC001–009 pass; balance over long sessions / target-device profiling not claimed. Acceptance matrix and usage in EVIDENCE.md.
