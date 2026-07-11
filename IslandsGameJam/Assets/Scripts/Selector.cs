using ColorMak3r.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

public class Selector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Vector2 sizeOffset = Vector2.one * 0.5f;
    [SerializeField]
    private SpriteRenderer selectorRenderer;
    [SerializeField]
    private CameraTarget cameraTarget;

    private void Awake()
    {
        SetSize(1);
    }

    private void Update()
    {
        if (GameManager.Main == null || !GameManager.Main.IsInitialized)
            return;

        if (Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        var snappedPosition = Camera.main.ScreenToWorldPoint(mousePosition).SnapToGrid();
        Vector2Int worldPosition = snappedPosition.ToInt();

        // Camera movement reset to center on space bar
        if (Keyboard.current?.spaceKey.isPressed ?? false)
            cameraTarget.transform.position = transform.position;
        else
            transform.position = snappedPosition;

        // Set selector size
        if (GameManager.Main.WorldManager.IsInsideAvailableChunk(worldPosition))
        {
            SetSize(3);
            transform.position = worldPosition.SnapToGrid(3, true);
        }
        else
        {
            SetSize(1);
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            OnLeftClicked(worldPosition);
        }
    }

    private void SetSize(int size)
    {
        selectorRenderer.size = new Vector2(size, size) + sizeOffset;
    }

    private void OnLeftClicked(Vector2Int worldPosition)
    {
        Vector2Int cell = worldPosition;
        Vector2Int chunk = worldPosition.SnapToGrid(3, true).ToInt();
        var cropSystem = GameManager.Main.CropSystem;
        var world = GameManager.Main.WorldManager;

        if (cropSystem == null || world == null)
        {
            return;
        }

        if (world.IsInsideAvailableChunk(cell))
        {
            world.UnlockChunk(chunk);
            return;
        }

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
