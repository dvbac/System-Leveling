# Kế hoạch triển khai

Owner: Main agent; thực hiện tuần tự, không delegation.
Approval: user "ok", 2026-10-21.

## Hợp đồng chung
PlayerStats giữ các field serialized hiện có; thêm Changed, MaxHealth, DamageMultiplier, PendingUpgrades và TryApplyUpgrade. UpgradeKind gồm Damage, MoveSpeed, Magnet, Vitality. Gameplay input dùng LevelUpSelection.InputBlocked. Asset gốc chỉ đọc.

## Các lát cắt
- TASK-001 (FR-004–006 / AC-004–005): PlayerStats, Scripts/System/Leveling và Scripts/Weapon: runtime stats, bonus damage, EXP và lượt chọn.
- TASK-002 (FR-001–003 / AC-001–003,006): Scripts/UI, Scripts/Editor, MyAssets/UI, Prefab/UI và SampleScene: UI, pause/input, asset import và editor setup có thể chạy lại.
- TASK-003 (mọi AC): Assets/Tests/Editor, docs/spec-driven/hud-level-up: chạy kiểm chứng logic thật, Play Mode, ảnh ở hai tỷ lệ màn hình, console và lưu kết quả.

Không sửa: spawn, Enemy, Scriptable Object gốc, thư mục Kenney nguồn, các file user không liên quan.

## Rủi ro đã truy vết
- Might=4 đang chưa dùng trong damage: bonus riêng mặc định 1 tránh thay đổi balance gốc.
- Heal đang đọc asset.MaxHealth: chuyển sang MaxHealth runtime ở cả RestoreHealth/Recover.
- Melee đọc Mouse trực tiếp trong Update dù timeScale=0: chặn rõ ràng và giữ fence một frame sau đóng bảng.
- Nhiều level từ cùng EXP: PlayerStats giữ PendingUpgrades, không dựa vào một thông báo UI duy nhất.
- Chưa có asmdef trong Assets: dùng regression harness editor gọi code thật, không tái cấu trúc toàn bộ assembly để thêm NUnit.
