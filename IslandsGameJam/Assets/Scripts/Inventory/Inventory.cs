using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour 
{
    // --- Relic inventory management --- 
    public List<RelicSO> ownedRelics = new();
    public void AddRelic(RelicSO relic) => ownedRelics.Add(relic);

    // --- Seed inventory management ---
    // TODO

    // --- Cropo inventory management ---
    // TODO
}
