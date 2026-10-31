using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class LoadoutOption : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public Button button;
    public Image icon, ring;
    [NonSerialized] public Action preview;
    public void Bind(Sprite sprite, bool weapon, UnityAction click, Action inspect)
    {
        icon.sprite = sprite;
        icon.preserveAspect = true;
        ring.rectTransform.anchorMin = Vector2.zero; ring.rectTransform.anchorMax = Vector2.one;
        ring.rectTransform.offsetMin = ring.rectTransform.offsetMax = Vector2.zero;
        icon.rectTransform.anchorMin = weapon ? new Vector2(0.32f, 0.32f) : new Vector2(0.28f, 0.22f);
        icon.rectTransform.anchorMax = weapon ? new Vector2(0.68f, 0.68f) : new Vector2(0.72f, 0.78f);
        icon.rectTransform.offsetMin = icon.rectTransform.offsetMax = Vector2.zero;
        button.image.alphaHitTestMinimumThreshold = 0.1f;
        button.onClick.AddListener(click);
        preview = inspect;
    }
    public void OnPointerEnter(PointerEventData eventData) => preview?.Invoke();
    public void OnSelect(BaseEventData eventData)
    {
        preview?.Invoke();
        ScrollRect scroll = GetComponentInParent<ScrollRect>();
        if (scroll == null) return;
        Canvas.ForceUpdateCanvases();
        RectTransform rect = (RectTransform)transform;
        Vector3 center = scroll.content.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
        float overflow = scroll.content.rect.width - scroll.viewport.rect.width;
        if (scroll.horizontal && overflow > 0)
            scroll.horizontalNormalizedPosition = Mathf.Clamp01((center.x - scroll.viewport.rect.width / 2) / overflow);
        overflow = scroll.content.rect.height - scroll.viewport.rect.height;
        if (scroll.vertical && overflow > 0)
            scroll.verticalNormalizedPosition = 1 - Mathf.Clamp01((-center.y - scroll.viewport.rect.height / 2) / overflow);
    }
}
