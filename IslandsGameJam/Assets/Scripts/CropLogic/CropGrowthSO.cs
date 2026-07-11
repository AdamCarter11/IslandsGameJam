using UnityEngine;

[CreateAssetMenu(fileName = "CropGrowth", menuName = "Scriptable Objects/CropGrowthGroup")]
public class CropGrowthSO : ScriptableObject
{
    public string cropName;
    [TextArea(3, 10)] public string desc;
    public CropPropertiesSO[] stages;
}
