using UnityEngine;

public class WeaponHolder : MonoBehaviour
{
    public Vector2 PointerPosition { get; set; }

    private void Update()
    {
        Vector2 direction = PointerPosition - (Vector2)transform.position;

        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();

        // Xoay vũ khí về phía chuột
        transform.right = direction;

        // Lật sprite khi chuột nằm bên trái
        Vector3 scale = transform.localScale;
        scale.y = direction.x < 0 ? -1 : 1;
        transform.localScale = scale;
    }

    public Vector2 GetDirection()
    {
        return transform.right;
    }
}