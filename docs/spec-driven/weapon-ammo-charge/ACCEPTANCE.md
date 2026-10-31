# Nghiệm thu vũ khí
Status: Frozen
Approval: User "chốt", 2026-10-29, theo hành vi/preset và kiểm tra an toàn đã trình bày.

Tất cả AC blocking; bằng chứng phải gọi code production, không mô phỏng lại logic.

| AC | FR | Hành vi đo được / bằng chứng |
| --- | --- | --- |
| AC001 | 001 | Sách bắt đầu 8, giữ bắn đúng cooldown, cạn thì đợi 1.8s mới đầy; test scheduler và projectile thật. |
| AC002 | 002 | Cung 7, interval .25, giữ không hồi kể cả cạn; nhả .45+.65s mới có tên đầu; hủy nạp không nhân đôi tên. |
| AC003 | 003 | Nỏ luôn 1 tên, thả sớm/3s/quá trần cho damage/scale đúng; chưa nhả không tự bắn; thực tế collider tăng cùng sprite. |
| AC004 | 004–005 | Ammo +1 nâng cap sách/cung, không tặng đạn; nỏ giữ 1 tên, tăng trần lực/thời gian; Reload +20% rút cả reload/charge. |
| AC005 | 006 | Prefab và scene mỗi player chỉ có một active weapon đúng lớp; sprite và projectile đúng, kiếm còn click attack; bảo toàn user edits. |
| AC006 | 007 | UI/pause không bắn, timer đóng băng, charge hủy và release-gate; Game Over dừng; reload scene reset ammo/cấp. |
| AC007 | 008 | Validate giá trị xấu, frame dài không burst, thiếu prefab không mất đạn; projectile dùng data/owner snapshot, mỗi mục tiêu nhận một hit. |
| AC008 | 009 | HUD ammo/reload/charge cập nhật; upgrade offer hợp lệ, chọn áp dụng đúng, không bắn xuyên click UI. |
| AC009 | Tất cả | Compile, regression HUD/GameOver/spawn/Enemy và ranged integration; báo rõ thiếu bằng chứng, không tự nhận hoàn tất. |

Không bao gồm inventory, đạn dự trữ/pickup, nhân vật/animation mới hoặc cân bằng trận dài. Done khi các AC có test/evidence, Inspector chỉnh được, không sửa asset gốc ngoài migration đã duyệt.
