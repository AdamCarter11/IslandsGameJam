using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Picks a random still-locked catalog seed and unlocks it permanently upon island discovery
/// We might want to make island biomes influence this
/// </summary>
public static class SeedUnlockService
{
    /// <summary>
    /// Unlocks one random seed from the catalog that is not yet unlocked.
    /// No-op if none remain locked. Returns true if a seed was unlocked
    /// </summary>
    public static bool TryUnlockRandom(Inventory inventory, SeedShopCatalog catalog)
    {
        if (inventory == null || catalog == null || catalog.allSeeds == null)
            return false;

        var candidates = new List<CropGrowthSO>();
        foreach (var seed in catalog.allSeeds)
        {
            if (seed != null && !inventory.IsUnlocked(seed))
                candidates.Add(seed);
        }

        if (candidates.Count == 0)
            return false;

        var pick = candidates[Random.Range(0, candidates.Count)];
        return inventory.TryUnlock(pick);
    }
}
