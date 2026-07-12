using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Prefab-authored hotbar slot visuals. HotbarUI binds clicks and refreshes data.
/// </summary>
public class HotbarSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image background;
    [SerializeField] Image highlight;
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI count;
    [SerializeField] Button button;

    CropGrowthSO boundCrop;
    System.Action<HotbarSlotView, CropGrowthSO> onHoverEnter;
    System.Action onHoverExit;

    public Image Background => background;
    public Image Highlight => highlight;
    public Image Icon => icon;
    public TextMeshProUGUI Count => count;
    public Button Button => button;
    public CropGrowthSO BoundCrop => boundCrop;

    public void BindCrop(CropGrowthSO crop)
    {
        boundCrop = crop;
    }

    public void SetHoverHandlers(
        System.Action<HotbarSlotView, CropGrowthSO> enter,
        System.Action exit)
    {
        onHoverEnter = enter;
        onHoverExit = exit;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (boundCrop != null)
            onHoverEnter?.Invoke(this, boundCrop);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onHoverExit?.Invoke();
    }

#if UNITY_EDITOR
    public void EditorAssign(Image bg, Image hl, Image ic, TextMeshProUGUI ct, Button btn)
    {
        background = bg;
        highlight = hl;
        icon = ic;
        count = ct;
        button = btn;
    }
#endif
}
