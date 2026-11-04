# Frostfang — implementation plan
Status: Frozen. Approval reference: user “Duyệt, triển khai”, 2026-11-04. Owner: main agent. No delegation.

## Contracts
FrostfangBoss.BeginDeath returns scaled death lifetime to EnemyStats.Kill; encounter owns spawn/HUD/victory; boss owns spawned effects/pillars and cleanup. EnemyStats reward/drop guards stay intact. Geometry separate from particle alpha. Shared changes serialized.

## Slices / allowed ownership
1. Runtime: new Assets/Scripts/Enemy/Boss/Frostfang*.cs and minimal death branch in EnemyStats.cs. AC-003/004/005. Preserve Rotwood and all ordinary enemy paths. Integration correction: GameOverController.Win gets a named-boss overload while preserving no-argument Win for existing callers, so the Frostfang result is not mislabeled Rotwood.
2. Setup: new Assets/Scripts/Editor/FrostfangBossBuilder.cs; generated copies under Frostfang/Generated; Assets/Prefab/Boss/Frostfang.prefab; add encounter only to SnowScene after backup/dirty-scene checks. AC-001/002/006. Read FX from Captures, preserve source art and all spawn settings.
3. Verification: new Assets/Tests/FrostfangBossRegression.cs, Captures/FrostfangBoss reports/screenshots; docs/frostfang-boss.md and spec loop evidence. Run own regression and existing Rotwood regression; no weakening old tests. SoulTestIsolation required.

Forbidden: source Frostfang PNGs, SampleScene authoring, existing HUD/weapon/save changes, unrelated dirty files, package changes, commit/push.
Stop/report: missing assets, dirty scene before setup, shared compatibility failure or changed approved behavior. Order: runtime -> compile -> setup -> tests -> evidence judgment.
