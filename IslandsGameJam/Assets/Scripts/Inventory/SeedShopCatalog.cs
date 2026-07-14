using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeedShopCatalog", menuName = "Scriptable Objects/SeedShopCatalog")]
public class SeedShopCatalog : ScriptableObject
{
    [Tooltip("Full seed pool used for island unlock rolls and shop listings.")]
    public List<CropGrowthSO> allSeeds = new();

    [Tooltip("Early-island unlock sequence. Unlocks in list order first; must also appear in allSeeds.")]
    public List<CropGrowthSO> orderedUnlockSeeds = new();

    public int startingGold = 50;

    [Header("Rarity Weights")]
    [Tooltip("Relative weight for Common seeds in weighted unlock rolls (higher = more likely).")]
    public float commonWeight = 60f;
    [Tooltip("Relative weight for Rare seeds in weighted unlock rolls.")]
    public float rareWeight = 25f;
    [Tooltip("Relative weight for Epic seeds in weighted unlock rolls.")]
    public float epicWeight = 12f;
    [Tooltip("Relative weight for Legendary seeds in weighted unlock rolls.")]
    public float legendaryWeight = 3f;

    public float GetWeight(SeedRarity rarity)
    {
        switch (rarity)
        {
            case SeedRarity.Common:
                return commonWeight;
            case SeedRarity.Rare:
                return rareWeight;
            case SeedRarity.Epic:
                return epicWeight;
            case SeedRarity.Legendary:
                return legendaryWeight;
            default:
                return commonWeight;
        }
    }
}
