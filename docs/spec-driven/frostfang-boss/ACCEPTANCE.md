# Frostfang Alpha — điều kiện nghiệm thu
Status: Frozen. Approval: Approved by user 2026-11-04. Based on PRD/TECH_DESIGN 2026-11-04.

Tất cả AC dưới đây blocking; evidence tại Captures/FrostfangBoss. Không tuyên bố hoàn thành chỉ dựa vào compile.

| AC | FR | Kịch bản / kết quả quan sát | Evidence |
|---|---|---|---|
| AC-001 | 001,006 | Load map tuyết: chưa spawn ở 900s hoặc 1019s, pause không tăng clock, qua 1020s spawn một boss, gọi lại không trùng. Map rừng không có Frostfang. | Runtime assertions |
| AC-002 | 002 | Các motion/FX có frame hợp lệ, advance thực tế, không bị EnemyAnimation cạnh tranh; source PNG SHA không đổi. | Import read-back, hashes, ảnh boss |
| AC-003 | 003 | Vồ khóa mục tiêu khi báo, né khỏi vùng đáp không trúng; sweep không xuyên vật cản; sát thương chỉ sau báo. | Production tick/physics assertions |
| AC-004 | 003,004 | Phun đúng hướng/range/cone, ngoài cone không trúng, nhịp damage đúng; ba cọc warning trước hit, mỗi cọc hit một lần; bão giới hạn vùng/thời gian, slow không mất upgrade, pause đóng băng logic/FX. | Runtime assertions, ảnh đòn |
| AC-005 | 005 | HP boss khớp stats; kiếm/đạn thật gây damage; Kill lặp không thêm reward/drop; Death hoàn tất mới thắng; hết run dọn cọc/FX/HUD. Restart tạo encounter mới, thua không bị ghi đè thành thắng. | Runtime assertions, ảnh victory |
| AC-006 | 006 | Rotwood và quái thường vẫn chết đúng; setup chạy lại không trùng encounter, spawn config/source art không đổi, ví thật không đổi; compile và Console không có lỗi mới. | Regressions/read-back/source snapshots |

Definition of done: toàn bộ AC Pass, ảnh thực tế được xem, report lưu và giới hạn còn lại nêu rõ. Thiếu evidence của AC blocking là chưa đạt.
Deferred: build/DPI, full-run balancing, audio mới. Không mở rộng sang cơ chế trú bão/phá cọc trong bản này.

## Acceptance matrix — 2026-11-04
Overall: **ACCEPTED_WITH_CAVEATS**. Tất cả AC blocking Pass.

| AC | Result | Evidence | Defect/Caveat |
|---|---|---|---|
| AC-001 | Pass | runtime-regression.json: forest isolation, 899/900s, paused clock, repeat spawn; safe spawn on actual snow Tilemap | Không chạy chờ đủ 15 phút, dùng production Tick để kiểm tra mốc |
| AC-002 | Pass | 7 motions/4 FX runtime read-back; Animate advance/pause; asset-validation.json: 82 uncut body frames, source SHA unchanged, FX copies exact | Không thêm audio mới |
| AC-003 | Pass | Locked landing/dodge/no early damage/single impact/real wall sweep assertions | Tuning ban đầu |
| AC-004 | Pass | Cone bounds/damage interval/dodge; 3 pillar warnings/one hit; blizzard radius/interval/duration; body/FX pause and slow-expiry assertions; breath.png, pillars.png, blizzard.png inspected | Screenshot dùng ép tick/HP cao cho fixture, không phải run production |
| AC-005 | Pass | Real melee/projectile paths, real stats HP, repeated Kill/Soul guard, cleanup after death/loss, Death then correctly named victory, restart and defeat precedence; victory.png inspected. Rotwood regression additionally checks shared drop-once path | None |
| AC-006 | Pass | Frostfang **62 checks PASS**, Rotwood **49 checks PASS** after final shared edits; real PlayerPrefs Soul unchanged; source/scene snapshots; scene-validation.json: old documents unchanged; one encounter after setup twice; Console 0 errors | Existing invalid spawn-pool warnings retained |

Final Editor returned outside Play Mode. Shared diffs reviewed: EnemyStats adds one death branch; GameOverController preserves Win() while accepting a boss name. Pre-existing GameAudio/other user edits preserved. No commit/push.
