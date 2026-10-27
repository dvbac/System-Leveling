using UnityEngine;

public class Experience : MonoBehaviour, ICollectible
{
    public int experienceGranted;

    [SerializeField] private float collectDistance = 0.15f;

    private PlayerStats target;
    private float pullSpeed;
    private bool isCollected;

    public void StartPull(PlayerStats player, float speed)
    {
        target = player;
        pullSpeed = speed;
    }

    private void Update()
    {
        if (GameOverController.GameplayStopped || target == null || target.IsDead || isCollected)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target.transform.position,
            pullSpeed * Time.deltaTime
        );

        float distance = Vector2.Distance(
            transform.position,
            target.transform.position
        );

        if (distance <= collectDistance)
        {
            Collect();
        }
    }

    public void Collect()
    {
        if (GameOverController.GameplayStopped || isCollected || target == null || target.IsDead)
        {
            return;
        }

        isCollected = true;
        target.IncreaseExperience(experienceGranted);

        Destroy(gameObject);
    }
}
