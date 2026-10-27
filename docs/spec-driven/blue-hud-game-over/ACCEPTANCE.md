# Điều kiện nghiệm thu

Status: Frozen
Approval: User "được rồi", 2026-10-22

Mọi tiêu chí dưới đây là blocking; cần kiểm chứng thực tế trước khi báo hoàn tất.

| ID | FR | Kết quả bắt buộc | Bằng chứng |
| --- | --- | --- | --- |
| AC-001 | FR-001,002,006 | HUD, level-up và Game Over dùng navy/xanh/cyan nhất quán, nhãn không tràn ở 16:9 và 4:3. | Ảnh và kiểm tra bố cục |
| AC-002 | FR-003 | Sát thương chí tử làm HP=0, phát chết một lần, hiện level cuối; hồi máu/regen/damage tiếp không hồi sinh hay mở lặp. | Regression/Play Mode |
| AC-003 | FR-003,004,006 | Khi chết: player/enemy/combat dừng; không chọn nâng cấp; đóng/disable selection không vô tình resume lượt đã chết. | Regression/Play Mode |
| AC-004 | FR-005 | Bấm Restart nhiều lần chỉ tải một lần; scene mới HP đầy, level1/EXP0, bonus reset, không còn đối tượng runtime cũ, timeScale1. Chết và Restart lần hai vẫn hoạt động. | Play Mode có reload thực |
| AC-005 | FR-002,006 | Regression HUD/level-up, pickup, damage và pause vẫn qua; không error/warning mới từ code game. | Harness cũ và Console |
| AC-006 | FR-007 | Chờ lâu hơn spawnInterval sau chết: số quái không tăng; vị trí và vận tốc không đổi; quái sống ở state Idle và normalizedTime tăng. Kiểm tra mọi loại trong spawner, kể cả Blue Slime cũ. | Runtime assertions và ảnh |

Definition of Done: các AC có bằng chứng; giới hạn ngoài phạm vi được ghi rõ. Người dùng đã duyệt triển khai.
