using ColorMak3r.Utility;
using UnityEngine;

[CreateAssetMenu(fileName = "TU__", menuName = "Island/Terrain Unit")]
public class TerrainData : ScriptableObject
{
    [Header("Terrain Unit Settings")]
    [SerializeField]
    private Sprite baseSprite;
    [SerializeField]
    private Sprite underlaySprite;
    [Range(0f, 1f)]
    [SerializeField]
    private float overlayChance = 0.5f;
    [SerializeField]
    private Sprite[] overlaySprites;

    public Sprite BaseSprite => baseSprite;
    public Sprite OverlaySprite => Random.value < overlayChance ? overlaySprites.Random() : null;
    public Sprite UnderlaySprite => underlaySprite;
}