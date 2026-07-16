using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared tooltip for shop rows, hotbar slots, and plain (non-crop) shop messages.
/// Crop show path uses authored identity (not relic-resolved).
/// </summary>
public class SeedTooltipUI : MonoBehaviour
{
    public static bool IsCropPreviewActive { get; private set; }

    [SerializeField] GameObject tooltipRoot;
    [SerializeField] TextMeshProUGUI tooltipName;
    [SerializeField] TextMeshProUGUI tooltipBody;
    [SerializeField] RectTransform tooltipFollowRoot;
    [SerializeField] RawImage previewRawImage;

    public void Show(CropGrowthSO crop, RectTransform near)
    {
        if (crop == null)
        {
            Hide();
            return;
        }

        ShowContent(
            string.IsNullOrEmpty(crop.cropName) ? crop.name : crop.cropName,
            crop.BuildTooltipBody(),
            near);

        var previewSystem = ShopPreviewSystem.Instance;
        var previewService = GameManager.Main?.SeedPreviewService;
        if (previewSystem == null || previewService == null)
        {
            SetPreviewVisible(false);
            return;
        }

        previewService.PreviewCrop(crop, previewSystem.CenterTile);
        previewSystem.SetCameraActive(true);
        IsCropPreviewActive = true;
        SetPreviewVisible(true);
    }

    public void ShowPlain(string title, string body, RectTransform near)
    {
        StopPreview();
        ShowContent(title, body, near);
    }

    public void Hide()
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);

        StopPreview();
    }

    void ShowContent(string title, string body, RectTransform near)
    {
        if (tooltipName != null)
            tooltipName.text = title ?? string.Empty;

        if (tooltipBody != null)
            tooltipBody.text = body ?? string.Empty;

        if (tooltipRoot != null)
            tooltipRoot.SetActive(true);

        PositionNear(near);
    }

    void StopPreview()
    {
        IsCropPreviewActive = false;
        GameManager.Main?.SeedPreviewService?.Clear();
        ShopPreviewSystem.Instance?.SetCameraActive(false);
        SetPreviewVisible(false);
    }

    void OnDisable()
    {
        StopPreview();
    }

    void SetPreviewVisible(bool visible)
    {
        if (previewRawImage != null)
            previewRawImage.enabled = visible;
    }

    void PositionNear(RectTransform near)
    {
        UiTooltipPositioner.PlaceNear(tooltipFollowRoot, near);
    }

#if UNITY_EDITOR
    public void EditorAssign(
        GameObject tooltip,
        TextMeshProUGUI name,
        TextMeshProUGUI body,
        RectTransform tooltipFollow = null)
    {
        tooltipRoot = tooltip;
        tooltipName = name;
        tooltipBody = body;
        tooltipFollowRoot = tooltipFollow != null
            ? tooltipFollow
            : tooltip != null ? tooltip.transform as RectTransform : null;
    }
#endif
}
