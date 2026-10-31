# Thiết kế trang bị
Status: Frozen
Approval: User "duyệt", 2026-10-29.

- Giữ CharacterScriptableObject.StartingWeapon làm default. Không thêm SelectionManager, singleton mới, enum class, kho asset hoặc dependency.
- WeaponHolder quản lý `WeaponController CurrentWeapon { get; }` và `bool Equip(GameObject weaponPrefab)`. Chọn prefab, không sửa damage/ammo data của vũ khí để lắp.
- Equip là API Play Mode, dùng cả khi player chưa active; ngoài Play Mode trả false thay vì sửa asset/scene. Weapon prefab không chứa WeaponHolder lồng nhau để tránh tự trang bị đệ quy.
- Equip validate root có WeaponController, weaponData và WeaponGrip trước khi thay. Lắp instance đúng anchor với local identity, áp dụng grip, vô hiệu vũ khí cũ rồi Destroy để không cùng bắn trong frame chờ hủy. Không Destroy WeaponHolder. Thất bại giữ vũ khí cũ.
- Gọi trước Awake (player được tạo trong parent inactive) hoặc ngay sau spawn đều phải tôn trọng lựa chọn. Awake nhận vũ khí đã được Equip, tương thích weapon child cũ khi cần, và chỉ dùng default khi chưa có lựa chọn.
- Profile spriteGrip + visualSize + angle là tọa độ weapon; anchor transform là tọa độ character. Không hardcode tên nhân vật trong runtime. Muzzle/aim đi theo cùng transform để không lệch sau flip.
- WeaponHud lấy CurrentWeapon từ holder đã bind thay vì cache weapon một lần. PlayerStats.HasRangedWeapon theo vũ khí hoạt động; không thêm upgrade mới.
- Migration chỉ bỏ weapon children đã biết, giữ anchor và phần khác của nhân vật, giữ default trong data. Cập nhật menu setup cũ để không tái nhúng weapon vào prefab. Không tự tạo thêm Mage prefab ngoài yêu cầu.
- Regression kiểm mọi tổ hợp từ scene Mage và ba prefab, selection trước/sau Awake, lỗi prefab, gọi lặp, không trùng, data không đổi, vị trí/scale/aim/muzzle và hồi quy gameplay.
