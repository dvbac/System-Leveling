using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    public WeaponScriptableObject weaponData;

    protected float currentCooldown;

    protected WeaponHolder wh;

    protected virtual void Start()
    {
        wh = GetComponentInParent<WeaponHolder>();

        currentCooldown = 0f;
    }

    protected virtual void Update()
    {
        if (Time.timeScale <= 0f || LevelUpSelection.InputBlocked)
            return;
        // weaponData null sẽ gây NullReferenceException ở Attack().
        if (weaponData == null)
            return;

        currentCooldown -= Time.deltaTime;

        if (currentCooldown <= 0f)
        {
            Attack();
        }
    }

    protected virtual void Attack()
    {
        if (weaponData == null)
            return;

        currentCooldown = weaponData.Cooldown;
    }
}
