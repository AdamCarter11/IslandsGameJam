using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns crop growth ticks, planted-cell tracking, drought death, and chain harvest.
/// </summary>
public class CropSystem : MonoBehaviour
{
    static readonly Vector2Int[] AdjacentOffsets =
    {
        new Vector2Int(1, 0),
        new Vector2Int(-1, 0),
        new Vector2Int(0, 1),
        new Vector2Int(0, -1),
    };

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
    readonly List<Vector2Int> fertilizedPos = new List<Vector2Int>();

    [Header("Fertilizer")]
    [SerializeField]
    private int fertilizerCount = 0;
    public int FertilizerCount => fertilizerCount;

    /// <summary>Next harvest starts at this multi when set (persist relics).</summary>
    float? persistedChainMulti;

    /// <summary>Additive bonus applied once at the next harvest start, then cleared.</summary>
    float pendingStartMultiBonus;

    /// <summary>Multiplies death gold; grows on death relics; resets when a harvest chain starts.</summary>
    float deathGoldStreakMulti = 1f;

    /// <summary>Fraction off next relic roll cost (0-1). Consumed by shop service.</summary>
    float relicRollDiscount;

    /// <summary>Toggles each completed harvest when PersistComboMultiEveryOther is owned.</summary>
    bool everyOtherPersistToggle;

    public CropGrowthSO DebugCropToPlant => debugCropToPlant;
    public IReadOnlyList<Vector2Int> PlantedPositions => plantedPositions;
    public bool IsHarvestBusy { get; private set; }

    public float? PersistedChainMulti
    {
        get => persistedChainMulti;
        set => persistedChainMulti = value;
    }

    public float PendingStartMultiBonus
    {
        get => pendingStartMultiBonus;
        set => pendingStartMultiBonus = Mathf.Max(0f, value);
    }

    public float DeathGoldStreakMulti
    {
        get => deathGoldStreakMulti;
        set => deathGoldStreakMulti = Mathf.Max(0f, value);
    }

    public float RelicRollDiscount
    {
        get => relicRollDiscount;
        set => relicRollDiscount = Mathf.Clamp(value, 0f, 0.5f);
    }

    public bool EveryOtherPersistToggle
    {
        get => everyOtherPersistToggle;
        set => everyOtherPersistToggle = value;
    }

    public void AddPendingStartMultiBonus(float amount)
    {
        if (amount == 0f)
            return;
        pendingStartMultiBonus = Mathf.Max(0f, pendingStartMultiBonus + amount);
    }

    public void AddRelicRollDiscount(float fraction)
    {
        if (fraction <= 0f)
            return;
        // Merchant Favor and similar: stack seed-buy discounts up to 50% off.
        relicRollDiscount = Mathf.Clamp(relicRollDiscount + fraction, 0f, 0.5f);
    }

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
    /// Removes a plant from a single cell. Ready crops grant destroy-harvestable relic rewards.
    /// </summary>
    public void DestroyAt(Vector2Int position)
    {
        var world = GameManager.Main.WorldManager;
        if (world == null)
            return;

        if (!world.TryGetCrop(position, out CropCell cell) || cell == null)
            return;

        if (cell.IsReady && cell.crop != null)
            ApplyDestroyHarvestableRelics(position, cell, world);

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
        RelicEffectContext context = BuildCellContext(position, cell, world);

        int deathGold = resolver.GetDeathGold(crop, context);
        deathGold = Mathf.Max(0, Mathf.RoundToInt(deathGold * deathGoldStreakMulti));
        deathGold += CountAdjacentUnwateredGold(position, world);

        if (coinDropService != null && deathGold > 0)
            coinDropService.SpawnDrops(new Vector2(position.x, position.y), deathGold);

        ApplyDeathStreakMultiRelics(crop, context);

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

        int comboIndex = 0;
        float multi = 1f;
        Vector2Int endPos = origin;

        try
        {
            multi = ResolveHarvestStartMulti(resolver);
            deathGoldStreakMulti = 1f;

            harvestQueue.Clear();
            harvestVisited.Clear();

            Sprite originSprite = null;
            if (world.TryGetCrop(origin, out CropCell originCell) && originCell?.crop != null)
                originSprite = originCell.CurrentStage?.cropVisual;

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
            endPos = origin;

            while (harvestQueue.Count > 0)
            {
                Vector2Int pos = harvestQueue.Dequeue();
                if (harvestVisited.Contains(pos))
                    continue;

                if (!world.TryGetCrop(pos, out CropCell cell) || cell == null || !cell.IsReady)
                    continue;

                if (cell.crop == null || cell.CurrentStage == null)
                    continue;

                flyer.SetSprite(cell.CurrentStage.cropVisual);
                yield return flyer.Hop(CellToWorld(current), CellToWorld(pos), hopDuration, hopHeight);

                if (!HarvestOneCell(pos, world, resolver, ref multi, ref comboIndex))
                    continue;

                current = pos;
                endPos = pos;
            }
        }
        finally
        {
            if (comboIndex > 0)
                ApplyHarvestEndRelics(comboIndex, multi, endPos);

            if (flyer != null)
                Destroy(flyer.gameObject);
            IsHarvestBusy = false;
        }
    }

    float ResolveHarvestStartMulti(CropStateResolver resolver)
    {
        float multi = persistedChainMulti ?? 1f;
        multi = resolver.GetBaseComboMulti(multi);
        multi += pendingStartMultiBonus;
        pendingStartMultiBonus = 0f;
        return Mathf.Max(0f, multi);
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

        RelicEffectContext context = BuildCellContext(pos, cell, world);

        GameManager.Main?.AudioService?.PlayHarvest(comboIndex);
        comboIndex++;

        if (comboIndex > 0 && comboIndex % 5 == 0)
            JuiceToast.Spawn(transform, CellToWorld(pos), $"x{comboIndex}!", JuiceToast.ComboSortingOrder, flyerSortingLayer);

        harvestVisited.Add(pos);

        int gold = resolver.GetGold(stage, crop, context);
        float multiBonus = resolver.GetMulti(stage, crop, context);

        // Fertilize apply bonus before payout
        if (fertilizedPos.Contains(pos))
        {
            multi += 1f;
            fertilizedPos.Remove(pos);
        }

        int payout = Mathf.RoundToInt(gold * multi);
        coinDropService.SpawnDrops(new Vector2(pos.x, pos.y), payout);
        multi += multiBonus;

        ApplyComboMilestoneRelics(pos, comboIndex, ref multi);

        EnqueuePattern(stage.harvestPattern, pos, world);
        EnqueueExtraPatternsFromRelics(pos, crop, world);
        EnqueueMirrorOppositeFromRelics(pos, stage.harvestPattern, crop, context);

        world.ClearCrop(pos);
        UnregisterPlanted(pos);
        ApplyHarvestSpawnTile(pos, crop, world);
        SaveGameService.NotifyChanged();
        return true;
    }

    void ApplyComboMilestoneRelics(Vector2Int pos, int comboIndex, ref float multi)
    {
        RelicEffectUtility.ForEachEffect(RelicEffectType.OnComboEveryNGold, effect =>
        {
            if (effect.threshold <= 0)
                return;
            if (comboIndex % effect.threshold != 0)
                return;

            int bonus = Mathf.RoundToInt(effect.amount);
            if (bonus > 0 && coinDropService != null)
                coinDropService.SpawnDrops(new Vector2(pos.x, pos.y), bonus);
        });

        float multiLocal = multi;
        RelicEffectUtility.ForEachEffect(RelicEffectType.OnComboAtNAddMulti, effect =>
        {
            if (comboIndex != effect.threshold)
                return;
            multiLocal += effect.amount;
        });
        multi = multiLocal;
    }

    void ApplyHarvestEndRelics(int comboIndex, float endingMulti, Vector2Int endPos)
    {
        RelicEffectUtility.ForEachEffect(RelicEffectType.OnHarvestEndComboEqualsGold, effect =>
        {
            if (comboIndex != effect.threshold)
                return;

            int bonus = Mathf.RoundToInt(effect.amount);
            if (bonus > 0 && coinDropService != null)
                coinDropService.SpawnDrops(new Vector2(endPos.x, endPos.y), bonus);
        });

        RelicEffectUtility.ForEachEffect(RelicEffectType.OnHarvestEndComboLessThanRandomSeed, effect =>
        {
            if (comboIndex >= effect.threshold)
                return;
            if (!RelicEffectUtility.RollChance(effect))
                return;
            TryGrantRandomUnlockedSeed();
        });

        ResolvePersistComboMulti(comboIndex, endingMulti);
        SaveGameService.NotifyChanged();
    }

    void ResolvePersistComboMulti(int comboIndex, float endingMulti)
    {
        bool everyOtherPersist = false;
        if (RelicEffectUtility.HasEffect(RelicEffectType.PersistComboMultiEveryOther))
        {
            everyOtherPersistToggle = !everyOtherPersistToggle;
            everyOtherPersist = everyOtherPersistToggle;
        }

        bool shouldPersist = RelicEffectUtility.HasEffect(RelicEffectType.PersistComboMulti) || everyOtherPersist;

        if (!shouldPersist)
        {
            RelicEffectUtility.ForEachEffect(RelicEffectType.PersistComboMultiOnEndCombo, effect =>
            {
                if (comboIndex == effect.threshold)
                    shouldPersist = true;
            });
        }

        persistedChainMulti = shouldPersist ? endingMulti : (float?)null;
    }

    void TryGrantRandomUnlockedSeed()
    {
        var inventory = GameManager.Main?.Inventory;
        if (inventory == null)
            return;

        IReadOnlyCollection<CropGrowthSO> unlocked = inventory.UnlockedSeeds;
        if (unlocked == null || unlocked.Count == 0)
            return;

        var candidates = new List<CropGrowthSO>(unlocked.Count);
        foreach (CropGrowthSO crop in unlocked)
        {
            if (crop != null && inventory.CanFitSeed(crop, 1))
                candidates.Add(crop);
        }

        if (candidates.Count == 0)
            return;

        CropGrowthSO pick = candidates[Random.Range(0, candidates.Count)];
        inventory.TryAddSeeds(pick, 1);
    }

    void ApplyDestroyHarvestableRelics(Vector2Int position, CropCell cell, WorldManager world)
    {
        CropGrowthSO crop = cell.crop;
        CropPropertiesSO stage = cell.CurrentStage;
        if (crop == null || stage == null)
            return;

        RelicEffectContext context = BuildCellContext(position, cell, world);
        float goldTotal = 0f;

        RelicEffectUtility.ForEachEffect(
            RelicEffectType.OnDestroyHarvestableGold,
            crop,
            context,
            effect =>
            {
                if (effect.multiplicative)
                    goldTotal += stage.goldGain * effect.amount;
                else
                    goldTotal += effect.amount;
            });

        int gold = Mathf.Max(0, Mathf.RoundToInt(goldTotal));
        if (gold > 0 && coinDropService != null)
            coinDropService.SpawnDrops(new Vector2(position.x, position.y), gold);

        RelicEffectUtility.ForEachEffect(
            RelicEffectType.OnDestroyHarvestableReturnSeeds,
            crop,
            context,
            effect =>
            {
                if (!RelicEffectUtility.RollChance(effect))
                    return;

                int seeds = Mathf.Max(0, Mathf.RoundToInt(effect.amount));
                if (seeds > 0)
                    GameManager.Main?.Inventory?.TryAddSeeds(crop, seeds);
            });

        RelicEffectUtility.ForEachEffect(
            RelicEffectType.OnDestroyHarvestableNextStartMulti,
            crop,
            context,
            effect => AddPendingStartMultiBonus(effect.amount));
    }

    int CountAdjacentUnwateredGold(Vector2Int position, WorldManager world)
    {
        int total = 0;
        RelicEffectUtility.ForEachEffect(RelicEffectType.OnCropDeathAdjacentUnwateredGold, effect =>
        {
            int perCrop = Mathf.RoundToInt(effect.amount);
            if (perCrop == 0)
                return;

            int count = 0;
            for (int i = 0; i < AdjacentOffsets.Length; i++)
            {
                Vector2Int neighbor = position + AdjacentOffsets[i];
                if (!world.TryGetCrop(neighbor, out CropCell adj) || adj == null || adj.crop == null)
                    continue;
                if (adj.isWatered)
                    continue;
                count++;
            }

            total += count * perCrop;
        });
        return total;
    }

    void ApplyDeathStreakMultiRelics(CropGrowthSO crop, RelicEffectContext context)
    {
        RelicEffectUtility.ForEachEffect(
            RelicEffectType.OnCropDeathStreakMulti,
            crop,
            context,
            effect =>
            {
                if (effect.multiplicative)
                    deathGoldStreakMulti *= effect.amount;
                else
                    deathGoldStreakMulti += effect.amount;
            });

        deathGoldStreakMulti = Mathf.Max(0f, deathGoldStreakMulti);
    }

    static RelicEffectContext BuildCellContext(Vector2Int pos, CropCell cell, WorldManager world)
    {
        TerrainType terrain = default;
        if (world.TryGetTerrainUnit(pos, out TerrainUnit unit) && unit != null)
            terrain = unit.Type;

        HarvestPattern pattern = cell.CurrentStage != null ? cell.CurrentStage.harvestPattern : null;
        return RelicEffectContext.ForHarvest(cell.isWatered, terrain, pattern);
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

    void EnqueueMirrorOppositeFromRelics(
        Vector2Int origin,
        HarvestPattern pattern,
        CropGrowthSO harvestedCrop,
        RelicEffectContext context)
    {
        if (!RelicEffectUtility.IsSingleOffsetPattern(pattern))
            return;
        if (!RelicEffectUtility.HasEffect(RelicEffectType.OnHarvestMirrorOpposite, harvestedCrop, context))
            return;

        cellOffset offset = pattern.offsets[0];
        Vector2Int opposite = new Vector2Int(origin.x - offset.x, origin.y - offset.y);
        if (!harvestVisited.Contains(opposite))
            harvestQueue.Enqueue(opposite);
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

    /// <summary>
    /// Writes relic runtime stacks into <paramref name="data"/>.
    /// </summary>
    public void CaptureTo(GameSaveData data)
    {
        if (data == null)
            return;

        data.hasPersistedChainMulti = persistedChainMulti.HasValue;
        data.persistedChainMulti = persistedChainMulti ?? 0f;
        data.pendingStartMultiBonus = pendingStartMultiBonus;
        data.deathGoldStreakMulti = deathGoldStreakMulti;
        data.relicRollDiscount = relicRollDiscount;
        data.everyOtherPersistToggle = everyOtherPersistToggle;
        ObstacleClearTracker.CaptureTo(data);
    }

    /// <summary>
    /// Restores relic runtime stacks from <paramref name="data"/>.
    /// </summary>
    public void ApplyFrom(GameSaveData data)
    {
        if (data == null)
        {
            ResetRelicRuntimeState();
            return;
        }

        persistedChainMulti = data.hasPersistedChainMulti ? data.persistedChainMulti : (float?)null;
        pendingStartMultiBonus = Mathf.Max(0f, data.pendingStartMultiBonus);
        // Old saves omit this field (0); treat as the default streak of 1.
        deathGoldStreakMulti = data.deathGoldStreakMulti > 0f ? data.deathGoldStreakMulti : 1f;
        relicRollDiscount = Mathf.Clamp(data.relicRollDiscount, 0f, 0.5f);
        everyOtherPersistToggle = data.everyOtherPersistToggle;
        ObstacleClearTracker.ApplyFrom(data);
    }

    /// <summary>
    /// Resets session relic stacks (used when starting a fresh run).
    /// </summary>
    public void ResetRelicRuntimeState()
    {
        persistedChainMulti = null;
        pendingStartMultiBonus = 0f;
        deathGoldStreakMulti = 1f;
        relicRollDiscount = 0f;
        everyOtherPersistToggle = false;
        ObstacleClearTracker.Reset();
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

    public void FertilizeAt(Vector2Int cell)
    {
        if (fertilizedPos.Contains(cell))
            return;

        fertilizedPos.Add(cell);
        fertilizerCount--;
        ToolModeController.Main.HandleFertilizerCount(fertilizerCount);
    }

    public void AddFertilizer()
    {
        fertilizerCount++;
        ToolModeController.Main.HandleFertilizerCount(fertilizerCount);
    }
}
