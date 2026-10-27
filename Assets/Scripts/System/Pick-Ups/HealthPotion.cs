using UnityEngine;

public class HealthPotion : MonoBehaviour, ICollectible
{
    public int healthToRestore;

    private PlayerStats player;

    // Bình máu tự nhặt khi người chơi chạm vào.
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerStats found = other.GetComponentInParent<PlayerStats>();

        if (found == null)
            return;

        player = found;
        Collect();
    }

    public void Collect()
    {
        if (player == null)
            player = FindAnyObjectByType<PlayerStats>();

        if (player == null)
            return;

        player.RestoreHealth(healthToRestore);
        Destroy(gameObject);
    }
}
