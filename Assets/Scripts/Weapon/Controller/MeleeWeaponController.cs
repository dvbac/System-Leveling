using UnityEngine;
using UnityEngine.InputSystem;

public class MeleeWeaponController : WeaponController
{
    [Header("References")]
    [SerializeField] private Transform swordPivot;

    private Quaternion idleRotation;
    private Quaternion attackRotation;

    private float currentSwingAngle;
    private float currentSwingSpeed;
    private float sweptAngle;

    private bool isAttacking;

    protected override void Start()
    {
        base.Start();

        if (swordPivot != null)
            idleRotation = swordPivot.localRotation;
    }

    protected override void Update()
    {
        if (Time.timeScale <= 0f || LevelUpSelection.InputBlocked)
            return;

        currentCooldown -= Time.deltaTime;

        if (isAttacking)
        {
            UpdateSwing();
            return;
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            (UnityEngine.EventSystems.EventSystem.current == null ||
             !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) &&
            currentCooldown <= 0f)
        {
            Attack();
        }
    }

    protected override void Attack()
    {
        if (weaponData == null || wh == null || swordPivot == null)
            return;

        if (weaponData.Speed <= 0f || weaponData.SwingAngle <= 0f)
            return;

        base.Attack();

        currentSwingAngle = weaponData.SwingAngle;
        currentSwingSpeed = weaponData.Speed;

        Vector2 direction = wh.GetDirection();

        float aimAngle =
            Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        attackRotation = Quaternion.Euler(0f, 0f, aimAngle);

        sweptAngle = 0f;
        isAttacking = true;

        ApplySwingRotation();

        if (weaponData.Prefab == null)
            return;

        GameObject slash = Instantiate(
            weaponData.Prefab,
            swordPivot.position,
            attackRotation
        );

        if (slash.TryGetComponent(out MeleeWeaponBehaviour melee))
        {
            melee.Initialize(
                weaponData,
                swordPivot,
                direction
            );
        }
        else
        {
            Destroy(slash);
        }
    }

    private void LateUpdate()
    {
        // Giữ hướng nhát chém dù WeaponHolder đang xoay theo chuột.
        if (isAttacking)
            ApplySwingRotation();
    }

    private void UpdateSwing()
    {
        sweptAngle = Mathf.Min(
            sweptAngle + currentSwingSpeed * Time.deltaTime,
            currentSwingAngle
        );

        ApplySwingRotation();

        if (sweptAngle >= currentSwingAngle)
        {
            isAttacking = false;
            swordPivot.localRotation = idleRotation;
        }
    }

    private void ApplySwingRotation()
    {
        float angle = -currentSwingAngle / 2f + sweptAngle;

        swordPivot.rotation =
            attackRotation * Quaternion.Euler(0f, 0f, angle);
    }
}
