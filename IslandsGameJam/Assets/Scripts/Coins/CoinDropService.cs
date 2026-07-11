using UnityEngine;

/// <summary>
/// Spawns coin prefabs for a harvest payout using the prefab's denomination config
/// </summary>
public class CoinDropService : MonoBehaviour
{
    [SerializeField] Coin coinPrefab;
    [SerializeField] float scatterRadius = 0.35f;

    /// <summary>
    /// Decomposes totalAmount into denominations and instantiates coins near worldPos
    /// </summary>
    public void SpawnDrops(Vector2 worldPos, int totalAmount)
    {
        if (coinPrefab == null || totalAmount <= 0)
            return;

        var values = CoinBatcher.Decompose(totalAmount, coinPrefab.Denominations);
        for (int i = 0; i < values.Count; i++)
        {
            // TODO: improve this drop logic once we start polishing
            Vector2 offset = Random.insideUnitCircle * scatterRadius;
            Vector3 spawnPos = new Vector3(worldPos.x + offset.x, worldPos.y + offset.y, 0f);
            Coin coin = Instantiate(coinPrefab, spawnPos, Quaternion.identity);
            coin.Setup(values[i]);
        }
    }
}
