# Thiết kế spawn sinh tồn và Horde

Status: Frozen
Approval: User "duyệt", 2026-10-22

## Thiết kế
- Giữ EnemySpawner làm entry point trong scene, player/container/camera được gắn Inspector. Giữ/migrate serialized references hiện có, không để spawner cũ chạy song song.
- Cấu hình progression thường bằng danh sách phase có startTime, interval, batch và danh sách prefab/trọng số; mỗi phase tự chứa pool hiện hành để dễ chỉnh mở khóa/tỷ lệ.
- Horde có state machine thuần logic tách random/time input để kiểm tra xác suất và deadline một cách xác định. Các tham số xác suất dùng đơn vị phần trăm 0–100, log rõ random roll và tăng theo điểm phần trăm.
- Lớp1 thành công dự trữ số lượng trong cap Horde; chờ interval lớp2 đầu tiên. Mỗi lần thất bại cộng chanceIncrement; deadline ưu tiên trước phép quay. WaitingForClear chặn lớp1 cho tới khi đoàn hết quái sống.
- Hai danh sách population/cap riêng; quái chết đang phát Die không tính là còn sống. EnemyStats bổ sung getter IsDead tối thiểu; không thay Damage/AI/animation và không thêm pooling vì Destroy/Die hiện tại chưa có reset contract.
- Placement dùng camera orthographic đang hoạt động, margin ngoài màn hình, khoảng cách tối thiểu với player, radius/separation, optional allowed Tilemap/area và obstacle LayerMask. Scene hiện chưa có TilemapCollider2D/CompositeCollider2D, nên không giả định mọi hình trang trí là collider; tác giả cần cấu hình mask/ground phù hợp.
- Chọn anchor một phía lúc Horde được triển khai; bố trí cụm, kiểm tra từng vị trí theo camera/player hiện tại. Thử có giới hạn, không Instantiate để thử vị trí. Tạo tối đa ngân sách/frame; normal và Horde có budget riêng để Horde không bỏ đói normal.
- Runtime clamp/OnValidate chống interval<=0, chance ngoài [0,100], min>max, cap nhỏ hơn đoàn, weight rỗng/null hoặc không dương. Prefab thiếu Enemy/EnemyStats bị loại kèm cảnh báo một lần; không vòng lặp random vô hạn.
- Mọi timer dùng scaled gameplay time; không chạy khi Time.timeScale=0 hoặc GameOverController.GameplayStopped. Restart tạo spawner mới và reset các state/population.

## Kiểm tra/migration
- Cập nhật ReferenceEnemyRegression dùng cấu hình mới nhưng vẫn chứng minh spawner tạo được từng prefab thật.
- Cập nhật GameOverRegression tránh phụ thuộc tên private spawnTimer cũ: kiểm tra public elapsed/timer diagnostics và actual count không đổi khi Game Over.
- Không thay dữ liệu enemy/character/weapon; không sửa UI ngoài phạm vi. Test data theo Mage runtime hiện có.

## Hợp đồng triển khai
EnemySpawner giữ player/enemyContainer và legacy prefab references (ẩn) để không mất dữ liệu. Cấu hình mới dùng SpawnPhase, WeightedEnemySpawn, HordeSpawnSettings, SpawnPlacementSettings; HordeSpawnCycle.Tick nhận scaled delta, trạng thái capacity và nguồn roll để kiểm tra xác định. Diagnostics public: ElapsedTime, NormalSpawnTimer, HordeState, HordeChance, NormalAliveCount, HordeAliveCount. EnemyStats chỉ thêm IsDead getter.
