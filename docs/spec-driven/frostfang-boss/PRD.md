# Frostfang Alpha — yêu cầu
Status: Frozen. Approval: Approved by user 2026-11-04: “Duyệt, triển khai”. Owner: main agent.

## Mục tiêu và luồng chơi
Tạo boss map tuyết từ bộ sprite người dùng cung cấp, có đủ bốn đòn và hiệu ứng riêng đã tạo. Người chơi sống sót đến mốc xuất hiện, né báo đòn, đánh boss, xem hoạt ảnh chết rồi kết quả trận.
Thành công: các AC trong ACCEPTANCE.md đều có kiểm tra runtime và ảnh thực tế.

## Phạm vi đã duyệt
- FR-001: Chỉ SnowScene, xuất hiện một lần tại 17:00 thời gian gameplay; pause/level-up không cộng thời gian. Có menu Spawn Now để thử nhanh.
- FR-002: Dùng ảnh trong Assets/MyAssets/Monster/Boss/Frostfang cho Idle, Move, Pounce, Breath, Pillars, Howl, Death. Chuẩn hóa bản sao, không sửa PNG gốc. Dùng bốn sprite sheet hiệu ứng trong Captures/FrostfangFX/Animations.
- FR-003: Bám đuổi rồi luân phiên bốn đòn: vồ vào vị trí khóa trước, phun băng hình quạt khóa hướng, gọi ba cọc băng tại các điểm đã báo, tru tạo bão tuyết quanh boss trong 4 giây. Có báo đòn trước sát thương, có hồi phục giữa các đòn. Không gây sát thương ngoài phạm vi báo tương ứng.
- FR-004: Cọc gây sát thương một lần khi trồi; phun băng và bão gây sát thương theo nhịp, kèm slow tạm bằng cơ chế ApplySlow hiện có, không thay tốc độ nâng cấp gốc. Bão không che kín nhân vật. Bản đầu không thêm cơ chế trú sau cọc hoặc phá cọc.
- FR-005: Thanh HP/tên boss; nhận sát thương từ kiếm và đạn hiện tại. Chết phát hết Death rồi VICTORY như Rotwood; player chết trước vẫn thua. Cleanup hiệu ứng/cọc khi boss chết, run kết thúc hoặc đổi scene. Thưởng Soul đúng một lần theo EnemyStats hiện có.
- FR-006: Giữ nguyên boss rừng, HUD/menu, cơ chế vũ khí, save Soul và weighted spawn pools. Không ghi đè thay đổi khác đang có.

## Giá trị khởi đầu đề xuất
HP 2200, tốc độ 1.6, chạm 15, Soul 150. Vồ 25; phun 10 mỗi 0.5s; cọc 20; bão 6 mỗi 0.75s. Slow 40% trong 1.2s. Đây là tuning ban đầu, không cam kết cân bằng đầy đủ.

## Ngoài phạm vi / deferred
Không phase hai, minion, audio mới, redesign HUD, đổi vũ khí, thêm package hoặc commit/push. Chưa cân bằng một run 15 phút hoặc build/DPI.

## Quyết định còn mở
D-001: Approved 2026-11-04, toàn bộ phạm vi trên, mốc 15:00 và victory sau Death.

D-002: User update 2026-11-05: đổi mốc spawn thành **17:00 (1020s)** trên cả hai map, thay mốc 15:00 của D-001; các cơ chế boss khác giữ nguyên.

## Dữ liệu và rủi ro
Chỉ prefab/data mới, bản sprite chuẩn hóa và thêm encounter SnowScene; giữ backup scene trước khi lưu. Không thay định dạng save. Regression dùng SoulTestIsolation, không ghi thưởng vào ví thật.
Decision log: 2026-11-04 người dùng yêu cầu tạo boss bằng bộ Frostfang; hành vi gameplay cụ thể ở trên đang chờ xác nhận.
