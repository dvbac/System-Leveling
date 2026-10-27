# Blue HUD, Game Over và Restart

## Sử dụng
Mở `Assets/Scenes/SampleScene.unity`, bấm Play. HUD/level-up dùng navy, xanh dương/cyan và chữ sáng. Thanh HP giữ màu đỏ để dễ nhận biết.

Hết máu sẽ hiện Game Over với level cuối. Nút Restart tải lại scene: đầy HP theo CharacterScriptableObject hiện tại, level 1, EXP 0, không còn bonus hoặc đối tượng runtime lượt cũ. Không thêm menu chính hoặc save/load.

## Quái khi Game Over
- EnemySpawner không đếm tiếp timer hoặc tạo quái.
- Enemy ngừng đuổi và tắt mô phỏng Rigidbody2D, không trượt do lực va chạm.
- Quái sống chuyển Idle, ưu tiên hơn Hit; quái đã chết giữ Die.
- Time.timeScale=1 trong Game Over để Idle tiếp tục chạy. Movement, combat, damage, projectile và EXP pickup được chặn riêng.
- Blue Slime cũ có thêm Idle thở nhẹ bằng sprite hiện có; Move/Hit và các ReferenceEnemies do người dùng thêm được giữ nguyên.

## File chính
- `Assets/Scripts/UI/GameOverController.cs`: sự kiện chết, modal và tải lại scene, chống Restart trùng.
- `Assets/Scripts/Player/PlayerStats.cs`: IsDead/Died, chống chết lặp hoặc hồi sinh bằng regen/pickup/upgrade.
- `Assets/Scripts/UI/LevelUpSelection.cs`: Game Over có ưu tiên cao hơn level-up và chặn input.
- `Assets/Scripts/Enemy/Enemy.cs`, `EnemyAnimation.cs`, `EnemySpawner.cs`: đứng yên, Idle và ngừng spawn.
- `Assets/Scripts/Editor/HudSceneBuilder.cs`: migration palette/layout/Game Over có mục tiêu; menu Build HUD cập nhật prefab và scene hiện có, không xóa scene.

## Kiểm chứng
Vào Play Mode rồi chạy:
1. `Tools > System Leveling > Run HUD Regression (Play Mode)`.
2. `Tools > System Leveling > Run Game Over Regression (Play Mode)`.

Harness Game Over tự tải scene sạch làm baseline, chụp UI ở 1280×720 và 1280×960, kiểm tra năm loại quái sau hơn 5 giây, và sống qua hai lần Restart thật để xác minh reset. Các size Game View tạm và runInBackground được phục hồi. Test code nằm trong UNITY_EDITOR, không đi vào player build.

Kết quả:
- `Captures/HudLevelUp/regression.json`: PASS, 42 kiểm tra, lấy Mage runtime 50 HP theo lựa chọn của người dùng, không ghi asset.
- `Captures/BlueHudGameOver/regression.json`: PASS, 44 kiểm tra, 2 Restart (ngoài lần tải baseline).
- `Captures/BlueHudGameOver/final-editor-state.json`: không có error mới trong lượt kiểm chứng cuối; scene không dirty; Editor đã dừng Play.
- Ảnh `hud-*`, `level-up-*`, `game-over-*` trong `Captures/BlueHudGameOver`.

## Ghi chú dữ liệu hiện có
- Mage Editor đang nạp 50 HP trong khi file trên đĩa lúc kiểm tra ghi 200 HP. Test dùng dữ liệu thực tế theo lựa chọn "Giữ 50 HP cho test"; không ép ghi thay đổi vào asset của người dùng.
- Prefab bình máu hiện đã có HealthPotion; scene vẫn còn component đã thêm trước đó, thành hai HealthPotion trên cùng object. Không tự xóa cấu hình người dùng trong tính năng này. Regression pickup kiểm tra source prefab một component.
