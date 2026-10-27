using UnityEngine;

public class PlayerCollector : MonoBehaviour
{
    private PlayerStats player;
    private CircleCollider2D playerCollector;

    public float pullSpeed = 5f;

    private void Start()
    {
        player = GetComponentInParent<PlayerStats>();
        playerCollector = GetComponent<CircleCollider2D>();
    }

    private void Update()
    {
        playerCollector.radius = player.currentMagnet;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.TryGetComponent(out Experience experience))
        {
            experience.StartPull(player, pullSpeed);
        }
    }
}