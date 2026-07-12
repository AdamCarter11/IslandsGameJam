using ColorMak3r.Utility;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct WorldGenData
{
    public MinMaxFloat elevation;
    public TerrainData data;
    [Range(0f, 1f)]
    public float obstacleChance;
    public GameObject[] obstaclePrefabs;
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

    public bool TryGetObstacle(TerrainData data, out GameObject prefab)
    {
        foreach (var d in dataSet)
        {
            if (d.data == data)
            {
                if (UnityEngine.Random.value < d.obstacleChance)
                {
                    prefab = d.obstaclePrefabs.Random();
                    return true;
                }
                else
                {
                    prefab = null;
                    return false;
                }

            }
        }

        prefab = null;
        return false;
    }

    public void CollectTerrains(List<TerrainData> into)
    {
        if (into == null || dataSet == null)
            return;

        for (int i = 0; i < dataSet.Length; i++)
        {
            TerrainData terrain = dataSet[i].data;
            if (terrain != null)
                into.Add(terrain);
        }
    }
}
