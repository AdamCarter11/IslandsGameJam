using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "RelicShopCatalog", menuName = "Scriptable Objects/RelicShopCatalog")]
public class RelicShopCatalog : ScriptableObject
{
    [Tooltip("Full relic pool used for shop rolls.")]
    public List<RelicSO> allRelics = new();

    [Tooltip("Gold cost of the first relic roll.")]
    public int baseRollCost = 25;

    [Tooltip("Each purchase multiplies cost: round(base * multiplier^purchaseCount).")]
    public float costMultiplierPerPurchase = 1.5f;

    [Tooltip("Refund percent of this roll's paid cost when taking a never-owned relic that was skipped before.")]
    public float skipRefundBasePercent = 0.10f;

    [Tooltip("Added to refund percent per prior skip of that relic.")]
    public float skipRefundIncreasePerSkip = 0.05f;

    [Tooltip("Cap on skip refund percent.")]
    public float skipRefundMaxPercent = 0.50f;

    [Header("Rarity Weights")]
    [Tooltip("Relative weight for Common relics in shop rolls (higher = more likely).")]
    public float commonWeight = 60f;
    [Tooltip("Relative weight for Rare relics in shop rolls.")]
    public float rareWeight = 25f;
    [Tooltip("Relative weight for Epic relics in shop rolls.")]
    public float epicWeight = 12f;
    [Tooltip("Relative weight for Legendary relics in shop rolls.")]
    public float legendaryWeight = 3f;

    public float GetWeight(RelicRarity rarity)
    {
        switch (rarity)
        {
            case RelicRarity.Common:
                return commonWeight;
            case RelicRarity.Rare:
                return rareWeight;
            case RelicRarity.Epic:
                return epicWeight;
            case RelicRarity.Legendary:
                return legendaryWeight;
            default:
                return commonWeight;
        }
    }
}
