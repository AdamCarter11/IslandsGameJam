using Sirenix.OdinInspector;
using UnityEngine;

public interface IActivatable
{
    bool Activate();
}

public class Composter : MonoBehaviour, IActivatable
{
    [Header("Settings")]
    [SerializeField]
    private float compostInterval = 10f;
    [SerializeField]
    private SpriteRenderer readySprite;

    [Header("Runtime")]
    [SerializeField]
    private bool hasFertilizer = false;
    public bool HasFertilizer => hasFertilizer;

    private float nextCompost = 0f;

    private void Awake()
    {
        UpdateVisual();
    }

    private void Update()
    {
        if (!hasFertilizer && Time.time > nextCompost)
        {
            Compost();
        }
    }

    [Button]
    public void Compost()
    {
        hasFertilizer = true;
        UpdateVisual();
    }

    public bool Activate()
    {
        return GetFertilizer();
    }

    public bool GetFertilizer()
    {
        if (!hasFertilizer) return false;

        GameManager.Main.CropSystem.AddFertilizer();
        hasFertilizer = false;
        nextCompost = Time.time + compostInterval;

        UpdateVisual();
        return true;
    }

    private void UpdateVisual()
    {
        readySprite.gameObject.SetActive(hasFertilizer);
    }
}
