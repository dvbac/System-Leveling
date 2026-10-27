# Delivery loop

Current state: delivered
Current loop: LOOP-002 (closed)
Approval: user "duyệt", 2026-10-22
Frozen specification: PRD.md, TECH_DESIGN.md, ACCEPTANCE.md
Evidence: EVIDENCE.md, Captures/SurvivalSpawning/
Next action: none blocking; user tunes preset in the Enemy Spawner Inspector if desired.

## LOOP-001
- Facts checked: current spawner fields, EnemyStats private death flag, five prefabs, ReferenceEnemyRegression serialized configuration and GameOverRegression private timer coupling.
- UnitySkills health: same SystemLeveling_BC4E0716, port8091, bypass/full, not compiling.
- Preserving all user stats, UI, Enemy AI/animation and reference prefabs; only add IsDead read access to EnemyStats.
- Judgment: Pending.

## LOOP-001 results
- Compile job9454823c completed with zero C# errors. Approved preset migrated and saved into SampleScene.
- New survival/Horde regression passed on first run, including forced deadline, weighted selection/unlocks, finite geometry rejection, caps/budgets, clear/death, pause, Game Over and actual Restart.
- Reference regression was invoked after the gameplay player had died during the gap between menu calls. Live PlayerStats.IsDead=true was read back; spawner correctly refused to spawn under Game Over. Test failed its missing fresh-run precondition, not the spawning implementation.
- Preserved failure: Captures/SurvivalSpawning/reference-loop001-failed.json.
- Judgment: CHANGES_REQUESTED (reference test baseline only).

## LOOP-002
- Reference harness now starts from a freshly loaded scene, then disables only the unrelated scene spawner while testing its own real spawners. Enemy movement/hit/Die/drop assertions unchanged.
- No production behavior changed for the fixture correction.
- Found and fixed one real defect while reviewing the deployment pipeline: the delta that triggered a forced deadline was also charged to the new deployment timeout, so a single long frame could abort the drop. `BeginDeployment()` now starts deployment with zero elapsed; regression covers a 100 s frame.
- Added an assertion that every Horde member stays outside the camera on the chosen side and keeps separation.
- Results: Survival/Horde 140 PASS, ReferenceEnemy 51 PASS, HUD 42 PASS, Game Over/Restart 44 PASS.
- Console: no new errors after the runs. Scene saved; saving produced byte-identical YAML (dirty flag was cosmetic).
- Judgment: PASS. All blocking AC have live evidence in EVIDENCE.md.
