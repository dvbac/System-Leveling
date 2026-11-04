using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "Scriptable Objects/Enemy"
)]
public class EnemyScriptableObject : ScriptableObject
{
    [SerializeField, Min(0)] private float experienceReward = 1.5f;
    public float ExperienceReward => float.IsNaN(experienceReward) || float.IsInfinity(experienceReward) ? 0f : Mathf.Max(0f, experienceReward);

    // Base stats for enemy

    [SerializeField]
    private float moveSpeed;

    public float MoveSpeed
    {
        get => moveSpeed;
        private set => moveSpeed = value;
    }


    [SerializeField]
    private float maxHealth;

    public float MaxHealth
    {
        get => maxHealth;
        private set => maxHealth = value;
    }


    [SerializeField]
    private float damage;

    public float Damage
    {
        get => damage;
        private set => damage = value;
    }
}
