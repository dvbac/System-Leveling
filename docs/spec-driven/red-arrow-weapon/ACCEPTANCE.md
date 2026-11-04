# Red Arrow — acceptance
Status: Frozen. Approval: user “Duyệt bản này”, 2026-11-05. Based on PRD/TECH_DESIGN 2026-11-05.
All AC blocking; evidence: Captures/RedArrowWeapon.

Current amendment: damage3/speed6/interval0.7s/ammo9, bắn ngắt hồi khi còn đạn, Sword tạm ẩn khỏi lựa chọn. Regression runtime đã cập nhật theo yêu cầu mới; matrix và số12 bên dưới lưu evidence lịch sử của bản đầu, không phải chỉ số hiện tại. Xem `docs/weapon-reload.md`.

| AC | FR | Expected / evidence |
|---|---|---|
| AC-001 | 001,006 | Catalog append đúng một Red Arrow, các entry cũ/thứ tự/defaults giữ nguyên; chọn và load được cả hai map; ảnh cầm vũ khí, stats, grip/muzzle hợp lệ. Runtime assertions + screenshot. |
| AC-002 | 002 | Bắn trừ một ammo, cooldown0.5/12ammo/reload1.5 theo state thật, hướng khởi đầu nằm trong±30°. HUD theo đúng state; pause/UI/end không bắn xuyên. Runtime assertions. |
| AC-003 | 003 | Đạn đổi hướng theo target đang di chuyển, tìm lại target chết, bỏ target đã hit, không target vẫn bay thẳng; pause freeze/lifetime5s hữu hạn; không damage sau run kết thúc. Production steering/hit assertions. |
| AC-004 | 004 | Base hit A rồi bounce hit B đủ damage, không hit A lặp; hết bounce hủy. Pierce xử lý trước bounce, RubberBullets/Elasticity cộng base bounce. Real collision-path assertions. |
| AC-005 | 005 | Damage/projectile-count/fire-rate/ammo/reload upgrades tác động thực; status và splinter/secondary paths vẫn dùng pipeline chung. Own + existing combat/upgrade regressions. |
| AC-006 | 006 | Vũ khí cũ không có homing/base bounce mới, ammo/combat regressions pass; setup lặp không trùng; ảnh gốc/Soul thật không đổi; compile và Console không lỗi mới. Hash/read-back/reports. |

Definition of done: tất cả AC có evidence Pass, visual review, docs và giới hạn nói rõ. Thiếu evidence blocking là chưa đạt. Deferred theo PRD: summon system, evolutions/unlock, full-run balancing và standalone build.

## Acceptance matrix — 2026-11-05
Judgment: **ACCEPTED_WITH_CAVEATS**. All blocking ACs Pass.

| AC | Result | Evidence | Caveat |
|---|---|---|---|
| AC-001 | Pass | Own selection-to-both-map tests, grip validation, selection/forest/snow screenshots; existing LoadoutGameplay covers15 pairings (3 characters x5 weapons). | No new Vampire character added |
| AC-002 | Pass | Actual controller/State: single ammo, spread bounds, half-second cooldown,12-round magazine,1.5s reload, HUD, pause and UI block. | Existing partial-magazine release reload retained |
| AC-003 | Pass | Bounded steering to moving target, dead/inactive retarget, no-target straight heading, pause, actual Start/Destroy lifetime5s, post-defeat damage rejection. | Large-scale/long-run performance not profiled |
| AC-004 | Pass | Full-damage A/B hits, same-target dedup, no third base hit, pierce before bounce, base1+Rubber1+Elasticity2, actual physics contact with kinematic boss-style target. | Bounce search range5, homing acquisition range8 |
| AC-005 | Pass | Own real damage/ammo/reload/DoubleShot/Tome/ice-fragment/Splinter assertions; UpgradeTree284 and CombatUpgrade222 checks. | Summon stats/tree explicitly out of scope |
| AC-006 | Pass | All five suites1020 checks PASS; old BaseBounces0; source/default/save preservation; setup twice unique; asset-validation.json proves catalog append only and other scene contents unchanged; Console0 errors. | Existing unrelated spawn warnings unchanged |

Evidence root: Captures/RedArrowWeapon; validation-summary.json references exact compatibility reports. Initial contaminated baseline failure retained separately and explained in LOOP; existing tests were not weakened.
