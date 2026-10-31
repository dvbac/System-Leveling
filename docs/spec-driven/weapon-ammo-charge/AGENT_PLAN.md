# Kế hoạch
Approval: user "chốt", 2026-10-29. Main agent làm tuần tự, không delegation.

- TASK001: Assets/Scripts/Weapon/{WeaponScriptableObject.cs,Controller/,Behaviour/,Base/}; scheduler và tích hợp input/projectile/equip; AC001–007.
- TASK002: Assets/Scripts/{Player/PlayerStats.cs,System/Leveling/UpgradeKind.cs,UI/LevelUpSelection.cs,UI/WeaponHud.cs}; upgrade và HUD; AC004/006/008.
- TASK003: Assets/Scripts/Editor/WeaponAmmoSetup.cs; Assets/Prefab/{Weapons/,Characters/}, Assets/Scriptable Object/{Weapon/,Character/}, Assets/Scenes/SampleScene.unity; migrate có backup, không rebuild user character; AC005/008.
- TASK004: Assets/Tests/WeaponAmmoRegression.cs và regression hiện có nếu đổi assumptions; Captures/WeaponAmmo/, docs/spec-driven/weapon-ammo-charge/; compile/Play Mode và audit. AC001–009.

Không sửa animation/ảnh nguồn, AI/spawner, hệ HUD cũ ngoài điểm nối cần thiết. Dừng nếu cần thay hành vi đã duyệt hoặc phát hiện nguy cơ mất dữ liệu user. Test thất bại giữ artifact; sau 3 vòng cùng AC dừng xin quyết định.
