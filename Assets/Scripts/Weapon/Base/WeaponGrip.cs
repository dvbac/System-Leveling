using UnityEngine;

// Sprite calibration belongs to the weapon, not to its character or texture importer.
[ExecuteAlways]
public class WeaponGrip : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField, Min(0.01f)] private float visualSize = 0.4f;
    [SerializeField] private float spriteAngle;
    [SerializeField] private Vector2 holdOffset = new Vector2(0.2f, 0);
    [Tooltip("Grip point inside the sprite rectangle, from bottom-left (0,0) to top-right (1,1).")]
    [SerializeField] private Vector2 spriteGrip = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 muzzleOffset = new Vector2(0.55f, 0);

    public Vector3 MuzzlePosition => transform.TransformPoint(muzzleOffset);
    public bool IsConfigured => visual != null && visual.sprite != null && visual.transform != transform && visual.transform.IsChildOf(transform);

    private void OnEnable() => Apply();
    private void LateUpdate() => Apply();
    private void OnValidate()
    {
        visualSize = WeaponScriptableObject.Safe(visualSize, 0.01f);
        spriteAngle = float.IsNaN(spriteAngle) || float.IsInfinity(spriteAngle) ? 0 : spriteAngle;
        spriteGrip = new Vector2(Mathf.Clamp01(WeaponScriptableObject.Safe(spriteGrip.x, 0)), Mathf.Clamp01(WeaponScriptableObject.Safe(spriteGrip.y, 0)));
        if (!Finite(holdOffset)) holdOffset = Vector2.zero;
        if (!Finite(muzzleOffset)) muzzleOffset = new Vector2(0.55f, 0);
        Apply();
    }

    public void Apply()
    {
        if (!IsConfigured) return;
        Sprite sprite = visual.sprite;
        float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        if (size <= 0) return;
        float scale = visualSize / size;
        Quaternion rotation = Quaternion.Euler(0, 0, spriteAngle);
        Vector2 grip = (Vector2.Scale(sprite.rect.size, spriteGrip) - sprite.pivot) / sprite.pixelsPerUnit;
        Transform target = visual.transform;
        Vector3 desiredScale = new Vector3(scale, scale, 1);
        Vector3 desiredPosition = (Vector3)holdOffset - rotation * (Vector3)(grip * scale);
        if (target.localScale != desiredScale) target.localScale = desiredScale;
        if (target.localRotation != rotation) target.localRotation = rotation;
        if (target.localPosition != desiredPosition) target.localPosition = desiredPosition;
    }

    private static bool Finite(Vector2 value) => !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsInfinity(value.x) && !float.IsInfinity(value.y);
}
