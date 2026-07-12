using UnityEngine;

/// <summary>
/// Visual component for a planted crop. Stage sprite + watered tile + needs-water / harvest-ready icons.
/// Assign sprites on CropPrefab (WateredTile / NeedsWaterIcon / HarvestReadyIcon children).
/// </summary>
public class CropView : MonoBehaviour
{
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] SpriteRenderer wateredTileRenderer;
    [SerializeField] SpriteRenderer needsWaterIconRenderer;
    [SerializeField] SpriteRenderer harvestReadyIconRenderer;

    [Header("Optional sprite overrides (else uses renderer.sprite on children)")]
    [SerializeField] Sprite wateredTileSprite;
    [SerializeField] Sprite needsWaterIconSprite;
    [SerializeField] Sprite harvestReadyIconSprite;
    [SerializeField] float needsWaterIconYPadding = 0.15f;

    bool isWatered;
    bool isHarvestReady;

    void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (wateredTileSprite != null && wateredTileRenderer != null)
            wateredTileRenderer.sprite = wateredTileSprite;
        if (needsWaterIconSprite != null && needsWaterIconRenderer != null)
            needsWaterIconRenderer.sprite = needsWaterIconSprite;
        if (harvestReadyIconSprite != null && harvestReadyIconRenderer != null)
            harvestReadyIconRenderer.sprite = harvestReadyIconSprite;

        ApplyStatusIcons();
    }

    void OnEnable()
    {
        GameplaySettings.OnHideCropStatusIconsChanged += ApplyStatusIcons;
        ApplyStatusIcons();
    }

    void OnDisable()
    {
        GameplaySettings.OnHideCropStatusIconsChanged -= ApplyStatusIcons;
    }

    public void SetVisual(Sprite sprite)
    {
        if (spriteRenderer == null)
            return;
        spriteRenderer.sprite = sprite;
        //RepositionNeedsWaterIcon();
    }

    private void RepositionNeedsWaterIcon()
    {
        if (needsWaterIconSprite == null || spriteRenderer?.sprite == null)
            return;

        float topY = spriteRenderer.sprite.bounds.max.y;

        float iconHalfH = 0f;
        if(needsWaterIconRenderer.sprite != null)
        {
            iconHalfH = needsWaterIconRenderer.sprite.bounds.extents.y * needsWaterIconRenderer.transform.localScale.y;
        }
        var t = needsWaterIconRenderer.transform;
        t.localPosition = new Vector3(0f, topY + needsWaterIconYPadding + iconHalfH, t.localPosition.z);
    }

    /// <summary>
    /// Watered: show tile indicator, hide needs-water icon.
    /// Dry: hide tile indicator, show needs-water icon (unless status icons are hidden in settings).
    /// Independent of <see cref="SetHarvestReady"/>.
    /// </summary>
    public void SetWatered(bool watered)
    {
        isWatered = watered;

        if (wateredTileRenderer != null)
            wateredTileRenderer.enabled = watered;

        ApplyStatusIcons();
    }

    /// <summary>
    /// Shows/hides the harvest-ready icon. Independent of water state
    /// (dry + ready can show both icons; prefab offsets keep them apart).
    /// Respects <see cref="GameplaySettings.HideCropStatusIcons"/>.
    /// </summary>
    public void SetHarvestReady(bool ready)
    {
        isHarvestReady = ready;
        ApplyStatusIcons();
    }

    void ApplyStatusIcons()
    {
        bool hide = GameplaySettings.HideCropStatusIcons;

        if (needsWaterIconRenderer != null)
            needsWaterIconRenderer.enabled = !hide && !isWatered;

        if (harvestReadyIconRenderer != null)
            harvestReadyIconRenderer.enabled = !hide && isHarvestReady;
    }
}
