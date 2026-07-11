using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tracks tool modes such as watering. Water button lives on the Game HUD.
/// </summary>
public class ToolModeController : MonoBehaviour
{
    public static ToolModeController Main { get; private set; }

    static readonly Color WaterIdleColor = new(0.2f, 0.45f, 0.55f, 0.95f);
    static readonly Color WaterActiveColor = new(0.25f, 0.7f, 0.85f, 1f);

    [SerializeField] Button waterButton;
    [SerializeField] Image waterButtonImage;

    public bool IsWateringMode { get; private set; }

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
        if (waterButton != null)
        {
            waterButton.onClick.RemoveAllListeners();
            waterButton.onClick.AddListener(ToggleWatering);
        }

        RefreshVisual();
    }

    void OnDestroy()
    {
        if (Main == this)
            Main = null;
    }

    public void ToggleWatering() => SetWatering(!IsWateringMode);

    public void SetWatering(bool enabled)
    {
        if (IsWateringMode == enabled)
            return;

        IsWateringMode = enabled;
        RefreshVisual();
    }

    void RefreshVisual()
    {
        if (waterButtonImage == null && waterButton != null)
            waterButtonImage = waterButton.targetGraphic as Image;

        if (waterButtonImage != null)
            waterButtonImage.color = IsWateringMode ? WaterActiveColor : WaterIdleColor;
    }

#if UNITY_EDITOR
    public void EditorAssign(Button button, Image image)
    {
        waterButton = button;
        waterButtonImage = image;
    }
#endif
}
