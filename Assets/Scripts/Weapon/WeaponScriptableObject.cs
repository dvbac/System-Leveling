using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponData",
    menuName = "Scriptable Objects/Weapon"
)]
public class WeaponScriptableObject : ScriptableObject
{
    [Header("Prefab")]
    [SerializeField] private GameObject prefab;

    [Header("Base Stats")]
    [SerializeField] private float damage;
    [SerializeField] private float speed;
    [SerializeField] private float cooldown;
    [SerializeField] private int pierce;

    [Header("Melee Stats")]
    [SerializeField] private float swingAngle;
    [SerializeField] private float range;

    public GameObject Prefab => prefab;

    public float Damage => damage;
    public float Speed => speed;
    public float Cooldown => cooldown;
    public int Pierce => pierce;

    public float SwingAngle => swingAngle;
    public float Range => range;
}