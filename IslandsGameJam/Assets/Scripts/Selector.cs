using ColorMak3r.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
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

        // Pause world interaction while the shop is open.
        if (ShopController.Main != null && ShopController.Main.IsOpen)
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
            if (IsPointerOverUi())
                return;
            OnLeftClicked(worldPosition);
        }
    }

    static bool IsPointerOverUi()
    {
        if (EventSystem.current == null)
            return false;

        // Input System: pass the active pointer device id when available.
        if (Pointer.current != null)
            return EventSystem.current.IsPointerOverGameObject(Pointer.current.deviceId);

        return EventSystem.current.IsPointerOverGameObject();
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

        var toolMode = ToolModeController.Main;
        if (toolMode != null)
        {
            if (toolMode.IsWateringMode)
            {
                if (world.TryGetCrop(cell, out var wateredCrop) && wateredCrop != null)
                    cropSystem.WaterAt(cell);
                return;
            }

            if (toolMode.IsHarvestMode)
            {
                if (world.TryGetCrop(cell, out var harvestCrop) && harvestCrop != null && harvestCrop.IsReady)
                    cropSystem.HarvestAt(cell);
                return;
            }

            if (toolMode.IsDestroyMode)
            {
                if (world.TryGetCrop(cell, out var destroyCrop) && destroyCrop != null)
                    cropSystem.DestroyAt(cell);
                return;
            }
        }

        // No tool mode: plant from hotbar on empty cells only.
        if (world.TryGetCrop(cell, out CropCell existing) && existing != null)
            return;

        var inventory = GameManager.Main.Inventory;
        if (inventory == null)
            return;

        var selected = inventory.GetSlot(inventory.SelectedSlot);
        if (selected == null || selected.IsEmpty)
            return;

        if (cropSystem.PlantCrop(cell, selected.crop))
            inventory.TryConsumeSelected(out _);
    }
}
