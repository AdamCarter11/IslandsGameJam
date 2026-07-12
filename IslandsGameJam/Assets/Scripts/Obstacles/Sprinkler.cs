using ColorMak3r.Utility;
using Sirenix.OdinInspector;
using UnityEngine;

public class Sprinkler : MonoBehaviour
{
    private Vector2Int[] checkPositions = new Vector2Int[]
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right,
        Vector2Int.up + Vector2Int.left,
        Vector2Int.up + Vector2Int.right,
        Vector2Int.down + Vector2Int.left,
        Vector2Int.down + Vector2Int.right
    };

    [SerializeField]
    private float waterInterval = 5f;

    private float nextWater = 0f;

    private void Update()
    {
        if (Time.time > nextWater)
        {
            nextWater = Time.time + waterInterval;
            WaterCrop();
        }
    }

    [Button]
    public void WaterCrop()
    {
        var world = GameManager.Main.WorldManager;
        var position = transform.position.SnapToGrid().ToInt();
        foreach (var check in checkPositions)
        {
            if (world.TryGetCrop(position + check, out var cell))
            {
                cell.Water();
            }
        }
    }
}
