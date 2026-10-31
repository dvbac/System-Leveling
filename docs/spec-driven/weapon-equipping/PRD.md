# Trang bị độc lập nhân vật / vũ khí

Status: Frozen
Approval: User "duyệt", 2026-10-29; chọn cấu trúc prefab trung lập, default trong data, API Equip chung, chưa làm UI chọn.

## Mục tiêu
Người dùng muốn chuẩn bị hệ thống cầm cho màn chọn nhân vật và chọn vũ khí khởi đầu sau này, không phải chỉ chỉnh riêng Hunter+nỏ. Không làm màn chọn ở bước này.

## Facts
- CharacterScriptableObject đã có StartingWeapon (default), không cần thêm asset chọn nhân vật hoặc dữ liệu save.
- WeaponHolder.Awake hiện bỏ qua default khi thấy weapon child. Nếu nhúng vũ khí theo lớp vào character prefab, lựa chọn bên ngoài dễ bị bỏ qua hoặc tạo trùng.
- WeaponGrip đang tách size, góc sprite, điểm cầm và muzzle theo từng vũ khí; WeaponHolder là anchor/ngắm của nhân vật.
- WeaponHud hiện cache ranged một lần ở Start: cần đọc vũ khí đang trang bị khi lựa chọn thay đổi, không giữ reference vũ khí đã hủy.
- Scene đang có cả Player và Hunter preview; bảo toàn actor scene, không biến game singleton thành multiplayer.

## Đề xuất cấu trúc
- Mỗi character prefab giữ một WeaponHolder/điểm tay chuẩn, KHÔNG nhúng sẵn kiếm/cung/nỏ.
- CharacterData.StartingWeapon chỉ làm vũ khí mặc định nếu không có lựa chọn từ bên ngoài; lựa chọn phiên chơi không ghi vào asset này.
- Mọi weapon prefab có WeaponController + WeaponGrip, tự chứa size/góc/tay cầm/muzzle. Không có bảng offset theo từng cặp nhân vật-vũ khí.
- Một API Equip nhận prefab được chọn; cùng một prefab dùng được cho mọi nhân vật đã chuẩn hóa.

## Yêu cầu / nghiệm thu tham chiếu
| FR | Nội dung |
| --- | --- |
| FR001 | Trang bị kiếm, sách, cung, nỏ lên Mage/Archer/Hunter/Knight mà không sửa offset riêng cho từng cặp. |
| FR002 | Gắn root weapon local position=0, rotation=identity, scale=1 vào anchor. Visual calibration thuộc weapon; vị trí anchor thuộc nhân vật. |
| FR003 | Lựa chọn khởi đầu ngoài override default; không có lựa chọn thì dùng StartingWeapon hiện tại. Không sửa CharacterData khi chọn. |
| FR004 | Trang bị chỉ có một vũ khí hoạt động; gọi Equip lặp lại cùng lựa chọn không sinh trùng; lựa chọn không hợp lệ không làm mất vũ khí đang có. |
| FR005 | Aim, flip, grip, projectile muzzle, ammo/reload/charge và HUD dùng vũ khí hiện tại. Giữ tương thích kiếm. |
| FR006 | Giữ root/Animator/collider/stats/input/actor đặt tay của người dùng. Backup trước migration. |

## Không làm / giới hạn
Không làm UI chọn nhân vật/vũ khí, inventory, save lựa chọn, đổi vũ khí giữa trận, restriction theo class, nhiều người chơi hoặc animation mới. Trong Editor, prefab nhân vật trung lập sẽ không mang vũ khí sẵn; runtime lắp default hoặc lựa chọn. Chỉ anchor nhân vật và grip của từng weapon cần hiệu chỉnh một lần; sprite mới/animation đổi vị trí tay vẫn cần calibration, không hứa mọi ảnh bất kỳ tự khớp.
