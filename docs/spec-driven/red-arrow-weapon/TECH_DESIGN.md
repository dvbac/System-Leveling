# Red Arrow — thiết kế
Status: Frozen. Approval: user “Duyệt bản này”, 2026-11-05. Based on PRD 2026-11-05.

## Hiện trạng / phạm vi ảnh hưởng
RangedWeaponController dùng RangedWeaponState/WeaponGrip, tạo projectile qua InitializeShot. ProjectileWeaponBehaviour đã chứa hitTargets, pierce, bounce, UpgradeHit và status/tome hooks; bounce tìm địch khác trong5 unit. LoadoutSelection có prefab catalog và mô tả chung theo FireMode. PlayerUpgradeTrees tạo đạn phụ từ cùng prefab vũ khí. Existing enums/asset defaults phải giữ nguyên.

## Phương án tối thiểu
- Prefab projectile homing kế thừa ProjectileWeaponBehaviour, chỉ bổ sung steering/target acquisition; tái dùng damage/status/bounce/lifetime. Không viết lại pipeline hit hoặc thêm framework projectile.
- WeaponScriptableObject thêm base bounce mặc định0 để cũ không đổi; projectile cộng base bounce với player BulletBounces. Chỉ mở hook nội bộ cần thiết cho homing bỏ mục tiêu đã hit.
- Spread ngẫu nhiên của một viên chỉ áp dụng Red Arrow; không đổi cách ngắm vũ khí cũ. Ưu tiên reuse hooks/pattern thực tế khi triển khai.
- Builder tạo prefab/data mới, import bản sprite riêng nếu cần Point/PPU khác, calibrate grip bằng WeaponGrip; append duy nhất vào LoadoutSelection. Không ghi đè PNG gốc hoặc asset đã chỉnh tay.
- Mô tả lựa chọn nêu homing/bounce đúng, không giả là đạn thẳng. Không thêm singleton hoặc dependency.

## Contracts / safety
InitializeShot vẫn giữ mọi caller cũ. BaseBounces=0 tương thích assets cũ; FireMode Magazine không thêm enum/đổi index serialized. HitTargets thuộc projectile, không bị reset khi retarget; đạn phụ vẫn nhận UpgradeHit secondary guards. Lifetime hữu hạn5s, target scan có nhịp giới hạn, không scan mỗi frame trên mọi đạn. Dừng khi pause/end, bỏ target chết/destroyed/inactive; kiểm tra delta hữu hạn.

## Kiểm tra / release
Own regression chạy production state/controller/hit và steering, catalog-to-game cả hai map, pause/lifetime/death/reload, nâng cấp và baseline weapon regression. Snapshot source SHA, catalog before/after, wallet và assets cũ. Setup idempotent, backup scene trước write; scene dirty thì stop, không tự bỏ thay đổi người dùng.
Approved. Runtime safety của shared projectile edits chưa chứng minh, phải chạy regressions sau implementation. RedArrowController overrides a narrow single-shot spread hook; RedArrowProjectile owns steering, shared ProjectileWeaponBehaviour owns hits. No new enum or player stats. API Mathf.MoveTowardsAngle verified through live Unity reflection. UnitySkills instance is on port8091 (registry), not old8090.
