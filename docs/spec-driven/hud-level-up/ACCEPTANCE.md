# Điều kiện nghiệm thu

Status: Frozen
Approval: Approved by user ("ok"), 2026-10-21

Tất cả tiêu chí dưới đây là blocking, cần bằng chứng trước khi báo hoàn tất.

| ID | FR | Kịch bản và kết quả bắt buộc | Bằng chứng |
| --- | --- | --- | --- |
| AC-001 | FR-001, FR-002 | Play SampleScene: HUD đúng HP/EXP/level, khung Kenney không che nhân vật ở trung tâm, không tràn ở 16:9 và 4:3. | Ảnh/chạy kiểm tra layout |
| AC-002 | FR-001 | Nhận sát thương, hồi máu, nhặt EXP: số và thanh phản ánh trạng thái mới, fill trong [0,1]. | Play Mode hoặc test tự động |
| AC-003 | FR-003 | Đủ EXP: đúng ba lựa chọn khác nhau, enemy/player không di chuyển và click bảng không tạo slash. | Play Mode/test |
| AC-004 | FR-004, FR-006 | Mỗi loại nâng cấp thay đổi đúng chỉ số đã công bố; damage áp dụng cả melee/projectile, asset nguồn không đổi. | Test và kiểm tra asset |
| AC-005 | FR-005 | EXP vượt nhiều ngưỡng: giữ EXP dư, mỗi level một lượt, click lặp không cấp hai phần thưởng; resume sau lượt cuối. | Test |
| AC-006 | FR-003 | Đóng/disable điều phối UI trả lại timeScale trước đó, không để game kẹt pause. | Test |
| AC-007 | Tất cả | Compile không lỗi mới; gameplay hiện có vẫn di chuyển, đánh, nhặt EXP và hồi máu được. | Console và smoke test |

Ngoài phạm vi: Game Over, persistence, menu, thay đổi spawn/EXP curve.
Definition of Done: các AC có bằng chứng và mọi hạn chế kiểm thử được báo rõ.
