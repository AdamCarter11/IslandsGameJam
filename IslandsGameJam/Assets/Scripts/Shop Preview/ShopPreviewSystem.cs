using UnityEngine;

public class ShopPreviewSystem : MonoBehaviour
{
    public static ShopPreviewSystem Instance { get; private set; }

    [SerializeField]
    private Camera shopPreviewCamera;
    [SerializeField]
    private GameObject terrainUnitPrefab;
    [SerializeField]
    private Vector2Int previewSize = new Vector2Int(7, 7);

    public Vector2Int CenterTile { get; private set; }

    private void Awake()
    {
        Instance = this;
        SetCameraActive(false);

        if (shopPreviewCamera == null || terrainUnitPrefab == null)
        {
            Debug.LogError("Shop preview camera and terrain prefab must be assigned.", this);
            return;
        }

        previewSize.x = Mathf.Max(1, previewSize.x);
        previewSize.y = Mathf.Max(1, previewSize.y);
        Vector3 cameraPosition = shopPreviewCamera.transform.position;
        CenterTile = Vector2Int.RoundToInt(new Vector2(cameraPosition.x, cameraPosition.y));

        SpawnTerrainGrid();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetCameraActive(bool active)
    {
        if (shopPreviewCamera != null)
            shopPreviewCamera.enabled = active;
    }

    private void SpawnTerrainGrid()
    {
        Vector2 halfSize = new Vector2(previewSize.x - 1, previewSize.y - 1) * 0.5f;
        Vector3 gridCenter = new Vector3(CenterTile.x, CenterTile.y, 0f);

        for (int x = 0; x < previewSize.x; x++)
        {
            for (int y = 0; y < previewSize.y; y++)
            {
                Vector3 position = gridCenter + new Vector3(x - halfSize.x, y - halfSize.y, 0f);
                Instantiate(terrainUnitPrefab, position, Quaternion.identity, shopPreviewCamera.transform);
            }
        }
    }
}