# HUD và nâng cấp khi lên cấp

Status: Frozen
Approval: Approved by user ("ok"), 2026-10-21

## Bối cảnh
SampleScene có PlayerStats, EXP pickup và tăng level, nhưng chưa có HUD hay phần thưởng. Nguồn: Assets/Scripts/Player/PlayerStats.cs. Bộ Kenney Fantasy UI Borders có tại thư mục PNG do người dùng cung cấp (Default và Double).

## Mục tiêu và luồng sử dụng đề xuất
Di chuyển/chiến đấu → nhặt EXP → đủ EXP → tạm dừng gameplay và chọn nâng cấp → tiếp tục. HUD phản ánh máu, EXP và level hiện tại.

## Yêu cầu đã duyệt
| ID | Yêu cầu |
| --- | --- |
| FR-001 | HUD hiển thị HP hiện tại/tối đa, EXP hiện tại/ngưỡng và level; co giãn theo màn hình. |
| FR-002 | Dùng khung Kenney fantasy, máu đỏ và EXP xanh; chữ giao diện tiếng Anh để tránh lỗi font thiếu ký tự. |
| FR-003 | Mỗi lần lên cấp hiển thị ba lựa chọn khác nhau, ngẫu nhiên từ tập nâng cấp. Gameplay và tấn công chuột dừng khi bảng mở. |
| FR-004 | Tập nâng cấp đề xuất: sát thương +10%, tốc độ di chuyển +10%, bán kính hút +20%, máu tối đa +20 và hồi ngay 20 HP. Có thể chọn lại ở các cấp sau. |
| FR-005 | Mỗi level cấp đúng một lượt chọn; EXP dư được giữ, nhiều level trong một lần nhận EXP được xếp hàng. |
| FR-006 | Chỉ số nâng cấp tồn tại trong lượt chơi, không thay đổi ScriptableObject nguồn. |

## Phạm vi
HUD, bảng nâng cấp, kết nối với PlayerStats và cả hai loại vũ khí, tích hợp SampleScene, kiểm tra hồi máu/EXP/combat không hồi quy.

## Ngoài phạm vi
Game Over, menu chính, save/load, inventory, vũ khí mới, thay đổi spawn và đường cong EXP.

## Quyết định và giả định đã duyệt
- D-001: Người dùng duyệt FR-001–FR-006 và điều kiện nghiệm thu.
- ASSUMPTION: Dùng uGUI hiện có; chỉ sao chép asset Kenney cần dùng, giữ nguyên thư mục nguồn.
- ASSUMPTION: Phần trăm tăng tốc/hút/sát thương cộng dồn theo chỉ số gốc mỗi lượt chọn.

## Nhật ký
- Ban đầu: người dùng yêu cầu dựng UI/HUD và hệ thống level-up.
- 2026-10-21: người dùng trả lời "ok", duyệt đặc tả, phạm vi và điều kiện nghiệm thu.
