using ColorMak3r.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.LightTransport;

public class Selector : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private Vector2 sizeOffset = Vector2.one * 0.5f;
    [SerializeField]
    private SpriteRenderer selectorRenderer;
    [SerializeField]
    private SpriteRenderer seedPreview;
    [SerializeField]
    private CameraTarget cameraTarget;

    [Header("Crop Preview")]
    [SerializeField]
    private Sprite previewTile;
    [SerializeField]
    private Transform previewRoot;

    [Header("Tool Preview")]
    [SerializeField]
    private Sprite harvestSprite;
    [SerializeField]
    private Sprite waterSprite;
    [SerializeField]
    private Sprite destroySprite;
    [SerializeField]
    private Sprite fertilizeSprite;

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

        if (GameManager.Main.ConfirmPanelUI.IsVisible)
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

        switch (ToolModeController.Main.CurrentMode)
        {
            case ToolMode.None:
                seedPreview.enabled = true;
                seedPreview.sprite = null;
                break;
            case ToolMode.Water:
                seedPreview.sprite = waterSprite;
                seedPreview.enabled = true;
                ClearPatternPreview();
                break;
            case ToolMode.Harvest:
                seedPreview.sprite = harvestSprite;
                seedPreview.enabled = true;
                ClearPatternPreview();
                break;
            case ToolMode.Destroy:
                seedPreview.sprite = destroySprite;
                seedPreview.enabled = true;
                ClearPatternPreview();
                break;
            case ToolMode.Fertilize:
                seedPreview.sprite = fertilizeSprite;
                seedPreview.enabled = true;
                ClearPatternPreview();
                break;
        }

        // Set selector size
        if (GameManager.Main.WorldManager.IsInsideAvailableChunk(worldPosition))
        {
            SetSize(3);
            transform.position = worldPosition.SnapToGrid(3, true);
            var cost = GameManager.Main.LandUnlockSystem.GetCurrentCost();
            GameManager.Main.LandCostUI.UpdateText($"Unlock for\n${cost.ToString("N0")}");
            GameManager.Main.LandCostUI.Show();
            ClearPreview();
            cachedPattern = null;
        }
        else
        {
            SetSize(1);
            if (ToolModeController.Main.CurrentMode == ToolMode.None &&
                GameManager.Main.WorldManager.TryGetTerrainUnit(worldPosition, out _)) TryPreviewCrop(worldPosition);
            GameManager.Main.LandCostUI.Hide();
        }

        selectorRenderer.enabled = !IsPointerOverUi();

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

    private void TryPreviewCrop(Vector2Int worldPosition)
    {
        Vector2Int cell = worldPosition;
        Vector2Int chunk = worldPosition.SnapToGrid(3, true).ToInt();
        var cropSystem = GameManager.Main.CropSystem;
        var world = GameManager.Main.WorldManager;
        var landUnlocker = GameManager.Main.LandUnlockSystem;
        var confirmPanel = GameManager.Main.ConfirmPanelUI;
        var inventory = GameManager.Main.Inventory;

        if (cropSystem == null || world == null)
        {
            return;
        }

        if (world.TryGetCrop(cell, out var cropCell) && cropCell.crop.TryGetHarvestPattern(out var pattern))
        {
            PreviewPattern(pattern, worldPosition);
            seedPreview.sprite = null;
        }
        else if (inventory.GetCurrentHarvestPattern(out pattern, out var seedSprite))
        {
            seedPreview.sprite = seedSprite;
            PreviewPattern(pattern, worldPosition);
        }
        else
        {
            ClearPreview();
            cachedPattern = null;
        }
    }

    private HarvestPattern cachedPattern;
    private void PreviewPattern(HarvestPattern pattern, Vector2Int worldPosition)
    {
        previewRoot.transform.position = (Vector2)worldPosition;

        if (cachedPattern == pattern)
        {
            return;
        }
        cachedPattern = pattern;

        ClearPatternPreview();

        if (pattern.kind == HarvestPatternKind.Offsets)
        {
            foreach (var offset in pattern.offsets)
            {
                SpawnPreviewTile(new Vector2Int(offset.x, offset.y));
            }
        }
        else if (pattern.kind == HarvestPatternKind.Ray)
        {
            Vector2Int current = Vector2Int.zero;
            for (int i = 0; i < pattern.maxSteps; i++)
            {
                current += pattern.direction;
                SpawnPreviewTile(current);
            }
        }
    }

    private void ClearPreview()
    {
        seedPreview.sprite = null;
        ClearPatternPreview();
    }

    private void ClearPatternPreview()
    {
        for (int i = previewRoot.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = previewRoot.transform.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    private void SpawnPreviewTile(Vector2Int localEndPosition)
    {
        GameObject previewTileInstance = new GameObject("PreviewTile");
        previewTileInstance.transform.SetParent(previewRoot, false);
        previewTileInstance.transform.localPosition = Vector3.zero;
        var spriteRenderer = previewTileInstance.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = previewTile;
        spriteRenderer.sortingOrder = 100;
        previewTileInstance.AddComponent<SpriteRGB>();
        previewTileInstance.AddComponent<AutoMove>().Initialize(localEndPosition);
    }



    private void OnLeftClicked(Vector2Int worldPosition)
    {
        Vector2Int cell = worldPosition;
        Vector2Int chunk = worldPosition.SnapToGrid(3, true).ToInt();
        var cropSystem = GameManager.Main.CropSystem;
        var world = GameManager.Main.WorldManager;
        var landUnlocker = GameManager.Main.LandUnlockSystem;
        var confirmPanel = GameManager.Main.ConfirmPanelUI;

        if (cropSystem == null || world == null)
        {
            return;
        }

        if (cropSystem.IsHarvestBusy)
            return;

        if (world.IsInsideAvailableChunk(cell) && landUnlocker.CanUnlockLand())
        {
            confirmPanel.Show($"Unlock land for ${landUnlocker.GetCurrentCost().ToString("N0")}", onYes: () =>
            {
                if (landUnlocker.UnlockLand())
                {
                    world.UnlockChunk(chunk);
                }
            });
            return;
        }

        // Obstacle clear (EnableClearObstacles): prefer when not in a tool mode.
        var toolMode = ToolModeController.Main;
        bool inToolMode = toolMode != null && toolMode.CurrentMode != ToolMode.None;
        if (!inToolMode
            && world.HasObstacle(cell)
            && RelicEffectUtility.HasEffect(RelicEffectType.EnableClearObstacles))
        {
            TryClearObstacleAt(cell, world);
            return;
        }

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

            if (toolMode.IsFertilizeMode)
            {
                if (world.TryGetCrop(cell, out var fertilizeCrop) && fertilizeCrop != null)
                    cropSystem.FertilizeAt(cell);
                return;
            }
        }

        if (world.TryActivateObstacle(cell))
            return;

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

    /// <summary>
    /// If an <see cref="RelicEffectType.EnableClearObstacles"/> relic is owned and the cell has an
    /// obstacle, spends the effect's gold cost and removes it. Uses the lowest cost among matching effects.
    /// </summary>
    static bool TryClearObstacleAt(Vector2Int cell, WorldManager world)
    {
        if (world == null || !world.HasObstacle(cell))
            return false;
        if (!RelicEffectUtility.HasEffect(RelicEffectType.EnableClearObstacles))
            return false;

        var inventory = GameManager.Main?.Inventory;
        if (inventory == null)
            return false;

        float bestCost = float.PositiveInfinity;
        RelicEffectUtility.ForEachEffect(RelicEffectType.EnableClearObstacles, effect =>
        {
            if (effect.amount < bestCost)
                bestCost = effect.amount;
        });

        if (float.IsPositiveInfinity(bestCost))
            return false;

        int cost = Mathf.Max(0, Mathf.RoundToInt(bestCost));
        if (!inventory.TrySpendGold(cost))
            return false;

        world.RemoveObstacle(cell);
        SaveGameService.NotifyChanged();
        return true;
    }
}
