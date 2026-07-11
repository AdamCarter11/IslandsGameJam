using System.Collections;
using UnityEngine;

/// <summary>
/// Temporary harvest-chain sprite: scale pop at spawn, then parabolic hops between cells.
/// Spawned at runtime (no prefab); destroy the GameObject when the chain ends.
/// </summary>
public class HarvestHopVisual : MonoBehaviour
{
    SpriteRenderer spriteRenderer;

    /// <summary>
    /// Creates a flyer under <paramref name="parent"/> at world <paramref name="position"/>,
    /// scaled to zero so <see cref="Pop"/> can animate it in.
    /// </summary>
    public static HarvestHopVisual Spawn(
        Transform parent,
        Vector3 position,
        Sprite sprite,
        int sortingOrder,
        string sortingLayerName = "Overlay")
    {
        var go = new GameObject("HarvestFlyer");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = Vector3.zero;

        var visual = go.AddComponent<HarvestHopVisual>();
        visual.spriteRenderer = go.AddComponent<SpriteRenderer>();
        visual.spriteRenderer.sprite = sprite;
        visual.spriteRenderer.sortingOrder = sortingOrder;
        if (!string.IsNullOrEmpty(sortingLayerName))
            visual.spriteRenderer.sortingLayerName = sortingLayerName;

        return visual;
    }

    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;
    }

    /// <summary>Ease-out scale from 0 → 1 with a short overshoot.</summary>
    public IEnumerator Pop(float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease out + slight overshoot, settle to 1 at the end.
            float overshoot = 1.15f;
            float scale = t < 1f
                ? Mathf.Sin(t * Mathf.PI * 0.5f) * overshoot
                : 1f;
            if (t > 0.85f)
            {
                float settle = (t - 0.85f) / 0.15f;
                float peaked = Mathf.Sin(0.85f * Mathf.PI * 0.5f) * overshoot;
                scale = Mathf.Lerp(peaked, 1f, settle);
            }

            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Lerps XZ/XY from <paramref name="from"/> to <paramref name="to"/> with a sine arc on Y,
    /// plus a small squash near landing.
    /// </summary>
    public IEnumerator Hop(Vector3 from, Vector3 to, float duration, float height)
    {
        duration = Mathf.Max(0.01f, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 pos = Vector3.Lerp(from, to, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * height;
            transform.position = pos;

            // Slight squash near landing.
            float landPunch = t > 0.85f ? 1f - (t - 0.85f) / 0.15f * 0.15f : 1f;
            transform.localScale = new Vector3(1f / landPunch, landPunch, 1f);
            yield return null;
        }

        transform.position = to;
        transform.localScale = Vector3.one;
    }
}
