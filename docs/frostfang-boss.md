# Frostfang Alpha — boss map tuyết

## Chơi / thử nhanh
- Mở `Assets/Scenes/SnowScene.unity`, Play: boss xuất hiện một lần tại **17:00 gameplay (1020 giây)**. Pause/level-up không cộng thời gian; mốc 15:00 cũ không còn spawn boss.
- Không cần đợi: **Tools > System Leveling > Frostfang > Spawn Now (Play Mode)**.
- Prefab: `Assets/Prefab/Boss/Frostfang.prefab`.
- Hạ boss: phát Death rồi **VICTORY / FROSTFANG ALPHA DEFEATED**. Chết trước vẫn thua; Restart tạo encounter mới.

## Bốn đòn
1. **Rending Pounce:** vòng báo tại vị trí khóa trước, lấy đà 0,75s, vồ tối đa 6 unit trong 0,5s, gây damage một lần lúc đáp trong bán kính 1,3. Sweep dừng trước vật cản/ground không tải; không đuổi theo vị trí mới sau khi báo.
2. **Freezing Breath:** báo hình quạt 0,75s, khóa hướng, phun 1,5s; dài 5 unit, độ rộng nửa quạt bằng 25% khoảng cách từ miệng. Damage mỗi 0,5s trong quạt, vật cản đặc chặn sát thương.
3. **Ice Pillars:** ba điểm báo quanh vị trí người chơi khi cast. Cọc trồi lần lượt sau 0,75/0,95/1,15s, hit một lần trong bán kính 0,8; giữ hình rồi vỡ. Đây là hazard, không phải vật chắn/trú bão hoặc mục tiêu phá được.
4. **Whiteout:** tru báo 1s, bão trong bán kính 8 quanh boss trong 4s, damage mỗi 0,75s; hạt tuyết thưa, giữ đường biên vùng đánh. Vật cản đặc chặn sát thương.

Giữa các đòn có hồi phục 1s và bám đuổi 2,5s. Phun/cọc/bão gây slow 40% trong 1,2s bằng `ApplySlow`, không sửa stat nâng cấp gốc. Không có phase hai trong bản này.

## Tuning ban đầu
HP 2200; đi 1,6; chạm 15; vồ 25; phun 10/nhịp; cọc 20; bão 6/nhịp; thưởng 150 Soul. Sửa damage trong prefab, HP/tốc độ trong `Generated/FrostfangStats.asset`.

## Assets / tích hợp
- Ảnh gốc trong `Assets/MyAssets/Monster/Boss/Frostfang` giữ nguyên. `Generated/` chứa bản 192×160 mỗi ô, pivot chân (0,5;0,125), PPU48, không rescale pixel wolf.
- Số frame thực tế: Idle8, Run8, Pounce12, Breath16, Pillars10, Howl16, Death12.
- Bốn FX được copy nguyên PNG từ `Captures/FrostfangFX/Animations` rồi import/slice riêng trong Generated. Hitbox không dựa vào hạt bay.
- Playback bằng sequence như Rotwood, không thêm Animator/Enemy AI thường. Đạn/kiếm hiện tại nhận diện qua EnemyStats. EnemyStats chỉ thêm nhánh death cho Frostfang; Win() cũ vẫn giữ nguyên, overload tên boss dùng cho kết quả tuyết.
- Encounter chỉ được thêm vào SnowScene. Các document scene cũ và spawn pools không thay đổi; backup: `Captures/FrostfangBoss/Before-SnowScene.unity`.
- Ngoài Play: **Frostfang > Build Snow Boss** để tạo asset/encounter thiếu. Chạy lại giữ prefab/data đã chỉnh tay và không tạo trùng; không tự ghi đè sheet Generated đã có.

## Kiểm tra
Trong Play: **Frostfang > Run Regression (Play Mode)**. Test reload hai map, thử combat/death/restart/loss với ví Soul test riêng, không thay số Soul thật. Không chạy trong một run muốn giữ.
Report/ảnh: `Captures/FrostfangBoss/`; tiêu chí và lịch sử: `docs/spec-driven/frostfang-boss/`.
Kết quả hiện tại: **63 kiểm tra Frostfang PASS**, **50 kiểm tra Rotwood PASS**, gồm không spawn ở 15:00/16:59 và spawn một lần khi qua 17:00. 82 frame cơ thể không clipping; nguồn PNG, spawn settings và ví Soul thật giữ nguyên.
Ảnh regression dùng HP cao và ép tick để thử damage; đồng hồ 00:00 trong ảnh không phải mốc spawn production.

Chưa cân bằng một run đủ 17 phút, chưa kiểm tra standalone build/DPI. Không thêm audio mới. Cảnh báo spawn pools cũ được giữ nguyên, không sửa ngoài phạm vi boss.
