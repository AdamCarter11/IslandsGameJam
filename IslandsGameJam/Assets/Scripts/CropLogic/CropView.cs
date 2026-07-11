using UnityEngine;

/// <summary>
/// Visual component for a planted crop. Stage sprite + watered tile + needs-water icon.
/// Assign sprites on CropPrefab (WateredTile / NeedsWaterIcon children).
/// </summary>
public class CropView : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] SpriteRenderer wateredTileRenderer;
    [SerializeField] SpriteRenderer needsWaterIconRenderer;

    [Header("Optional sprite overrides (else uses renderer.sprite on children)")]
    [SerializeField] Sprite wateredTileSprite;
    [SerializeField] Sprite needsWaterIconSprite;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (wateredTileSprite != null && wateredTileRenderer != null)
            wateredTileRenderer.sprite = wateredTileSprite;
        if (needsWaterIconSprite != null && needsWaterIconRenderer != null)
            needsWaterIconRenderer.sprite = needsWaterIconSprite;
    }

    public void SetVisual(Sprite sprite)
    {
        if (spriteRenderer == null)
            return;
        spriteRenderer.sprite = sprite;
    }

    /// <summary>
    /// Watered: show tile indicator, hide needs-water icon.
    /// Dry: hide tile indicator, show needs-water icon.
    /// </summary>
    public void SetWatered(bool watered)
    {
        if (wateredTileRenderer != null)
            wateredTileRenderer.enabled = watered;

        if (needsWaterIconRenderer != null)
            needsWaterIconRenderer.enabled = !watered;
    }
}
