# Bằng chứng nghiệm thu

Date: 2026-10-22
Unity: 6000.4.12f1 — instance `SystemLeveling_BC4E0716` (UnitySkills, bypass/full)
Scene: `Assets/Scenes/SampleScene.unity` (đã lưu; hash trước/sau khi lưu giống nhau)

## Kết quả test (Play Mode, component thật)

| Suite | Kết quả | Checks | Thời điểm (UTC) | File |
| --- | --- | --- | --- | --- |
| Survival + Horde spawn | PASS | 140 | 04:47:27 | `Captures/SurvivalSpawning/regression.json` |
| Reference enemy (5 prefab, hit/Die/loot/animation) | PASS | 51 | 04:47:35 | `Captures/ReferenceAnimation/runtime-regression.json` |
| HUD / Level-up | PASS | 42 | 04:47:35 | `Captures/HudLevelUp/regression.json` |
| Game Over / Restart | PASS | 44 | 04:47:42 | `Captures/BlueHudGameOver/regression.json` |

Compile: job `c9027bda` completed, 0 lỗi C#. Không có Error mới trong Console sau LOOP-002.
Log chi tiết transition/xác suất: `Captures/SurvivalSpawning/state-log.txt`.
Lỗi của lượt LOOP-001 được giữ nguyên: `Captures/SurvivalSpawning/reference-loop001-failed.json`.

## Đối chiếu AC

| AC | Bằng chứng chính |
| --- | --- |
| AC-001 | Phase 0/30/60/120s đọc lại từ scene; pool mở dần 2→3→4→5 prefab; weighted pick đúng biên, bỏ entry null/weight 0/NaN. |
| AC-002 | Lớp1 thắng chỉ sang `WaitingLayer2`, chưa tạo quái; roll lớp2 đầu tiên sau đủ interval; chuỗi 10→25→40→55→70→85%, clamp 100%. |
| AC-003 | Deadline 30s, deadline ngắn hơn interval lớp2 (0.5s) và frame dài 100s đều ép triển khai; vị trí bất khả thi vẫn không spawn bừa. |
| AC-004 | Không có đoàn thứ hai khi đang chờ/triển khai/còn sống; diệt một phần vẫn `WaitingForClear`; diệt thành viên cuối reset timer/chance về đầu. |
| AC-005 | Normal spawn trong cả 3 trạng thái Horde; cap 100/30 độc lập; budget 2 normal + 4 Horde mỗi frame; frame dài không bù dồn. |
| AC-006 | Trong camera, gần player, ngoài Tilemap, đè obstacle, thiếu separation đều bị từ chối; 8 điểm footprint; thử hữu hạn rồi log cảnh báo. |
| AC-007 | `Validate()` chuẩn hóa interval/chance/min-max/count/budget; log có toggle `debugLogging`; Inspector runtime hiển thị trạng thái chỉ đọc. |
| AC-008 | `Time.timeScale=0` đóng băng cả 3 đồng hồ; Game Over dừng spawn khi Horde đang chờ; Restart thật reset thời gian/state/chance/population/counter. |
| AC-009 | Compile sạch; ReferenceEnemyRegression 51 checks trên 5 prefab; HUD 42 và Game Over 44 vẫn PASS. |

## Ghi chú kiểm chứng

- `SurvivalSpawnRegression` chạy trực tiếp `EnemySpawner.Tick(delta)` và `HordeSpawnCycle` production, không dùng bản mô phỏng song song; có reload scene thật cho Restart.
- Đo trong fixture: tick spawn cao nhất khoảng 6.8 ms khi tạo cụm 12 quái với đầy đủ kiểm tra vị trí (máy Editor, không phải benchmark thiết bị đích).
- Ảnh hưởng ngoài phạm vi: không có. Dữ liệu `Assets/Scriptable Object` được so sánh byte-identical trước/sau test.
- Preset đã ghi vào scene và có thể chỉnh toàn bộ trong Inspector của `Enemy Spawner` (mục Normal survival spawning / Horde / Placement / Diagnostics).
