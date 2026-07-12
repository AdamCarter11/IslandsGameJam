using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab-authored shop row visuals. ShopPanelUI populates crop data and buy state.
/// </summary>
public class ShopRowView : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI priceText;
    [SerializeField] Button buyButton;

    public CropGrowthSO Crop { get; private set; }
    public Button BuyButton => buyButton;

    public void Bind(CropGrowthSO crop, System.Action onBuy, int? resolvedPrice = null)
    {
        Crop = crop;
        if (crop == null)
            return;

        var sprite = crop.GetShopIcon();
        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
            icon.preserveAspect = true;
        }

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(crop.cropName) ? crop.name : crop.cropName;

        SetPrice(resolvedPrice ?? crop.seedPrice);

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => onBuy?.Invoke());
        }
    }

    public void SetPrice(int price)
    {
        if (priceText != null)
            priceText.text = $"{price} gold";
    }

    public void SetBuyInteractable(bool canBuy)
    {
        if (buyButton != null)
            buyButton.interactable = canBuy;
    }

#if UNITY_EDITOR
    public void EditorAssign(Image iconImage, TextMeshProUGUI name, TextMeshProUGUI price, Button buy)
    {
        icon = iconImage;
        nameText = name;
        priceText = price;
        buyButton = buy;
    }
#endif
}
