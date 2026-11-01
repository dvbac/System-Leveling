# Snowman — quái thường map tuyết

Status: complete — 43 asset checks + 13 real Play Mode checks PASS (2026-11-01).

## Validation loop
- Asset/Animator validation passed 43 checks, compile8879df02 succeeded.
- Runtime loop001 passed stats/follow/Move, then its Idle fixture failed after assuming two frames of disabled AI were enough. Kept `Captures/Snowman/runtime-loop001-failed.json`. Loop002 explicitly halted physics and waited for the actual Animator state, but exposed a separate isolation issue: the existing play session was already Game Over (live read-back IsGameOver=true), and the game's OnDisable correctly keeps that gameplay gate. Kept `runtime-loop002-failed.json`.
- Loop003 saves/temporarily clears/restores the GameOver singleton only inside the editor test, so prior gameplay cannot freeze the Snowman fixture. No enemy/GameOver production logic was changed. The stopped case still explicitly halts physics and observes the real state rather than assuming frame order.
- Loop003 PASS 13 checks: initialization, follow/Move, stopped/Idle, actual 2D contact damage eight with i-frames, damage/Hit, lethal damage, movement/physics/collider shutdown, one loot drop despite repeated death, Die animation with stationary world transform, destruction after .8s. Failed loops remain archived.
- Final compile job1278af61 completed with 0 errors. Summary: `Captures/Snowman/summary.json`; source preservation: 22 original files unchanged, including the input PNG/importer and Skeleton template assets. Editor returned to stopped mode; no scene save or spawn-pool modification performed.

## Bằng chứng / preview
`Captures/Snowman/asset-validation.json`, `runtime-validation.json`, `preservation.json`, `animation-preview.png`. The preview was visually checked and opened in Review. Animation clips contain no position/rotation/scale curves; only sprite frames change.

## Assets / thông số ban đầu
- Prefab: `Assets/Prefab/ReferenceEnemies/Snow/Snowman.prefab`.
- Data riêng: `Assets/Scriptable Object/Enemy/Snow/Snowman.asset`.
- HP **45**, tốc độ **1.25**, sát thương tiếp xúc **8** — chậm/trâu hơn Skeleton; các số chỉnh trên EnemyData.
- Capsule thân cao .9, rộng .5, offset y=.45; collider không bao gồm toàn bộ cành tay. PPU48, frame64, pivot bàn chân (32,10).
- Dùng Enemy/EnemyStats/EnemyAnimation và cấu hình loot EXP của Skeleton; không có script AI mới hoặc cơ chế đóng băng.

## Animation
Ảnh nguồn do người dùng cung cấp: `Assets/MyAssets/Monster/SnowVariants/Snowman/Snowman.png`, giữ nguyên PNG và importer.

Các sheet và clip mới ở thư mục con `Generated/`: Idle nhún/lắc nhẹ; Move lắc/nhún kiểu bước trượt tuyết; Hit giật/chớp sáng; Die xẹp/tan và vụn tuyết. Đây là chuyển động sinh từ **một ảnh tĩnh**, không phải sprite animation vẽ tay mới. Chỉ đổi SpriteRenderer.m_Sprite, không animate Transform của Enemy nên không giằng co với AI hoặc kéo xác quái.

Controller override giữ hợp đồng Idle/Move/Hit/Die và thời gian hit/death đang dùng. Prefab độc lập được clone/unpack từ Skeleton; có EnemyData riêng nên sửa HP Snowman không làm đổi Skeleton.

## Tools / kiểm tra
- `Tools/System Leveling/Build Snowman Enemy`: tạo asset chưa có, giữ chỉnh sửa của người dùng khi chạy lại.
- `Validate Snowman Enemy`: stats/collider/loot, frame bindings/pivot/PPU, loops và Animator thật trong Preview Scene.
- `Run Snowman Regression (Play Mode)`: tạm cô lập scene trong phiên test; kiểm tra đuổi player, animation Move/Idle/Hit/Die, va chạm gây damage/i-frame, chết/dừng physics/drop một lần/xóa sau animation; khôi phục scene roots sau test. Không lưu scene.

Không tự thêm vào spawn pool/map hoặc đặt thêm actor vào scene. Chưa thêm ném cầu tuyết, slow, skill/boss hoặc cân bằng encounter dài.
