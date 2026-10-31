using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    private Vector2 pointerPosition;
    private bool hasPointer;
    private GameObject equippedPrefab;
    public WeaponController CurrentWeapon { get; private set; }
    public Vector2 PointerPosition
    {
        get => pointerPosition;
        set { pointerPosition = value; hasPointer = true; Aim(); }
    }

    private void Awake()
    {
        if (CurrentWeapon != null) return; // A selection can be equipped while the player is still inactive.
        CurrentWeapon = GetComponentInChildren<WeaponController>(true);
        if (CurrentWeapon != null) return; // Compatibility with older, authored weapon children.
        PlayerStats player = GetComponentInParent<PlayerStats>();
        if (player != null && player.characterData != null && player.characterData.StartingWeapon != null)
            Equip(player.characterData.StartingWeapon);
    }

    public bool Equip(GameObject weaponPrefab)
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Equip is a runtime API; configure the character anchor and weapon grip in Edit Mode.", this);
            return false;
        }
        if (weaponPrefab == null || !weaponPrefab.activeSelf ||
            !weaponPrefab.TryGetComponent(out WeaponController source) || source.weaponData == null ||
            !weaponPrefab.TryGetComponent(out WeaponGrip grip) || !grip.IsConfigured ||
            weaponPrefab.GetComponentsInChildren<WeaponController>(true).Length != 1 ||
            weaponPrefab.GetComponentInChildren<WeaponHolder>(true) != null)
        {
            Debug.LogWarning("Equip rejected: use an active weapon prefab with one root WeaponController, data and a configured WeaponGrip.", this);
            return false;
        }
        if (CurrentWeapon != null && equippedPrefab == weaponPrefab && CurrentWeapon.gameObject.activeSelf) return true;
        WeaponController[] previous = GetComponentsInChildren<WeaponController>(true);
        foreach (WeaponController weapon in previous)
            if (weapon.gameObject == gameObject)
            {
                Debug.LogWarning("Equip rejected: the weapon controller must be a child, not the hand anchor itself.", this);
                return false;
            }

        GameObject created = null;
        try
        {
            created = Instantiate(weaponPrefab, transform);
            created.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            created.transform.localScale = Vector3.one;
            created.GetComponent<WeaponGrip>().Apply();
        }
        catch (System.Exception exception)
        {
            if (created != null) { created.SetActive(false); Destroy(created); }
            Debug.LogWarning("Equip failed; previous weapon retained: " + exception.Message, this);
            return false;
        }
        foreach (WeaponController weapon in previous)
        {
            weapon.gameObject.SetActive(false); // Destroy is deferred; stop the old weapon before another Update.
            Destroy(weapon.gameObject);
        }
        CurrentWeapon = created.GetComponent<WeaponController>();
        equippedPrefab = weaponPrefab;
        Aim();
        return true;
    }

    private void Update() => Aim();

    private void Aim()
    {
        if (!hasPointer || Time.timeScale <= 0f || LevelUpSelection.InputBlocked) return;
        Vector2 direction = PointerPosition - (Vector2)transform.position;

        if (float.IsNaN(direction.x) || float.IsNaN(direction.y) || float.IsInfinity(direction.x) || float.IsInfinity(direction.y)) return;

        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();

        // Xoay vũ khí về phía chuột
        transform.right = direction;

        // Lật sprite khi chuột nằm bên trái
        Vector3 scale = transform.localScale;
        if (Mathf.Abs(direction.x) > 0.01f)
            scale.y = Mathf.Abs(scale.y) * (direction.x < 0 ? -1 : 1);
        transform.localScale = scale;
    }

    public Vector2 GetDirection()
    {
        Aim();
        return transform.right;
    }
}
