using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Owned-relic inventory panel: unique icons with stack badges and hover tooltips.
/// </summary>
public class RelicInventoryPanelUI : MonoBehaviour
{
    [SerializeField] Button closeButton;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] RelicInventorySlotView slotPrefab;
    [SerializeField] TextMeshProUGUI emptyLabel;
    [SerializeField] GameObject tooltipRoot;
    [SerializeField] TextMeshProUGUI tooltipName;
    [SerializeField] TextMeshProUGUI tooltipDesc;
    [SerializeField] RectTransform tooltipFollowRoot;

    Inventory inventory;
    readonly List<RelicInventorySlotView> slots = new();
    readonly List<RelicSO> uniqueScratch = new();

    public bool IsOpen => gameObject.activeSelf;

    public void Initialize(Inventory inv)
    {
        if (inventory != null)
            inventory.OnRelicsChanged -= Refresh;

        inventory = inv;

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Close);
        }

        HideTooltip();

        if (inventory == null)
            return;

        inventory.OnRelicsChanged += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null)
            inventory.OnRelicsChanged -= Refresh;
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        HideTooltip();
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        HideTooltip();

        if (contentRoot == null || slotPrefab == null)
            return;

        uniqueScratch.Clear();
        if (inventory != null)
        {
            for (int i = 0; i < inventory.ownedRelics.Count; i++)
            {
                RelicSO relic = inventory.ownedRelics[i];
                if (relic == null || uniqueScratch.Contains(relic))
                    continue;
                uniqueScratch.Add(relic);
            }
        }

        EnsureSlotCount(uniqueScratch.Count);

        for (int i = 0; i < slots.Count; i++)
        {
            RelicInventorySlotView slot = slots[i];
            if (slot == null)
                continue;

            if (i >= uniqueScratch.Count)
            {
                slot.Clear();
                continue;
            }

            RelicSO relic = uniqueScratch[i];
            int count = inventory != null ? inventory.CountOwned(relic) : 0;
            slot.Bind(relic, count);
            slot.SetHoverHandlers(ShowTooltipForSlot, HideTooltip);
        }

        bool hasAny = uniqueScratch.Count > 0;
        if (emptyLabel != null)
            emptyLabel.gameObject.SetActive(!hasAny);
    }

    void EnsureSlotCount(int needed)
    {
        while (slots.Count < needed)
        {
            RelicInventorySlotView slot = Instantiate(slotPrefab, contentRoot);
            slot.gameObject.name = $"RelicInventorySlot_{slots.Count}";
            slots.Add(slot);
        }
    }

    void ShowTooltipForSlot(RelicInventorySlotView slot, RelicSO relic)
    {
        if (relic == null)
        {
            HideTooltip();
            return;
        }

        if (tooltipName != null)
            tooltipName.text = string.IsNullOrEmpty(relic.relicName) ? relic.name : relic.relicName;

        if (tooltipDesc != null)
            tooltipDesc.text = relic.desc ?? string.Empty;

        if (tooltipRoot != null)
            tooltipRoot.SetActive(true);

        PositionTooltipNearSlot(slot);
    }

    void PositionTooltipNearSlot(RelicInventorySlotView slot)
    {
        if (tooltipFollowRoot == null || slot == null)
            return;

        var slotRt = slot.transform as RectTransform;
        if (slotRt == null)
            return;

        Vector3[] corners = new Vector3[4];
        slotRt.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        tooltipFollowRoot.position = topCenter;
        tooltipFollowRoot.anchoredPosition += new Vector2(0f, 8f);
    }

    void HideTooltip()
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
    }

#if UNITY_EDITOR
    public void EditorAssign(
        Button close,
        RectTransform content,
        RelicInventorySlotView prefab,
        TextMeshProUGUI empty,
        GameObject tooltip,
        TextMeshProUGUI name,
        TextMeshProUGUI desc,
        RectTransform tooltipFollow = null)
    {
        closeButton = close;
        contentRoot = content;
        slotPrefab = prefab;
        emptyLabel = empty;
        tooltipRoot = tooltip;
        tooltipName = name;
        tooltipDesc = desc;
        tooltipFollowRoot = tooltipFollow != null ? tooltipFollow : tooltip != null ? tooltip.transform as RectTransform : null;
    }
#endif
}
