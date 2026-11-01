# Quái biến thể map tuyết

Status: complete — Unity build/validation PASS, 2026-11-01.

## Assets
- `Assets/Prefab/ReferenceEnemies/Snow/SnowWolf.prefab`: sói lông bạc/trắng, bóng xanh lạnh, viền xanh đậm và điểm sáng băng.
- `Assets/Prefab/ReferenceEnemies/Snow/IceSlime.prefab`: slime xanh băng nhạt, phản sáng trắng, lõi xanh đậm để dễ đọc trên nền tuyết.
- Sprite sheets, animation clips và AnimatorOverrideController riêng: `Assets/MyAssets/Monster/SnowVariants/{SnowWolf,IceSlime}/`.

Hai ảnh yêu cầu tương ứng prefab gốc Wolf và Slime đang có. Biến thể là prefab variant thật, kế thừa AI, EnemyStats data, collider, flip, loot và EnemyAnimation từ bản gốc. Chỉ override sprite ban đầu và Animator controller; không thêm runtime script, trạng thái slow/freeze hoặc tăng chỉ số.

Idle/Move/Hit/Die đều có skin tuyết, giữ nguyên frame count, timing, loop, sprite rect, pivot, PPU và alpha. AnimatorOverrideController dùng lại state machine của bản gốc, nên không nhảy về màu cũ khi chạy/trúng đòn/chết.

## Tạo / kiểm tra
Menu `Tools/System Leveling/Build Snow Enemy Variants` chỉ tạo các asset chưa tồn tại, giữ chỉnh sửa của người dùng khi chạy lại. Menu `Validate Snow Enemy Variants` kiểm prefab inheritance, stats/AI/loot/collider, toàn bộ sprite geometry, timing/keyframe bindings, alpha và thực tế Animator đổi frame ở bốn trạng thái trong Preview Scene riêng.

Không sửa scene hiện tại hoặc tự gắn vào spawn pool. Để dùng trong map tuyết, tham chiếu hai prefab mới ở pool/biome thích hợp khi cấu hình spawning cho map. Đổi chỉ số bản gốc cũng sẽ ảnh hưởng variant vì dùng cùng EnemyData.

## Bằng chứng
`Captures/SnowEnemies/validation.json`: **102 checks PASS**, bao gồm Animator thực sự đổi sprite ở mọi trạng thái. Compile job `9cbd29dc` completed, 0 C# errors. Có 8 sprite sheets, 8 clips, 2 override controllers và 2 prefab variants, tổng 56 sprite frames.

`Captures/SnowEnemies/preservation.json`: **40 file nguồn không thay đổi hash**, gồm sprite/importer/clip/controller/prefab/data gốc. Ảnh so sánh đã kiểm: `Captures/SnowEnemies/preview.png` (trái gốc, phải tuyết).

Không chạy lại toàn bộ gameplay vì không sửa logic runtime; chưa kiểm tra encounter/balance trong map tuyết. Chưa thay spawn pool và không lưu hay sửa scene hiện tại. Các biến thể dùng chung EnemyData với parent; muốn chỉ buff bản tuyết thì cần gán EnemyData riêng, không sửa nhầm data dùng chung.
