using System;
using UnityEngine;

[Serializable]
public struct WorldGenData
{
    public MinMaxFloat elevation;
    public TerrainData data;
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
}
