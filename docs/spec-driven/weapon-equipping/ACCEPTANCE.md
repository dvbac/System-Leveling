# Điều kiện nghiệm thu
Status: Frozen
Approval: User "duyệt", 2026-10-29.

Tất cả AC blocking. Test gọi Equip/holder/grip và component gameplay production, không dùng bản mô phỏng khác.

| AC | FR | Kết quả đo được |
| --- | --- | --- |
| AC001 | 001–002 | 4 nhân vật hiện có × 4 vũ khí: một anchor, root local identity, grip trùng tay theo profile, barrel/muzzle đúng 4 hướng. Không sửa offset theo cặp. |
| AC002 | 003 | Không chọn thì default đúng; chọn khác trước activation hoặc sau spawn đều giữ lựa chọn; CharacterData và weapon data byte-identical sau test. |
| AC003 | 004 | Equip cùng lựa chọn nhiều lần vẫn một controller hoạt động; thay lựa chọn ngắt vũ khí cũ; prefab null/thiếu component không xóa current. |
| AC004 | 005 | HUD và ranged upgrades theo current; bắn thật từ muzzle; đổi lựa chọn không còn reference HUD tới weapon đã hủy; kiếm vẫn chém. |
| AC005 | 005–006 | Ammo/charge/pause/GameOver/Restart và regression cũ qua; nguyên actor/transform root/collider/animator/PPU người dùng được giữ. |

Done: compile sạch, prefab trung lập được migrate có backup, đủ bằng chứng trên. Chưa có màn chọn; API sẵn để màn chọn sau này gọi.
