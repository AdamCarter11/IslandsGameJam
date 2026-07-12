using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRGB : MonoBehaviour
{
    [Header("Cycle Settings")]
    [Tooltip("How many full rainbow loops per second.")]
    [Min(0f)] public float cyclesPerSecond = 0.2f;

    [Tooltip("Saturation of the color (0 = gray, 1 = full color).")]
    [Range(0f, 1f)] public float saturation = 1f;

    [Tooltip("Value/Brightness of the color (0 = black, 1 = full brightness).")]
    [Range(0f, 1f)] public float value = 1f;

    [Tooltip("Use unscaled time so sprites keep animating during pauses.")]
    public bool useUnscaledTime = true;

    [Tooltip("Randomize where on the rainbow the cycle starts.")]
    public bool randomizeStartHue = true;

    [Header("Alpha")]
    [Tooltip("Keep the starting alpha from the SpriteRenderer color.")]
    public bool preserveAlpha = true;

    [Tooltip("Only used if Preserve Alpha is off.")]
    [Range(0f, 1f)] public float fixedAlpha = 1f;

    private SpriteRenderer spriteRenderer;
    private float startHue;
    private float baseAlpha;

    void Reset() => CacheSpriteRenderer();
    void Awake() => CacheSpriteRenderer();

    void OnEnable()
    {
        if (spriteRenderer == null) CacheSpriteRenderer();
        if (randomizeStartHue) startHue = Random.value;
    }

    void Update()
    {
        if (spriteRenderer == null) return;

        float t = useUnscaledTime ? Time.unscaledTime : Time.time;
        float hue = Mathf.Repeat(startHue + t * cyclesPerSecond, 1f);

        Color c = Color.HSVToRGB(hue, saturation, value);
        c.a = preserveAlpha ? baseAlpha : fixedAlpha;

        spriteRenderer.color = c;
    }

    private void CacheSpriteRenderer()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            baseAlpha = spriteRenderer.color.a;
            if (!randomizeStartHue)
            {
                // Initialize start hue from current color so it continues smoothly.
                Color.RGBToHSV(spriteRenderer.color, out startHue, out _, out _);
            }
        }
    }
}
