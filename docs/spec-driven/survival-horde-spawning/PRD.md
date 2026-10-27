# Spawn sinh tồn theo thời gian và Horde hai lớp

Status: Frozen
Approval: User "duyệt", 2026-10-22 (preset and safety fallback included)

## Bối cảnh
EnemySpawner hiện dùng một timer, chọn đều prefab, vòng tròn 10–14 đơn vị; scene cấu hình một quái/5s, tối đa100. Có năm loại quái, Enemy/EnemyStats/EnemyAnimation, Game Over và Restart hiện hoạt động. Game Over regression đọc field spawnTimer; ReferenceEnemyRegression đang cấu hình spawner qua SerializedObject — cần migration tests thay vì bỏ kiểm tra.

## Luồng yêu cầu
Spawn thường chạy độc lập theo thời gian sống sót. Horde: CheckingLayer1 → WaitingLayer2 → Spawning → WaitingForClear → quay lại CheckingLayer1. Không spawn ngay khi lớp1 thành công; chỉ một đoàn hoạt động, phải diệt hết đoàn mới được kiểm tra lớp1 lại. Lớp2 có kiểm tra định kỳ, tăng xác suất sau thất bại và thời hạn ép kích hoạt.

## Yêu cầu
| ID | Nội dung |
| --- | --- |
| FR-001 | Spawn thường ngoài màn hình, tăng cường độ theo thời gian, mở loại quái theo thời điểm và trọng số riêng. Thông số progression chỉnh Inspector. |
| FR-002 | Lớp1: kiểm tra mỗi khoảng thời gian cấu hình; thành công chỉ chuyển sang chờ lớp2, không spawn ngay. |
| FR-003 | Lớp2: lần đầu kiểm tra sau interval; dùng xác suất ban đầu, sau mỗi thất bại cộng thêm điểm phần trăm, clamp100%. |
| FR-004 | Chạm max wait thì kích hoạt Horde không phụ thuộc lần quay xác suất tiếp theo; quy tắc thiếu chỗ/vị trí hợp lệ cần chốt. |
| FR-005 | Tất cả timer, xác suất, min/max số lượng, prefab, phạm vi/vị trí và ngân sách tạo quái có SerializeField/Header/Range/Min hoặc ScriptableObject phù hợp; có validate runtime/Inspector. |
| FR-006 | Một Horde pending/spawning/alive tại một thời điểm; chỉ sau khi diệt hết đoàn mới bắt đầu lại timer lớp1. Không reset hoặc dừng lịch spawn thường khi Horde hoạt động. |
| FR-007 | Spawn hợp lệ: ngoài camera, không quá gần người chơi, tránh vật cản được cấu hình, trong khu vực nền được cấu hình; không vòng lặp thử vô hạn. |
| FR-008 | Dàn việc tạo đoàn qua nhiều frame bằng ngân sách cấu hình; không thêm pooling không tương thích với Enemy.Destroy/Die hiện có. |
| FR-009 | Debug Log tùy bật/tắt: trạng thái, runtime elapsed, roll/chance, tăng xác suất, deadline, kết quả placement. Không log mỗi frame. |
| FR-010 | Level-up pause dừng cả hai đồng hồ; Game Over dừng spawn, quái giữ Idle; Restart reset thời gian, xác suất và trạng thái. |

## Các quyết định còn mở
- D-001: Đã chọn cụm một phía ngoài màn hình, có khoảng cách giữa quái.
- D-002: Đã chọn hai cap độc lập; dự trữ đủ slot cho đoàn trước khi lớp1 chuyển sang chờ.
- D-003: Đã chọn sau khi diệt hết đoàn mới bắt đầu lại lớp1 (thay thế mô tả ban đầu quay lại ngay khi tạo xong).
- D-004: Preset/timeline bên dưới đã được duyệt, chỉnh được Inspector.

## Preset đã duyệt
- Lớp1: mỗi20s, xác suất25%; lớp2: mỗi5s, ban đầu10%, tăng15 điểm phần trăm/lần thất bại, deadline30s.
- Horde:10–20 quái, cap riêng30, tạo tối đa4 quái/frame; prefab/trọng số riêng dùng enemy hiện có.
- Spawn thường: cap100; 0s:1 quái/2.5s; 30s:1/2s và mở Skeleton; 60s:2/1.5s và mở Spider; 120s:3/1s và mở Wolf. Ban đầu dùng hai loại Slime. Đây là preset chỉnh được, không hardcode.
- D-005, đã duyệt: deadline ép bắt đầu triển khai Horde, nhưng không bỏ qua kiểm tra an toàn. Nếu không tìm được chỗ hợp lệ sau ngân sách thử cấu hình, log cảnh báo, bỏ các thành viên chưa đặt được; không spawn trong tường/trên người chơi. Nếu không đặt được con nào, ghi nhận thất bại và bắt đầu lại timer lớp1. Không lặp thử vô hạn hoặc thay đổi scene để ép có chỗ.

## Không thuộc phạm vi
Vũ khí/AI mới, boss mới, UI cảnh báo Horde, auto-save, thay đổi chỉ số gốc Enemy/Character, pooling toàn bộ enemy, sửa dữ liệu người dùng không liên quan.
