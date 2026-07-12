using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Prefab-authored relic choice card. RelicChoicePanelUI populates data and select callback.
/// </summary>
public class RelicChoiceCardView : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI rarityText;
    [SerializeField] TextMeshProUGUI descText;
    [SerializeField] TextMeshProUGUI refundText;
    [SerializeField] Button selectButton;

    public void Bind(RelicSO relic, int refundGold, System.Action onSelect)
    {
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

        if (nameText != null)
            nameText.text = string.IsNullOrEmpty(relic.relicName) ? relic.name : relic.relicName;

        if (rarityText != null)
        {
            rarityText.text = relic.rarity.ToString();
            rarityText.color = GetRarityColor(relic.rarity);
        }

        if (descText != null)
            descText.text = relic.desc ?? string.Empty;

        if (refundText != null)
        {
            bool showRefund = refundGold > 0;
            refundText.gameObject.SetActive(showRefund);
            if (showRefund)
                refundText.text = $"SALE, refunds: {refundGold} gold";
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelect?.Invoke());
            selectButton.interactable = true;
        }
    }

    public void Clear()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveAllListeners();
        gameObject.SetActive(false);
    }

    static Color GetRarityColor(RelicRarity rarity) => rarity switch
    {
        RelicRarity.Common => new Color(0.75f, 0.75f, 0.78f),
        RelicRarity.Rare => new Color(0.35f, 0.55f, 1f),
        RelicRarity.Epic => new Color(0.75f, 0.4f, 0.95f),
        RelicRarity.Legendary => new Color(1f, 0.75f, 0.25f),
        _ => Color.white,
    };

#if UNITY_EDITOR
    public void EditorAssign(
        Image iconImage,
        TextMeshProUGUI name,
        TextMeshProUGUI rarity,
        TextMeshProUGUI desc,
        TextMeshProUGUI refund,
        Button select)
    {
        icon = iconImage;
        nameText = name;
        rarityText = rarity;
        descText = desc;
        refundText = refund;
        selectButton = select;
    }
#endif
}
