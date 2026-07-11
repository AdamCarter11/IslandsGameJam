using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns a HarvestPattern + origin into world grid cells.
/// Rays stop on out-of-bounds or empty (no crop) - occupied immature cells do not block (might want to change this)
/// </summary>
public static class HarvestPatternResolver
{
    /// <summary>
    /// Yields target cells for pattern relative to origin
    /// </summary>
    public static void Resolve(HarvestPattern pattern, Vector2Int origin, WorldManager world, List<Vector2Int> results)
    {
        if (pattern == null || world == null || results == null)
            return;

        switch (pattern.kind)
        {
            case HarvestPatternKind.Offsets:
                ResolveOffsets(pattern, origin, world, results);
                break;
            case HarvestPatternKind.Ray:
                ResolveRay(pattern, origin, world, results);
                break;
        }
    }

    #region Offset crop harvest logic
    static void ResolveOffsets(HarvestPattern pattern, Vector2Int origin, WorldManager world, List<Vector2Int> results)
    {
        if (pattern.offsets == null)
            return;

        for (int i = 0; i < pattern.offsets.Length; i++)
        {
            cellOffset offset = pattern.offsets[i];
            Vector2Int pos = new Vector2Int(origin.x + offset.x, origin.y + offset.y);
            if (!IsInBounds(world, pos))
                continue;
            results.Add(pos);
        }
    }
    #endregion

    #region Ray crop harvest logic
    static void ResolveRay(HarvestPattern pattern, Vector2Int origin, WorldManager world, List<Vector2Int> results)
    {
        Vector2Int dir = pattern.direction;
        if (dir == Vector2Int.zero)
            return;

        int maxSteps = pattern.maxSteps;
        Vector2Int pos = origin;

        for (int step = 0; maxSteps < 0 || step < maxSteps; step++)
        {
            pos += dir;
            if (!IsInBounds(world, pos))
                break;

            // Empty / no crop stops the ray (watermelon-style).
            if (!world.TryGetCrop(pos, out CropCell cell) || cell == null)
                break;

            results.Add(pos);
        }
    }
    #endregion

    static bool IsInBounds(WorldManager world, Vector2Int pos) => world.IsInWorldBounds(pos);
}
