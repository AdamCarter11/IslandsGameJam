using UnityEngine;

public enum RelicEffectType
{
    ModifyGold,
    ModifyMulti,
    ModifyGrowthTime,
    ModifyDryGrowthMultiplier,
    ModifyDryDeathTime,
    ModifyDeathGold,
    OnHarvestSpawnTile,
    OnHarvestExtraPattern,
    OnCropDeathSpawnTile,
}

[CreateAssetMenu(fileName = "Relic", menuName = "Scriptable Objects/Relics")]
public class RelicSO : ScriptableObject
{
    public string relicName;
    [TextArea(2, 6)] public string desc;
    public RelicEffect[] effects;
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
}
