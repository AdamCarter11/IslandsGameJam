using TMPro;
using UnityEngine;

/// <summary>
/// Shared tooltip for shop rows, hotbar slots, and plain (non-crop) shop messages.
/// Crop show path uses authored identity (not relic-resolved).
/// </summary>
public class SeedTooltipUI : MonoBehaviour
{
    [SerializeField] GameObject tooltipRoot;
    [SerializeField] TextMeshProUGUI tooltipName;
    [SerializeField] TextMeshProUGUI tooltipBody;
    [SerializeField] RectTransform tooltipFollowRoot;

    public void Show(CropGrowthSO crop, RectTransform near)
    {
        if (crop == null)
        {
            Hide();
            return;
        }

        ShowPlain(
            string.IsNullOrEmpty(crop.cropName) ? crop.name : crop.cropName,
            crop.BuildTooltipBody(),
            near);
    }

    public void ShowPlain(string title, string body, RectTransform near)
    {
        if (tooltipName != null)
            tooltipName.text = title ?? string.Empty;

        if (tooltipBody != null)
            tooltipBody.text = body ?? string.Empty;

        if (tooltipRoot != null)
            tooltipRoot.SetActive(true);

        PositionNear(near);
    }

    public void Hide()
    {
        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
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
