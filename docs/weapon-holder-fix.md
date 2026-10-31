# Sửa điểm cầm/ngắm vũ khí (2026-10-29)

Status: Integrated into the approved neutral-equipping work. User clarified that the goal is reusable character/starting-weapon selection, not only Hunter+nỏ positioning. Current contract and verification: docs/spec-driven/weapon-equipping/.

## Nguyên nhân / phạm vi
- Bow/Cross_bow đã đổi từ PPU100 sang PPU64, nhưng localScale cũ được tính một lần theo PPU100: kích thước từ .65 tăng thành khoảng 1.016 world unit. Giữ nguyên importer/ảnh của người dùng.
- Sprite nỏ trỏ chéo +45°, cung trỏ chéo +135°, còn logic cũ giả định +X. Bắn từ offset .45 cố định, không phải đầu vũ khí.
- WeaponHolder cũ ghi scale.y thành ±1, làm mất scale gốc; thứ tự đọc pointer/ngắm/bắn không được bảo đảm. Pointer chưa gán có thể làm holder xoay về gốc world.
- Callers đã kiểm: PlayerController.PointerPosition; MeleeWeaponController.GetDirection; RangedWeaponController.GetDirection; WeaponAmmoSetup và prefab nhân vật/weapon.

## Sửa tối thiểu
- WeaponGrip nằm trên từng weapon prefab: Visual Size, Sprite Angle, Hold Offset, Sprite Grip (tọa độ chuẩn hóa trong sprite), Muzzle Offset. Chuẩn hóa kích thước và grip theo sprite hiện tại, kể cả đổi PPU; hoạt động cả Edit/Play Mode. Không sửa texture, damage, ammo hay reload.
- WeaponHolder nhận pointer sẽ cập nhật aim ngay, giữ độ lớn scale khi lật và tránh đổi facing liên tục khi ngắm thẳng đứng; không ngắm khi pause/Game Over. PlayerController chỉ thêm execution order để input đi trước weapon.
- Ranged sinh đạn tại WeaponGrip.MuzzlePosition; prefab cũ chưa có profile vẫn dùng fallback .45, không vỡ caller cũ.
- Calibration: book .34/angle0, bow .40/angle-135, crossbow .40/angle-45, sword .52/angle-90. Chỉ anchor zero cũ chuyển xuống waist y=-.12; anchor đã chỉnh tay giữ nguyên.
- Menu `Tools/System Leveling/Fix Weapon Holding` cập nhật pose tại chỗ, không thay trang bị, không reset balance hoặc tạo lại nhân vật. Backup trong Captures/WeaponHolder/Before.
- Giữ Hunter người dùng đặt trong scene. Vì game là singleton, các regression tạm tắt nhân vật preview thừa trong Play Mode và chọn Player làm đối tượng kiểm tra; không xóa/tắt Hunter trong scene Edit Mode.

## Kiểm chứng cần chạy
Compile; hướng ngắm 4 phía, không lag khi set pointer, giữ scale, near-zero/NaN pointer, pause; grip trùng tay; PPU32→64 giữ kích thước bằng sprite test tạm không đổi .meta; art hướng đúng khi flip; projectile thật xuất phát từ muzzle; regression ammo/charge/kiếm/HUD/GameOver/spawn. Ảnh Editor/Play để kiểm scale và giữ nguyên actor scene.

## Không làm
Không thêm inventory/đổi vũ khí, không thay nhiều-player thành multiplayer, không chỉnh chỉ số/nguồn sprite. Khi chơi thực tế nên chỉ có một PlayerController active.
