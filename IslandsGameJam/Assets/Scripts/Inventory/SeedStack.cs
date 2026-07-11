using System;
using UnityEngine;

[Serializable]
public class SeedStack
{
    public const int MaxStackSize = 32;

    public CropGrowthSO crop;
    public int count;

    public bool IsEmpty => crop == null || count <= 0;

    public void Clear()
    {
        crop = null;
        count = 0;
    }
}
