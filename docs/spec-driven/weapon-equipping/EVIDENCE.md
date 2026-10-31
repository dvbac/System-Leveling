# Nghiệm thu — trang bị trung lập

Date: 2026-10-29. Judgment: **ACCEPTED**.

## Kết quả
| Suite | Result | Checks | File |
| --- | --- | ---: | --- |
| 16 character/weapon combinations + Equip | PASS | 355 | `Captures/WeaponEquipping/regression.json` |
| Ammo/charge/holding/PPU | PASS | 117 | `Captures/WeaponAmmo/regression.json` |
| Reference enemies + legacy behavior | PASS | 51 | `Captures/ReferenceAnimation/runtime-regression.json` |
| HUD / level-up | PASS | 42 | `Captures/HudLevelUp/regression.json` |
| Game Over / Restart | PASS | 44 | `Captures/BlueHudGameOver/regression.json` |
| Survival / Horde | PASS | 140 | `Captures/SurvivalSpawning/regression.json` |

Tổng **749** kiểm tra; metadata/timestamp: `Captures/WeaponEquipping/summary.json`. Compile jobs e663247d and 2bc83460: completed, 0 errors.

## Acceptance matrix
| AC | Result | Evidence | Caveat |
| --- | --- | --- | --- |
| AC001 | Pass | 4 actors × 4 prefabs, local identity, grip-on-hand and four directions per combination; actual ranged projectiles at muzzle | Geometry calibrated once per character/weapon, not arbitrary uncalibrated art |
| AC002 | Pass | All four data defaults; explicit choices on active actors and before activation of neutral prefabs; data + .meta snapshots byte-identical | Mage tested through actual scene actor, no extra Mage prefab created |
| AC003 | Pass | Repeated same choice keeps instance/ammo; old instance inactive immediately and gone after frame; null/missing-grip/missing-data rejected without losing current | No inventory-owned ammo persistence between different weapons |
| AC004 | Pass | Actual scene HUD follows book/bow/crossbow/sword and returns from melee to book; ranged-upgrade eligibility follows CurrentWeapon; projectile shot direction/origin verified | No new selection UI added |
| AC005 | Pass | All regression suites above; original components and importer/data hashes preserved | Existing retired Blue Slime behavior/fixture handling unchanged |

## Bảo toàn dữ liệu / scene
- `Captures/WeaponEquipping/preservation.json`: mỗi character prefab giữ nguyên 12 section object/component gốc ngoài Transform; bỏ weapon children theo hợp đồng, không rebuild character.
- `Captures/WeaponEquipping/preserved-data-and-imports.json`: giữ nguyên importer PPU64/filter/compression do người dùng sửa, không đổi melee controller, damage/ammo/reload data.
- `Before/Assets/Scenes/SampleScene.unity` giữ bản disk trước migration; `Before/Live-SampleScene.unity` giữ cả scene live đã lưu trước khi chỉnh. Các prefab bị tác động cũng có backup.
- `Captures/WeaponEquipping/final-editor-state.json`: scene SampleScene, Edit Mode sau khi stop, isDirty=false.
- Scene Hunter trong ảnh cũ đã không còn ở cả hai snapshot trước migration này (không có tên hoặc prefab GUID). Công việc này không xóa hay tự phục hồi actor đó. Hunter prefab vẫn được giữ và chạy trong ma trận 16 tổ hợp.
- Ảnh runtime đã xem: `Captures/WeaponEquipping/hud-runtime.png`, cho thấy default book + ammo HUD sau khi holder tự equip.

## Dùng tiếp
Xem `USAGE.md`. Character prefab rỗng chỉ giữ điểm tay; default ở CharacterData; lựa chọn gọi `holder.Equip(prefab)` và đọc `holder.CurrentWeapon`. Không chỉnh offset theo cặp hoặc thay asset default để lưu lựa chọn phiên chơi.
