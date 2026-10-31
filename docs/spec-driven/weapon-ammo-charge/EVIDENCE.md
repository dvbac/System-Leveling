# Kết quả và cách dùng

Date: 2026-10-29. Judgment: **ACCEPTED_WITH_CAVEATS** (mọi AC blocking PASS).

## Đã triển khai
- Mage: sách phép, 8 đạn, giữ chuột trái để bắn, tự nạp cả băng khi cạn.
- Archer: cung, 7 tên, giữ bắn; nhả mới hồi sau delay, từng tên. Giữ khi hết tên sẽ không hồi — cần nhả chuột.
- Hunter: nỏ, 1 tên, nhấn giữ tích lực rồi thả để bắn. Đạt trần vẫn đợi thả; giữ qua lúc hồi tên không tự tích lực lượt kế tiếp.
- Knight: kiếm, giữ code chém bằng click như trước; không có đạn.
- +Ammo: thêm 1 sức chứa cho sách/cung, không tặng ngay đạn. Nỏ vẫn 1 tên, mỗi cấp mở +0.5s trần lực cơ sở, +0.5 hệ số damage tối đa, +1/6 scale tối đa.
- +Reload: +20% tốc độ/cấp cộng dồn. Thời gian nạp và tích lực nỏ chia cho `(1 + 0.2 × cấp)`.
- HUD có ammo, trạng thái READY/RELOAD/CHARGE và thanh tiến độ. Level-up thêm CAPACITY/RECHARGE chỉ với ranged.
- Pause hủy charge không bắn, đóng băng reload, cần nhả input trước khi bắn lại; Game Over chặn toàn bộ; Restart reset đạn/cấp runtime.

## Chỉnh Inspector
Data: `Assets/Scriptable Object/Weapon/{Magic Book,Bow,Crossbow}.asset`.
Weapon/projectile prefabs: `Assets/Prefab/Weapons/`.
Trang bị đầu: `StartingWeapon` trên data nhân vật. Prefab nhân vật đã có đúng một vũ khí; holder rỗng mới tự equip, không tạo trùng.
`Configure Weapon Ammo` là menu migration/preset: chạy lại sẽ reset cấu hình vũ khí theo preset và thay weapon children; chỉnh data trực tiếp để cân bằng, không chạy menu này sau khi tự sửa vũ khí.
Không thêm inventory/phím 1–4. Nếu đổi Player bằng tay, phải đổi các tham chiếu scene/HUD/camera sang Player mới; không đặt nhiều singleton Player cùng lúc.

## Test cuối cùng
| Suite | Result | Checks | Evidence |
| --- | --- | ---: | --- |
| Weapon ammo/charge | PASS | 81 | `Captures/WeaponAmmo/regression.json` |
| Reference enemies + legacy fixture | PASS | 51 | `Captures/ReferenceAnimation/runtime-regression.json` |
| HUD/level-up | PASS | 42 | `Captures/HudLevelUp/regression.json` |
| Game Over/Restart | PASS | 44 | `Captures/BlueHudGameOver/regression.json` |
| Survival/Horde | PASS | 140 | `Captures/SurvivalSpawning/regression.json` |

Tổng **358**. Timestamp và compile job trong `Captures/WeaponAmmo/summary.json`.
Ảnh runtime đã kiểm tra: `Captures/WeaponAmmo/hud-final.png`.
Backup trước migration: `Captures/WeaponAmmo/Before/` (YAML có trước khi ghi, không phải bản reset git).
Kiểm tra bảo toàn bằng script so YAML: mỗi prefab nhân vật giữ đủ 15 object/component gốc, chỉ Transform của WeaponHolder thay danh sách child; không thay các component gốc khác. Bằng chứng: `Captures/WeaponAmmo/prefab-preservation.json` (3 PASS).
`git diff --check` chỉ báo bốn khoảng trắng sau `m_Name:` rỗng do Unity sinh trong scene YAML; không có lỗi whitespace ở mã C#.

## Acceptance matrix
| AC | Result | Evidence | Caveat |
| --- | --- | --- | --- |
| AC001 | Pass | 8 shots, interval .4, refill threshold 1.8; real book projectile damage14 | None |
| AC002 | Pass | 7 shots/.25, empty held no recovery, .45+.65 first arrow, partial recovery interruption | None |
| AC003 | Pass | Half/full/over-cap hold, release output24→96, actual sprite/collider x2 | None |
| AC004 | Pass | Actual owner and actual level-up card choices, capacity/no-free-ammo, cross ceiling3.5/1.2 and damage4.5 | None |
| AC005 | Pass | One weapon/controller per character + scene, Knight real melee slash, StartingWeapon auto-equip fixtures | User character edits preserved in-place |
| AC006 | Pass | Pause cancels charge and freezes reload, resume gate, UI click gate, death block, actual Restart full8/cấp0 | None |
| AC007 | Pass | NaN/negative delta, invalid config, missing prefab/no ammo loss, long-frame single shot, snapshot and duplicate-target collision | Not a target-device stress benchmark |
| AC008 | Pass | Scene HUD read-back, live screenshot, actual offer/select Ammo/Reload while modal blocks trigger | No new character selection UI |
| AC009 | Pass | Final compile0 errors + all five suites above | Retired Blue Slime fixtures migrated, not restored |

## Những giới hạn còn lại
- Chưa cân bằng qua trận dài hoặc profile thiết bị đích; chỉ số là preset đã duyệt.
- Một số phase scene còn tham chiếu Blue Slime đã bị xóa trước feature. Spawner cảnh báo và bỏ entry không hợp lệ; không sửa spawn balance ngoài phạm vi. Regression kiểm đúng tập prefab còn hợp lệ theo mốc và giữ test legacy behavior bằng clone Slime bỏ EnemyAnimation.
- Sửa sprite/animation hoặc add character skills không nằm trong yêu cầu; dùng sprite cung/nỏ/tên có sẵn, không thêm dependency.
