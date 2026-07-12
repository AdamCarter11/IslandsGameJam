using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves ScriptableObject identity by Unity asset <see cref="Object.name"/>
/// against seed/relic catalogs and terrain assets.
/// </summary>
public sealed class SaveIdLookup
{
    readonly Dictionary<string, CropGrowthSO> cropsById = new();
    readonly Dictionary<string, RelicSO> relicsById = new();
    readonly Dictionary<string, TerrainData> terrainsById = new();

    public SaveIdLookup(
        SeedShopCatalog seedCatalog,
        RelicShopCatalog relicCatalog,
        IEnumerable<TerrainData> terrains)
    {
        if (seedCatalog?.allSeeds != null)
        {
            for (int i = 0; i < seedCatalog.allSeeds.Count; i++)
            {
                CropGrowthSO crop = seedCatalog.allSeeds[i];
                if (crop == null || string.IsNullOrEmpty(crop.name))
                    continue;
                cropsById[crop.name] = crop;
            }
        }

        if (relicCatalog?.allRelics != null)
        {
            for (int i = 0; i < relicCatalog.allRelics.Count; i++)
            {
                RelicSO relic = relicCatalog.allRelics[i];
                if (relic == null || string.IsNullOrEmpty(relic.name))
                    continue;
                relicsById[relic.name] = relic;
            }
        }

        if (terrains != null)
        {
            foreach (TerrainData terrain in terrains)
            {
                if (terrain == null || string.IsNullOrEmpty(terrain.name))
                    continue;
                terrainsById[terrain.name] = terrain;
            }
        }
    }

    public static string GetId(Object asset) => asset != null ? asset.name : null;

    public bool TryGetCrop(string id, out CropGrowthSO crop)
    {
        if (string.IsNullOrEmpty(id))
        {
            crop = null;
            return false;
        }
        return cropsById.TryGetValue(id, out crop);
    }

    public bool TryGetRelic(string id, out RelicSO relic)
    {
        if (string.IsNullOrEmpty(id))
        {
            relic = null;
            return false;
        }
        return relicsById.TryGetValue(id, out relic);
    }

    public bool TryGetTerrain(string id, out TerrainData terrain)
    {
        if (string.IsNullOrEmpty(id))
        {
            terrain = null;
            return false;
        }
        return terrainsById.TryGetValue(id, out terrain);
    }
}
