# Dùng cho màn chọn sau này

## Hợp đồng prefab

```text
Character (PlayerStats + PlayerController + Animator + SpriteRenderer)
└── WeaponHolder                 ← điểm tay, hiệu chỉnh một lần trên nhân vật
    └── weapon instance           ← được tạo khi chạy, local position/rotation/scale = 0/identity/1
        └── Visual                ← WeaponGrip căn sprite vào tay
```

- Prefab nhân vật không gắn sẵn vũ khí; `CharacterData.StartingWeapon` chỉ là default.
- Weapon prefab có một WeaponController ở root, weaponData và WeaponGrip hợp lệ. Visual phải là child; không đặt WeaponHolder trong weapon prefab.
- WeaponGrip định nghĩa Visual Size, Sprite Angle, Sprite Grip, Hold Offset, Muzzle Offset. Không chỉnh localScale của Visual để bù PPU: component sẽ chuẩn hóa lại theo Visual Size.
- Anchor thuộc nhân vật; sprite/grip/muzzle thuộc vũ khí. Chỉ hiệu chỉnh từng nhân vật và từng weapon một lần, không có offset theo từng tổ hợp. Nhân vật có animation dịch chuyển bàn tay hoặc sprite mới vẫn phải được hiệu chỉnh phù hợp.

## API runtime

```csharp
GameObject player = Instantiate(selectedCharacterPrefab, spawnPosition, Quaternion.identity);
WeaponHolder holder = player.GetComponentInChildren<WeaponHolder>();
if (selectedWeaponPrefab != null && !holder.Equip(selectedWeaponPrefab))
    Debug.LogWarning("Lựa chọn không hợp lệ; giữ vũ khí mặc định.");
```

- Không chọn weapon: để Awake lắp default. Không dùng `Equip(null)` để yêu cầu default; null là lựa chọn sai và trả false.
- `Equip` dùng được trước Awake nếu nhân vật được instantiate dưới parent inactive, hoặc ngay sau spawn. Lựa chọn đã gắn không bị Awake ghi đè.
- Trước activation, CurrentWeapon/pose đã có; state ammo được khởi tạo ở Start khi weapon hoạt động. HUD hiển thị EQUIPPING nếu state chưa sẵn sàng.
- `CurrentWeapon` là instance đang dùng. `Equip` cùng prefab lần nữa giữ nguyên instance và ammo; chọn prefab khác ngắt instance cũ rồi tạo instance mới.
- Thất bại validation giữ current, không xóa tay/nhân vật; không sửa CharacterData.StartingWeapon và không ghi asset của lựa chọn phiên chơi.
- WeaponHud và điều kiện nâng cấp ranged đọc current. Không cần màn chọn chỉnh offset, root scale hoặc hướng nòng.

## Giới hạn có chủ ý

Chưa có màn chọn hay lưu lựa chọn. Khi làm màn chọn, chỉ tạo một Player hoạt động và nối camera/spawner/HUD/level-up/GameOver vào instance được chọn; không instantiate thêm một Player cạnh Player cũ rồi dựa vào singleton. `Equip` chuẩn bị vũ khí khởi đầu, không phải inventory giữa trận: đổi sang prefab khác tạo state đạn mới, không lưu đạn cũ.

`Tools/System Leveling/Configure Neutral Weapon Holders` là migration/calibration có backup, không phải thao tác cần gọi khi chọn nhân vật. Menu preset Weapon Ammo cũng đã ngừng nhúng weapon vào prefab nhân vật.
