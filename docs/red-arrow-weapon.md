# Red Arrow — vũ khí tự tìm địch

"Blood Arrow" trong yêu cầu chỉnh chỉ số là Red Arrow hiện tại. Kiếm tạm ẩn khỏi lựa chọn, không xóa asset.

## Sử dụng
Chọn nhân vật → chọn **RED ARROW** (biểu tượng mũi tên đỏ ở cuối danh sách) → chọn map rừng hoặc tuyết. Giữ chuột trái để bắn.

- Damage **3**, **0,7 giây/phát** (khoảng 1,43 phát/giây), **9 đạn**, nạp **1,5s**.
- Tốc độ đạn6, knockback2; mỗi phát một viên, hướng ban đầu ngẫu nhiên trong±30° so với hướng ngắm.
- Đạn tìm địch sống trong8 unit, quay tối đa720°/s, bay tối đa5s. Không có mục tiêu thì giữ hướng; mục tiêu chết/mất/đã trúng thì tìm lại. Tìm lại không quá một lần mỗi0,15s.
- Có **1 bounce gốc**: đánh một địch rồi nảy sang địch khác trong5 unit, giữ nguyên damage. Không nảy vào tường, không đánh lại cùng địch.
- Pierce thêm được tiêu trước bounce. Rubber Bullets và Elasticity cộng vào bounce gốc; damage/fire rate/projectile count/ammo/reload/status/Splinter dùng hệ nâng cấp hiện có.
- Dùng magazine scheduler chung: hết băng tự nạp; thả chuột khi băng đã dùng một phần cũng bắt đầu nạp. Bắn lại khi còn đạn sẽ hủy nạp, không được cộng đạn hay giữ tiến độ hồi. Hết đạn phải hồi xong trước. Pause/UI/end không bắn xuyên; pause đóng băng đạn.

Đây là phiên bản **homing+bounce theo Batgun**, dùng hình RedArrow của dự án, không có hình dơi. Không thêm summon stats/tree, giá mở khóa3150 Soul hoặc3 evolution level20.

## Assets và setup
- Ảnh gốc giữ nguyên: `Assets/MyAssets/Weapon/Base/RedArrow.png`.
- Bản import Point/PPU96: `Assets/MyAssets/Weapon/Generated/RedArrow.png`.
- Vũ khí: `Assets/Prefab/Weapons/RedArrow.prefab`.
- Đạn: `Assets/Prefab/Weapons/RedArrowProjectile.prefab`.
- Data: `Assets/Scriptable Object/Weapon/Red Arrow.asset`.
- Ngoài Play: **Tools > System Leveling > Red Arrow > Build Weapon** tạo asset/catalog thiếu; chạy lại không trùng, giữ prefab/data có sẵn. Backup catalog trước sửa ở `Captures/RedArrowWeapon/Before-LoadoutSelection.unity`.

Không sửa default weapon/nhân vật hay gameplay scenes. RedArrowController chỉ thay góc bắn một viên; RedArrowProjectile chỉ điều khiển homing; damage, pierce, bounce và status vẫn ở ProjectileWeaponBehaviour. WeaponData mới có BaseBounces, mặc định0 cho mọi asset cũ.

## Kiểm tra
Trong Play: **Tools > System Leveling > Red Arrow > Run Regression (Play Mode)**. Test tự chuyển scene, dùng ví Soul riêng và trở về màn chọn khi xong; không dùng giữa run muốn giữ.
Evidence: `Captures/RedArrowWeapon/runtime.json`, `asset-validation.json`, ảnh selection/forest/snow. Ảnh gameplay được tạo bằng tick nhanh để kiểm tra băng đạn, không phải tốc độ bắn production.
Kết quả bản đầu: **70 kiểm tra riêng +950 kiểm tra tương thích PASS** (1020 tổng), gồm15 tổ hợp nhân vật–vũ khí và va chạm physics thật với mục tiêu kiểu boss. Tổng hợp lịch sử: `Captures/RedArrowWeapon/validation-summary.json`. Kết quả hiện tại sau chỉnh chỉ số nằm trong `runtime.json` và `Captures/WeaponHoldReload/state.json`.

Tham khảo cơ chế/chỉ số: [Batgun](https://20minutestilldawn.wiki.gg/wiki/Batgun), [Bounce](https://20minutestilldawn.wiki.gg/wiki/Bounce). Không sao chép art từ wiki.
Chưa cân bằng một run đầy đủ, đo tải dài hạn hoặc kiểm tra standalone build; tốc độ/chỉ số giữa hai game có thể cần chỉnh.
