# Kế hoạch triển khai

Main agent sở hữu toàn bộ công việc, làm tuần tự; không delegation.
Approval: user "duyệt", 2026-10-22.

## TASK-001 — scheduler/cấu hình
Paths: Assets/Scripts/Enemy/Spawning/. Contracts: settings, phase/weighted selection, HordeSpawnCycle state transitions; không phụ thuộc scene để test random/time xác định. AC001–004,007.

## TASK-002 — tích hợp spawning/geometry
Paths: Assets/Scripts/Enemy/EnemySpawner.cs, EnemyStats.cs (IsDead getter), Assets/Scripts/Editor/SurvivalSpawnSetup.cs, EnemySpawnerEditor.cs, Assets/Scenes/SampleScene.unity. Hai population độc lập, placement an toàn, budget, Debug Log, preset. AC005–009. Không thay AI/animation/stats assets/UI.

## TASK-003 — evidence
Paths: Assets/Tests/SurvivalSpawnRegression.cs, ReferenceEnemyRegression.cs và GameOverRegressionRunner.cs (migrate API), Captures/SurvivalSpawning, docs/spec-driven/survival-horde-spawning. Chạy scheduler boundary tests, thực tế spawn/từng prefab/placement/budgets/clear/deadline và regression UI/GameOver/Restart.

## Validation
Compile qua UnitySkills asset_refresh/job_wait, Play Mode real components. Không báo đạt AC nếu chỉ test logic mô phỏng khác code thật. Thất bại phải giữ artifact và giới hạn sửa đúng phạm vi. Sau ba loop cùng AC thất bại, dừng và xin quyết định theo skill.
