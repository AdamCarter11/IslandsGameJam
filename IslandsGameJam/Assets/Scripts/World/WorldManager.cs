using ColorMak3r.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static UnityEngine.UI.Image;

public class WorldManager : MonoBehaviour
{
    [Header("World Settings")]
    [SerializeField]
    private int worldWidth = 500;
    [SerializeField]
    private int worldHeight = 500;

    [Header("Chunk Settings")]
    [SerializeField]
    private int chunkSize = 3;

    [Header("Generation Settings")]
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
    private WorldGenDataSet worldGenDataSet;

    [Header("Prefabs")]
    [SerializeField]
    private GameObject terrainUnitPrefab;
    [SerializeField]
    private GameObject cropPrefab;

    private OffsetArray2D<TerrainData> terrainDataSet;
    private OffsetArray2D<TerrainUnit> terrainUnits;
    private OffsetArray2D<CropCell> crops;
    private Dictionary<Vector2Int, TerrainChunk> currentChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Dictionary<Vector2Int, TerrainChunk> availableChunks = new Dictionary<Vector2Int, TerrainChunk>();

    public void Initialize()
    {
        int minX = -worldWidth / 2;
        int maxX = worldWidth / 2;
        int minY = -worldHeight / 2;
        int maxY = worldHeight / 2;
        terrainDataSet = new OffsetArray2D<TerrainData>(minX, maxX, minY, maxY);
        terrainUnits = new OffsetArray2D<TerrainUnit>(minX, maxX, minY, maxY);
        crops = new OffsetArray2D<CropCell>(minX, maxX, minY, maxY);

        GenerateWorld();

        var firstChunk = new TerrainChunk(Vector2Int.zero);
        currentChunks.Add(firstChunk.Position, firstChunk);
        GenerateAvailableChunksFromPosition(firstChunk.Position);
        BuildChunk(firstChunk.Position);
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
        if (crops[position.x, position.y] != null)
            return false;

        var cell = new CropCell
        {
            crop = crop,
            stageIndex = 0,
            stageElapsed = 0f
        };
        crops[position.x, position.y] = cell;

        var spawnPosition = new Vector2(position.x, position.y);
        var cropObject = Instantiate(cropPrefab, spawnPosition, Quaternion.identity, transform);
        var view = cropObject.GetComponent<CropView>();
        var stage = cell.CurrentStage;
        if (view != null)
            view.SetVisual(stage != null ? stage.cropVisual : null);
        cell.view = view;
        return true;
    }

    #endregion

    #region Terrain Management

    public void GenerateWorld()
    {
        for (int i = terrainDataSet.MinX; i < terrainDataSet.MaxX; ++i)
        {
            for (int j = terrainDataSet.MinY; j < terrainDataSet.MaxY; ++j)
            {
                var position = new Vector2Int(i, j);
                var elevation = GetValue(i, j);
                var data = worldGenDataSet.Match(elevation);
                if (data != null)
                {
                    terrainDataSet[i, j] = data;
                }
                else
                {
                    Debug.LogError($"No terrain data found for elevation {elevation} at position {position}");
                }
            }
        }
    }

    protected virtual float GetValue(float x, float y)
    {
        return GetNoise(x + worldWidth / 2, y + worldHeight / 2, Vector2.zero, dimension, scale, octaves, persistence, frequencyBase, exponent);
    }

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

        if (GameManager.Main != null)
            SeedUnlockService.TryUnlockRandom(GameManager.Main.Inventory, GameManager.Main.SeedShopCatalog);
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
                Debug.Log($"Added available chunk at {pos}");
            }
        }
    }

    [Button]
    public void BuildChunk(Vector2Int position)
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
                var data = terrainDataSet[x, y];
                GameObject terrainUnitObject = Instantiate(terrainUnitPrefab, spawnPosition, Quaternion.identity, transform);
                TerrainUnit terrainUnit = terrainUnitObject.GetComponent<TerrainUnit>();
                terrainUnit.Initialize(data);
                terrainUnits[x, y] = terrainUnit;
            }
        }
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