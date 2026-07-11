using ColorMak3r.Utility;
using System;
using UnityEngine;

[Serializable]
public struct WorldGenData
{
    public MinMaxFloat elevation;
    public TerrainData data;
    [Range(0f, 1f)]
    public float obstacleChance;
    public Sprite[] obstacleSprites;
}

[CreateAssetMenu(fileName = "WGDS__", menuName = "Island/World Gen DataSet")]
public class WorldGenDataSet : ScriptableObject
{
    [SerializeField]
    private WorldGenData[] dataSet;

    public TerrainData Match(float elevation)
    {
        foreach (var data in dataSet)
        {
            if (data.elevation.InRange(elevation))
            {
                return data.data;
            }
        }

        Debug.LogError($"No matching terrain data found for elevation: {elevation}");
        return null;
    }

    public bool TryGetObstacleData(TerrainData data, out Sprite obstacleSprite)
    {
        foreach (var d in dataSet)
        {
            if (d.data == data)
            {
                if (UnityEngine.Random.value < d.obstacleChance)
                {
                    obstacleSprite = d.obstacleSprites.Random();
                    return true;
                }
                else
                {
                    obstacleSprite = null;
                    return false;
                }

            }
        }

        obstacleSprite = null;
        return false;
    }
}
