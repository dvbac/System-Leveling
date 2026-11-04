# Âm thanh trong game

`Assets/Resources/GameAudio.asset` tham chiếu trực tiếp các WAV gốc trong `Assets/MyAssets/Sound/`. Không sao chép hoặc sửa dữ liệu WAV. Import nhạc dạng Streaming/Vorbis; SFX dạng DecompressOnLoad/PCM.

## Nhạc nền

| Màn hình | Bài nhạc |
| --- | --- |
| MainMenu, LoadoutSelection | Awakening the Ancient System |
| SampleScene / cỏ | Cursed Wilderness |
| SnowScene / tuyết | Frozen Moonlight |

Nhạc lặp, không khởi động lại khi chuyển menu → chọn nhân vật/vũ khí. Nguồn nhạc tồn tại qua scene. Listener dự phòng phục vụ menu và tự tắt nếu scene có listener hoạt động. Không thay âm thanh riêng của Rotwood.

## SFX

Các vai trò được gán theo tên file hiện có; tên bị cắt ngắn nên cần nghe thử để xác nhận chất âm.

| Sự kiện | WAV trong `Sound/sfx/` |
| --- | --- |
| Preview nhân vật/vũ khí | `A_subtle_dark_fantas_#1-1793594188708.wav` |
| Click/menu/chọn loadout/map | `A_single_premium_dar_#4-1793594238489.wav` |
| Nhặt EXP | `A_tiny_magical_exper_#4-1793594279337.wav` |
| Lên cấp | `A_dark_fantasy_level_#4-1793594310731.wav` |
| Nhân vật nhận sát thương | `A_single_compact_pla_#2-1793594348898.wav` |
| Sách bắn phép | `A_single_dark_magic__#1-1793594424465.wav` |
| Phép trúng địch/vật thể | `A_single_magical_pro_#3-1793594471636.wav` |
| Chọn nâng cấp thành công | `A_magical_upgrade_ca_#1-1793594510152.wav` |
| Vung kiếm | `A_single_fast_sword__#4-1793596044689.wav` |
| Bắn cung | `A_single_wooden_bow__#2-1793596120706.wav` |
| Giữ sạc nỏ (loop) | `A_seamless_looping_m_#3-1793596507434.wav` |
| Reload/hồi tên — đã tắt phát trong gameplay | `One_single_clean,_br_#3-1793596609017.wav` |
| Nhả nỏ | `A_single_medieval_cr_#4-1793596642117.wav` |
| Kiếm trúng địch/vật thể | `A_single_universal_a_#2-1793596830142.wav` |
| Tên cung/nỏ trúng địch/vật thể | `A_single_subtle_arro_#2-1793597011034.wav` |

## Phát và điều chỉnh

- Reload sách/nỏ và hồi tên cung không phát SFX. Giữ file và slot audio cũ để không đổi ID/mapping đã serialize; tiếng bắn và loop giữ sạc nỏ không đổi.
- Kiểm tra thay đổi tắt SFX reload: **139 kiểm tra vũ khí PASS** trong Play Mode, gồm xác nhận audio service vẫn hoạt động nhưng magazine reload, crossbow reload và bow recovery không gán/phát clip reload.

- Nhạc 0.28, giảm về 0.12 khi pause, chọn nâng cấp hoặc kết thúc trận; nội suy theo thời gian không bị scale. SFX thường 0.55, hover/EXP 0.22, sạc nỏ 0.3.
- Thanh Volume cũ (`AudioListener.volume`) điều khiển tất cả, gồm cả boss. Không thêm thiết lập hoặc thay cách lưu volume.
- Tối đa 12 voice SFX dùng lại theo vòng, cộng một nguồn sạc trên vũ khí đang dùng. Các đợt EXP/impact cùng loại cách nhau ít nhất 70 ms; SFX khác 40 ms. Không dùng random pitch, không tác động RNG chiến đấu.
- Một tiếng bắn cho mỗi đợt bắn, không nhân theo số projectile của Double Shot. Impact đặt sau bước chống trùng collider. Tiếng đau chỉ phát khi thực sự nhận damage; pickup chỉ phát một lần.
- Sạc nỏ dừng khi nhả, pause, con trỏ vào UI, game over hoặc disable vũ khí. Gameplay SFX dừng khi pause/kết thúc trận; UI/lên cấp/chọn nâng cấp vẫn nghe được khi `timeScale = 0`.
- Không thêm nhạc thắng/thua, tiếng chết hoặc tiếng boss mới vì không có file riêng xác định cho các vai trò đó.

## Thiết lập và kiểm tra

- `Tools > System Leveling > Setup Game Audio`: tạo/cập nhật bank và import settings, không lưu scene. Inspector của bank cho phép đổi mapping.
- `Tools > System Leveling > Validate Game Audio`: kiểm tra Edit Mode trong preview scene riêng; kết quả `Captures/GameAudio/validation.json` (86 kiểm tra, gồm bộ kiểm tra scheduler vũ khí hiện có).
- `Captures/GameAudio/source-before.json` và `verification.json`: đối chiếu SHA-256 của 18 WAV; trạng thái scene sau thao tác.
- Chưa chạy regression tải scene trong Play Mode để tránh ảnh hưởng scene `LoadoutSelection` đang có thay đổi chưa lưu. Chưa kiểm tra bản build, thiết bị âm thanh hoặc nghe cân âm/chất lượng nối vòng nhạc. Kiểm tra Edit Mode không chứng minh đã nghe phát âm thanh thực tế.
