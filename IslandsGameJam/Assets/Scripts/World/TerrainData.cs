using ColorMak3r.Utility;
using UnityEngine;

public enum TerrainType
{
    Plains,
    Wasteland,
    Wetland,
    Desert,
    Mountainous,
    Tundra,
}

[CreateAssetMenu(fileName = "TU__", menuName = "Island/Terrain Unit")]
public class TerrainData : ScriptableObject
{
    [Header("Terrain Unit Settings")]
    [SerializeField]
    private TerrainType type;

    [Header("Graphic Settings")]
    [SerializeField]
    private Sprite baseSprite;
    [SerializeField]
    private Sprite underlaySprite;
    [Range(0f, 1f)]
    [SerializeField]
    private float overlayChance = 0.5f;
    [SerializeField]
    private Sprite[] overlaySprites;

    public TerrainType Type => type;
    public Sprite BaseSprite => baseSprite;
    public Sprite OverlaySprite => Random.value < overlayChance ? overlaySprites.Random() : null;
    public Sprite UnderlaySprite => underlaySprite;
}