# Thiết kế kỹ thuật

Status: Frozen
Approval: User "được rồi", 2026-10-22

## Hiện trạng và rủi ro
- PlayerStats hiện không lưu trạng thái chết, Kill chỉ log, Recover có thể hồi HP sau 0.
- LevelUpSelection.Close khôi phục timeScale đã lưu; khi Game Over/reload cần ngăn giải phóng pause sai hoặc để lượt mới bị timeScale=0.
- HUD prefab gắn PlayerStats bằng scene override. Builder hiện giữ UI cũ nếu đã tồn tại: cần migration có mục tiêu thay vì tạo HUD trùng hoặc xóa scene.

## Phương án
- Thêm trạng thái chết và sự kiện chết trong PlayerStats, chặn nhận EXP/nâng cấp/hồi máu sau chết; phát sự kiện idempotent.
- Điều phối Game Over quản lý overlay, level cuối và Restart; kết nối với selection/input gates. Game Over có ưu tiên pause; lifecycle/reload phải trả timeScale đúng.
- Tải lại scene hiện hành bằng SceneManager, chống yêu cầu tải trùng; không lưu bonus vào ScriptableObject.
- Game Over để timeScale=1 và khóa actor/physics, spawner, đòn đánh và pickup riêng. EnemyAnimation ưu tiên Idle hơn Hit khi lượt kết thúc; giữ Die cho quái đã chết. Blue Slime cũ chưa có Idle: thêm clip thở nhẹ dùng sprite Slime.png hiện có và state Idle, không thay Move/Hit hay art nguồn.
- Chỉnh palette và bố cục HUD/level-up trong prefab, scene instance và builder; giữ camera/light/tilemap/gameplay khác.
- Thêm kiểm tra bằng code gameplay thật và Play Mode, ảnh hai tỷ lệ; giữ regression HUD/level-up cũ.

## Phạm vi file dự kiến
Assets/Scripts/Player/PlayerStats.cs, Scripts/UI, Scripts/Editor/HudSceneBuilder.cs, input gates trong PlayerController và Weapon controllers nếu cần, Prefab/UI/System HUD.prefab, Scenes/SampleScene.unity, Assets/Tests, docs/spec-driven/blue-hud-game-over.

Không sửa dữ liệu character/weapon/enemy gốc hay nhịp spawn; chỉ thêm điều kiện ngừng spawn khi Game Over. Không sửa thư mục asset nguồn ngoài project.
