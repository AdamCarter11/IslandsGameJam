using UnityEngine;

public class CropStateResolver : MonoBehaviour
{
    const float MinGrowthTime = 0.05f;
    const float MinDryDeathTime = 0.05f;

    public float GetGrowthTime(
        CropPropertiesSO stage,
        CropGrowthSO crop,
        bool isWatered = true,
        RelicEffectContext context = default)
    {
        float value = RelicEffectUtility.ApplyModifiers(
            stage.growthTime,
            RelicEffectType.ModifyGrowthTime,
            crop,
            context);
        if (!isWatered)
        {
            float dryMult = GetDryGrowthMultiplier(stage, crop, context);
            // Rate multiplier: 0 = no dry growth, >1 = faster while dry.
            if (dryMult <= 0f)
                return float.PositiveInfinity;
            value /= dryMult;
        }
        return Mathf.Max(MinGrowthTime, value);
    }

    public float GetDryGrowthMultiplier(
        CropPropertiesSO stage,
        CropGrowthSO crop,
        RelicEffectContext context = default)
    {
        float value = RelicEffectUtility.ApplyModifiers(
            stage.dryGrowthMultiplier,
            RelicEffectType.ModifyDryGrowthMultiplier,
            crop,
            context);
        return Mathf.Max(0f, value);
    }

    public float GetDryDeathTime(CropGrowthSO crop, RelicEffectContext context = default)
    {
        float value = RelicEffectUtility.ApplyModifiers(
            crop.dryDeathTime,
            RelicEffectType.ModifyDryDeathTime,
            crop,
            context);
        return Mathf.Max(MinDryDeathTime, value);
    }

    public int GetDeathGold(CropGrowthSO crop, RelicEffectContext context = default)
    {
        float value = RelicEffectUtility.ApplyModifiers(
            crop.deathGold,
            RelicEffectType.ModifyDeathGold,
            crop,
            context);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public int GetGold(
        CropPropertiesSO stage,
        CropGrowthSO crop,
        RelicEffectContext context = default)
    {
        float value = RelicEffectUtility.ApplyModifiers(
            stage.goldGain,
            RelicEffectType.ModifyGold,
            crop,
            context);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public float GetMulti(
        CropPropertiesSO stage,
        CropGrowthSO crop,
        RelicEffectContext context = default)
    {
        float value = RelicEffectUtility.ApplyModifiers(
            stage.multiBonus,
            RelicEffectType.ModifyMulti,
            crop,
            context);
        return Mathf.Max(0f, value);
    }

    /// <summary>
    /// Starting chain multi after <see cref="RelicEffectType.ModifyBaseComboMulti"/> relics.
    /// Pass persisted multi (or 1) as <paramref name="baseMulti"/>.
    /// </summary>
    public float GetBaseComboMulti(float baseMulti = 1f, RelicEffectContext context = default)
    {
        float value = RelicEffectUtility.ApplyModifiers(
            baseMulti,
            RelicEffectType.ModifyBaseComboMulti,
            crop: null,
            context);
        return Mathf.Max(0f, value);
    }

    /// <summary>
    /// Shop seed cost after <see cref="RelicEffectType.ModifySeedPrice"/> relics.
    /// </summary>
    public int GetSeedPrice(CropGrowthSO crop, RelicEffectContext context = default)
    {
        if (crop == null)
            return 0;

        float value = RelicEffectUtility.ApplyModifiers(
            crop.seedPrice,
            RelicEffectType.ModifySeedPrice,
            crop,
            context);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }
}
