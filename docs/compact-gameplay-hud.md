# HUD gameplay gọn

Áp dụng cho `SampleScene` và `SnowScene`:
- HP ngắn **240×18** (đơn vị UI tham chiếu), góc trái trên, cách mép 8; số HP nằm trên thanh. Không khung trang trí hay panel lớn.
- Đạn hiện tại / tối đa, LV, Soul nằm dưới HP. Giữ nghĩa Soul của run hiện tại và cảnh báo lỗi lưu. Timer gọn ở góc phải trên.
- EXP là thanh 6 đơn vị chạy sát toàn bộ mép dưới; giá trị EXP nhỏ ở bên phải.
- Cursor dùng asset riêng **`Assets/MyAssets/Other/Cursor/GameCursor.png`**, đúng **16×16**, tâm bấm (8,8), không đệm lên 32×32. Import dạng **Cursor**, RGBA32 không nén, Point, không mipmap; không dùng Sprite Multiple để hiển thị con trỏ. Ảnh nguồn `Cursor.png` được giữ nguyên.
- Khi chơi, số đạn 9 pixel vật lý nằm cạnh chuột, tự giữ trong màn hình; nhỏ hơn cursor. Thanh reload `[---|------------------]` nằm trên đầu nhân vật và theo `ReloadFraction` thật.
- Feedback ẩn khi melee, pause/settings/level-up hoặc kết thúc run. Khi sạc nỏ, cùng thanh `[---|---]` trên đầu nhân vật hiện màu vàng và chạy theo `ChargeFraction`; khi nạp đạn hiện màu trắng theo `ReloadFraction`. Đầy sạc giữ vạch ở cuối cho tới khi thả bắn. Pause hủy sạc và ẩn thanh, không bắn ké khi tiếp tục. UI không chặn click bắn.

## Sửa lại / chạy kiểm tra
- Ngoài Play: **Tools > System Leveling > Apply Compact Gameplay HUD**. Chạy lại không tạo nhãn trùng. Bản sao scene/prefab/config trước sửa ở `Captures/CompactHud/Before`.
- Chỉ đặt lại cursor: **Tools > System Leveling > Reset Game Cursor**; `Refresh Small Cursor` gọi cùng thao tác. Nguồn đã 16×16 thì sao chép nguyên pixel; nguồn 32×32 thì lấy mẫu Point xuống 16×16 đúng một lần. Chạy lại không làm cursor nhỏ tiếp thành 8×8, không đổi layout HUD hay lưu scene.
- **Validate Game Cursor** kiểm tra kích thước, format/import, hotspot và toàn bộ 256 pixel. Bằng chứng: `Captures/CursorReset/validation.json`; có bản sao ảnh nguồn trước sửa. Reset áp dụng `CursorMode.Auto` qua API Cursor, không đổi Windows DPI hoặc zoom Game View.
- **Build HUD** cũng dùng cùng layout compact, tránh trở lại panel lớn.
- Trong Play: **Tools > System Leveling > Run Compact HUD Regression (Play Mode)**. Test tự load lại hai map, đổi tạm tỷ lệ Game View, bắn/nạp/đổi vũ khí/pause/level-up/chết/restart; không thay số Soul đã lưu. Không chạy giữa một run muốn giữ.
- Report: `Captures/CompactHud/runtime-regression.json` — **275 checks PASS**, gồm asset cursor hiện có, thanh sạc nỏ màu vàng, tiến độ nửa/đầy, hủy sạc khi pause, cùng luồng reload cũ. Screenshot sạc/nạp cho rừng/tuyết ở 1920×1080 và 1024×768; bản sao bằng chứng mới ở `Captures/CrossbowChargeTuning/`.

Menu/chọn nhân vật, cơ chế vũ khí, wallet, boss và spawn pools không bị đổi. Chỉ thay cursor mặc định dùng chung toàn game.
Chưa kiểm tra bản build và scale con trỏ theo DPI hệ điều hành. Screenshot Unity không bao gồm hình hardware cursor; kích thước/hotspot được kiểm tra bằng cấu hình và asset thật.

Lần reset cursor hiện tại chỉ được kiểm tra trong Edit Mode để giữ các thay đổi scene chưa lưu. Cần kiểm tra lại cảm nhận độ nét trong Game View sau khi người dùng vào Play; không coi kiểm tra pixel asset là bằng chứng đã xử lý mọi nội suy do DPI.
