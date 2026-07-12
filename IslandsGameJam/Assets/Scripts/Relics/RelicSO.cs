using UnityEngine;

public enum RelicRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
}

public enum RelicEffectType
{
    // Continuous modifiers (resolver)
    ModifyGold,
    ModifyMulti,
    ModifyGrowthTime,
    ModifyDryGrowthMultiplier,
    ModifyDryDeathTime,
    ModifyDeathGold,
    ModifyBaseComboMulti,
    ModifySeedPrice,

    // Harvest / death / destroy hooks
    OnHarvestSpawnTile,
    OnHarvestExtraPattern,
    OnHarvestMirrorOpposite,
    OnCropDeathSpawnTile,
    OnCropDeathAdjacentUnwateredGold,
    OnCropDeathStreakMulti,
    OnComboEveryNGold,
    OnComboAtNAddMulti,
    OnHarvestEndComboEqualsGold,
    OnHarvestEndComboLessThanRandomSeed,
    PersistComboMulti,
    PersistComboMultiEveryOther,
    PersistComboMultiOnEndCombo,
    OnDestroyHarvestableGold,
    OnDestroyHarvestableReturnSeeds,
    OnDestroyHarvestableNextStartMulti,

    // Shop / buy hooks
    OnBuySeedNextStartMulti,
    OnBuySeedRefundChance,
    OnBuySeedRelicDiscount,
    OnBuySeedGainGold,

    // World interaction
    EnableClearObstacles,
}

[CreateAssetMenu(fileName = "Relic", menuName = "Scriptable Objects/Relics")]
public class RelicSO : ScriptableObject
{
    public string relicName;
    [TextArea(2, 6)] public string desc;
    public RelicEffect[] effects;

    [Tooltip("Shop roll rarity; higher rarities use lower catalog weights.")]
    public RelicRarity rarity = RelicRarity.Common;

    [Tooltip("If true, this relic can appear in the shop again after purchase and stack via multiple AddRelic calls.")]
    public bool allowMultiplePurchases;

    [Tooltip("Optional icon shown on relic choice cards.")]
    public Sprite shopIcon;
}

[System.Serializable]
public class RelicEffect
{
    public RelicEffectType type;
    public float amount;
    [Tooltip("false means additive, true means multiplicative")]
    public bool multiplicative;

    [Tooltip("If you want this relic to only affect certain types of crops, set those here, otherwise null means all crops")]
    public CropGrowthSO onlyCrop;
    [Tooltip("If we want spawn-tile effects (we might not care about this)")]
    public TerrainData tileToSpawn;
    [Tooltip("Optional extra harvest pattern for OnHarvestExtraPattern relics")]
    public HarvestPattern extraPattern;

    [Header("Optional filters (defaults = no filter)")]
    [Tooltip("When true, effect only applies on the given terrain type.")]
    public bool filterByTerrain;
    public TerrainType terrainType = TerrainType.Plains;

    [Tooltip("When true, effect only applies to unwatered crops.")]
    public bool requireUnwatered;

    [Tooltip("When true, effect only applies when the ready-stage harvest pattern is Offsets with exactly one offset.")]
    public bool requireSingleOffset;

    [Header("Optional params")]
    [Tooltip("Probability this effect fires (0-1). Used by chance-based hooks like refund / seed return.")]
    [Range(0f, 1f)]
    public float chance = 1f;

    [Tooltip("Combo / milestone value (every N, at N, end combo equals/less-than, persist-on-end-combo).")]
    public int threshold;
}
