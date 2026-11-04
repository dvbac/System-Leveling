# Rotwood — Technical Design

Status: Frozen. Approval: User approved on 2026-11-01. Based on PRD 2026-11-01.

## Hiện trạng
- EnemyStats là điểm nhận sát thương chung của melee và projectile; có IsDead và adapter EnemyAnimation tùy chọn.
- EnemySpawner có giờ survival/horde riêng, placement có kiểm tra ground/collider. Không sửa cấu hình của nó.
- GameOverController kiểm soát GameplayStopped; các hành vi combat đã dùng cờ này.
- Sprite nguồn có ô 128px nhưng số rect tự cắt không đồng đều. Tạo bản asset runtime riêng, không sửa sprite nguồn.

## Thiết kế
- RotwoodBoss: state machine Chase -> Root/Charge/Summon -> Recovery/Exposed -> Chase; Death là terminal. Serialized reference tới EnemyStats, sprite sequences/audio; không dùng EnemyAnimation để tránh nó ghi đè đòn boss.
- RotwoodEncounter: timer 1020s (17 phút), spawn một lần tại vị trí ground hợp lệ gần người chơi, quản lý UI máu, nút thử Editor.
- Root hazard/telegraph: có vòng báo, khóa vị trí trước damage, scale theo thời gian gameplay; hủy khi boss chết/game-over. Slow có thời hạn, không ghi đè stat nâng cấp của người chơi.
- EnemyStats: thêm hook optional cho hệ số damage/Death của Rotwood, giữ nhánh enemy thường nguyên vẹn.
- GameOverController: thêm kết quả thắng dùng cùng panel và nút có sẵn; GameplayStopped cũng dừng thắng. Không đổi hành vi thua/restart/menu.
- PlayerController: nhân tốc độ với slow tạm thời; stat gốc không bị thay đổi.
- Editor builder: asset generated riêng, prefab/data boss, encounter trong SampleScene; giữ thay đổi người dùng và chỉ save scene đã mở đúng SampleScene.

## Hợp đồng và an toàn
Không thay chữ ký EnemyStats.TakeDamage/Kill. Không đưa boss vào weighted pools. Không reset đồng hồ spawn cũ.
Mọi đồng hồ gameplay dùng scaled delta và GameplayStopped; cleanup rễ/sói khi death/unload. Win gọi một lần; khi player chết trước, không ghi đè kết quả thua.
API Unity phải được kiểm tra qua reflection trước khi dùng; kiểm tra compile trước setup.

## Validation và rollback
Builder lặp lại không tạo encounter trùng. Play Mode regression kiểm tra timer, đòn báo trước/hit/miss, slow hết hạn, cap sói, exposed damage, death/win/pause và nhánh enemy cũ. Screenshot UI boss.
Gỡ encounter và asset generated để bỏ boss; không xóa/ghi đè source art hoặc công việc khác.
