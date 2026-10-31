# Thiết kế vũ khí
Status: Frozen
Approval: User "chốt", 2026-10-29; triển khai phương án đã trình bày, không mở rộng tính năng.

- Giữ WeaponController/MeleeWeaponController và đơn vị speed của kiếm. MagicBookController kế thừa RangedWeaponController mới; Bow/Crossbow dùng chung controller theo WeaponFireMode. Không thêm dependency.
- WeaponScriptableObject thêm cấu hình ammo/reload/charge với default tương thích; dữ liệu và trạng thái runtime tách biệt.
- RangedWeaponState là scheduler production thuần C#, Tick(delta, held) trả Shoot + hệ số charge; controller nhận input, chặn UI/pause/death, tạo projectile. Tick không bù nhiều phát trong frame dài. Ammo/Reload upgrades đọc từ PlayerStats.
- Reload băng: bắn cạn → nạp đầy. Cung: nhả nút → idle delay → hồi từng tên; nhấn lại hủy tiến độ chưa hoàn thành. Nỏ: press khi sẵn sàng → charge → release snapshot → reload; giữ qua reload không tự bắt đầu charge mới. Pause hủy charge và yêu cầu nhả input trước khi tiếp tục, reload/cooldown đóng băng.
- InitializeShot(data, owner, direction, damageFactor, sizeFactor) trên projectile: snapshot damage/speed/pierce/scale, không sửa asset; SpriteRenderer và collider cùng root scale. Chống trúng nhiều collider của cùng mục tiêu.
- WeaponHolder trang bị StartingWeapon khi child chưa có WeaponController, tái sử dụng hierarchy kiếm hiện có. Migration chỉ thay phần vũ khí ở Archer/Hunter và Player Mage, giữ mọi chỉnh sửa khác; Knight có kiếm. Không thêm selection/inventory.
- LevelUpSelection thêm Ammo/Reload khi có ranged; enum thêm ở cuối, giữ giá trị cũ. PlayerStats sở hữu số cấp runtime reset mỗi run. HUD component riêng dùng UI Text/Image có sẵn.
- Validation ở data, giới hạn chỉ số không hữu hạn/âm, khoảng thời gian dương, cap an toàn. Test production scheduler + Play Mode projectile/collider/upgrades/UI/pause/restart, regression cũ migrate đúng hành vi sách không còn auto-fire.
- Backup asset bị migration trước khi ghi trong Captures/WeaponAmmo/Before; không SaveAssets toàn project. Lỗi thiếu tham chiếu phải báo, không trừ ammo cho prefab không hợp lệ. Không revert thay đổi sẵn có.
