using UnityEngine;

/// <summary>
/// Places UI tooltips near a target, flipping and clamping so they stay on screen.
/// </summary>
public static class UiTooltipPositioner
{
    const float Gap = 8f;
    const float ScreenPadding = 8f;

    public static void PlaceNear(RectTransform tooltip, RectTransform near)
    {
        if (tooltip == null || near == null)
            return;

        Canvas canvas = tooltip.GetComponentInParent<Canvas>();
        Camera cam = GetCanvasCamera(canvas);

        Vector3[] nearCorners = new Vector3[4];
        near.GetWorldCorners(nearCorners);
        Vector3 aboveAnchor = (nearCorners[1] + nearCorners[2]) * 0.5f;
        Vector3 belowAnchor = (nearCorners[0] + nearCorners[3]) * 0.5f;

        // Prefer above the target (pivot at tooltip bottom).
        tooltip.pivot = new Vector2(0.5f, 0f);
        tooltip.position = aboveAnchor;
        tooltip.anchoredPosition += new Vector2(0f, Gap);

        Canvas.ForceUpdateCanvases();

        if (OverflowsTop(tooltip, cam, ScreenPadding))
        {
            // Flip below (pivot at tooltip top).
            tooltip.pivot = new Vector2(0.5f, 1f);
            tooltip.position = belowAnchor;
            tooltip.anchoredPosition += new Vector2(0f, -Gap);
        }

        ClampToScreen(tooltip, cam, ScreenPadding);
    }

    static Camera GetCanvasCamera(Canvas canvas)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;
        return canvas.worldCamera;
    }

    static bool OverflowsTop(RectTransform tooltip, Camera cam, float padding)
    {
        GetScreenBounds(tooltip, cam, out _, out _, out _, out float maxY);
        return maxY > Screen.height - padding;
    }

    static void ClampToScreen(RectTransform tooltip, Camera cam, float padding)
    {
        GetScreenBounds(tooltip, cam, out float minX, out float maxX, out float minY, out float maxY);

        float dx = 0f;
        float dy = 0f;
        if (minX < padding)
            dx = padding - minX;
        else if (maxX > Screen.width - padding)
            dx = Screen.width - padding - maxX;

        if (minY < padding)
            dy = padding - minY;
        else if (maxY > Screen.height - padding)
            dy = Screen.height - padding - maxY;

        if (Mathf.Approximately(dx, 0f) && Mathf.Approximately(dy, 0f))
            return;

        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(cam, tooltip.position);
        screenPos.x += dx;
        screenPos.y += dy;

        var parent = tooltip.parent as RectTransform;
        if (parent != null
            && RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screenPos, cam, out Vector3 world))
        {
            tooltip.position = world;
        }
    }

    static void GetScreenBounds(
        RectTransform tooltip,
        Camera cam,
        out float minX,
        out float maxX,
        out float minY,
        out float maxY)
    {
        Vector3[] corners = new Vector3[4];
        tooltip.GetWorldCorners(corners);

        minX = float.MaxValue;
        maxX = float.MinValue;
        minY = float.MaxValue;
        maxY = float.MinValue;

        for (int i = 0; i < 4; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, corners[i]);
            minX = Mathf.Min(minX, screen.x);
            maxX = Mathf.Max(maxX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxY = Mathf.Max(maxY, screen.y);
        }
    }
}
