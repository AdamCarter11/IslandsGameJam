using UnityEngine;

public class SeedPreviewService : MonoBehaviour
{
    [SerializeField] private SpriteRenderer cropGhostRenderer;
    [SerializeField] private Sprite previewTile;
    [SerializeField] private Transform previewRoot;
    [SerializeField] private LayerMask previewLayerMask;

    private CropGrowthSO cachedCrop;
    private HarvestPattern cachedPattern;
    private int cachedStage = int.MinValue;

    public void PreviewCrop(CropGrowthSO crop, Vector2Int coordinate, int stage = -1)
    {
        if (crop == null)
        {
            Clear();
            return;
        }

        int resolvedStage = ResolveStage(crop, stage);
        if (resolvedStage < 0 || crop.stages[resolvedStage] == null)
        {
            Clear();
            return;
        }

        if (!crop.TryGetHarvestPattern(out HarvestPattern pattern, resolvedStage))
        {
            Clear();
            return;
        }

        SetGhost(crop, coordinate, resolvedStage);
        PreviewPattern(crop, pattern, coordinate, resolvedStage);
    }

    public void Clear()
    {
        cachedCrop = null;
        cachedPattern = null;
        cachedStage = int.MinValue;

        if (cropGhostRenderer != null)
            cropGhostRenderer.sprite = null;

        ClearPatternPreview();
    }

    int ResolveStage(CropGrowthSO crop, int stage)
    {
        if (crop.stages == null || crop.stages.Length == 0)
            return -1;

        if (stage < 0)
            return crop.stages.Length - 1;

        return Mathf.Clamp(stage, 0, crop.stages.Length - 1);
    }

    void SetGhost(CropGrowthSO crop, Vector2Int coordinate, int stage)
    {
        if (cropGhostRenderer == null)
            return;

        cropGhostRenderer.transform.position = (Vector2)coordinate;
        cropGhostRenderer.enabled = true;
        cropGhostRenderer.sprite = crop.stages[stage] != null ? crop.stages[stage].cropVisual : null;
    }

    void PreviewPattern(CropGrowthSO crop, HarvestPattern pattern, Vector2Int coordinate, int stage)
    {
        if (previewRoot == null)
            return;

        previewRoot.transform.position = (Vector2)coordinate;

        if (cachedCrop == crop && cachedPattern == pattern && cachedStage == stage)
            return;

        cachedCrop = crop;
        cachedPattern = pattern;
        cachedStage = stage;

        ClearPatternPreview();

        if (pattern == null)
            return;

        if (pattern.kind == HarvestPatternKind.Offsets)
        {
            foreach (var offset in pattern.offsets)
                SpawnPreviewTile(new Vector2Int(offset.x, offset.y));
        }
        else if (pattern.kind == HarvestPatternKind.Ray)
        {
            Vector2Int current = Vector2Int.zero;
            for (int i = 0; i < pattern.maxSteps; i++)
            {
                current += pattern.direction;
                SpawnPreviewTile(current);
            }
        }
    }

    void ClearPatternPreview()
    {
        if (previewRoot == null)
            return;

        for (int i = previewRoot.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = previewRoot.transform.GetChild(i).gameObject;
            Destroy(child);
        }
    }

    void SpawnPreviewTile(Vector2Int localEndPosition)
    {
        if (previewRoot == null)
            return;

        GameObject previewTileInstance = new GameObject("PreviewTile");
        previewTileInstance.transform.SetParent(previewRoot, false);
        previewTileInstance.transform.localPosition = Vector3.zero;
        var spriteRenderer = previewTileInstance.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = previewTile;
        spriteRenderer.sortingOrder = -1;
        previewTileInstance.AddComponent<SpriteRGB>();
        previewTileInstance.AddComponent<AutoMove>().Initialize(localEndPosition);
    }

#if UNITY_EDITOR
    public void EditorAssign(SpriteRenderer ghostRenderer, Sprite tileSprite, Transform root)
    {
        cropGhostRenderer = ghostRenderer;
        previewTile = tileSprite;
        previewRoot = root;
    }
#endif
}
