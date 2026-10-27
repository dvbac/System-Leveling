# HUD và level-up — hướng dẫn

## Chạy
Mở `Assets/Scenes/SampleScene.unity`, bấm Play. HUD hiển thị HP, EXP và level. Khi đủ EXP, chọn một trong ba nâng cấp bằng chuột hoặc điều hướng UI/Submit của Input System. Mỗi level nhận một lượt; game chỉ tiếp tục khi đã chọn hết.

Nâng cấp cộng dồn theo chỉ số gốc: damage +10%, move speed +10%, magnet +20%; Vitality tăng max HP runtime thêm 20 và hồi 20. Các ScriptableObject gốc không bị thay đổi.

## File chính
- `Assets/Scripts/Player/PlayerStats.cs`: stats runtime, sự kiện Changed, EXP và lượt nâng cấp.
- `Assets/Scripts/UI/PlayerHud.cs`: thanh và nhãn HUD, chỉ tạo lại chuỗi khi số hiển thị đổi.
- `Assets/Scripts/UI/LevelUpSelection.cs`: ba lựa chọn khác nhau, hàng đợi, pause và chặn click xuyên.
- `Assets/Prefab/UI/System HUD.prefab`: bố cục tái sử dụng; reference PlayerStats được gắn bằng override trong scene.
- `Assets/Scripts/Editor/HudSceneBuilder.cs`: menu `Tools > System Leveling > Build HUD`; nếu đã có HUD thì giữ nguyên thay vì tạo bản trùng/ghi đè bố cục.
- `Assets/MyAssets/UI/Kenney`: border gốc Kenney + giấy phép; `bar-fill.png` là ảnh trắng 8×8 tự tạo cho thanh phẳng.

## Chạy lại kiểm tra
1. Mở SampleScene, bấm Play.
2. Chọn `Tools > System Leveling > Run HUD Regression (Play Mode)`.
3. Xem `Captures/HudLevelUp/regression.json` và Console (`HUD REGRESSION PASS`).

Harness được bọc `UNITY_EDITOR`, không đi vào player build. Nó tạo fixture tạm, kiểm tra code gameplay thật, xóa fixture và phục hồi UI/timeScale/runInBackground. Không phải bộ NUnit/EditMode Test Runner; không có tái cấu trúc assembly dự án.

## Kết quả
42 kiểm tra qua trên Unity 6000.4.12f1. Ảnh: `Captures/HudLevelUp/hud-16x9.png`, `level-up-16x9-1.png`, `level-up-4x3.png`. Console cuối không có error/warning. Scene đã lưu, Play Mode đã dừng, timeScale=1.

## Hạn chế ngoài phạm vi
- Prefab `Assets/Prefab/red health potion.prefab` vốn thiếu HealthPotion; bình đang đặt trong scene được cấu hình riêng và đã qua kiểm tra pickup/hồi máu. Chưa sửa prefab đó trong tính năng này.
- Chưa có Game Over, lưu nâng cấp qua lượt chơi, menu chính hoặc thay đổi nhịp spawn.
- Giữ nguyên cách xử lý khi vượt LevelRange cuối: cap không tăng thêm nếu không có range khớp.
