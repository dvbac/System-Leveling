# Thiết kế HUD và level-up

Status: Frozen
Approval: Approved by user ("ok"), 2026-10-21

## Hiện trạng
PlayerStats quản lý HP, EXP và LevelRange; tăng cấp hiện chỉ ghi log. CharacterScriptableObject chứa chỉ số gốc. MeleeWeaponBehaviour và ProjectileWeaponBehaviour hiện dùng damage từ WeaponScriptableObject, chưa áp dụng bonus của nhân vật.

## Phương án đề xuất
- uGUI Canvas với HUD và modal nâng cấp; Input System UI module, chặn click xuyên modal xuống melee controller.
- PlayerStats phát thông báo thay đổi và level-up; giữ ngưỡng EXP hiện có, thêm max HP runtime và bonus damage riêng, không ghi asset nguồn.
- Bộ điều phối nâng cấp giữ hàng đợi level chưa chọn, sinh ba lựa chọn khác nhau, chỉ chấp nhận một lần chọn mỗi lượt.
- Mở modal: lưu timeScale trước đó, đặt 0, chặn input combat. Đóng/disable: trả lại timeScale đã lưu; nhiều lượt chỉ resume sau lượt cuối.
- Cập nhật sát thương cho melee/projectile bằng bonus runtime trung tính mặc định (1), không nhân trực tiếp giá trị Might=4 vào sát thương hiện tại.
- HUD cập nhật qua sự kiện và khởi tạo snapshot; không tìm Player hoặc cấp phát chuỗi mỗi frame.
- Asset Kenney cần dùng được sao chép vào Assets/MyAssets/UI; setup editor và UI prefab/scene wiring được lưu để chạy lại ổn định.

## Giới hạn và kiểm chứng
Không sửa spawn, movement input hay dữ liệu gốc; kiểm tra compile, Play Mode, nhiều level, chống click lặp, chặn tấn công khi UI mở, khôi phục pause và ảnh HUD/modal. Xác minh API Unity qua MCP docs nếu nhóm đó sẵn có.

## Phụ thuộc
FR-001–FR-006 đã được duyệt bằng tin nhắn "ok"; triển khai tuần tự, không delegation.
