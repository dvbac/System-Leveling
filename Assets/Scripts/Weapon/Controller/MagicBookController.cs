using UnityEngine;

public class MagicBookController : WeaponController
{
    protected override void Attack()
    {
        // Không có dữ liệu vũ khí thì không bắn.
        if (weaponData == null || wh == null)
            return;

        if (weaponData.Prefab == null)
            return;

        base.Attack();

        GameObject spawnedSpell = Instantiate(
            weaponData.Prefab,
            transform.position,
            transform.rotation
        );

        if (spawnedSpell.TryGetComponent(out SpellBehaviour spell))
        {
            spell.ApplyOwnerStats(GetComponentInParent<PlayerStats>());
            spell.Initialize(wh.GetDirection());
        }
        else
        {
            Destroy(spawnedSpell);
        }
    }
}
