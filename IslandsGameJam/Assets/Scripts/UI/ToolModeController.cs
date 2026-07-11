using UnityEngine;
using UnityEngine.UI;

public enum ToolMode
{
    None,
    Water,
    Harvest,
    Destroy
}

/// <summary>
/// Tracks mutually exclusive tool modes (Water / Harvest / Destroy). Buttons live on the Game HUD.
/// </summary>
public class ToolModeController : MonoBehaviour
{
    public static ToolModeController Main { get; private set; }

    static readonly Color WaterIdleColor = new(0.2f, 0.45f, 0.55f, 0.95f);
    static readonly Color WaterActiveColor = new(0.25f, 0.7f, 0.85f, 1f);
    static readonly Color HarvestIdleColor = new(0.2f, 0.45f, 0.25f, 0.95f);
    static readonly Color HarvestActiveColor = new(0.3f, 0.75f, 0.35f, 1f);
    static readonly Color DestroyIdleColor = new(0.5f, 0.2f, 0.2f, 0.95f);
    static readonly Color DestroyActiveColor = new(0.85f, 0.3f, 0.3f, 1f);

    [SerializeField] Button waterButton;
    [SerializeField] Image waterButtonImage;
    [SerializeField] Button harvestButton;
    [SerializeField] Image harvestButtonImage;
    [SerializeField] Button destroyButton;
    [SerializeField] Image destroyButtonImage;

    public ToolMode CurrentMode { get; private set; } = ToolMode.None;

    public bool IsWateringMode => CurrentMode == ToolMode.Water;
    public bool IsHarvestMode => CurrentMode == ToolMode.Harvest;
    public bool IsDestroyMode => CurrentMode == ToolMode.Destroy;

    void Awake()
    {
        if (Main != null && Main != this)
        {
            Destroy(this);
            return;
        }
        Main = this;
    }

    void Start()
    {
        WireButton(waterButton, ToggleWatering);
        WireButton(harvestButton, ToggleHarvest);
        WireButton(destroyButton, ToggleDestroy);
        RefreshVisual();
    }

    void OnDestroy()
    {
        if (Main == this)
            Main = null;
    }

    static void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void ToggleWatering() => SetMode(CurrentMode == ToolMode.Water ? ToolMode.None : ToolMode.Water);
    public void ToggleHarvest() => SetMode(CurrentMode == ToolMode.Harvest ? ToolMode.None : ToolMode.Harvest);
    public void ToggleDestroy() => SetMode(CurrentMode == ToolMode.Destroy ? ToolMode.None : ToolMode.Destroy);

    public void SetMode(ToolMode mode)
    {
        if (CurrentMode == mode)
            return;

        CurrentMode = mode;
        RefreshVisual();
        GameManager.Main?.AudioService?.PlayUiClick();
    }

    /// <summary>Clears Water / Harvest / Destroy so planting can resume.</summary>
    public void ClearAllModes() => SetMode(ToolMode.None);

    /// <summary>Compatibility: enable watering, or clear all tool modes when disabled.</summary>
    public void SetWatering(bool enabled)
    {
        if (enabled)
            SetMode(ToolMode.Water);
        else
            ClearAllModes();
    }

    void RefreshVisual()
    {
        ResolveImage(ref waterButtonImage, waterButton);
        ResolveImage(ref harvestButtonImage, harvestButton);
        ResolveImage(ref destroyButtonImage, destroyButton);

        if (waterButtonImage != null)
            waterButtonImage.color = IsWateringMode ? WaterActiveColor : WaterIdleColor;
        if (harvestButtonImage != null)
            harvestButtonImage.color = IsHarvestMode ? HarvestActiveColor : HarvestIdleColor;
        if (destroyButtonImage != null)
            destroyButtonImage.color = IsDestroyMode ? DestroyActiveColor : DestroyIdleColor;
    }

    static void ResolveImage(ref Image image, Button button)
    {
        if (image == null && button != null)
            image = button.targetGraphic as Image;
    }

#if UNITY_EDITOR
    public void EditorAssign(Button water, Image waterImage, Button harvest, Image harvestImage, Button destroy, Image destroyImage)
    {
        waterButton = water;
        waterButtonImage = waterImage;
        harvestButton = harvest;
        harvestButtonImage = harvestImage;
        destroyButton = destroy;
        destroyButtonImage = destroyImage;
    }

    /// <summary>Legacy water-only assign used until HUD builder adds Harvest/Destroy buttons.</summary>
    public void EditorAssign(Button button, Image image)
    {
        waterButton = button;
        waterButtonImage = image;
    }
#endif
}
