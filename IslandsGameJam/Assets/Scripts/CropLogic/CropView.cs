using UnityEngine;

/// <summary>
/// Visual component for a planted crop instance. Swaps stage sprites on the SpriteRenderer.
/// </summary>
public class CropView : MonoBehaviour
{
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetVisual(Sprite sprite)
    {
        if (spriteRenderer == null)
            return;
        spriteRenderer.sprite = sprite;
    }
}
