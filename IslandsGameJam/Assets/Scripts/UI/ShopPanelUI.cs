using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelUI : MonoBehaviour
{
    [SerializeField] Button closeButton;
    [SerializeField] RectTransform listRoot;
    [SerializeField] ShopRowView shopRowPrefab;
    [SerializeField] Button relicRollButton;
    [SerializeField] Text relicRollLabel;
    [SerializeField] RelicChoicePanelUI relicChoicePanel;

    Inventory inventory;
    RelicShopService relicShop;
    System.Action onClose;
    readonly List<ShopRowView> rows = new();

    public bool IsRelicChoiceOpen => relicChoicePanel != null && relicChoicePanel.IsOpen;

    public void Initialize(Inventory inv, System.Action closeCallback, RelicShopService shopService = null, RelicChoicePanelUI choicePanel = null)
    {
        if (inventory != null)
        {
            inventory.OnShopUnlocksChanged -= Rebuild;
            inventory.OnGoldChanged -= OnGoldChanged;
            inventory.OnHotbarChanged -= RefreshBuyStates;
        }

        inventory = inv;
        onClose = closeCallback;
        if (shopService != null)
            relicShop = shopService;
        if (choicePanel != null)
            relicChoicePanel = choicePanel;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                if (IsRelicChoiceOpen)
                    return;
                onClose?.Invoke();
            });
        }

        if (relicRollButton != null)
        {
            relicRollButton.onClick.RemoveAllListeners();
            relicRollButton.onClick.AddListener(OnRelicRollClicked);
        }

        if (relicChoicePanel != null)
            relicChoicePanel.Initialize(relicShop, RefreshRelicRollButton);

        if (inventory == null)
            return;

        inventory.OnShopUnlocksChanged += Rebuild;
        inventory.OnGoldChanged += OnGoldChanged;
        inventory.OnHotbarChanged += RefreshBuyStates;
        Rebuild();
        RefreshRelicRollButton();
    }

    void OnDestroy()
    {
        if (inventory == null)
            return;
        inventory.OnShopUnlocksChanged -= Rebuild;
        inventory.OnGoldChanged -= OnGoldChanged;
        inventory.OnHotbarChanged -= RefreshBuyStates;
    }

    void OnGoldChanged(int _)
    {
        RefreshBuyStates();
        RefreshRelicRollButton();
    }

    void OnRelicRollClicked()
    {
        if (relicShop == null || IsRelicChoiceOpen)
            return;
        if (!relicShop.TryBeginRoll())
        {
            RefreshRelicRollButton();
            return;
        }

        RefreshRelicRollButton();
        relicChoicePanel?.ShowOffers();
        SetCloseInteractable(false);
    }

    void Rebuild()
    {
        foreach (var row in rows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        rows.Clear();

        if (inventory == null || listRoot == null || shopRowPrefab == null)
            return;

        foreach (var crop in inventory.UnlockedSeeds)
        {
            if (crop == null)
                continue;
            rows.Add(CreateRow(crop));
        }

        RefreshBuyStates();
        RefreshRelicRollButton();
    }

    ShopRowView CreateRow(CropGrowthSO crop)
    {
        var row = Instantiate(shopRowPrefab, listRoot);
        row.gameObject.name = $"ShopRow_{crop.name}";
        row.Bind(crop, () =>
        {
            if (inventory != null && inventory.TryBuySeed(crop))
                RefreshBuyStates();
        });
        return row;
    }

    void RefreshBuyStates()
    {
        if (inventory == null)
            return;

        foreach (var row in rows)
        {
            if (row == null || row.Crop == null)
                continue;

            bool canBuy = inventory.IsUnlocked(row.Crop)
                          && inventory.gold >= row.Crop.seedPrice
                          && inventory.CanFitSeed(row.Crop, 1);
            row.SetBuyInteractable(canBuy);
        }
    }

    public void RefreshRelicRollButton()
    {
        SetCloseInteractable(!IsRelicChoiceOpen);

        if (relicRollLabel != null)
        {
            int cost = relicShop != null ? relicShop.GetCurrentRollCost() : 0;
            relicRollLabel.text = $"Buy Relic ({cost} gold)";
        }

        if (relicRollButton != null)
            relicRollButton.interactable = relicShop != null && relicShop.CanRoll() && !IsRelicChoiceOpen;
    }

    void SetCloseInteractable(bool interactable)
    {
        if (closeButton != null)
            closeButton.interactable = interactable;
    }

#if UNITY_EDITOR
    public void EditorAssign(
        Button close,
        RectTransform list,
        ShopRowView rowPrefab,
        Button rollButton = null,
        Text rollLabel = null,
        RelicChoicePanelUI choicePanel = null)
    {
        closeButton = close;
        listRoot = list;
        shopRowPrefab = rowPrefab;
        relicRollButton = rollButton;
        relicRollLabel = rollLabel;
        relicChoicePanel = choicePanel;
    }
#endif
}
