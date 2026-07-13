using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum SeedShopSortMode
{
    PriceAsc,
    PriceDesc,
    RecentUnlock
}

public class ShopPanelUI : MonoBehaviour
{
    static readonly Color SortButtonActiveColor = new(0.62f, 0.48f, 0.22f, 1f);
    static readonly Color SortButtonInactiveColor = new(0.32f, 0.24f, 0.14f, 1f);

    [SerializeField] Button closeButton;
    [SerializeField] RectTransform listRoot;
    [SerializeField] ShopRowView shopRowPrefab;
    [SerializeField] Button relicRollButton;
    [SerializeField] TextMeshProUGUI relicRollLabel;
    [SerializeField] RelicChoicePanelUI relicChoicePanel;
    [SerializeField] SeedTooltipUI seedTooltip;
    [SerializeField] Button sortPriceAscButton;
    [SerializeField] Button sortPriceDescButton;
    [SerializeField] Button sortRecentButton;

    Inventory inventory;
    RelicShopService relicShop;
    Action onClose;
    readonly List<ShopRowView> rows = new();

    SeedShopSortMode sortMode = SeedShopSortMode.PriceAsc;

    public bool IsRelicChoiceOpen => relicChoicePanel != null && relicChoicePanel.IsOpen;

    public void Initialize(Inventory inv, Action closeCallback, RelicShopService shopService = null, RelicChoicePanelUI choicePanel = null)
    {
        if (inventory != null)
        {
            inventory.OnShopUnlocksChanged -= Rebuild;
            inventory.OnGoldChanged -= OnGoldChanged;
            inventory.OnHotbarChanged -= RefreshBuyStates;
            inventory.OnRelicsChanged -= RefreshBuyStates;
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
                HideSeedTooltip();
                onClose?.Invoke();
            });
        }

        if (relicRollButton != null)
        {
            relicRollButton.onClick.RemoveAllListeners();
            relicRollButton.onClick.AddListener(OnRelicRollClicked);
        }

        WireSortButton(sortPriceAscButton, SeedShopSortMode.PriceAsc);
        WireSortButton(sortPriceDescButton, SeedShopSortMode.PriceDesc);
        WireSortButton(sortRecentButton, SeedShopSortMode.RecentUnlock);
        RefreshSortButtonVisuals();

        if (relicChoicePanel != null)
            relicChoicePanel.Initialize(relicShop, RefreshRelicRollButton);

        HideSeedTooltip();

        if (inventory == null)
            return;

        inventory.OnShopUnlocksChanged += Rebuild;
        inventory.OnGoldChanged += OnGoldChanged;
        inventory.OnHotbarChanged += RefreshBuyStates;
        inventory.OnRelicsChanged += RefreshBuyStates;
        Rebuild();
        RefreshRelicRollButton();
    }

    void OnDisable()
    {
        HideSeedTooltip();
    }

    void OnDestroy()
    {
        HideSeedTooltip();
        if (inventory == null)
            return;
        inventory.OnShopUnlocksChanged -= Rebuild;
        inventory.OnGoldChanged -= OnGoldChanged;
        inventory.OnHotbarChanged -= RefreshBuyStates;
        inventory.OnRelicsChanged -= RefreshBuyStates;
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

        GameManager.Main?.AudioService?.PlayRelicRoll();
        RefreshRelicRollButton();
        relicChoicePanel?.ShowOffers();
        SetCloseInteractable(false);
    }

    void Rebuild()
    {
        HideSeedTooltip();

        foreach (var row in rows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }
        rows.Clear();

        if (inventory == null || listRoot == null || shopRowPrefab == null)
            return;

        var crops = new List<CropGrowthSO>();
        foreach (var crop in inventory.UnlockedSeeds)
        {
            if (crop != null)
                crops.Add(crop);
        }

        SortCrops(crops);

        foreach (var crop in crops)
            rows.Add(CreateRow(crop));

        RefreshBuyStates();
        RefreshRelicRollButton();
        RefreshSortButtonVisuals();
    }

    void SortCrops(List<CropGrowthSO> crops)
    {
        var unlockIndex = BuildUnlockIndexMap();

        crops.Sort((a, b) =>
        {
            int cmp;
            switch (sortMode)
            {
                case SeedShopSortMode.PriceAsc:
                    cmp = inventory.GetResolvedSeedPrice(a).CompareTo(inventory.GetResolvedSeedPrice(b));
                    break;
                case SeedShopSortMode.PriceDesc:
                    cmp = inventory.GetResolvedSeedPrice(b).CompareTo(inventory.GetResolvedSeedPrice(a));
                    break;
                default:
                    int indexA = unlockIndex.TryGetValue(a, out int ia) ? ia : -1;
                    int indexB = unlockIndex.TryGetValue(b, out int ib) ? ib : -1;
                    cmp = indexB.CompareTo(indexA);
                    break;
            }

            if (cmp != 0)
                return cmp;

            return string.Compare(CropSortName(a), CropSortName(b), StringComparison.OrdinalIgnoreCase);
        });
    }

    Dictionary<CropGrowthSO, int> BuildUnlockIndexMap()
    {
        var map = new Dictionary<CropGrowthSO, int>();
        var order = inventory.UnlockOrder;
        for (int i = 0; i < order.Count; i++)
        {
            var crop = order[i];
            if (crop != null)
                map[crop] = i;
        }
        return map;
    }

    static string CropSortName(CropGrowthSO crop)
    {
        if (crop == null)
            return string.Empty;
        return !string.IsNullOrEmpty(crop.cropName) ? crop.cropName : crop.name;
    }

    void WireSortButton(Button button, SeedShopSortMode mode)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SetSortMode(mode));
    }

    void SetSortMode(SeedShopSortMode mode)
    {
        if (sortMode == mode)
            return;
        sortMode = mode;
        Rebuild();
    }

    void RefreshSortButtonVisuals()
    {
        SetSortButtonVisual(sortPriceAscButton, sortMode == SeedShopSortMode.PriceAsc);
        SetSortButtonVisual(sortPriceDescButton, sortMode == SeedShopSortMode.PriceDesc);
        SetSortButtonVisual(sortRecentButton, sortMode == SeedShopSortMode.RecentUnlock);
    }

    static void SetSortButtonVisual(Button button, bool active)
    {
        if (button == null)
            return;

        if (button.targetGraphic is Image image)
            image.color = active ? SortButtonActiveColor : SortButtonInactiveColor;
    }

    ShopRowView CreateRow(CropGrowthSO crop)
    {
        var row = Instantiate(shopRowPrefab, listRoot);
        row.gameObject.name = $"ShopRow_{crop.name}";
        int price = inventory.GetResolvedSeedPrice(crop);
        row.Bind(crop, () =>
        {
            if (inventory != null && inventory.TryBuySeed(crop))
            {
                GameManager.Main?.AudioService?.PlayBuySeed();
                RefreshBuyStates();
                RefreshRelicRollButton();
            }
        }, price);
        row.SetHoverHandlers(ShowTooltipForRow, HideSeedTooltip);
        return row;
    }

    void ShowTooltipForRow(ShopRowView row, CropGrowthSO crop)
    {
        if (seedTooltip == null || crop == null || row == null)
        {
            HideSeedTooltip();
            return;
        }

        seedTooltip.Show(crop, row.transform as RectTransform);
    }

    void HideSeedTooltip()
    {
        seedTooltip?.Hide();
    }

    void RefreshBuyStates()
    {
        if (inventory == null)
            return;

        foreach (var row in rows)
        {
            if (row == null || row.Crop == null)
                continue;

            int price = inventory.GetResolvedSeedPrice(row.Crop);
            row.SetPrice(price);

            bool canBuy = inventory.IsUnlocked(row.Crop)
                          && inventory.gold >= price
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
        TextMeshProUGUI rollLabel = null,
        RelicChoicePanelUI choicePanel = null,
        SeedTooltipUI tooltip = null,
        Button priceAscSort = null,
        Button priceDescSort = null,
        Button recentSort = null)
    {
        closeButton = close;
        listRoot = list;
        shopRowPrefab = rowPrefab;
        relicRollButton = rollButton;
        relicRollLabel = rollLabel;
        relicChoicePanel = choicePanel;
        seedTooltip = tooltip;
        sortPriceAscButton = priceAscSort;
        sortPriceDescButton = priceDescSort;
        sortRecentButton = recentSort;
    }
#endif
}
