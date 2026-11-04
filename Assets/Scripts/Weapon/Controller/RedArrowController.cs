using UnityEngine;

public class RedArrowController : RangedWeaponController
{
    protected override float SingleShotAngle(float spread) => Random.Range(-spread * 0.5f, spread * 0.5f);
}
