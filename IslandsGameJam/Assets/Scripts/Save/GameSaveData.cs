using System;
using UnityEngine;

/// <summary>
/// Root Easy Save 3 payload stored under the "game" key.
/// </summary>
[Serializable]
public class GameSaveData
{
    public int version;

    // --- Inventory ---
    public int gold;
    public int highestGoldEver;
    public HotbarSlotSaveData[] hotbar;
    public string[] unlockedCropIds;
    public string[] ownedRelicIds;

    // --- Runtime ---
    public bool timerStarted;
    public float playTime;

    // --- World ---
    public Vector2 origin;
    public Vector2Int[] unlockedChunks;
    public TerrainOverrideSaveData[] terrainOverrides;
    public Vector2Int[] obstacleCells;

    // --- Crops ---
    public CropSaveData[] crops;

    // --- Relic runtime stacks (CropSystem session state) ---
    public bool hasPersistedChainMulti;
    public float persistedChainMulti;
    public float pendingStartMultiBonus;
    public float deathGoldStreakMulti;
    public float relicRollDiscount;
    public bool everyOtherPersistToggle;

    // --- Relic shop ---
    public int relicShopPurchaseCount;
    public RelicSkipCountSaveData[] relicSkipCounts;
}

[Serializable]
public struct HotbarSlotSaveData
{
    public string cropId;
    public int count;
}

[Serializable]
public struct TerrainOverrideSaveData
{
    public Vector2Int pos;
    public string terrainId;
}

[Serializable]
public struct CropSaveData
{
    public Vector2Int pos;
    public string cropId;
    public int stageIndex;
    public float stageElapsed;
    public bool isWatered;
    public float dryElapsed;
}

[Serializable]
public struct RelicSkipCountSaveData
{
    public string relicId;
    public int count;
}
