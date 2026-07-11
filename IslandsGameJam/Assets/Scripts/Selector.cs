using ColorMak3r.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

public class Selector : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer selectorRenderer;

    private void Awake()
    {
        selectorRenderer.size = Vector2.one * 1.5f;
    }

    private void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mousePosition);

        transform.position = worldPosition.SnapToGrid();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            OnLeftClicked(worldPosition);
        }
    }

    private void OnLeftClicked(Vector2 mousePosition)
    {
        // GameManager.Main.WorldManager.GetTerrainUnit(mousePosition.SnapToGrid().ToInt());
        Vector2Int cell = mousePosition.SnapToGrid().ToInt();
        var cropSystem = GameManager.Main.CropSystem;
        var world = GameManager.Main.WorldManager;
        if (cropSystem == null || world == null)
            return;

        if (world.TryGetCrop(cell, out CropCell crop) && crop != null)
        {
            if (crop.IsReady)
                cropSystem.HarvestAt(cell);
            return;
        }

        if (cropSystem.DebugCropToPlant != null)
        {
            cropSystem.PlantCrop(cell, cropSystem.DebugCropToPlant);
            return;
        }
    }
}
