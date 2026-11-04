# Red Arrow delivery
Current state: accepted
Current loop: LOOP-004
Frozen specification: PRD/TECH_DESIGN/ACCEPTANCE approved 2026-11-05
Objective: delivery complete, approved homing+bounce scope verified
Blocking issue: none
Next action: user playtest and tuning; no additional scope authorized

## LOOP-001
- Investigated Batgun and Bounce wiki; actual RedArrow PNG; ranged state/controller, projectile damage/bounce/status pipeline, player upgrade trees/tomes and loadout catalog.
- Confirmed summon stats/tree absent. Proposed no new summon system rather than silently claim full Batgun parity.
- Production files unchanged. Existing dirty work preserved. Agent: main only, no delegation.
- Judgment Pending; shared projectile safety unproven until runtime tests.

## LOOP-002
- User approved via question tool. Main agent only.
- Caller census: InitializeShot in ranged controller, PlayerUpgradeTrees and projectile regressions; base bounce default0 must preserve all old assets. Reuse private hit history via read-only protected predicate, not a second hit set.
- Unity port8090 timed out; registry identified the same project on8091, health bypass/full and editor outside Play Mode. API verification succeeded on connected project.
- Next: implement contracts in AGENT_PLAN, compile and run production tests. Judgment Pending.

## LOOP-003
- Own regression: 68 checks PASS, all homing/bounce/ammo/status/Splinter/Soul paths. Selection/forest/snow screenshots inspected; original style and readable UI preserved.
- Independent asset validation PASS: source SHA unchanged, sprite copy exact, old scene unchanged except one appended catalog entry; setup rerun without duplicate.
- UpgradeTreeRegression: 284 checks PASS with shared projectile changes.
- WeaponAmmoRegression initially failed its Mage/default-book precondition because own previous selection persisted in LoadoutLauncher statics, not due to changed old weapon. Evidence: ammo-contaminated-selection-failure.json. Own fixture now returns to selection scene at completion, clearing its static loadout; rerun own + clean baseline suite. No production behavior or old tests changed to accommodate this.
- Judgment Pending; compile and finish compatibility checks.

## LOOP-004
- **ACCEPTED_WITH_CAVEATS**. Own Red Arrow regression70 PASS, Weapon Ammo139 PASS after clean loadout, Upgrade Trees284 PASS, Combat Upgrades222 PASS, Loadout Gameplay305 PASS (all15 character/weapon pairings). Total1020 checks; summary in validation-summary.json.
- Added actual physics trigger against a kinematic boss-style fixture: damage18 delivered with real engine contact, not only manually invoked callbacks. Body collision compatibility proven live.
- Final test compile a570937d succeeded; Console0 errors after complete suites. Runtime returned outside Play Mode.
- Visuals inspected: selection lists Red Arrow fifth, icon matches original, description/stats correct; held weapon/projectiles readable on both maps. Gameplay captures intentionally batch scheduler ticks, not actual production fire-rate footage.
- Independent asset/scene proof: asset-validation.json; old catalog4 entries preserved in order, only fifth appended, other scene bytes unchanged, repeated setup unique, source SHA unchanged, Generated PNG byte-identical to source.
- Shared diffs reviewed: default-zero BaseBounces, additive bounce budget, read-only HasHit hook, default-zero single-shot spread hook, special selection description. No old weapon data rewritten, no InitializeShot API change, all pre-existing user edits preserved.
- Files owned: RedArrowController/Projectile/Builder/Regression; tiny shared edits in WeaponScriptableObject, ProjectileWeaponBehaviour, RangedWeaponController, LoadoutSelection; new sprite copy/prefabs/data; catalog append; docs/reports. Source PNG, gameplay scenes, defaults, save formats untouched.
- Caveats: no summon system/evolutions/unlock price per approval; no full-run balancing, high-load profiling or standalone build. Existing spawn-pool warnings retained. No commit/push.
- Next state accepted. All blocking ACs Pass; acceptance matrix in ACCEPTANCE.md.
