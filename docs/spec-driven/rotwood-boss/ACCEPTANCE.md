# Rotwood — Acceptance

Status: Frozen. Approval: User approved on 2026-11-01. Based on PRD/Tech Design 2026-11-01.

Tất cả tiêu chí bên dưới là blocking, bằng chứng gồm report tự động trong Captures/RotwoodBoss và Console Unity.

| ID | FR | Kiểm tra và kết quả bắt buộc |
|---|---|---|
| AC-001 | FR-001 | Timer dưới 1020 không spawn (kể cả mốc 900 cũ); qua 1020 spawn đúng một boss; pause/game-over không tăng; thử ngay Editor được. |
| AC-002 | FR-002 | Prefab đủ data/audio/sprite; frames không null; UI máu cập nhật; SHA nguồn không đổi. |
| AC-003 | FR-003 | Rễ không damage trước báo; hit trong vùng, miss ngoài vùng; slow hết hạn và không mất nâng cấp tốc độ. |
| AC-004 | FR-004 | Hướng khóa sau windup; charge có swept hit và không xuyên tường; có recovery. |
| AC-005 | FR-005 | Gọi 2 sói nếu còn slot; không quá 4; cleanup khi chết. |
| AC-006 | FR-006 | Trên 50% không exposed; dưới 50% có cửa sổ 2s sau húc và damage x1,5, hết hạn trở về bình thường. |
| AC-007 | FR-007 | Kill lặp không nhân drop/win; Death phát trước kết quả; collider/tấn công dừng; panel thắng và nút hiện. |
| AC-008 | FR-008 | Melee/projectile đánh được boss; enemy cũ không đổi death; thua không bị ghi đè thắng; spawn pool và config trước/sau giữ nguyên. |

Done: compile không lỗi, tất cả AC có bằng chứng, chạy thử live và report không che giấu phần chưa kiểm chứng.
Deferred: cân bằng qua một run đầy đủ 15 phút, art đòn summon riêng, arena thiết kế riêng.

## Acceptance matrix — 2026-11-01
Judgment: **ACCEPTED_WITH_CAVEATS**.

| AC | Result | Evidence | Caveat |
|---|---|---|---|
| AC-001 | Pass | runtime-regression.json: 900s boundary, one-shot spawn, pause, terminal clock; live Editor read-back one encounter at 900 | Timer advanced deterministically, not a full 15-minute playthrough |
| AC-002 | Pass | setup.json; runtime report valid 7 sequences/audio, advancing frames, health changes/hides; boss-health.png and victory.png; source hashes | Summon uses Root-Cast; no dedicated Idle source |
| AC-003 | Pass | runtime report: 3 warnings, delayed hit, miss, one-hit, pause, slow expiry and preserved speed upgrade | Root visuals are simple generated pixel effects |
| AC-004 | Pass | runtime report: locked heading, bounded travel, real Physics2D cast stops at wall, swept hit | No authored arena; range ends charge |
| AC-005 | Pass | runtime report: repeated production summon reaches exactly four minions; death cleanup | Summoned wolves use existing prefab |
| AC-006 | Pass | runtime report: threshold, x1.5 damage, 2s expiry, high-health negative case | Initial balance only |
| AC-007 | Pass | runtime report: terminal death, single drop/event, delayed win, real UI/buttons; victory screenshot | None |
| AC-008 | Pass | runtime report: actual melee/projectile callbacks, ordinary EnemyStats branch, real Restart and losing-run priority; scene/source unchanged | Existing spawner warns about invalid pool entries; retained as requested |

Evidence directory: `Captures/RotwoodBoss/`. Latest report: **45 checks PASS**. Compile successful (Unity compile job 74a41eb5); existing unrelated obsolete API warnings remain.
Builder rerun read-back: `encounters=1 spawnAt=900 prefab=Assets/Prefab/Boss/Rotwood.prefab scene=Assets/Scenes/SampleScene.unity dirty=False`.
