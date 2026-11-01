# Rotwood Implementation Plan
Owner: Main agent, no delegation. Specification approved 2026-11-01.

1. TASK-001 (FR-001..008 / AC-001..008): runtime scripts in Assets/Scripts/Enemy/Boss, optional hooks in EnemyStats, temporary movement modifier in PlayerController, win result in GameOverController. Preserve existing callers and user changes.
2. TASK-002 (depends on TASK-001): Assets/Scripts/Editor/RotwoodBossBuilder.cs, generated assets under Assets/MyAssets/Monster/Boss/Rotwood/Generated, Assets/Prefab/Boss, one new encounter in SampleScene. Do not change source art/audio or normal spawn configuration.
3. TASK-003 (depends on 001/002): Assets/Tests/RotwoodBossRegression.cs, Captures/RotwoodBoss reports/screenshots. Run production code via real PlayerStats/EnemyStats and physics; regression current game-over flow.

Do not edit unrelated UI art, loadout, maps, spawner pools or other user-modified paths. Shared TakeDamage/Kill APIs stay unchanged; win entry point is GameOverController.Win().
Stop if scope changes or accepted mechanics cannot fit current APIs. Validate compile, asset setup, deterministic runtime checks and screenshot before acceptance.
