using Sirenix.OdinInspector;
using UnityEngine;

public enum HarvestPatternKind
{
    Offsets,            // Exact relative cells (ex: cabbage effects 1 cell over, corn affects the cell 2 to the right and 1 up)
    Ray,                // Continuous in a line until blocked or edge
    // ring shape? technically we could do everything with offsets
}

[System.Serializable]
public struct cellOffset
{
    public int x;
    public int y;
}

[System.Serializable]
public class HarvestPattern
{
    public HarvestPatternKind kind = HarvestPatternKind.Offsets;

    // --- Kind->Offset based data ---
    [ShowIf("kind", HarvestPatternKind.Offsets)]
    [Tooltip("0,0 is this crops origin (the cell this crop is on). ex: x=-1, y=0 means the cell to the left of this crop")]
    public cellOffset[] offsets;

    // --- Kind->Ray based data ---
    [ShowIf("kind", HarvestPatternKind.Ray)]
    public Vector2Int direction;
    [ShowIf("kind", HarvestPatternKind.Ray)]
    [Tooltip("Ray length, -1 means until blocked or edge. 3 means only 3 cells straight")] 
    public int maxSteps = -1;

}
