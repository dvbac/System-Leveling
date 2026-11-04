# Frostfang Alpha — thiết kế
Status: Frozen. Approval: Approved by user 2026-11-04. Based on PRD 2026-11-04. Owner: main agent.

## Hiện trạng đã kiểm tra
- RotwoodBoss/RotwoodEncounter là mẫu runtime riêng, không dùng Enemy/EnemyAnimation của quái thường.
- EnemyStats.Kill hiện chỉ chờ BeginDeath của Rotwood hoặc EnemyAnimation; Frostfang cần nhánh riêng tối thiểu, giữ nguyên reward/drop/idempotency.
- ProjectileWeaponBehaviour tìm EnemyStats trên collider cha; không cần sửa đường đạn. Melee dùng cùng stats.
- ApplySlow giữ nguyên stat nâng cấp. GameOverController.Win đã có flow victory/restart.
- Sprite đầu vào: idle 1024x128, Move 1408x128; Pounce 2304x128; Breath 2048x128; Pillars 2050x205; Howl 3280x205; Death 2460x205. Cần kiểm tra phân ô thực tế, không đoán số frame từ prompt.

## Modules / contracts đề xuất
- FrostfangBoss: state machine đòn đánh, motion playback theo thời gian scaled, collider EnemyStats, BeginDeath trả thời lượng cho EnemyStats.Kill.
- FrostfangEncounter: clock/spawn once, tìm ground theo các chunk và obstacle mask của map, HUD boss, kết thúc trận. Không đưa boss vào pool.
- Effect/pillar runtime nhỏ: animation độc lập với collider/hitbox, lifetime có chủ sở hữu, warning rồi hit; không dùng hạt bay để tính damage.
- FrostfangBossBuilder: normalize/slice sprite copies với SpriteDataProvider API hiện có, import FX riêng, tạo prefab/data thiếu và gắn encounter SnowScene. Lần chạy lại giữ chỉnh tay, không tạo trùng.
- FrostfangBossRegression: gọi production tick/combat, kiểm tra trạng thái, geometry, pause/death/cleanup và flow thắng/restart/thua; SoulTestIsolation bảo vệ ví thật.

## Tính đúng / tương thích
Khóa vị trí/hướng khi báo đòn. Di chuyển vồ dùng sweep chống xuyên tường và kiểm tra ground. State chết terminal, không phát thêm đòn; damage ticks có interval rõ. Scaled time cho pause. Hiệu ứng và HUD được dọn theo encounter/scene. Không singleton hoặc shared boss framework mới.

## Recovery / release
Không có điểm spawn an toàn thì retry, không ép spawn trong vật cản. Thiếu asset hoặc scene đang dirty thì dừng setup và báo lỗi. Backup SnowScene trước write; không sửa SampleScene hoặc source PNG. Không thay API save hay thêm dependency.

## Ranh giới kiểm thử / rủi ro
Sự an toàn của sửa EnemyStats cần chứng minh qua Frostfang death, Rotwood regression và quái thường; chưa được chứng minh runtime ở thời điểm draft. Geometry telegraph/damage, source hash và setup idempotency cần assertion/read-back. Tuning và build ngoài Editor deferred theo PRD.
Asset read-back: Move có 8 ô rộng 176px; Pounce có 12 ô rộng 192px; các sheet 205px chỉ có padding khác, hình wolf đầu tiên đều 121x115px. Normalize sang 192x160, không scale hình wolf, giữ baseline/arc tương đối; pivot (0.5,0.125), PPU48. Không có technical blocker.
