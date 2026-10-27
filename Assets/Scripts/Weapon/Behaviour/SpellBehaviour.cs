using UnityEngine;

public class SpellBehaviour : ProjectileWeaponBehaviour
{
    public override void Initialize(Vector3 dir)
    {
        base.Initialize(dir);

        transform.right = direction;
    }
}