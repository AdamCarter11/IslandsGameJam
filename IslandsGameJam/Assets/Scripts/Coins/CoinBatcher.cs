using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Greedy decomposition of a gold payout into configured coin denominations,
/// with a chance to split larger coins into smaller ones.
/// </summary>
public static class CoinBatcher
{
    const float SplitChance = 0.25f;

    /// <summary>
    /// Walks denominations high to low and emits one entry per coin to spawn.
    /// Each time a denomination would be used, there is a SplitChance
    /// to instead fill that value with smaller denominations (ie, one 8 coin splits into two 4s).
    /// </summary>
    public static List<int> Decompose(int amount, IReadOnlyList<CoinDenomination> dens)
    {
        #region Grab coin denominations and sort them from largest to smallest
        var result = new List<int>();
        if (amount <= 0 || dens == null || dens.Count == 0)
            return result;

        var sorted = new List<CoinDenomination>(dens.Count);
        for (int i = 0; i < dens.Count; i++)
        {
            if (dens[i].amount > 0)
                sorted.Add(dens[i]);
        }

        sorted.Sort((a, b) => b.amount.CompareTo(a.amount));
        #endregion

        int remaining = amount;
        for (int i = 0; i < sorted.Count; i++)
        {
            int denom = sorted[i].amount;
            while (remaining >= denom)
            {
                // random chance to split into smaller denominations for some variance
                if (Random.value < SplitChance && TryAppendSplit(result, denom, sorted, i + 1))
                {
                    remaining -= denom;
                    continue;
                }

                result.Add(denom);
                remaining -= denom;
            }
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"CoinBatcher: leftover {remaining} after greedy decompose (amount={amount}). Ensure denomination list includes 1.");
        }

        return result;
    }

    /// <summary>
    /// Appends coins totaling exactly value using dens at fromIndex and below.
    /// Returns false (and writes nothing) if value cannot be formed from those dens
    /// </summary>
    static bool TryAppendSplit(List<int> result, int value, List<CoinDenomination> sorted, int fromIndex)
    {
        if (fromIndex >= sorted.Count)
            return false;

        int startCount = result.Count;
        int remaining = value;
        for (int i = fromIndex; i < sorted.Count; i++)
        {
            int denom = sorted[i].amount;
            while (remaining >= denom)
            {
                result.Add(denom);
                remaining -= denom;
            }
        }

        if (remaining > 0)
        {
            result.RemoveRange(startCount, result.Count - startCount);
            return false;
        }

        return true;
    }
}
