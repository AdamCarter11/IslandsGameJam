using UnityEngine;

[CreateAssetMenu(fileName = "CropProperties", menuName = "Scriptable Objects/CropProperties")]
public class CropPropertiesSO : ScriptableObject
{
    public Sprite cropVisual;
    public float growthTime = 5.0f;
    [Tooltip("Dry growth speed vs watered (0 = no growth while dry, 1 = same as watered, 2 = twice as fast while dry).")]
    public float dryGrowthMultiplier = 1f;

    // I think we will pull this info into the UI (ie, don't put it in the desc)
    public int goldGain = 10;
    [Tooltip("0.5 is baseline chain growth (prob want to keep this between .25 and 1.5)")] 
    public float multiBonus = 0.5f;

    public HarvestPattern harvestPattern;

}
