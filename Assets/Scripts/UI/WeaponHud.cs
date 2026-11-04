using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class WeaponHud : MonoBehaviour
{
    [SerializeField] private PlayerStats player;
    [SerializeField] private Text label;
    [SerializeField] private Image progress;
    [SerializeField] private bool compact;
    [SerializeField] private Text cursorAmmo;
    [SerializeField] private Text reloadIndicator;
    private WeaponHolder holder;
    private Canvas canvas;
    private Camera gameplayCamera;
    private SpriteRenderer playerSprite;
    public bool Compact => compact;

    public void Configure(PlayerStats target, Text text, Image fill)
    {
        player = target; label = text; progress = fill;
        holder = player != null ? player.GetComponentInChildren<WeaponHolder>(true) : null;
        canvas = GetComponentInParent<Canvas>();
        playerSprite = player != null ? player.GetComponent<SpriteRenderer>() : null;
    }

    private void Start()
    {
        if (player != null) holder = player.GetComponentInChildren<WeaponHolder>(true);
        canvas = GetComponentInParent<Canvas>();
        playerSprite = player != null ? player.GetComponent<SpriteRenderer>() : null;
        gameplayCamera = Camera.main;
    }

    private void Update()
    {
        if (label == null) return;
        WeaponController current = holder != null ? holder.CurrentWeapon : null;
        RangedWeaponController weapon = current as RangedWeaponController;
        if (weapon == null || weapon.State == null)
        {
            label.text = current == null ? "NO WEAPON" : weapon != null ? "EQUIPPING" : "MELEE";
            if (progress != null) progress.fillAmount = 0;
            return;
        }
        RangedWeaponState state = weapon.State;
        string status = state.IsCharging ? "CHARGE" : state.IsReloading ? "RELOAD" : "READY";
        string text = compact ? $"{state.Ammo} / {state.Capacity}" :
            $"{weapon.weaponData.name.ToUpperInvariant()}   {state.Ammo} / {state.Capacity}   {status}";
        if (label.text != text) label.text = text;
        if (progress != null)
        {
            progress.fillAmount = state.IsCharging ? state.ChargeFraction : state.ReloadFraction;
            progress.color = state.IsCharging ? new Color(1f, 0.72f, 0.2f) : new Color(0.2f, 0.7f, 1f);
        }
    }

    private void LateUpdate()
    {
        Update(); // Weapon ticks complete before drawing cursor/reload feedback.
        RangedWeaponState state = (holder != null ? holder.CurrentWeapon as RangedWeaponController : null)?.State;
        bool visible = compact && state != null && player != null && !player.IsDead &&
            !LevelUpSelection.InputBlocked && Time.timeScale > 0 && canvas != null;
        Vector2 mouse = Mouse.current != null ? Mouse.current.position.ReadValue() : new Vector2(-1,-1);
        bool inside = Mouse.current != null && mouse.x >= 0 && mouse.y >= 0 && mouse.x <= Screen.width && mouse.y <= Screen.height;
        if (cursorAmmo != null)
        {
            cursorAmmo.gameObject.SetActive(visible && inside);
            if (visible && inside)
            {
                cursorAmmo.text = $"{state.Ammo}/{state.Capacity}";
                cursorAmmo.fontSize = Mathf.Max(1, Mathf.RoundToInt(9f / canvas.scaleFactor));
                cursorAmmo.rectTransform.sizeDelta = new Vector2(72,14) / canvas.scaleFactor;
                Place(cursorAmmo.rectTransform, mouse + new Vector2(11,-16));
            }
        }
        bool showProgress = visible && (state.IsReloading || state.IsCharging);
        if (reloadIndicator != null)
        {
            if (gameplayCamera == null) gameplayCamera = Camera.main;
            Vector3 top = playerSprite != null ? new Vector3(playerSprite.bounds.center.x,playerSprite.bounds.max.y,playerSprite.bounds.center.z)
                : player != null ? player.transform.position + Vector3.up : Vector3.zero;
            Vector3 screen = gameplayCamera != null ? gameplayCamera.WorldToScreenPoint(top) : new Vector3(0,0,-1);
            showProgress &= gameplayCamera != null && screen.z > 0 && screen.x >= 0 && screen.x <= Screen.width && screen.y >= 0 && screen.y <= Screen.height;
            reloadIndicator.gameObject.SetActive(showProgress);
            if (showProgress)
            {
                float fraction = state.IsCharging ? state.ChargeFraction : state.ReloadFraction;
                int position = Mathf.Clamp(Mathf.RoundToInt(fraction * 21),0,21);
                reloadIndicator.text = "[" + new string('-',position) + "|" + new string('-',21-position) + "]";
                reloadIndicator.color = state.IsCharging ? new Color(1f,0.72f,0.2f) : Color.white;
                reloadIndicator.fontSize = Mathf.Max(1,Mathf.RoundToInt(11f/canvas.scaleFactor));
                reloadIndicator.rectTransform.sizeDelta = new Vector2(120,18)/canvas.scaleFactor;
                Place(reloadIndicator.rectTransform,(Vector2)screen + new Vector2(-60,8));
            }
        }
    }

    private void Place(RectTransform target, Vector2 screenPoint)
    {
        RectTransform area = (RectTransform)canvas.transform;
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area,screenPoint,camera,out Vector2 point)) return;
        target.anchoredPosition = new Vector2(
            Mathf.Clamp(point.x,area.rect.xMin+2,area.rect.xMax-target.rect.width-2),
            Mathf.Clamp(point.y,area.rect.yMin+2,area.rect.yMax-target.rect.height-2));
    }

    private void OnDisable()
    {
        if (cursorAmmo != null) cursorAmmo.gameObject.SetActive(false);
        if (reloadIndicator != null) reloadIndicator.gameObject.SetActive(false);
    }
}
