# Vũ khí: sách phép, cung, nỏ — đạn / nạp / tích lực

Status: Frozen
Approval: User "chốt", 2026-10-29 — toàn bộ khuyến nghị D001/D002/D003, preset, HUD và nâng cấp đã trình bày được chấp thuận. Các nhãn "đề xuất/chờ" dưới đây là lịch sử bản nháp, nay được giải quyết theo cột khuyến nghị.
Last updated: 2026-10-29

## Bối cảnh / mục tiêu
Thêm cung và nỏ bằng sprite đã có; phân biệt nhịp chiến đấu qua đạn, nạp và tích lực. Giữ kiếm tương thích. Tất cả số cân bằng chỉnh được Inspector, không lưu trạng thái runtime lên ScriptableObject.

Facts đã kiểm tra:
- WeaponController đang tự Attack theo cooldown; MagicBookController kế thừa cơ chế đó. Chưa có ammo/reload/charge.
- MeleeWeaponController xử lý click chuột riêng; trường speed của kiếm là tốc độ vung, không phải tốc độ đạn. Không áp dụng logic đạn hoặc đổi đơn vị lên kiếm.
- WeaponScriptableObject có damage/speed/cooldown/pierce/prefab, chưa có trường đạn.
- ProjectileWeaponBehaviour đọc damage/speed từ data riêng trên projectile; cần đồng bộ với vũ khí bắn ra để không mất sát thương tích lực hoặc nâng cấp.
- PlayerStats/LevelUpSelection hiện chỉ có Damage, MoveSpeed, Magnet, Vitality. StartingWeapon có khai báo nhưng chưa được dùng để trang bị.
- Có sprite Base/Bow.png, Cross_bow.png, Magic_book.png; Hit/Arows1.png và sprite phép. Không cần thêm thư viện.
- Git có chỉnh sửa do người dùng ở cả 3 prefab nhân vật; WeaponHolder.prefab và Arows1.png/meta chưa tracked. Phải bảo toàn, không tạo lại prefab nhân vật từ đầu.

## Phạm vi và người dùng
Người chơi trong gameplay hiện có. Sửa hệ thống ranged, tạo weapon/projectile prefabs và data, nối trang bị theo quyết định D002, thêm nâng cấp liên quan, phản hồi đạn/nạp/tích lực tối thiểu trên HUD; regression kiếm, HUD, pause/Game Over/Restart. Không làm inventory, loot ammo, chế tạo, multiplayer, animation nhân vật mới hoặc character-select menu.

## Yêu cầu
| ID | Hành vi | Trạng thái |
| --- | --- | --- |
| FR-001 | Sách phép có 8 đạn cơ bản, nạp cả băng, sát thương/nhịp bắn/tốc độ đạn trung bình. | User required; số cụ thể đề xuất |
| FR-002 | Cung có 7 tên cơ bản, bắn nhanh, damage cao, tên nhanh. Chỉ nạp khi ngừng bắn, từng tên một, tốc độ nạp thấp hơn tốc độ bắn. | User required; cách nhận diện ngừng bắn chờ D001 |
| FR-003 | Nỏ luôn có 1 tên; giữ để tích lực, thả để bắn; cơ bản đạt tối đa sau khoảng 3s, tăng damage và kích thước hình/collider tên theo lực. | User required; hành vi giữ tại trần đề xuất chờ thả |
| FR-004 | Nâng cấp ammo tăng sức chứa sách/cung; với nỏ tăng trần thời gian tích lực, không tăng số tên. | User required; giá trị lợi ích chờ D003 |
| FR-005 | Nâng cấp reload tăng tốc nạp; với nỏ còn giảm thời gian thực để đạt trần tích lực. | User required; công thức đề xuất |
| FR-006 | Một vũ khí sử dụng input tại một thời điểm, không cùng lúc bắn và chém do click UI. | Chờ D001/D002 |
| FR-007 | Pause đóng băng timer, không phát sinh phát bắn do đóng UI. Game Over dừng bắn/nạp/tích lực; Restart reset đạn và nâng cấp runtime. | Proposed |
| FR-008 | Các tham số có Inspector validation, chống interval <=0, ammo âm, damage/scale không hữu hạn; giữ API/đơn vị kiếm cũ. | Proposed |
| FR-009 | Hiển thị số đạn, tiến độ nạp hoặc tích lực; thêm 2 lựa chọn nâng cấp Ammo và Reload vào level-up khi phù hợp với vũ khí. | Proposed |

## Preset đề xuất — chưa áp dụng
| Vũ khí | Đạn | Damage | Khoảng cách giữa phát | Tốc độ đạn (world units/s) | Nạp |
| --- | ---: | ---: | ---: | ---: | --- |
| Sách phép | 8 | 14 | 0.40s | 9 | Hết băng: 1.8s nạp đầy |
| Cung | 7 | 22 | 0.25s | 15 | Sau 0.45s ngừng bắn: 0.65s cho mỗi tên; phát đầu hồi sau tổng 1.10s |
| Nỏ | 1 | 24→96 | Theo thả nút + nạp | 18 | Sau bắn: 1.5s |

Nỏ: tích lực tuyến tính, 0→3s: damage x1→x4, kích thước x1→x2. Chạm trần không tự bắn, đợi thả. Thả sớm vẫn bắn yếu. Không cần ammo dự trữ/pickup; nạp là hồi tài nguyên tự động.

Nâng cấp đề xuất: Ammo +1; Reload +20% tốc độ nạp cộng dồn. Thời gian nạp = cơ bản / (1 + 0.2 * cấp). Với nỏ, cùng hệ số giảm thời gian tích lực; Ammo +1 mở thêm 0.5s lực cơ sở và nâng mức damage tối đa thêm 0.5 lần damage gốc, scale tối đa thêm 1/6. Do đó thêm thời gian có phần thưởng chứ không chỉ khiến chờ lâu hơn. Các giá trị là cấu hình, không hardcode trong UI.

## Quyết định cần chốt (frontier)
| ID | Lựa chọn | Khuyến nghị / lý do | FR |
| --- | --- | --- | --- |
| D001 | Giữ chuột trái để sách/cung bắn và nhả để cung hồi, hoặc giữ auto-fire hiện tại và cần nút tạm ngừng riêng. | Giữ chuột trái cho ranged; nỏ giữ/thả. Cung chỉ hồi khi nhả, kể cả khi cạn tên. Nhất quán input. | FR001–003,006 |
| D002 | Trang bị theo lớp hoặc hệ đổi vũ khí 1–4. | Knight=kiếm, Archer=cung, Hunter=nỏ, Mage=sách; mỗi lần một vũ khí. Không thêm inventory/đổi vũ khí ngoài yêu cầu. Scene Player hiện tại dùng Mage, không sinh cả 3 Player vào scene. | FR006 |
| D003 | Ammo upgrade của nỏ mở trần lực cao hơn và mất thêm thời gian để đạt; hoặc chỉ kéo dài cửa sổ giữ sau khi đạt trần cũ. | Mở trần lực cao hơn theo preset; reload upgrade rút ngắn thời gian thực tới trần. Kéo dài cửa sổ đơn thuần không có giá trị nếu giữ ở trần vô hạn. | FR004–005 |

## Tiêu chí nghiệm thu dự kiến
Đo bằng logic production và Play Mode: đúng số đạn, nhịp nạp, cung không hồi khi đang giữ bắn, ngắt nạp không nhân đôi tên; nỏ thả sớm/đủ lực/giữ quá trần và hai loại upgrade; collider/hình tên cùng tăng; projectile nhận đúng snapshot damage khi bắn; pause/UI không bắn ngoài ý muốn; chết/restart reset; kiếm và dữ liệu prefab người dùng giữ tương thích. Chi tiết AC/technical contract sẽ chốt sau D001–D003 và xin duyệt trước production.

## Kết quả / giả định / vòng đời
Đạn bắt đầu đầy mỗi run; nâng cấp không sửa data gốc. Không tặng đạn tức thì khi tăng capacity; chỉ tăng giới hạn, đợi nạp. Reload/charge thuộc runtime của vũ khí, không phải lịch sử save. Người dùng đã chốt các khuyến nghị/preset; trạng thái triển khai và bằng chứng nằm trong LOOP.md.
