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

    private void Awake()
    {
        if (mockData != null) Initialize(mockData);
    }

    public void Initialize(TerrainData data)
    {
        terrainData = data;
        baseRenderer.sprite = terrainData.BaseSprite;
        overlayRenderer.sprite = terrainData.OverlaySprite;
        underlayRenderer.sprite = terrainData.UnderlaySprite;
    }
}
