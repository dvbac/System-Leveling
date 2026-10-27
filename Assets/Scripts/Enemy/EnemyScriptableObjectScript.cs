using UnityEngine;

[CreateAssetMenu(
    fileName = "EnemyData",
    menuName = "Scriptable Objects/Enemy"
)]
public class EnemyScriptableObject : ScriptableObject
{
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