# Red Arrow — vũ khí theo cơ chế Batgun
Status: Frozen. Approval: user “Duyệt bản này”, 2026-11-05. Owner: main agent.

Amendment theo yêu cầu chỉnh vũ khí tiếp theo: Red Arrow/Blood Arrow dùng damage3, speed6, interval0.7s, ammo9; magazine đang hồi có thể bị hủy bằng bắn khi còn đạn. Sword tạm ẩn khỏi lựa chọn. Các yêu cầu số cũ bên dưới là phạm vi bản đầu; giá trị hiện tại và quy tắc hồi đạn trong `docs/weapon-reload.md` được ưu tiên.

## Mục tiêu
Dùng sprite RedArrow.png người dùng chỉ định để thêm một vũ khí chọn được, bắn mũi tên đỏ tự đuổi địch và nảy như Batgun. Thành công khi các AC runtime đạt và vũ khí cũ không đổi hành vi.

## Nguồn đã kiểm tra
- https://20minutestilldawn.wiki.gg/wiki/Batgun : damage18, fire rate2/s, projectile1, ammo12, reload1.5s; hidden speed15, spread60°, knockback2, piercing0, bounce1. Batgun gốc hưởng summon damage/attack-speed.
- https://20minutestilldawn.wiki.gg/wiki/Bounce : nảy giữa địch, giữ nguyên damage; có pierce thì xuyên trước, hết pierce mới bounce. Không phải bật vào tường.
- Game đã có đạn, reload, pierce, bounce, projectile upgrades/tomes; chưa có summon stats hoặc cây summon upgrade. Asset base thực tế hướng phải.

## Phạm vi đã duyệt
- FR-001: Thêm Red Arrow vào danh sách chọn vũ khí; dùng base Assets/MyAssets/Weapon/Base/RedArrow.png cho hình cầm và đạn. Không thay default weapon hoặc ép gắn Vampire.
- FR-002: Giữ chuột bắn, damage18, 2 phát/s (cooldown0.5), 1 đạn/phát, băng12, reload1.5s theo magazine scheduler hiện có; speed15, knockback2, góc ra đạn ngẫu nhiên ±30°.
- FR-003: Đạn tìm/đuổi địch sống trong bán kính8, quay có giới hạn thay vì teleport; tìm lại nếu mục tiêu chết/mất. Không có địch thì tiếp tục hướng hiện tại. Lifetime5s scaled; pause dừng di chuyển/tìm mục tiêu, hết run không gây damage.
- FR-004: Base bounce+1, không xuyên mặc định: một viên đánh tối đa hai địch khác nhau ở base. Không trúng lại cùng địch do collider lặp. Upgrade pierce/bounce cộng thêm, xuyên trước khi nảy; damage không giảm sau nảy.
- FR-005: Hưởng nâng cấp đạn/tốc bắn/băng/nạp/status/splinter/tomes hiện có; đạn phụ tương thích khi dùng prefab Red Arrow. Không thêm hệ summon stats hoặc cây summon mới trong bản đầu; do đó đây là bản cơ chế homing+bounce của Batgun, không phải full summon-hybrid.
- FR-006: Dùng được cả map rừng/tuyết và nhân vật hiện có; HUD ammo/reload giữ nguyên. Không sửa ảnh gốc, save Soul, boss, chỉ số vũ khí cũ hoặc catalog thứ tự cũ. Chỉ append vũ khí mới.

## Ngoài phạm vi / deferred
Không copy hình dơi/súng Batgun, không giá unlock3150 Soul, không ba evolution level20, không character mới, không summon tree. Không thay semantics magazine hiện có (thả chuột khi dùng dở băng cũng bắt đầu reload).
Chỉ số trên là khởi đầu từ nguồn, chưa cam kết cân bằng hai game khác nhau. Build/performance dài hạn deferred.

## Quyết định mở
D-001: Approved 2026-11-05, homing+bounce + upgrades hiện có, không summon-hybrid/evolution.
Không đổi dữ liệu save; prefab/data mới và append catalog có backup. Regression dùng ví Soul test riêng.
