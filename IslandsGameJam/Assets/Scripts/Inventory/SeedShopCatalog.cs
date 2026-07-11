using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SeedShopCatalog", menuName = "Scriptable Objects/SeedShopCatalog")]
public class SeedShopCatalog : ScriptableObject
{
    [Tooltip("Full seed pool used for island unlock rolls and shop listings.")]
    public List<CropGrowthSO> allSeeds = new();

    public int startingGold = 50;
}
