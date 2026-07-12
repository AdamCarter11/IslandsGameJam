using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles relic shop rolls: escalating cost, three-offer choice, and skip refunds.
/// </summary>
public class RelicShopService : MonoBehaviour
{
    const int OfferSlotCount = 3;

    [SerializeField] Inventory inventory;
    [SerializeField] RelicShopCatalog catalog;

    int purchaseCount;
    readonly Dictionary<RelicSO, int> skipCounts = new();

    bool rollActive;
    int paidCost;
    readonly RelicSO[] offers = new RelicSO[OfferSlotCount];

    public bool HasActiveRoll => rollActive;
    public int PaidCost => paidCost;
    public IReadOnlyList<RelicSO> CurrentOffers => offers;

    Inventory Inventory
    {
        get
        {
            if (inventory == null)
                inventory = GetComponent<Inventory>()
                    ?? GetComponentInParent<Inventory>()
                    ?? GameManager.Main?.Inventory;
            return inventory;
        }
    }

    RelicShopCatalog Catalog
    {
        get
        {
            if (catalog == null)
                catalog = GameManager.Main?.RelicShopCatalog;
            return catalog;
        }
    }

    public int GetCurrentRollCost()
    {
        if (Catalog == null)
            return 1;

        float raw = Catalog.baseRollCost * Mathf.Pow(Catalog.costMultiplierPerPurchase, purchaseCount);
        return Mathf.Max(1, Mathf.RoundToInt(raw));
    }

    public bool CanRoll()
    {
        if (rollActive || Inventory == null || Catalog == null)
            return false;
        if (CountEligible() < 1)
            return false;
        return Inventory.gold >= GetCurrentRollCost();
    }

    public bool TryBeginRoll()
    {
        if (rollActive || Inventory == null || Catalog == null)
            return false;

        var eligible = BuildEligibleList();
        if (eligible.Count < 1)
            return false;

        int cost = GetCurrentRollCost();
        if (!Inventory.TrySpendGold(cost))
            return false;

        purchaseCount++;
        paidCost = cost;
        ClearOffers();

        int offerCount = Mathf.Min(OfferSlotCount, eligible.Count);
        for (int i = 0; i < offerCount; i++)
        {
            int pick = UnityEngine.Random.Range(i, eligible.Count);
            (eligible[i], eligible[pick]) = (eligible[pick], eligible[i]);
            offers[i] = eligible[i];
        }

        rollActive = true;
        SaveGameService.NotifyChanged();
        return true;
    }

    public int GetPreviewRefund(RelicSO relic)
    {
        if (!rollActive || relic == null || Inventory == null || Catalog == null)
            return 0;
        if (Inventory.OwnsRelic(relic))
            return 0;
        if (!skipCounts.TryGetValue(relic, out int skips) || skips <= 0)
            return 0;

        float percent = Mathf.Min(
            Catalog.skipRefundMaxPercent,
            Catalog.skipRefundBasePercent + Catalog.skipRefundIncreasePerSkip * skips);
        return Mathf.FloorToInt(paidCost * percent);
    }

    public bool TrySelectOffer(int index)
    {
        if (!rollActive || Inventory == null)
            return false;
        if (index < 0 || index >= OfferSlotCount)
            return false;

        RelicSO selected = offers[index];
        if (selected == null)
            return false;

        // Ownership before this buy — used for skip credit on the other offers.
        bool[] ownedBefore = new bool[OfferSlotCount];
        for (int i = 0; i < OfferSlotCount; i++)
        {
            RelicSO offer = offers[i];
            ownedBefore[i] = offer != null && Inventory.OwnsRelic(offer);
        }

        int refund = GetPreviewRefund(selected);
        if (refund > 0)
            Inventory.AddGold(refund);

        skipCounts.Remove(selected);
        Inventory.AddRelic(selected);

        for (int i = 0; i < OfferSlotCount; i++)
        {
            if (i == index)
                continue;
            RelicSO other = offers[i];
            if (other == null || ownedBefore[i])
                continue;
            skipCounts.TryGetValue(other, out int prev);
            skipCounts[other] = prev + 1;
        }

        ClearActiveRoll();
        SaveGameService.NotifyChanged();
        return true;
    }

    int CountEligible()
    {
        if (Catalog?.allRelics == null || Inventory == null)
            return 0;

        int count = 0;
        for (int i = 0; i < Catalog.allRelics.Count; i++)
        {
            if (IsEligible(Catalog.allRelics[i]))
                count++;
        }
        return count;
    }

    List<RelicSO> BuildEligibleList()
    {
        var list = new List<RelicSO>();
        if (Catalog?.allRelics == null)
            return list;

        for (int i = 0; i < Catalog.allRelics.Count; i++)
        {
            RelicSO relic = Catalog.allRelics[i];
            if (IsEligible(relic))
                list.Add(relic);
        }
        return list;
    }

    bool IsEligible(RelicSO relic)
    {
        if (relic == null)
            return false;
        if (!relic.allowMultiplePurchases && Inventory.OwnsRelic(relic))
            return false;
        return true;
    }

    void ClearOffers()
    {
        for (int i = 0; i < OfferSlotCount; i++)
            offers[i] = null;
    }

    void ClearActiveRoll()
    {
        rollActive = false;
        paidCost = 0;
        ClearOffers();
    }

    /// <summary>
    /// Clears purchase/skip progress and any mid-roll UI state for a fresh game.
    /// </summary>
    public void ResetForNewGame()
    {
        purchaseCount = 0;
        skipCounts.Clear();
        ClearActiveRoll();
    }

    /// <summary>
    /// Writes relic-shop economy fields into <paramref name="data"/>. Mid-roll UI is not saved.
    /// </summary>
    public void CaptureTo(GameSaveData data)
    {
        if (data == null)
            return;

        data.relicShopPurchaseCount = purchaseCount;

        if (skipCounts.Count == 0)
        {
            data.relicSkipCounts = Array.Empty<RelicSkipCountSaveData>();
            return;
        }

        var skips = new RelicSkipCountSaveData[skipCounts.Count];
        int i = 0;
        foreach (KeyValuePair<RelicSO, int> pair in skipCounts)
        {
            skips[i++] = new RelicSkipCountSaveData
            {
                relicId = SaveIdLookup.GetId(pair.Key),
                count = pair.Value
            };
        }
        data.relicSkipCounts = skips;
    }

    /// <summary>
    /// Restores purchase count and skip credits. Active roll UI stays cleared.
    /// </summary>
    public void ApplySaveState(int savedPurchaseCount, RelicSkipCountSaveData[] skips, SaveIdLookup lookup)
    {
        ClearActiveRoll();
        purchaseCount = Mathf.Max(0, savedPurchaseCount);
        skipCounts.Clear();

        if (skips == null || lookup == null)
            return;

        for (int i = 0; i < skips.Length; i++)
        {
            RelicSkipCountSaveData entry = skips[i];
            if (entry.count <= 0 || !lookup.TryGetRelic(entry.relicId, out RelicSO relic))
                continue;
            skipCounts[relic] = entry.count;
        }
    }

#if UNITY_EDITOR
    public void EditorAssign(Inventory inv, RelicShopCatalog shopCatalog)
    {
        inventory = inv;
        catalog = shopCatalog;
    }
#endif
}
