# Delivery loop

Current state: accepted
Current loop: LOOP-004
Frozen specification: PRD.md, TECH_DESIGN.md, ACCEPTANCE.md
Approval: user "được rồi", 2026-10-22
Next action: User plays the saved SampleScene; unrelated potion component cleanup is deferred.

## LOOP-001
- Inspected live Editor through UnitySkills REST: bypass/full, SampleScene saved and not playing.
- Preserving user changes: EnemyAnimation, spriteFacesLeft, four ReferenceEnemies and added spawn references.
- No acceptance claimed until runtime/reload/Idle checks and screenshots.
- Judgment: Pending.

## LOOP-003 results and user decision
- HUD test reported actual runtime values: Mage max HP=50, current HP=50, cap=100, while the file on disk still had maxHealth=200. This was live asset data differing from the old hardcoded 200-HP fixture, not a deferred Start race.
- Stopped automatic rework as required after repeated regression setup failures. Asked user whether to clone test data, use current stats or defer the old test.
- User selected "Giữ 50 HP cho test": expectations now derive from current Mage data (10% damage, heal clamp, regeneration/potion relative to max). No gameplay asset edited and no assertions removed.
- Judgment: CHANGES_REQUESTED with user-approved fixture redesign.

## LOOP-004
- Objective: Verify current 50-HP data, then fresh-scene two-restart integration and Idle animation liveness.
- Production scope unchanged; new approval applies only to regression fixture expectations.
- Judgment: Pending.

## Final evidence and judgment — LOOP-004
- ACCEPTED_WITH_CAVEATS. Every blocking AC passes. Caveats are existing asset-memory/potion configuration noted below; no gameplay data overwritten.
- HUD regression: PASS, 42 assertions, actual Mage=50 HP. Source data files byte-identical across tests.
- Game Over regression: PASS, 44 assertions, two real Restart reloads after a separate baseline load. All five spawner enemy types entered Idle, remained stationary with zero velocity/disabled simulation, and advanced normalized animation time.
- No-spawn verification waited 5.5s (> configured 5s interval); count and spawnTimer unchanged.
- Screenshots allow three frames for CanvasScaler resize to settle; both aspect ratios inspected for HUD, level-up and Game Over without overflow.
- Final compile job 3a7b3690 completed. Final console read-back had 0 errors from the final test window. Earlier test failures and unrelated historical Blender connection error were not deleted or hidden.
- Final editor state: SampleScene, dirty=false, isPlaying=false, isCompiling=false. Artifacts: README.md and Captures/BlueHudGameOver/final-editor-state.json.

| AC | Result | Evidence | Caveat |
| --- | --- | --- | --- |
| AC-001 | Pass | hud/level-up/game-over screenshots at 16:9 and 4:3 | English labels retained |
| AC-002 | Pass | Lethal TakeDamage, single Died event, no regen/heal/upgrade resurrection | Test explicitly clears pre-existing i-frame before lethal input |
| AC-003 | Pass | Input lock, level-up dismissal/disable cannot resume run, player/enemy stationary | Game Over runs timeScale=1, as approved for live Idle |
| AC-004 | Pass | Two real reloads; repeated Restart only one load; fresh stats, no prior objects/locks | No save/load across runs |
| AC-005 | Pass | 42 HUD regression assertions and final compile/error read-back | Expectations use current Mage stats by user approval |
| AC-006 | Pass | Five enemy variants: Idle and advancing normalizedTime; no spawn after 5.5s | Already dying enemies keep Die |

## LOOP-002 results
- New test was started in the previous long-running gameplay scene, not a fresh run. It correctly refused a pre-existing dead run. Harness now performs its own baseline reload before asserting fresh state.
- HUD fixture initial snapshot raced deferred Start after dynamic object activation; now waits (bounded 5s) for experienceCap initialization and includes actual values in any failure.
- Failure artifacts preserved with loop002 suffixes. No production behavior changed for these repairs.
- Judgment: CHANGES_REQUESTED (fixture setup).

## LOOP-003
- Objective: Fresh-scene deterministic integration; bounded initialization waits; unchanged product assertions.
- If the same blocking criterion fails a third consecutive loop, stop rework and report rather than weakening it.
- Judgment: Pending.

## LOOP-001 results
- Production compiled successfully (UnitySkills jobs 28324f69 and f3e5dc00); blue UI and Game Over migrated without replacing the scene.
- REST listener changed port on reload (8090 → 8091), same instance ID; recovered using registry and completed compile job read-back.
- Existing HUD test failed due to a changed fixture assumption: user has now added HealthPotion to source prefab, while scene potion retains its earlier added component, yielding two HealthPotion components on scene object. Fixture now prefers the valid source prefab; scene user components are not removed.
- New test reached visual evidence and level-up successfully, but lethal damage was blocked by a pre-existing gameplay i-frame in the scene; harness now explicitly clears that test subject's i-frame before calling real TakeDamage.
- Failure artifacts: Captures/HudLevelUp/regression-blue-loop001-failed.json and Captures/BlueHudGameOver/regression-loop001-failed.json.
- Judgment: CHANGES_REQUESTED (test fixtures); production behavior unchanged.

## LOOP-002
- Objective: Run deterministic pickup and lethal-damage checks, five enemy Idle liveness checks, actual two-restart integration and screenshots.
- Changed: test fixtures only; no weakening of assertions.
- Known unrelated scene issue: two HealthPotion components; deferred rather than removing user configuration.
- Judgment: Pending.
