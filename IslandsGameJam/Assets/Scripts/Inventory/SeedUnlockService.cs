using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unlocks catalog seeds on island discovery: ordered early unlocks first, then rarity-weighted.
/// </summary>
public static class SeedUnlockService
{
    /// <summary>
    /// Unlocks one seed for an island unlock: next locked entry in
    /// <see cref="SeedShopCatalog.orderedUnlockSeeds"/>, otherwise a rarity-weighted pick
    /// among <see cref="SeedShopCatalog.allSeeds"/> not in that ordered list.
    /// No-op if neither pool has a candidate. Returns true if a seed was unlocked;
    /// <paramref name="unlocked"/> is the crop when successful, otherwise null.
    /// </summary>
    public static bool TryUnlockForIsland(Inventory inventory, SeedShopCatalog catalog, out CropGrowthSO unlocked)
    {
        unlocked = null;
        if (inventory == null || catalog == null || catalog.allSeeds == null)
            return false;

        if (TryUnlockNextOrdered(inventory, catalog, out unlocked))
            return true;

        return TryUnlockWeightedBonus(inventory, catalog, out unlocked);
    }

    static bool TryUnlockNextOrdered(Inventory inventory, SeedShopCatalog catalog, out CropGrowthSO unlocked)
    {
        unlocked = null;
        var ordered = catalog.orderedUnlockSeeds;
        if (ordered == null)
            return false;

        for (int i = 0; i < ordered.Count; i++)
        {
            var seed = ordered[i];
            if (seed == null || inventory.IsUnlocked(seed))
                continue;

            if (!inventory.TryUnlock(seed))
                return false;

            unlocked = seed;
            return true;
        }

        return false;
    }

    static bool TryUnlockWeightedBonus(Inventory inventory, SeedShopCatalog catalog, out CropGrowthSO unlocked)
    {
        unlocked = null;

        var orderedSet = BuildOrderedSet(catalog.orderedUnlockSeeds);
        var candidates = new List<CropGrowthSO>();
        foreach (var seed in catalog.allSeeds)
        {
            if (seed == null || inventory.IsUnlocked(seed))
                continue;
            if (orderedSet.Contains(seed))
                continue;
            candidates.Add(seed);
        }

        if (candidates.Count == 0)
            return false;

        var pick = candidates[PickWeightedIndex(candidates, catalog)];
        if (!inventory.TryUnlock(pick))
            return false;

        unlocked = pick;
        return true;
    }

    static HashSet<CropGrowthSO> BuildOrderedSet(List<CropGrowthSO> ordered)
    {
        var set = new HashSet<CropGrowthSO>();
        if (ordered == null)
            return set;

        for (int i = 0; i < ordered.Count; i++)
        {
            if (ordered[i] != null)
                set.Add(ordered[i]);
        }

        return set;
    }

    /// <summary>
    /// Weighted pick: chance ≈ weight(rarity) / sum(weights).
    /// Falls back to uniform if total weight is 0.
    /// </summary>
    static int PickWeightedIndex(List<CropGrowthSO> pool, SeedShopCatalog catalog)
    {
        float totalWeight = 0f;
        for (int i = 0; i < pool.Count; i++)
            totalWeight += catalog.GetWeight(pool[i].rarity);

        if (totalWeight <= 0f)
            return Random.Range(0, pool.Count);

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            cumulative += catalog.GetWeight(pool[i].rarity);
            if (roll < cumulative)
                return i;
        }

        return pool.Count - 1;
    }
}
