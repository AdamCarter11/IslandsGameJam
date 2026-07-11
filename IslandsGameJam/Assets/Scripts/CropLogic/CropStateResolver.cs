using UnityEngine;

public class CropStateResolver : MonoBehaviour
{
    const float MinGrowthTime = 0.05f;
    const float MinDryDeathTime = 0.05f;

    public float GetGrowthTime(CropPropertiesSO stage, CropGrowthSO crop, bool isWatered = true)
    {
        float value = ApplyRelicModifiers(stage.growthTime, RelicEffectType.ModifyGrowthTime, crop);
        if (!isWatered)
        {
            float dryMult = GetDryGrowthMultiplier(stage, crop);
            // Rate multiplier: 0 = no dry growth, >1 = faster while dry.
            if (dryMult <= 0f)
                return float.PositiveInfinity;
            value /= dryMult;
        }
        return Mathf.Max(MinGrowthTime, value);
    }

    public float GetDryGrowthMultiplier(CropPropertiesSO stage, CropGrowthSO crop)
    {
        float value = ApplyRelicModifiers(stage.dryGrowthMultiplier, RelicEffectType.ModifyDryGrowthMultiplier, crop);
        return Mathf.Max(0f, value);
    }

    public float GetDryDeathTime(CropGrowthSO crop)
    {
        float value = ApplyRelicModifiers(crop.dryDeathTime, RelicEffectType.ModifyDryDeathTime, crop);
        return Mathf.Max(MinDryDeathTime, value);
    }

    public int GetDeathGold(CropGrowthSO crop)
    {
        float value = ApplyRelicModifiers(crop.deathGold, RelicEffectType.ModifyDeathGold, crop);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public int GetGold(CropPropertiesSO stage, CropGrowthSO crop)
    {
        float value = ApplyRelicModifiers(stage.goldGain, RelicEffectType.ModifyGold, crop);
        return Mathf.Max(0, Mathf.RoundToInt(value));
    }

    public float GetMulti(CropPropertiesSO stage, CropGrowthSO crop)
    {
        float value = ApplyRelicModifiers(stage.multiBonus, RelicEffectType.ModifyMulti, crop);
        return Mathf.Max(0f, value);
    }

    float ApplyRelicModifiers(float baseValue, RelicEffectType effectType, CropGrowthSO crop)
    {
        float value = baseValue;
        foreach (var relic in GameManager.Main.Inventory.ownedRelics)
        {
            if (relic?.effects == null) continue;

            foreach (var effect in relic.effects)
            {
                if (effect == null)
                {
                    Debug.LogError("The effect on relic is null: " + relic.relicName);
                    continue;
                }
                if (effect.type != effectType) continue;
                if (effect.onlyCrop != null && effect.onlyCrop != crop) continue;
                value = effect.multiplicative ? value * effect.amount : value + effect.amount;
            }
        }
        return value;
    }
}
