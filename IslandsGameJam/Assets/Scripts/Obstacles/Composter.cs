using Sirenix.OdinInspector;
using UnityEngine;

public class Composter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private float compostInterval = 10f;

    [Header("Runtime")]
    [SerializeField]
    private bool hasFertilizer = false;
    public bool HasFertilizer => hasFertilizer;

    private float nextCompost = 0f;

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
    }

    public bool GetFertilizer()
    {
        if (!hasFertilizer) return false;

        hasFertilizer = false;
        nextCompost = Time.time + compostInterval;
        return true;
    }
}
