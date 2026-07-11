using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopPanelUI : MonoBehaviour
{
    [SerializeField] Button closeButton;
    [SerializeField] RectTransform listRoot;
    [SerializeField] ShopRowView shopRowPrefab;

    Inventory inventory;
    System.Action onClose;
    readonly List<ShopRowView> rows = new();

    public void Initialize(Inventory inv, System.Action closeCallback)
    {
        if (inventory != null)
        {
            inventory.OnShopUnlocksChanged -= Rebuild;
            inventory.OnGoldChanged -= OnGoldChanged;
            inventory.OnHotbarChanged -= RefreshBuyStates;
        }

        inventory = inv;
        onClose = closeCallback;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => onClose?.Invoke());
        }

        if (inventory == null)
            return;

        inventory.OnShopUnlocksChanged += Rebuild;
        inventory.OnGoldChanged += OnGoldChanged;
        inventory.OnHotbarChanged += RefreshBuyStates;
        Rebuild();
    }

    void OnDestroy()
    {
        if (inventory == null)
            return;
        inventory.OnShopUnlocksChanged -= Rebuild;
        inventory.OnGoldChanged -= OnGoldChanged;
        inventory.OnHotbarChanged -= RefreshBuyStates;
    }

    void OnGoldChanged(int _) => RefreshBuyStates();

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

#if UNITY_EDITOR
    public void EditorAssign(Button close, RectTransform list, ShopRowView rowPrefab)
    {
        closeButton = close;
        listRoot = list;
        shopRowPrefab = rowPrefab;
    }
#endif
}
