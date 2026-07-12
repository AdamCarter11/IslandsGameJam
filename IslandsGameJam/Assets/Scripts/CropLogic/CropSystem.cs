using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns crop growth ticks, planted-cell tracking, drought death, and chain harvest.
/// </summary>
public class CropSystem : MonoBehaviour
{
    [SerializeField] private CropGrowthSO debugCropToPlant;
    [SerializeField] private CoinDropService coinDropService;

    [Header("Harvest hop")]
    [SerializeField] private float popDuration = 0.12f;
    [SerializeField] private float hopDuration = 0.22f;
    [SerializeField] private float hopHeight = 0.5f;
    [SerializeField] private int flyerSortingOrder = 10;
    [SerializeField] private string flyerSortingLayer = "Overlay";

    readonly List<Vector2Int> plantedPositions = new List<Vector2Int>();
    readonly Queue<Vector2Int> harvestQueue = new Queue<Vector2Int>();
    readonly HashSet<Vector2Int> harvestVisited = new HashSet<Vector2Int>();
    readonly List<Vector2Int> patternBuffer = new List<Vector2Int>();

    public CropGrowthSO DebugCropToPlant => debugCropToPlant;
    public IReadOnlyList<Vector2Int> PlantedPositions => plantedPositions;
    public bool IsHarvestBusy { get; private set; }

    /// <summary>
    /// Plants via WorldManager and registers the cell for growth updates
    /// </summary>
    public bool PlantCrop(Vector2Int position, CropGrowthSO crop)
    {
        var world = GameManager.Main.WorldManager;
        if (!world.PlantCrop(position, crop))
            return false;

        if (!plantedPositions.Contains(position))
            plantedPositions.Add(position);

        GameManager.Main?.AudioService?.PlayPlant();
        SaveGameService.NotifyChanged();
        return true;
    }

    /// <summary>
    /// Removes a cell from the growth list (e.g. after harvest/clear)
    /// </summary>
    public void UnregisterPlanted(Vector2Int position)
    {
        plantedPositions.Remove(position);
    }

    /// <summary>
    /// Waters a planted crop at the given cell. One water lasts until harvest/death.
    /// </summary>
    public void WaterAt(Vector2Int position)
    {
        var world = GameManager.Main.WorldManager;
        if (world == null)
            return;

        if (!world.TryGetCrop(position, out CropCell cell) || cell == null)
            return;

        if (cell.isWatered)
            return;

        cell.Water();
        GameManager.Main?.AudioService?.PlayWater();
        SaveGameService.NotifyChanged();
    }

    /// <summary>
    /// Removes a plant from a single cell with no coin drops and no death/harvest relics.
    /// </summary>
    public void DestroyAt(Vector2Int position)
    {
        var world = GameManager.Main.WorldManager;
        if (world == null)
            return;

        if (!world.TryGetCrop(position, out CropCell cell) || cell == null)
            return;

        world.ClearCrop(position);
        UnregisterPlanted(position);
        GameManager.Main?.AudioService?.PlayDestroy();
        SaveGameService.NotifyChanged();
    }

    /// <summary>
    /// Drought-kills a crop: death gold drops, OnCropDeath relic side-effects, then clear.
    /// No harvest chain / multi.
    /// </summary>
    public void KillAt(Vector2Int position)
    {
        var world = GameManager.Main.WorldManager;
        var resolver = GameManager.Main.CropStateResolver;
        if (world == null || resolver == null)
            return;

        if (!world.TryGetCrop(position, out CropCell cell) || cell == null || cell.crop == null)
            return;

        CropGrowthSO crop = cell.crop;
        int deathGold = resolver.GetDeathGold(crop);
        if (coinDropService != null && deathGold > 0)
            coinDropService.SpawnDrops(new Vector2(position.x, position.y), deathGold);

        world.ClearCrop(position);
        UnregisterPlanted(position);
        ApplyCropDeathSpawnTile(position, crop, world);
        GameManager.Main?.AudioService?.PlayKill();
        SaveGameService.NotifyChanged();
    }

    /// <summary>
    /// BFS chain harvest from a ready crop. A flyer pops at the origin then hops
    /// cell-to-cell; each landing runs gold * multi, coin drops, pattern enqueue, and clear.
    /// Immature pattern targets are skipped (not cleared) - might want to change this, design decision
    /// empty cells stop rays
    /// </summary>
    public void HarvestAt(Vector2Int origin)
    {
        if (IsHarvestBusy)
            return;

        var world = GameManager.Main.WorldManager;
        var resolver = GameManager.Main.CropStateResolver;
        if (world == null || resolver == null || coinDropService == null)
            return;

        if (!world.TryGetCrop(origin, out CropCell originCell) || originCell == null || !originCell.IsReady)
            return;

        StartCoroutine(HarvestChainRoutine(origin));
    }

    IEnumerator HarvestChainRoutine(Vector2Int origin)
    {
        var world = GameManager.Main.WorldManager;
        var resolver = GameManager.Main.CropStateResolver;
        if (world == null || resolver == null || coinDropService == null)
            yield break;

        IsHarvestBusy = true;
        HarvestHopVisual flyer = null;

        try
        {
            float multi = 1f;
            int comboIndex = 0;
            harvestQueue.Clear();
            harvestVisited.Clear();

            Sprite originSprite = null;
            if (world.TryGetCrop(origin, out CropCell originCell) && originCell?.crop != null)
                originSprite = originCell.crop.GetHarvestBounceVisual();

            flyer = HarvestHopVisual.Spawn(
                transform,
                CellToWorld(origin),
                originSprite,
                flyerSortingOrder,
                flyerSortingLayer);

            yield return flyer.Pop(popDuration);

            if (!HarvestOneCell(origin, world, resolver, ref multi, ref comboIndex))
                yield break;

            Vector2Int current = origin;

            while (harvestQueue.Count > 0)
            {
                Vector2Int pos = harvestQueue.Dequeue();
                if (harvestVisited.Contains(pos))
                    continue;

                if (!world.TryGetCrop(pos, out CropCell cell) || cell == null || !cell.IsReady)
                    continue;

                if (cell.crop == null || cell.CurrentStage == null)
                    continue;

                flyer.SetSprite(cell.crop.GetHarvestBounceVisual());
                yield return flyer.Hop(CellToWorld(current), CellToWorld(pos), hopDuration, hopHeight);

                if (!HarvestOneCell(pos, world, resolver, ref multi, ref comboIndex))
                    continue;

                current = pos;
            }
        }
        finally
        {
            if (flyer != null)
                Destroy(flyer.gameObject);
            IsHarvestBusy = false;
        }
    }

    /// <summary>
    /// Harvests one ready cell: payout, multi growth, pattern enqueue, clear, spawn-tile relics.
    /// </summary>
    bool HarvestOneCell(Vector2Int pos, WorldManager world, CropStateResolver resolver, ref float multi, ref int comboIndex)
    {
        if (harvestVisited.Contains(pos))
            return false;

        if (!world.TryGetCrop(pos, out CropCell cell) || cell == null || !cell.IsReady)
            return false;

        CropPropertiesSO stage = cell.CurrentStage;
        CropGrowthSO crop = cell.crop;
        if (stage == null || crop == null)
            return false;

        GameManager.Main?.AudioService?.PlayHarvest(comboIndex);
        comboIndex++;

        harvestVisited.Add(pos);

        int gold = resolver.GetGold(stage, crop);
        float multiBonus = resolver.GetMulti(stage, crop);

        int payout = Mathf.RoundToInt(gold * multi);
        coinDropService.SpawnDrops(new Vector2(pos.x, pos.y), payout);
        multi += multiBonus;

        EnqueuePattern(stage.harvestPattern, pos, world);
        EnqueueExtraPatternsFromRelics(pos, crop, world);

        world.ClearCrop(pos);
        UnregisterPlanted(pos);
        ApplyHarvestSpawnTile(pos, crop, world);
        SaveGameService.NotifyChanged();
        return true;
    }

    static Vector3 CellToWorld(Vector2Int cell) => new Vector3(cell.x, cell.y, 0f);

    void EnqueuePattern(HarvestPattern pattern, Vector2Int origin, WorldManager world)
    {
        if (pattern == null)
            return;

        patternBuffer.Clear();
        HarvestPatternResolver.Resolve(pattern, origin, world, patternBuffer);
        for (int i = 0; i < patternBuffer.Count; i++)
        {
            Vector2Int target = patternBuffer[i];
            if (!harvestVisited.Contains(target))
                harvestQueue.Enqueue(target);
        }
    }

    /// <summary>
    /// If relic makes certain crops affect more cells, this is where it gets applied
    /// </summary>
    void EnqueueExtraPatternsFromRelics(Vector2Int origin, CropGrowthSO harvestedCrop, WorldManager world)
    {
        var relics = GameManager.Main.Inventory?.ownedRelics;
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
                if (effect == null || effect.type != RelicEffectType.OnHarvestExtraPattern)
                    continue;
                if (effect.onlyCrop != null && effect.onlyCrop != harvestedCrop)
                    continue;
                if (effect.extraPattern == null)
                    continue;

                EnqueuePattern(effect.extraPattern, origin, world);
            }
        }
    }

    void ApplyHarvestSpawnTile(Vector2Int pos, CropGrowthSO harvestedCrop, WorldManager world)
    {
        ApplySpawnTileRelics(pos, harvestedCrop, world, RelicEffectType.OnHarvestSpawnTile);
    }

    void ApplyCropDeathSpawnTile(Vector2Int pos, CropGrowthSO deadCrop, WorldManager world)
    {
        ApplySpawnTileRelics(pos, deadCrop, world, RelicEffectType.OnCropDeathSpawnTile);
    }

    void ApplySpawnTileRelics(Vector2Int pos, CropGrowthSO crop, WorldManager world, RelicEffectType effectType)
    {
        var relics = GameManager.Main.Inventory?.ownedRelics;
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
                if (effect == null || effect.type != effectType)
                    continue;
                if (effect.onlyCrop != null && effect.onlyCrop != crop)
                    continue;
                if (effect.tileToSpawn == null)
                    continue;

                world.ApplyTerrainOverride(pos, effect.tileToSpawn);
            }
        }
    }

    /// <summary>
    /// Rebuilds the growth tracking list from crops currently in WorldManager (after load).
    /// </summary>
    public void RebuildPlantedFromWorld()
    {
        plantedPositions.Clear();
        harvestQueue.Clear();
        harvestVisited.Clear();
        IsHarvestBusy = false;

        var world = GameManager.Main?.WorldManager;
        if (world == null)
            return;

        world.CollectPlantedPositions(plantedPositions);
    }

    void Update()
    {
        if (plantedPositions.Count == 0)
            return;

        var world = GameManager.Main.WorldManager;
        var resolver = GameManager.Main.CropStateResolver;
        if (world == null || resolver == null)
            return;

        float dt = Time.deltaTime;

        for (int i = 0; i < plantedPositions.Count; i++)
        {
            Vector2Int pos = plantedPositions[i];
            if (!world.TryGetCrop(pos, out CropCell cell) || cell == null)
            {
                plantedPositions.RemoveAt(i);
                i--;
                continue;
            }

            if (cell.crop == null)
                continue;

            // Drought tick: ready crops still die if left dry (frozen during harvest chain anim)
            if (!IsHarvestBusy && !cell.isWatered)
            {
                cell.dryElapsed += dt;
                if (cell.dryElapsed >= resolver.GetDryDeathTime(cell.crop))
                {
                    KillAt(pos);
                    i--;
                    continue;
                }
            }

            if (cell.IsReady)
                continue;

            CropPropertiesSO stage = cell.CurrentStage;
            if (stage == null)
                continue;

            cell.stageElapsed += dt;
            float duration = resolver.GetGrowthTime(stage, cell.crop, cell.isWatered);
            if (cell.stageElapsed < duration)
                continue;

            cell.AdvanceStage();
            CropPropertiesSO nextStage = cell.CurrentStage;
            world.SetCropVisualAt(pos, nextStage != null ? nextStage.cropVisual : null);
        }
    }
}
