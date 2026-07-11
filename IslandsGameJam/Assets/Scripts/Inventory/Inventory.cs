using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public const int HotbarSlotCount = 10;

    // --- Relic inventory management ---
    public List<RelicSO> ownedRelics = new();
    public void AddRelic(RelicSO relic) => ownedRelics.Add(relic);

    public bool OwnsRelic(RelicSO relic)
    {
        if (relic == null)
            return false;
        for (int i = 0; i < ownedRelics.Count; i++)
        {
            if (ownedRelics[i] == relic)
                return true;
        }
        return false;
    }

    public int CountOwned(RelicSO relic)
    {
        if (relic == null)
            return 0;
        int count = 0;
        for (int i = 0; i < ownedRelics.Count; i++)
        {
            if (ownedRelics[i] == relic)
                count++;
        }
        return count;
    }

    // --- Gold ---
    public int gold;

    public event Action<int> OnGoldChanged;

    public void AddGold(int amount)
    {
        if (amount == 0)
            return;
        gold += amount;
        OnGoldChanged?.Invoke(gold);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || gold < amount)
            return false;
        gold -= amount;
        OnGoldChanged?.Invoke(gold);
        return true;
    }

    // --- Seed hotbar ---
    readonly SeedStack[] slots = new SeedStack[HotbarSlotCount];
    int selectedSlot;

    public event Action OnHotbarChanged;
    public event Action<int> OnSelectedSlotChanged;

    public int SelectedSlot => selectedSlot;
    public IReadOnlyList<SeedStack> Slots => slots;

    // --- Shop unlocks ---
    readonly HashSet<CropGrowthSO> unlockedSeeds = new();

    public event Action OnShopUnlocksChanged;

    public IReadOnlyCollection<CropGrowthSO> UnlockedSeeds => unlockedSeeds;

    void Awake()
    {
        for (int i = 0; i < HotbarSlotCount; i++)
            slots[i] = new SeedStack();
    }

    /// <summary>
    /// Sets starting gold and default unlocks from the shop catalog. we will need to change where this is called when/if we add saving 
    /// </summary>
    public void InitializeFromCatalog(SeedShopCatalog catalog)
    {
        if (catalog == null)
            return;

        gold = catalog.startingGold;
        OnGoldChanged?.Invoke(gold);

        unlockedSeeds.Clear();
        if (catalog.allSeeds != null)
        {
            foreach (var seed in catalog.allSeeds)
            {
                if (seed != null && seed.unlockedByDefault)
                    unlockedSeeds.Add(seed);
            }
        }

        OnShopUnlocksChanged?.Invoke();
    }

    public bool IsUnlocked(CropGrowthSO crop) => crop != null && unlockedSeeds.Contains(crop);

    public bool TryUnlock(CropGrowthSO crop)
    {
        if (crop == null || unlockedSeeds.Contains(crop))
            return false;
        unlockedSeeds.Add(crop);
        OnShopUnlocksChanged?.Invoke();
        return true;
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= HotbarSlotCount)
            return;
        if (selectedSlot == index)
            return;
        selectedSlot = index;
        OnSelectedSlotChanged?.Invoke(selectedSlot);
    }

    public SeedStack GetSlot(int index)
    {
        if (index < 0 || index >= HotbarSlotCount)
            return null;
        return slots[index];
    }

    public bool CanFitSeed(CropGrowthSO crop, int count = 1)
    {
        if (crop == null || count <= 0)
            return false;

        int remaining = count;

        for (int i = 0; i < HotbarSlotCount; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty || slot.crop != crop)
                continue;
            int space = SeedStack.MaxStackSize - slot.count;
            if (space <= 0)
                continue;
            remaining -= space;
            if (remaining <= 0)
                return true;
        }

        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (!slots[i].IsEmpty)
                continue;
            remaining -= SeedStack.MaxStackSize;
            if (remaining <= 0)
                return true;
        }

        return false;
    }

    public bool TryAddSeeds(CropGrowthSO crop, int count)
    {
        if (!CanFitSeed(crop, count))
            return false;

        int remaining = count;

        // Stack into existing same-type stacks first.
        for (int i = 0; i < HotbarSlotCount && remaining > 0; i++)
        {
            var slot = slots[i];
            if (slot.IsEmpty || slot.crop != crop)
                continue;
            int space = SeedStack.MaxStackSize - slot.count;
            if (space <= 0)
                continue;
            int add = Mathf.Min(space, remaining);
            slot.count += add;
            remaining -= add;
        }

        // Then fill empty slots.
        for (int i = 0; i < HotbarSlotCount && remaining > 0; i++)
        {
            var slot = slots[i];
            if (!slot.IsEmpty)
                continue;
            int add = Mathf.Min(SeedStack.MaxStackSize, remaining);
            slot.crop = crop;
            slot.count = add;
            remaining -= add;
        }

        OnHotbarChanged?.Invoke();
        return true;
    }

    public bool TryConsumeSelected(out CropGrowthSO crop)
    {
        crop = null;
        var slot = slots[selectedSlot];
        if (slot.IsEmpty)
            return false;

        crop = slot.crop;
        slot.count--;
        if (slot.count <= 0)
            slot.Clear();

        OnHotbarChanged?.Invoke();
        return true;
    }

    public bool TryBuySeed(CropGrowthSO crop)
    {
        if (crop == null || !IsUnlocked(crop))
            return false;
        if (!CanFitSeed(crop, 1))
            return false;
        if (!TrySpendGold(crop.seedPrice))
            return false;
        if (!TryAddSeeds(crop, 1))
        {
            // Should not happen after CanFitSeed + spend; refund to stay consistent.
            AddGold(crop.seedPrice);
            return false;
        }
        return true;
    }
}
