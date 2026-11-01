# Map tuyết độc lập

- **Thay thế thiết kế vùng Bắc Y=50:** cỏ và tuyết nay là hai scene độc lập. Không còn ranh giới hay tự đổi địa hình theo tọa độ.
- Chọn **nhân vật → vũ khí → màn chọn map riêng**. Bấm **GRASSLANDS / SNOWFIELDS** để bắt đầu; Back/Esc trên màn map quay về vũ khí. Xem `docs/run-menus.md`.
- Cỏ: `Assets/Scenes/SampleScene.unity`. Tuyết: `Assets/Scenes/SnowScene.unity`.
- SnowScene dùng ba prefab `Snow Chunk`, `Snow Chunk 2`, `Snow Chunk 3` trong `Assets/Prefab/Chunk/`, tại mọi vị trí. Tile, vị trí prop và collider do người dùng tạo được giữ nguyên.
- Mỗi snow chunk dùng đúng năm prefab trong `Assets/Prefab/Props/Snow/`: Bonfire_0, IceTileset_0, IceTileset_2, IceTileset_3, IceTileset_10. Không dùng prop cỏ.
- Pool quái tuyết: IceSlime (weight 2), SnowWolf (1), Snowman (1), dùng cho các phase và Horde trong SnowScene. Không thay enemy data/AI/prefab.

## Spawn

Mỗi scene cấu hình `MapController.terrainChunks` và `EnemySpawner.phases` / `horde.enemies` riêng, dùng lại hệ thống streaming/spawn hiện có. Không cần runtime biome selector, không dùng `snowStartRow`, `snowChunks`, `snowEnemies` hoặc `biomeMap` nữa.

Nhịp độ phase, giới hạn quái, kiểm tra vị trí an toàn và trạng thái Horde giữ cấu hình hiện có. Rotwood chỉ bật trong scene cỏ; chưa có boss/victory encounter riêng cho tuyết. Restart tải lại chính scene đang chơi và giữ nhân vật/vũ khí; Main Menu cho bắt đầu lượt mới. Chọn map không lưu xuống đĩa.

## Tương thích

- Giữ các bản sửa đồng bộ collider và tái sử dụng chunk đã cull từ lần tích hợp trước, để không mất/trùng chunk.
- SnowScene được sao chép từ gameplay hiện có để giữ camera, Player, GameplayLoadout, HUD, Game Over/Restart và các tham chiếu scene. Chỉ đổi chunk/quái và tắt encounter boss rừng trong bản sao.
- Giữ nguyên prefab, texture, stat assets; không khôi phục tài nguyên người dùng đã xóa. Backup scene disk và scene live chưa lưu nằm trong `Captures/MapSelection/Before/`.

## Kiểm tra

- `Tools/System Leveling/Create Separate Maps and Selection`: migration một lần, không ghi đè SnowScene đã tồn tại.
- `Validate Separate Maps`: chunk/prop/quái riêng, gameplay references, Build Settings.
- `Run Map Selection Regression (Play Mode)`: bấm nút UI thật, chọn map/nhân vật/vũ khí, chống tải trùng, normal/Horde đúng scene, streaming xa cả Bắc/Nam/quay lại, props, Game Over/Restart và Main Menu.
- Bằng chứng mới nằm trong `Captures/MapSelection/`. `Captures/SnowBiome/` chỉ lưu lịch sử thiết kế vùng Bắc đã được thay thế, không còn là kiểm chứng cho cấu hình hiện tại.
- Map selection runtime: **1.031 checks PASS**, gồm tải/Restart cả hai scene, loadout, quái thường/Horde và streaming Bắc/Nam/quay lại. Ảnh thật: `selection-snow.png`, `grass-start.png`, `snow-start.png`.
- Loadout gameplay cũ: **229 checks PASS**, đủ 16 tổ hợp nhân vật/vũ khí. UI 16:9/4:3 và raycast/navigation: **3 checks PASS**; đã xem ảnh ở cả hai tỷ lệ.
- Compile cuối `5683107c`: 0 errors. Hash 128 file grass chunk/prop và enemy prefab/data vẫn giữ nguyên (`preservation.json`).
- Survival/Horde regression: **140 checks PASS** khi chạy từ gameplay scene như contract của test. Lần gọi nhầm từ màn chọn đã được lưu ở `survival-wrong-entry.json`; không đổi production code để chữa fixture. Tổng bốn suite: **1.403 checks PASS**. Editor kết thúc tại LoadoutSelection, Edit Mode, scene đã lưu.

Chưa thêm thời tiết, boss tuyết, lưu lựa chọn map giữa các lần mở game hoặc cân bằng chơi lâu. Hai scene có thể chỉnh sửa độc lập trong Editor.
