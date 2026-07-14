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

    [Header("UI")]
    [SerializeField]
    private ToolTipUI toolTipUI;

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

    private bool isMouseDown = false;
    private Vector2Int lastMousePosition = Vector2Int.zero;

    private void Awake()
    {
        SetSize(1);
    }

    private void OnDisable()
    {
        HideObstacleTooltip();
    }

    private void Update()
    {
        if (GameManager.Main == null || !GameManager.Main.IsInitialized)
        {
            isMouseDown = false;
            HideObstacleTooltip();
            return;
        }

        if (GameManager.Main.IsGameOver)
        {
            isMouseDown = false;
            HideObstacleTooltip();
            return;
        }

        // Pause world interaction while the shop or options is open.
        var shopController = ShopController.Main;
        if (shopController != null && (shopController.IsOpen || shopController.IsOptionsOpen || shopController.IsRelicChoiceOpen))
        {
            isMouseDown = false;
            HideObstacleTooltip();
            return;
        }

        if (GameManager.Main.ConfirmPanelUI != null && GameManager.Main.ConfirmPanelUI.IsVisible)
        {
            isMouseDown = false;
            HideObstacleTooltip();
            return;
        }

        if (Mouse.current == null)
        {
            isMouseDown = false;
            HideObstacleTooltip();
            return;
        }

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

        var world = GameManager.Main.WorldManager;

        // Set selector size
        bool isUnlockChunk = world.IsInsideAvailableChunk(worldPosition);
        if (isUnlockChunk)
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
            if (ToolModeController.Main.CurrentMode == ToolMode.None)
            {
                if (world.HasObstacle(worldPosition))
                {
                    ClearPreview();
                    cachedPattern = null;
                }
                else if (world.TryGetTerrainUnit(worldPosition, out _))
                {
                    TryPreviewCrop(worldPosition);
                }
            }
            GameManager.Main.LandCostUI.Hide();
        }

        bool pointerOverUi = IsPointerOverUi();
        selectorRenderer.enabled = !pointerOverUi;

        if (world.TryGetCrop(worldPosition, out _))
            HideObstacleTooltip();
        else
            UpdateObstacleTooltip(worldPosition, pointerOverUi || isUnlockChunk);


        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (!pointerOverUi)
            {
                isMouseDown = true;
                OnLeftClicked(worldPosition);
                lastMousePosition = worldPosition;
            }
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isMouseDown = false;
        }

        if (isMouseDown && lastMousePosition != worldPosition)
        {
            // Prevent unlock chunk prompt on drag and stop drag on UI hover
            if (pointerOverUi || isUnlockChunk)
            {
                isMouseDown = false;
                return;
            }

            OnLeftClicked(worldPosition);
            lastMousePosition = worldPosition;
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

    private void UpdateObstacleTooltip(Vector2Int worldPosition, bool shouldHide)
    {
        if (shouldHide)
        {
            HideObstacleTooltip();
            return;
        }

        var world = GameManager.Main != null ? GameManager.Main.WorldManager : null;
        if (world == null || !world.TryGetObstacle(worldPosition, out GameObject obstacle))
        {
            HideObstacleTooltip();
            return;
        }

        ToolTipUI ui = ResolveToolTipUI();
        if (ui == null)
            return;

        var toolMode = ToolModeController.Main;
        bool inToolMode = toolMode != null && toolMode.CurrentMode != ToolMode.None;
        if (!inToolMode && ObstacleClearTracker.TryGetCurrentCost(out int clearCost))
        {
            ui.Show($"Clear for ${clearCost.ToString("N0")}", transform);
            return;
        }

        ToolTipObject toolTipObject = obstacle.GetComponent<ToolTipObject>();
        if (toolTipObject == null)
            toolTipObject = obstacle.GetComponentInChildren<ToolTipObject>(true);

        if (toolTipObject == null || !toolTipObject.HasToolTip)
        {
            HideObstacleTooltip();
            return;
        }

        ui.Show(toolTipObject, transform);
    }

    private ToolTipUI ResolveToolTipUI()
    {
        if (toolTipUI == null)
            toolTipUI = ToolTipUI.GetOrCreate();

        return toolTipUI;
    }

    private void HideObstacleTooltip()
    {
        toolTipUI?.Hide();
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
        spriteRenderer.sortingOrder = -1;
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
    /// obstacle, spends the current escalating gold cost and removes it.
    /// </summary>
    static bool TryClearObstacleAt(Vector2Int cell, WorldManager world)
    {
        if (world == null || !world.HasObstacle(cell))
            return false;

        var inventory = GameManager.Main?.Inventory;
        if (inventory == null)
            return false;

        if (!ObstacleClearTracker.TryGetCurrentCost(out int cost))
            return false;

        if (!inventory.TrySpendGold(cost))
            return false;

        world.RemoveObstacle(cell);
        ObstacleClearTracker.RegisterClear();
        SaveGameService.NotifyChanged();
        return true;
    }
}
