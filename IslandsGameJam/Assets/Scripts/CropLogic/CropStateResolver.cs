using UnityEngine;

public class CropStateResolver : MonoBehaviour
{
    const float MinGrowthTime = 0.05f;

    public float GetGrowthTime(CropPropertiesSO stage, CropGrowthSO crop)
    {
        float value = ApplyRelicModifiers(stage.growthTime, RelicEffectType.ModifyGrowthTime, crop);
        return Mathf.Max(MinGrowthTime, value);
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
