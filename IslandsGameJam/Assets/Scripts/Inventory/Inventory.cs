using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public const int HotbarSlotCount = 10;

    // --- Relic inventory management ---
    public List<RelicSO> ownedRelics = new();

    public event Action OnRelicsChanged;

    public void AddRelic(RelicSO relic)
    {
        if (relic == null)
            return;
        ownedRelics.Add(relic);
        OnRelicsChanged?.Invoke();
    }

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
    /// Clears hotbar, owned relics, and shop unlocks so a New Game never inherits leftover state.
    /// Call before <see cref="InitializeFromCatalog"/>.
    /// </summary>
    public void ClearForNewGame()
    {
        for (int i = 0; i < HotbarSlotCount; i++)
            slots[i].Clear();
        OnHotbarChanged?.Invoke();

        ownedRelics.Clear();

        unlockedSeeds.Clear();
        OnShopUnlocksChanged?.Invoke();
    }

    /// <summary>
    /// Sets starting gold and default unlocks from the shop catalog.
    /// Prefer <see cref="ClearForNewGame"/> first when starting a new run.
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

    /// <summary>
    /// Writes gold, hotbar, unlocks, and owned relics into <paramref name="data"/>.
    /// </summary>
    public void CaptureTo(GameSaveData data)
    {
        if (data == null)
            return;

        data.gold = gold;

        data.hotbar = new HotbarSlotSaveData[HotbarSlotCount];
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            SeedStack slot = slots[i];
            data.hotbar[i] = new HotbarSlotSaveData
            {
                cropId = slot.IsEmpty ? null : SaveIdLookup.GetId(slot.crop),
                count = slot.IsEmpty ? 0 : slot.count
            };
        }

        var unlocked = new List<string>(unlockedSeeds.Count);
        foreach (CropGrowthSO crop in unlockedSeeds)
        {
            string id = SaveIdLookup.GetId(crop);
            if (!string.IsNullOrEmpty(id))
                unlocked.Add(id);
        }
        data.unlockedCropIds = unlocked.ToArray();

        data.ownedRelicIds = new string[ownedRelics.Count];
        for (int i = 0; i < ownedRelics.Count; i++)
            data.ownedRelicIds[i] = SaveIdLookup.GetId(ownedRelics[i]);
    }

    /// <summary>
    /// Restores gold, hotbar, unlocks, and owned relics from <paramref name="data"/>.
    /// </summary>
    public void ApplyFrom(GameSaveData data, SaveIdLookup lookup)
    {
        if (data == null || lookup == null)
            return;

        gold = data.gold;
        OnGoldChanged?.Invoke(gold);

        for (int i = 0; i < HotbarSlotCount; i++)
        {
            slots[i].Clear();
            if (data.hotbar == null || i >= data.hotbar.Length)
                continue;

            HotbarSlotSaveData saved = data.hotbar[i];
            if (saved.count <= 0 || !lookup.TryGetCrop(saved.cropId, out CropGrowthSO crop))
                continue;

            slots[i].crop = crop;
            slots[i].count = Mathf.Clamp(saved.count, 1, SeedStack.MaxStackSize);
        }
        OnHotbarChanged?.Invoke();

        unlockedSeeds.Clear();
        if (data.unlockedCropIds != null)
        {
            for (int i = 0; i < data.unlockedCropIds.Length; i++)
            {
                if (lookup.TryGetCrop(data.unlockedCropIds[i], out CropGrowthSO crop))
                    unlockedSeeds.Add(crop);
            }
        }
        OnShopUnlocksChanged?.Invoke();

        ownedRelics.Clear();
        if (data.ownedRelicIds != null)
        {
            for (int i = 0; i < data.ownedRelicIds.Length; i++)
            {
                if (lookup.TryGetRelic(data.ownedRelicIds[i], out RelicSO relic))
                    ownedRelics.Add(relic);
            }
        }
        OnRelicsChanged?.Invoke();
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
