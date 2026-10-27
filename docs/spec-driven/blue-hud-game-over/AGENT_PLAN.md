# Kế hoạch

Owner: main agent, triển khai tuần tự; không delegation.
Approval: user "được rồi", 2026-10-22.

## Contracts
- PlayerStats.IsDead và Died idempotent, không hồi sinh từ health/EXP/upgrade.
- GameOverController.GameplayStopped là cổng gameplay cho scene; timeScale=1 trong Game Over để Idle chạy.
- LevelUpSelection đóng khi PlayerStats.IsDead, không khôi phục pause khi Game Over sở hữu lượt.
- Enemy.HoldForGameOver chặn physics/movement và đưa quái sống vào Idle; EnemyAnimation giữ Die cho quái chết.

## Ownership và thứ tự
1. PlayerStats, UI/GameOverController, LevelUpSelection và gameplay gates (Player, Enemy, Spawner, Weapon, Experience) — FR003–007/AC002–006.
2. HudSceneBuilder, prefab UI, SampleScene, Blue Slime Idle/controller — FR001–002,007/AC001,006. Migration có mục tiêu, không xóa scene hoặc overwrite phần khác.
3. Assets/Tests và Captures/BlueHudGameOver, docs/spec-driven/blue-hud-game-over — mọi AC. Harness cũ chạy lại; harness mới sống qua scene reload để kiểm tra Restart thật hai lần.

## Fact/risk census
- Có thay đổi user mới trong Enemy: spriteFacesLeft và EnemyAnimation (ReferenceAnimated). Phải giữ chúng.
- Spawner dùng cả Blue Slime cũ lẫn bốn ReferenceEnemies; controller cũ chỉ có Move/Hit, cần Idle thực.
- Không dừng thời gian toàn cục trong Game Over; phải chặn cả projectile đang bay, slash đang chạy, physics và spawn.
- Restart phải đóng level-up trước, tránh OnDisable trả lại timeScale=0 sau khi tải scene.
