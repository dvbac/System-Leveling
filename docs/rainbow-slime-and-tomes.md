# Rainbow Slime và thẻ đỏ

## Luật xuất hiện

- Cả SampleScene và SnowScene dùng `EnemySpawner` hiện có, tải `Assets/Resources/RainbowSlime.prefab`; không cần sửa/lưu scene.
- Phút **7:00**: xét một lần, mặc định **50%**. Phút **12:00**: nếu đã có một con thì xét 50% cho con thứ hai; nếu chưa có con nào thì bảo đảm một lần xuất hiện. Như vậy một lượt đạt phút 12 có **1 hoặc 2** slime (75% một con, 25% hai con).
- Tỷ lệ 50% là mặc định tạm chọn, có thể chỉnh `rainbowSpawnChance` trong Inspector. Nếu chưa có vị trí an toàn, giữ yêu cầu và thử lại mỗi giây; không ép spawn vào tường/ngoài nền hợp lệ. Lượt kết thúc sớm có thể chưa gặp slime.
- Có quần thể riêng, không chiếm cap quái thường/horde/elite; cùng kiểm tra nền, vật cản và khoảng cách spawn an toàn. Không tạo thêm đợt sau hai mốc, không tạo vô hạn khi hụt FPS hoặc tắt/bật spawner.

## Hành vi và phần thưởng

- Slime dùng lại 4 animation Idle/Move/Hit/Die. Bản grayscale riêng giữ slicing, pivot, alpha và timing của slime gốc; SpriteRenderer đổi màu cầu vồng theo thời gian. Không sửa art/animation gốc.
- Cấu hình ban đầu: **120 HP**, tốc độ **1.2**, kích thước **x1.5**, sát thương tiếp xúc **0**. Đi lang thang quanh vị trí xuất hiện, không truy đuổi. Có phản lực khi bị bắn và kiểm tra nền/vật cản khi di chuyển.
- Tồn tại **60 giây gameplay**. Pause, settings, lựa chọn nâng cấp/Tome không trừ thời gian. Có tên/thời gian trên đầu và chỉ dẫn hướng/khoảng cách ở HUD.
- Hết giờ: vô hiệu hóa ngay, hủy collider rồi biến mất; **không Kill, không Soul/EXP, không thẻ**. Callback va chạm/death bị trễ cũng không thể biến timeout thành phần thưởng.
- Bị hạ: rơi **đúng một thẻ đỏ**, dùng ảnh sẵn có `Assets/MyAssets/Other/Drop/RedCard.png`; không roll loot thường. Soul mặc định từ EnemyStats vẫn hoạt động khi bị hạ.
- Chạm thẻ bằng collider nhân vật (không phải vòng hút EXP) mở modal hiện có, pause gameplay và cho chọn **1 trong 3 Tome khác nhau**. Không tự chọn do click mở modal, không cộng trùng vì nhiều collider/click. Tối đa hai phần thưởng đặc biệt mỗi lượt.
- Toàn bộ modal thẻ đỏ dùng **tông đỏ**: nền tối đỏ, viền, chữ phụ, thẻ và trạng thái hover/selection. Khi trở về nâng cấp lên cấp thường, khôi phục đúng toàn bộ palette xanh ban đầu; không sửa màu prefab/scene gốc.

## Tome

- Nhóm Tome tách khỏi nâng cấp level thường; không đổi ID/giá trị enum `UpgradeKind` cũ. Các nâng cấp thường vẫn hoạt động.
- Mỗi Tome chỉ nhận một lần/lượt; không đưa Tome đã nhận vào offer tiếp theo. Buff/debuff cùng áp dụng và reset khi tải lượt mới, không ghi vào Soul/save lâu dài.
- Tên/mô tả và toàn bộ chỉ số nằm trong `TomeInfo` / `PlayerTomes.cs`, theo 11 Tome đã yêu cầu. HP tăng hồi thêm đúng số HP tăng; HP giảm chỉ cắt current HP nếu vượt max mới. Không đưa Tome giảm HP vào offer nếu max HP chỉ còn 1.
- Glass nhân mọi sát thương được tích hợp x2, tắt né tránh và khiến damage được chấp nhận qua i-frame giết ngay. Might tính lại bonus `2 * MaxHealth` khi max HP thay đổi. Wind tính bonus tốc độ di chuyển vĩnh viễn, không lấy slow do tấn công/địch làm giảm chỉ số này.
- Elasticity: tối đa hai lần ricochet tới địch chưa trúng trong bán kính 5, sau khi dùng hết pierce; không quay lại cùng mục tiêu. Power/Stillness áp dụng size, damage, piercing/projectiles vào projectile thực tế. Stillness chặn đi bộ hoàn toàn.
- Rage nhân **ammo cơ bản** x3 (kể cả nỏ 1 → 3), không nhân cấp Capacity; không cấp đạn miễn phí ngay khi nhận. Nỏ vẫn giữ cơ chế sạc/nhả và nâng trần charge qua Capacity.
- Electro gọi sét **một lần mỗi đợt bắn hết đạn**, không nhân số lần theo Double Shot. Đánh các địch sống trong bán kính 5 quanh điểm bắn với damage nền 22; hưởng Elements và bonus all-damage/Might, không hưởng bullet-only modifiers.
- **Giới hạn Elements:** game chưa có đòn burn/freeze của người chơi. Hai multiplier burn/freeze đã có giá trị 1.35 nhưng chưa có nguồn sử dụng; không tự thêm proc cháy/đóng băng ngoài yêu cầu. Elements chỉ được offer khi đã có Electro để +35% lightning có tác dụng thực tế. Khi thêm nguồn burn/freeze phải nối hai multiplier còn lại (đã đánh dấu `shortcut:`).
- Né tránh cơ bản hiện là 0%; Vitality giảm 25 **điểm phần trăm**, chặn ở 0. Không tự thêm nâng cấp né mới.

## Thiết lập và xác minh

- `Tools > System Leveling > Build Rainbow Slime`: tạo art/override controller/prefab/stat asset riêng, không ghi đè asset đã có và không lưu scene.
- `Tools > System Leveling > Run Rainbow Slime Regression (Play Mode)`: kiểm tra trực tiếp cả hai map, scheduler, lifetime/pause, timeout/death/pickup chống trùng, UI 3-chọn-1, nhóm thường tách riêng, tất cả Tome stats và các hook projectile/sét/scheduler. Dùng `SoulTestIsolation` để không cộng/xóa Soul thật.
- Bằng chứng: `Captures/RainbowSlime/regression.json`, `rainbow-slime.png`, `red-card-choice.png`.
- Kết quả Play Mode: **90 kiểm tra mới PASS** và **222 kiểm tra Combat Upgrade cũ PASS**. Sau kiểm tra đã thoát Play, quay về `LoadoutSelection` không có thay đổi scene chưa lưu. Các mốc thời gian trên ảnh là thời gian giả lập của test, không phải một lượt chơi thực kéo dài tương ứng.
- Chưa kiểm tra standalone build hoặc cân bằng thực chiến một lượt 15 phút; kiểm tra dùng Tick có delta giả lập để tới các mốc spawn, không chờ 12 phút thật.
