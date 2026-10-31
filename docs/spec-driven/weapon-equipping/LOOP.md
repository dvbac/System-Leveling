# Trạng thái
Current state: accepted
Current loop: LOOP001
Objective: tách equip khỏi prefab nhân vật theo yêu cầu mới; không làm UI chọn lúc này.
Next: none blocking. API ready for a future selection UI; usage and acceptance evidence in USAGE.md/EVIDENCE.md.
Approval: user "duyệt", 2026-10-29.

- Đã đọc CharacterScriptableObject, WeaponHolder, WeaponGrip và các consumer HUD/controller ở phiên hiện tại.
- Bước nền trước đã kết thúc timeout vì server đổi từ port8090 sang8091 sau reload. Đã đọc registry, xác nhận cùng instance SystemLeveling_BC4E0716; GET job7bece1b9 trên port8091 xác nhận compile completed, 0 errors. Chưa xác nhận menu migration cũ đã chạy, không replay write khi chưa biết kết quả. Thiết kế equip mới thay thế việc chỉ sửa pose.
- Chưa viết production cho API Equip hoặc tháo weapon children ở bước thiết kế này.

## LOOP001
- Scope frozen; same main agent works sequentially, no delegation.
- Actor and weapon source assets backed up before migration; do not reset or revert user changes.
- Current compile from prior task succeeded; calibration apply not assumed. New migration will explicitly perform calibration and neutralization once, read back and test.
- Judgment: Pending.

## LOOP001 result
- Compile e663247d/2bc83460 completed with 0 errors. New migration menu ran successfully; explicit read-back log NEUTRAL WEAPON HOLDERS READY confirmed completion.
- WeaponEquippingRegression PASS 355: all 16 combinations, four aim directions, real muzzle shots, root identity, correct grip, pre-Awake and post-spawn selection, invalid selection, idempotency, no ammo refill on repeated selection, current HUD, data/import snapshots and fresh-scene fallback.
- Existing suites PASS: WeaponAmmo117 (includes holding/PPU/flip checks), Reference51, HUD42, GameOver44, Survival140. Total 749.
- Preservation script: all 12 original non-transform object/component sections unchanged in each character prefab. Original user PPU64/filter/compression settings, melee controller and ranged balance data hashes unchanged.
- User scene was saved before migration as Before/Live-SampleScene.unity; original disk backup also retained. Scene clean in Edit Mode after tests. Both pre-migration snapshots already had no scene Hunter instance; no Hunter actor was deleted or recreated by this work. The Hunter prefab remains intact and tested.
- Judgment: ACCEPTED. AC001–005 all Pass. No selection UI, inventory or save system implemented, as approved. New artwork still needs one-time anchor/grip calibration.
