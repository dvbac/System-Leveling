# Rotwood — boss rừng đầu tiên

## Chơi và thử nhanh
- Mở `Assets/Scenes/SampleScene.unity`, Play. Boss xuất hiện **một lần ở 17:00 gameplay (1020 giây)**. Pause/level-up không tính thời gian; mốc 15:00 cũ không còn spawn boss.
- Không cần đợi 17 phút: trong Play Mode chọn **Tools > System Leveling > Rotwood > Spawn Now (Play Mode)**.
- Hạ boss: phát Death rồi hiện **VICTORY / YOU WIN**, dừng combat, cho Restart hoặc Main Menu. Player chết trước thì vẫn là thua.
- Prefab: `Assets/Prefab/Boss/Rotwood.prefab`.
- Encounter trong scene: `Rotwood Boss Encounter - 15 Minutes`; chỉnh `Spawn At Seconds` nếu cần test.

## Cơ chế
1. Rễ: 3 vòng báo tại vị trí người chơi vừa đi qua, báo 0,8s rồi đánh lần lượt, slow 40% trong 1,2s. Không sửa stat tốc độ gốc nên không mất nâng cấp.
2. Húc: khóa hướng khi báo 1s, lao tối đa 8 unit, bị chặn bởi ground/vật cản theo mask. Có 1,5s hồi phục.
3. Gọi bầy: dùng tư thế Root-Cast và vòng báo, gọi 2 Sói; tối đa 4 Sói của boss. Sói bị dọn khi boss chết/kết thúc run.
4. Dưới 50% HP: sau hồi phục húc, lõi mở 2s, nhận thêm 50% sát thương.

HP đầu 1800, đi 1,2; chạm 15, rễ 18, húc 25. Đây là chỉ số khởi đầu, chưa cân bằng qua đủ run 15 phút.

## Assets và setup
- Ảnh/audio gốc trong `Assets/MyAssets/Monster/Boss/Rotwood` được giữ nguyên.
- `Generated/` chứa bản sheet fixed-cell và hiệu ứng rễ/vòng báo tối giản. Playback dùng `RotwoodBoss.Sequence` thay vì Animator để không xung đột với EnemyAnimation của quái thường.
- Tạo lại asset thiếu bằng **Rotwood > Build First Boss**, ngoài Play Mode và đang mở SampleScene. Prefab/data/encounter đã có được giữ để không ghi đè chỉnh tay.
- Ground được tìm qua các chunk có cùng tên Tilemap với ground của spawner. Không đưa boss vào weighted pool/horde; cấu hình spawner giữ nguyên.
- Không có art gọi sói/Idle riêng trong bộ nguồn; dùng Root-Cast cho triệu hồi và Walk cho bám đuổi.

## Kiểm tra
**Rotwood > Run Regression (Play Mode)** chạy lại gameplay thật rồi kiểm tra mốc spawn, pause, 4 cơ chế, collider/vật cản, kiếm/đạn, cleanup, thắng/restart/thua.
Report và ảnh: `Captures/RotwoodBoss/`. Chạy test sẽ restart scene đang chơi; không dùng giữa một run muốn giữ.
Specification, approval, các vòng kiểm tra và acceptance: `docs/spec-driven/rotwood-boss/`.

Kết quả gần nhất: **50 kiểm tra PASS**, gồm không spawn ở 15:00/16:59 và spawn một lần khi qua 17:00. Chưa cân bằng một run đủ 17 phút; mốc thời gian được kiểm tra bằng production Tick. Cấu hình spawner hiện tại có cảnh báo `Spawn entry excluded` ở một số pool cũ, được giữ nguyên chứ không tự sửa ngoài phạm vi boss.
