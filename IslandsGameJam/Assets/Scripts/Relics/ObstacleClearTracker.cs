using UnityEngine;

/// <summary>
/// Tracks how many obstacles have been cleared this run so Pathfinder-style
/// <see cref="RelicEffectType.EnableClearObstacles"/> costs scale: base * multiplier^clears.
/// </summary>
public static class ObstacleClearTracker
{
    public const float DefaultCostMultiplier = 2f;

    static int clearedCount;

    public static int ClearedCount => clearedCount;

    public static bool TryGetCurrentCost(out int cost)
    {
        cost = 0;
        if (!RelicEffectUtility.HasEffect(RelicEffectType.EnableClearObstacles))
            return false;

        float bestBase = float.PositiveInfinity;
        float bestMultiplier = DefaultCostMultiplier;
        RelicEffectUtility.ForEachEffect(RelicEffectType.EnableClearObstacles, effect =>
        {
            if (effect.amount >= bestBase)
                return;

            bestBase = effect.amount;
            bestMultiplier = effect.costMultiplier > 1f
                ? effect.costMultiplier
                : DefaultCostMultiplier;
        });

        if (float.IsPositiveInfinity(bestBase))
            return false;

        float raw = bestBase * Mathf.Pow(bestMultiplier, clearedCount);
        cost = Mathf.Max(0, Mathf.RoundToInt(raw));
        return true;
    }

    public static void RegisterClear()
    {
        clearedCount++;
    }

    public static void CaptureTo(GameSaveData data)
    {
        if (data == null)
            return;
        data.obstaclesClearedCount = clearedCount;
    }

    public static void ApplyFrom(GameSaveData data)
    {
        clearedCount = data != null ? Mathf.Max(0, data.obstaclesClearedCount) : 0;
    }

    public static void Reset()
    {
        clearedCount = 0;
    }
}
