using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional per-call context for relic effect filters (terrain, watered, single-offset).
/// Unset fields mean the caller did not supply that info; filter requirements then fail closed.
/// </summary>
public readonly struct RelicEffectContext
{
    public readonly bool? IsWatered;
    public readonly TerrainType? Terrain;
    public readonly bool? IsSingleOffset;

    public RelicEffectContext(bool? isWatered = null, TerrainType? terrain = null, bool? isSingleOffset = null)
    {
        IsWatered = isWatered;
        Terrain = terrain;
        IsSingleOffset = isSingleOffset;
    }

    public static RelicEffectContext None => default;

    public static RelicEffectContext ForHarvest(bool isWatered, TerrainType terrain, HarvestPattern pattern)
    {
        return new RelicEffectContext(
            isWatered,
            terrain,
            RelicEffectUtility.IsSingleOffsetPattern(pattern));
    }
}

/// <summary>
/// Shared queries over <see cref="Inventory.ownedRelics"/> for resolvers and systems.
/// </summary>
public static class RelicEffectUtility
{
    public static bool IsSingleOffsetPattern(HarvestPattern pattern)
    {
        return pattern != null
            && pattern.kind == HarvestPatternKind.Offsets
            && pattern.offsets != null
            && pattern.offsets.Length == 1;
    }

    public static bool MatchesFilters(RelicEffect effect, CropGrowthSO crop, RelicEffectContext context = default)
    {
        if (effect == null)
            return false;

        if (effect.onlyCrop != null && effect.onlyCrop != crop)
            return false;

        if (effect.filterByTerrain)
        {
            if (!context.Terrain.HasValue || context.Terrain.Value != effect.terrainType)
                return false;
        }

        if (effect.requireUnwatered)
        {
            if (!context.IsWatered.HasValue || context.IsWatered.Value)
                return false;
        }

        if (effect.requireSingleOffset)
        {
            if (!context.IsSingleOffset.HasValue || !context.IsSingleOffset.Value)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns true if <paramref name="chance"/> succeeds. Values &gt;= 1 always succeed; &lt;= 0 always fail.
    /// </summary>
    public static bool RollChance(float chance)
    {
        if (chance >= 1f)
            return true;
        if (chance <= 0f)
            return false;
        return UnityEngine.Random.value < chance;
    }

    public static bool RollChance(RelicEffect effect)
    {
        if (effect == null)
            return false;
        return RollChance(effect.chance);
    }

    public static bool HasEffect(RelicEffectType type, CropGrowthSO crop = null, RelicEffectContext context = default)
    {
        var relics = GetOwnedRelics();
        if (relics == null)
            return false;

        for (int r = 0; r < relics.Count; r++)
        {
            RelicSO relic = relics[r];
            if (relic?.effects == null)
                continue;

            for (int e = 0; e < relic.effects.Length; e++)
            {
                RelicEffect effect = relic.effects[e];
                if (effect == null)
                {
                    Debug.LogError("The effect on relic is null: " + relic.relicName);
                    continue;
                }

                if (effect.type != type)
                    continue;
                if (!MatchesFilters(effect, crop, context))
                    continue;
                return true;
            }
        }

        return false;
    }

    public static void ForEachEffect(
        RelicEffectType type,
        CropGrowthSO crop,
        RelicEffectContext context,
        Action<RelicEffect> action)
    {
        if (action == null)
            return;

        var relics = GetOwnedRelics();
        if (relics == null)
            return;

        for (int r = 0; r < relics.Count; r++)
        {
            RelicSO relic = relics[r];
            if (relic?.effects == null)
                continue;

            for (int e = 0; e < relic.effects.Length; e++)
            {
                RelicEffect effect = relic.effects[e];
                if (effect == null)
                {
                    Debug.LogError("The effect on relic is null: " + relic.relicName);
                    continue;
                }

                if (effect.type != type)
                    continue;
                if (!MatchesFilters(effect, crop, context))
                    continue;

                action(effect);
            }
        }
    }

    public static void ForEachEffect(RelicEffectType type, Action<RelicEffect> action)
    {
        ForEachEffect(type, null, RelicEffectContext.None, action);
    }

    /// <summary>
    /// Applies additive then multiplicative stacking in encounter order (same rules as the old resolver).
    /// Does not roll <see cref="RelicEffect.chance"/> — callers that need chance use <see cref="RollChance"/>.
    /// </summary>
    public static float ApplyModifiers(
        float baseValue,
        RelicEffectType type,
        CropGrowthSO crop = null,
        RelicEffectContext context = default)
    {
        float value = baseValue;
        ForEachEffect(type, crop, context, effect =>
        {
            value = effect.multiplicative ? value * effect.amount : value + effect.amount;
        });
        return value;
    }

    static IList<RelicSO> GetOwnedRelics()
    {
        return GameManager.Main?.Inventory?.ownedRelics;
    }
}
