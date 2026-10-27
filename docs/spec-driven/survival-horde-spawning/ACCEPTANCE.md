# Điều kiện nghiệm thu

Status: Frozen
Approval: User "duyệt", 2026-10-22

Tất cả AC là blocking. Test phải gọi logic thật và tích hợp Play Mode; không chỉ kiểm tra bản mô phỏng độc lập.

| AC | FR | Kết quả quan sát được | Bằng chứng |
| --- | --- | --- | --- |
| AC-001 | FR001,005 | Các mốc thời gian chọn đúng interval/batch/pool; không spawn loại chưa mở; trọng số0/null không được chọn. Mọi preset sửa Inspector được. | Unit/regression và Inspector read-back |
| AC-002 | FR002,003 | Lớp1 thành công chưa tạo quái; lớp2 chỉ kiểm tra sau interval; chuỗi thất bại dùng 10%,25%,40%... và clamp100%. | Random/time xác định chạy state machine thật |
| AC-003 | FR004 | Toàn bộ roll thất bại vẫn triển khai khi maxWait, kể cả maxWait ngắn hơn interval lớp2. Không bỏ safety checks theo D005. | Regression và Play Mode |
| AC-004 | FR006 | Không có hai đoàn pending/spawning/alive; sau khi tạo hết vẫn không chạy lớp1 cho đến khi diệt/despawn hết đoàn; timer lớp1 bắt đầu mới sau clear. | Regression/population count |
| AC-005 | FR006,008 | Normal vẫn spawn trong WaitingLayer2/Spawning/WaitingForClear; không vượt cap riêng hoặc budget/frame; không tạo đột biến bù toàn bộ tick sau frame dài. | Runtime counters/frame checks |
| AC-006 | FR007,008 | Spawn ngoài camera, không đè player/obstacle hoặc ngoài ground cấu hình; có separation; vị trí bất khả thi kết thúc thử hữu hạn và log kết quả thay vì treo game. | Geometry tests và Play Mode |
| AC-007 | FR005,009 | Giá trị cấu hình sai được validate/clamp hoặc báo rõ; log đúng transition, chance/deadline và deployment result, có toggle tắt. | Inspector/script tests, Console |
| AC-008 | FR010 | Level-up dừng tất cả timer; Game Over không spawn thêm, quái Idle tiếp tục; Restart reset state/thời gian/chance/population. | Existing HUD/GameOver regression đã migrate và tích hợp mới |
| AC-009 | Tất cả | Compile không lỗi mới; năm loại Enemy hiện có vẫn spawn và chết/loot/animation như trước. | Compile, ReferenceEnemyRegression và smoke test |

Ngoài phạm vi: UI báo Horde, boss/AI mới, pooling toàn bộ game. Definition of Done: từng AC có bằng chứng, chỉ báo hoàn tất khi không còn AC blocking thiếu kiểm chứng.
