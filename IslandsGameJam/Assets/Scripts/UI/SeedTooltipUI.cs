using TMPro;
using UnityEngine;

/// <summary>
/// Shared seed tooltip for shop rows and hotbar slots. Shows authored crop identity (not relic-resolved).
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

        if (tooltipName != null)
            tooltipName.text = string.IsNullOrEmpty(crop.cropName) ? crop.name : crop.cropName;

        if (tooltipBody != null)
            tooltipBody.text = crop.BuildTooltipBody();

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
        if (tooltipFollowRoot == null || near == null)
            return;

        Vector3[] corners = new Vector3[4];
        near.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        tooltipFollowRoot.position = topCenter;
        tooltipFollowRoot.anchoredPosition += new Vector2(0f, 8f);
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
