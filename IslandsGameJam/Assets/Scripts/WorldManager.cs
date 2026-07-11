using ColorMak3r.Utility;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

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

    [Header("World Settings")]
    [SerializeField]
    private GameObject terrainUnitPrefab;

    private OffsetArray2D<TerrainUnit> terrainUnits;
    private OffsetArray2D<CropCell> crops;
    private Dictionary<Vector2Int, TerrainChunk> currentChunks = new Dictionary<Vector2Int, TerrainChunk>();
    private Dictionary<Vector2Int, TerrainChunk> availableChunks = new Dictionary<Vector2Int, TerrainChunk>();

    public void Initialize()
    {
        int minX = -worldWidth / 2;
        int maxX = worldWidth / 2;
        int minY = -worldHeight / 2;
        int maxY = worldHeight;
        terrainUnits = new OffsetArray2D<TerrainUnit>(minX, maxX, minY, maxY);
        crops = new OffsetArray2D<CropCell>(minX, maxX, minY, maxY);

        var firstChunk = new TerrainChunk(Vector2Int.zero);
        GenerateChunk(Vector2Int.zero); //temp
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

        crops[position.x, position.y] = null;

        if (terrainUnits != null &&
            terrainUnits.Contains(position.x, position.y) &&
            terrainUnits[position.x, position.y] != null)
        {
            terrainUnits[position.x, position.y].ClearCropVisual();
        }
    }

    /// <summary>
    /// Plants a crop at the given cell. Fails if out of bounds, no terrain, or cell occupied.
    /// </summary>
    public bool PlantCrop(Vector2Int position, CropGrowthSO crop)
    {
        if (crop == null || crops == null || terrainUnits == null)
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

        var stage = cell.CurrentStage;
        terrainUnits[position.x, position.y].SetCropVisual(stage != null ? stage.cropVisual : null);
        return true;
    }

    public List<TerrainChunk> GetAvailableChunksFromPosition(Vector2Int position)
    {
        position = position.SnapToGrid(chunkSize, true).ToInt();

        return null;
    }

    [Button]
    public void GenerateChunk(Vector2Int position)
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
                GameObject terrainUnitObject = Instantiate(terrainUnitPrefab, spawnPosition, Quaternion.identity, transform);
                TerrainUnit terrainUnit = terrainUnitObject.GetComponent<TerrainUnit>();
                terrainUnits[x, y] = terrainUnit;
            }
        }
    }

    [Button]
    public TerrainUnit GetTerrainUnit(Vector2Int position)
    {
        var x = position.x;
        var y = position.y;
        if (!terrainUnits.Contains(x, y))
            return null;
        var unit = terrainUnits[x, y];
        Debug.Log($"GetTerrainUnit({x}, {y}) = {unit}", unit);
        return unit;
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
