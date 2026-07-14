using ColorMak3r.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.UI.Image;

public class WorldManager : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField]
    private int worldWidth = 500;
    [SerializeField]
    private int worldHeight = 500;
    [SerializeField]
    private int chunkSize = 3;
    [SerializeField]
    private Vector2Int noObstacleChunkZone = new Vector2Int(5, 5);

    [Header("Generation Settings")]
    [SerializeField]
    private bool randomizeOrigin = false;
    [HideIf("randomizeOrigin"), SerializeField]
    private Vector2 origin = Vector2.zero;
    [SerializeField]
    private Vector2Int dimension = new Vector2Int(50, 50);
    [SerializeField]
    private float scale = 1.0f;
    [SerializeField]
    private int octaves = 3;
    [SerializeField]
    private float persistence = 0.5f;
    [SerializeField]
    private float frequencyBase = 2f;
    [SerializeField]
    private float exponent = 1f;
    [SerializeField]
    private TerrainData voidTerrainData;
    [SerializeField]
    private WorldGenDataSet worldGenDataSet;

    [Header("FailSafe")]
    [SerializeField]
    private int maxFailSafeIterations = 1000;
    [SerializeField]
    private Vector2Int failSafeZone;
    [SerializeField]
    private float maxWaterAllowance = 0.1f;
    [SerializeField]
    private float waterThreshold = 0.3f;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject terrainUnitPrefab;
    [SerializeField]
    private GameObject cropPrefab;

    private OffsetArray2D<TerrainUnit> terrainUnits;
    private OffsetArray2D<CropCell> crops;
    private Dictionary<Vector2Int, GameObject> obstacles = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, TerrainChunk> currentChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Dictionary<Vector2Int, TerrainChunk> availableChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private readonly Dictionary<Vector2Int, TerrainData> terrainOverrides = new Dictionary<Vector2Int, TerrainData>();

    public void Initialize()
    {
        int minX = -worldWidth / 2;
        int maxX = worldWidth / 2;
        int minY = -worldHeight / 2;
        int maxY = worldHeight / 2;
        terrainUnits = new OffsetArray2D<TerrainUnit>(minX, maxX, minY, maxY);
        crops = new OffsetArray2D<CropCell>(minX, maxX, minY, maxY);

        obstacles.Clear();
        currentChunks.Clear();
        availableChunks.Clear();
        terrainOverrides.Clear();

        if (randomizeOrigin) FailSafeOrigin();

        var firstChunk = new TerrainChunk(Vector2Int.zero);
        currentChunks.Add(firstChunk.Position, firstChunk);
        GenerateAvailableChunksFromPosition(firstChunk.Position);
        BuildChunk(firstChunk.Position);
    }

    int failSafeRetryCount = 0;
    private void FailSafeOrigin()
    {
        while (failSafeRetryCount < maxFailSafeIterations)
        {
            Vector2Int newOrigin = (UnityEngine.Random.insideUnitCircle * (worldHeight + worldWidth) / 2).SnapToGrid().ToInt();

            float total = 0;
            float water = 0;
            for (int x = -failSafeZone.x; x <= failSafeZone.x; x++)
            {
                for (int y = -failSafeZone.y; y <= failSafeZone.y; y++)
                {
                    Vector2Int pos = new Vector2Int(newOrigin.x + x, newOrigin.y + y);
                    float elevation = GetNoise(pos.x + worldWidth / 2, pos.y + worldHeight / 2, newOrigin, dimension, scale, octaves, persistence, frequencyBase, exponent);
                    if (elevation < waterThreshold)
                        water++;
                    total++;
                }
            }

            var ratio = water / total;
            if (ratio <= maxWaterAllowance)
            {
                Debug.Log($"Failsafe iteration {failSafeRetryCount}, water({water})/land ratio = {ratio}, total = {total}");
                origin = newOrigin;
                return;
            }

            failSafeRetryCount++;
        }
    }

    /// <summary>
    /// Restores world arrays, unlocked chunks (no random obstacles), saved obstacles,
    /// terrain overrides, and crop cells from save data.
    /// </summary>
    public void LoadFromSave(GameSaveData data, SaveIdLookup lookup)
    {
        if (data == null)
            return;

        int minX = -worldWidth / 2;
        int maxX = worldWidth / 2;
        int minY = -worldHeight / 2;
        int maxY = worldHeight / 2;
        terrainUnits = new OffsetArray2D<TerrainUnit>(minX, maxX, minY, maxY);
        crops = new OffsetArray2D<CropCell>(minX, maxX, minY, maxY);

        obstacles.Clear();
        currentChunks.Clear();
        availableChunks.Clear();
        terrainOverrides.Clear();

        origin = data.origin;

        Vector2Int[] unlocked = data.unlockedChunks;
        if (unlocked == null || unlocked.Length == 0)
            unlocked = new[] { Vector2Int.zero };

        for (int i = 0; i < unlocked.Length; i++)
        {
            Vector2Int chunkPos = unlocked[i];
            if (currentChunks.ContainsKey(chunkPos))
                continue;

            BuildChunk(chunkPos, placeRandomObstacles: false);
            currentChunks.Add(chunkPos, new TerrainChunk(chunkPos));
        }

        foreach (Vector2Int chunkPos in currentChunks.Keys)
            GenerateAvailableChunksFromPosition(chunkPos);

        if (data.obstacleCells != null)
        {
            for (int i = 0; i < data.obstacleCells.Length; i++)
                PlaceObstacle(data.obstacleCells[i]);
        }

        if (data.terrainOverrides != null && lookup != null)
        {
            for (int i = 0; i < data.terrainOverrides.Length; i++)
            {
                TerrainOverrideSaveData ov = data.terrainOverrides[i];
                if (!lookup.TryGetTerrain(ov.terrainId, out TerrainData terrain))
                    continue;
                ApplyTerrainOverride(ov.pos, terrain);
            }
        }

        if (data.crops != null && lookup != null)
        {
            for (int i = 0; i < data.crops.Length; i++)
                RestoreCrop(data.crops[i], lookup);
        }
    }

    /// <summary>
    /// Writes origin, unlocked chunks, obstacles, terrain overrides, and crops into <paramref name="data"/>.
    /// </summary>
    public void CaptureTo(GameSaveData data)
    {
        if (data == null)
            return;

        data.origin = origin;

        if (currentChunks.Count == 0)
        {
            data.unlockedChunks = Array.Empty<Vector2Int>();
        }
        else
        {
            var chunks = new Vector2Int[currentChunks.Count];
            int i = 0;
            foreach (Vector2Int pos in currentChunks.Keys)
                chunks[i++] = pos;
            data.unlockedChunks = chunks;
        }

        if (obstacles.Count == 0)
        {
            data.obstacleCells = Array.Empty<Vector2Int>();
        }
        else
        {
            var cells = new Vector2Int[obstacles.Count];
            int i = 0;
            foreach (Vector2Int pos in obstacles.Keys)
                cells[i++] = pos;
            data.obstacleCells = cells;
        }

        if (terrainOverrides.Count == 0)
        {
            data.terrainOverrides = Array.Empty<TerrainOverrideSaveData>();
        }
        else
        {
            var overrides = new TerrainOverrideSaveData[terrainOverrides.Count];
            int i = 0;
            foreach (KeyValuePair<Vector2Int, TerrainData> pair in terrainOverrides)
            {
                overrides[i++] = new TerrainOverrideSaveData
                {
                    pos = pair.Key,
                    terrainId = SaveIdLookup.GetId(pair.Value)
                };
            }
            data.terrainOverrides = overrides;
        }

        var cropList = new List<CropSaveData>();
        foreach (Vector2Int chunkPos in currentChunks.Keys)
        {
            ForEachCellInChunk(chunkPos, (x, y) =>
            {
                if (crops == null || !crops.Contains(x, y))
                    return;
                CropCell cell = crops[x, y];
                if (cell == null || cell.crop == null)
                    return;

                cropList.Add(new CropSaveData
                {
                    pos = new Vector2Int(x, y),
                    cropId = SaveIdLookup.GetId(cell.crop),
                    stageIndex = cell.stageIndex,
                    stageElapsed = cell.stageElapsed,
                    isWatered = cell.isWatered,
                    dryElapsed = cell.dryElapsed
                });
            });
        }
        data.crops = cropList.Count == 0 ? Array.Empty<CropSaveData>() : cropList.ToArray();
    }

    public void CollectTerrains(List<TerrainData> into)
    {
        if (into == null)
            return;

        if (voidTerrainData != null)
            into.Add(voidTerrainData);

        worldGenDataSet?.CollectTerrains(into);

        foreach (TerrainData terrain in terrainOverrides.Values)
        {
            if (terrain != null)
                into.Add(terrain);
        }
    }

    /// <summary>
    /// Fills <paramref name="into"/> with positions that currently have a planted crop.
    /// </summary>
    public void CollectPlantedPositions(List<Vector2Int> into)
    {
        if (into == null || crops == null)
            return;

        foreach (Vector2Int chunkPos in currentChunks.Keys)
        {
            ForEachCellInChunk(chunkPos, (x, y) =>
            {
                if (!crops.Contains(x, y))
                    return;
                if (crops[x, y] != null)
                    into.Add(new Vector2Int(x, y));
            });
        }
    }

    /// <summary>
    /// Re-initializes terrain at a cell and records an override when it differs from noise.
    /// </summary>
    public void ApplyTerrainOverride(Vector2Int position, TerrainData terrain)
    {
        if (terrain == null || !TryGetTerrainUnit(position, out TerrainUnit unit))
            return;

        unit.Initialize(terrain);

        TerrainData raw = GeRawTerrainData(position);
        if (terrain != raw)
            terrainOverrides[position] = terrain;
        else
            terrainOverrides.Remove(position);

        SaveGameService.NotifyChanged();
    }

    #region Crop Management

    public bool IsInWorldBounds(Vector2Int position)
    {
        return crops != null && crops.Contains(position.x, position.y);
    }

    public bool TryGetCrop(Vector2Int position, out CropCell cell)
    {
        if (crops == null || !crops.Contains(position.x, position.y))
        {
            cell = null;
            return false;
        }

        cell = crops[position.x, position.y];
        return cell != null;
    }

    public void SetCrop(Vector2Int position, CropCell cell)
    {
        if (crops == null || !crops.Contains(position.x, position.y))
            return;

        crops[position.x, position.y] = cell;
    }

    public void ClearCrop(Vector2Int position)
    {
        if (crops == null || !crops.Contains(position.x, position.y))
            return;

        var cell = crops[position.x, position.y];
        if (cell != null && cell.view != null)
        {
            Destroy(cell.view.gameObject);
            cell.view = null;
        }

        crops[position.x, position.y] = null;
    }

    public void SetCropVisualAt(Vector2Int position, Sprite sprite)
    {
        if (!TryGetCrop(position, out var cell) || cell.view == null)
            return;

        cell.view.SetVisual(sprite);
    }

    /// <summary>
    /// Plants a crop at the given cell. Fails if out of bounds, no terrain, or cell occupied.
    /// </summary>
    public bool PlantCrop(Vector2Int position, CropGrowthSO crop)
    {
        if (crop == null || cropPrefab == null || crops == null || terrainUnits == null)
            return false;
        if (!crops.Contains(position.x, position.y))
            return false;
        if (!terrainUnits.Contains(position.x, position.y) || terrainUnits[position.x, position.y] == null)
            return false;
        if (obstacles.ContainsKey(position))
            return false;
        if (crops[position.x, position.y] != null)
            return false;

        var cell = new CropCell
        {
            crop = crop,
            stageIndex = 0,
            stageElapsed = 0f,
            isWatered = false,
            dryElapsed = 0f
        };
        crops[position.x, position.y] = cell;

        var spawnPosition = new Vector2(position.x, position.y);
        var cropObject = Instantiate(cropPrefab, spawnPosition, Quaternion.identity, transform);
        var view = cropObject.GetComponent<CropView>();
        var stage = cell.CurrentStage;
        if (view != null)
        {
            view.SetVisual(stage != null ? stage.cropVisual : null);
            view.SetWatered(false);
            view.SetHarvestReady(cell.IsReady);
        }
        cell.view = view;
        return true;
    }

    /// <summary>
    /// Restores a planted crop with full saved growth/water state.
    /// </summary>
    public bool RestoreCrop(CropSaveData save, SaveIdLookup lookup)
    {
        if (lookup == null || cropPrefab == null || crops == null || terrainUnits == null)
            return false;
        if (!lookup.TryGetCrop(save.cropId, out CropGrowthSO crop) || crop == null)
            return false;
        if (!crops.Contains(save.pos.x, save.pos.y))
            return false;
        if (!terrainUnits.Contains(save.pos.x, save.pos.y) || terrainUnits[save.pos.x, save.pos.y] == null)
            return false;
        if (obstacles.ContainsKey(save.pos))
            return false;
        if (crops[save.pos.x, save.pos.y] != null)
            return false;

        int stageIndex = save.stageIndex;
        if (crop.stages != null && crop.stages.Length > 0)
            stageIndex = Mathf.Clamp(stageIndex, 0, crop.stages.Length - 1);
        else
            stageIndex = 0;

        var cell = new CropCell
        {
            crop = crop,
            stageIndex = stageIndex,
            stageElapsed = Mathf.Max(0f, save.stageElapsed),
            isWatered = save.isWatered,
            dryElapsed = Mathf.Max(0f, save.dryElapsed)
        };
        crops[save.pos.x, save.pos.y] = cell;

        var spawnPosition = new Vector2(save.pos.x, save.pos.y);
        var cropObject = Instantiate(cropPrefab, spawnPosition, Quaternion.identity, transform);
        var view = cropObject.GetComponent<CropView>();
        var stage = cell.CurrentStage;
        if (view != null)
        {
            view.SetVisual(stage != null ? stage.cropVisual : null);
            view.SetWatered(cell.isWatered);
            view.SetHarvestReady(cell.IsReady);
        }
        cell.view = view;
        return true;
    }

    #endregion

    public bool TryActivateObstacle(Vector2Int position)
    {
        if (!obstacles.ContainsKey(position))
            return false;
        var obstacle = obstacles[position];
        if (obstacle == null)
            return false;
        var activatable = obstacle.GetComponent<IActivatable>();
        if (activatable == null)
            return false;
        activatable.Activate();
        return true;
    }

    #region Terrain Management

    private float GetNoise(float x, float y, Vector2 origin, Vector2 dimension,
        float scale, int octaves, float persistence, float frequencyBase, float exponent)
    {
        float xCoord = origin.x + x / dimension.x * scale;
        float yCoord = origin.y + y / dimension.y * scale;

        var total = 0f;
        var frequency = 1f;
        var amplitude = 1f;
        var maxValue = 0f;
        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(xCoord * frequency, yCoord * frequency) * amplitude;

            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= frequencyBase;
        }

        return Mathf.Pow(total / maxValue, exponent);
    }


    public void UnlockChunk(Vector2 position)
    {
        var p = position.SnapToGrid(chunkSize, true).ToInt();
        if (!availableChunks.ContainsKey(p))
        {
            Debug.LogWarning($"Attempted to unlock chunk at {p} but it is not available.");
            return;
        }

        var chunk = availableChunks[p];
        availableChunks.Remove(p);
        BuildChunk(p);
        currentChunks.Add(p, chunk);
        GenerateAvailableChunksFromPosition(p);

        GameManager.Main?.AudioService?.PlayChunkUnlock();

        if (GameManager.Main != null
            && SeedUnlockService.TryUnlockForIsland(GameManager.Main.Inventory, GameManager.Main.SeedShopCatalog, out CropGrowthSO unlocked)
            && unlocked != null)
        {
            int granted = GrantUnlockSeeds(GameManager.Main.Inventory, unlocked);
            GameManager.Main?.AudioService?.PlaySeedUnlock();
            JuiceToast.SpawnScreenCenter(transform, BuildUnlockToastText(unlocked, granted), 10);
        }

        SaveGameService.NotifyChanged();
    }

    /// <summary>Gives the player a random 1–3 of the newly unlocked seed (as many as the hotbar can fit).</summary>
    static int GrantUnlockSeeds(Inventory inventory, CropGrowthSO crop)
    {
        if (inventory == null || crop == null)
            return 0;

        int amount = UnityEngine.Random.Range(1, 4); // 1–3 inclusive
        while (amount > 0 && !inventory.TryAddSeeds(crop, amount))
            amount--;
        return amount;
    }

    static string BuildUnlockToastText(CropGrowthSO crop, int granted)
    {
        string name = !string.IsNullOrWhiteSpace(crop.cropName) ? crop.cropName.Trim() : crop.name;
        if (granted > 0)
            return $"Seed unlocked!\n+{granted} {name}";
        return $"Seed unlocked!\n{name}";
    }

    public bool IsInsideAvailableChunk(Vector2 position)
    {
        var p = position.SnapToGrid(chunkSize, true).ToInt();
        return availableChunks.ContainsKey(p);
    }

    public void GenerateAvailableChunksFromPosition(Vector2Int position)
    {
        position = position.SnapToGrid(chunkSize, true).ToInt();
        var chunks = new List<TerrainChunk>();
        var toCheck = new Vector2Int[] {
            position + Vector2Int.up * chunkSize,
            position + Vector2Int.down * chunkSize,
            position + Vector2Int.left * chunkSize,
            position + Vector2Int.right * chunkSize };

        foreach (var pos in toCheck)
        {
            if (!currentChunks.ContainsKey(pos) && !availableChunks.ContainsKey(pos))
            {
                availableChunks.Add(pos, new TerrainChunk(pos));
            }
        }
    }

    [Button]
    public void BuildChunk(Vector2Int position) => BuildChunk(position, placeRandomObstacles: true);

    public void BuildChunk(Vector2Int position, bool placeRandomObstacles)
    {
        var halfChunk = Mathf.FloorToInt(chunkSize / 2);

        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                int x = position.x + i - halfChunk;
                int y = position.y + j - halfChunk;
                if (!terrainUnits.Contains(x, y))
                    continue;
                if (terrainUnits[x, y] != null)
                    continue;
                var spawnPosition = new Vector2(x, y);
                GameObject terrainUnitObject = Instantiate(terrainUnitPrefab, spawnPosition,
                    Quaternion.identity, transform);
                TerrainUnit terrainUnit = terrainUnitObject.GetComponent<TerrainUnit>();
                var data = GeRawTerrainData(spawnPosition.ToInt());
                terrainUnit.Initialize(data);
                terrainUnits[x, y] = terrainUnit;

                if (!placeRandomObstacles)
                    continue;

                if (x < -noObstacleChunkZone.x || x > noObstacleChunkZone.x ||
                    y < -noObstacleChunkZone.y || y > noObstacleChunkZone.y)
                {
                    if (worldGenDataSet.TryGetObstacle(data, out var prefab))
                    {
                        PlaceObstacleObject(new Vector2Int(x, y), terrainUnit, prefab);
                    }
                }
            }
        }
    }

    void PlaceObstacle(Vector2Int position)
    {
        if (obstacles.ContainsKey(position))
            return;
        if (!TryGetTerrainUnit(position, out TerrainUnit terrainUnit))
            return;

        TerrainData data = terrainUnit.Data ?? GeRawTerrainData(position);
        if (worldGenDataSet.TryGetObstacle(data, out var prefab))
        {
            PlaceObstacleObject(position, terrainUnit, prefab);
        }
        // Todo: save and restore obstacle by enum type
    }

    void PlaceObstacleObject(Vector2Int position, TerrainUnit terrainUnit, GameObject prefab)
    {
        if (prefab == null || terrainUnit == null || obstacles.ContainsKey(position))
            return;

        var spawnPosition = new Vector2(position.x, position.y);
        GameObject obstacleObject = Instantiate(prefab, spawnPosition,
            Quaternion.identity, terrainUnit.transform);
        obstacles.Add(position, obstacleObject);
    }

    void ForEachCellInChunk(Vector2Int chunkPosition, Action<int, int> action)
    {
        if (action == null)
            return;

        int halfChunk = Mathf.FloorToInt(chunkSize / 2);
        for (int i = 0; i < chunkSize; i++)
        {
            for (int j = 0; j < chunkSize; j++)
            {
                int x = chunkPosition.x + i - halfChunk;
                int y = chunkPosition.y + j - halfChunk;
                action(x, y);
            }
        }
    }

    public bool HasObstacle(Vector2Int position) => obstacles.ContainsKey(position);

    public bool TryGetObstacle(Vector2Int position, out GameObject obstacle)
    {
        if (obstacles.TryGetValue(position, out obstacle) && obstacle != null)
            return true;

        obstacle = null;
        return false;
    }

    public void RemoveObstacle(Vector2Int position)
    {
        if (obstacles.TryGetValue(position, out var obstacle))
        {
            Destroy(obstacle);
            obstacles.Remove(position);
        }
    }

    public TerrainData GeRawTerrainData(Vector2Int position)
    {
        var elevation = GetNoise(position.x + worldWidth / 2,
            position.y + worldHeight / 2,
            origin, dimension, scale, octaves, persistence, frequencyBase, exponent);
        var data = worldGenDataSet.Match(elevation);
        if (data == null) data = voidTerrainData;

        return data;
    }

    [Button]
    public TerrainUnit GetTerrainUnit(Vector2Int position)
    {
        if (!TryGetTerrainUnit(position, out TerrainUnit unit))
        {
            Debug.Log($"GetTerrainUnit({position.x}, {position.y}) = null");
            return null;
        }

        Debug.Log($"GetTerrainUnit({position.x}, {position.y}) = {unit}", unit);
        return unit;
    }

    public bool TryGetTerrainUnit(Vector2Int position, out TerrainUnit unit)
    {
        unit = null;
        if (terrainUnits == null || !terrainUnits.Contains(position.x, position.y))
            return false;

        unit = terrainUnits[position.x, position.y];
        return unit != null;
    }

    [Button]
    private void ClearAll()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.Undo.DestroyObjectImmediate(child);
            }
            else
#endif
            {
                Destroy(child);
            }
        }
    }

    #endregion
}

public class TerrainChunk
{
    public Vector2Int Position { get; }

    public TerrainChunk(Vector2Int position)
    {
        Position = position;
    }
}

public sealed class OffsetArray2D<T>
{
    private readonly T[,] _data;

    public int MinX { get; }
    public int MinY { get; }
    public int MaxX => MinX + _data.GetLength(0) - 1;
    public int MaxY => MinY + _data.GetLength(1) - 1;

    public OffsetArray2D(int minX, int maxX, int minY, int maxY)
    {
        if (maxX < minX)
            throw new ArgumentOutOfRangeException(nameof(maxX));

        if (maxY < minY)
            throw new ArgumentOutOfRangeException(nameof(maxY));

        MinX = minX;
        MinY = minY;

        int width = checked(maxX - minX + 1);
        int height = checked(maxY - minY + 1);

        _data = new T[width, height];
    }

    public T this[int x, int y]
    {
        get => _data[TranslateX(x), TranslateY(y)];
        set => _data[TranslateX(x), TranslateY(y)] = value;
    }

    public bool Contains(int x, int y) =>
        x >= MinX && x <= MaxX &&
        y >= MinY && y <= MaxY;

    private int TranslateX(int x)
    {
        if (x < MinX || x > MaxX)
            throw new IndexOutOfRangeException($"X coordinate {x} is outside {MinX}..{MaxX}.");

        return x - MinX;
    }

    private int TranslateY(int y)
    {
        if (y < MinY || y > MaxY)
            throw new IndexOutOfRangeException($"Y coordinate {y} is outside {MinY}..{MaxY}.");

        return y - MinY;
    }
}
