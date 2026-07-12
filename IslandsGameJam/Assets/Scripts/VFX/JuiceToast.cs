using TMPro;
using UnityEngine;

/// <summary>
/// Temporary world-space TMP toast: billboards on XZ, sin-wobbles on Y, then fades out.
/// Spawned at runtime (no prefab); destroys itself after lifetime.
/// </summary>
public class JuiceToast : MonoBehaviour
{
    const string UiFontResourcePath = "slapduck SDF";

    /// <summary>Sorting order high enough to draw above hop flyers, unlock toasts, and world sprites.</summary>
    public const int ComboSortingOrder = 32767;

    public float angle = 15f;
    public float speed = 2f;
    public float lifetime = 2.5f;
    public float fadeDuration = 0.6f;
    public float fontSize = 16f;

    static TMP_FontAsset cachedUiFont;

    TextMeshPro tmp;
    Quaternion billboardBase;
    float age;
    bool followScreenCenter;

    /// <summary>
    /// Creates a toast under <paramref name="parent"/> at world <paramref name="worldPos"/>.
    /// </summary>
    public static JuiceToast Spawn(
        Transform parent,
        Vector3 worldPos,
        string message,
        int sortingOrder,
        string sortingLayerName = "Overlay")
    {
        return SpawnInternal(parent, worldPos, message, sortingOrder, sortingLayerName, followScreenCenter: false);
    }

    /// <summary>
    /// Creates a toast that stays locked to the center of the main camera viewport.
    /// </summary>
    public static JuiceToast SpawnScreenCenter(
        Transform parent,
        string message,
        int sortingOrder,
        string sortingLayerName = "Overlay")
    {
        Vector3 pos = GetScreenCenterWorld();
        return SpawnInternal(parent, pos, message, sortingOrder, sortingLayerName, followScreenCenter: true);
    }

    static JuiceToast SpawnInternal(
        Transform parent,
        Vector3 worldPos,
        string message,
        int sortingOrder,
        string sortingLayerName,
        bool followScreenCenter)
    {
        var go = new GameObject("JuiceToast");
        go.transform.SetParent(parent, false);
        go.transform.position = worldPos;

        var toast = go.AddComponent<JuiceToast>();
        toast.followScreenCenter = followScreenCenter;

        var tmp = go.AddComponent<TextMeshPro>();
        TMP_FontAsset font = GetUiFont();
        if (font != null)
            tmp.font = font;

        tmp.text = message ?? string.Empty;
        tmp.fontSize = toast.fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.color = Color.white;

        var rect = tmp.rectTransform;
        rect.sizeDelta = followScreenCenter ? new Vector2(14f, 4f) : new Vector2(4f, 2f);

        var renderer = tmp.renderer;
        if (renderer != null)
        {
            renderer.sortingOrder = sortingOrder;
            if (!string.IsNullOrEmpty(sortingLayerName))
                renderer.sortingLayerName = sortingLayerName;
        }

        toast.tmp = tmp;
        toast.UpdateBillboardBase();
        return toast;
    }

    static TMP_FontAsset GetUiFont()
    {
        if (cachedUiFont != null)
            return cachedUiFont;

        cachedUiFont = Resources.Load<TMP_FontAsset>(UiFontResourcePath);
        if (cachedUiFont == null)
            cachedUiFont = TMP_Settings.defaultFontAsset;
        return cachedUiFont;
    }

    static Vector3 GetScreenCenterWorld()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return Vector3.zero;

        float depth = Mathf.Abs(cam.transform.position.z);
        if (depth < 0.01f)
            depth = 10f;
        return cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
    }

    void Update()
    {
        if (followScreenCenter)
            transform.position = GetScreenCenterWorld();

        UpdateBillboardBase();
        float y = Mathf.Sin(Time.time * speed) * angle;
        transform.rotation = billboardBase * Quaternion.Euler(0f, y, 0f);

        age += Time.deltaTime;
        float fadeStart = Mathf.Max(0f, lifetime - fadeDuration);
        if (tmp != null && age >= fadeStart)
        {
            float t = fadeDuration > 0.01f
                ? Mathf.Clamp01((age - fadeStart) / fadeDuration)
                : 1f;
            Color c = tmp.color;
            c.a = 1f - t;
            tmp.color = c;
        }

        if (age >= lifetime)
            Destroy(gameObject);
    }

    void UpdateBillboardBase()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            billboardBase = Quaternion.identity;
            return;
        }

        Vector3 forward = transform.position - cam.transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = cam.transform.forward;
            forward.y = 0f;
        }

        billboardBase = forward.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(forward.normalized, Vector3.up)
            : Quaternion.identity;
    }
}
