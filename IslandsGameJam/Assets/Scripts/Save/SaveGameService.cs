using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BootMode
{
    None,
    New,
    Load
}

/// <summary>
/// Single-slot Easy Save 3 API: file ops, boot handoff, debounced autosave, quit flush,
/// and capture/apply of live game state.
/// Attach to MainGame (or GameManager) so debounce and quit/pause flush run.
/// </summary>
public class SaveGameService : MonoBehaviour
{
    public const string FileName = "SaveFile.es3";
    public const string SaveKey = "game";
    public const int CurrentVersion = 1;
    const float DebounceSeconds = 0.5f;

    static global::BootMode bootMode = global::BootMode.None;

    /// <summary>Scene handoff from MainMenu: New, Load, or None after GameManager boots.</summary>
    public static global::BootMode BootMode
    {
        get => bootMode;
        set => bootMode = value;
    }

    static SaveGameService instance;
    static GameManager pendingGame;
    static GameManager boundGame;
    static bool dirty;
    static bool autosaveEnabled;

    Coroutine debounceRoutine;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            UnbindAutosave();
            instance = null;
        }
    }

    void OnApplicationQuit() => Flush();

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
            Flush();
    }

    public static bool HasSave => ES3.FileExists(FileName);

    public static void DeleteSave()
    {
        if (HasSave)
            ES3.DeleteFile(FileName);

        dirty = false;
        pendingGame = null;
        CancelDebounce();
    }

    /// <summary>
    /// Subscribes to inventory change events and enables autosave after boot.
    /// Call once GameManager has finished New Game init or Load.
    /// Ensures a <see cref="SaveGameService"/> host exists for debounce and quit flush.
    /// </summary>
    public static void BindAutosave(GameManager game)
    {
        if (game == null)
            return;

        EnsureHost(game);
        UnbindAutosave();
        boundGame = game;

        Inventory inv = game.Inventory;
        if (inv != null)
        {
            inv.OnGoldChanged += OnInventoryChanged;
            inv.OnHotbarChanged += OnInventoryChangedNoArg;
            inv.OnShopUnlocksChanged += OnInventoryChangedNoArg;
            inv.OnRelicsChanged += OnInventoryChangedNoArg;
        }

        autosaveEnabled = true;
    }

    static void EnsureHost(GameManager game)
    {
        if (instance != null)
            return;

        SaveGameService host = game.GetComponent<SaveGameService>();
        if (host == null)
            host = game.gameObject.AddComponent<SaveGameService>();
        instance = host;
    }

    /// <summary>
    /// Unsubscribes inventory listeners and disables event-driven autosave.
    /// </summary>
    public static void UnbindAutosave()
    {
        autosaveEnabled = false;

        if (boundGame?.Inventory != null)
        {
            Inventory inv = boundGame.Inventory;
            inv.OnGoldChanged -= OnInventoryChanged;
            inv.OnHotbarChanged -= OnInventoryChangedNoArg;
            inv.OnShopUnlocksChanged -= OnInventoryChangedNoArg;
            inv.OnRelicsChanged -= OnInventoryChangedNoArg;
        }

        boundGame = null;
    }

    /// <summary>
    /// Marks dirty from a gameplay mutation when autosave is enabled.
    /// Uses the bound game, or <see cref="GameManager.Main"/> as fallback.
    /// </summary>
    public static void NotifyChanged()
    {
        if (!autosaveEnabled)
            return;

        RequestSave(boundGame != null ? boundGame : GameManager.Main);
    }

    /// <summary>
    /// Marks the game dirty and schedules a coalesced disk write (~0.5s).
    /// </summary>
    public static void RequestSave(GameManager game)
    {
        if (game == null)
            return;

        pendingGame = game;
        dirty = true;

        if (instance != null)
            instance.ScheduleDebouncedSave();
    }

    static void OnInventoryChanged(int _) => NotifyChanged();

    static void OnInventoryChangedNoArg() => NotifyChanged();

    /// <summary>
    /// Immediately captures and writes the save file.
    /// </summary>
    public static void Save(GameManager game)
    {
        if (game == null)
            return;

        CancelDebounce();
        GameSaveData data = Capture(game);
        ES3.Save(SaveKey, data, FileName);
        dirty = false;
        pendingGame = null;
    }

    /// <summary>
    /// Loads the save file and applies it to <paramref name="game"/>.
    /// </summary>
    public static void Load(GameManager game)
    {
        if (game == null || !HasSave)
            return;

        GameSaveData data = ES3.Load<GameSaveData>(SaveKey, FileName);
        Apply(game, data);
        dirty = false;
    }

    /// <summary>
    /// Writes immediately if a debounced save is pending (quit / pause).
    /// </summary>
    public static void Flush()
    {
        if (!dirty)
            return;

        GameManager game = pendingGame != null ? pendingGame : GameManager.Main;
        if (game == null)
            return;

        Save(game);
    }

    static void CancelDebounce()
    {
        if (instance == null || instance.debounceRoutine == null)
            return;

        instance.StopCoroutine(instance.debounceRoutine);
        instance.debounceRoutine = null;
    }

    void ScheduleDebouncedSave()
    {
        if (debounceRoutine != null)
            StopCoroutine(debounceRoutine);
        debounceRoutine = StartCoroutine(DebouncedSaveCoroutine());
    }

    IEnumerator DebouncedSaveCoroutine()
    {
        yield return new WaitForSecondsRealtime(DebounceSeconds);
        debounceRoutine = null;

        if (dirty && pendingGame != null)
            Save(pendingGame);
    }

    /// <summary>
    /// Builds a <see cref="GameSaveData"/> from live inventory, world, crops, and relic shop.
    /// </summary>
    public static GameSaveData Capture(GameManager game)
    {
        GameSaveData data = CreateEmptySaveData();
        if (game == null)
            return data;

        game.Inventory?.CaptureTo(data);
        game.WorldManager?.CaptureTo(data);
        game.RelicShopService?.CaptureTo(data);
        return data;
    }

    /// <summary>
    /// Applies a <see cref="GameSaveData"/> to live inventory, world, crops, and relic shop.
    /// </summary>
    public static void Apply(GameManager game, GameSaveData data)
    {
        if (game == null || data == null)
            return;

        SaveIdLookup lookup = CreateLookup(game);

        game.Inventory?.ApplyFrom(data, lookup);
        game.WorldManager?.LoadFromSave(data, lookup);
        game.CropSystem?.RebuildPlantedFromWorld();
        game.RelicShopService?.ApplySaveState(data.relicShopPurchaseCount, data.relicSkipCounts, lookup);
    }

    static SaveIdLookup CreateLookup(GameManager game)
    {
        var terrains = new List<TerrainData>();
        game.WorldManager?.CollectTerrains(terrains);

        RelicShopCatalog relicCatalog = game.RelicShopCatalog;
        if (relicCatalog?.allRelics != null)
        {
            for (int i = 0; i < relicCatalog.allRelics.Count; i++)
            {
                RelicSO relic = relicCatalog.allRelics[i];
                if (relic?.effects == null)
                    continue;

                for (int e = 0; e < relic.effects.Length; e++)
                {
                    TerrainData tile = relic.effects[e]?.tileToSpawn;
                    if (tile != null)
                        terrains.Add(tile);
                }
            }
        }

        return new SaveIdLookup(game.SeedShopCatalog, relicCatalog, terrains);
    }

    static GameSaveData CreateEmptySaveData()
    {
        return new GameSaveData
        {
            version = CurrentVersion,
            hotbar = new HotbarSlotSaveData[Inventory.HotbarSlotCount],
            unlockedCropIds = System.Array.Empty<string>(),
            ownedRelicIds = System.Array.Empty<string>(),
            unlockedChunks = System.Array.Empty<Vector2Int>(),
            terrainOverrides = System.Array.Empty<TerrainOverrideSaveData>(),
            obstacleCells = System.Array.Empty<Vector2Int>(),
            crops = System.Array.Empty<CropSaveData>(),
            relicSkipCounts = System.Array.Empty<RelicSkipCountSaveData>(),
        };
    }
}
