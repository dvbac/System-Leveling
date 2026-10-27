# HUD xanh dương, Game Over và Restart

Status: Frozen
Approval: User "được rồi", 2026-10-22 (including live enemy Idle requirement)

## Bối cảnh
Người dùng yêu cầu thiết kế lại HUD với xanh dương làm màu chủ đạo, thêm Game Over và Restart. HUD và level-up hiện có trong SampleScene. PlayerStats.Kill() hiện chỉ ghi log; LevelUpSelection đang tự quản lý pause.

## Yêu cầu đề xuất
| ID | Hành vi |
| --- | --- |
| FR-001 | Thiết kế lại HUD với nền navy, viền xanh dương/cyan, chữ sáng; HP giữ đỏ để dễ nhận biết, EXP xanh; giữ asset Kenney. |
| FR-002 | Bảng level-up đồng bộ phong cách xanh, giữ nguyên cơ chế nâng cấp đã có. |
| FR-003 | Khi HP về 0: kết thúc lượt đúng một lần, khóa gameplay/combat, hiển thị Game Over và level đạt được. Không tự hồi sinh bởi hồi máu/regen. |
| FR-004 | Game Over ưu tiên cao hơn level-up; không có hai modal chồng nhau hoặc chọn nâng cấp sau khi chết. |
| FR-005 | Restart tải lại scene hiện tại, HP đầy, level 1, EXP 0, xóa bonus và enemy/drop của lượt trước; gameplay chạy lại với timeScale=1. Chống bấm Restart liên tục tạo nhiều lượt tải. |
| FR-006 | UI hoạt động ở 16:9 và 4:3; không click xuyên modal xuống vũ khí. |
| FR-007 | Trong Game Over: ngừng spawn, quái còn sống đứng yên và chuyển sang Idle; hoạt ảnh Idle tiếp tục chạy, không đóng băng theo timeScale=0. |

## Phạm vi và lựa chọn chờ duyệt
- D-001: Người dùng đã duyệt FR-001–007. Game Over hiển thị level cuối và nút RESTART; không thêm timer/kill counter chưa có dữ liệu.
- ASSUMPTION: Giữ tiếng Anh trên UI, tương tự bản trước.
- Ngoài phạm vi: main menu, save/load, hiệu ứng chết/animation mới, sửa loot prefab cũ, thay đổi nhịp spawn.

## Luồng
Chơi → chết → Game Over → Restart → lượt mới. Nhánh level-up giữ nguyên; trạng thái chết không thể bị nhánh này giải phóng pause.
