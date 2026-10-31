using UnityEngine;
using UnityEngine.UI;

public class WeaponHud : MonoBehaviour
{
    [SerializeField] private PlayerStats player;
    [SerializeField] private Text label;
    [SerializeField] private Image progress;
    private WeaponHolder holder;

    public void Configure(PlayerStats target, Text text, Image fill)
    {
        player = target; label = text; progress = fill;
        holder = player != null ? player.GetComponentInChildren<WeaponHolder>(true) : null;
    }

    private void Start()
    {
        if (player != null) holder = player.GetComponentInChildren<WeaponHolder>(true);
    }

    private void Update()
    {
        if (label == null || progress == null) return;
        WeaponController current = holder != null ? holder.CurrentWeapon : null;
        RangedWeaponController weapon = current as RangedWeaponController;
        if (weapon == null || weapon.State == null)
        {
            label.text = current == null ? "NO WEAPON" : weapon != null ? "EQUIPPING" : "MELEE";
            progress.fillAmount = 0;
            return;
        }
        RangedWeaponState state = weapon.State;
        string status = state.IsCharging ? "CHARGE" : state.IsReloading ? "RELOAD" : "READY";
        string text = $"{weapon.weaponData.name.ToUpperInvariant()}   {state.Ammo} / {state.Capacity}   {status}";
        if (label.text != text) label.text = text;
        progress.fillAmount = state.IsCharging ? state.ChargeFraction : state.ReloadFraction;
        progress.color = state.IsCharging ? new Color(1f, 0.72f, 0.2f) : new Color(0.2f, 0.7f, 1f);
    }
}
