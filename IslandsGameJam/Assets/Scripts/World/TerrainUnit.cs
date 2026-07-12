using UnityEngine;

public class TerrainUnit : MonoBehaviour
{
    [Header("Components")]
    [SerializeField]
    private SpriteRenderer baseRenderer;
    [SerializeField]
    private SpriteRenderer overlayRenderer;
    [SerializeField]
    private SpriteRenderer underlayRenderer;

    [Header("Terrain Data")]
    [SerializeField]
    private TerrainData terrainData;
    [SerializeField]
    private TerrainData mockData;

    public TerrainType Type => terrainData != null ? terrainData.Type : default;
    public TerrainData Data => terrainData;

    private Sprite terrainOverlaySprite;

    private void Awake()
    {
        if (mockData != null) Initialize(mockData);
    }

    public void Initialize(TerrainData data)
    {
        terrainData = data;
        baseRenderer.sprite = terrainData.BaseSprite;
        terrainOverlaySprite = terrainData.OverlaySprite;
        overlayRenderer.sprite = terrainOverlaySprite;
        underlayRenderer.sprite = terrainData.UnderlaySprite;
    }

    public void SetCropVisual(Sprite sprite)
    {
        overlayRenderer.sprite = sprite;
    }

    public void ClearCropVisual()
    {
        overlayRenderer.sprite = terrainOverlaySprite;
    }
}
