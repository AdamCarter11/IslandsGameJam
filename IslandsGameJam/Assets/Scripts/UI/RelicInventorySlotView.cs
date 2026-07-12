using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Prefab-authored relic inventory slot. RelicInventoryPanelUI binds data and hover tooltip.
/// </summary>
public class RelicInventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI countText;

    RelicSO boundRelic;
    System.Action<RelicInventorySlotView, RelicSO> onHoverEnter;
    System.Action onHoverExit;

    public RelicSO BoundRelic => boundRelic;

    public void Bind(RelicSO relic, int count)
    {
        boundRelic = relic;
        if (relic == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        if (icon != null)
        {
            icon.sprite = relic.shopIcon;
            icon.enabled = relic.shopIcon != null;
            icon.preserveAspect = true;
        }

        if (countText != null)
        {
            bool showStack = count > 1;
            countText.gameObject.SetActive(showStack);
            if (showStack)
                countText.text = $"×{count}";
        }
    }

    public void SetHoverHandlers(
        System.Action<RelicInventorySlotView, RelicSO> enter,
        System.Action exit)
    {
        onHoverEnter = enter;
        onHoverExit = exit;
    }

    public void Clear()
    {
        boundRelic = null;
        onHoverEnter = null;
        onHoverExit = null;
        gameObject.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (boundRelic != null)
            onHoverEnter?.Invoke(this, boundRelic);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }

#if UNITY_EDITOR
    public void EditorAssign(Image iconImage, TextMeshProUGUI count)
    {
        icon = iconImage;
        countText = count;
    }
#endif
}
