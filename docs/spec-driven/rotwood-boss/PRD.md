# Rotwood — Boss đầu tiên

Status: Frozen. Approval: User approved “Duyệt, triển khai” on 2026-11-01. Owner: Main agent.

## Bối cảnh và quyết định
Người chơi muốn triển khai boss từ sprite/audio đã có trong `Assets/MyAssets/Monster/Boss/Rotwood`.
Đã xác nhận: boss xuất hiện ở phút 15 và hạ boss sẽ thắng màn.

## Phạm vi đề xuất
- FR-001: Trong SampleScene, tự spawn một Rotwood sau 1020 giây (17 phút) gameplay thực chạy; pause/level-up/game-over không tăng giờ. Có menu Editor thử boss ngay.
- FR-002: Giữ ảnh/audio gốc; dùng animation Walk, Root-Cast, Charge-Windupt, Charge-Loop, Charge-Stagger, Heart-Exposed, Death với âm thanh tương ứng. UI thanh máu có tên boss.
- FR-003: Rễ Trói: ba dấu báo 0,8s rồi trồi rễ lần lượt ở các vị trí đã khóa; gây sát thương và làm chậm ngắn, không khóa điều khiển.
- FR-004: Húc: báo hướng 1s, lao theo hướng đã khóa, dừng khi gặp vật cản hoặc hết tầm; hồi phục 1,5s. Không xuyên vật cản.
- FR-005: Gọi 2 Sói với vị trí báo trước; tối đa 4 sói của boss cùng sống.
- FR-006: Dưới 50% máu, sau húc lõi mở 2s và boss nhận thêm 50% sát thương. Không chồng nhiều đòn chủ động.
- FR-007: Hạ boss phát Death, vô hiệu sát thương/collider, dọn rễ/sói triệu hồi; sau Death hiện YOU WIN với Restart/Main Menu. Không chết/rơi thưởng hai lần.
- FR-008: Hỗ trợ cả melee/projectile hiện tại, pause, level-up và game-over; không đổi pool/config spawn thường hoặc dữ liệu nhân vật.

## Giá trị khởi đầu, được chỉnh ở Inspector
HP 1800; tốc độ đi 1,2; chạm 15; rễ 18; húc 25. Các giá trị này không phải kết quả cân bằng cuối.

## Ngoài phạm vi
Không tạo art AI mới, map/arena mới, hệ thống save, loot mới, boss khác hoặc thay UI đang có.
Giới hạn: không có sprite triệu hồi riêng; dùng Root-Cast cho tư thế gọi sói. Không có arena cố định: húc kết thúc khi hết tầm thay cho bắt buộc đâm tường.

## Thành công
Một boss đúng mốc 1020s, chơi được đủ bốn cơ chế, chiến thắng chỉ khi boss chết và không làm hỏng combat/spawner cũ.
