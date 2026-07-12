using UnityEngine;

public class LandUnlockSystem : MonoBehaviour
{
    [Header("Cost")]
    [SerializeField]
    private int[] cost = new int[]
    {
        // Ante 1
        300, 450, 600,

        // Ante 2
        800, 1200, 1600,

        // Ante 3
        2000, 3000, 4000,

        // Ante 4
        5000, 7500, 10000,

        // Ante 5
        11000, 16500, 22000,

        // Ante 6
        20000, 30000, 40000,

        // Ante 7
        35000, 52500, 70000,

        // Ante 8
        50000, 75000, 100000
    };

    [Header("Runtime")]
    [SerializeField]
    private int landCount = 0;

    public int GetCurrentCost()
    {
        // Todo: Infinite land cost scaling
        return cost[landCount];
    }

    public bool CanUnlockLand()
    {
        Inventory inventory = GameManager.Main?.Inventory;
        if (inventory == null)
            return false;
        int currentCost = GetCurrentCost();
        return inventory.gold >= currentCost;
    }

    public bool UnlockLand()
    {
        Inventory inventory = GameManager.Main?.Inventory;
        if (inventory == null)
            return false;

        int currentCost = GetCurrentCost();
        if (!inventory.TrySpendGold(currentCost))
            return false;

        landCount++;
        return true;
    }
}
