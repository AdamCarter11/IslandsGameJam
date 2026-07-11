using UnityEngine;

[CreateAssetMenu(fileName = "CropProperties", menuName = "Scriptable Objects/CropProperties")]
public class CropPropertiesSO : ScriptableObject
{
    public Sprite cropVisual;
    public float growthTime = 5.0f;

    // I think we will pull this info into the UI (ie, don't put it in the desc)
    public int goldGain = 10;
    [Tooltip("1 means default factorial growth (prob want to keep this between .5 and 2)")] 
    public float multiBonus = 1;

    public HarvestPattern harvestPattern;

}
