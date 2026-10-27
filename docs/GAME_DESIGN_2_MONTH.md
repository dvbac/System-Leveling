# System Leveling — Kế hoạch thiết kế & sản xuất 8 tuần

| Hạng mục | Nội dung |
|---|---|
| Trạng thái | Draft để duyệt — 2026-10-22 |
| Thời gian | 22/10/2026 → 16/12/2026 (8 tuần) |
| Nền tảng | Windows PC, Unity 6000.4.12f1, Input System, UGUI |
| Quy mô | 1 dev, ~30 giờ/tuần (~240 giờ) |
| Nội dung launch | 3 map, 3 boss, 4 nhân vật, 4 vũ khí, 12 thẻ nâng cấp + 2 thẻ dự phòng |
| Ngôn ngữ | UI tiếng Anh (khớp HUD hiện có), tài liệu tiếng Việt |
| Giả định | "2 tháng" là thời gian **sản xuất**. Độ bền nội dung mục tiêu: 6–10 giờ để phá 3 map, 12–15 giờ để max Forge |

> Nếu "2 tháng" có nghĩa là **người chơi chơi liên tục 2 tháng**, xem §17. Bản này ưu tiên làm ra một game hoàn chỉnh, chơi được end-to-end trong 8 tuần.

---

## 0. Tóm tắt một trang

**Pitch:** Một game sinh tồn top-down 2D. Người chơi chọn nhân vật, vào một đấu trường, sống sót qua các đợt quái dày lên theo thời gian, nhặt vàng và XP, chọn nâng cấp ngẫu nhiên sau mỗi level, rồi hạ boss để mở map tiếp theo. Vàng mang về Camp để mua nâng cấp vĩnh viễn và mở khóa nhân vật.

**Bối cảnh (3 dòng, không cutscene):**
> "Hệ Thống" đóng băng thế giới thành những đêm lặp lại. Mỗi vùng đất là một mảnh ký ức bị một thực thể canh giữ. Kẻ sống sót hấp thụ ký ức để mạnh lên — và mỗi lần chết, vàng vẫn là thứ duy nhất ở lại.

**Người chơi làm gì trong 30 giây đầu:**
1. Di chuyển bằng WASD, ngắm bằng chuột, đánh bằng chuột trái (hoặc vũ khí tự động).
2. Giết quái → nhặt XP → level up → chọn 1 trong 3 thẻ nâng cấp.
3. Giết quái → nhặt vàng → sống tới mốc boss → hạ boss → nhận vàng → về Camp.

**Định nghĩa thành công:**
- Một người mới phá được map 1 sau 3–6 run, không cần đọc hướng dẫn ngoài.
- Mỗi run có ít nhất một quyết định build khác run trước.
- Không có softlock, không mất save, 60 FPS ở 120 quái.
- Toàn bộ vòng lặp chạy được trên build sạch, không cần Unity Editor.

---

## 1. Trụ cột thiết kế

| # | Trụ cột | Cách thể hiện | Điều không làm |
|---|---|---|---|
| P1 | Đọc trận đấu trong 1 giây | Quái đông nhưng silhouette rõ; đòn boss có telegraph; màu nguy hiểm thống nhất | Không hiệu ứng rối, không đạn cùng màu nền |
| P2 | Mỗi run một build | 3 thẻ ngẫu nhiên mỗi level, 4 vũ khí, 8 passive, tối đa 3 vũ khí | Không cho chọn lại cùng một thẻ tối đa |
| P3 | Vàng là tiến bộ | Vàng rơi từ quái/boss, giữ 100% khi chết, tiêu ở Forge | Không mất vàng khi thua, không IAP |
| P4 | Boss là cửa ải | Mỗi map kết thúc bằng 1 boss riêng, mở map kế tiếp | Không cho qua map bằng cách chết |

**Tham chiếu:** Vampire Survivors (spawn + draft), Brotato (meta rõ ràng, run ngắn), Hades (telegraph boss). Không sao chép nội dung; chỉ học nhịp.

**Ngoài phạm vi launch:** multiplayer, mobile, cloud save, procedural map, cốt truyện phân nhánh, achievement, leaderboard, IAP, localization ngoài EN.

---

## 2. Vòng lặp game

```
CAMP
 ├─ Forge: tiêu vàng mua 5 chỉ số vĩnh viễn
 ├─ Characters: chọn / mở khóa 4 nhân vật
 ├─ Maps: chọn 3 map đã mở
 └─ START
      ↓
   ARENA (10 / 15 / 20 phút)
      ├─ di chuyển, đánh, nhặt XP + vàng
      ├─ level up → 3 thẻ → chọn 1
      ├─ horde / elite / hazard theo timeline
      └─ BOSS ở cuối map
           ├─ thắng → Results (vàng + unlock) → Camp
           └─ chết → Results (giữ vàng) → Camp
```

**Trong run:**
- Timer chạy từ 0. Boss spawn đúng mốc. Giết boss = thắng, run kết thúc ngay.
- Chết trước boss = thua. Vàng đã nhặt được giữ 100%.
- Pause và màn chọn nâng cấp dừng toàn bộ timer, spawn, đạn, quái.
- Không có "endless mode" ở launch.

**Ngoài run:**
- Kết quả hiển thị: thời gian, level, số quái giết, vàng kiếm được (breakdown), unlock mới.
- Vàng cộng vào save ngay khi nhặt, không chờ hết run.
- Mua nâng cấp áp dụng ngay từ run kế tiếp.

---

## 3. Điều khiển, camera, cảm giác

| Hạng mục | Quyết định |
|---|---|
| Di chuyển | WASD / phím mũi tên |
| Ngắm | Chuột; nhân vật quay theo con trỏ |
| Đánh thường | Giữ chuột trái để đánh liên tục khi hồi xong; vũ khí projectile bắn theo hướng chuột |
| Chọn thẻ | Click hoặc phím 1 / 2 / 3 |
| Pause | Esc; pause không huỷ run |
| Camera | Orthographic, theo player, giữ nguyên cấu hình hiện tại; không zoom trong combat thường |
| Độ phân giải | 16:9, hỗ trợ cửa sổ + fullscreen |
| I-frame | 0.6 giây sau khi trúng đòn; nhấp nháy sprite |
| Feedback trúng đòn | Flash trắng 0.05 giây, knockback nhẹ, SFX; không hiện damage number ở launch |
| Screen shake | Có, toggle trong Settings; tắt mặc định ở chế độ dễ |

**Nguyên tắc:** người chơi phải phân biệt được "đang bị đánh" và "đang đánh trúng" mà không cần nhìn thanh máu.

---

## 4. Tiến trình vĩnh viễn — Vàng & Forge

### 4.1 Quy tắc vàng

- Vàng rơi từ quái, elite, horde, prop, boss và thưởng thời gian.
- Vàng được **cộng vào tổng của run ngay khi nhặt**; chết không mất (chốt vào save ở màn Result).
- Chỉ một loại tiền: vàng. Không kim cương, không vé, không IAP.
- Vàng dùng cho: 5 track Forge + mở khóa Knight.
- Không bán vật phẩm, không loot box.

### 4.2 Nguồn vàng

| Nguồn | Giá trị | Ghi chú |
|---|---|---|
| Quái thường | 25% rơi 1 vàng | Tự động hút trong bán kính magnet |
| Elite | 5 vàng, 100% | Elite là biến thể to hơn, không phải quái mới |
| Horde | 1 vàng/quái | Đoàn đông, rủi ro cao |
| Prop vỡ | 15% rơi 1 vàng | Tái sử dụng `BreakableProps` |
| Boss | 150 / 250 / 400 | Theo map 1 / 2 / 3 |
| Thưởng thời gian | 5 vàng/phút | 50 / 75 / 100 |
| First clear | +150 / +250 / +400 | Một lần duy nhất mỗi map |

**Ước tính vàng/run:**

| Map | Thời gian | Quái giết (ước tính) | Vàng/run | Sau first clear |
|---|---:|---:|---:|---:|
| 1 | 10 phút | ~500 | ~410 | ~560 |
| 2 | 15 phút | ~900 | ~780 | ~1.030 |
| 3 | 20 phút | ~1.400 | ~1.340 | ~1.740 |

Con số này là mục tiêu cân bằng, không hardcode; tuần 7 chỉnh theo telemetry.

### 4.3 Forge — 5 track, 5 cấp mỗi track

| Track | Hiệu ứng mỗi cấp | Cấp 1 | Cấp 2 | Cấp 3 | Cấp 4 | Cấp 5 | Tổng | Max |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Might | +5% sát thương | 60 | 100 | 160 | 260 | 420 | 1.000 | +25% |
| Vitality | +6 máu tối đa | 60 | 100 | 160 | 260 | 420 | 1.000 | +30 |
| Agility | +3% tốc chạy | 80 | 130 | 210 | 340 | 550 | 1.310 | +15% |
| Wisdom | +6% XP | 80 | 130 | 210 | 340 | 550 | 1.310 | +30% |
| Fortune | +8% vàng | 100 | 160 | 260 | 420 | 680 | 1.620 | +40% |
| **Tổng** | | | | | | | **6.240** | |

- Mua bằng click, trừ vàng ngay, lưu ngay.
- Không hoàn tiền; nút mua bị disable khi thiếu vàng hoặc đã max.
- Hiệu ứng áp dụng vào đầu run, không cần restart.
- Tổng 6.240 vàng + 600 vàng mở Knight ≈ 6.840 vàng. Với 400–800 vàng/run đầu, người chơi chạm max sau ~12–15 run.

### 4.4 Kỳ vọng tiến trình

| Mốc | Số run dự kiến | Trạng thái |
|---|---:|---|
| Run đầu | 1 | Mage, map 1, 0 vàng |
| Phá map 1 | 3–6 | 5–8 cấp Forge, mở map 2 |
| Mở Knight | 2–4 | 600 vàng |
| Phá map 2 | 2–4 lần thử sau khi farm | Mở map 3, Cleric |
| Phá map 3 | 2–4 lần thử | Max gần đủ Forge |
| Max toàn bộ | 12–15 | Hết nội dung launch |

Nếu người chơi phá map 1 ngay run đầu, map 1 quá dễ. Nếu quá 8 run, giảm HP boss hoặc tăng vàng/run.

---

## 5. Nhân vật

### 5.1 Bảng nhân vật

| Nhân vật | HP | Hồi máu/s | Tốc chạy | Might | Projectile | Magnet | Vũ khí đầu | Mở khóa |
|---|---:|---:|---:|---:|---:|---:|---|---|
| **Mage** | 20 | 0 | 3.0 | 4.0 | 2.0 | 1.6 | Magic Book | Mặc định |
| **Knight** | 32 | 0 | 2.8 | 5.0 | 2.0 | 1.4 | Sword | 600 vàng |
| **Ranger** | 22 | 0 | 3.4 | 4.5 | 3.0 | 2.0 | Bow | Sống sót 12:00 trong một run (map 2 trở lên) |
| **Cleric** | 26 | 0.5 | 2.9 | 3.5 | 2.0 | 2.2 | Holy Aura | Hạ Broodmother |

- `Might` chuẩn hoá: sát thương cuối × `Might / 4`.
- `Projectile` chuẩn hoá: tốc độ đạn × `Projectile / 2`.
- `Recovery` là hồi máu mỗi giây, đã có trong `PlayerStats`.
- Không thêm passive riêng bằng code; khác biệt đến từ stat + vũ khí đầu. Đủ để 4 nhân vật chơi khác nhau mà không phình hệ thống.

### 5.2 Vai trò

| Nhân vật | Lối chơi | Điểm mạnh | Điểm yếu |
|---|---|---|---|
| Mage | Ranged, kiểm soát khoảng cách | Nhiều projectile, pierce, magnet cao | Máu thấp, DPS đơn mục tiêu thấp |
| Knight | Melee, áp sát | Máu cao, sát thương mỗi đòn lớn | Phải vào gần, dễ ăn đòn |
| Ranger | Kite, tốc độ cao | Chạy nhanh, đạn nhanh, tầm xa | HP thấp, cần aim tốt |
| Cleric | AoE quanh người, hồi máu | Sustain, dọn đám đông | Tầm ngắn, DPS boss thấp |

### 5.3 Mở khóa

- **Mage:** có sẵn.
- **Knight:** mua 600 vàng ở màn Characters. Có thể mua ngay từ đầu nếu đủ vàng.
- **Ranger:** sống sót tới 12:00 trong một run (map 2 trở lên, vì map 1 kết thúc ở 10:00). Hiển thị tiến độ bằng best time trong màn Characters.
- **Cleric:** hạ Broodmother lần đầu. Không cần mua.
- Mọi unlock lưu ngay, có toast "CHARACTER UNLOCKED".
- Không cho chọn nhân vật chưa mở; card hiển thị điều kiện thay vì ẩn hoàn toàn.

### 5.4 Chọn nhân vật

- 4 card ngang, portrait + 4 chỉ số chính + vũ khí đầu + điều kiện.
- Nhân vật đã chọn được highlight; nút START chỉ bật khi map và nhân vật hợp lệ.
- Nhân vật mới mở có badge "NEW" cho tới khi được chọn lần đầu.

---

## 6. Map & độ khó

### 6.1 Cấu trúc map

- **Một scene Arena duy nhất**, tham số hoá bằng `MapDefinition` (theme, tilemap, prop, spawn phase, hazard, boss, hệ số).
- Lý do: 3 scene riêng = 3 lần setup spawner/camera/HUD, dễ lệch. Một scene + data rẻ hơn và dễ test.
- Mỗi map là một đấu trường kín (arena) có tường/obstacle, không phải màn đi ngang.
- Kích thước arena mục tiêu: 40 × 40 unit; camera theo player; spawn ngoài camera.
- Khác biệt map đến từ: palette, prop, pool quái, hazard, boss, hệ số độ khó.

### 6.2 Bảng map

| # | Map | Chủ đề | Mốc boss | Boss | Mở khóa | Pool quái | Hazard | Hệ số vàng |
|---|---|---|---:|---|---|---|---|---:|
| 1 | **Verdant Graveyard** | Nghĩa địa xanh, đá rêu | 10:00 | Slime King | Mặc định | Slime, Skeleton, Wolf | Không | ×1.0 |
| 2 | **Weaver Cavern** | Hang nhện, tơ tím | 15:00 | Broodmother | Hạ Slime King | Spider, Skeleton, Wolf, elite Spider | Web làm chậm 30% | ×1.15 |
| 3 | **Frostfall Ruins** | Phế tích băng, xương trắng | 20:00 | Bone Warlord | Hạ Broodmother | Skeleton, Wolf, Spider, elite | Băng làm chậm 20% | ×1.3 |

**Map 4:** không nằm trong 8 tuần. Chỉ làm sau launch nếu người chơi muốn thêm (xem §17).

### 6.3 Timeline trong map

**Map 1 — Verdant Graveyard**

| Thời điểm | Nội dung |
|---|---|
| 0:00 | Slime; 1 quái / 2.0s |
| 2:00 | Thêm Skeleton; 1 / 1.8s |
| 4:00 | Bật horde; bắt đầu có prop vàng |
| 5:00 | Thêm Wolf; 2 / 1.8s |
| 8:00 | Elite pack: 2 elite Slime + 6 Slime |
| 10:00 | **Slime King** spawn; dừng spawn thường |

**Map 2 — Weaver Cavern**

| Thời điểm | Nội dung |
|---|---|
| 0:00 | Spider; 1 / 2.0s |
| 2:00 | Thêm Skeleton |
| 3:00 | Bật horde |
| 4:00 | Thêm Wolf |
| 7:00 | Elite Spider đầu tiên |
| 10:00 | Elite pack: 2 elite Spider + 8 Spider |
| 12:00 | Elite Skeleton |
| 15:00 | **Broodmother** spawn |

**Map 3 — Frostfall Ruins**

| Thời điểm | Nội dung |
|---|---|
| 0:00 | Skeleton; 1 / 2.0s |
| 2:00 | Thêm Wolf; bật horde |
| 4:00 | Thêm Spider |
| 7:00 | Elite Wolf |
| 12:00 | Elite Skeleton |
| 17:00 | Mixed horde: 12 quái trộn |
| 20:00 | **Bone Warlord** spawn |

### 6.4 Công thức scale trong run

```
m = số phút đã sống
HP quái      = baseHP × mapHP × (1 + 0.07 × m) × eliteHP
Sát thương   = baseDMG × mapDMG × (1 + 0.05 × m) × eliteDMG
Tốc chạy     = baseSpeed × min(1.30, 1 + 0.02 × m)
Nhịp spawn   = baseInterval × max(0.55, 1 - 0.03 × m)
```

| Map | mapHP | mapDMG | Ghi chú |
|---|---:|---:|---|
| 1 | 1.00 | 1.00 | Chuẩn |
| 2 | 1.35 | 1.15 | Quái dày hơn, hazard |
| 3 | 1.80 | 1.30 | Cuối game, cần build tốt |

**Elite:** scale 1.25×, tint màu, HP ×4, sát thương ×1.5, tốc chạy ×0.9, vàng ×5, XP chắc chắn là gem lớn. Dùng lại prefab gốc + `EnemyStats` override, không cần art mới.

**Horde:** tái sử dụng `HordeSpawnCycle` hiện có. Preset theo map:
- Map 1: bắt đầu 4:00, kiểm tra 20s, xác suất 25%, 10–18 quái.
- Map 2: bắt đầu 3:00, kiểm tra 18s, xác suất 30%, 12–22 quái.
- Map 3: bắt đầu 2:00, kiểm tra 15s, xác suất 35%, 14–26 quái.

### 6.5 Hazard

- **Web (map 2):** vùng bán kính 2.0, làm chậm player 30% trong 1.5s; spawn tối đa 6 vùng; có viền tím, không gây sát thương.
- **Ice (map 3):** vùng bán kính 2.5, làm chậm 20%; spawn tối đa 5 vùng; không gây sát thương.
- Hazard không spawn dưới chân player; không chồng quá 2 vùng.
- Nếu week 6 thiếu thời gian, hazard là mục đầu tiên bị cắt (xem §13.4).

---

## 7. Boss

### 7.1 Quy tắc chung

- Boss spawn đúng mốc, bất tử 1 giây đầu, gầm + rung nhẹ.
- Khi boss spawn: dừng spawn thường, huỷ horde đang chờ, xoá quái thường/horde còn sống (không rơi vàng/XP) để đấu boss sạch. Chỉ còn boss + adds do boss gọi.
- Hiện thanh HP boss ở đầu màn hình; vẫn giữ thanh HP player.
- Mọi đòn có **telegraph ≥ 0.5 giây**: đổi màu, vòng tròn dưới đất hoặc pose rõ.
- Không có đòn nào gây sát thương từ ngoài màn hình.
- Adds có cap; boss không summon thêm nếu đã đạt cap.
- Boss chết → toàn bộ adds chết theo → 1.5s sau hiện Victory.
- Boss không bị đẩy lùi (knockback), không bị choáng.

### 7.2 Bảng boss

| Boss | Map | HP | Sát thương chạm | Tốc chạy | Đòn | Phase |
|---|---|---:|---:|---:|---|---|
| **Slime King** | 1 | 2.200 | 10 | 2.2 | Slam, Dash, Split | Enrage ở 25% |
| **Broodmother** | 2 | 6.000 | 12 | 2.6 | Web Volley, Summon, Bite Dash | Enrage ở 40% |
| **Bone Warlord** | 3 | 12.000 | 15 | 2.0 | Sweep, Bone Ring, Summon | Phase 2 ở 50% |

### 7.3 Pattern

**Slime King** (art `Slime_king` đã có: Move / Jump / Dash / Split)
- **Slam (5s):** nhảy tới vị trí player, vòng AoE bán kính 2.5, 12 sát thương; telegraph 0.7s.
- **Dash (7s):** 3 lần lao nhanh, mỗi lần 10 sát thương; telegraph 0.5s mỗi lần.
- **Split (một lần ở 50% HP):** tách ra 4 Slime nhỏ (HP 50%, damage 50%); không hồi máu boss.
- **Enrage 25%:** cooldown đòn −25%, Dash thành 4 lần.

**Broodmother** (art Spider + tint, bụng trứng)
- **Web Volley (6s):** bắn 5 projectile tơ, mỗi cái tạo vùng chậm 4s; telegraph 0.6s.
- **Summon (12s):** gọi 4 Spider; tối đa 8 adds sống.
- **Bite Dash (8s):** 2 lần lao, 15 sát thương; telegraph 0.5s.
- **Enrage 40%:** Volley thành 2 đợt liên tiếp, cooldown −20%.

**Bone Warlord** (art Skeleton + giáp/crown)
- **Sweep (5s):** quét 180° bán kính 3.0, 18 sát thương; telegraph 0.6s.
- **Bone Ring (7s):** 12 projectile toả tròn, 10 sát thương; telegraph 0.7s.
- **Summon (14s):** gọi 3 Skeleton; tối đa 6 adds.
- **Phase 2 (50% HP):** tốc chạy ×1.3, Ring thành 18 projectile, cooldown −25%.

### 7.4 Phần thưởng boss

| Boss | Vàng | Unlock | First clear |
|---|---:|---|---|
| Slime King | 150 | Map 2 | +150 vàng |
| Broodmother | 250 | Map 3 + Cleric | +250 vàng |
| Bone Warlord | 400 | Endgame / credits | +400 vàng |

- Boss lặp lại vẫn rơi vàng đầy đủ, nhưng không cộng first clear.
- Boss không rơi XP (run kết thúc ngay).
- Sau khi hạ Bone Warlord: màn hình "ALL MAPS CLEARED", tổng kết tài khoản, nút về Camp.

---

## 8. Nâng cấp ngẫu nhiên trong run

### 8.1 XP và level

- XP rơi dưới dạng gem: small = 1, medium = 3, large = 8.
- Tỉ lệ quái thường: 75% small / 20% medium / 5% large. Elite: 100% large.
- `experienceCap` hiện có tăng dần:
  - Level 1–9: +3 mỗi level.
  - Level 10–19: +5 mỗi level.
  - Level 20+: +8 mỗi level.
  - Base cap = 5.
- Ước tính: ~level 20 ở phút 10; ~level 30 ở phút 20.
- Mỗi level: +1 lượt chọn thẻ. Nhiều level cùng lúc → xếp hàng, chọn lần lượt (đã có `PendingUpgrades`).

### 8.2 Pool thẻ

**4 vũ khí:**

> Số liệu dưới đây là **target cân bằng**, không phải giá trị asset hiện tại. Asset cũ (Magic Book 5 dmg, Sword range 0.5) sẽ được chỉnh khi làm W3.

| Vũ khí | Sát thương | Cooldown | Tầm / tốc độ | Pierce | Vai trò |
|---|---:|---:|---|---:|---|
| Sword | 20 | 0.8s | 150° / 0.6 | 0 | Melee, hit nhiều mục tiêu trong cung |
| Magic Book | 10 | 0.9s | projectile 6 | 2 | Ranged, xuyên nhẹ |
| Bow | 12 | 0.7s | projectile 9 | 1 | Ranged, nhanh, chính xác |
| Holy Aura | 6 | 0.5s | bán kính 2.2 | — | AoE quanh người, hit tất cả |

**Cấp vũ khí (áp dụng chung):**

| Level | Hiệu ứng |
|---|---|
| 1 | Base |
| 2 | +25% sát thương |
| 3 | −15% cooldown |
| 4 | +25% sát thương |
| 5 | Đặc biệt theo vũ khí |

**Đặc biệt level 5:**
- Sword: mỗi đòn thứ 3 là vòng 360°.
- Magic Book: bắn thêm 1 projectile.
- Bow: +1 pierce, tốc độ đạn +25%.
- Holy Aura: bán kính +30%, tick 0.5s → 0.35s.

**8 passive:**

| Thẻ | Hiệu ứng mỗi cấp | Max |
|---|---|---:|
| Might | +10% sát thương | +50% |
| Haste | −6% cooldown | −30% |
| Boots | +6% tốc chạy | +30% |
| Vitality | +8 máu tối đa, hồi ngay 8 | +40 |
| Regen | +0.3 máu/giây | +1.5/s |
| Magnet | +25% bán kính hút | +125% |
| Armor | −1 sát thương mỗi đòn | −5 (tối thiểu 1) |
| Growth | +8% XP | +40% |

**2 thẻ dự phòng** (chỉ xuất hiện khi không đủ 3 thẻ hợp lệ):
- **Feast:** hồi 50% máu tối đa.
- **Purse:** +30 vàng ngay.

Tổng pool chuẩn: 4 vũ khí + 8 passive = 12 thẻ, mỗi thẻ tối đa 5 cấp. Tổng 60 lượt nâng nếu muốn max tất cả — nhiều hơn số level trong một run, nên không bao giờ max hết.

### 8.3 Giới hạn build

- Tối đa **3 vũ khí** cùng lúc (tính cả vũ khí đầu).
- Khi đã có 3 vũ khí, thẻ vũ khí chưa sở hữu bị loại khỏi pool.
- Vũ khí đầu không thể bị thay; thẻ vũ khí mới thêm vào slot trống.
- Không cho trùng vũ khí.
- Mỗi vũ khí có level riêng, tối đa 5.

### 8.4 Thuật toán chọn thẻ

Mỗi lần level up:
1. Tạo pool hợp lệ: thẻ chưa max, vũ khí chưa sở hữu (nếu còn slot), passive.
2. Nếu người chơi có < 2 vũ khí và còn vũ khí chưa sở hữu: **ép 1 slot** là vũ khí chưa sở hữu (random).
3. Chọn 3 thẻ **khác nhau** theo trọng số:
   - Vũ khí chưa sở hữu: ×3.
   - Vũ khí đã sở hữu: ×1.5.
   - Passive: ×1.
4. Nếu bộ 3 thẻ trùng y hệt bộ trước đó: roll lại, tối đa 3 lần; sau đó chấp nhận (tránh vòng lặp).
5. Nếu < 3 thẻ hợp lệ: bù bằng thẻ dự phòng, không trùng nhau.
6. Mở modal, pause game; click hoặc phím 1/2/3.
7. Áp dụng ngay, đóng modal, chạy tiếp. Nếu còn `PendingUpgrades`, mở tiếp.

**Quy tắc chống trùng lặp:**
- Không 2 thẻ trùng id trong một offer (3 thẻ luôn khác nhau).
- Không lặp y hệt bộ 3 thẻ của offer liền trước.
- Thẻ đã max bị loại vĩnh viễn khỏi pool.
- Trọng số vũ khí mới cao hơn để run nào cũng có hướng build mới.

### 8.5 UI thẻ

- 3 card ngang, mỗi card: icon, tên, loại (WEAPON / PASSIVE), mô tả 2 dòng, cấp hiện tại → cấp sau (VD: `LV 2 → 3`).
- Màu: vũ khí xanh dương, passive xanh lá, dự phòng vàng.
- Card vũ khí mới ghi "NEW"; card max không bao giờ xuất hiện.
- Nút reroll **không có** ở launch (cắt scope).

---

## 9. Kinh tế & cân bằng

### 9.1 Tổng thu nhập kỳ vọng

| Map | Vàng/run (không first clear) | Vàng/giờ | Mục tiêu |
|---|---:|---:|---|
| 1 | ~410 | ~2.460 | Farm early, mua 3–5 cấp Forge |
| 2 | ~780 | ~3.120 | Farm mid, mở Cleric |
| 3 | ~1.340 | ~4.020 | Farm endgame, max Forge |

- Map 3 thưởng nhiều hơn vì khó hơn và dài hơn; không cần hệ số vàng riêng quá lớn.
- First clear là phần thưởng một lần, không phải nguồn farm.
- Người chơi chết sớm vẫn giữ vàng; không có "mất trắng" gây ức chế.

### 9.2 Mục tiêu cân bằng

| Chỉ số | Mục tiêu | Cách đo |
|---|---|---|
| Thời lượng run map 1 | 8–12 phút | Timer kết thúc |
| Thời lượng run map 2 | 12–17 phút | Timer kết thúc |
| Thời lượng run map 3 | 17–22 phút | Timer kết thúc |
| Tỉ lệ thắng map 1 | 60–80% sau 3 run | Telemetry |
| Tỉ lệ thắng map 2 | 50–70% sau 2 lần farm | Telemetry |
| Tỉ lệ thắng map 3 | 40–60% sau khi max gần đủ Forge | Telemetry |
| Thẻ bị chọn nhiều nhất | < 40% lượt chọn | Telemetry |
| Số thẻ khác nhau được chọn / 10 run | ≥ 8 | Telemetry |
| FPS trung bình | ≥ 60 ở 120 quái | Profiler |

### 9.3 Công thức sát thương

```
damage = weaponBase
       × (1 + metaMight)
       × (1 + inRunMight)
       × (characterMight / 4)
       × weaponLevelMultiplier
```

- `inRunMight` cộng dồn: 10%/cấp, tối đa +50%.
- `weaponLevelMultiplier`: L1 = 1.0; L2 = 1.25; L3 = 1.25; L4 = 1.5625; L5 = 1.5625 (cộng thêm special).
- Sát thương nhận vào: `max(1, enemyDamage - armor)`; i-frame 0.6s.
- Không có crit ở launch (cắt scope).

### 9.4 Công thức vàng

```
goldRun = (kills × 0.25) + (elites × 5) + (hordeKills × 1)
        + (props × 0.15) + bossGold + (minutes × 5)
goldRun = round(goldRun × (1 + metaFortune) × mapGold)
```

- `metaFortune` tối đa +40%.
- `mapGold`: 1.0 / 1.15 / 1.3.
- Làm tròn xuống; không bao giờ âm.

---

## 10. UX flow & màn hình

### 10.1 Luồng màn hình

```
BOOT (load save, 1s)
  ↓
CAMP
  ├─ tab FORGE
  ├─ tab CHARACTERS
  ├─ tab MAPS
  ├─ SETTINGS
  └─ START → ARENA
               ├─ LEVEL UP (modal)
               ├─ PAUSE (modal)
               └─ RESULT (modal)
                     └─ CAMP
```

### 10.2 Màn hình & nội dung

| Màn hình | Thành phần chính | Hành động |
|---|---|---|
| Boot | Logo, loading | Load save, vào Camp |
| Camp | Vàng hiện có, 3 tab, nút START | Điều hướng |
| Forge | 5 track, cấp hiện tại, giá, nút Buy | Mua nâng cấp |
| Characters | 4 card, chỉ số, điều kiện mở, chọn | Chọn / mua |
| Maps | 3 card, boss, thời lượng, best time, locked state | Chọn map |
| Settings | Âm lượng, fullscreen, screen shake, reset save | Lưu ngay |
| Run HUD | HP, XP, level, timer, vàng, boss HP, vũ khí | Theo dõi |
| Level-up | 3 card, queue count | Chọn 1 |
| Pause | Resume, Settings, Abandon | Dừng / bỏ run |
| Result | Victory/Defeat, thời gian, level, kills, vàng breakdown, unlock | Retry / Camp |

### 10.3 Chi tiết HUD

- Trên trái: thanh HP + số `current / max`.
- Trên giữa: timer `MM:SS`; dưới timer là thanh XP + level.
- Trên phải: vàng run + tổng vàng (nhỏ).
- Dưới cùng trái: 3 icon vũ khí + level.
- Boss: thanh HP lớn giữa trên, tên boss, phase marker.
- Không hiện damage number, không hiện kill count trong HUD (chỉ ở Result).

### 10.4 Onboarding

- Run đầu tiên hiện 3 tooltip một lần:
  1. "WASD to move. Mouse to aim."
  2. "Hold left click to attack."
  3. "Collect XP. Choose 1 of 3 upgrades."
- Không có tutorial level riêng; tooltip tự tắt sau 3 giây hoặc khi người chơi làm đúng.
- Lần đầu mở Forge: tooltip "Gold is permanent. Spend it before your next run."

---

## 11. Save & telemetry

### 11.1 Save data

Đường dẫn: `Application.persistentDataPath/system-leveling-save.json`.

```json
{
  "version": 1,
  "gold": 0,
  "meta": { "might": 0, "vitality": 0, "agility": 0, "wisdom": 0, "fortune": 0 },
  "characters": ["mage"],
  "maps": ["map1"],
  "bossKills": { "map1": 0, "map2": 0, "map3": 0 },
  "bestTime": { "map1": 0, "map2": 0, "map3": 0 },
  "totalRuns": 0,
  "settings": {
    "masterVolume": 0.8,
    "sfxVolume": 0.8,
    "musicVolume": 0.6,
    "screenShake": true,
    "fullscreen": true
  }
}
```

**Quy tắc:**
- Ghi atomic: ghi `*.tmp` → flush → replace file chính; giữ `*.bak` một bản.
- Nếu file chính hỏng: thử `.bak`; nếu cả hai hỏng: tạo save mới, hiện cảnh báo, không crash.
- Lưu sau: kết thúc run, mua Forge, mở khóa, đổi setting.
- Không ghi save giữa run; vàng của run được chốt vào save ở Result. Nếu thoát ngang hoặc crash trước Result, vàng của run đó mất.
- `version` để migrate; hiện chỉ version 1.

### 11.2 Telemetry local

Mỗi run ghi một file `run_YYYYMMDD_HHMMSS.json` trong `persistentDataPath/runs/`:

```json
{
  "map": "map1",
  "character": "mage",
  "result": "victory",
  "durationSec": 642,
  "level": 21,
  "kills": 512,
  "gold": 430,
  "damageDealt": 18400,
  "damageTaken": 64,
  "cards": [
    { "level": 2, "id": "magic_book" },
    { "level": 3, "id": "might" }
  ],
  "bossKillSec": 58,
  "deathCause": null
}
```

- Chỉ lưu local, không gửi mạng.
- Dùng để cân bằng tuần 7: thẻ nào bị bỏ qua, run nào quá dài, chết ở đâu.
- Giới hạn 100 file gần nhất, tự xoá file cũ.

---

## 12. Kiến trúc kỹ thuật

### 12.1 Tận dụng code hiện có

| Hiện có | Giữ / sửa | Việc cần làm |
|---|---|---|
| `PlayerStats` | Giữ, mở rộng | Nối `might`, `projectileSpeed`; thêm `armor`, `xpMultiplier`, `goldMultiplier`; `ApplyMeta()`; reset khi vào run |
| `CharacterScriptableObject` | Giữ, mở rộng | Thêm `id`, `displayName`, `startingWeapon`, `unlockType`, `unlockValue`; gán weapon cho Mage |
| `WeaponScriptableObject` | Giữ | Thêm `id`, `displayName`, `icon`, `maxLevel`, `specialLevel` |
| `WeaponController` / `MagicBookController` | Giữ, refactor nhẹ | Thêm `WeaponInstance` (level, multiplier); dùng lại MagicBook cho Bow; thêm `AuraWeaponController` |
| `EnemySpawner` / `HordeSpawnCycle` | Giữ | Nạp `SpawnPhase` + horde preset từ `MapDefinition`; thêm elite prefab |
| `EnemyStats` / `Enemy` | Giữ | Thêm multiplier cho elite; không đổi AI cơ bản |
| `LevelUpSelection` | Giữ UI, đổi nguồn dữ liệu | Feed 3 `UpgradeCard` từ `UpgradeDraft`; bỏ enum cứng 4 loại |
| `GameOverController` | Mở rộng | Thêm victory, breakdown vàng, về Camp; restart theo `RunConfig` |
| `PlayerHud` | Mở rộng | Thêm timer, vàng, boss bar, weapon icons |
| `DropRateManager` / `Experience` | Giữ | Thêm `GoldPickup` cùng interface `ICollectible`; mở rộng `PlayerCollector` để hút vàng |
| `PlayerCollector` | Mở rộng | Nhận cả `Experience` và `GoldPickup`; giữ nguyên pull speed/radius |
| Scene hiện tại | Tách | `Boot` (manager), `Camp` (menu), `Arena` (gameplay) |

### 12.2 Thành phần mới

- `SaveManager` — load/save atomic, backup, migration.
- `MetaProgression` — áp dụng 5 track Forge vào `PlayerStats`.
- `RunManager` — state `Playing / Draft / Paused / Result`, timer, victory/defeat, bank vàng.
- `RunConfig` — `DontDestroyOnLoad`, giữ map + character + seed giữa các scene.
- `MapDefinition` (SO) — theme, tilemap, spawn phases, hazard, boss, hệ số.
- `CharacterDefinition` (SO hoặc mở rộng SO hiện có) — stat + unlock.
- `UpgradeDefinition` (SO) — id, loại, mô tả, max level, hiệu ứng.
- `UpgradeDraft` — pool, trọng số, chống trùng, fallback.
- `WeaponLoadout` — 3 slot, thêm/xoá, level từng vũ khí.
- `GoldPickup` — vàng rơi, hút, cộng ngay.
- `BossController` + các `BossAttack` (Slam, Dash, Volley, Summon, Ring, Sweep).
- `BossHealthBar` — UI.
- `AuraWeaponController` — vũ khí AoE.

### 12.3 Cấu trúc scene & data

```
Assets/Scenes/
  Boot.unity
  Camp.unity
  Arena.unity

Assets/Scriptable Object/
  Characters/   Mage, Knight, Ranger, Cleric
  Maps/         Map1_Graveyard, Map2_Cavern, Map3_Ruins
  Upgrades/     12 card + 2 fallback
  Bosses/       SlimeKing, Broodmother, BoneWarlord
  Weapons/      Sword, MagicBook, Bow, Aura
```

- `Arena` là scene duy nhất chứa gameplay; `RunConfig` quyết định map nào được load.
- `Boot` chứa `SaveManager`, audio, `RunConfig`; không unload.
- Build Settings: Boot → Camp → Arena.
- Không thêm package mới. Dùng Input System, UGUI, Tilemap, 2D hiện có. Không DOTween; dùng coroutine cho animation UI.

### 12.4 Những điểm dễ vỡ

- **`Time.timeScale`:** hiện `LevelUpSelection` và `GameOverController` cùng quản lý. Phải có một `RunState` duy nhất; chỉ một owner được set timescale. Test: pause → level up → chết → resume.
- **Restart:** `GameOverController.Restart()` đang reload scene hiện tại. Với Camp, phải reload `Arena` và giữ `RunConfig`; nếu không sẽ mất map/character.
- **Stat chưa nối:** `might`, `projectileSpeed` hiện chưa được dùng trong tính sát thương/tốc đạn. Nối trước khi làm 4 nhân vật, nếu không nhân vật chỉ khác HP/speed.
- **Spawner reset:** `EnemySpawner` cố tình không reset trong `OnEnable`; khi chuyển map phải tạo scene mới hoặc reset tường minh qua `RunManager`.
- **Pooling:** giữ Instantiate/Destroy ở launch; chỉ thêm pool nếu week 5 không đạt 60 FPS với 120 quái. Đừng tối ưu sớm.

---

## 13. Kế hoạch 8 tuần

### 13.1 Giả định

- 1 dev, 30 giờ/tuần, 6 ngày/tuần.
- Ngày 1: khởi tạo git (repo hiện chưa có) + `.gitignore` cho Unity (`Library/`, `Temp/`, `Logs/`, `UserSettings/`, `*.csproj`, `*.slnx`); commit mỗi ngày.
- Có sẵn: player, 2 vũ khí, 4 quái + Slime King, spawner, HUD, game over.
- Art placeholder được phép tới hết week 5; thay art thật ở week 6.
- Mỗi ngày kết thúc bằng một playmode test 10 phút; build xanh mới commit.

### 13.2 Lịch tuần

| Tuần | Ngày | Trọng tâm | Deliverable | Gate |
|---|---|---|---|---|
| **W1** | 22–28/10 | Nền tảng meta | SaveManager + schema; GoldPickup; vàng HUD; Result screen; Forge UI 5 track; Camp shell | **G1:** chết → nhận vàng → mua Forge → run sau mạnh hơn → save qua restart |
| **W2** | 29/10–04/11 | Vertical slice | `MapDefinition` + Arena; BossController + Slime King; Victory flow; mở map 2; boss HP bar | **G2:** chơi trọn map 1 từ Camp → boss → victory → Camp |
| **W3** | 05–11/11 | Build diversity | WeaponInstance + 4 vũ khí; 3 slot; 12 card + 2 fallback; draft + chống trùng; 8 passive | **G3:** 5 run liên tiếp có build khác nhau; không có offer trùng/lỗi |
| **W4** | 12–18/11 | Nhân vật & unlock | 3 nhân vật mới; màn chọn; mua Knight; khung mission + debug unlock; toast unlock | **G4:** 4 nhân vật chạy đúng stat/vũ khí (test bằng debug unlock); mua Knight và lưu unlock đúng; mission survival/boss kiểm ở W5 |
| **W5** | 19–25/11 | Map 2 | MapDefinition 2; pool + timeline; elite; web hazard; Broodmother; map select; mission Ranger/Cleric | **G5:** map 2 thắng được, mở map 3; Ranger mở ở 12:00, Cleric mở sau Broodmother; 60 FPS ở 120 quái |
| **W6** | 26/11–02/12 | Map 3 + content freeze | MapDefinition 3; ice hazard; Bone Warlord; mixed horde; art pass; credits screen | **G6:** 3 map chơi end-to-end; content freeze; không thêm feature |
| **W7** | 03–09/12 | Cân bằng & playtest | Telemetry review; chỉnh HP/vàng/XP; onboarding; settings; SFX; bug save | **G7:** 8–10 tester ngoài; map 1 clear 60–80% sau 3 run; không softlock |
| **W8** | 10–16/12 | RC & buffer | Bugfix only; performance; build sạch; test trên máy khác; đóng gói | **G8:** 30 phút không crash; save nguyên vẹn; build chạy không cần Editor |

**Ngày release mục tiêu:** 16/12/2026. Nếu trượt gate quá 2 ngày, áp dụng danh sách cắt ở §13.4 ngay, không dồn việc.

### 13.3 Phân bổ giờ

| Nhóm | Giờ | Ghi chú |
|---|---:|---|
| Code gameplay | 95 | Meta, draft, weapon, boss, map data |
| UI/UX | 35 | Camp, HUD, results, settings |
| Art/audio | 45 | 3 nhân vật, 2 boss biến thể, prop, SFX |
| Balance/playtest | 30 | Tuần 7 chủ yếu |
| QA/bugfix | 20 | Tuần 8 |
| Buffer | 15 | Dự phòng |
| **Tổng** | **240** | |

### 13.4 Danh sách cắt scope (theo thứ tự)

Cắt từ trên xuống khi trượt gate:

1. Hazard web/ice (giữ map khác nhau bằng palette + pool quái).
2. Elite variants (dùng quái thường dày hơn).
3. Cleric (còn 3 nhân vật).
4. First-clear bonus phụ (vẫn giữ vàng boss cơ bản).
5. Aura weapon (còn 3 vũ khí).
6. Onboarding tooltip (thay bằng text ở Camp).
7. Map 4 (vốn đã ngoài launch).

**Không được cắt:** 3 map, 3 boss, 3 nhân vật, 10 thẻ nâng cấp, vàng + Forge, save, 3 vũ khí, vòng lặp Camp → Run → Result.

Nếu vẫn không kịp với mức tối thiểu: giảm thời lượng map 3 từ 20 → 15 phút, không bỏ map.

---

## 14. Ngân sách art & audio

### 14.1 Art

| Hạng mục | Số lượng | Nguồn | Deadline |
|---|---:|---|---|
| Nhân vật | 4 bộ (idle/walk 6–8 frame) | Pixel art mới; Mage có sẵn | W4 |
| Vũ khí | 4 icon + 4 projectile/slash | Tái sử dụng + chỉnh màu | W3 |
| Quái | 5 bộ có sẵn | Blue Slime, Skeleton, Spider, Wolf | Đã có |
| Elite | 4 tint/scale | Biến thể từ quái có sẵn | W5 |
| Boss | Slime King có sẵn; Broodmother = Spider + trứng; Bone Warlord = Skeleton + giáp/crown | Biến thể pixel | W6 |
| Map | 1 tileset dungeon + 3 palette + 10 prop + 2 hazard decal | Tái sử dụng tối đa | W6 |
| UI | 9-slice panel, 3 card frame, icon HP/XP/gold | Kenney/UI có sẵn + chỉnh | W2 |
| VFX | slash, hit flash, level-up, boss telegraph | Sprite đơn giản | W4 |

**Quy tắc:** mọi sprite quái giữ chuẩn project (cao ~1 unit, 3/4 top-down, outline 1px, palette trầm) theo `docs/Monster_PixelArt_Prompts.md`.

### 14.2 Audio

| Loại | Số lượng | Ghi chú |
|---|---:|---|
| SFX | 10 | hit, slash, shoot, pickup XP, pickup gold, level up, boss roar, player hurt, death, UI click |
| Music | 3 loop | Camp, map thường, boss; có thể dùng free asset, 1 loop + layer boss |
| Ambience | 0 | Cắt scope |

- Luôn có nút mute + 3 thanh volume.
- SFX quan trọng hơn music ở tuần 7; nếu thiếu thời gian, dùng 1 music loop.

---

## 15. QA & tiêu chí chấp nhận

### 15.1 Acceptance criteria

| ID | Tiêu chí |
|---|---|
| AC-01 | Save mới: chỉ Mage + map 1, 0 vàng, Forge level 0 |
| AC-02 | Giết quái nhặt vàng; chết giữ 100% vàng đã nhặt; Result hiện breakdown |
| AC-03 | Mua Forge trừ đúng vàng, áp dụng ngay run sau, lưu qua restart |
| AC-04 | Mỗi level up hiện đúng 3 thẻ khác nhau; không lặp y hệt offer trước; thẻ max bị loại; đủ fallback khi thiếu |
| AC-05 | 4 vũ khí hoạt động; tối đa 3 slot; level 1–5; level 5 có special đúng mô tả |
| AC-06 | 4 nhân vật đúng stat + vũ khí đầu; unlock đúng điều kiện; lưu qua restart |
| AC-07 | 3 map mở đúng thứ tự; boss spawn đúng 10/15/20 phút; chỉ thắng sau khi boss chết |
| AC-08 | Mọi đòn boss có telegraph ≥ 0.5s; không có sát thương ngoài màn hình; phase đúng ngưỡng |
| AC-09 | Pause, level-up, result dừng timer/spawn/đạn; resume đúng; không softlock |
| AC-10 | Save hỏng: fallback `.bak`; cả hai hỏng: tạo save mới, cảnh báo, không crash |
| AC-11 | 60 FPS trung bình ở 120 quái trên máy target; không freeze > 1s |
| AC-12 | Victory/defeat không cộng trùng vàng, không mở sai map, không kẹt modal |

### 15.2 Test matrix

| Case | Cách test | Kỳ vọng |
|---|---|---|
| Fresh save | Xoá save, chạy | Camp đúng trạng thái, không lỗi |
| Chết sớm | Vào map 1, đứng yên | Result defeat, vàng giữ, về Camp được |
| Thắng map 1 | Chơi tới boss | Victory, mở map 2, first clear +150 |
| Pause khi level up | Level up → Esc | Không mở 2 modal, timescale đúng |
| Alt-tab | Alt-tab 30s | Không nhân đôi timer/spawn |
| Save corruption | Sửa file thành rác | Fallback backup, không crash |
| 120 quái | Spawn horde ở map 3 | FPS ≥ 60, không tụt bất thường |
| Max thẻ | Chơi tới khi hết pool | Fallback xuất hiện, không treo |
| 3 slot | Nhặt 4 vũ khí | Vũ khí thứ 4 không xuất hiện trong offer |
| Restart | Thắng/thua rồi Retry | Đúng map + nhân vật, timer reset |

### 15.3 Playtest

- 8–10 người ngoài, 2 buổi: buổi 1 quan sát im lặng, buổi 2 hỏi cảm nhận.
- Ghi lại: thời gian tới level 5, số lần chết, thẻ được chọn, chỗ bối rối.
- Không sửa design trong buổi test; chỉ ghi chú, sửa sau.
- Gate: ≥ 70% tester hiểu cách chơi trong 2 phút đầu; ≥ 60% muốn chơi run thứ 2.

---

## 16. Rủi ro

| Rủi ro | Khả năng | Ảnh hưởng | Giảm thiểu | Dấu hiệu |
|---|---|---|---|---|
| Scope 4 nhân vật × 3 map quá lớn | Cao | Trễ release | Reuse art, cắt theo §13.4, content freeze W6 | Trượt G3/G4 > 2 ngày |
| Cân bằng nổ (quái quá khỏe/yếu) | Cao | Game không vui | Công thức có cap, telemetry, chỉnh W7 | Map 1 win rate < 40% hoặc > 90% |
| Boss AI lỗi/không né được | Trung bình | Ức chế | Telegraph bắt buộc, test từng đòn, không đòn ngoài màn hình | Tester chết liên tục không hiểu lý do |
| Save hỏng | Thấp | Mất tiến trình | Atomic write + backup + test phá file | File JSON lỗi sau crash |
| Art thiếu | Trung bình | Map/nhân vật na ná | Placeholder tới W6, palette swap có chủ đích | W5 chưa có 3 nhân vật |
| Performance 120 quái | Trung bình | Giật | Cap 120, profiler W5, pooling chỉ khi cần | FPS < 50 ở horde |
| Time.timeScale xung đột | Trung bình | Softlock | Một `RunState` duy nhất, test chéo modal | Pause/level-up chồng nhau |
| Hiểu sai "2 tháng" | Thấp | Sai loại kế hoạch | Ghi rõ giả định §0; nếu cần bản live-content, mở rộng §17 | User yêu cầu daily quest/season |

---

## 17. Quyết định đã chốt & còn mở

### Đã chốt

- 3 map launch; map 4 chỉ sau launch nếu có nhu cầu.
- 4 nhân vật, 4 vũ khí, 3 slot, 12 thẻ + 2 fallback.
- Một loại tiền: vàng; không IAP, không loot box.
- Vàng giữ 100% khi chết; meta không mất.
- UI tiếng Anh; tài liệu tiếng Việt.
- PC Windows, keyboard + mouse, 16:9.
- Một scene Arena + `MapDefinition`; không làm 3 scene riêng.
- Không online, không cloud, không multiplayer, không procedural map.

### Còn mở (cần chốt trước W2)

1. Tên thương mại: giữ "System Leveling" hay đổi? (ảnh hưởng logo/store, không ảnh hưởng code)
2. Có phát hành miễn phí hay bán? (ảnh hưởng store page, không ảnh hưởng design)
3. Nếu mục tiêu là **2 tháng chơi liên tục**, có thêm post-launch không?
   - Phương án rẻ: 1 map 4 + 1 boss + chế độ "weekly challenge" dùng seed cố định.
   - Phương án đắt: daily quest, season pass, leaderboard → cần thêm 4–6 tuần, ngoài phạm vi bản này.

---

## Phụ lục A — Công thức & hằng số

```
# Scale trong run
m = elapsedMinutes
enemyHP    = baseHP    × mapHP    × (1 + 0.07m) × eliteHP(1|4)
enemyDMG   = baseDMG   × mapDMG   × (1 + 0.05m) × eliteDMG(1|1.5)
enemySpeed = baseSpeed × min(1.30, 1 + 0.02m) × eliteSpeed(1|0.9)
spawnEvery = baseEvery × max(0.55, 1 - 0.03m)

# Player
damage = weaponBase × (1 + metaMight) × (1 + inRunMight)
       × (characterMight / 4) × weaponLevelMult
damageTaken = max(1, enemyDamage - armor)
iFrame = 0.6s
levelCap = 5 + 3/5/8 per level range (xem §8.1)

# Gold
gold = kills×0.25 + elites×5 + hordeKills + props×0.15
     + bossGold + minutes×5
gold = floor(gold × (1 + metaFortune) × mapGold)

# Boss
slimeKing  = 2200 HP, 10 contact, 12 slam, 10 dash
broodmother= 6000 HP, 12 contact, 15 bite, web slow
boneWarlord= 12000 HP, 15 contact, 18 sweep, 10 ring
```

**Cap quan trọng:**
- max 3 vũ khí; max 5 level/vũ khí; max 5 level/passive.
- max 120 quái thường; max 8 adds/boss.
- max 6 hazard/map 2; max 5 hazard/map 3.
- armor giảm tối đa 5, sát thương nhận tối thiểu 1.
- i-frame 0.6s; không cộng dồn.

---

## Phụ lục B — Checklist trước khi coi là "playable"

- [ ] Camp → chọn map/nhân vật → Arena → chết/thắng → Result → Camp, không lỗi.
- [ ] Vàng nhặt được, bank, mua Forge, áp dụng run sau.
- [ ] 3 map mở đúng thứ tự; 3 boss spawn đúng mốc; victory đúng.
- [ ] 4 nhân vật chọn được; 3 nhân vật unlock đúng điều kiện.
- [ ] 12 thẻ + 2 fallback hoạt động; không offer trùng; max level bị loại.
- [ ] 4 vũ khí, 3 slot, level 5 special đúng.
- [ ] Save qua restart; phá file save không crash.
- [ ] Pause/level-up/result không softlock, timescale đúng.
- [ ] 60 FPS ở 120 quái trên máy target.
- [ ] Build sạch chạy trên máy không cài Unity.
