using UnityEngine;

[CreateAssetMenu(fileName = "CropGrowth", menuName = "Scriptable Objects/CropGrowthGroup")]
public class CropGrowthSO : ScriptableObject
{
    public string cropName;
    [TextArea(3, 10)] public string desc;
    public CropPropertiesSO[] stages;

    [Header("Shop")]
    [Tooltip("Shown in the shop/hotbar. Falls back to the first stage visual if unset.")]
    public Sprite shopIcon;
    public int seedPrice = 10;
    public bool unlockedByDefault;

    public Sprite GetShopIcon()
    {
        if (shopIcon != null)
            return shopIcon;
        if (stages != null && stages.Length > 0 && stages[0] != null)
            return stages[0].cropVisual;
        return null;
    }
}
